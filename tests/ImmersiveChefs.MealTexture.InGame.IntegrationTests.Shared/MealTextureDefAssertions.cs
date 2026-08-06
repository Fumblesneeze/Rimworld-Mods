using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

internal static class MealTextureDefAssertions
{
    internal const string DmtrPackageId = "Thekiborg.DMTR";
    internal const string FtvCorePackageId = "Goat.Food.Texture.Variety.Core";
    internal const string FtvPackageId = "Goat.Food.Texture.Variety";
    internal const string DmtrGraphicType =
        "DynamicMealTextureReplacer.Graphic_IngredientsVariant";
    internal const string DmtrExtensionType =
        "DynamicMealTextureReplacer.ModExtension_DynamicMealTextureReplacer";
    internal const string FtvGraphicType =
        "FoodTextureVariety.Graphic_MealVariantsExpanded";
    internal const string FtvCompType =
        "FoodTextureVariety.CompFoodAlternateTexture";

    internal static void RequireActive(params string[] packageIds)
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        foreach (var packageId in packageIds)
        {
            IntegrationAssert.True(
                active.Contains(packageId, StringComparer.OrdinalIgnoreCase),
                $"Expected exact active package '{packageId}'.");
        }
    }

    internal static void RequireAbsent(params string[] packageIds)
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        foreach (var packageId in packageIds)
        {
            IntegrationAssert.True(
                !active.Contains(packageId, StringComparer.OrdinalIgnoreCase),
                $"Package '{packageId}' must be absent from this exact group.");
        }
    }

    internal static void AssertVanillaMealOwner()
    {
        var meal = DefDatabase<ThingDef>.GetNamed("MealSimple");
        IntegrationAssert.Equal(
            typeof(Graphic_MealVariants),
            meal.graphicData.graphicClass,
            "The absent/FTV-only group must retain vanilla's MealSimple graphic owner.");
        IntegrationAssert.True(
            meal.modExtensions?.All(extension =>
                !string.Equals(extension.GetType().FullName, DmtrExtensionType, StringComparison.Ordinal)) != false,
            "MealSimple must not retain a DMTR extension when DMTR is absent.");
        AssertImmersiveMealState(meal);
    }

    internal static void AssertDmtrOwnsVanillaMeals()
    {
        const string Eggs = "categories=EggsFertilized,EggsUnfertilized";
        const string Meat = "categories=MeatRaw;disallowedThingDefs=Meat_Megaspider";
        const string SpecialMeat = "thingDefs=Meat_Megaspider";
        const string Fruit = "thingDefs=RawAgave,RawBerries";
        var expected = new[]
        {
            new { Def = "MealSimple", Atlas = "DMTR/SimpleMealAtlas/Simple", Fallback = "Things/Item/Meal/Simple", FallbackClass = typeof(Graphic_MealVariants), Width = 432, Height = 1440, Mappings = new[] { Eggs + "=>1", Meat + "=>4", "thingDefs=InsectJelly=>2", SpecialMeat + "=>1", "thingDefs=Milk=>1", Fruit + "=>2", "thingDefs=RawCorn=>1", "thingDefs=RawFungus=>1", "thingDefs=RawPotatoes=>2", "thingDefs=RawRice=>1" } },
            new { Def = "MealFine", Atlas = "DMTR/FineMealAtlas/Fine", Fallback = "Things/Item/Meal/Fine", FallbackClass = typeof(Graphic_MealVariants), Width = 288, Height = 2160, Mappings = new[] { Eggs + "=>2", Meat + "=>2", "thingDefs=InsectJelly=>1", SpecialMeat + "=>1", "thingDefs=Milk,RawCorn=>1", "thingDefs=Milk,RawPotatoes=>1", "thingDefs=Milk=>2", Fruit + "=>2", "thingDefs=RawCorn=>1", "thingDefs=RawFungus=>1", "thingDefs=RawPotatoes;" + Meat + "=>1", "thingDefs=RawPotatoes=>2", "thingDefs=RawRice;" + Eggs + "=>1", "thingDefs=RawRice;" + Meat + "=>1", "thingDefs=RawRice=>2" } },
            new { Def = "MealFine_Meat", Atlas = "DMTR/FineMealAtlas/FineCarn", Fallback = "Things/Item/Meal/Fine", FallbackClass = typeof(Graphic_StackCount), Width = 576, Height = 720, Mappings = new[] { Eggs + "=>3", Meat + "=>4", "thingDefs=InsectJelly=>3", SpecialMeat + "=>1", "thingDefs=Milk=>2" } },
            new { Def = "MealFine_Veg", Atlas = "DMTR/FineMealAtlas/FineVeg", Fallback = "Things/Item/Meal/FineVeg", FallbackClass = typeof(Graphic_StackCount), Width = 432, Height = 1152, Mappings = new[] { Eggs + "=>3", "thingDefs=InsectJelly=>3", "thingDefs=Milk=>2", Fruit + "=>2", "thingDefs=RawCorn=>1", "thingDefs=RawFungus=>1", "thingDefs=RawPotatoes=>3", "thingDefs=RawRice=>3" } },
            new { Def = "MealLavish", Atlas = "DMTR/LavishMealAtlas/Lavish", Fallback = "Things/Item/Meal/Fine", FallbackClass = typeof(Graphic_MealVariants), Width = 288, Height = 2304, Mappings = new[] { Eggs + "=>2", Meat + "=>2", "thingDefs=InsectJelly=>1", SpecialMeat + "=>1", "thingDefs=Milk,RawCorn=>1", "thingDefs=Milk,RawPotatoes=>1", "thingDefs=Milk=>2", "thingDefs=RawAgave=>1", "thingDefs=RawBerries=>2", "thingDefs=RawCorn=>1", "thingDefs=RawFungus=>1", "thingDefs=RawPotatoes;" + Meat + "=>2", "thingDefs=RawPotatoes=>2", "thingDefs=RawRice;" + Eggs + "=>1", "thingDefs=RawRice;" + Meat + "=>2", "thingDefs=RawRice=>2" } },
            new { Def = "MealLavish_Meat", Atlas = "DMTR/LavishMealAtlas/LavishCarn", Fallback = "Things/Item/Meal/Fine", FallbackClass = typeof(Graphic_StackCount), Width = 576, Height = 720, Mappings = new[] { Eggs + "=>3", Meat + "=>4", "thingDefs=InsectJelly=>1", SpecialMeat + "=>1", "thingDefs=Milk=>2" } },
            new { Def = "MealLavish_Veg", Atlas = "DMTR/FineMealAtlas/FineVeg", Fallback = "Things/Item/Meal/FineVeg", FallbackClass = typeof(Graphic_StackCount), Width = 432, Height = 1152, Mappings = new[] { Eggs + "=>3", "thingDefs=InsectJelly=>3", "thingDefs=Milk=>2", Fruit + "=>2", "thingDefs=RawCorn=>1", "thingDefs=RawFungus=>1", "thingDefs=RawPotatoes=>3", "thingDefs=RawRice=>3" } }
        };

        foreach (var item in expected)
        {
            var meal = DefDatabase<ThingDef>.GetNamed(item.Def);
            IntegrationAssert.Equal(
                DmtrGraphicType,
                meal.graphicData.graphicClass?.FullName,
                $"DMTR must remain the finalized {item.Def} graphic owner.");
            IntegrationAssert.Equal(item.Atlas, meal.graphicData.texPath,
                $"DMTR must retain the installed atlas path for {item.Def}.");
            IntegrationAssert.True(
                meal.graphicData.attachments is { Count: 1 } &&
                meal.graphicData.attachments[0].graphicClass == item.FallbackClass,
                $"{item.Def} must retain its one declared attachment fallback class.");
            IntegrationAssert.Equal(item.Fallback, meal.graphicData.attachments![0].texPath,
                $"DMTR must retain the installed fallback texture path for {item.Def}.");

            var extensions = meal.modExtensions?
                .Where(extension => string.Equals(
                    extension.GetType().FullName,
                    DmtrExtensionType,
                    StringComparison.Ordinal))
                .ToArray() ?? Array.Empty<DefModExtension>();
            IntegrationAssert.Equal(1, extensions.Length,
                $"{item.Def} must retain exactly one DMTR atlas extension.");
            IntegrationAssert.Equal(item.Width, ReadInt(extensions[0], "widthPixels"),
                $"DMTR's installed {item.Def} atlas width must remain finalized.");
            IntegrationAssert.Equal(item.Height, ReadInt(extensions[0], "heightPixels"),
                $"DMTR's installed {item.Def} atlas height must remain finalized.");
            IntegrationAssert.Equal(
                string.Join("|", item.Mappings.OrderBy(mapping => mapping, StringComparer.Ordinal)),
                string.Join("|", ReadMappings(extensions[0], "dimensionsMapping")),
                $"DMTR's installed ingredient keys and atlas rows must remain exact for {item.Def}.");
            AssertImmersiveMealState(meal);
        }
    }

    internal static void AssertFtvOwnsVarietyMeals()
    {
        foreach (var defName in new[] { "FTV_MealSimple", "FTV_MealFine", "FTV_MealLavish" })
        {
            var phase = "resolve finalized Def";
            try
            {
                var meal = DefDatabase<ThingDef>.GetNamed(defName);
                phase = "inspect graphic owner";
                IntegrationAssert.NotNull(meal.graphicData,
                    $"{defName} must retain finalized graphic data.");
                IntegrationAssert.Equal(
                    FtvGraphicType,
                    meal.graphicData.graphicClass?.FullName,
                    $"FTV must remain the finalized graphic owner for {defName}.");
                phase = "inspect Def components";
                IntegrationAssert.True(meal.comps is not null,
                    $"{defName} must retain a component list.");
                IntegrationAssert.Equal(
                    1,
                    meal.comps!.Count(properties => string.Equals(
                        properties.compClass?.FullName,
                        FtvCompType,
                        StringComparison.Ordinal)),
                    $"{defName} must retain exactly one FTV alternate-texture comp.");
                AssertImmersiveMealState(meal);
            }
            catch (IntegrationTestAssertionException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new IntegrationTestAssertionException(
                    $"FTV Def '{defName}' phase '{phase}' failed with " +
                    $"{exception.GetType().FullName}: {exception.Message}");
            }
        }
    }

    private static void AssertImmersiveMealState(ThingDef meal)
    {
        IntegrationAssert.True(meal.comps is not null,
            $"{meal.defName} must retain a component list.");
        IntegrationAssert.Equal(
            1,
            meal.comps!.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
            $"{meal.defName} must retain exactly one physical-plate component.");
        IntegrationAssert.Equal(
            1,
            meal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
            $"{meal.defName} must retain exactly one culinary-state component.");
        IntegrationAssert.Equal(
            1,
            meal.comps.Count(properties => properties.compClass == typeof(CompIngredients)),
            $"{meal.defName} must retain exactly one CompIngredients provenance component.");
    }

    private static int ReadInt(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName) ??
                    throw new InvalidOperationException(
                        $"Optional graphic owner changed required field '{fieldName}'.");
        return Convert.ToInt32(field.GetValue(instance));
    }

    private static IReadOnlyList<string> ReadMappings(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName) ??
                    throw new InvalidOperationException(
                        $"Optional graphic owner changed required field '{fieldName}'.");
        if (field.GetValue(instance) is not IDictionary dictionary)
        {
            throw new InvalidOperationException(
                $"Optional graphic owner field '{fieldName}' is not a dictionary.");
        }

        var mappings = new List<string>();
        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key ?? throw new InvalidOperationException(
                $"Optional graphic owner field '{fieldName}' contains a null key.");
            var parts = new List<string>();
            AddDefNames(parts, key, "thingDefs");
            AddDefNames(parts, key, "categories");
            AddDefNames(parts, key, "disallowedThingDefs");
            mappings.Add(string.Join(";", parts) + "=>" + Convert.ToInt32(entry.Value));
        }

        return mappings.OrderBy(mapping => mapping, StringComparer.Ordinal).ToArray();
    }

    private static void AddDefNames(List<string> parts, object key, string fieldName)
    {
        var field = key.GetType().GetField(
                        fieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        $"Optional ingredient key changed required field '{fieldName}'.");
        if (field.GetValue(key) is not IEnumerable values)
        {
            return;
        }

        var names = values.Cast<object>()
            .Select(value => value is Def def ? def.defName : value as string)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (names.Length > 0)
        {
            parts.Add(fieldName + "=" + string.Join(",", names));
        }
    }
}
