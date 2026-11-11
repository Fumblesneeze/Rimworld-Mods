using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using HarmonyLib;

namespace Megamod.Patching;

public sealed class NativeDefinitionConversionPatch
{
    private static readonly ConcurrentDictionary<MethodBase, NativeDefinitionConversionPatch> Routes = new();
    private static long nextGeneration;
    private readonly MethodInfo? target;
    private readonly Type assetType;
    private readonly Type resultType;
    private readonly MethodInfo resolveNode;
    private readonly MethodInfo parse;
    private readonly Func<XmlNode, object?, IDisposable?> enter;
    private readonly Func<bool> safeToRefresh;
    private readonly Action? rejectedEntry;
    private readonly OwnedHarmonyPatch? receipt;
    private sealed record Validation(bool Accepted, int Version = 0, long Generation = 0,
        IReadOnlyList<HarmonyPublication.Operation>? Body = null);
    private Validation incoming = new(false);
    private Validation? publication;
    private volatile bool installed;
    private volatile bool enabled;

    public NativeDefinitionConversionPatch(Type converter, string methodName, Type assetType, Type resultType,
        MethodInfo resolveNode, MethodInfo parse, Func<XmlNode, object?, IDisposable?> enter, Func<bool>? safeToRefresh = null,
        Action? rejectedEntry = null)
    {
        this.assetType = assetType;
        this.resultType = resultType;
        this.resolveNode = resolveNode;
        this.parse = parse;
        this.enter = enter;
        this.safeToRefresh = safeToRefresh ?? (() => true);
        this.rejectedEntry = rejectedEntry;
        target = converter.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null, new[] { typeof(XmlNode), assetType }, null);
        if (target != null) receipt = new OwnedHarmonyPatch(target, HarmonyPatchType.Transpiler, Callback, Patches);
        string id = methodName == "DefFromNodeNew" ? "DDI-CONVERT-NEW" : "DDI-CONVERT-LEGACY";
        Candidate = new PatchCandidate(id, id,
            converter.FullName + "." + methodName, "Current definition context", "Ordinary native diagnostics",
            Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    internal MethodInfo? Target => target;
    public string? Failure { get; private set; }
    public void Enable()
    {
        if (!installed || !Refresh()) throw new InvalidOperationException("Conversion context is unavailable.");
        enabled = true;
    }
    private static MethodInfo Callback => typeof(NativeDefinitionConversionPatch).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.NonPublic)!;
    private string? Validate()
    {
        if (target == null || target.ReturnType != resultType || target.IsGenericMethod || target.DeclaringType!.IsGenericType ||
            resultType.IsValueType || assetType.IsValueType || target.GetMethodBody() == null)
            return "The exact synchronous native conversion callable changed.";
        if (!resolveNode.IsStatic || resolveNode.ReturnType != typeof(XmlNode) || resolveNode.IsGenericMethod ||
            !resolveNode.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(XmlNode) }))
            return "The native node-resolution callable changed.";
        if (parse.IsStatic || parse.Name != "Invoke" || parse.IsGenericMethod || !typeof(Delegate).IsAssignableFrom(parse.DeclaringType))
            return "The synchronous conversion delegate changed.";
        Type[] parameters = parse.GetParameters().Select(p => p.ParameterType).ToArray();
        if (!(parameters.SequenceEqual(new[] { typeof(XmlNode), typeof(bool) }) && parse.ReturnType == typeof(object)) &&
            !(parameters.SequenceEqual(new[] { typeof(int), typeof(int), typeof(XmlNode), typeof(Type) }) && parse.ReturnType == resultType))
            return "The native delegate arguments or result changed.";
        if (Patches().Any(IsFactory)) return "Conversion factories cannot be inspected safely.";
        return receipt!.ValidateOwnership();
    }
    private void Install()
    {
        if (target == null || receipt == null || Patches().Any(IsFactory)) throw new InvalidOperationException("Conversion ownership is unavailable.");
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("Conversion already has an owner.");
        receipt.Install();
        installed = true;
        if (!Refresh()) throw new InvalidOperationException(Failure ?? "Conversion stream was not admitted.");
    }
    private void Rollback()
    {
        enabled = installed = false;
        Volatile.Write(ref publication, null);
        try { receipt?.Rollback(); }
        finally
        {
            if (target != null && Routes.TryGetValue(target, out var route) && ReferenceEquals(route, this)) Routes.TryRemove(target, out _);
        }
    }
    private Patch[] Patches()
    {
        Patch[]? patches = null;
        if (!HarmonyPublication.Inspect(target!, snapshot => { patches = snapshot.Transpilers; return true; }))
            throw new InvalidOperationException("Published conversion metadata is busy or cannot be inspected during composition.");
        return patches!;
    }
    private static bool IsFactory(Patch patch) => patch.PatchMethod.IsStatic &&
        (patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod)) &&
        patch.PatchMethod.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    private bool Refresh()
    {
        var generation = Volatile.Read(ref incoming);
        if (!installed || !generation.Accepted) return false;
        if (ReferenceEquals(generation, Volatile.Read(ref publication))) return true;
        try { return HarmonyPublication.Inspect(target!, RefreshPublished); }
        catch (Exception exception) { Failure = exception.GetBaseException().Message; return false; }
    }
    internal object? PublishedGeneration
    {
        get
        {
            var generation = Volatile.Read(ref incoming);
            return installed && generation.Accepted && ReferenceEquals(generation, Volatile.Read(ref publication)) ? generation : null;
        }
    }
    internal bool RefreshPublished(HarmonyPublication.Snapshot snapshot)
    {
        var generation = Volatile.Read(ref incoming);
        if (!installed || !generation.Accepted) return false;
        if (snapshot.Version != generation.Version) { Failure = "The inspected conversion generation has not published."; return false; }
        var patches = snapshot.Transpilers;
        if (patches.Any(IsFactory) || patches.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            !patches.Any(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Callback) ||
            patches.Count(p => p.PatchMethod == Callback) != 1)
        { Failure = "Conversion factories or changed owned receipt prevent safe inspection."; return false; }
        if (ReferenceEquals(generation, Volatile.Read(ref publication))) return true;
        var sorted = PatchProcessor.GetSortedPatchMethods(target!, patches);
        if (sorted.Count == 0 || sorted[sorted.Count - 1] != Callback)
        { Failure = "The owned conversion adapter is not last in ordinary composition."; return false; }
        var body = generation.Body!;
        var required = new HashSet<int>();
        int enterIndex = -1, exitIndex = -1;
        for (int i = 0; i < body.Count; i++)
        {
            if (Equals(body[i].Operand, EnterMethod)) { enterIndex = i; ProtectInputs(i, 4); }
            if (Equals(body[i].Operand, ExitMethod)) { exitIndex = i; ProtectInputs(i, 1); }
            if (Equals(body[i].Operand, resolveNode)) ProtectInputs(i, 1);
            if (Equals(body[i].Operand, parse)) ProtectInputs(i, parse.GetParameters().Length);
        }
        void ProtectInputs(int call, int count)
        {
            required.Add(call);
            for (int i = call - 1; i >= 0 && count > 0; i--)
                if (body[i].Code != OpCodes.Nop) { required.Add(i); count--; }
        }
        string? failure = snapshot.FailureForConversion(body, required, enterIndex, exitIndex);
        if (failure != null) { Failure = failure; return false; }
        if (!installed || !ReferenceEquals(generation, Volatile.Read(ref incoming))) return false;
        Failure = null;
        Volatile.Write(ref publication, generation);
        return true;
    }
    private static MethodInfo EnterMethod => typeof(NativeDefinitionConversionPatch).GetMethod(nameof(Enter), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static MethodInfo ExitMethod => typeof(NativeDefinitionConversionPatch).GetMethod(nameof(Exit), BindingFlags.Static | BindingFlags.NonPublic)!;

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return body;
        Volatile.Write(ref route.publication, null);
        Volatile.Write(ref route.incoming, new Validation(false));
        int version;
        try { version = HarmonyPublication.ExpectedVersion(__originalMethod); }
        catch (Exception exception)
        {
            route.Failure = "Conversion publication metadata is unavailable: " + exception.GetBaseException().Message;
            return body;
        }
        long generation = Interlocked.Increment(ref nextGeneration);
        if (!route.Supported(body)) { route.Failure = "The incoming synchronous node-binding or exception/return shape changed."; return body; }
        LocalBuilder token = generator.DeclareLocal(typeof(IDisposable));
        LocalBuilder result = generator.DeclareLocal(route.resultType);
        Label finished = generator.DefineLabel();
        var output = new List<CodeInstruction>
        {
            new(OpCodes.Ldarg_0), new(OpCodes.Ldarg_1), new(OpCodes.Ldtoken, __originalMethod),
            new(OpCodes.Ldc_I8, generation), new(OpCodes.Call, EnterMethod),
            new(OpCodes.Stloc, token),
            new(OpCodes.Nop) { blocks = new List<ExceptionBlock> { new(ExceptionBlockType.BeginExceptionBlock) } }
        };
        foreach (var original in body)
        {
            var instruction = new CodeInstruction(original);
            if (instruction.opcode == OpCodes.Ret)
            {
                instruction.opcode = OpCodes.Stloc;
                instruction.operand = result;
                output.Add(instruction);
                output.Add(new CodeInstruction(OpCodes.Leave, finished));
            }
            else output.Add(instruction);
        }
        output.Add(new CodeInstruction(OpCodes.Nop) { blocks = new List<ExceptionBlock> { new(ExceptionBlockType.BeginFinallyBlock) } });
        output.Add(new CodeInstruction(OpCodes.Ldloc, token));
        output.Add(new CodeInstruction(OpCodes.Call, ExitMethod));
        output.Add(new CodeInstruction(OpCodes.Nop) { blocks = new List<ExceptionBlock> { new(ExceptionBlockType.EndExceptionBlock) } });
        output.Add(new CodeInstruction(OpCodes.Ldloc, result) { labels = new List<Label> { finished } });
        output.Add(new CodeInstruction(OpCodes.Ret));
        Volatile.Write(ref route.incoming, new Validation(true, version, generation,
            Array.AsReadOnly(output.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray())));
        return output;
    }

    private bool Supported(List<CodeInstruction> body)
    {
        var meaningful = body.Where(i => i.opcode != OpCodes.Nop).ToList();
        if (!body.Any(i => i.opcode == OpCodes.Ret) || body.Any(i => i.opcode == OpCodes.Tailcall || i.opcode == OpCodes.Jmp ||
            i.opcode.Name!.StartsWith("starg", StringComparison.Ordinal) || i.opcode.Name.StartsWith("ldarga", StringComparison.Ordinal))) return false;
        int depth = 0;
        foreach (var instruction in body)
        {
            foreach (var block in instruction.blocks)
            {
                if (block.blockType == ExceptionBlockType.BeginExceptionBlock) depth++;
                if (block.blockType == ExceptionBlockType.EndExceptionBlock && --depth < 0) return false;
                if (block.blockType is ExceptionBlockType.BeginExceptFilterBlock or ExceptionBlockType.BeginFaultBlock) return false;
            }
            if (instruction.opcode == OpCodes.Ret && (depth != 0 || instruction.blocks.Count != 0)) return false;
        }
        if (depth != 0 || meaningful.Count(i => i.Calls(resolveNode)) != 1 || meaningful.Count(i => i.Calls(parse)) != 1) return false;
        int resolve = meaningful.FindIndex(i => i.Calls(resolveNode));
        int invoke = meaningful.FindIndex(i => i.Calls(parse));
        if (resolve < 1 || meaningful[resolve - 1].opcode != OpCodes.Ldarg_0) return false;
        if (!BoundInput(body, meaningful[resolve - 1], meaningful[resolve]) ||
            invoke < 2 || !BoundInput(body, meaningful[invoke - 2], meaningful[invoke])) return false;
        return parse.GetParameters().Length == 2
            ? invoke >= 2 && meaningful[invoke - 2].opcode == OpCodes.Ldarg_0 && meaningful[invoke - 1].opcode == OpCodes.Ldc_I4_1
            : invoke >= 4 && meaningful[invoke - 4].opcode == OpCodes.Ldc_I4_0 && meaningful[invoke - 3].opcode == OpCodes.Ldc_I4_0 &&
                meaningful[invoke - 2].opcode == OpCodes.Ldarg_0 && meaningful[invoke - 1].opcode.Name!.StartsWith("ldloc", StringComparison.Ordinal);
    }
    private static bool BoundInput(List<CodeInstruction> body, CodeInstruction first, CodeInstruction last)
    {
        int start = body.IndexOf(first);
        int end = body.IndexOf(last);
        var protectedInstructions = body.Skip(start + 1).Take(end - start).ToArray();
        if (protectedInstructions.Any(i => i.blocks.Count != 0)) return false;
        var protectedLabels = new HashSet<Label>(protectedInstructions.SelectMany(i => i.labels));
        return !body.Any(i => i.operand is Label label && protectedLabels.Contains(label) ||
            i.operand is Label[] labels && labels.Any(protectedLabels.Contains));
    }
    private static IDisposable? Enter(XmlNode node, object? asset, RuntimeMethodHandle handle, long bodyGeneration)
    {
        NativeDefinitionConversionPatch? route = null;
        bool acquired = false;
        try
        {
            var target = MethodBase.GetMethodFromHandle(handle);
            if (target == null || !Routes.TryGetValue(target, out route) || !route.enabled ||
                Volatile.Read(ref route.incoming).Generation != bodyGeneration || !route.safeToRefresh() || !route.Refresh()) return null;
            var generation = Volatile.Read(ref route.incoming);
            if (generation.Generation != bodyGeneration || !ReferenceEquals(generation, Volatile.Read(ref route.publication))) return null;
            var token = route.enter(node, asset);
            acquired = token != null;
            return token;
        }
        catch (Exception) { return null; }
        finally
        {
            // A rejected older body must not expose an otherwise still-valid caller's identity.
            if (!acquired) { try { route?.rejectedEntry?.Invoke(); } catch (Exception) { } }
        }
    }
    private static void Exit(IDisposable? token)
    {
        try { token?.Dispose(); }
        catch (Exception) { /* Diagnostic cleanup cannot replace the conversion result or exception. */ }
    }
}
