using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.expanded-masonry-adobe-plates",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "argon.corelib",
    "oskarpotocki.vanillafactionsexpanded.core",
    "argon.expandedmaterials.masonry",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_000,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class ExpandedMasonryAdobePlateTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn crafter = null!;
    private Building_WorkTable craftingSpot = null!;
    private RecipeDef recipe = null!;
    private Thing adobeBricks = null!;
    private ThingWithComps? plates;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);

        crafter = GenerateCrafter();
        GenSpawn.Spawn(crafter, center + (IntVec3.South * 3), map);

        var spotDef = DefDatabase<ThingDef>.GetNamed("CraftingSpot");
        craftingSpot = (Building_WorkTable)ThingMaker.MakeThing(spotDef);
        craftingSpot.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(craftingSpot, center, map, Rot4.North);

        recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeAdobePlates");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(crafter);
        craftingSpot.BillStack.AddBill(bill);

        adobeBricks = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("EM_AdobeBricks"));
        adobeBricks.stackCount = 4;
        GenSpawn.Spawn(adobeBricks, center + (IntVec3.West * 2), map);
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "package-ID-gated Expanded Masonry adobe plate contract",
            _ => new Dictionary<string, string>
            {
                ["package"] = LoadedModManager.RunningModsListForReading
                    .Single(mod => string.Equals(
                        mod.PackageId,
                        "argon.expandedmaterials.masonry",
                        StringComparison.OrdinalIgnoreCase))
                    .PackageId,
                ["recipe"] = recipe.defName,
                ["bench"] = craftingSpot.def.defName,
                ["input"] = adobeBricks.def.defName + ":" + adobeBricks.stackCount,
                ["output"] = recipe.products.Single().thingDef.defName + ":" + recipe.products.Single().count
            });

        var fixtureThings = new[] { crafter.ThingID, craftingSpot.ThingID, adobeBricks.ThingID };
        yield return new SelectionActionStep(
            "select the adobe crafting fixture",
            fixtureThings,
            additive: false);
        yield return new CameraActionStep(
            "frame the crafting spot and adobe bricks",
            fixtureThings,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "before the native adobe plate bill",
            fixtureThings,
            paddingPixels: 220);

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(crafter.ThingID, craftingSpot.ThingID);
        var prioritize = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            prioritize.Length,
            "Expected one enabled native Prioritize option for the adobe plate bill; observed " +
            string.Join(", ", options.Select(option => $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "prioritize the adobe plate bill through the native float menu",
            crafter.ThingID,
            craftingSpot.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep(
            "run the player-ordered adobe work",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the crafter begins the native adobe plate bill",
            _ => crafter.CurJobDef == JobDefOf.DoBill && crafter.CurJob?.RecipeDef == recipe,
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep(
            "adobe plate bill in progress",
            new[] { crafter.ThingID, craftingSpot.ThingID },
            paddingPixels: 220);
        yield return new WaitUntilStep(
            "the native bill produces four adobe plates",
            _ => TryResolveAdobePlates(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after adobe plate crafting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "ordinary crafting consumes four bricks and produces fixed-material adobe plates",
            _ =>
            {
                EndToEndAssert.True(adobeBricks.Destroyed, "The exact four-unit adobe-brick input must be consumed.");
                EndToEndAssert.NotNull(plates, "The ordinary bill must produce adobe plates.");
                EndToEndAssert.True(plates!.Stuff is null, "Adobe plates are fixed-material products, not invented Stuff.");
                EndToEndAssert.Equal(4, plates.stackCount, "One native bill must produce four plates.");
                EndToEndAssert.True(plates.GetComp<CompQuality>() is null, "Crafted plates have no quality grade.");
                EndToEndAssert.Equal(
                    5f,
                    plates.GetComp<CompKitchenwareStats>().CurrentStats.MaterialCleanliness,
                    "The crafted plates must use the fixed adobe material profile.");
            });
        yield return new SelectionActionStep(
            "select the crafted adobe plates",
            new[] { plates!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the crafted adobe plates and crafting spot",
            new[] { plates.ThingID, craftingSpot.ThingID, crafter.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "crafted adobe plates with player inspector",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "native Expanded Masonry adobe plate result",
            _ => new Dictionary<string, string>
            {
                ["plates"] = plates.ThingID + ":" + plates.LabelCap,
                ["stuff"] = plates.Stuff?.defName ?? "fixed-adobe",
                ["count"] = plates.stackCount.ToString(),
                ["quality"] = plates.GetComp<CompQuality>()?.Quality.ToString() ?? "missing",
                ["cleanliness"] = plates.GetComp<CompKitchenwareStats>()
                    .CurrentStats.MaterialCleanliness.ToString("0")
            });
    }

    private bool TryResolveAdobePlates()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_AdobePlate");
        plates ??= map.listerThings.ThingsOfDef(plateDef)
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.Spawned);
        return plates is { stackCount: 4 };
    }

    private static Pawn GenerateCrafter()
    {
        var crafting = WorkTypeDefOf.Crafting;
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(crafting) ||
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

            pawn.workSettings.SetPriority(crafting, 1);
            pawn.skills.GetSkill(SkillDefOf.Crafting).Level = 12;
            if (pawn.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable adobe crafter.");
    }

    private static IntVec3 FindFixtureCenter(Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (CellRect.CenteredOn(candidate, 10).Cells.All(cell =>
                    cell.InBounds(map) &&
                    cell.Standable(map) &&
                    !map.roofGrid.Roofed(cell) &&
                    cell.GetThingList(map).Count == 0))
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear adobe crafting fixture area.");
    }
}
