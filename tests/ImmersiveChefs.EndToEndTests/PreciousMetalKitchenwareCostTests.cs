using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.precious-metal-kitchenware-costs",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 9_000,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 280)]
public sealed class PreciousMetalKitchenwareCostTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn crafter = null!;
    private Building_WorkTable silverTable = null!;
    private Building_WorkTable steelTable = null!;
    private Building powerSource = null!;
    private RecipeDef recipe = null!;
    private Thing silver = null!;
    private Thing steel = null!;
    private ThingWithComps? silverPlates;
    private ThingWithComps? steelPlates;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);

        crafter = GenerateCrafter();
        GenSpawn.Spawn(crafter, center + (IntVec3.South * 5), map);

        recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MachinePlates");
        powerSource = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("VanometricPowerCell"));
        powerSource.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(powerSource, center, map, Rot4.North);
        silverTable = SpawnMachiningTable(center + (IntVec3.West * 4), ThingDefOf.Silver);
        steelTable = SpawnMachiningTable(center + (IntVec3.East * 4), ThingDefOf.Steel);

        silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = 40;
        GenSpawn.Spawn(silver, center + (IntVec3.West * 6) + IntVec3.South, map);

        steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 4;
        GenSpawn.Spawn(steel, center + (IntVec3.East * 6) + IntVec3.South, map);
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "allow the native power network to initialize",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "both machining tables receive propagated power",
            _ => silverTable.TryGetComp<CompPowerTrader>()?.PowerOn == true &&
                 steelTable.TryGetComp<CompPowerTrader>()?.PowerOn == true,
            new EndToEndDeadline(600, 1_200, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause after native power propagation",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CheckpointStep(
            "finalized small-volume kitchenware recipe contract",
            _ => new Dictionary<string, string>
            {
                ["recipe"] = recipe.defName,
                ["baseUnits"] = recipe.ingredients.Single().GetBaseCount().ToString("0"),
                ["silverSmallVolume"] = ThingDefOf.Silver.smallVolume.ToString(),
                ["goldSmallVolume"] = ThingDefOf.Gold.smallVolume.ToString(),
                ["silverValuePerItem"] = recipe.IngredientValueGetter!
                    .ValuePerUnitOf(ThingDefOf.Silver).ToString("0.0"),
                ["goldValuePerItem"] = recipe.IngredientValueGetter
                    .ValuePerUnitOf(ThingDefOf.Gold).ToString("0.0"),
                ["steelValuePerItem"] = recipe.IngredientValueGetter
                    .ValuePerUnitOf(ThingDefOf.Steel).ToString("0.0"),
                ["silverPhysicalCount"] = silver.stackCount.ToString(),
                ["ordinaryPhysicalCount"] = steel.stackCount.ToString(),
                ["powerSource"] = powerSource.def.defName,
                ["silverTablePowered"] = silverTable.TryGetComp<CompPowerTrader>()!.PowerOn.ToString(),
                ["steelTablePowered"] = steelTable.TryGetComp<CompPowerTrader>()!.PowerOn.ToString()
            });

        foreach (var step in CraftPlates(
                     context,
                     silverTable,
                     silver,
                     "Silver",
                     40,
                     () => silverPlates,
                     TryResolveSilverPlates))
        {
            yield return step;
        }

        foreach (var step in CraftPlates(
                     context,
                     steelTable,
                     steel,
                     "Steel",
                     4,
                     () => steelPlates,
                     TryResolveSteelPlates))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "small-volume and ordinary-volume native bills consume their exact physical stacks",
            _ =>
            {
                EndToEndAssert.True(silver.Destroyed, "The Silver bill must consume all 40 physical Silver.");
                EndToEndAssert.True(steel.Destroyed, "The ordinary-volume bill must consume all 4 Steel.");
                AssertPlateBatch(silverPlates, ThingDefOf.Silver);
                AssertPlateBatch(steelPlates, ThingDefOf.Steel);
                EndToEndAssert.Equal(
                    0.1f,
                    recipe.IngredientValueGetter!.ValuePerUnitOf(ThingDefOf.Gold),
                    "Gold must share Silver's ten-to-one physical-count conversion.");
            });
        yield return new CheckpointStep(
            "native precious-metal cost result",
            _ => new Dictionary<string, string>
            {
                ["silverConsumed"] = silver.Destroyed.ToString(),
                ["silverPlates"] = silverPlates!.ThingID + ":" + silverPlates.LabelCap,
                ["steelConsumed"] = steel.Destroyed.ToString(),
                ["steelPlates"] = steelPlates!.ThingID + ":" + steelPlates.LabelCap
            });
    }

    private IEnumerable<EndToEndStep> CraftPlates(
        IEndToEndContext context,
        Building_WorkTable table,
        Thing input,
        string materialLabel,
        int expectedPhysicalCount,
        Func<ThingWithComps?> getOutput,
        Func<bool> resolveOutput)
    {
        var beforeTargets = new[] { crafter.ThingID, table.ThingID, input.ThingID };
        yield return new SelectionActionStep(
            $"select the {materialLabel} plate input",
            new[] { input.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame the {materialLabel} plate bill fixture",
            beforeTargets,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            $"before the native {materialLabel} plate bill",
            Array.Empty<string>(),
            paddingPixels: 0);

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(crafter.ThingID, table.ThingID);
        var prioritize = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            prioritize.Length,
            $"Expected one enabled native Prioritize option for the {materialLabel} plate bill; observed " +
            string.Join(", ", options.Select(option => $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            $"prioritize the {materialLabel} plate bill through the native float menu",
            crafter.ThingID,
            table.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep(
            $"run the player-ordered {materialLabel} machining work",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            $"the crafter begins the native {materialLabel} plate bill",
            _ => crafter.CurJobDef == JobDefOf.DoBill &&
                 crafter.CurJob?.RecipeDef == recipe &&
                 crafter.CurJob.GetTarget(TargetIndex.A).Thing == table,
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep(
            $"{materialLabel} plate bill in progress",
            new[] { crafter.ThingID, table.ThingID },
            paddingPixels: 220);
        yield return new WaitUntilStep(
            $"the native bill produces four {materialLabel} plates",
            _ => resolveOutput(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            $"pause after {materialLabel} plate crafting",
            paused: true,
            EndToEndGameSpeed.Normal);

        var output = getOutput();
        EndToEndAssert.NotNull(output, $"The {materialLabel} bill must produce a plate batch.");
        yield return new AssertionStep(
            $"the crafted {materialLabel} plate info reports its physical input count",
            _ =>
            {
                AssertPlateBatch(output, input.def);
                var request = StatRequest.For(output);
                var ingredients = output!.def.SpecialDisplayStats(request)
                    .Single(entry => entry.DisplayPriorityWithinCategory == 1102);
                var expected = "ImmersiveChefs_IngredientRequirement".Translate(
                    expectedPhysicalCount,
                    input.def.label).ToString();
                EndToEndAssert.Equal(
                    expected,
                    ingredients.ValueString,
                    $"The native info card must report the physical {materialLabel} stack consumed by its bill.");
            });
        yield return new SelectionActionStep(
            $"select the crafted {materialLabel} plates",
            new[] { output!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame the crafted {materialLabel} plates and machining table",
            new[] { output.ThingID, table.ThingID, crafter.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            $"crafted {materialLabel} plates with player inspector",
            Array.Empty<string>(),
            paddingPixels: 0);
    }

    private Building_WorkTable SpawnMachiningTable(IntVec3 cell, ThingDef ingredientStuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed("TableMachining");
        var table = (Building_WorkTable)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        table.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(table, cell, map, Rot4.North);

        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 10f
        };
        bill.ingredientFilter.SetDisallowAll();
        bill.ingredientFilter.SetAllow(ingredientStuff, true);
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(crafter);
        table.BillStack.AddBill(bill);
        return table;
    }

    private bool TryResolveSilverPlates()
    {
        silverPlates ??= FindPlateBatch(ThingDefOf.Silver);
        return silverPlates is { stackCount: 4 };
    }

    private bool TryResolveSteelPlates()
    {
        steelPlates ??= FindPlateBatch(ThingDefOf.Steel);
        return steelPlates is { stackCount: 4 };
    }

    private ThingWithComps? FindPlateBatch(ThingDef stuff)
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        return map.listerThings.ThingsOfDef(plateDef)
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.Spawned && thing.Stuff == stuff);
    }

    private static void AssertPlateBatch(ThingWithComps? plates, ThingDef stuff)
    {
        EndToEndAssert.NotNull(plates, $"The native bill must produce {stuff.label} plates.");
        EndToEndAssert.Equal(stuff, plates!.Stuff, "The plate batch must retain its exact input Stuff.");
        EndToEndAssert.Equal(4, plates.stackCount, "One native plate bill must produce four plates.");
        EndToEndAssert.NotNull(plates.GetComp<CompQuality>(), "Native machining must assign plate quality.");
    }

    private static Pawn GenerateCrafter()
    {
        var smithing = DefDatabase<WorkTypeDef>.GetNamed("Smithing");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(smithing) ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.9f ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) < 0.9f)
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            HumanlikePawnFixture.SetName(pawn, "Lina");
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(smithing, 1);
            pawn.skills.GetSkill(SkillDefOf.Crafting).Level = 12;
            if (pawn.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable kitchenware smith.");
    }

    private static IntVec3 FindFixtureCenter(Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (CellRect.CenteredOn(candidate, 14).Cells.All(cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !map.roofGrid.Roofed(cell) &&
                    cell.GetThingList(map).Count == 0))
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear precious-metal machining fixture area.");
    }
}
