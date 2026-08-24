using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GuestBedGizmo.Compatibility.Hospitality;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GuestBedGizmo.Beds;

internal static class GuestBedOwnerAssignmentPatch
{
    internal const string ClosureTypeName = "<>c__DisplayClass55_0";
    internal const string CommitMethodName = "<SetBedOwnerTypeByInterface>b__0";

    private static readonly Type ClosureType = typeof(Building_Bed).GetNestedType(
        ClosureTypeName,
        BindingFlags.NonPublic) ?? throw new MissingMemberException(
        typeof(Building_Bed).FullName,
        ClosureTypeName);

    private static readonly FieldInfo BedsToAffectField = ClosureType.GetField(
        "bedsToAffect",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
        throw new MissingFieldException(ClosureType.FullName, "bedsToAffect");

    internal static readonly MethodInfo OwnerInterfaceMethod = AccessTools.Method(
        typeof(Building_Bed),
        nameof(Building_Bed.SetBedOwnerTypeByInterface));

    internal static readonly MethodInfo CommitMethod = AccessTools.Method(
        ClosureType,
        CommitMethodName);

    internal static readonly MethodInfo TranspilerMethod = AccessTools.Method(
        typeof(GuestBedOwnerAssignmentPatch),
        nameof(PrepareDeferredCommit));

    internal static readonly MethodInfo CommitPrefixMethod = AccessTools.Method(
        typeof(GuestBedOwnerAssignmentPatch),
        nameof(ConvertGuestBedsAtCommit));

    private static readonly MethodInfo PrepareAffectedBedsMethod = AccessTools.Method(
        typeof(GuestBedOwnerAssignmentPatch),
        nameof(PrepareAffectedBeds));

    private static HospitalityRuntimeAdapter? adapter;

    internal static void Configure(HospitalityRuntimeAdapter? supportedAdapter) =>
        adapter = supportedAdapter;

    private static IEnumerable<CodeInstruction> PrepareDeferredCommit(
        IEnumerable<CodeInstruction> source)
    {
        List<CodeInstruction> instructions = source.ToList();
        int commitDelegateIndex = instructions.FindIndex(instruction =>
            instruction.opcode == OpCodes.Ldftn &&
            Equals(instruction.operand, CommitMethod));
        if (commitDelegateIndex < 1 ||
            instructions[commitDelegateIndex - 1].opcode != OpCodes.Ldloc_0)
        {
            throw new InvalidOperationException(
                "RimWorld's deferred bed-owner commit seam no longer matches the inspected 1.6 shape.");
        }

        CodeInstruction originalClosureLoad = instructions[commitDelegateIndex - 1];
        var injectedClosureLoad = new CodeInstruction(OpCodes.Ldloc_0);
        injectedClosureLoad.labels.AddRange(originalClosureLoad.labels);
        injectedClosureLoad.blocks.AddRange(originalClosureLoad.blocks);
        originalClosureLoad.labels.Clear();
        originalClosureLoad.blocks.Clear();
        instructions.InsertRange(
            commitDelegateIndex - 1,
            new[]
            {
                injectedClosureLoad,
                new CodeInstruction(OpCodes.Call, PrepareAffectedBedsMethod),
            });
        return instructions;
    }

    private static void PrepareAffectedBeds(object closure)
    {
        HospitalityRuntimeAdapter? currentAdapter = adapter;
        if (currentAdapter == null)
        {
            return;
        }

        var bedsToAffect = (List<Building_Bed>)BedsToAffectField.GetValue(closure);
        Building_Bed[] selectedBeds = Find.Selector.SelectedObjects
            .OfType<Building_Bed>()
            .Distinct()
            .ToArray();
        var selectedSet = new HashSet<Building_Bed>(selectedBeds);

        bedsToAffect.RemoveAll(bed =>
            selectedSet.Contains(bed) && !BedOwnerCommandEligibility.IsEligible(bed));
        foreach (Building_Bed bed in selectedBeds.Where(BedOwnerCommandEligibility.IsEligible))
        {
            if (currentAdapter.IsGuestBed(bed) && !bedsToAffect.Contains(bed))
            {
                bedsToAffect.Add(bed);
            }
        }
    }

    private static bool ConvertGuestBedsAtCommit(object __instance)
    {
        HospitalityRuntimeAdapter? currentAdapter = adapter;
        if (currentAdapter == null)
        {
            return true;
        }

        var bedsToAffect = (List<Building_Bed>)BedsToAffectField.GetValue(__instance);
        bool[] included = Enumerable.Repeat(true, bedsToAffect.Count).ToArray();
        if (GuestBedConversionTransaction.TryConvert(
                bedsToAffect,
                included,
                currentAdapter,
                expectedGuestState: false,
                out string? failure))
        {
            return true;
        }

        Building_Bed? target = bedsToAffect.FirstOrDefault(bed => bed != null && !bed.Destroyed);
        Log.ErrorOnce(
            $"[Guest Bed Gizmo] Vanilla owner assignment was rejected before ownership changed: {failure}",
            1748243122);
        if (target != null)
        {
            Messages.Message(
                failure ?? "Guest Bed Gizmo could not resolve the corresponding vanilla bed.",
                target,
                MessageTypeDefOf.RejectInput,
                historical: false);
        }

        return false;
    }
}
