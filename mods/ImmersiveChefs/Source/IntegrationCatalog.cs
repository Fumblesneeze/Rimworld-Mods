using System.Collections.ObjectModel;

namespace ImmersiveChefs;

public enum OptionalIntegration
{
    Royalty,
    ProcessorFramework,
    ExpandedMaterialsMetals,
    ExpandedMaterialsMasonry,
    AbsPolymer,
    DubsBadHygiene,
    Gastronomy,
    VarietyMatters,
    VanillaFoodVarietyExpanded,
    VanillaExpandedFramework,
    VanillaNutrientPasteExpanded
}

public sealed class IntegrationSnapshot
{
    internal IntegrationSnapshot(
        IReadOnlyDictionary<OptionalIntegration, bool> states,
        IReadOnlyCollection<OptionalIntegration> active)
    {
        States = states;
        Active = active;
    }

    public IReadOnlyDictionary<OptionalIntegration, bool> States { get; }

    public IReadOnlyCollection<OptionalIntegration> Active { get; }

    public bool IsActive(OptionalIntegration integration)
    {
        return States.TryGetValue(integration, out var isActive) && isActive;
    }
}

public static class IntegrationCatalog
{
    private static readonly IReadOnlyDictionary<OptionalIntegration, string> PackageIds =
        new Dictionary<OptionalIntegration, string>
        {
            [OptionalIntegration.Royalty] = "ludeon.rimworld.royalty",
            [OptionalIntegration.ProcessorFramework] = "syrchalis.processor.framework",
            [OptionalIntegration.ExpandedMaterialsMetals] = "argon.expandedmaterials.metals",
            [OptionalIntegration.ExpandedMaterialsMasonry] = "argon.expandedmaterials.masonry",
            [OptionalIntegration.AbsPolymer] = "mlie.simplysublimeabspolymer",
            [OptionalIntegration.DubsBadHygiene] = "dubwise.dubsbadhygiene",
            [OptionalIntegration.Gastronomy] = "orion.gastronomy",
            [OptionalIntegration.VarietyMatters] = "evyatar108.varietymattersimprovedredux",
            [OptionalIntegration.VanillaFoodVarietyExpanded] = "vanillaexpanded.vanillafoodvarietyexpanded",
            [OptionalIntegration.VanillaExpandedFramework] = "oskarpotocki.vanillafactionsexpanded.core",
            [OptionalIntegration.VanillaNutrientPasteExpanded] = "vanillaexpanded.vnutriente"
        };

    public static IntegrationSnapshot Detect(IEnumerable<string> loadedPackageIds)
    {
        if (loadedPackageIds is null)
        {
            throw new ArgumentNullException(nameof(loadedPackageIds));
        }

        var normalizedPackageIds = new HashSet<string>(loadedPackageIds, StringComparer.OrdinalIgnoreCase);
        var mutableStates = PackageIds.ToDictionary(
            pair => pair.Key,
            pair => normalizedPackageIds.Contains(pair.Value));
        var states = new ReadOnlyDictionary<OptionalIntegration, bool>(mutableStates);
        var active = new ReadOnlyCollection<OptionalIntegration>(
            mutableStates.Where(pair => pair.Value).Select(pair => pair.Key).ToList());

        return new IntegrationSnapshot(states, active);
    }
}
