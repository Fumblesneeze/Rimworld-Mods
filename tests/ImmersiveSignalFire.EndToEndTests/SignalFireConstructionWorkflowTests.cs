using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImmersiveSignalFire.Buildings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveSignalFire.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-signal-fire.native-construction",
    "fumblesneeze.immersivesignalfire",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "blues.forge",
    "fumblesneeze.immersivesignalfire",
    MaxFrames = 8_000,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 300)]
public sealed class SignalFireConstructionWorkflowTests : IRimWorldEndToEndTest
{
    private readonly List<string> fixtureIds = new();
    private readonly HashSet<string> baselineSignalPhaseIds = new();
    private Map map = null!;
    private ThingDef fireDef = null!;
    private ResearchProjectDef research = null!;
    private Pawn builder = null!;
    private IntVec3 buildCell;
    private EndToEndGizmoOption build = null!;
    private Blueprint? blueprint;
    private Frame? frame;
    private Building_SignalFire? fire;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new EndToEndAssertionException("A playable map is required.");
        fireDef = DefDatabase<ThingDef>.GetNamed("ImmersiveSignalFire_SignalFire");
        baselineSignalPhaseIds.UnionWith(map.listerThings.AllThings
            .Where(IsSignalFirePhase)
            .Select(thing => thing.ThingID));
        research = DefDatabase<ResearchProjectDef>.GetNamed("ImmersiveSignalFire_SmokeSignals");
        buildCell = GenRadial.RadialCellsAround(map.Center, 40f, useCenter: true)
            .Take(2_048)
            .Where(cell =>
                GenAdj.OccupiedRect(cell, Rot4.North, fireDef.Size).Cells.All(candidate =>
                    candidate.InBounds(map) &&
                    (fireDef.terrainAffordanceNeeded is null ||
                     candidate.GetTerrain(map).affordances.Contains(fireDef.terrainAffordanceNeeded))) &&
                CellRect.CenteredOn(cell, 2).ExpandedBy(4).Cells.All(candidate =>
                    candidate.InBounds(map) && candidate.Standable(map) && !candidate.Fogged(map) &&
                    candidate.GetThingList(map).Count == 0))
            .First();

        var manager = Find.ResearchManager;
        FieldInfo progressField = typeof(ResearchManager).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException("Could not resolve RimWorld's research progress store.");
        var progress = (Dictionary<ResearchProjectDef, float>)progressField.GetValue(manager);
        bool hadProgress = progress.TryGetValue(research, out float originalProgress);
        progress[research] = 0f;
        context.DeferCleanup(() =>
        {
            if (hadProgress)
            {
                progress[research] = originalProgress;
            }
            else
            {
                progress.Remove(research);
            }
        });

        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        EndToEndAssert.False(research.IsFinished,
            "The bounded precondition must leave smoke-signalling research incomplete.");
        EndToEndAssert.False(catalog.Query(Array.Empty<string>(), new[] { "Misc" }).Any(option =>
                !option.Disabled && option.BuildableDefName == fireDef.defName),
            "The native Misc architect catalog must not expose an enabled signal fire before research.");

        manager.FinishProject(research, doCompletionDialog: false, researcher: null, doCompletionLetter: false);

        build = catalog.Query(Array.Empty<string>(), new[] { "Misc" })
            .Single(option => !option.Disabled &&
                              option.BuildableDefName == fireDef.defName &&
                              option.Interaction == EndToEndGizmoInteraction.Place);

        builder = GenerateBuilder();
        GenSpawn.Spawn(builder, buildCell + new IntVec3(0, 0, -4), map);
        fixtureIds.Add(builder.ThingID);
        SpawnStack(ThingDefOf.WoodLog, 60, buildCell + new IntVec3(-3, 0, -2));

        context.DeferCleanup(() =>
        {
            Map cleanupMap = Find.CurrentMap ?? map;
            foreach (Thing thing in cleanupMap.listerThings.AllThings.Where(thing =>
                         fixtureIds.Contains(thing.ThingID) ||
                         (IsSignalFirePhase(thing) && !baselineSignalPhaseIds.Contains(thing.ThingID))).ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep(
            "frame the researched native signal-fire construction site",
            fixtureIds,
            220);
        yield return new ScreenshotStep(
            "materials and capable builder before native signal-fire designation",
            fixtureIds,
            180);
        yield return new GizmoActionStep(
            "place the researched signal fire through the native Misc designator",
            Array.Empty<string>(),
            build.RuntimeType,
            EndToEndGizmoInteraction.Place,
            stableGizmoId: build.StableId,
            startCell: new EndToEndMapCell(buildCell.x, buildCell.z),
            architectCategoryDefNames: new[] { "Misc" });
        yield return new WaitUntilStep(
            "native designation creates a signal-fire blueprint",
            _ => (blueprint = FindPhase<Blueprint>()) is not null,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(20)));
        yield return new ScreenshotStep(
            "native signal-fire blueprint before hauling and construction",
            new[] { blueprint!.ThingID },
            180);

        EndToEndFloatMenuOption[] prioritize = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(builder.ThingID, blueprint.ThingID)
            .Where(option => !option.Disabled &&
                             option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, prioritize.Length,
            "The capable builder must expose one enabled native prioritize-construction order.");
        yield return new FloatMenuActionStep(
            "prioritize the signal-fire blueprint through the native pawn menu",
            builder.ThingID,
            blueprint.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep(
            "run ordinary hauling and signal-fire construction",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the blueprint advances to a frame or completed signal fire",
            _ => (frame = FindPhase<Frame>()) is not null ||
                 (fire = FindPhase<Building_SignalFire>()) is not null,
            new EndToEndDeadline(2_400, 6_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after the first observable native construction result",
            paused: true,
            EndToEndGameSpeed.Normal);
        frame = FindPhase<Frame>();
        fire = FindPhase<Building_SignalFire>();
        EndToEndAssert.True(frame is not null || fire is not null,
            "Pausing the native build must retain either its ordinary frame or its completed signal fire.");
        if (frame is not null && frame.Spawned && !frame.Destroyed)
        {
            yield return new ScreenshotStep(
                "ordinary signal-fire frame during native construction",
                new[] { frame.ThingID },
                180);
            yield return new TimeControlActionStep(
                "resume ordinary signal-fire construction",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "the builder completes the signal fire through ordinary Construction work",
                _ => (fire = FindPhase<Building_SignalFire>()) is not null,
                new EndToEndDeadline(3_600, 10_000, TimeSpan.FromSeconds(100)));
        }
        yield return new TimeControlActionStep(
            "pause after native signal-fire construction",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep("the completed custom hearth starts cold", _ =>
        {
            EndToEndAssert.False(fire!.SignalComp.IsBusy,
                "A newly built signal hearth must not begin an active fire session.");
            EndToEndAssert.False(fire.AllComps.Any(comp => comp is CompGlower or CompRefuelable or CompHeatPusher),
                "The completed hearth must have no permanent light, fuel, or heat component.");
        });
        yield return new ScreenshotStep(
            "player-built custom signal hearth visibly cold after construction",
            new[] { fire!.ThingID },
            220);
        yield return new CheckpointStep("complete native signal-fire construction workflow", _ =>
            new Dictionary<string, string>
            {
                ["researchDef"] = research.defName,
                ["researchTechLevel"] = research.techLevel.ToString(),
                ["buildingThingId"] = fire!.ThingID,
                ["stuffDef"] = fire.Stuff?.defName ?? "none",
                ["startsCold"] = (!fire.SignalComp.IsBusy).ToString(),
            });
    }

    private void SpawnStack(ThingDef def, int count, IntVec3 cell)
    {
        Thing stack = ThingMaker.MakeThing(def);
        stack.stackCount = count;
        GenSpawn.Spawn(stack, cell, map);
        fixtureIds.Add(stack.ThingID);
    }

    private T? FindPhase<T>() where T : Thing => buildCell.GetThingList(map)
        .OfType<T>()
        .SingleOrDefault(thing => thing.def.entityDefToBuild == fireDef || thing.def == fireDef);

    private bool IsSignalFirePhase(Thing thing) =>
        thing.def == fireDef || thing.def.entityDefToBuild == fireDef;

    private static Pawn GenerateBuilder()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Pawn candidate = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (candidate.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
            {
                candidate.Destroy(DestroyMode.Vanish);
                continue;
            }

            candidate.workSettings.EnableAndInitialize();
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!candidate.WorkTypeIsDisabled(workType))
                {
                    candidate.workSettings.SetPriority(workType, 0);
                }
            }

            candidate.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            candidate.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            if (candidate.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            if (candidate.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return candidate;
        }

        throw new EndToEndAssertionException("Could not generate a capable signal-fire builder.");
    }
}
