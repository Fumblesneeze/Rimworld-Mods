using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal sealed class KitchenAssistanceRequest
{
    internal KitchenAssistanceRequest(Pawn lead, Job leadJob, Thing billGiver)
    {
        Lead = lead;
        LeadJob = leadJob;
        BillGiver = billGiver;
    }

    internal Pawn Lead { get; }
    internal Job LeadJob { get; }
    internal Thing BillGiver { get; }
    internal Dictionary<Pawn, Thing> Claims { get; } = new();
    internal Dictionary<Pawn, AssistantWorkGate> WorkGates { get; } = new();

    internal bool IsActive => !Lead.Destroyed && Lead.CurJob == LeadJob &&
                              !BillGiver.Destroyed && BillGiver.Spawned;
}

public sealed class AssistantWorkGate
{
    public bool IsWorking { get; private set; }

    public void BeginWorking() => IsWorking = true;

    public void StopWorking() => IsWorking = false;
}

internal static class KitchenAssistanceRegistry
{
    private static readonly ConditionalWeakTable<Job, KitchenAssistanceRequest> Requests = new();
    private static readonly Dictionary<Pawn, KitchenAssistanceRequest> PawnClaims = new();
    private static readonly Dictionary<Thing, KitchenAssistanceRequest> StationClaims = new();

    internal static void Open(Pawn lead, Job job, Thing billGiver)
    {
        if (!ImmersiveChefsMod.Settings.AutoCallAssistants ||
            ImmersiveChefsMod.Settings.MaximumAssistants <= 0)
        {
            return;
        }

        Requests.Add(job, new KitchenAssistanceRequest(lead, job, billGiver));
    }

    internal static bool CanClaim(Pawn assistant, Thing station, out KitchenAssistanceRequest? request)
    {
        request = null;
        if (!AvailableAssistant(assistant) || StationClaims.ContainsKey(station) || !Operational(station))
        {
            return false;
        }

        foreach (var mapPawn in assistant.Map.mapPawns.FreeColonistsSpawned)
        {
            var job = mapPawn.CurJob;
            if (job is null || !Requests.TryGetValue(job, out var candidate) || !candidate.IsActive ||
                candidate.Claims.Count >= ImmersiveChefsMod.Settings.MaximumAssistants ||
                candidate.Lead == assistant || !IsLinked(candidate.BillGiver, station))
            {
                continue;
            }

            request = candidate;
            return true;
        }

        return false;
    }

    internal static bool Claim(Pawn assistant, Thing station, Pawn lead)
    {
        if (lead.CurJob is not { } leadJob || !Requests.TryGetValue(leadJob, out var request) ||
            !CanClaim(assistant, station, out var found) || found != request)
        {
            return false;
        }

        request.Claims.Add(assistant, station);
        request.WorkGates.Add(assistant, new AssistantWorkGate());
        PawnClaims.Add(assistant, request);
        StationClaims.Add(station, request);
        return true;
    }

    internal static IReadOnlyList<int> ActiveSkills(Job leadJob)
    {
        if (!Requests.TryGetValue(leadJob, out var request) || !request.IsActive)
        {
            return Array.Empty<int>();
        }

        return request.Claims
            .Where(pair => pair.Key.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_AssistCooking &&
                           request.WorkGates.TryGetValue(pair.Key, out var gate) && gate.IsWorking &&
                           pair.Key.Position.AdjacentTo8WayOrInside(pair.Value.InteractionCell) &&
                           Operational(pair.Value))
            .Select(pair => pair.Key.skills?.GetSkill(SkillDefOf.Cooking).Level ?? 0)
            .ToList();
    }

    internal static bool ClaimStillActive(Pawn assistant) =>
        PawnClaims.TryGetValue(assistant, out var request) && request.IsActive &&
        request.Claims.TryGetValue(assistant, out var station) && Operational(station);

    internal static void BeginWorking(Pawn assistant)
    {
        if (PawnClaims.TryGetValue(assistant, out var request) &&
            request.WorkGates.TryGetValue(assistant, out var gate))
        {
            gate.BeginWorking();
        }
    }

    internal static void StopWorking(Pawn assistant)
    {
        if (PawnClaims.TryGetValue(assistant, out var request) &&
            request.WorkGates.TryGetValue(assistant, out var gate))
        {
            gate.StopWorking();
        }
    }

    internal static void Cleanup(Pawn pawn, Job? job)
    {
        ReleaseAssistant(pawn);
        if (job is null || !Requests.TryGetValue(job, out var request))
        {
            return;
        }

        foreach (var assistant in request.Claims.Keys.ToList())
        {
            ReleaseAssistant(assistant);
            if (assistant.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_AssistCooking)
            {
                assistant.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: true);
            }
        }

        Requests.Remove(job);
    }

    private static void ReleaseAssistant(Pawn assistant)
    {
        if (!PawnClaims.TryGetValue(assistant, out var request))
        {
            return;
        }

        if (request.Claims.TryGetValue(assistant, out var station))
        {
            request.Claims.Remove(assistant);
            request.WorkGates.Remove(assistant);
            StationClaims.Remove(station);
        }

        PawnClaims.Remove(assistant);
    }

    private static bool AvailableAssistant(Pawn pawn)
    {
        return !PawnClaims.ContainsKey(pawn) && !pawn.Drafted && !pawn.Downed &&
               pawn.MentalState is null &&
               !pawn.WorkTypeIsDisabled(DefDatabase<WorkTypeDef>.GetNamed("Cooking")) &&
               pawn.CurJob?.playerForced != true;
    }

    private static bool Operational(Thing station)
    {
        if (!station.Spawned || station.IsForbidden(Faction.OfPlayer))
        {
            return false;
        }

        var power = station.TryGetComp<CompPowerTrader>();
        var refuelable = station.TryGetComp<CompRefuelable>();
        var breakdown = station.TryGetComp<CompBreakdownable>();
        return (power is null || power.PowerOn) &&
               (refuelable is null || refuelable.HasFuel) &&
               (breakdown is null || !breakdown.BrokenDown);
    }

    private static bool IsLinked(Thing billGiver, Thing station)
    {
        return billGiver.TryGetComp<CompAffectedByFacilities>()?
            .LinkedFacilitiesListForReading.Contains(station) == true;
    }
}
