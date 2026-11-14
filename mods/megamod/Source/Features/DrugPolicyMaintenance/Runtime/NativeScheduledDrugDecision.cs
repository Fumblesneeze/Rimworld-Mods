using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Harmony;
using Megamod.Features.DrugTolerance.Runtime;
using Megamod.Features.PracticalNeeds.Runtime;
using Megamod.Patching;
using Megamod.Settings;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyMaintenance.Runtime;

// One process-lifetime native owner. Neither consumer owns or rolls back the other's boundary.
public sealed class NativeScheduledDrugDecision
{
    private readonly ScheduledDrugDecisionPatch? bridge;
    private readonly string? resolutionFailure;

    public NativeScheduledDrugDecision(SettingsRuntime settings,
        Func<NativeDrugToleranceRuntime?> tolerance, Action<string> report)
    {
        PotentialMood = new(settings, () => Available, report);
        try
        {
            if (!ScheduledContext.ShapeAvailable) throw new InvalidOperationException("Native scheduled policy storage changed.");
            var target = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(ThingDef) }, null);
            if (target == null || target.IsGenericMethod || target.ReturnType != typeof(bool))
                throw new InvalidOperationException("Native scheduled permission callable changed.");
            bridge = new(target,
                (actor, drug) => PotentialMood.EvaluateScheduled((Pawn_DrugPolicyTracker)actor, (ThingDef)drug),
                (actor, drug) => tolerance()?.EvaluateScheduled((Pawn_DrugPolicyTracker)actor, (ThingDef)drug)
                    ?? ScheduledDrugReadResult.Unavailable,
                () => ScheduledContext.Ready(settings), report,
                (actor, drug, evaluate) =>
                {
                    if (actor is not Pawn_DrugPolicyTracker tracker || drug is not ThingDef definition ||
                        !ScheduledContext.TryCapture(settings, tracker, definition, out var context)) return true;
                    bool permitted = evaluate();
                    // A native getter may run foreign code. Never publish an old-context veto.
                    return context!.IsCurrent(settings) ? permitted : true;
                });
            S138Dependency = bridge.S138Dependency;
            Group = new("drug-policy.scheduled-decision", AdmissionPhase.Early, new[] { bridge.Candidate });
        }
        catch (Exception error)
        {
            resolutionFailure = error.GetBaseException().Message;
            var unavailable = new PatchCandidate("DRUG-SCHEDULED-RESOLVE", "DRUG-SCHEDULED-COMPOSITION",
                "native scheduled permission", "scheduled mood restriction and atomic tolerance editing/enforcement",
                "native scheduled permission; retained choices", () => resolutionFailure, () => { }, () => { });
            S138Dependency = unavailable;
            Group = new("drug-policy.scheduled-decision", AdmissionPhase.Early, new[] { unavailable });
        }
    }

    public PatchGroup Group { get; }
    public PatchCandidate S138Dependency { get; }
    public bool Available => resolutionFailure == null && bridge?.Available == true;
    public string? Failure => resolutionFailure ?? bridge?.Failure;
    public NativePotentialMood PotentialMood { get; }
}

// Synchronous identity snapshot only. Reading curPolicy avoids the native lazy default-policy getter.
internal sealed class ScheduledContext
{
    private static readonly FieldInfo? CurrentPolicy = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy", typeof(DrugPolicy));
    private static readonly FieldInfo? Policies = Field(typeof(DrugPolicyDatabase), "policies", typeof(List<DrugPolicy>));
    private readonly Game game;
    private readonly SaveSettingsBinding binding;
    private readonly DrugPolicyDatabase database;
    private readonly Pawn_DrugPolicyTracker tracker;
    private readonly DrugPolicy policy;
    internal readonly Pawn Pawn;
    internal readonly ThingDef Drug;
    internal readonly DrugPolicyEntry Entry;

    private ScheduledContext(Game game, SaveSettingsBinding binding, Pawn_DrugPolicyTracker tracker,
        Pawn pawn, DrugPolicy policy, ThingDef drug, DrugPolicyEntry entry)
    {
        this.game = game; this.binding = binding; this.tracker = tracker; this.policy = policy;
        database = game.drugPolicyDatabase; Pawn = pawn; Drug = drug; Entry = entry;
    }

    internal static bool ShapeAvailable => CurrentPolicy != null && Policies != null;
    internal static bool Ready(SettingsRuntime settings) => UnityData.IsInMainThread && Current.Game != null &&
        Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting && settings.CurrentBinding != null;

    internal static bool TryCapture(SettingsRuntime settings, Pawn_DrugPolicyTracker tracker, ThingDef drug, out ScheduledContext? context)
    {
        context = null;
        if (!ShapeAvailable || !Ready(settings)) return false;
        var game = Current.Game; var binding = settings.CurrentBinding; var pawn = tracker?.pawn;
        if (game == null || binding == null || pawn == null || !ReferenceEquals(pawn.drugs, tracker) ||
            !CurrentDrug(drug) || CurrentPolicy!.GetValue(tracker) is not DrugPolicy policy ||
            Policies!.GetValue(game.drugPolicyDatabase) is not List<DrugPolicy> policies ||
            !policies.Any(item => ReferenceEquals(item, policy))) return false;
        var entry = NativeDrugToleranceRows.FindUnique(policy, drug);
        if (entry == null || !entry.allowScheduled) return false;
        context = new(game, binding, tracker!, pawn, policy, drug, entry);
        return context.IsCurrent(settings);
    }

    internal bool IsCurrent(SettingsRuntime settings) => Ready(settings) && ReferenceEquals(Current.Game, game) &&
        ReferenceEquals(settings.CurrentBinding, binding) && !binding.IsRevoked && ReferenceEquals(game.drugPolicyDatabase, database) &&
        ReferenceEquals(tracker.pawn, Pawn) && ReferenceEquals(Pawn.drugs, tracker) && CurrentDrug(Drug) &&
        ReferenceEquals(CurrentPolicy!.GetValue(tracker), policy) && Policies!.GetValue(database) is List<DrugPolicy> policies &&
        policies.Any(item => ReferenceEquals(item, policy)) && Entry.allowScheduled && NativeDrugToleranceRows.IsCurrent(policy, Entry, Drug);

    private static bool CurrentDrug(ThingDef drug) => drug != null && drug.category == ThingCategory.Item && drug.IsDrug &&
        DefDatabase<ThingDef>.AllDefsListForReading.Any(item => ReferenceEquals(item, drug));
    private static FieldInfo? Field(Type type, string name, Type expected)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        return field?.FieldType == expected && !field.IsStatic ? field : null;
    }
}
