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

    public bool Operational
    {
        get
        {
            var supported = parent.Spawned &&
                            parent.MapHeld is { } map &&
                            MicrowaveSupportRuntime.FindAt(parent.Position, map, parent) is not null;
            return MicrowaveOperationalPolicy.Allows(
                parent.Spawned,
                parent.IsForbidden(Faction.OfPlayer),
                parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? true,
                parent.TryGetComp<CompBreakdownable>()?.BrokenDown ?? false,
                supported);
        }
    }

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
