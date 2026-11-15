using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Megamod.Patching;

// Shared effective HarmonyArgument resolution. Attribute constructors are never executed;
// the inspected provider's injected name takes precedence over ordinary argument indexing.
internal static class HarmonyArgumentBinding
{
    private sealed record ArgumentAlias(string? Original, int Index, string? NewName);
    internal static (string Name, int Index) Resolve(MethodInfo target, ParameterInfo parameter)
    {
        var aliases = new List<ArgumentAlias>();
        void Add(IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes.Where(a => a.AttributeType.Name == nameof(HarmonyArgument)))
            {
                if (aliases.Count >= 64) throw new InvalidOperationException("Native argument metadata exceeds its bound.");
                aliases.Add(ParseAlias(attribute));
            }
        }
        var direct = parameter.CustomAttributes.Where(a => a.AttributeType.Name == nameof(HarmonyArgument)).FirstOrDefault();
        ArgumentAlias? specific = direct == null ? null : ParseAlias(direct);
        var callback = (MethodInfo)parameter.Member;
        Add(callback.CustomAttributes);
        int depth = 0;
        for (var type = callback.DeclaringType; type != null; type = type.BaseType)
        {
            if (++depth > 32) throw new InvalidOperationException("Native argument attribute inheritance exceeds its bound.");
            Add(type.CustomAttributes);
        }
        string raw = parameter.Name ?? string.Empty;
        var mapped = aliases.FirstOrDefault(a => a.Original == raw);
        string mappedName = !string.IsNullOrEmpty(mapped?.NewName) ? mapped!.NewName! : raw;
        string name = specific != null ? specific.Original ?? raw : mappedName;
        var originals = target.GetParameters();
        int index;
        if (specific != null && !string.IsNullOrEmpty(specific.Original)) index = Array.FindIndex(originals, p => p.Name == specific.Original);
        else if (specific != null && specific.Index >= 0 && specific.Index < originals.Length) index = specific.Index;
        else index = Array.FindIndex(originals, p => p.Name == mappedName);
        if (name.StartsWith("__", StringComparison.Ordinal) && int.TryParse(name.Substring(2), out int numbered) &&
            numbered >= 0 && numbered < originals.Length) index = numbered;
        return (name, index);
    }
    private static ArgumentAlias ParseAlias(CustomAttributeData attribute)
    {
        var arguments = attribute.ConstructorArguments;
        if (attribute.AttributeType != typeof(HarmonyArgument) || attribute.NamedArguments.Count != 0 || arguments.Count < 1 || arguments.Count > 2)
            throw new InvalidOperationException("Native argument alias has an unproved metadata shape.");
        string? original = null, replacement = null; int index = -1;
        if (arguments[0].ArgumentType == typeof(string)) original = (string?)arguments[0].Value;
        else if (arguments[0].ArgumentType == typeof(int)) index = (int)arguments[0].Value!;
        else throw new InvalidOperationException("Native argument alias source is unproved.");
        if (arguments.Count == 2)
        {
            if (arguments[1].ArgumentType == typeof(string)) replacement = (string?)arguments[1].Value;
            else if (arguments[0].ArgumentType != typeof(string) || arguments[1].ArgumentType != typeof(ArgumentMode) ||
                Convert.ToInt32(arguments[1].Value) != (int)ArgumentMode.Default)
                throw new InvalidOperationException("Native argument uses an unsupported binding mode.");
        }
        return new(original, index, replacement);
    }
}
