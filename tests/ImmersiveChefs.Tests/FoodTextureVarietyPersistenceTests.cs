using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class FoodTextureVarietyPersistenceTests
{
    [TestCase("FoodTextureVariety", "1.0.0.0", true)]
    [TestCase("FoodTextureVariety", "9.8.7.6", true)]
    [TestCase("Lookalike", "1.0.0.0", false)]
    public void Compatible_texture_provider_rebuild_is_accepted(string name, string version, bool expected)
    {
        var identity = new System.Reflection.AssemblyName { Name = name, Version = new Version(version) };
        Assert.That(FoodTextureVarietyCompatibility.IsSupportedAssembly(identity), Is.EqualTo(expected));
    }

    [TestCase(true, true, false, true)]
    [TestCase(false, true, false, false)]
    [TestCase(true, false, false, false)]
    [TestCase(true, true, true, false)]
    public void Supplemental_persistence_requires_usable_members_and_no_upstream_persistence(
        bool fieldsMatch, bool callableMatches, bool declaresPersistence, bool expected)
    {
        Assert.That(
            FoodTextureVarietyCompatibility.CanSupplementPersistence(fieldsMatch, callableMatches, declaresPersistence),
            Is.EqualTo(expected));
    }

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
