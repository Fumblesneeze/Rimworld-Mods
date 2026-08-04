using RimWorld;
using Verse;

namespace ImmersiveChefs;

[DefOf]
public static class ImmersiveChefsDefOf
{
    public static JobDef ImmersiveChefs_DoDishes = null!;
    public static JobDef ImmersiveChefs_AssistCooking = null!;
    public static JobDef ImmersiveChefs_DispensePreparedPaste = null!;
    public static JobDef ImmersiveChefs_PlateMeals = null!;
    public static ThingDef ImmersiveChefs_Dishwasher = null!;
    public static ThingDef ImmersiveChefs_IndustrialDishwasher = null!;

    static ImmersiveChefsDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ImmersiveChefsDefOf));
    }
}
