using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed class MessageConsumptionPatch
{
    private static readonly Dictionary<MethodBase, MessageConsumptionPatch> Routes = new();
    private static readonly MethodInfo PrefixMethod = typeof(MessageConsumptionPatch).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MessageDrawShape shape;
    private readonly MethodInfo collisionQuery;
    private readonly MethodInfo[] targets;
    private readonly OwnedHarmonyPatch[] receipts;
    private readonly Action invalidatePendingPass;
    private readonly Action<Exception> reportFailure;
    private bool installed, enabled, faulted;

    public MessageConsumptionPatch(MessageDrawShape shape, MethodInfo collisionQuery, Action invalidatePendingPass, Action<Exception> reportFailure)
    {
        this.shape = shape; this.collisionQuery = collisionQuery;
        this.invalidatePendingPass = invalidatePendingPass; this.reportFailure = reportFailure;
        targets = new[] { shape.Callback, collisionQuery };
        receipts = targets.Select(target => new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, PrefixMethod,
            () => ReadOwnedSlot(target))).ToArray();
        Candidate = new("MSG-NATIVE-CONSUMPTION", "MSG-NATIVE-FRAME", collisionQuery.ToString()!, "message placement",
            "native placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => installed && !faulted && OrderingFailure() == null;
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Native message consumer observation is unavailable.");
        enabled = true;
    }

    private string? Validate()
    {
        var parameters = collisionQuery.GetParameters();
        if (shape.Callback.IsStatic || shape.Callback.ReturnType != typeof(void) || shape.Callback.GetParameters().Length != 0 ||
            shape.Callback.DeclaringType != shape.RectangleField.DeclaringType || shape.Callback.DeclaringType != shape.MessageField.DeclaringType ||
            shape.RectangleField.IsStatic || shape.MessageField.IsStatic || !shape.RectangleField.FieldType.IsValueType ||
            !collisionQuery.IsPublic || !collisionQuery.IsStatic || collisionQuery.ReturnType != typeof(bool) || parameters.Length != 2 ||
            parameters[0].ParameterType != shape.RectangleField.FieldType || parameters[1].ParameterType != typeof(float).MakeByRefType() ||
            !parameters[1].IsOut || targets.Any(target => target.IsGenericMethod || target.ContainsGenericParameters || target.IsAbstract))
            return "The resolved native message callback or collision-query callable shape changed.";
        return targets.Select(target => InspectTarget(target, acquired: false)).FirstOrDefault(failure => failure != null);
    }

    private void Install()
    {
        if (targets.Any(target => Routes.ContainsKey(target))) throw new InvalidOperationException("The native message consumer route already has an owner.");
        foreach (var target in targets) Routes.Add(target, this);
        foreach (var receipt in receipts) receipt.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("Native consumer entry cannot be observed before foreign prefixes.");
    }

    private void Rollback()
    {
        enabled = false; installed = false;
        var failures = new List<Exception>();
        foreach (var receipt in receipts.Reverse())
            try { receipt.Rollback(); } catch (Exception error) { failures.Add(error); }
        foreach (var target in targets)
            if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(target);
        if (failures.Count != 0) throw new AggregateException("Native message consumer cleanup failed.", failures);
    }

    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(MethodBase __originalMethod)
    {
        if (!Routes.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        try
        {
            string? failure = owner.OrderingFailure();
            if (failure != null) { owner.Fail(new InvalidOperationException(failure)); return; }
            owner.invalidatePendingPass();
        }
        catch (Exception failure) { owner.Fail(failure); }
    }

    private string? OrderingFailure()
    {
        foreach (var target in targets)
        {
            string? failure = InspectTarget(target, acquired: true);
            if (failure != null) return failure;
        }
        return null;
    }

    private static string? InspectTarget(MethodInfo target, bool acquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            // Prefix factory, ownership and order decisions share one target snapshot.
            if (snapshot.Prefixes.Any(patch => patch.IsFactory))
            { failure = "A prefix factory prevents safe native consumer ordering inspection."; return true; }
            if (!acquired)
            {
                if (snapshot.Prefixes.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId))
                    failure = "The owned target/kind slot already contains a patch; safe rollback cannot be acquired.";
                return true;
            }
            if (snapshot.Prefixes.Count(patch => patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == PrefixMethod) != 1)
            { failure = "The acquired native consumer prefix is absent or duplicated."; return true; }
            var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(patch => patch.ToOrderingPatch()).ToArray());
            if (sorted.Count == 0 || sorted[0] != PrefixMethod)
                failure = "A foreign prefix precedes native message consumer observation.";
            return true;
        });
        return inspected ? failure : "Published native consumer metadata is busy or cannot be inspected during composition.";
    }

    private static IReadOnlyList<Patch> ReadOwnedSlot(MethodInfo target)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            // A later unrelated foreign factory must not be resolved for owned rollback.
            owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId)
                .Select(patch => patch.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Published native consumer ownership metadata is busy or cannot be inspected during composition.");
        return owned!;
    }

    private void Fail(Exception failure)
    {
        enabled = false;
        if (faulted) return;
        faulted = true;
        try { reportFailure(failure); } catch (Exception) { /* Observation must not replace native behavior. */ }
    }
}
