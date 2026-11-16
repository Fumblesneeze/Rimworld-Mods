using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.SettingsSearch.Harmony;

// Successful native-body completion is distinct from a postfix after a foreign skip-original.
public sealed class SettingsChooserCompletion : IDisposable
{
    private static readonly ConcurrentDictionary<MethodBase, SettingsChooserCompletion> routes = new();
    private static readonly MethodInfo NotifyMethod = typeof(SettingsChooserCompletion).GetMethod(nameof(Notify), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly SettingsChooserPublication publication;
    private readonly Action<object> completed;
    public SettingsChooserCompletion(MethodInfo target, Action<object> completed, Action<string> warning)
    { this.completed = completed; publication = new(target, Rewrite, warning); }
    public bool Install()
    {
        if (!routes.TryAdd(publication.Target, this)) return false;
        return publication.Install();
    }
    public bool Available => publication.Current();
    private List<CodeInstruction>? Rewrite(List<CodeInstruction> body, ILGenerator generator, long generation)
    {
        if (publication.Target.IsStatic || publication.Target.ReturnType != typeof(void) || !body.Any(code => code.opcode == OpCodes.Ret)) return null;
        var result = new List<CodeInstruction>();
        foreach (var code in body)
        {
            if (code.opcode == OpCodes.Ret)
            {
                var enter = new CodeInstruction(OpCodes.Ldarg_0);
                enter.labels.AddRange(code.labels); code.labels.Clear();
                // Return labels enter the callback, but EH markers stay at their native boundary.
                enter.blocks.AddRange(code.blocks); code.blocks.Clear();
                result.AddRange(new[] { enter, new CodeInstruction(OpCodes.Ldtoken, publication.Target),
                    new CodeInstruction(OpCodes.Ldc_I8, generation), new CodeInstruction(OpCodes.Call, NotifyMethod) });
            }
            result.Add(code);
        }
        return result;
    }
    private static void Notify(object instance, RuntimeMethodHandle handle, long generation)
    {
        if (routes.TryGetValue(MethodBase.GetMethodFromHandle(handle)!, out var route) && route.publication.Current(generation))
            route.completed(instance);
    }
    public void Dispose()
    { try { publication.Dispose(); } finally { routes.TryRemove(publication.Target, out _); } }
}

// Wraps exactly one existing virtual page draw, inside Window's exception catch boundary.
// The supplied forwarder performs the same native virtual call; no second page draw is issued.
public sealed class SettingsChooserPageDraw : IDisposable
{
    private static readonly ConcurrentDictionary<MethodBase, SettingsChooserPageDraw> routes = new();
    private static readonly MethodInfo ObserveMethod = typeof(SettingsChooserPageDraw).GetMethod(nameof(Observe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly SettingsChooserPublication publication;
    private readonly MethodInfo draw;
    private readonly Action<object, object> forward;
    private readonly Action<object, object, Action> observe;
    public SettingsChooserPageDraw(MethodInfo target, MethodInfo draw, Action<object, object> forward,
        Action<object, object, Action> observe, Action<string> warning)
    {
        this.draw = draw; this.forward = forward; this.observe = observe;
        publication = new(target, Rewrite, warning);
    }
    public bool Install()
    {
        if (!routes.TryAdd(publication.Target, this)) return false;
        return publication.Install();
    }
    public bool Available => publication.Current();
    private List<CodeInstruction>? Rewrite(List<CodeInstruction> body, ILGenerator generator, long generation)
    {
        var hits = body.Where(code => code.Calls(draw)).ToArray();
        if (hits.Length != 1 || hits[0].opcode != OpCodes.Callvirt || draw.IsStatic || draw.ReturnType != typeof(void) ||
            draw.GetParameters().Length != 1 || !draw.GetParameters()[0].ParameterType.IsValueType) return null;
        var result = new List<CodeInstruction>();
        foreach (var code in body)
        {
            if (!ReferenceEquals(code, hits[0])) { result.Add(code); continue; }
            var box = new CodeInstruction(OpCodes.Box, draw.GetParameters()[0].ParameterType);
            box.labels.AddRange(code.labels); box.blocks.AddRange(code.blocks);
            result.AddRange(new[] { box, new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldtoken, publication.Target), new CodeInstruction(OpCodes.Ldc_I8, generation),
                new CodeInstruction(OpCodes.Call, ObserveMethod) });
        }
        return result;
    }
    private static void Observe(object mod, object rect, object window, RuntimeMethodHandle handle, long generation)
    {
        if (!routes.TryGetValue(MethodBase.GetMethodFromHandle(handle)!, out var route))
            throw new InvalidOperationException("Native page observer route was removed while still executing.");
        Action original = () => route.forward(mod, rect);
        if (route.publication.Current(generation)) route.observe(mod, window, original);
        else original();
    }
    public void Dispose()
    { try { publication.Dispose(); } finally { routes.TryRemove(publication.Target, out _); } }
}
