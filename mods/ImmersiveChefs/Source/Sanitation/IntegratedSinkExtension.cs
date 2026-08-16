using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class IntegratedSinkExtension : DefModExtension
{
    public float dishwashingWater = 1f;
    public float preparationWater = 1f;
    public float minimumDrinkWater = 0.04f;
}

public sealed class CompProperties_IntegratedSink : CompProperties
{
    public CompProperties_IntegratedSink()
    {
        compClass = typeof(CompIntegratedSink);
    }
}

public sealed class CompIntegratedSink : ThingComp
{
    public override string CompInspectStringExtra()
    {
        var requiredWater = parent.def.GetModExtension<IntegratedSinkExtension>()?.preparationWater ?? 1f;
        return IntegratedSinkRuntime.RequiresDubsWater(parent) &&
               !DubsWaterAdapter.CanUseIntegratedSink(parent, requiredWater)
            ? "ImmersiveChefs_Dishwasher_PauseNoWater".Translate().CapitalizeFirst()
            : string.Empty;
    }
}

public static class IntegratedSinkWaterPolicy
{
    public static bool RequiresSuppliedWater(
        bool hasIntegratedSink,
        bool dubsIntegrationEnabled,
        bool validatedBridgeAvailable) =>
        hasIntegratedSink && dubsIntegrationEnabled && validatedBridgeAvailable;

    public static float RequiredAmount(float configuredAmount) =>
        Math.Max(0.001f, configuredAmount);
}

internal static class IntegratedSinkRuntime
{
    internal static bool IsIntegratedSink(Thing thing) =>
        thing.def.GetModExtension<IntegratedSinkExtension>() is not null;

    internal static bool RequiresDubsWater(Thing thing) =>
        IntegratedSinkWaterPolicy.RequiresSuppliedWater(
            IsIntegratedSink(thing),
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene),
            DubsWaterAdapter.IntegratedSinkBridgeAvailable);

    internal static bool CanStartBill(Thing thing)
    {
        if (!RequiresDubsWater(thing) ||
            thing.def.GetModExtension<IntegratedSinkExtension>() is not { } extension)
        {
            return true;
        }

        return DubsWaterAdapter.CanUseIntegratedSink(
            thing,
            IntegratedSinkWaterPolicy.RequiredAmount(extension.preparationWater));
    }

    internal static IEnumerable<Toil> AddPreparationWaterUse(
        JobDriver_DoBill driver,
        IEnumerable<Toil> original)
    {
        var sink = driver.job.GetTarget(TargetIndex.A).Thing;
        if (sink is null || !RequiresDubsWater(sink) ||
            sink.def.GetModExtension<IntegratedSinkExtension>() is not { } extension)
        {
            foreach (var toil in original)
            {
                yield return toil;
            }

            yield break;
        }

        var debit = ToilMaker.MakeToil("ImmersiveChefs_DebitPrepSinkWater");
        debit.initAction = () =>
        {
            if (!DubsWaterAdapter.TryConsumeCycleWater(
                    sink,
                    IntegratedSinkWaterPolicy.RequiredAmount(extension.preparationWater),
                    out _))
            {
                driver.EndJobWith(JobCondition.Incompletable);
            }
        };
        debit.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return debit;

        foreach (var toil in original)
        {
            yield return toil;
        }
    }
}
