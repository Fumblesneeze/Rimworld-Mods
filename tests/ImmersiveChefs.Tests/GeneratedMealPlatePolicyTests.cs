using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class GeneratedMealPlatePolicyTests
{
    [TestCase(GeneratedMealOrigin.GenericOrUnknown, false)]
    [TestCase(GeneratedMealOrigin.ExternalPawnInventory, true)]
    [TestCase(GeneratedMealOrigin.TradeStock, true)]
    public void Only_explicit_external_generation_origins_receive_plates(
        GeneratedMealOrigin origin,
        bool expected)
    {
        Assert.That(GeneratedMealPlatePolicy.AllowsOrigin(origin), Is.EqualTo(expected));
    }

    [TestCase(true, true, false, true)]
    [TestCase(true, true, true, false)]
    [TestCase(false, true, false, false)]
    [TestCase(true, false, false, false)]
    public void Only_non_player_humanlike_faction_pawns_are_external_inventory_origins(
        bool humanlike,
        bool hasFaction,
        bool playerFaction,
        bool expected)
    {
        Assert.That(
            GeneratedMealPlatePolicy.AllowsExternalPawnInventory(
                humanlike,
                hasFaction,
                playerFaction),
            Is.EqualTo(expected));
    }

    [TestCase(true, 3, 0, 3)]
    [TestCase(true, 3, 1, 2)]
    [TestCase(true, 3, 3, 0)]
    [TestCase(true, 3, 4, 0)]
    [TestCase(false, 3, 0, 0)]
    [TestCase(true, 0, 0, 0)]
    public void Missing_count_never_duplicates_or_invents_bindings(
        bool covered,
        int servings,
        int existingBindings,
        int expected)
    {
        Assert.That(
            GeneratedMealPlatePolicy.MissingPlateCount(
                covered,
                servings,
                existingBindings),
            Is.EqualTo(expected));
    }

    [Test]
    public void Simple_selects_the_cheapest_supported_plate()
    {
        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Simple,
            Candidates());

        Assert.That(selected?.StuffDefName, Is.EqualTo("WoodLog"));
    }

    [Test]
    public void Simple_prefers_primitive_service_over_a_cheaper_precious_metal()
    {
        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Simple,
            new[]
            {
                new GeneratedPlateCandidate(
                    "ImmersiveChefs_Plate",
                    "Silver",
                    KitchenMaterialKind.Silver,
                    1f),
                new GeneratedPlateCandidate(
                    "ImmersiveChefs_Plate",
                    "BlocksGranite",
                    KitchenMaterialKind.PrimitiveStone,
                    1.4f)
            });

        Assert.That(selected?.StuffDefName, Is.EqualTo("BlocksGranite"));
    }

    [Test]
    public void Fine_rejects_cheaper_wood_and_stone_then_selects_metal()
    {
        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Advanced,
            Candidates());

        Assert.That(selected?.StuffDefName, Is.EqualTo("Steel"));
    }

    [Test]
    public void Fine_prefers_ordinary_service_over_a_cheaper_precious_metal()
    {
        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Advanced,
            new[]
            {
                new GeneratedPlateCandidate(
                    "ImmersiveChefs_Plate",
                    "Silver",
                    KitchenMaterialKind.Silver,
                    1f),
                new GeneratedPlateCandidate(
                    "ImmersiveChefs_Plate",
                    "Steel",
                    KitchenMaterialKind.Steel,
                    1.9f)
            });

        Assert.That(selected?.StuffDefName, Is.EqualTo("Steel"));
    }

    [Test]
    public void Lavish_rejects_cheaper_steel_and_selects_silver()
    {
        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Elaborate,
            Candidates());

        Assert.That(selected?.StuffDefName, Is.EqualTo("Silver"));
    }

    [Test]
    public void Equal_value_candidates_are_selected_deterministically()
    {
        var candidates = new[]
        {
            new GeneratedPlateCandidate(
                "Z_Plate",
                "Steel",
                KitchenMaterialKind.Steel,
                10f),
            new GeneratedPlateCandidate(
                "A_Plate",
                "Steel",
                KitchenMaterialKind.Steel,
                10f),
            new GeneratedPlateCandidate(
                "A_Plate",
                "Iron",
                KitchenMaterialKind.Iron,
                10f)
        };

        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexity.Advanced,
            candidates);

        Assert.Multiple(() =>
        {
            Assert.That(selected?.PlateDefName, Is.EqualTo("A_Plate"));
            Assert.That(selected?.StuffDefName, Is.EqualTo("Iron"));
        });
    }

    private static IReadOnlyList<GeneratedPlateCandidate> Candidates() =>
        new[]
        {
            new GeneratedPlateCandidate(
                "ImmersiveChefs_Plate",
                "WoodLog",
                KitchenMaterialKind.Wood,
                2f),
            new GeneratedPlateCandidate(
                "ImmersiveChefs_Plate",
                "BlocksGranite",
                KitchenMaterialKind.PrimitiveStone,
                4f),
            new GeneratedPlateCandidate(
                "ImmersiveChefs_Plate",
                "Steel",
                KitchenMaterialKind.Steel,
                8f),
            new GeneratedPlateCandidate(
                "ImmersiveChefs_Plate",
                "Silver",
                KitchenMaterialKind.Silver,
                20f),
            new GeneratedPlateCandidate(
                "ImmersiveChefs_Plate",
                "Gold",
                KitchenMaterialKind.Gold,
                40f)
        };
}
