using RimWorld;

namespace ImmersiveChefs;

public readonly struct KitchenwareStats
{
    public KitchenwareStats(
        float materialCleanliness,
        float cookingSpeedFactor,
        float comfort,
        int materialCulinaryOffset,
        int craftsmanshipOffset)
    {
        MaterialCleanliness = Math.Max(0f, Math.Min(100f, materialCleanliness));
        CookingSpeedFactor = Math.Max(0.1f, cookingSpeedFactor);
        Comfort = Math.Max(0f, Math.Min(1f, comfort));
        MaterialCulinaryOffset = materialCulinaryOffset;
        CraftsmanshipOffset = craftsmanshipOffset;
        CulinaryQualityModifier = materialCulinaryOffset + craftsmanshipOffset;
        CulinaryToolScore = Math.Max(0, Math.Min(100, 50 + (2 * CulinaryQualityModifier)));
    }

    public float MaterialCleanliness { get; }
    public float CookingSpeedFactor { get; }
    public float Comfort { get; }
    public int MaterialCulinaryOffset { get; }
    public int CraftsmanshipOffset { get; }
    public int CulinaryQualityModifier { get; }
    public int CulinaryToolScore { get; }
}

public static class KitchenwareStatCalculator
{
    public static KitchenwareStats Calculate(
        KitchenMaterialKind material,
        KitchenwareProduct product,
        QualityCategory quality)
    {
        var profile = ProfileFor(material);
        var craftsmanshipOffset = quality switch
        {
            QualityCategory.Awful => -10,
            QualityCategory.Poor => -5,
            QualityCategory.Normal => 0,
            QualityCategory.Good => 3,
            QualityCategory.Excellent => 6,
            QualityCategory.Masterwork => 10,
            QualityCategory.Legendary => 15,
            _ => 0
        };

        return new KitchenwareStats(
            profile.Cleanliness,
            product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife or KitchenwareProduct.Plate
                ? profile.Speed
                : 1f,
            profile.Comfort,
            product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife
                ? profile.CulinaryOffset
                : 0,
            product is KitchenwareProduct.Cookware or KitchenwareProduct.ChefsKnife
                ? craftsmanshipOffset
                : 0);
    }

    private static MaterialProfile ProfileFor(KitchenMaterialKind material)
    {
        return material switch
        {
            KitchenMaterialKind.PrimitiveStone => new MaterialProfile(15f, 0.60f, 0.10f, -12),
            KitchenMaterialKind.Adobe => new MaterialProfile(5f, 0.65f, 0.10f, -12),
            KitchenMaterialKind.Wood => new MaterialProfile(10f, 0.80f, 0.30f, -8),
            KitchenMaterialKind.Lead => new MaterialProfile(12f, 0.75f, 0.25f, -10),
            KitchenMaterialKind.Iron => new MaterialProfile(30f, 0.90f, 0.35f, -4),
            KitchenMaterialKind.Copper => new MaterialProfile(38f, 1.00f, 0.45f, 0),
            KitchenMaterialKind.Bronze => new MaterialProfile(60f, 1.00f, 0.60f, 2),
            KitchenMaterialKind.Brass => new MaterialProfile(55f, 1.00f, 0.65f, 2),
            KitchenMaterialKind.Silver => new MaterialProfile(65f, 1.05f, 0.65f, 4),
            KitchenMaterialKind.Gold => new MaterialProfile(68f, 0.95f, 0.80f, 5),
            KitchenMaterialKind.Aluminium => new MaterialProfile(48f, 1.10f, 0.50f, 2),
            KitchenMaterialKind.Steel => new MaterialProfile(50f, 1.00f, 0.50f, 0),
            KitchenMaterialKind.StainlessSteel => new MaterialProfile(85f, 1.15f, 0.65f, 8),
            KitchenMaterialKind.AdvancedSteel => new MaterialProfile(72f, 1.20f, 0.65f, 8),
            KitchenMaterialKind.Glitterworld => new MaterialProfile(100f, 1.35f, 0.90f, 15),
            KitchenMaterialKind.Titanium => new MaterialProfile(80f, 1.25f, 0.70f, 10),
            KitchenMaterialKind.Plasteel => new MaterialProfile(78f, 1.25f, 0.70f, 10),
            KitchenMaterialKind.Plastic => new MaterialProfile(45f, 1.00f, 0.55f, -1),
            KitchenMaterialKind.Ceramic => new MaterialProfile(75f, 0.95f, 0.65f, 4),
            _ => new MaterialProfile(45f, 1.00f, 0.45f, 0)
        };
    }

    private readonly struct MaterialProfile
    {
        public MaterialProfile(float cleanliness, float speed, float comfort, int culinaryOffset)
        {
            Cleanliness = cleanliness;
            Speed = speed;
            Comfort = comfort;
            CulinaryOffset = culinaryOffset;
        }

        public float Cleanliness { get; }
        public float Speed { get; }
        public float Comfort { get; }
        public int CulinaryOffset { get; }
    }
}
