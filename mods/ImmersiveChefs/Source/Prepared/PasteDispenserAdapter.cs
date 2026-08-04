using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class PasteDispenserAdapter
{
    internal static bool CanDispense(Thing thing)
    {
        if (thing is not Building_NutrientPasteDispenser dispenser)
        {
            return false;
        }

        var kind = Classify(thing);
        if (kind == PasteDispenserKind.Unsupported)
        {
            return false;
        }

        try
        {
            return dispenser.CanDispenseNow;
        }
        catch (Exception exception) when (kind == PasteDispenserKind.VanillaNutrientPasteExpanded)
        {
            VanillaNutrientPasteExpandedAdapter.DisableAfterInvocationFailure(exception);
            return false;
        }
    }

    internal static Thing? TryDispense(Thing thing)
    {
        if (thing is not Building_NutrientPasteDispenser dispenser)
        {
            return null;
        }

        var kind = Classify(thing);
        if (kind == PasteDispenserKind.Unsupported)
        {
            return null;
        }

        try
        {
            return dispenser.TryDispenseFood();
        }
        catch (Exception exception) when (kind == PasteDispenserKind.VanillaNutrientPasteExpanded)
        {
            VanillaNutrientPasteExpandedAdapter.DisableAfterInvocationFailure(exception);
            return null;
        }
    }

    internal static bool Supports(Thing thing) =>
        Supports(thing.def, thing.GetType());

    internal static bool Supports(ThingDef def, Type runtimeType) =>
        Classify(def, runtimeType) != PasteDispenserKind.Unsupported;

    private static PasteDispenserKind Classify(Thing thing) =>
        Classify(thing.def, thing.GetType());

    private static PasteDispenserKind Classify(ThingDef def, Type runtimeType)
    {
        return PasteDispenserCompatibility.Classify(
            def.defName,
            runtimeType.FullName,
            runtimeType.Assembly.GetName().Name,
            runtimeType.BaseType?.FullName,
            def.comps?.Select(properties => properties.GetType().FullName ?? string.Empty),
            VanillaNutrientPasteExpandedAdapter.Controls(def, runtimeType));
    }
}
