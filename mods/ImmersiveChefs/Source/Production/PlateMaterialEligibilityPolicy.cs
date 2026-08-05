namespace ImmersiveChefs;

public static class PlateMaterialEligibilityPolicy
{
    public static bool Allows(MealComplexity? complexity, KitchenMaterialKind material)
    {
        return complexity switch
        {
            MealComplexity.Advanced => material is
                KitchenMaterialKind.Lead or
                KitchenMaterialKind.Iron or
                KitchenMaterialKind.Copper or
                KitchenMaterialKind.Bronze or
                KitchenMaterialKind.Brass or
                KitchenMaterialKind.Silver or
                KitchenMaterialKind.Gold or
                KitchenMaterialKind.Aluminium or
                KitchenMaterialKind.Steel or
                KitchenMaterialKind.StainlessSteel or
                KitchenMaterialKind.AdvancedSteel or
                KitchenMaterialKind.Glitterworld or
                KitchenMaterialKind.Titanium or
                KitchenMaterialKind.Plasteel or
                KitchenMaterialKind.Plastic or
                KitchenMaterialKind.Ceramic or
                KitchenMaterialKind.OtherMetal,
            MealComplexity.Elaborate => material is
                KitchenMaterialKind.Silver or
                KitchenMaterialKind.Gold or
                KitchenMaterialKind.Ceramic,
            _ => true
        };
    }
}
