using NUnit.Framework;
using System.Reflection;
using System.Runtime.Serialization;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PlateMaterialEligibilityPolicyTests
{
    [TestCase(KitchenMaterialKind.PrimitiveStone)]
    [TestCase(KitchenMaterialKind.Wood)]
    [TestCase(KitchenMaterialKind.Adobe)]
    [TestCase(KitchenMaterialKind.Lead)]
    [TestCase(KitchenMaterialKind.Steel)]
    [TestCase(KitchenMaterialKind.Plastic)]
    [TestCase(KitchenMaterialKind.Ceramic)]
    [TestCase(KitchenMaterialKind.Silver)]
    [TestCase(KitchenMaterialKind.Gold)]
    public void Simple_meals_accept_every_supported_plate_material(KitchenMaterialKind material)
    {
        Assert.That(
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Simple, material),
            Is.True);
    }

    [TestCase(KitchenMaterialKind.PrimitiveStone, false)]
    [TestCase(KitchenMaterialKind.Wood, false)]
    [TestCase(KitchenMaterialKind.Adobe, false)]
    [TestCase(KitchenMaterialKind.Lead, true)]
    [TestCase(KitchenMaterialKind.Iron, true)]
    [TestCase(KitchenMaterialKind.Copper, true)]
    [TestCase(KitchenMaterialKind.Bronze, true)]
    [TestCase(KitchenMaterialKind.Brass, true)]
    [TestCase(KitchenMaterialKind.Aluminium, true)]
    [TestCase(KitchenMaterialKind.Steel, true)]
    [TestCase(KitchenMaterialKind.StainlessSteel, true)]
    [TestCase(KitchenMaterialKind.AdvancedSteel, true)]
    [TestCase(KitchenMaterialKind.Titanium, true)]
    [TestCase(KitchenMaterialKind.Plasteel, true)]
    [TestCase(KitchenMaterialKind.OtherMetal, true)]
    [TestCase(KitchenMaterialKind.Glitterworld, true)]
    [TestCase(KitchenMaterialKind.Plastic, true)]
    [TestCase(KitchenMaterialKind.Ceramic, true)]
    [TestCase(KitchenMaterialKind.Silver, true)]
    [TestCase(KitchenMaterialKind.Gold, true)]
    public void Fine_meals_require_metal_plastic_or_registered_ceramic(
        KitchenMaterialKind material,
        bool expected)
    {
        Assert.That(
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Advanced, material),
            Is.EqualTo(expected));
    }

    [TestCase(KitchenMaterialKind.Silver, true)]
    [TestCase(KitchenMaterialKind.Gold, true)]
    [TestCase(KitchenMaterialKind.Ceramic, true)]
    [TestCase(KitchenMaterialKind.Wood, false)]
    [TestCase(KitchenMaterialKind.Steel, false)]
    [TestCase(KitchenMaterialKind.Plasteel, false)]
    [TestCase(KitchenMaterialKind.Plastic, false)]
    [TestCase(KitchenMaterialKind.Glitterworld, false)]
    public void Lavish_meals_accept_only_precious_or_registered_ceramic_plates(
        KitchenMaterialKind material,
        bool expected)
    {
        Assert.That(
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Elaborate, material),
            Is.EqualTo(expected));
    }

    [Test]
    public void Unclassified_covered_meals_use_the_permissive_simple_tier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PlateMaterialEligibilityPolicy.Allows(null, KitchenMaterialKind.Wood),
                Is.True);
            Assert.That(
                PlateMaterialEligibilityPolicy.Allows(null, KitchenMaterialKind.PrimitiveStone),
                Is.True);
        });
    }

    [TestCase("WoodLog", false, true, false, MealComplexity.Advanced, false)]
    [TestCase("BlocksGranite", false, false, true, MealComplexity.Simple, true)]
    [TestCase("Steel", true, false, false, MealComplexity.Advanced, true)]
    [TestCase("Steel", true, false, false, MealComplexity.Elaborate, false)]
    [TestCase("Gold", true, false, false, MealComplexity.Elaborate, true)]
    public void Runtime_eligibility_uses_the_actual_plate_stuff(
        string stuffDefName,
        bool metallic,
        bool woody,
        bool stony,
        MealComplexity complexity,
        bool expected)
    {
        var thing = Plate(stuffDefName, metallic, woody, stony);

        Assert.That(
            PlateMaterialEligibilityRuntime.Allows(thing, complexity),
            Is.EqualTo(expected));
    }

    [Test]
    public void Fixed_adobe_plate_is_simple_only_without_inventing_stuff()
    {
        var thing = Plate(stuffDefName: null, fixedKind: KitchenMaterialKind.Adobe);

        Assert.Multiple(() =>
        {
            Assert.That(
                PlateMaterialEligibilityRuntime.Allows(thing, MealComplexity.Simple),
                Is.True);
            Assert.That(
                PlateMaterialEligibilityRuntime.Allows(thing, MealComplexity.Advanced),
                Is.False);
        });
    }

    private static Thing Plate(
        string? stuffDefName,
        bool metallic = false,
        bool woody = false,
        bool stony = false,
        KitchenMaterialKind? fixedKind = null)
    {
        var plateDef = Uninitialized<ThingDef>();
        plateDef.modExtensions = new List<DefModExtension>
        {
            new KitchenwareExtension
            {
                product = KitchenwareProduct.Plate,
                fixedMaterialKind = fixedKind
            }
        };
        var thing = Uninitialized<Thing>();
        thing.def = plateDef;
        if (stuffDefName is null)
        {
            return thing;
        }

        var stuff = Uninitialized<ThingDef>();
        stuff.defName = stuffDefName;
        stuff.stuffProps = Uninitialized<StuffProperties>();
        stuff.stuffProps.categories = new List<StuffCategoryDef>();
        foreach (var categoryName in Categories(metallic, woody, stony))
        {
            var category = Uninitialized<StuffCategoryDef>();
            category.defName = categoryName;
            stuff.stuffProps.categories.Add(category);
        }

        typeof(Thing).GetField("stuffInt", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(thing, stuff);
        return thing;
    }

    private static IEnumerable<string> Categories(bool metallic, bool woody, bool stony)
    {
        if (metallic) yield return "Metallic";
        if (woody) yield return "Woody";
        if (stony) yield return "Stony";
    }

    private static T Uninitialized<T>() where T : class =>
        (T)FormatterServices.GetUninitializedObject(typeof(T));
}
