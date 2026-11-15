using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Megamod.Patching;

// Detached metadata policy only. The provider adapter supplies a known Dictionary under its locks;
// this general collection API is not a promise to invoke arbitrary enumerators without effects.
internal sealed class PublishedMethodFamilyInventory
{
    private const int MaximumEntries = 65536, MaximumMembers = 256, MaximumPayloadBytes = 4 * 1024 * 1024;
    private static readonly Type RuntimeMethod = typeof(object).GetMethod(nameof(ToString))!.GetType();
    private static readonly Type RuntimeConstructor = typeof(object).GetConstructor(Type.EmptyTypes)!.GetType();
    private static readonly Type? RuntimeDynamicMethod = typeof(DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.NonPublic);
    private readonly MethodInfo definition;
    private readonly Dictionary<MethodInfo, byte[]> members;
    internal IReadOnlyList<MethodInfo> Members { get; }

    private PublishedMethodFamilyInventory(MethodInfo definition, Dictionary<MethodInfo, byte[]> members)
    {
        this.definition = definition;
        this.members = members;
        Members = Array.AsReadOnly(members.Keys.ToArray());
    }

    internal static bool TryCreate(MethodInfo definition,
        IReadOnlyCollection<KeyValuePair<MethodBase, byte[]>> publications,
        out PublishedMethodFamilyInventory? inventory)
    {
        inventory = null;
        if (definition is null || definition.GetType() != RuntimeMethod ||
            !definition.IsGenericMethodDefinition || definition.DeclaringType == null ||
            definition.DeclaringType.IsGenericType || definition.ReflectedType != definition.DeclaringType ||
            publications == null) return false;
        try
        {
            int count = publications.Count;
            if (count < 0 || count > MaximumEntries) return false;
            var selected = new Dictionary<MethodInfo, byte[]>();
            int observed = 0;
            foreach (var entry in publications)
            {
                if (++observed > count || entry.Key is null) return false;
                Type keyType = entry.Key.GetType();
                // These exact native representations cannot be generic method-family members.
                // Do not call virtual metadata on an unfamiliar MethodBase wrapper to guess.
                if (keyType == RuntimeConstructor || keyType == typeof(DynamicMethod) || keyType == RuntimeDynamicMethod) continue;
                if (keyType != RuntimeMethod) return false;
                var method = (MethodInfo)entry.Key;
                if (!SameDeclaration(method, definition)) continue;
                if (!method.IsGenericMethod || !SameDeclaration(method.GetGenericMethodDefinition(), definition) ||
                    entry.Value == null || entry.Value.Length == 0 || entry.Value.Length > MaximumPayloadBytes ||
                    selected.Count >= MaximumMembers || !selected.TryAdd(method, entry.Value)) return false;
            }
            if (observed != count) return false;
            inventory = new PublishedMethodFamilyInventory(definition, selected);
            return true;
        }
        catch (Exception) { return false; }
    }

    internal bool SamePublications(PublishedMethodFamilyInventory other) =>
        other != null && definition == other.definition && members.Count == other.members.Count &&
        members.All(entry => other.members.TryGetValue(entry.Key, out var payload) && ReferenceEquals(entry.Value, payload));

    private static bool SameDeclaration(MethodInfo method, MethodInfo expected) =>
        method.Module == expected.Module && method.MetadataToken == expected.MetadataToken &&
        method.DeclaringType == expected.DeclaringType;
}
