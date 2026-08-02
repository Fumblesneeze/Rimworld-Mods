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
        IReadOnlyCollection<OptionalIntegration> active,
        IReadOnlyCollection<string> loadedPackageIds)
    {
        States = states;
        Active = active;
        LoadedPackageIds = loadedPackageIds;
    }

    public IReadOnlyDictionary<OptionalIntegration, bool> States { get; }

    public IReadOnlyCollection<OptionalIntegration> Active { get; }

    public IReadOnlyCollection<string> LoadedPackageIds { get; }

    public bool IsActive(OptionalIntegration integration)
    {
        return States.TryGetValue(integration, out var isActive) && isActive;
    }

    public bool ContainsPackage(string packageId)
    {
        return LoadedPackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase);
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
        var loaded = new ReadOnlyCollection<string>(
            normalizedPackageIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList());

        return new IntegrationSnapshot(states, active, loaded);
    }
}

public static class OptionalIntegrationPolicy
{
    public static bool IsEnabled(
        OptionalIntegration integration,
        IntegrationSnapshot snapshot,
        ImmersiveChefsSettings settings)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (!snapshot.IsActive(integration) || ModeFor(integration, settings) == OptionalIntegrationMode.Off)
        {
            return false;
        }

        return integration switch
        {
            OptionalIntegration.Gastronomy => snapshot.ContainsPackage("orion.cashregister"),
            OptionalIntegration.VanillaNutrientPasteExpanded =>
                snapshot.IsActive(OptionalIntegration.VanillaExpandedFramework) &&
                settings.VanillaExpandedFramework != OptionalIntegrationMode.Off,
            _ => true
        };
    }

    private static OptionalIntegrationMode ModeFor(
        OptionalIntegration integration,
        ImmersiveChefsSettings settings)
    {
        return integration switch
        {
            OptionalIntegration.ProcessorFramework => settings.ProcessorFramework,
            OptionalIntegration.ExpandedMaterialsMetals or OptionalIntegration.ExpandedMaterialsMasonry =>
                settings.ExpandedMaterials,
            OptionalIntegration.AbsPolymer => settings.AbsPolymer,
            OptionalIntegration.DubsBadHygiene => settings.DubsBadHygiene,
            OptionalIntegration.Gastronomy => settings.Gastronomy,
            OptionalIntegration.VarietyMatters => settings.VarietyMatters,
            OptionalIntegration.VanillaFoodVarietyExpanded => settings.VanillaFoodVarietyExpanded,
            OptionalIntegration.VanillaExpandedFramework => settings.VanillaExpandedFramework,
            OptionalIntegration.VanillaNutrientPasteExpanded => settings.VanillaNutrientPasteExpanded,
            _ => OptionalIntegrationMode.Auto
        };
    }
}
