using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.ceramics-continued-porcelain-plates",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "zal.ceramics",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_000,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class CeramicsContinuedPorcelainPlateTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn crafter = null!;
    private Building_WorkTable bench = null!;
    private RecipeDef recipe = null!;
    private Thing porcelain = null!;
    private ThingWithComps? plates;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindFixtureCenter(map);
        FinishResearchForFixture(context, "PrimitiveCeramics");
        FinishResearchForFixture(context, "BasicCeramics");

        crafter = GenerateCrafter();
        GenSpawn.Spawn(crafter, center + (IntVec3.South * 3), map);

        var benchDef = DefDatabase<ThingDef>.GetNamed("CeramicsBench_Basic");
        bench = (Building_WorkTable)ThingMaker.MakeThing(
            benchDef,
            benchDef.MadeFromStuff ? ThingDefOf.WoodLog : null);
        bench.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bench, center, map, Rot4.North);

        recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePorcelainPlates");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(crafter);
        bench.BillStack.AddBill(bill);

        porcelain = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("N7_Porcelain"));
        porcelain.stackCount = 4;
        GenSpawn.Spawn(porcelain, center + (IntVec3.West * 2), map);
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep(
            "finalized Ceramics porcelain plate contract",
            _ => new Dictionary<string, string>
            {
                ["package"] = LoadedModManager.RunningModsListForReading
                    .Single(mod => string.Equals(mod.PackageId, "zal.ceramics", StringComparison.OrdinalIgnoreCase))
                    .PackageId,
                ["recipe"] = recipe.defName,
                ["bench"] = bench.def.defName,
                ["input"] = porcelain.def.defName + ":" + porcelain.stackCount,
                ["output"] = recipe.products.Single().thingDef.defName + ":" + recipe.products.Single().count,
                ["research"] = recipe.researchPrerequisite?.defName ?? "missing"
            });

        var fixtureThings = new[] { crafter.ThingID, bench.ThingID, porcelain.ThingID };
        yield return new SelectionActionStep(
            "select the ceramics crafting fixture",
            fixtureThings,
            additive: false);
        yield return new CameraActionStep(
            "frame the ceramics bench and porcelain",
            fixtureThings,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "before the native porcelain plate bill",
            fixtureThings,
            paddingPixels: 220);

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(crafter.ThingID, bench.ThingID);
        var prioritize = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            prioritize.Length,
            "Expected one enabled native Prioritize option for the porcelain plate bill; observed " +
            string.Join(", ", options.Select(option => $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "prioritize the porcelain plate bill through the native float menu",
            crafter.ThingID,
            bench.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep(
            "run the player-ordered ceramics work",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the crafter begins the native porcelain plate bill",
            _ => crafter.CurJobDef == JobDefOf.DoBill && crafter.CurJob?.RecipeDef == recipe,
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep(
            "porcelain plate bill in progress",
            new[] { crafter.ThingID, bench.ThingID },
            paddingPixels: 220);
        yield return new WaitUntilStep(
            "the native bill produces four porcelain plates",
            _ => TryResolvePorcelainPlates(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after porcelain plate crafting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "ordinary ceramics work consumes four porcelain and preserves it as plate Stuff",
            _ =>
            {
                EndToEndAssert.True(porcelain.Destroyed, "The exact four-unit porcelain input must be consumed.");
                EndToEndAssert.NotNull(plates, "The ordinary bill must produce porcelain plates.");
                EndToEndAssert.Equal("N7_Porcelain", plates!.Stuff?.defName, "The product must retain porcelain as Stuff.");
                EndToEndAssert.Equal(4, plates.stackCount, "One native bill must produce four plates.");
                EndToEndAssert.Equal(
                    plates.Stuff!.stuffProps.color,
                    plates.DrawColor,
                    "The rendered plate color must come from the exact porcelain Stuff mask.");
                EndToEndAssert.NotNull(plates.GetComp<CompQuality>(), "Native crafting must assign plate quality.");
                EndToEndAssert.Equal(
                    75f,
                    plates.GetComp<CompKitchenwareStats>().CurrentStats.MaterialCleanliness,
                    "The crafted plates must use the ceramic profile rather than primitive stone.");
                EndToEndAssert.False(
                    DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitivePlates")
                        .ingredients.Single().filter.Allows(DefDatabase<ThingDef>.GetNamed("N7_Porcelain")),
                    "Porcelain must remain unavailable to the primitive stone recipe.");
            });
        yield return new SelectionActionStep(
            "select the crafted porcelain plates",
            new[] { plates!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the crafted porcelain plates and ceramics bench",
            new[] { plates.ThingID, bench.ThingID, crafter.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "crafted porcelain plates with player inspector",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "native Ceramics porcelain plate result",
            _ => new Dictionary<string, string>
            {
                ["plates"] = plates.ThingID + ":" + plates.LabelCap,
                ["stuff"] = plates.Stuff?.defName ?? "missing",
                ["stuffColor"] = plates.Stuff?.stuffProps.color.ToString() ?? "missing",
                ["drawColor"] = plates.DrawColor.ToString(),
                ["count"] = plates.stackCount.ToString(),
                ["quality"] = plates.GetComp<CompQuality>()?.Quality.ToString() ?? "missing",
                ["cleanliness"] = plates.GetComp<CompKitchenwareStats>().CurrentStats.MaterialCleanliness.ToString("0")
            });
    }

    private bool TryResolvePorcelainPlates()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        plates ??= map.listerThings.ThingsOfDef(plateDef)
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => thing.Spawned && thing.Stuff?.defName == "N7_Porcelain");
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

        throw new EndToEndAssertionException("Could not generate a capable ceramics crafter.");
    }

    private static void FinishResearchForFixture(IEndToEndContext context, string defName)
    {
        var project = DefDatabase<ResearchProjectDef>.GetNamed(defName);
        var progressField = typeof(ResearchManager).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException("Could not resolve RimWorld's research progress store.");
        var progress = (Dictionary<ResearchProjectDef, float>)progressField.GetValue(Find.ResearchManager);
        var hadProgress = progress.TryGetValue(project, out var originalProgress);
        context.DeferCleanup(() =>
        {
            if (hadProgress)
            {
                progress[project] = originalProgress;
            }
            else
            {
                progress.Remove(project);
            }
        });
        if (!project.IsFinished)
        {
            Find.ResearchManager.FinishProject(
                project,
                doCompletionDialog: false,
                researcher: null,
                doCompletionLetter: false);
        }
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

        throw new EndToEndAssertionException("Could not find a clear porcelain crafting fixture area.");
    }
}
