using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed class MessageBarLabelAdmission
{
    private readonly Part[] parts;
    public MessageBarLabelAdmission(MessageBarLabelGuard guard, params (MethodInfo Target, MessageBarLabelStage Stage)[] contracts)
    {
        parts = contracts.Select(contract => new Part(guard, contract.Target, contract.Stage)).ToArray();
        Group = new("messages.automatic-labels", AdmissionPhase.Early, parts.Select(part => part.Candidate).ToArray());
    }
    public PatchGroup Group { get; }
    public bool Available => parts.Length != 0 && parts.All(part => part.Available);
    public bool Refresh() => parts.Length != 0 && parts.All(part => part.Available || part.Refresh());
    public Func<bool> CaptureAuthority()
    {
        var receipts = parts.Select(part => part.CaptureAuthority()).ToArray();
        return () => Available && receipts.All(receipt => receipt());
    }

    private sealed class Part
    {
        private static readonly ConcurrentDictionary<MethodBase, Part> Routes = new();
        private static readonly MethodInfo Observer = typeof(Part).GetMethod(nameof(Observe), BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly MessageBarLabelGuard guard;
        private readonly MethodInfo target;
        private readonly MessageBarLabelStage stage;
        private readonly OwnedHarmonyPatch patch;
        private sealed record Incoming(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Required);
        private sealed record Publication(Incoming Generation, HarmonyPublication.Identity Identity);
        private Incoming? incoming;
        private Publication? published;
        private bool installed;
        private string? failure;
        internal Part(MessageBarLabelGuard guard, MethodInfo target, MessageBarLabelStage stage)
        {
            this.guard = guard; this.target = target; this.stage = stage;
            patch = new(target, HarmonyPatchType.Transpiler, Observer, ReadTranspilers);
            Candidate = new("MSG-BAR-LABEL-" + stage.ToString().ToUpperInvariant(), "MSG-BAR-LAYOUT-PREP", target.ToString()!,
                "automatic message clearance", "manual message placement", Validate, Install, Rollback);
        }
        internal PatchCandidate Candidate { get; }
        internal bool Available => installed && Volatile.Read(ref published) is { } value &&
            ReferenceEquals(value.Generation, Volatile.Read(ref incoming)) && value.Identity.IsCurrent();
        internal Func<bool> CaptureAuthority()
        {
            var generation = Volatile.Read(ref published);
            return () => generation != null && Available && ReferenceEquals(generation, Volatile.Read(ref published));
        }
        private string? Validate() => target.IsAbstract || target.ContainsGenericParameters ||
            target.ReturnType != typeof(void) && stage != MessageBarLabelStage.PortraitTexture ?
            "The native label callable changed." : ReadTranspilers().Any(IsFactory) ?
            "A label transpiler factory cannot be safely inspected." : patch.ValidateOwnership();
        private void Install()
        {
            if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The native label contract already has an owner.");
            patch.Install(); installed = true;
            if (!Refresh()) throw new InvalidOperationException(failure ?? "The effective native label geometry is unproved.");
        }
        private void Rollback()
        {
            installed = false; Volatile.Write(ref published, null);
            try { patch.Rollback(); }
            finally { if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.TryRemove(target, out _); }
        }
        internal bool Refresh()
        {
            Volatile.Write(ref published, null);
            var generation = Volatile.Read(ref incoming);
            if (!installed || generation == null) return false;
            try
            {
                return HarmonyPublication.Inspect(target, snapshot =>
                {
                    if (snapshot.Version != generation.Version || snapshot.Transpilers.Any(IsFactory)) return false;
                    var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Transpilers);
                    if (sorted.Count == 0 || sorted[^1] != Observer ||
                        snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Observer) != 1) return false;
                    bool Observation(HarmonyPublication.OuterRegistration registration) => !registration.IsFactory &&
                        !registration.UncheckedReferenceBinding && registration.Method.ReturnType == typeof(void) && registration.Method.GetParameters().Length == 0;
                    if (!snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).All(Observation))
                    { failure = "An outer patch can suppress or change native label geometry."; return false; }
                    failure = snapshot.FailureFor(generation.Body, generation.Required);
                    if (failure != null || !ReferenceEquals(generation, Volatile.Read(ref incoming))) return false;
                    Volatile.Write(ref published, new(generation, snapshot.Identity));
                    return true;
                });
            }
            catch (Exception error) { failure = error.GetBaseException().Message; return false; }
        }
        private Patch[] ReadTranspilers()
        {
            Patch[]? result = null;
            if (!HarmonyPublication.Inspect(target, snapshot => { result = snapshot.Transpilers; return true; }))
                throw new InvalidOperationException("Native label publication is busy.");
            return result!;
        }
        private static bool IsFactory(Patch patch) => patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod);
        [HarmonyPriority(int.MinValue)]
        private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var body = instructions.ToList();
            if (!Routes.TryGetValue(__originalMethod, out var owner)) return body;
            Volatile.Write(ref owner.published, null); Volatile.Write(ref owner.incoming, null);
            try
            {
                var proof = owner.guard.Describe(owner.stage, body);
                owner.failure = proof.Failure;
                if (proof.Required != null && proof.Failure == null)
                    Volatile.Write(ref owner.incoming, new(HarmonyPublication.ExpectedVersion(__originalMethod),
                        body.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray(), proof.Required));
            }
            catch (Exception error) { owner.failure = error.GetBaseException().Message; }
            return body; // Passive effective-stream observation, including every rejected stream.
        }
    }
}
