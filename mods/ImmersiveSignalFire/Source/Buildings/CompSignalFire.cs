using System;
using System.Collections.Generic;
using System.Linq;
using Blues;
using ImmersiveSignalFire.Contacts;
using ImmersiveSignalFire.Effects;
using ImmersiveSignalFire.Interactions;
using ImmersiveSignalFire.Signals;
using ImmersiveSignalFire.UI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ImmersiveSignalFire.Buildings;

public sealed class CompProperties_SignalFire : CompProperties
{
    public CompProperties_SignalFire()
    {
        compClass = typeof(CompSignalFire);
    }
}

internal enum SignalSessionPhase
{
    Idle,
    Gathering,
    Active,
}

public sealed class CompSignalFire : ThingComp
{
    private List<Pawn> participants = new();
    private List<IntVec3> workCells = new();
    private Faction? targetFaction;
    private float capturedQuality;
    private SignalSessionPhase phase;
    private int activeStartTick = -1;
    private int activeEndTick = -1;
    private bool cleanupDone;
    private SignalEffectController? effects;

    private Building_SignalFire Fire => (Building_SignalFire)parent;

    public bool IsBusy => phase != SignalSessionPhase.Idle;
    internal bool IsActive => phase == SignalSessionPhase.Active;
    internal int ActiveElapsedTicks => IsActive ? Math.Max(0, Find.TickManager.TicksGame - activeStartTick) : -1;
    internal IReadOnlyList<Pawn> ActiveParticipants => IsActive ? participants : Array.Empty<Pawn>();
    internal int LastCompletedDurationTicks { get; private set; }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!IsBusy && selPawn is not null)
        {
            yield return SignalFireMenu.MakeTopLevel(Fire, selPawn);
        }
    }

    public bool TryBegin(Faction faction, IReadOnlyList<Pawn> selectedParticipants, float quality, out string rejection)
    {
        rejection = string.Empty;
        if (IsBusy || !Fire.Spawned || Fire.Map is null)
        {
            rejection = "ImmersiveSignalFire_ContactUnavailable".Translate();
            return false;
        }

        if (!WorldContactCatalog.IsEligible(Fire.Map, faction))
        {
            rejection = "ImmersiveSignalFire_ContactUnavailable".Translate();
            return false;
        }

        List<Pawn> chosen = selectedParticipants.Distinct().ToList();
        if (chosen.Count is < 1 or > 3 || chosen.Any(pawn => !SignalParticipantUtility.IsEligible(pawn, Fire)))
        {
            rejection = "ImmersiveSignalFire_InvalidParticipants".Translate();
            return false;
        }

        if (!TryAssignWorkCells(chosen, out List<IntVec3> assignedCells))
        {
            rejection = "ImmersiveSignalFire_NoWorkCells".Translate();
            return false;
        }

        participants = chosen;
        workCells = assignedCells;
        targetFaction = faction;
        capturedQuality = Mathf.Clamp01(quality);
        cleanupDone = false;
        LastCompletedDurationTicks = 0;
        phase = SignalSessionPhase.Gathering;

        for (int index = 0; index < participants.Count; index++)
        {
            Job job = JobMaker.MakeJob(SignalFireDefOf.ImmersiveSignalFire_Approach, Fire, workCells[index]);
            if (!participants[index].jobs.TryTakeOrderedJob(job))
            {
                CancelBeforeSmoke();
                rejection = "ImmersiveSignalFire_JobRejected".Translate(participants[index].LabelShortCap);
                return false;
            }
        }

        return true;
    }

    public override void CompTick()
    {
        base.CompTick();
        switch (phase)
        {
            case SignalSessionPhase.Gathering:
                TickGathering();
                break;
            case SignalSessionPhase.Active:
                TickActive();
                break;
        }
    }

    private void TickGathering()
    {
        if (!ParticipantsRemainValid())
        {
            CancelBeforeSmoke();
            Messages.Message("ImmersiveSignalFire_Interrupted".Translate(), Fire, MessageTypeDefOf.NegativeEvent);
            return;
        }

        for (int index = 0; index < participants.Count; index++)
        {
            Pawn pawn = participants[index];
            if (pawn.CurJobDef != SignalFireDefOf.ImmersiveSignalFire_Approach || pawn.Position != workCells[index])
            {
                return;
            }
        }

        BeginActivePhase();
    }

    private void BeginActivePhase()
    {
        phase = SignalSessionPhase.Active;
        activeStartTick = Find.TickManager.TicksGame;
        activeEndTick = activeStartTick + MorseCadence.TotalDurationTicks;
        effects = new SignalEffectController(Fire);
        effects.Tick(0);

        for (int index = 0; index < participants.Count; index++)
        {
            Job job = JobMaker.MakeJob(SignalFireDefOf.ImmersiveSignalFire_Signal, Fire, workCells[index]);
            if (!participants[index].jobs.TryTakeOrderedJob(job))
            {
                CompleteActive(SignalOutcome.Unnoticed, interrupted: true);
                return;
            }
        }
    }

    private void TickActive()
    {
        if (!ParticipantsRemainValid() || participants.Any(pawn => pawn.CurJobDef != SignalFireDefOf.ImmersiveSignalFire_Signal))
        {
            CompleteActive(SignalOutcome.Unnoticed, interrupted: true);
            return;
        }

        if (Find.TickManager.TicksGame >= activeEndTick)
        {
            CompleteActive(SignalQuality.Outcome(capturedQuality), interrupted: false);
            return;
        }

        effects ??= new SignalEffectController(Fire);
        effects.Tick(ActiveElapsedTicks);
    }

    internal bool OwnsActiveJob(Pawn pawn) => IsActive && participants.Contains(pawn);

    internal void NotifyApproachJobEnded(Pawn pawn)
    {
        if (phase == SignalSessionPhase.Gathering && participants.Contains(pawn))
        {
            CancelBeforeSmoke(pawn);
        }
    }

    internal void NotifySignalJobEnded(Pawn pawn)
    {
        if (phase == SignalSessionPhase.Active && participants.Contains(pawn))
        {
            CompleteActive(SignalOutcome.Unnoticed, interrupted: true, reportingPawn: pawn);
        }
    }

    private bool ParticipantsRemainValid() =>
        Fire.Spawned && Fire.Map is { } map &&
        participants.Count is >= 1 and <= 3 &&
        participants.All(pawn => pawn.Spawned && pawn.Map == map && !pawn.Downed && !pawn.Dead);

    private bool TryAssignWorkCells(IReadOnlyList<Pawn> pawns, out List<IntVec3> assigned)
    {
        assigned = new List<IntVec3>();
        if (Fire.Map is not { } map)
        {
            return false;
        }

        List<IntVec3> available = GenAdj.CellsAdjacent8Way(Fire)
            .Where(cell => cell.InBounds(map) && cell.Walkable(map))
            .Distinct()
            .ToList();
        bool found = DistinctAssignmentPolicy.TryAssign(
            pawns,
            pawn => (IReadOnlyList<IntVec3>)available
                .Where(candidate => pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Some) &&
                                    pawn.CanReserve(candidate))
                .OrderBy(candidate => pawn.Position.DistanceToSquared(candidate))
                .ToList(),
            out IReadOnlyList<IntVec3> result);
        assigned = found ? result.ToList() : new List<IntVec3>();
        return found;
    }

    private void CompleteActive(
        SignalOutcome outcome,
        bool interrupted,
        Pawn? reportingPawn = null,
        bool destroySpentFire = true,
        Map? deSpawnMap = null)
    {
        if (phase != SignalSessionPhase.Active)
        {
            return;
        }

        Map? map = deSpawnMap ?? Fire.Map;
        Faction? faction = targetFaction;
        LastCompletedDurationTicks = interrupted
            ? Math.Max(0, Find.TickManager.TicksGame - activeStartTick)
            : activeEndTick - activeStartTick;
        phase = SignalSessionPhase.Idle;

        try
        {
            ResetSession(interruptJobs: true, reportingPawn: reportingPawn);
            if (interrupted)
            {
                Messages.Message("ImmersiveSignalFire_Interrupted".Translate(), Fire, MessageTypeDefOf.NegativeEvent);
                return;
            }

            switch (outcome)
            {
                case SignalOutcome.Immediate:
                    if (map is not null && NativeMilitaryAid.TryCall(map, faction))
                    {
                        Messages.Message("ImmersiveSignalFire_Immediate".Translate(), Fire, MessageTypeDefOf.PositiveEvent);
                    }
                    break;
                case SignalOutcome.Delayed:
                    if (map is null)
                    {
                        break;
                    }

                    int delay = Rand.RangeInclusive(600, 3600);
                    map.GetComponent<MapComponent_SignalResponses>().Schedule(faction, Find.TickManager.TicksGame + delay);
                    Messages.Message("ImmersiveSignalFire_Delayed".Translate(), Fire, MessageTypeDefOf.NeutralEvent);
                    break;
                case SignalOutcome.Misunderstood:
                    Messages.Message("ImmersiveSignalFire_Misunderstood".Translate(), Fire, MessageTypeDefOf.NegativeEvent);
                    break;
                default:
                    Messages.Message("ImmersiveSignalFire_Unnoticed".Translate(), Fire, MessageTypeDefOf.NegativeEvent);
                    break;
            }
        }
        finally
        {
            try
            {
                StopEffects();
            }
            finally
            {
                try
                {
                    if (destroySpentFire)
                    {
                        DestroySpentFire();
                    }
                }
                finally
                {
                    if (map is not null)
                    {
                        MakeSoot(map);
                    }
                }
            }
        }
    }

    private void DestroySpentFire()
    {
        if (!Fire.Destroyed)
        {
            Fire.Destroy(DestroyMode.Vanish);
        }
    }

    private void StopEffects()
    {
        effects?.Stop();
        effects = null;
    }

    private void MakeSoot(Map map)
    {
        if (cleanupDone)
        {
            return;
        }

        List<IntVec3> cells = Fire.OccupiedRect().ExpandedBy(1)
            .Cells
            .Where(cell => cell.InBounds(map) && cell.Walkable(map))
            .OrderBy(cell => cell.DistanceToSquared(Fire.Position))
            .ToList();
        if (cells.Count == 0)
        {
            throw new InvalidOperationException("The spent signal fire has no walkable soot-cleanup cell.");
        }

        int beforeThickness = AshThickness(cells, map);
        for (int unit = 0; unit < 16; unit++)
        {
            AddOneAshThickness(cells[unit % cells.Count], map);
        }

        int afterThickness = AshThickness(cells, map);
        if (afterThickness - beforeThickness != 16)
        {
            throw new InvalidOperationException(
                $"Signal-fire cleanup added {afterThickness - beforeThickness} ash thickness instead of 16.");
        }

        cleanupDone = true;
    }

    private static void AddOneAshThickness(IntVec3 cell, Map map)
    {
        Filth? existing = cell.GetThingList(map)
            .OfType<Filth>()
            .FirstOrDefault(filth => filth.def == ThingDefOf.Filth_Ash && filth.CanBeThickened);
        if (existing is not null)
        {
            int before = existing.thickness;
            existing.ThickenFilth();
            if (existing.thickness == before + 1)
            {
                return;
            }
        }

        int beforeThickness = AshThickness(new[] { cell }, map);
        FilthMaker.TryMakeFilth(
            cell,
            map,
            ThingDefOf.Filth_Ash,
            count: 1,
            additionalFlags: FilthSourceFlags.None,
            shouldPropagate: false);
        if (AshThickness(new[] { cell }, map) == beforeThickness + 1)
        {
            return;
        }

        var fallback = (Filth)ThingMaker.MakeThing(ThingDefOf.Filth_Ash);
        GenSpawn.Spawn(fallback, cell, map);
    }

    private static int AshThickness(IEnumerable<IntVec3> cells, Map map)
    {
        HashSet<IntVec3> included = cells.ToHashSet();
        return map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash)
            .OfType<Filth>()
            .Where(ash => included.Contains(ash.Position))
            .Sum(ash => ash.thickness);
    }

    private void CancelBeforeSmoke(Pawn? reportingPawn = null)
    {
        ResetSession(interruptJobs: true, reportingPawn: reportingPawn);
    }

    private void ResetSession(bool interruptJobs, Pawn? reportingPawn = null)
    {
        List<Pawn> previousParticipants = participants.ToList();
        phase = SignalSessionPhase.Idle;
        participants.Clear();
        workCells.Clear();
        targetFaction = null;
        activeStartTick = -1;
        activeEndTick = -1;
        capturedQuality = 0f;
        effects?.Stop();
        effects = null;

        if (!interruptJobs)
        {
            return;
        }

        foreach (Pawn pawn in previousParticipants.Where(pawn =>
                     pawn.Spawned && !ReferenceEquals(pawn, reportingPawn)))
        {
            if (pawn.CurJobDef == SignalFireDefOf.ImmersiveSignalFire_Approach ||
                pawn.CurJobDef == SignalFireDefOf.ImmersiveSignalFire_Signal)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (IsActive)
        {
            CompleteActive(
                SignalOutcome.Unnoticed,
                interrupted: true,
                destroySpentFire: false,
                deSpawnMap: map);
        }
        else
        {
            ResetSession(interruptJobs: true);
        }

        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref participants, "signalParticipants", LookMode.Reference);
        Scribe_Collections.Look(ref workCells, "signalWorkCells", LookMode.Value);
        Scribe_References.Look(ref targetFaction, "signalFaction");
        Scribe_Values.Look(ref capturedQuality, "signalQuality");
        Scribe_Values.Look(ref phase, "signalPhase");
        Scribe_Values.Look(ref activeStartTick, "signalStartTick", -1);
        Scribe_Values.Look(ref activeEndTick, "signalEndTick", -1);
        Scribe_Values.Look(ref cleanupDone, "signalCleanupDone");
        participants ??= new List<Pawn>();
        workCells ??= new List<IntVec3>();
    }

    public override void PostDraw()
    {
        base.PostDraw();
        OptionalToolsAdapter.DrawBlankets(this);
    }
}

internal static class NativeMilitaryAid
{
    public static bool TryCall(Map map, Faction? faction)
    {
        if (faction is null || faction.defeated || faction.PlayerRelationKind != FactionRelationKind.Ally)
        {
            Messages.Message("ImmersiveSignalFire_NoResponse".Translate(), MessageTypeDefOf.NegativeEvent);
            return false;
        }

        int remainingCooldown = MilitaryAidAvailabilityPolicy.RemainingCooldownTicks(
            faction.lastMilitaryAidRequestTick,
            Find.TickManager.TicksGame);
        if (remainingCooldown > 0)
        {
            Messages.Message(
                "ImmersiveSignalFire_AidCooldown".Translate(remainingCooldown.ToStringTicksToPeriod()),
                MessageTypeDefOf.NegativeEvent);
            return false;
        }

        try
        {
            Faction.OfPlayer.TryAffectGoodwillWith(
                faction,
                -25,
                canSendMessage: false,
                canSendHostilityLetter: true,
                HistoryEventDefOf.RequestedMilitaryAid);
            var incident = new IncidentParms
            {
                target = map,
                faction = faction,
                raidArrivalModeForQuickMilitaryAid = true,
                points = DiplomacyTuning.RequestedMilitaryAidPointsRange.RandomInRange,
            };
            faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
            bool executed = IncidentDefOf.RaidFriendly.Worker.TryExecute(incident);
            if (!executed)
            {
                Messages.Message("ImmersiveSignalFire_AidFailed".Translate(), MessageTypeDefOf.NegativeEvent);
            }

            return executed;
        }
        catch (Exception exception)
        {
            Log.Error($"[Immersive Signal Fire] Native military aid failed: {exception.GetBaseException().Message}");
            Messages.Message("ImmersiveSignalFire_AidFailed".Translate(), MessageTypeDefOf.NegativeEvent);
            return false;
        }
    }
}
