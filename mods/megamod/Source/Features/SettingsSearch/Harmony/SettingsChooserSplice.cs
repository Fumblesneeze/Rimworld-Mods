using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.SettingsSearch.Harmony;

// The chooser's original foreach and opening block remain executable. Only the owned
// presentation branches around the native filter/card drawing, inside that same foreach.
public sealed class SettingsChooserSplice : IDisposable
{
    public delegate object SelectSource(object source, object dialog, object listing, out object? context);
    private readonly FieldInfo source;
    private readonly MethodInfo button, add;
    private readonly SelectSource select;
    private readonly Func<object, object, bool> draw;
    private readonly Action<object>? completed;
    private readonly Action<object, object>? opened;
    private readonly SettingsChooserPublication publication;
    private static readonly ConcurrentDictionary<MethodBase, SettingsChooserSplice> routes = new();
    private sealed record Invocation(SettingsChooserSplice Owner, long Generation, object Context);
    private static readonly MethodInfo SourceMethod = typeof(SettingsChooserSplice).GetMethod(nameof(Source), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo DrawMethod = typeof(SettingsChooserSplice).GetMethod(nameof(Draw), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CompleteMethod = typeof(SettingsChooserSplice).GetMethod(nameof(Complete), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo OpenedMethod = typeof(SettingsChooserSplice).GetMethod(nameof(Opened), BindingFlags.Static | BindingFlags.NonPublic)!;

    public SettingsChooserSplice(MethodInfo target, FieldInfo source, MethodInfo button, MethodInfo add,
        SelectSource select, Func<object, object, bool> draw, Action<string> warning, Action<object>? completed = null,
        Action<object, object>? opened = null)
    {
        this.source = source; this.button = button; this.add = add; this.select = select; this.draw = draw; this.completed = completed;
        this.opened = opened;
        publication = new(target, Rewrite, warning);
    }
    public string? Failure => publication.Failure;
    public bool Available => publication.Current();
    public bool Install()
    {
        if (!routes.TryAdd(publication.Target, this) && !ReferenceEquals(routes[publication.Target], this)) return false;
        return publication.Install();
    }
    public void Dispose()
    {
        try { publication.Dispose(); }
        finally { routes.TryRemove(publication.Target, out _); }
    }
    private static object Source(object native, object dialog, object listing, RuntimeMethodHandle handle, long generation, out object? context)
    {
        context = null;
        if (!routes.TryGetValue(MethodBase.GetMethodFromHandle(handle)!, out var route) || !route.publication.Current(generation)) return native;
        var selected = route.select(native, dialog, listing, out var owned);
        if (owned != null) context = new Invocation(route, generation, owned);
        return selected;
    }
    private static bool Draw(object context, object current)
    {
        var invocation = (Invocation)context;
        return invocation.Owner.publication.Current(invocation.Generation) && invocation.Owner.draw(invocation.Context, current);
    }
    private static void Complete(object dialog, RuntimeMethodHandle handle, long generation)
    {
        if (routes.TryGetValue(MethodBase.GetMethodFromHandle(handle)!, out var route) && route.publication.Current(generation))
            route.completed?.Invoke(dialog);
    }
    private static void Opened(object? context, object window)
    {
        if (context is Invocation invocation && invocation.Owner.publication.Current(invocation.Generation))
            invocation.Owner.opened?.Invoke(invocation.Context, window);
    }

    private List<CodeInstruction>? Rewrite(List<CodeInstruction> body, ILGenerator generator, long generation)
    {
        int[] meaningful = Enumerable.Range(0, body.Count).Where(i => body[i].opcode != OpCodes.Nop).ToArray();
        int Position(int index) => Array.IndexOf(meaningful, index);
        int Next(int index, int count = 1) { int p = Position(index) + count; return p < meaningful.Length ? meaningful[p] : -1; }
        int Unique(Func<CodeInstruction, bool> predicate)
        { var hits = Enumerable.Range(0, body.Count).Where(i => predicate(body[i])).ToArray(); return hits.Length == 1 ? hits[0] : -1; }
        int sourceIndex = Unique(code => code.opcode == OpCodes.Ldfld && Equals(code.operand, source));
        int click = Unique(code => code.Calls(button));
        int opening = Unique(code => code.Calls(add));
        int current = Unique(code => code.operand is MethodInfo method && method.Name == "get_Current" &&
            method.DeclaringType?.IsGenericType == true && method.DeclaringType.GetGenericTypeDefinition() == typeof(IEnumerator<>));
        int move = Unique(code => code.operand is MethodInfo method && method.Name == nameof(System.Collections.IEnumerator.MoveNext) &&
            method.DeclaringType == typeof(System.Collections.IEnumerator));
        if (sourceIndex < 0 || click < 0 || opening < 0 || current < 0 || move < 0) return null;
        int enumeration = Next(sourceIndex), store = Next(current), gate = Next(click);
        if (enumeration < 0 || store < 0 || gate < 0 ||
            body[enumeration].operand is not MethodInfo getEnumerator || getEnumerator.Name != "GetEnumerator" ||
            getEnumerator.GetParameters().Length != 0 || getEnumerator.ReturnType != ((MethodInfo)body[current].operand).DeclaringType ||
            !source.FieldType.IsAssignableFrom(getEnumerator.DeclaringType) ||
            !TryLocal(body[store], false, out int currentLocal) ||
            (body[gate].opcode != OpCodes.Brfalse && body[gate].opcode != OpCodes.Brfalse_S) || body[gate].operand is not Label after) return null;
        int continuation = body.FindIndex(code => code.labels.Contains(after));
        int filter = Next(store);
        int moveReceiver = meaningful[Position(move) - 1];
        int currentReceiver = meaningful[Position(current) - 1];
        if (!(sourceIndex < current && store < click && gate < opening && opening < continuation && continuation < moveReceiver) ||
            !TryLocal(body[moveReceiver], true, out int enumeratorLocal) ||
            !TryLocal(body[currentReceiver], true, out int readEnumerator) || enumeratorLocal != readEnumerator ||
            Next(enumeration) < 0 || !TryLocal(body[Next(enumeration)], false, out int storedEnumerator) || storedEnumerator != enumeratorLocal) return null;

        // The concrete preserved opening pattern: WindowStack receiver, current Mod, constructor
        // OR a compatible static Mod -> Window factory, Add. Do not invoke/replace that factory.
        int[] open = meaningful.Where(i => i > gate && i <= opening).ToArray();
        if (open.Length != 4 || body[open[0]].operand is not MethodInfo stackGetter || !stackGetter.IsStatic ||
            stackGetter.GetParameters().Length != 0 || stackGetter.ReturnType != add.DeclaringType ||
            !TryLocal(body[open[1]], true, out int openingLocal) || openingLocal != currentLocal) return null;
        Type pageType = ((MethodInfo)body[current].operand).ReturnType;
        Type windowType = add.GetParameters().Single().ParameterType;
        bool constructor = body[open[2]].opcode == OpCodes.Newobj && body[open[2]].operand is ConstructorInfo ctor &&
            windowType.IsAssignableFrom(ctor.DeclaringType) && ctor.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { pageType });
        bool factory = body[open[2]].opcode == OpCodes.Call && body[open[2]].operand is MethodInfo method && method.IsStatic &&
            windowType.IsAssignableFrom(method.ReturnType) && method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { pageType });
        if (!constructor && !factory) return null;

        // A branch into a consumed producer/opening chain would bypass its required stack inputs.
        var protectedInterior = new HashSet<int> { enumeration, current, store, open[1], open[2], opening };
        foreach (var code in body)
        {
            IEnumerable<Label> destinations = code.operand is Label label ? new[] { label } : code.operand is Label[] labels ? labels : Array.Empty<Label>();
            if (destinations.Any(label => protectedInterior.Any(index => body[index].labels.Contains(label)))) return null;
        }
        // No current-object replacement between its producing store and the preserved opener.
        if (Enumerable.Range(filter, opening - filter).Any(i =>
            TryLocal(body[i], false, out int local) && local == currentLocal ||
            body[i].opcode.Name?.StartsWith("ldloca", StringComparison.Ordinal) == true && LocalIndex(body[i].operand) == currentLocal)) return null;

        var context = generator.DeclareLocal(typeof(object));
        var openedWindow = opened == null ? null : generator.DeclareLocal(typeof(object));
        Label nativeFilter = generator.DefineLabel(), clickGate = generator.DefineLabel(), loop = generator.DefineLabel();
        body[filter].labels.Add(nativeFilter);
        body[gate].labels.Add(clickGate);
        body[moveReceiver].labels.Add(loop);
        var result = new List<CodeInstruction>();
        for (int i = 0; i < body.Count; i++)
        {
            if (body[i].opcode == OpCodes.Ret && completed != null)
            {
                var enter = new CodeInstruction(OpCodes.Ldarg_0);
                enter.labels.AddRange(body[i].labels); body[i].labels.Clear();
                result.AddRange(new[] { enter, new CodeInstruction(OpCodes.Ldtoken, publication.Target),
                    new CodeInstruction(OpCodes.Ldc_I8, generation), new CodeInstruction(OpCodes.Call, CompleteMethod) });
            }
            if (i == opening && openedWindow != null)
                result.AddRange(new[] { new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Stloc, openedWindow) });
            if (i == filter)
            {
                // Existing labels entering the complete filter must enter our dispatch too.
                var enter = new CodeInstruction(OpCodes.Ldloc, context);
                enter.labels.AddRange(body[i].labels.Where(label => !label.Equals(nativeFilter)));
                body[i].labels.RemoveAll(label => !label.Equals(nativeFilter));
                result.AddRange(new[] { enter, new CodeInstruction(OpCodes.Brfalse, nativeFilter),
                    new CodeInstruction(OpCodes.Ldloc, context), LoadLocal(currentLocal),
                    new CodeInstruction(OpCodes.Call, DrawMethod), new CodeInstruction(OpCodes.Br, clickGate) });
            }
            if (i == continuation)
            {
                var enter = new CodeInstruction(OpCodes.Ldloc, context);
                enter.labels.AddRange(body[i].labels); body[i].labels.Clear();
                result.AddRange(new[] { enter, new CodeInstruction(OpCodes.Brtrue, loop) });
            }
            result.Add(body[i]);
            if (i == opening && openedWindow != null)
                result.AddRange(new[] { new CodeInstruction(OpCodes.Ldloc, context), new CodeInstruction(OpCodes.Ldloc, openedWindow),
                    new CodeInstruction(OpCodes.Call, OpenedMethod) });
            if (i == sourceIndex)
                result.AddRange(new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldtoken, publication.Target), new CodeInstruction(OpCodes.Ldc_I8, generation),
                    new CodeInstruction(OpCodes.Ldloca, context), new CodeInstruction(OpCodes.Call, SourceMethod),
                    new CodeInstruction(OpCodes.Castclass, source.FieldType) });
        }
        return result;
    }

    private static CodeInstruction LoadLocal(int index) => new(OpCodes.Ldloc, index);
    private static bool TryLocal(CodeInstruction code, bool load, out int index)
    {
        index = -1;
        var name = code.opcode.Name ?? "";
        string prefix = load ? "ldloc" : "stloc";
        if (name == prefix || name == prefix + ".s")
        { index = LocalIndex(code.operand); return true; }
        if (name.StartsWith(prefix + ".", StringComparison.Ordinal) && int.TryParse(name.Substring(prefix.Length + 1), out index)) return true;
        return false;
    }
    private static int LocalIndex(object operand) => operand is LocalBuilder builder ? builder.LocalIndex :
        operand is LocalVariableInfo info ? info.LocalIndex : Convert.ToInt32(operand);
}

// Shared only by the chooser's three native recipes. Reads one actual provider publication;
// neither original-body convenience APIs nor foreign transpilers are executed as probes.
internal sealed class SettingsChooserPublication : IDisposable
{
    private static readonly ConcurrentDictionary<MethodBase, SettingsChooserPublication> routes = new();
    private static readonly MethodInfo Callback = typeof(SettingsChooserPublication).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly Func<List<CodeInstruction>, ILGenerator, long, List<CodeInstruction>?> rewrite;
    private readonly Action<string> warning;
    private readonly OwnedHarmonyPatch receipt;
    private long generation;
    private int version;
    private List<CodeInstruction>? emitted;
    private HarmonyPublication.Identity? identity;
    private bool installed, warned;
    internal MethodInfo Target { get; }
    internal string? Failure { get; private set; }
    internal SettingsChooserPublication(MethodInfo target,
        Func<List<CodeInstruction>, ILGenerator, long, List<CodeInstruction>?> rewrite, Action<string> warning)
    {
        Target = target; this.rewrite = rewrite; this.warning = warning;
        receipt = new(target, HarmonyPatchType.Transpiler, Callback, () =>
        {
            Patch[] patches = Array.Empty<Patch>();
            if (!HarmonyPublication.Inspect(target, snapshot => { patches = snapshot.Transpilers; return true; }))
                throw new InvalidOperationException("Chooser publication unavailable.");
            return patches;
        });
    }
    internal bool Install()
    {
        if (installed) return Current();
        if (!routes.TryAdd(Target, this)) return false;
        try { receipt.Install(); installed = true; return Current(); }
        catch (Exception error) { Fail(error.GetBaseException().Message); Dispose(); return false; }
    }
    internal bool Current(long expected = 0)
    {
        if (!installed || emitted == null || expected != 0 && expected != generation) return false;
        if (identity?.IsCurrent() == true) return true;
        identity = null;
        try
        {
            bool accepted = HarmonyPublication.Inspect(Target, snapshot =>
            {
                if (snapshot.Version != version) return false;
                var patches = snapshot.Transpilers;
                if (patches.Any(p => IsFactory(p.PatchMethod)) || patches.Count(p => p.PatchMethod == Callback) != 1) return false;
                var sorted = PatchProcessor.GetSortedPatchMethods(Target, patches);
                if (sorted.Count == 0 || sorted[sorted.Count - 1] != Callback) return false;
                var operations = emitted.Select(code => new HarmonyPublication.Operation(code.opcode, code.operand)).ToArray();
                // This short recipe owns its consumed body. Read-only inner observers are allowed;
                // an inner writer at a consumed operation makes this invocation native fallback.
                if (snapshot.FailureFor(operations, new HashSet<int>(Enumerable.Range(0, operations.Length))) != null) return false;
                identity = snapshot.Identity; return true;
            });
            if (!accepted) Fail("Required effective chooser composition is unavailable; native navigation is retained.");
            return accepted;
        }
        catch (Exception error) { Fail(error.GetBaseException().Message); return false; }
    }
    private static bool IsFactory(MethodInfo method) => method.IsStatic &&
        (method.ReturnType == typeof(MethodInfo) || method.ReturnType == typeof(DynamicMethod)) &&
        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    private void Fail(string reason)
    {
        Failure = reason.Length > 320 ? reason.Substring(0, 320) : reason;
        if (warned) return; warned = true;
        try { warning("[fumblesneeze.megamod] S047/S141 chooser " + Target.DeclaringType?.FullName + "." + Target.Name + ": " + Failure); } catch { }
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var incoming = instructions.ToList();
        if (!routes.TryGetValue(__originalMethod, out var route)) return incoming;
        route.identity = null; route.emitted = null; route.generation++;
        try
        {
            route.version = HarmonyPublication.ExpectedVersion(__originalMethod);
            var clone = incoming.Select(code => new CodeInstruction(code)).ToList();
            var result = route.rewrite(clone, generator, route.generation);
            if (result == null) { route.Fail("Required native chooser operands/control flow changed; original incoming code retained."); return incoming; }
            route.emitted = result; return result;
        }
        catch (Exception error) { route.Fail(error.GetBaseException().Message); return incoming; }
    }
    public void Dispose()
    {
        installed = false; identity = null; emitted = null;
        try { receipt.Rollback(); }
        finally { routes.TryRemove(Target, out _); }
    }
}
