using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class FoodTextureVarietyPersistenceTests
{
    [Test]
    public void Finds_the_unique_selected_three_graphic_group_in_the_finalized_array()
    {
        var finalized = new[]
        {
            "Meals/Vegetable_a",
            "Meals/Vegetable_b",
            "Meals/Vegetable_c",
            "Meals/Meat_a",
            "Meals/Meat_b",
            "Meals/Meat_c"
        };

        var index = FoodTextureVarietyPersistencePolicy.FindUniqueGroupStart(
            finalized,
            new[] { "Meals/Meat_a", "Meals/Meat_b", "Meals/Meat_c" });

        Assert.That(index, Is.EqualTo(3));
    }

    [TestCaseSource(nameof(UnrestorableSelections))]
    public void Refuses_to_persist_missing_incomplete_or_ambiguous_selections(
        string[] finalized,
        string[] selected)
    {
        Assert.That(
            FoodTextureVarietyPersistencePolicy.FindUniqueGroupStart(finalized, selected),
            Is.EqualTo(FoodTextureVarietyPersistencePolicy.NoPersistedGroup));
    }

    [TestCase(-1, 6, false)]
    [TestCase(0, 2, false)]
    [TestCase(0, 3, true)]
    [TestCase(1, 6, false)]
    [TestCase(3, 6, true)]
    [TestCase(4, 6, false)]
    public void Restores_only_complete_three_graphic_groups(int start, int count, bool expected)
    {
        Assert.That(
            FoodTextureVarietyPersistencePolicy.CanRestoreGroup(start, count),
            Is.EqualTo(expected));
    }

    private static readonly object[] UnrestorableSelections =
    {
        new object[]
        {
            new[] { "Meals/A", "Meals/B", "Meals/C" },
            Array.Empty<string>()
        },
        new object[]
        {
            new[] { "Meals/A", "Meals/B", "Meals/C" },
            new[] { "Meals/A", "Meals/B" }
        },
        new object[]
        {
            new[] { "Meals/A", "Meals/B", "Meals/C" },
            new[] { "Meals/X", "Meals/Y", "Meals/Z" }
        },
        new object[]
        {
            new[] { "Meals/A", "Meals/B", "Meals/C", "Meals/A", "Meals/B", "Meals/C" },
            new[] { "Meals/A", "Meals/B", "Meals/C" }
        },
        new object[]
        {
            new[] { "Meals/X", "Meals/A", "Meals/B", "Meals/C", "Meals/Y", "Meals/Z" },
            new[] { "Meals/A", "Meals/B", "Meals/C" }
        }
    };
}
