using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Megamod.Features.MessagePlacement;

// Non-owning participant: selected entry-call arguments only, never full geometry authority.
public sealed record MessageBarEntryConsumptionPatch(Type Rectangle, MethodInfo Clicks, MethodInfo Draw)
{
    internal (ISet<int>? Required, string? Failure) Describe(IReadOnlyList<CodeInstruction> emitted, int handoffEnd)
    {
        bool Callable(MethodInfo method) => method.IsPublic && !method.IsStatic && !method.IsAbstract &&
            !method.ContainsGenericParameters && !method.IsGenericMethod && method.ReturnType == typeof(void) &&
            method.GetParameters() is { Length: > 0 } parameters && parameters[0].ParameterType == Rectangle;
        if (!Rectangle.IsValueType || Clicks == Draw || !Callable(Clicks) || !Callable(Draw))
            return (null, "The selected entry consumers no longer accept the supplied rectangle value.");

        var required = new HashSet<int>();
        foreach (var consumer in new[] { Clicks, Draw })
        {
            var calls = Enumerable.Range(0, emitted.Count).Where(index => emitted[index].Calls(consumer)).ToArray();
            if (calls.Length != 1 || calls[0] <= handoffEnd)
                return (null, "A selected entry consumer call is missing, ambiguous or precedes the handoff.");
            required.Add(calls[0]);
        }
        return (required, null);
    }
}
