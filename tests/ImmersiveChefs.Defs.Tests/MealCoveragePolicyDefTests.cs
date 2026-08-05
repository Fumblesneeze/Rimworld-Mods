using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace ImmersiveChefs.Defs.Tests;

[TestFixture]
[NonParallelizable]
public sealed class MealCoveragePolicyDefTests
{
    [Test]
    public void Exact_rimcuisine_owner_and_product_pair_is_excluded_from_finalized_meal_coverage()
    {
        var cannedMeal = CreateMeal("RC2_CannedMeal", "Mlie.RC2.MaME");

        using (DefDatabaseScope<ThingDef>.Register(cannedMeal))
        {
            Assert.That(MealCoveragePolicy.IsCovered(cannedMeal), Is.False);
        }
    }

    [Test]
    public void Same_product_name_from_an_unrelated_owner_remains_covered()
    {
        var unrelatedCannedMeal = CreateMeal("RC2_CannedMeal", "example.unrelated.meals");

        using (DefDatabaseScope<ThingDef>.Register(unrelatedCannedMeal))
        {
            Assert.That(MealCoveragePolicy.IsCovered(unrelatedCannedMeal), Is.True);
        }
    }

    [TestCase("RC2_Pizza", "Mlie.RC2.MaME")]
    [TestCase("RC2_ThinPottage", "Mlie.RC2.Core")]
    public void Registered_rimcuisine_meals_remain_covered(string defName, string packageId)
    {
        var meal = CreateMeal(defName, packageId);

        using (DefDatabaseScope<ThingDef>.Register(meal))
        {
            Assert.That(MealCoveragePolicy.IsCovered(meal), Is.True);
        }
    }

    private static ThingDef CreateMeal(string defName, string packageId)
    {
        // ThingDef's normal constructor initializes Unity shaders. The .NET Def test host has no
        // Unity runtime, so hydrate only the real Def fields the production policy reads.
        var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        def.defName = defName;
        def.thingClass = typeof(ThingWithComps);
        def.ingestible = new IngestibleProperties { foodType = FoodTypeFlags.Meal };
        def.modContentPack = CreateModContentPack(packageId);
        return def;
    }

    private static ModContentPack CreateModContentPack(string packageId)
    {
        // The normal constructor enumerates the live ModLister. That is another game-host-only
        // dependency, while production reads only the real PackageId property in this test.
        var pack = (ModContentPack)FormatterServices.GetUninitializedObject(typeof(ModContentPack));
        var packageIdField = typeof(ModContentPack).GetField(
            "packageIdInt",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new AssertionException("RimWorld 1.6 ModContentPack must retain packageIdInt.");
        packageIdField.SetValue(pack, packageId);
        return pack;
    }
}
