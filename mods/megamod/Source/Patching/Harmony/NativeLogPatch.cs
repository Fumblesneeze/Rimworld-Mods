using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace Megamod.Patching;

// Shared native output slot for S094 context and S035 capture policy.
public sealed class NativeLogPatch
{
    private static readonly ConcurrentDictionary<MethodBase, NativeLogPatch> Routes = new();
    private static long nextGeneration;
    private readonly Type logType;
    private readonly string methodName;
    private readonly Func<string, string> project;
    private readonly Action<string>? failureRecorder;
    private readonly Func<string, bool>? capture;
    private MethodInfo? target;
    private FieldInfo? category;
    private MethodInfo? conversion;
    private MethodInfo? output;
    private MethodInfo? preventLogging;
    private OwnedHarmonyPatch? receipt;
    private sealed record IncomingValidation(bool Accepted, string? CaptureFailure = null, int Version = 0,
        IReadOnlyList<HarmonyPublication.Operation>? Body = null, long Generation = 0);
    private IncomingValidation incomingValidation = new(false);
    private IncomingValidation? publication;
    private volatile bool enabled;
    private volatile bool installed;
    private volatile bool faulted;
    private volatile string? failure;
    private int failureReported;
    private int captureFaulted;
    private volatile string? captureFailure;
    private volatile string? publishedCaptureFailure;
    private int captureSemanticsReported;

    public NativeLogPatch(Type logType, string methodName, Func<string, string> project, Action<string>? failureRecorder = null,
        Func<string, bool>? capture = null)
    {
        this.logType = logType;
        this.methodName = methodName;
        this.project = project;
        this.failureRecorder = failureRecorder;
        this.capture = capture;
        string id = "DDI-REPORT-" + methodName.ToUpperInvariant();
        Candidate = new PatchCandidate(id, id, logType.FullName + "." + methodName,
            capture == null ? "Definition diagnostic context" : "Warning muting and definition-context warnings",
            "Ordinary native diagnostics without owned context or muting",
            Validate, Install, Rollback);
    }

    public PatchCandidate Candidate { get; }
    public string? Failure => failure;
    public string? CaptureFailure => captureFailure ?? publishedCaptureFailure ?? Volatile.Read(ref incomingValidation).CaptureFailure;
    public bool CaptureAvailable
    {
        get
        {
            IncomingValidation generation = Volatile.Read(ref incomingValidation);
            return capture != null && enabled && installed && !faulted && generation.CaptureFailure == null && publishedCaptureFailure == null &&
                ReferenceEquals(generation, Volatile.Read(ref publication)) && Volatile.Read(ref captureFaulted) == 0;
        }
    }
    public bool Available
    {
        get
        {
            IncomingValidation generation = Volatile.Read(ref incomingValidation);
            return installed && !faulted && ReferenceEquals(generation, Volatile.Read(ref publication));
        }
    }

    // Composition owner only, outside native logging locks. Never call from a log callback.
    // A fresh immutable identity represents every transpiler execution, including rejected streams.
    public bool RefreshPublication()
    {
        Volatile.Write(ref publication, null);
        IncomingValidation generation = Volatile.Read(ref incomingValidation);
        if (!installed || faulted || !generation.Accepted) return false;
        try
        {
            return HarmonyPublication.Inspect(target!, snapshot =>
            {
                if (snapshot.Version != generation.Version) { failure = "The inspected reporting generation has not published."; return false; }
                string? orderingFailure = OrderingFailure(snapshot.Transpilers);
                if (orderingFailure != null) { failure = orderingFailure; return false; }
                string? effects = snapshot.FailureFor(generation.Body!, ProtectedOperations(generation.Body!, captureOnly: false));
                if (effects != null) { failure = effects; return false; }
                publishedCaptureFailure = snapshot.FailureFor(generation.Body!, ProtectedOperations(generation.Body!, captureOnly: true));
                if (!installed || faulted || !ReferenceEquals(generation, Volatile.Read(ref incomingValidation))) return false;
                failure = null;
                Volatile.Write(ref publication, generation);
                return Available;
            });
        }
        catch (Exception exception) { failure = exception.GetBaseException().Message; return false; }
    }

    private ISet<int> ProtectedOperations(IReadOnlyList<HarmonyPublication.Operation> body, bool captureOnly)
    {
        MethodInfo helper = typeof(NativeLogPatch).GetMethod(captureOnly ? nameof(ShouldCapture) : nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!;
        var members = new HashSet<MemberInfo> { helper };
        if (!captureOnly) members.UnionWith(new MemberInfo[] { preventLogging!, category!, conversion!, output! });
        var result = new HashSet<int>();
        bool provenance = false;
        for (int i = 0; i < body.Count; i++)
        {
            var operation = body[i];
            if (operation.Operand is MemberInfo member && members.Contains(member)) result.Add(i);
            if (Equals(operation.Operand, helper)) { result.Add(i - 1); result.Add(i - 2); }
            if (!captureOnly)
            {
                if (Equals(operation.Operand, conversion)) provenance = true;
                if (provenance) result.Add(i);
                if (Equals(operation.Operand, output)) provenance = false;
            }
        }
        return result;
    }

    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("The native diagnostic route is unavailable.");
        enabled = true;
    }

    public void Disable() => enabled = false;

    // Owner-side notification only when the user requests eligible muting; a dormant capability
    // must neither reject valid context nor create a skipped-enabled-feature warning.
    public void ReportCaptureUnavailable(bool requested)
    {
        IncomingValidation generation = Volatile.Read(ref incomingValidation);
        string? semanticFailure = generation.CaptureFailure ?? publishedCaptureFailure;
        if (!requested || capture == null || !enabled || !installed || faulted ||
            !ReferenceEquals(generation, Volatile.Read(ref publication)) || semanticFailure == null ||
            Interlocked.Exchange(ref captureSemanticsReported, 1) != 0) return;
        try
        {
            failureRecorder?.Invoke($"[fumblesneeze.megamod] LOG-CAPTURE-WARNING unavailable; target={Candidate.Target}; " +
                $"targetSpec=LOG-CAPTURE-WARNING; reason={Bound(semanticFailure)}; unavailable=warning muting; " +
                "fallback=ordinary warnings; definition context remains independently gated. Stored request preserved.");
        }
        catch (Exception) { /* A failed protected explanation cannot reject the working context route. */ }
    }

    private string? Validate()
    {
        const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Static;
        if (methodName is not ("Warning" or "Error") || logType.IsGenericType || logType.ContainsGenericParameters)
            return "The requested native diagnostic surface is unsupported.";
        target = logType.GetMethod(methodName, declared | BindingFlags.Public, null, new[] { typeof(string) }, null);
        category = logType.GetField("logCategory", declared | BindingFlags.NonPublic);
        preventLogging = logType.GetMethod("get_PreventLogging", declared | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (target == null || target.ReturnType != typeof(void) || target.IsGenericMethod || category == null)
            return "The native static string/void entry point or category field changed.";
        if (preventLogging == null || preventLogging.ReturnType != typeof(bool) || preventLogging.IsGenericMethod)
            return "The native diagnostic suppression getter changed.";
        var outputs = category.FieldType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == methodName && method.ReturnType == typeof(void) && !method.IsGenericMethod)
            .Where(method => method.GetParameters().Select(parameter => parameter.ParameterType).Skip(1)
                .SequenceEqual(new[] { typeof(string), typeof(string), typeof(int) }))
            .Where(method =>
            {
                Type text = method.GetParameters()[0].ParameterType;
                return text.IsValueType && text.IsGenericType && !text.ContainsGenericParameters;
            })
            .Select(method => (Output: method, Conversions: method.GetParameters()[0].ParameterType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(candidate => candidate.Name == "op_Implicit" && !candidate.IsGenericMethod &&
                    candidate.ReturnType == method.GetParameters()[0].ParameterType &&
                    candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(string) }))
                .ToArray()))
            .Where(candidate => candidate.Conversions.Length != 0)
            .ToArray();
        // Exception and other unrelated overloads are not candidates for the consumed string route.
        // Cardinality is checked only after the complete callable/conversion prerequisites.
        if (outputs.Length != 1 || outputs[0].Conversions.Length != 1)
            return "The native diagnostic category output/string conversion is missing or ambiguous.";
        output = outputs[0].Output;
        conversion = outputs[0].Conversions[0];
        if (HasFactory()) return "Diagnostic transpiler factories cannot be inspected without executing foreign code.";
        receipt ??= new OwnedHarmonyPatch(target, HarmonyPatchType.Transpiler,
            typeof(NativeLogPatch).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)!, ReadTranspilers);
        return receipt.ValidateOwnership();
    }

    private void Install()
    {
        if (target == null || receipt == null) throw new InvalidOperationException("The diagnostic route was not validated.");
        if (HasFactory()) throw new InvalidOperationException("Diagnostic transpiler factories prevent safe admission.");
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The diagnostic route already has an owner.");
        receipt.Install();
        installed = true;
        if (!RefreshPublication()) throw new InvalidOperationException(failure ?? "The incoming diagnostic stream was not validated.");
    }

    private void Rollback()
    {
        enabled = false;
        installed = false;
        Volatile.Write(ref publication, null);
        try { receipt?.Rollback(); }
        finally
        {
            if (target != null && Routes.TryGetValue(target, out var route) && ReferenceEquals(route, this))
                Routes.TryRemove(target, out _);
        }
    }

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var incoming = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return incoming;
        Volatile.Write(ref route.publication, null);
        route.publishedCaptureFailure = null;
        Volatile.Write(ref route.incomingValidation, new IncomingValidation(false));
        int expectedVersion;
        try { expectedVersion = HarmonyPublication.ExpectedVersion(__originalMethod); }
        catch (Exception exception)
        {
            route.failure = Bound(exception.GetBaseException().Message);
            return incoming;
        }
        long generationToken = Interlocked.Increment(ref nextGeneration);
        var meaningful = Enumerable.Range(0, incoming.Count).Where(index => incoming[index].opcode != OpCodes.Nop).ToArray();
        var matches = new List<int>();
        for (int offset = 2; offset + 4 < meaningful.Length; offset++)
        {
            int index = meaningful[offset];
            if (incoming[index].Calls(route.conversion) && incoming[meaningful[offset - 1]].opcode == OpCodes.Ldarg_0 &&
                incoming[meaningful[offset - 2]].opcode == OpCodes.Ldsfld && Equals(incoming[meaningful[offset - 2]].operand, route.category) &&
                incoming[meaningful[offset + 1]].opcode == OpCodes.Ldstr && incoming[meaningful[offset + 2]].opcode == OpCodes.Ldstr &&
                incoming[meaningful[offset + 3]].opcode.Name!.StartsWith("ldc.i4", StringComparison.Ordinal) &&
                incoming[meaningful[offset + 4]].Calls(route.output))
                matches.Add(offset);
        }
        // This exact static one-string entry point must consume that argument only in its one output.
        int suppressedExit = -1;
        bool captureExitAvailable = false;
        if (matches.Count != 1 || incoming.Count(instruction => instruction.opcode.Name!.StartsWith("ldarg", StringComparison.Ordinal)) != 1 ||
            !route.AcceptedBranch(incoming, meaningful, matches[0], out suppressedExit, out captureExitAvailable))
        {
            route.failure = "The incoming native-suppressed supplied-text/category output is missing or ambiguous.";
            return incoming;
        }
        var transformed = incoming.Select(instruction => new CodeInstruction(instruction)).ToList();
        transformed.InsertRange(meaningful[matches[0]], new[]
        {
            new CodeInstruction(OpCodes.Ldtoken, __originalMethod),
            new CodeInstruction(OpCodes.Ldc_I8, generationToken),
            new CodeInstruction(OpCodes.Call, typeof(NativeLogPatch).GetMethod(nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!)
        });
        if (route.capture != null && captureExitAvailable)
        {
            Label rejected = generator.DefineLabel();
            // Find the cloned original exit by its original index, accounting for projection insertion.
            transformed[suppressedExit + (suppressedExit >= meaningful[matches[0]] ? 3 : 0)].labels.Add(rejected);
            int entry = meaningful[matches[0] - 2];
            var first = new CodeInstruction(OpCodes.Ldarg_0);
            first.labels.AddRange(transformed[entry].labels);
            transformed[entry].labels.Clear();
            transformed.InsertRange(entry, new[]
            {
                first, new CodeInstruction(OpCodes.Ldtoken, __originalMethod),
                new CodeInstruction(OpCodes.Ldc_I8, generationToken),
                new CodeInstruction(OpCodes.Call, typeof(NativeLogPatch).GetMethod(nameof(ShouldCapture), BindingFlags.NonPublic | BindingFlags.Static)!),
                new CodeInstruction(OpCodes.Brfalse, rejected)
            });
        }
        Volatile.Write(ref route.incomingValidation, new IncomingValidation(true,
            route.capture != null && !captureExitAvailable ? "The native suppressed exit does not return directly; warning muting cannot safely skip subsequent effects." : null,
            expectedVersion, transformed.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray(), generationToken));
        return transformed;
    }

    private bool AcceptedBranch(List<CodeInstruction> incoming, int[] meaningful, int conversionOffset,
        out int suppressedExit, out bool captureExitAvailable)
    {
        suppressedExit = -1;
        captureExitAvailable = false;
        int fieldOffset = conversionOffset - 2;
        int start = meaningful[fieldOffset];
        int end = meaningful[conversionOffset + 4];
        int guardBranch = -1;
        int guardGetter = -1;
        int Destination(Label label)
        {
            int index = incoming.FindIndex(instruction => instruction.labels.Contains(label));
            while (index >= 0 && index < incoming.Count && incoming[index].opcode == OpCodes.Nop) index++;
            return index;
        }
        if (fieldOffset >= 3 && incoming[meaningful[fieldOffset - 3]].Calls(preventLogging))
        {
            int branch = meaningful[fieldOffset - 2];
            var exit = incoming[meaningful[fieldOffset - 1]];
            if ((incoming[branch].opcode == OpCodes.Brfalse || incoming[branch].opcode == OpCodes.Brfalse_S) &&
                incoming[branch].operand is Label accepted && Destination(accepted) == start &&
                (exit.opcode == OpCodes.Ret || ((exit.opcode == OpCodes.Leave || exit.opcode == OpCodes.Leave_S) &&
                    exit.operand is Label leave && Destination(leave) > end)))
            {
                guardBranch = branch;
                guardGetter = meaningful[fieldOffset - 3];
                suppressedExit = meaningful[fieldOffset - 1];
            }
        }
        if (guardBranch < 0 && fieldOffset >= 2 && incoming[meaningful[fieldOffset - 2]].Calls(preventLogging))
        {
            int branch = meaningful[fieldOffset - 1];
            if ((incoming[branch].opcode == OpCodes.Brtrue || incoming[branch].opcode == OpCodes.Brtrue_S) &&
                incoming[branch].operand is Label suppressed && Destination(suppressed) > end &&
                Destination(suppressed) < incoming.Count && incoming[Destination(suppressed)].opcode == OpCodes.Ret)
            {
                guardBranch = branch;
                guardGetter = meaningful[fieldOffset - 2];
                suppressedExit = Destination(suppressed);
            }
        }
        // The getter must dominate the branch as well as the output: a branch/handler entry after
        // it could supply its own false value and bypass native suppression despite local adjacency.
        if (guardBranch < 0 || incoming.Skip(guardGetter + 1).Take(end - guardGetter).Any(instruction => instruction.blocks.Count != 0))
            return false;
        for (int index = 0; index < incoming.Count; index++)
        {
            IEnumerable<Label> destinations = incoming[index].operand switch
            {
                Label label => new[] { label },
                Label[] labels => labels,
                _ => Array.Empty<Label>()
            };
            foreach (Label label in destinations)
            {
                int destination = Destination(label);
                if (destination > guardGetter && destination <= end && !(index == guardBranch && destination == start))
                    return false;
            }
        }
        // Capture's stronger exit prerequisite is independent of baseline context admission.
        captureExitAvailable = incoming[suppressedExit].opcode == OpCodes.Ret;
        if (!captureExitAvailable && incoming[suppressedExit].operand is Label exitLabel)
        {
            int destination = Destination(exitLabel);
            captureExitAvailable = destination >= 0 && destination < incoming.Count && incoming[destination].opcode == OpCodes.Ret;
        }
        return true;
    }

    private bool HasFactory() => target != null && ReadTranspilers().Any(IsFactory);
    private Patch[] ReadTranspilers()
    {
        Patch[]? result = null;
        if (target == null || !HarmonyPublication.Inspect(target, snapshot => { result = snapshot.Transpilers; return true; }))
            throw new InvalidOperationException("Published diagnostic metadata is busy or cannot be inspected during composition.");
        return result!;
    }

    private static bool IsFactory(Patch patch)
    {
        MethodInfo method = patch.PatchMethod;
        return method.IsStatic && (method.ReturnType == typeof(MethodInfo) || method.ReturnType == typeof(DynamicMethod)) &&
            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    }

    private string? OrderingFailure(Patch[] patches)
    {
        // Use this same immutable snapshot for factory rejection and sorting. Sorting can invoke factories.
        if (patches.Any(IsFactory)) return "Diagnostic transpiler factories prevent safe ordering inspection.";
        MethodInfo callback = typeof(NativeLogPatch).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)!;
        if (patches.Count(patch => patch.PatchMethod == callback) != 1 ||
            patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            !patches.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId && patch.PatchMethod == callback))
            return "The owned diagnostic projection is absent or registered more than once.";
        var sorted = PatchProcessor.GetSortedPatchMethods(target!, patches);
        return sorted.Count > 0 && sorted[sorted.Count - 1] == callback ? null
            : "Another transpiler runs after the owned diagnostic projection; its final semantics cannot be established.";
    }

    private static string Project(string original, RuntimeMethodHandle targetHandle, long generationToken)
    {
        try
        {
            var method = MethodBase.GetMethodFromHandle(targetHandle);
            if (method == null || !Routes.TryGetValue(method, out var route) || !route.enabled) return original;
            if (!route.Available || Volatile.Read(ref route.publication)?.Generation != generationToken)
                return original;
            try { return route.project(original); }
            catch (Exception exception) { route.Deactivate(exception.GetBaseException().Message); return original; }
        }
        catch (Exception) { return original; }
    }

    private static bool ShouldCapture(string original, RuntimeMethodHandle targetHandle, long generationToken)
    {
        try
        {
            var method = MethodBase.GetMethodFromHandle(targetHandle);
            if (method == null || !Routes.TryGetValue(method, out var route) || !route.CaptureAvailable ||
                Volatile.Read(ref route.publication)?.Generation != generationToken) return true;
            try { return route.capture!(original); }
            catch (Exception exception)
            {
                if (Interlocked.Exchange(ref route.captureFaulted, 1) == 0)
                {
                    route.captureFailure = Bound(exception.GetBaseException().Message);
                    try { route.failureRecorder?.Invoke($"[fumblesneeze.megamod] LOG-CAPTURE-WARNING capture disabled: {route.captureFailure}; ordinary native diagnostics remain."); }
                    catch (Exception) { /* Capture must fail open even if its protected explanation fails. */ }
                }
                return true;
            }
        }
        catch (Exception) { return true; }
    }

    private void Deactivate(string reason)
    {
        enabled = false;
        faulted = true;
        Volatile.Write(ref publication, null);
        failure = Bound(reason);
        if (Interlocked.Exchange(ref failureReported, 1) != 0) return;
        try { failureRecorder?.Invoke($"[fumblesneeze.megamod] {Candidate.Id} context disabled: {failure}; ordinary native diagnostics remain."); }
        catch (Exception) { /* The original diagnostic must survive a failed independent explanation. */ }
    }

    private static string Bound(string reason)
    {
        var bounded = reason.Length <= 256 ? reason.ToCharArray() : (reason.Substring(0, 253) + "...").ToCharArray();
        for (int index = 0; index < bounded.Length; index++) if (char.IsControl(bounded[index])) bounded[index] = ' ';
        return new string(bounded);
    }
}
