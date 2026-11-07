using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace PathfindingLab.EndToEndTests;

// Ordinary-job routing acceptance. This small fixture is NOT a megabase benchmark.
[RimWorldEndToEndTest("pathfinding-lab.colony-timing-verification", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 24000, MaxGameTicks = 20000, MaxWallClockSeconds = 240)]
public sealed class ColonyTimingVerification : IRimWorldEndToEndTest
{
    private Map map = null!;
    private readonly List<Pawn> pawns = new();
    private readonly List<Thing> items = new();
    private readonly Dictionary<string, Thing> carriedBy = new();
    private Zone_Stockpile pile = null!;
    private SearchTimingProbe probe = null!;
    private Thing door = null!;
    private readonly Dictionary<string, List<IntVec3>> doorwayTrace = new();
    private static ColonyTimingVerification? observing;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        bool mode = LabMod.Settings.Experimental;
        context.DeferCleanup(() => LabMod.Settings.Experimental = mode);
        var area = new CellRect(5, 110, 241, 20);
        var terrain = area.ToDictionary(c => c, c => c.GetTerrain(map));
        context.DeferCleanup(() => { foreach (var cell in terrain) map.terrainGrid.SetTerrain(cell.Key, cell.Value); });
        foreach (var cell in area) { map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil); map.fogGrid.Unfog(cell); }
        // A complete dividing wall leaves exactly one permitted native door crossing.
        for (int z = 0; z < map.Size.z; z++)
        {
            var building = ThingMaker.MakeThing(z == 121 ? ThingDefOf.Door : ThingDefOf.Wall, ThingDefOf.WoodLog);
            building.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(building, new IntVec3(120, 0, z), map);
            if (z == 121) door = building;
        }
        pile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(pile);
        foreach (var cell in new CellRect(150, 114, 3, 15)) pile.AddCell(cell);
        pile.settings.filter.SetDisallowAll();
        var cargoDefs = new[] { ThingDefOf.Steel, ThingDefOf.WoodLog, ThingDef.Named("BlocksGranite") };
        foreach (var def in cargoDefs) pile.settings.filter.SetAllow(def, true);
        pile.settings.Priority = StoragePriority.Critical;
        for (int i = 0; i < 3; i++)
        {
            Pawn pawn = null!;
            for (int attempt = 0; attempt < 64; attempt++)
            {
                pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                    forceGenerateNewPawn: true, allowAddictions: false, fixedBiologicalAge: 25, fixedChronologicalAge: 25));
                if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)) break;
                pawn.Destroy(DestroyMode.Vanish);
            }
            EndToEndAssert.True(!pawn.Destroyed && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling), "Need a native hauling-capable pawn.");
            GenSpawn.Spawn(pawn, new IntVec3(85, 0, 115 + i * 6), map);
            foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1f;
            pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            foreach (var work in DefDatabase<WorkTypeDef>.AllDefs) pawn.workSettings.SetPriority(work, 0);
            pawns.Add(pawn);
            doorwayTrace[pawn.ThingID] = new List<IntVec3>();
            Thing item = ThingMaker.MakeThing(cargoDefs[i]);
            item.stackCount = 10;
            GenSpawn.Spawn(item, new IntVec3(90, 0, 115 + i * 6), map);
            item.SetForbidden(false, false);
            items.Add(item);
        }
        var tracePatch = new Harmony("fumblesneeze.pathfindinglab.door-transition-test");
        observing = this;
        context.DeferCleanup(() => { tracePatch.UnpatchAll(tracePatch.Id); observing = null; });
        tracePatch.Patch(AccessTools.DeclaredMethod(typeof(Pawn_PathFollower), "TryEnterNextPathCell", Type.EmptyTypes),
            prefix: new HarmonyMethod(typeof(ColonyTimingVerification), nameof(BeforeCellEntry)),
            postfix: new HarmonyMethod(typeof(ColonyTimingVerification), nameof(AfterCellEntry)));
        probe = new SearchTimingProbe(map);
        context.DeferCleanup(probe.Dispose);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause hauling preconditions", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame haulers and distant stockpile", pawns.Select(p => p.ThingID).Concat(items.Select(t => t.ThingID)).ToArray(), 250);
        yield return new ScreenshotStep("three undrafted haulers before native orders", Array.Empty<string>(), 0);
        if (!LabMod.Settings.Experimental)
        {
            var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { pawns[0].ThingID }, Array.Empty<string>())
                .Single(g => g.Label == "PL.Experimental".Translate().ToString());
            yield return new GizmoActionStep("enable experimental routing for ordinary work", new[] { pawns[0].ThingID }, toggle.RuntimeType,
                EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        }
        // Readiness/fallback is verified separately with a held builder. Prepare this ordinary-job
        // fixture while paused; the supporting query cannot count as native hauling evidence.
        using (var preflight = map.pathFinder.FindPathNow(pawns[0].Position, items[0].Position, pawns[0]))
            EndToEndAssert.True(preflight.Found, "Hauling precondition must be reachable.");
        yield return new WaitUntilStep("prepare hauling guide while paused", _ => !PathRuntime.PreparationPending(map) && probe.FlushBackground(),
            new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
        // Enable real Hauling work outside the timed window; native work selection supplies the jobs.
        foreach (var pawn in pawns) pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        var samples = probe.BeginJourney();
        var beforeCounts = pawns.ToDictionary(p => p.ThingID, PathRuntime.Counts);
        bool experimentalDuringHauling = false;
        void ObserveExperimentalHauling() => experimentalDuringHauling |= pawns.Any(p =>
            p.CurJobDef == JobDefOf.HaulToCell && PathRuntime.Counts(p).Handled > beforeCounts[p.ThingID].Handled);
        long started = Stopwatch.GetTimestamp();
        int startTick = GenTicks.TicksGame;
        yield return new TimeControlActionStep("resume ordinary colony hauling", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep("each native hauler picks up distinct cargo", _ =>
        {
            ObserveExperimentalHauling();
            foreach (var pawn in pawns)
            {
                if (pawn.CurJobDef == JobDefOf.HaulToCell && pawn.carryTracker.CarriedThing is { } cargo && items.Contains(cargo))
                    carriedBy[pawn.ThingID] = cargo;
            }
            return carriedBy.Count == 3 && carriedBy.Values.Distinct().Count() == 3;
        },
            new EndToEndDeadline(12000, 10000, TimeSpan.FromSeconds(90)));
        yield return new CheckpointStep("individual native cargo pickup", _ => new Dictionary<string, string>
        {
            ["pawnToCargo"] = JsonSerializer.Serialize(carriedBy.ToDictionary(pair => pair.Key, pair => pair.Value.ThingID)),
            ["jobs"] = JsonSerializer.Serialize(pawns.Select(p => new { p.ThingID, Job = p.CurJobDef?.defName,
                Cargo = p.carryTracker.CarriedThing?.ThingID, Position = p.Position.ToString() }))
        });
        yield return new CameraActionStep("follow working haulers", pawns.Select(p => p.ThingID).Append(door.ThingID).ToArray(), 150);
        yield return new ScreenshotStep("ordinary hauling jobs in progress", Array.Empty<string>(), 0);
        yield return new WaitUntilStep("observe cargo at the native doorway", _ =>
        {
            ObserveExperimentalHauling();
            return pawns.Any(p => p.Position == door.Position && p.carryTracker.CarriedThing != null);
        }, new EndToEndDeadline(12000, 10000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause native door crossing", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame the permitted doorway", new[] { door.ThingID }, 65);
        yield return new ScreenshotStep("hauler carries cargo through the native door", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("continue native deliveries", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep("all three identifiable cargo stacks reach the stockpile", _ =>
        {
            ObserveExperimentalHauling();
            return carriedBy.Count == 3 && carriedBy.Values.All(cargo => cargo.Spawned && pile.ContainsCell(cargo.Position) && cargo.stackCount == 10);
        },
            new EndToEndDeadline(12000, 10000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause delivered stockpile", true, EndToEndGameSpeed.Normal);
        // A yielded native pause runs on a later runner advance. Keep collecting through it,
        // then drain and freeze the counters at the same boundary as the originating window.
        probe.EndJourney();
        double wallMs = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
        yield return new WaitUntilStep("finish originating-window work", _ => samples.Pending == 0,
            new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
        var afterCounts = pawns.ToDictionary(p => p.ThingID, PathRuntime.Counts);
        var delivered = pile.Cells.SelectMany(c => c.GetThingList(map)).First(t => t.def == ThingDefOf.Steel);
        yield return new CameraActionStep("frame native delivery result", pawns.Select(p => p.ThingID).Append(delivered.ThingID).ToArray(), 150);
        yield return new SelectionActionStep("inspect delivered cargo", new[] { delivered.ThingID }, false);
        yield return new ScreenshotStep("three distinct stacks delivered by ordinary jobs", Array.Empty<string>(), 0);
        var totals = samples.Totals();
        yield return new CheckpointStep("all-pawn ordinary-job timing coverage", _ => new Dictionary<string, string>
        {
            ["workload"] = "three-pawn instrumentation acceptance, not a megabase benchmark",
            ["mapSize"] = map.Size.ToString(), ["pawnIds"] = string.Join(",", pawns.Select(p => p.ThingID)),
            ["deliveries"] = JsonSerializer.Serialize(carriedBy.Select(pair => new { PawnId = pair.Key,
                CargoId = pair.Value.ThingID, Definition = pair.Value.def.defName, Count = pair.Value.stackCount, Position = pair.Value.Position.ToString() })),
            ["rawSearchSamples"] = JsonSerializer.Serialize(samples.Snapshot()), ["totals"] = JsonSerializer.Serialize(totals),
            ["movementSamples"] = JsonSerializer.Serialize(probe.Movements.Snapshot()),
            ["movementOutcomes"] = JsonSerializer.Serialize(probe.Movements.Outcomes),
            ["movementLatencyTotal"] = JsonSerializer.Serialize(probe.Movements.LatencyTotal),
            ["pendingAtClose"] = samples.PendingAtClose.ToString()!,
            ["lateCompletions"] = samples.LateCompletions.ToString(),
            ["recordedPathfindingMs"] = totals.Sum(t => t.ElapsedMilliseconds).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ["preparationMs"] = totals.Sum(t => t.PreparationMilliseconds).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ["observationWallMs"] = wallMs.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ["observationTicks"] = (GenTicks.TicksGame - startTick).ToString(),
            ["pendingWorkers"] = samples.Pending.ToString(),
            ["doorCoverage"] = JsonSerializer.Serialize(samples.DoorCoverage()),
            ["experimentalDuringHauling"] = experimentalDuringHauling.ToString(),
            ["detailOverflowed"] = (samples.Overflowed || probe.Movements.Overflowed).ToString(),
            ["coverage"] = "all queued requests enter experimental dispatch; busy/invalidated guides explicitly use vanilla; native preparation retained; no speedup claim",
            ["routing"] = PathRuntime.Diagnostics(map),
            ["perPawnRouting"] = JsonSerializer.Serialize(pawns.Select(p => new { p.ThingID, Before = beforeCounts[p.ThingID], Counts = afterCounts[p.ThingID] })),
            ["doorwayTrace"] = JsonSerializer.Serialize(doorwayTrace.ToDictionary(p => p.Key, p => p.Value.Select(c => c.ToString()).ToArray()))
        });
        yield return new AssertionStep("all workers and native preparation are counted", _ =>
        {
            EndToEndAssert.True(!samples.Overflowed && !probe.Movements.Overflowed && samples.Pending == 0, "Complete non-truncated window required.");
            EndToEndAssert.True(samples.DoorCoverage().Complete && totals.All(t => t.Failed == 0),
                "Every search and preparation sample in this reachable controlled workload must succeed with complete door coverage.");
            foreach (var pawn in pawns)
            {
                EndToEndAssert.True(totals.Any(t => t.PawnId == pawn.ThingID && t.Source.EndsWith("-search") && t.Count > 0 && t.Failed == 0), "Each hauler's actual searches are captured.");
                long readiness = totals.Where(t => t.PawnId == pawn.ThingID && t.Source == "experimental-readiness-dispatch").Sum(t => t.Count);
                EndToEndAssert.True(totals.Where(t => t.PawnId == pawn.ThingID && t.Source == "vanilla-search").Sum(t => t.Count) == readiness, "Every native readiness fallback is explicit.");
                EndToEndAssert.True(afterCounts[pawn.ThingID].Fallbacks == 0,
                    "Native hauling must not need emergency fallback; expected readiness fallback is counted separately.");
                long handled = afterCounts[pawn.ThingID].Handled - beforeCounts[pawn.ThingID].Handled;
                EndToEndAssert.True(totals.Where(t => t.PawnId == pawn.ThingID && t.Source == "experimental-search").Sum(t => t.Count) == handled,
                    "Every handled request contributes exactly one search timing.");
                EndToEndAssert.True(totals.Where(t => t.PawnId == pawn.ThingID && t.Source == "vanilla-doors").Sum(t => t.Count) == handled + readiness,
                    "Every handled request contributes door/provider preparation.");
                EndToEndAssert.True(doorwayTrace[pawn.ThingID].Contains(door.Position) && doorwayTrace[pawn.ThingID].All(c => c.x != 120 || c.z == 121),
                    "Every hauler was observed using the one permitted doorway, never a wall cell.");
                var edges = doorwayTrace[pawn.ThingID].Zip(doorwayTrace[pawn.ThingID].Skip(1));
                EndToEndAssert.True(edges.All(e => Math.Abs(e.First.x - e.Second.x) <= 1 && Math.Abs(e.First.z - e.Second.z) <= 1) &&
                    edges.Where(e => e.First.x == 120 || e.Second.x == 120).All(e => e.First.z == 121 && e.Second.z == 121 && Math.Abs(e.First.x - e.Second.x) == 1),
                    "Consecutive native cell entries cross the door cardinally, never through adjacent wall corners.");
                EndToEndAssert.True(probe.Movements.Snapshot().Any(m => m.PawnId == pawn.ThingID && m.Outcome == "moved"), "Each hauler has an observed native movement onset.");
            }
            EndToEndAssert.True(experimentalDuringHauling && totals.Any(t => t.Source == "experimental-search" && t.Count > t.Failed &&
                    pawns.Any(p => p.ThingID == t.PawnId)),
                "Active native hauling must include successful experimental routing, not only readiness fallback or later idle walking.");
            EndToEndAssert.True(totals.Any(t => t.Source == "vanilla-grid-worker" && t.PawnId == null && t.PreparationMilliseconds > 0), "Shared grid preparation is timed once at batch level.");
        });
        // Supporting full-query contract check; native hauling above is the player workflow.
        using (var preflight = map.pathFinder.FindPathNow(new IntVec3(5, 0, 121), new IntVec3(245, 0, 121), pawns[0]))
            EndToEndAssert.True(preflight.Found, "Full-query precondition must be reachable.");
        yield return new WaitUntilStep("prepare synchronous-query guide while paused", _ => !PathRuntime.PreparationPending(map) && probe.FlushBackground(),
            new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
        long beforeFull = PathRuntime.FullRequests, beforeFallback = PathRuntime.FallbackRequests;
        var syncSamples = probe.BeginJourney();
        using var fullPath = map.pathFinder.FindPathNow(new IntVec3(5, 0, 121), new IntVec3(245, 0, 121), pawns[0]);
        probe.EndJourney();
        EndToEndAssert.True(fullPath.Found && fullPath.LastNode == new IntVec3(245, 0, 121) && fullPath.NodesLeftCount > 200 && PathRuntime.FullRequests == beforeFull + 1,
            "Synchronous callers receive a complete experimental path through the door.");
        EndToEndAssert.True(syncSamples.Totals().Single(t => t.Source == "experimental-search").Count == 1 && syncSamples.DoorCoverage().Complete &&
            PathRuntime.FallbackRequests == beforeFallback, "The full synchronous request is timed exactly once without fallback.");
        yield return new CheckpointStep("synchronous full-query supporting diagnostic", _ => new Dictionary<string, string>
        { ["timings"] = JsonSerializer.Serialize(syncSamples.Export()), ["nodes"] = fullPath.NodesLeftCount.ToString() });

        // Deliberate failure injection verifies attribution, not native gameplay correctness.
        var failureSamples = probe.BeginJourney();
        var failurePatch = new Harmony("fumblesneeze.pathfindinglab.timing-failure-test");
        var target = AccessTools.Method(typeof(RequestRouter), nameof(RequestRouter.Find));
        try
        {
            failurePatch.Patch(target, prefix: new HarmonyMethod(typeof(ColonyTimingVerification), nameof(ThrowTimingProbe)));
            using var fallbackPath = map.pathFinder.FindPathNow(new IntVec3(5, 0, 121), new IntVec3(245, 0, 121), pawns[0]);
            EndToEndAssert.True(fallbackPath.Found, "Emergency fallback keeps the diagnostic query usable.");
        }
        finally { failurePatch.UnpatchAll(failurePatch.Id); probe.EndJourney(); }
        var failureTotals = failureSamples.Totals();
        EndToEndAssert.True(failureTotals.Single(t => t.Source == "experimental-attempt").Failed == 1 &&
            failureTotals.Single(t => t.Source == "vanilla-search").Count == 1 &&
            !failureTotals.Any(t => t.Source == "experimental-search") && failureSamples.Pending == 0 && PathRuntime.FallbackRequests == beforeFallback + 1,
            "A forced failed attempt and its native fallback are separate records, not a successful experimental sample.");
        yield return new CheckpointStep("intentional failure-attribution probe, not benchmark data", _ => new Dictionary<string, string>
        { ["timings"] = JsonSerializer.Serialize(failureSamples.Export()), ["expectedEmergencyFallbacks"] = "1" });
    }

    private static void ThrowTimingProbe() => throw new InvalidOperationException("Intentional Pathfinding Lab timing failure probe");

    private static void BeforeCellEntry(Pawn ___pawn, out IntVec3 __state) => __state = ___pawn.Position;
    private static void AfterCellEntry(Pawn ___pawn, IntVec3 __state)
    {
        if (observing == null || ___pawn.Position == __state || ___pawn.carryTracker.CarriedThing == null ||
            !observing.doorwayTrace.TryGetValue(___pawn.ThingID, out var trace)) return;
        if (__state.x < 118 && ___pawn.Position.x < 118 || __state.x > 122 && ___pawn.Position.x > 122) return;
        if (trace.Count == 0) trace.Add(__state);
        trace.Add(___pawn.Position);
    }
}
