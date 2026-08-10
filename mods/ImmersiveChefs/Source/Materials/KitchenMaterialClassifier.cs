namespace ImmersiveChefs;

public enum KitchenwareProduct
{
    Cookware,
    Plate,
    Cutlery,
    ChefsKnife
}

public enum FabricationTier
{
    PrimitiveStone,
    Soft,
    Intermediate,
    Modern
}

public enum KitchenMaterialKind
{
    PrimitiveStone,
    Wood,
    Lead,
    Uranium,
    Iron,
    Copper,
    Bronze,
    Brass,
    Silver,
    Gold,
    Aluminium,
    Steel,
    StainlessSteel,
    AdvancedSteel,
    Glitterworld,
    Titanium,
    Plasteel,
    Plastic,
    Ceramic,
    Adobe,
    OtherMetal
}

public static class KitchenMaterialFabricationPolicy
{
    public static bool Allows(
        KitchenwareProduct product,
        FabricationTier recipeTier,
        KitchenMaterialClassification classification)
    {
        if (recipeTier == FabricationTier.Modern &&
            product is KitchenwareProduct.Plate or KitchenwareProduct.Cutlery)
        {
            return classification.Kind is not (
                KitchenMaterialKind.PrimitiveStone or
                KitchenMaterialKind.Wood or
                KitchenMaterialKind.Adobe or
                KitchenMaterialKind.Ceramic or
                KitchenMaterialKind.Lead);
        }

        if (product == KitchenwareProduct.ChefsKnife)
        {
            return classification.FabricationTier is FabricationTier.Intermediate or FabricationTier.Modern;
        }

        return classification.FabricationTier == recipeTier;
    }
}

public sealed class KitchenMaterialDescriptor
{
    public KitchenMaterialDescriptor(
        string defName,
        bool isMetallic = false,
        bool isWoody = false,
        bool isStony = false)
    {
        if (string.IsNullOrWhiteSpace(defName))
        {
            throw new ArgumentException("A Stuff Def name is required.", nameof(defName));
        }

        DefName = defName;
        IsMetallic = isMetallic;
        IsWoody = isWoody;
        IsStony = isStony;
    }

    public string DefName { get; }
    public bool IsMetallic { get; }
    public bool IsWoody { get; }
    public bool IsStony { get; }
}

public sealed class KitchenMaterialClassification
{
    public KitchenMaterialClassification(KitchenMaterialKind kind, FabricationTier fabricationTier)
    {
        Kind = kind;
        FabricationTier = fabricationTier;
    }

    public KitchenMaterialKind Kind { get; }
    public FabricationTier FabricationTier { get; }
}

public sealed class KitchenMaterialClassifier
{
    private static readonly HashSet<string> WoodDefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "WoodLog"
    };

    private static readonly IReadOnlyDictionary<string, KitchenMaterialKind> ExplicitKinds =
        new Dictionary<string, KitchenMaterialKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["Steel"] = KitchenMaterialKind.Steel,
            ["Silver"] = KitchenMaterialKind.Silver,
            ["Gold"] = KitchenMaterialKind.Gold,
            ["Uranium"] = KitchenMaterialKind.Uranium,
            ["Plasteel"] = KitchenMaterialKind.Plasteel,
            ["EM_Iron"] = KitchenMaterialKind.Iron,
            ["EM_MildSteel"] = KitchenMaterialKind.Steel,
            ["EM_TemperedSteel"] = KitchenMaterialKind.AdvancedSteel,
            ["EM_Lead"] = KitchenMaterialKind.Lead,
            ["EM_Bronze"] = KitchenMaterialKind.Bronze,
            ["EM_Copper"] = KitchenMaterialKind.Copper,
            ["EM_StainlessSteel"] = KitchenMaterialKind.StainlessSteel,
            ["EM_Titanium"] = KitchenMaterialKind.Titanium,
            ["ABSPolymer"] = KitchenMaterialKind.Plastic
        };

    private readonly Dictionary<string, KitchenMaterialKind> includes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> excludes = new(StringComparer.OrdinalIgnoreCase);

    private KitchenMaterialClassifier()
    {
        foreach (var pair in ExplicitKinds)
        {
            includes[pair.Key] = pair.Value;
        }
    }

    public static KitchenMaterialClassifier CreateDefault()
    {
        return new KitchenMaterialClassifier();
    }

    public void Register(string defName, KitchenMaterialKind kind)
    {
        includes[defName] = kind;
        excludes.Remove(defName);
    }

    public void Exclude(string defName)
    {
        excludes.Add(defName);
        includes.Remove(defName);
    }

    public KitchenMaterialClassification? Classify(
        KitchenMaterialDescriptor material,
        KitchenwareProduct product)
    {
        if (material is null)
        {
            throw new ArgumentNullException(nameof(material));
        }

        if (excludes.Contains(material.DefName))
        {
            return null;
        }

        var hasExplicitKind = includes.TryGetValue(material.DefName, out var kind);
        if (hasExplicitKind)
        {
            return ClassifyExplicit(kind, product);
        }

        if (WoodDefs.Contains(material.DefName) || (material.IsWoody && !material.IsMetallic && !material.IsStony))
        {
            return product is KitchenwareProduct.Plate or KitchenwareProduct.Cutlery
                ? new KitchenMaterialClassification(KitchenMaterialKind.Wood, FabricationTier.Soft)
                : null;
        }

        if (material.IsStony)
        {
            return product is KitchenwareProduct.Cookware or KitchenwareProduct.Plate
                ? new KitchenMaterialClassification(KitchenMaterialKind.PrimitiveStone, FabricationTier.PrimitiveStone)
                : null;
        }

        if (!material.IsMetallic)
        {
            return null;
        }

        kind = InferMetalKind(material.DefName);
        return ClassifyExplicit(kind, product);
    }

    private static KitchenMaterialClassification? ClassifyExplicit(
        KitchenMaterialKind kind,
        KitchenwareProduct product)
    {
        if (kind is KitchenMaterialKind.Plastic or KitchenMaterialKind.Ceramic)
        {
            if (product is not (KitchenwareProduct.Plate or KitchenwareProduct.Cutlery))
            {
                return null;
            }

            return new KitchenMaterialClassification(kind, FabricationTier.Modern);
        }

        if (kind == KitchenMaterialKind.Lead &&
            product is KitchenwareProduct.Plate or KitchenwareProduct.Cutlery)
        {
            return new KitchenMaterialClassification(kind, FabricationTier.Soft);
        }

        return new KitchenMaterialClassification(kind, TierForMetal(kind));
    }

    private static KitchenMaterialKind InferMetalKind(string defName)
    {
        if (defName.IndexOf("Stainless", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.StainlessSteel;
        if (defName.IndexOf("Titan", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Titanium;
        if (defName.IndexOf("Aluminium", StringComparison.OrdinalIgnoreCase) >= 0 ||
            defName.IndexOf("Aluminum", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Aluminium;
        if (defName.IndexOf("Bronze", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Bronze;
        if (defName.IndexOf("Brass", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Brass;
        if (defName.IndexOf("Copper", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Copper;
        if (defName.IndexOf("Lead", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Lead;
        if (defName.IndexOf("Uranium", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Uranium;
        if (defName.IndexOf("Iron", StringComparison.OrdinalIgnoreCase) >= 0) return KitchenMaterialKind.Iron;
        return KitchenMaterialKind.OtherMetal;
    }

    private static FabricationTier TierForMetal(KitchenMaterialKind kind)
    {
        return kind switch
        {
            KitchenMaterialKind.Steel or
            KitchenMaterialKind.StainlessSteel or
            KitchenMaterialKind.AdvancedSteel or
            KitchenMaterialKind.Uranium or
            KitchenMaterialKind.Titanium or
            KitchenMaterialKind.Plasteel => FabricationTier.Modern,
            _ => FabricationTier.Intermediate
        };
    }
}
