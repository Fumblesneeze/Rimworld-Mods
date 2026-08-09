using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.AbsPolymer.InGame.IntegrationTests;

public static class AbsPolymerFinalizedMaterialTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactAbsGroupKeepsPlasticOutOfPrimitiveStockAndInModernTableware()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                mod.PackageId,
                "Mlie.SimplySublimeABSPolymer",
                StringComparison.OrdinalIgnoreCase)),
            "ABS Polymer must be an actually active ModContentPack for this exact group.");

        var abs = DefDatabase<ThingDef>.GetNamed("ABSPolymer");
        IntegrationAssert.True(
            abs.stuffProps?.categories?.Any(category =>
                category.defName.Equals("Stony", StringComparison.OrdinalIgnoreCase)) == true,
            "The inspected ABS Def must retain the overlapping Stony metadata this test guards.");

        var primitiveTrader = DefDatabase<TraderKindDef>
            .GetNamed("Caravan_Neolithic_BulkGoods")
            .stockGenerators
            .OfType<StockGenerator_Kitchenware>()
            .Single(generator => generator.thingDef.defName == "ImmersiveChefs_PrimitiveCookware");
        var bulkTrader = DefDatabase<TraderKindDef>
            .GetNamed("Caravan_Outlander_BulkGoods")
            .stockGenerators
            .OfType<StockGenerator_Kitchenware>()
            .ToArray();

        IntegrationAssert.True(
            !primitiveTrader.EligibleStuffs().Contains(abs),
            "Explicit ABS plastic registration must win over broad Stony metadata for primitive trader stock.");
        foreach (var product in new[] { KitchenwareProduct.Plate, KitchenwareProduct.Cutlery })
        {
            var generator = bulkTrader.Single(candidate => candidate.product == product);
            IntegrationAssert.True(
                generator.EligibleStuffs().Contains(abs),
                $"ABS must remain eligible modern {product} Stuff for outlander bulk stock.");
        }

        IntegrationAssert.True(
            !DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware")
                .ingredients[0].filter.Allows(abs),
            "The finalized primitive cookware bill must reject ABS.");
        IntegrationAssert.True(
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MachinePlates")
                .ingredients[0].filter.Allows(abs) &&
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MachineCutlery")
                .ingredients[0].filter.Allows(abs),
            "The finalized modern tableware bills must accept ABS.");
    }
}
