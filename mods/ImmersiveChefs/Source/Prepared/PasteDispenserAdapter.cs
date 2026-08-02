using System.Reflection;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class PasteDispenserAdapter
{
    internal static bool CanDispense(Thing thing)
    {
        if (thing is Building_NutrientPasteDispenser vanilla)
        {
            return vanilla.CanDispenseNow;
        }

        return FindMethod(thing, "get_CanDispenseNow")?.Invoke(thing, null) is true;
    }

    internal static Thing? TryDispense(Thing thing)
    {
        if (thing is Building_NutrientPasteDispenser vanilla)
        {
            return vanilla.TryDispenseFood();
        }

        try
        {
            return FindMethod(thing, "TryDispenseFood")?.Invoke(thing, null) as Thing;
        }
        catch (Exception exception)
        {
            Log.Warning($"[ImmersiveChefs] Optional paste dispenser shape failed closed: {exception.GetType().Name}: {exception.Message}");
            return null;
        }
    }

    private static MethodInfo? FindMethod(Thing thing, string name) =>
        thing.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null, Type.EmptyTypes, modifiers: null);
}
