using Verse;

namespace ImmersiveChefs;

public sealed class KitchenwareExtension : DefModExtension
{
    public KitchenwareProduct product;
    public KitchenMaterialKind? fixedMaterialKind;
    public bool selfCleaning;
    public float plateEquivalent = 1f;
}

public sealed class MealCoverageExtension : DefModExtension
{
    public bool excluded;
    public MealComplexity? complexity;
}

public sealed class KitchenwareRecipeExtension : DefModExtension
{
    public KitchenwareProduct product;
    public FabricationTier fabricationTier;
}

public sealed class KitchenwareAlertStationExtension : DefModExtension
{
    public bool enabled = true;
}
