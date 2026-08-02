using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class UrgentProductionWindow
{
    private readonly int durationTicks;
    private int expiresAt = -1;

    public UrgentProductionWindow(int durationTicks)
    {
        this.durationTicks = Math.Max(1, durationTicks);
    }

    public void Register(int currentTick)
    {
        expiresAt = currentTick + durationTicks;
    }

    public bool IsActive(int currentTick) => expiresAt >= 0 && currentTick < expiresAt;

    public void Consume() => expiresAt = -1;
}

internal static class UrgentProductionRequestRegistry
{
    private sealed class Request
    {
        internal readonly UrgentProductionWindow Window = new(durationTicks: 5000);
        internal System.WeakReference<Pawn>? Consumer;
    }

    private static readonly ConditionalWeakTable<Map, Request> Requests = new();

    internal static void Register(Pawn consumer)
    {
        if (consumer.Map is not { } map)
        {
            return;
        }

        var request = Requests.GetOrCreateValue(map);
        request.Consumer = new System.WeakReference<Pawn>(consumer);
        request.Window.Register(Find.TickManager?.TicksGame ?? 0);
    }

    internal static bool HasActive(Map map)
    {
        if (!Requests.TryGetValue(map, out var request) ||
            !request.Window.IsActive(Find.TickManager?.TicksGame ?? 0) ||
            request.Consumer is null || !request.Consumer.TryGetTarget(out var consumer) ||
            consumer.Destroyed || consumer.Map != map)
        {
            return false;
        }

        return consumer.needs?.food?.CurLevelPercentage <=
               ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
    }

    internal static void Consume(Map? map)
    {
        if (map is not null && Requests.TryGetValue(map, out var request))
        {
            request.Window.Consume();
        }
    }
}

[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
internal static class StarvingPawnProductionRequestPatch
{
    private static void Postfix(Pawn pawn, Job? __result)
    {
        if (__result is null && pawn.RaceProps.Humanlike &&
            pawn.needs?.food?.CurLevelPercentage <=
            ImmersiveChefsMod.Settings.EmergencyHungerThreshold)
        {
            UrgentProductionRequestRegistry.Register(pawn);
        }
    }
}
