using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.countertop-microwave-placement",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class CountertopMicrowavePlacementTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> supports = new();
    private Map map = null!;
    private ThingDef microwaveDef = null!;
    private Building table = null!;
    private Building workbench = null!;
    private Building shelf = null!;
    private Building bed = null!;
    private Thing blueprint = null!;
    private Pawn builder = null!;
    private IntVec3 tableTarget;
    private IntVec3 workbenchTarget;
    private IntVec3 shelfTarget;
    private IntVec3 bedTarget;
    private IntVec3 blueprintTarget;
    private IntVec3 floorTarget;
    private EndToEndGizmoOption buildMicrowave = null!;
    private EndToEndGizmoOption deconstruct = null!;
    private Building_Microwave? tableMicrowave;
    private Building_Microwave? workbenchMicrowave;
    private MinifiedThing? recoveredMicrowave;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        microwaveDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        var originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() => DebugSettings.godMode = originalGodMode);

        var electricity = DefDatabase<ResearchProjectDef>.GetNamed("Electricity");
        var research = Find.ResearchManager;
        var progressField = typeof(ResearchManager).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException("Could not resolve RimWorld's research progress store.");
        var progress = (Dictionary<ResearchProjectDef, float>)progressField.GetValue(research);
        var hadProgress = progress.TryGetValue(electricity, out var originalProgress);
        if (!electricity.IsFinished)
        {
            research.FinishProject(
                electricity,
                doCompletionDialog: false,
                researcher: null,
                doCompletionLetter: false);
            context.DeferCleanup(() =>
            {
                if (hadProgress)
                {
                    progress[electricity] = originalProgress;
                }
                else
                {
                    progress.Remove(electricity);
                }
            });
        }

        var fixtureCells = FindSeparatedClearCells(map, 6);
        table = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            ThingDefOf.Steel);
        workbench = (Building)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TableMachining"));
        shelf = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Shelf"),
            ThingDefOf.Steel);
        bed = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Bed"),
            ThingDefOf.Steel);
        supports.Add(table);
        supports.Add(workbench);
        supports.Add(shelf);
        supports.Add(bed);
        GenSpawn.Spawn(table, fixtureCells[0], map, Rot4.North);
        GenSpawn.Spawn(workbench, fixtureCells[1], map, Rot4.North);
        GenSpawn.Spawn(shelf, fixtureCells[2], map, Rot4.North);
        GenSpawn.Spawn(bed, fixtureCells[3], map, Rot4.North);
        table.SetFaction(Faction.OfPlayer);
        workbench.SetFaction(Faction.OfPlayer);
        shelf.SetFaction(Faction.OfPlayer);
        bed.SetFaction(Faction.OfPlayer);
        blueprint = GenConstruct.PlaceBlueprintForBuild(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            fixtureCells[4],
            map,
            Rot4.North,
            Faction.OfPlayer,
            ThingDefOf.Steel);
        supports.Add(blueprint);

        builder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        builder.Name = new NameSingle("Countertop Appliance Builder");
        builder.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            builder.workSettings.SetPriority(workType, 0);
        }

        builder.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
        GenSpawn.Spawn(
            builder,
            table.OccupiedRect().ExpandedBy(2).EdgeCells
                .First(cell => cell.InBounds(map) && cell.Standable(map) && cell.GetThingList(map).Count == 0),
            map);

        tableTarget = SouthernmostCell(table.OccupiedRect());
        workbenchTarget = SouthernmostCell(workbench.OccupiedRect(), preferWest: true);
        shelfTarget = shelf.Position;
        bedTarget = bed.Position;
        blueprintTarget = blueprint.Position;
        floorTarget = fixtureCells[5];

        buildMicrowave = SingleEnabled(
            catalog.Query(Array.Empty<string>(), new[] { "Production" })
                .Where(option => string.Equals(
                    option.BuildableDefName,
                    microwaveDef.defName,
                    StringComparison.Ordinal)),
            "the finalized microwave build designator");
        EndToEndAssert.Equal(
            EndToEndGizmoInteraction.Place,
            buildMicrowave.Interaction,
            "The one-cell microwave designator must expose native placement.");

        deconstruct = SingleEnabled(
            catalog.Query(new[] { table.ThingID }, Array.Empty<string>())
                .Where(option => option.Interaction == EndToEndGizmoInteraction.Invoke &&
                                 option.Label.IndexOf(
                                     "deconstruct",
                                     StringComparison.OrdinalIgnoreCase) >= 0),
            "the selected table's native deconstruct command");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select countertop placement supports",
            supports.Select(thing => thing.ThingID),
            additive: false);
        yield return new CameraActionStep(
            "frame countertop placement supports",
            supports.Select(thing => thing.ThingID),
            paddingPixels: 140);
        yield return new ScreenshotStep(
            "before native countertop placement",
            supports.Select(thing => thing.ThingID),
            paddingPixels: 140);

        yield return BuildStep("place microwave on dining table", tableTarget);
        yield return new WaitUntilStep(
            "table microwave materializes through native construction",
            _ => (tableMicrowave = MicrowaveAt(tableTarget)) is not null,
            new EndToEndDeadline(300, 500, TimeSpan.FromSeconds(20)));
        yield return BuildStep("place microwave on machining workbench", workbenchTarget);
        yield return new WaitUntilStep(
            "workbench microwave materializes through native construction",
            _ => (workbenchMicrowave = MicrowaveAt(workbenchTarget)) is not null,
            new EndToEndDeadline(300, 500, TimeSpan.FromSeconds(20)));

        yield return BuildStep("reject microwave on storage shelf", shelfTarget, expectRejected: true);
        yield return BuildStep("reject microwave on bed", bedTarget, expectRejected: true);
        yield return BuildStep("reject microwave on unfinished blueprint", blueprintTarget, expectRejected: true);
        yield return BuildStep("reject microwave on bare floor", floorTarget, expectRejected: true);
        yield return new AssertionStep(
            "native placement preserves usable supports and creates no rejected appliance",
            _ =>
            {
                EndToEndAssert.True(table.Spawned && workbench.Spawned && shelf.Spawned && bed.Spawned && blueprint.Spawned,
                    "Countertop placement must not replace or destroy its supports.");
                EndToEndAssert.Equal(2, SpawnedMicrowaves().Count,
                    "Only the two accepted native placements may create microwaves.");
                EndToEndAssert.True(MicrowaveAt(shelfTarget) is null,
                    "The rejected shelf target must remain microwave-free.");
                EndToEndAssert.True(MicrowaveAt(bedTarget) is null,
                    "The rejected bed target must remain microwave-free.");
                EndToEndAssert.True(MicrowaveAt(blueprintTarget) is null,
                    "The rejected blueprint target must remain microwave-free.");
                EndToEndAssert.True(MicrowaveAt(floorTarget) is null,
                    "The rejected floor target must remain microwave-free.");
                EndToEndAssert.True(
                    ReferenceEquals(
                        table,
                        MicrowaveSupportRuntime.FindAt(tableTarget, map, tableMicrowave)),
                    "The table microwave must retain the exact live table support.");
                EndToEndAssert.True(
                    ReferenceEquals(
                        workbench,
                        MicrowaveSupportRuntime.FindAt(workbenchTarget, map, workbenchMicrowave)),
                    "The workbench microwave must retain the exact live workbench support.");
                EndToEndAssert.True(
                    workbench.InteractionCell.InBounds(map) &&
                    workbench.InteractionCell.Standable(map) &&
                    MicrowaveAt(workbench.InteractionCell) is null,
                    "The countertop appliance must leave the machining table's interaction cell usable.");
            });
        yield return new SelectionActionStep(
            "select dining table beneath accepted microwave",
            new[] { table.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "dining table remains selectable beneath countertop appliance",
            new[] { table.ThingID, tableMicrowave!.ThingID },
            paddingPixels: 120);
        yield return new SelectionActionStep(
            "select machining table beneath accepted microwave",
            new[] { workbench.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "machining table remains selectable beneath countertop appliance",
            new[] { workbench.ThingID, workbenchMicrowave!.ThingID },
            paddingPixels: 120);
        yield return new SelectionActionStep(
            "select accepted countertop microwaves",
            new[] { tableMicrowave!.ThingID, workbenchMicrowave!.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "accepted countertop appliances remain on their supports",
            new[] { table.ThingID, workbench.ThingID, tableMicrowave.ThingID, workbenchMicrowave.ThingID },
            paddingPixels: 120);

        yield return DeconstructStep();
        yield return new TimeControlActionStep(
            "run rare support validation",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "unsupported microwave becomes one recoverable minified appliance",
            _ =>
            {
                recoveredMicrowave = map.listerThings.AllThings
                    .OfType<MinifiedThing>()
                    .SingleOrDefault(candidate => ReferenceEquals(candidate.InnerThing, tableMicrowave));
                return recoveredMicrowave is not null;
            },
            new EndToEndDeadline(1_800, 2_500, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after support recovery",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "support loss conserves exactly one appliance without floating or duplication",
            _ =>
            {
                EndToEndAssert.True(table.Destroyed,
                    "The native deconstruct action must remove the supporting dining table.");
                EndToEndAssert.True(tableMicrowave is { Spawned: false },
                    "The unsupported microwave must no longer float as a spawned building.");
                EndToEndAssert.Equal(1, map.listerThings.AllThings.OfType<MinifiedThing>()
                    .Count(candidate => ReferenceEquals(candidate.InnerThing, tableMicrowave)),
                    "Exactly one minified wrapper must retain the original microwave identity.");
                EndToEndAssert.True(workbench.Spawned && workbenchMicrowave is { Spawned: true },
                    "Removing one support must not affect another supported appliance.");
            });
        yield return new SelectionActionStep(
            "select recovered and still-supported microwaves",
            new[] { recoveredMicrowave!.ThingID, workbenchMicrowave!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame recovered and supported microwaves",
            new[] { recoveredMicrowave.ThingID, workbenchMicrowave.ThingID },
            paddingPixels: 140);
        yield return new ScreenshotStep(
            "after native support removal and microwave recovery",
            new[] { recoveredMicrowave.ThingID, workbench.ThingID, workbenchMicrowave.ThingID },
            paddingPixels: 120);
        yield return new CheckpointStep(
            "countertop microwave placement result",
            _ => new Dictionary<string, string>
            {
                ["acceptedMicrowaves"] = SpawnedMicrowaves().Count.ToString(),
                ["recoveredThingId"] = recoveredMicrowave.ThingID,
                ["tableDestroyed"] = table.Destroyed.ToString(),
                ["workbenchStillSpawned"] = workbench.Spawned.ToString()
            });
    }

    private GizmoActionStep BuildStep(string name, IntVec3 cell, bool expectRejected = false) =>
        new(
            name,
            Array.Empty<string>(),
            buildMicrowave.RuntimeType,
            EndToEndGizmoInteraction.Place,
            stableGizmoId: buildMicrowave.StableId,
            startCell: new EndToEndMapCell(cell.x, cell.z),
            architectCategoryDefNames: new[] { "Production" },
            expectRejected: expectRejected);

    private GizmoActionStep DeconstructStep() =>
        new(
            "deconstruct dining table support through native order",
            new[] { table.ThingID },
            deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: deconstruct.StableId,
            architectCategoryDefNames: Array.Empty<string>());

    private Building_Microwave? MicrowaveAt(IntVec3 cell) =>
        cell.GetThingList(map).OfType<Building_Microwave>().SingleOrDefault();

    private List<Building_Microwave> SpawnedMicrowaves() =>
        map.listerThings.ThingsOfDef(microwaveDef).OfType<Building_Microwave>().ToList();

    private static EndToEndGizmoOption SingleEnabled(
        IEnumerable<EndToEndGizmoOption> candidates,
        string description)
    {
        var matches = candidates.Where(candidate => !candidate.Disabled).ToArray();
        EndToEndAssert.Equal(1, matches.Length, $"Expected one enabled {description}");
        return matches[0];
    }

    private static IntVec3 SouthernmostCell(CellRect rect, bool preferWest = false) =>
        rect.Cells
            .OrderBy(cell => cell.z)
            .ThenBy(cell => preferWest ? cell.x : -cell.x)
            .First();

    private static IntVec3[] FindSeparatedClearCells(Map map, int count)
    {
        var result = new List<IntVec3>();
        for (var x = -72; x <= 72; x += 18)
        {
            for (var z = -72; z <= 72; z += 18)
            {
                var cell = map.Center + new IntVec3(x, 0, z);
                if (result.Any(existing => existing.DistanceToSquared(cell) <= 100) ||
                    !SquareIsClear(map, cell, 4))
                {
                    continue;
                }

                result.Add(cell);
                if (result.Count == count)
                {
                    return result.ToArray();
                }
            }
        }

        throw new EndToEndAssertionException($"Could not find {count} separated countertop fixture areas.");
    }

    private static bool SquareIsClear(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Standable(map) ||
                    cell.GetThingList(map).Count != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
