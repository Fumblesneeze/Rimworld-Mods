using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_Microwave : CompProperties
{
    public CompProperties_Microwave()
    {
        compClass = typeof(CompMicrowave);
    }

    public int heatingTicks = 180;
}

public sealed class CompMicrowave : ThingComp
{
    public int HeatingTicks => Math.Max(1, ((CompProperties_Microwave)props).heatingTicks);

    public bool Operational => parent.Spawned &&
                               !parent.IsForbidden(Faction.OfPlayer) &&
                               (parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? true) &&
                               !(parent.TryGetComp<CompBreakdownable>()?.BrokenDown ?? false);

    public bool TryReheat(Thing meal)
    {
        if (!TemperatureOwnership.ImmersiveChefsFeaturesActive ||
            !Operational ||
            meal is not ThingWithComps withComps ||
            !MealCoveragePolicy.IsCovered(meal.def))
        {
            return false;
        }

        return withComps.GetComp<CompCulinaryState>()?.ReheatCurrentServing(
            targetTemperature: 60f,
            ImmersiveChefsMod.Settings.MicrowaveQualityLoss,
            Find.TickManager?.TicksGame ?? 0) == true;
    }
}
