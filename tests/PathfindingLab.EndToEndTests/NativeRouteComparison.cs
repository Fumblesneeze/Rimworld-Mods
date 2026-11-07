using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HarmonyLib;
using PathfindingLab;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.native-route-comparison", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 60000, MaxGameTicks = 60000, MaxWallClockSeconds = 600)]
public class NativeRouteComparison : IRimWorldEndToEndTest
{
    protected virtual bool LargeMap => false;
    protected virtual bool Barrier => false;
    private Pawn pawn = null!;
    private Thing left = null!, right = null!;
    private Map map = null!;
    private bool originalMode;
    private int z;
    private SearchTimingProbe timing = null!;
    private const string QuietOwner = "fumblesneeze.pathfindinglab.benchmark-quiet-map";
    private static WildAnimalSpawner? suppressedWildlife;
    private int removedHazards;
    private int experimentalJourneys;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        if (LargeMap)
            EndToEndAssert.True(LabMod.Settings.MapSize >= 500 && map.Size.x == LabMod.Settings.MapSize && map.Size.z == LabMod.Settings.MapSize,
                "Native settlement generation must honor the explicitly isolated size setting.");
        originalMode = LabMod.Settings.Experimental;
        context.DeferCleanup(() => LabMod.Settings.Experimental = originalMode);
        PrepareQuietMap(context);
        z = map.Size.z / 2;
        // Disposable routing fixture, not a colony: open ground with a later rock barrier.
        var rect = new CellRect(20, z - (LargeMap ? 2 : 40), map.Size.x - 40, LargeMap ? 5 : 81)
            .Concat(LargeMap && Barrier ? new CellRect(map.Size.x / 2 - 40, z - 40, 81, 81) : Enumerable.Empty<IntVec3>())
            .Distinct().ToArray();
        // This Core-only fixture supports ordinary top/under terrain, not foundations or seasonal overlays.
        EndToEndAssert.True(rect.All(cell => map.terrainGrid.FoundationAt(cell) == null && map.terrainGrid.TempTerrainAt(cell) == null),
            "Fixture requires terrain without foundation/temporary layers; refuse before mutation.");
        var baseline = rect.Select(cell => (Cell: cell, Terrain: map.terrainGrid.TopTerrainAt(cell), Under: map.terrainGrid.UnderTerrainAt(cell),
            Color: map.terrainGrid.ColorAt(cell), Roof: map.roofGrid.RoofAt(cell), Fog: cell.Fogged(map))).ToArray();
        context.DeferCleanup(() =>
        {
            foreach (var value in baseline)
            {
                if (value.Cell.GetTerrain(map) != value.Terrain || map.terrainGrid.UnderTerrainAt(value.Cell) != value.Under)
                    map.terrainGrid.SetTerrain(value.Cell, value.Terrain);
                if (value.Under != null) map.terrainGrid.SetUnderTerrain(value.Cell, value.Under);
                if (map.terrainGrid.ColorAt(value.Cell) != value.Color) map.terrainGrid.SetTerrainColor(value.Cell, value.Color);
                map.roofGrid.SetRoof(value.Cell, value.Roof);
                if (value.Fog) map.fogGrid.Refog(new CellRect(value.Cell.x, value.Cell.z, 1, 1));
                else map.fogGrid.Unfog(value.Cell);
            }
            EndToEndAssert.True(baseline.All(value => map.terrainGrid.TopTerrainAt(value.Cell) == value.Terrain &&
                map.terrainGrid.UnderTerrainAt(value.Cell) == value.Under && map.terrainGrid.ColorAt(value.Cell) == value.Color &&
                map.terrainGrid.FoundationAt(value.Cell) == null && map.terrainGrid.TempTerrainAt(value.Cell) == null &&
                map.roofGrid.RoofAt(value.Cell) == value.Roof && value.Cell.Fogged(map) == value.Fog), "Fixture grid state restored.");
        });
        foreach (IntVec3 cell in rect)
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in cell.GetThingList(map).ToArray())
                if (thing.def.destroyable) thing.Destroy();
            if (cell.GetTerrain(map) != TerrainDefOf.Soil) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            map.fogGrid.Unfog(cell);
        }
        left = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.WoodLog), new IntVec3(30, 0, z), map);
        right = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Steel), new IntVec3(map.Size.x - 31, 0, z), map);
        left.SetForbidden(true, false);
        right.SetForbidden(true, false);
        pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, allowAddictions: false, fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoGear: true));
        PrepareJourney();
        foreach (string name in new[] { "Apparel_Parka", "Apparel_Pants", "Apparel_CollarShirt" })
        {
            var clothing = (Apparel)ThingMaker.MakeThing(ThingDef.Named(name), ThingDefOf.Cloth);
            clothing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, null);
            pawn.apparel.Wear(clothing, dropReplacedApparel: false, locked: false);
        }
        GenSpawn.Spawn(pawn, left.Position, map);
        timing = new SearchTimingProbe(map);
        context.DeferCleanup(timing.Dispose);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause fixture", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("experimental hooks available", _ => EndToEndAssert.True(LabMod.Ready, "Beta API hooks must be installed."));
        yield return new SelectionActionStep("select test colonist", new[] { pawn.ThingID }, false);
        yield return new CameraActionStep("frame starting colonist", new[] { pawn.ThingID }, 80);
        yield return new ScreenshotStep("before long-distance native orders", Array.Empty<string>(), 0);
        var draft = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.ToggleState == false && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("draft through native gizmo", new[] { pawn.ThingID }, draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: draft.StableId);

        int postEditExperimentalJourneys = 0;
        // Separate tests keep eight 45-second journey budgets plus actions below 600 seconds.
        foreach (string scenario in new[] { Barrier ? "barrier" : "open" })
        {
            string geometry = Barrier ? "open-before-edit" : scenario;
            int modeIndex = 0;
            foreach (bool experimental in new[] { false, true, true, false })
            {
                if (Barrier && modeIndex++ == 2)
                {
                    // Edit only after experimental routes are warm, then use native orders again.
                    for (int dz = -30; dz <= 20; dz++)
                        GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Granite), new IntVec3(map.Size.x / 2, 0, z + dz), map);
                    geometry = "barrier-after-edit";
                }
                foreach (var step in SetMode(context, experimental)) yield return step;
                // At one million cells, ABBA uses one alternating full crossing per mode.
                foreach (Thing target in map.Size.x >= 1000
                             ? new[] { pawn.Position == left.Position ? right : left }
                             : new[] { right, left })
                {
                    PrepareJourney();
                    RequireUninterruptedPawn();
                    float temperature = GenTemperature.GetTemperatureForCell(pawn.Position, map);
                    var safeTemperature = pawn.SafeTemperatureRange();
                    EndToEndAssert.True(safeTemperature.Includes(temperature),
                        $"Benchmark requires safe ambient temperature: {temperature} C, pawn range {safeTemperature}.");
                    var before = PathRuntime.Counts(pawn);
                    var boundsBefore = PathRuntime.Bounds(pawn);
                    string movementBefore = $"{pawn.TicksPerMoveCardinal}/{pawn.TicksPerMoveDiagonal}";
                    int distance = Math.Max(Math.Abs(pawn.Position.x - target.Position.x), Math.Abs(pawn.Position.z - target.Position.z));
                    int horizon = PathfindingLab.Core.LabPolicy.RefinementCells(LabMod.Settings.RefinementCells);
                    var go = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID)
                        .Single(option => !option.Disabled && option.Label == "GoHere".Translate().ToString());
                    int tick = GenTicks.TicksGame;
                    yield return new TimeControlActionStep("run before latency-sensitive order", false, LargeMap ? EndToEndGameSpeed.Superfast : EndToEndGameSpeed.Fast);
                    SearchSamples samples = timing.BeginJourney();
                    yield return new FloatMenuActionStep($"{geometry} {(experimental ? "experimental" : "vanilla")} native Go here",
                        pawn.ThingID, target.ThingID, go.StableId);
                    yield return new WaitUntilStep("first native movement before screenshot", _ =>
                            timing.Movements.Snapshot().Any(value => value.PawnId == pawn.ThingID && value.Outcome == "moved"),
                        new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
                    yield return new ScreenshotStep("native order has begun movement", Array.Empty<string>(), 0);
                    yield return new WaitUntilStep("arrive at original destination", _ =>
                        {
                            RequireUninterruptedPawn();
                            if (pawn.Position != target.Position || pawn.pather.Moving) return false;
                            timing.EndJourney(); // Close at arrival, before later UI/camera steps.
                            return true;
                        },
                        new EndToEndDeadline(LargeMap ? 60000 : 10000, LargeMap ? 40000 : 10000, TimeSpan.FromSeconds(LargeMap ? 600 : 45)));
                    yield return new TimeControlActionStep("pause arrived pawn", true, EndToEndGameSpeed.Normal);
                    yield return new CameraActionStep("frame arrival", new[] { pawn.ThingID, target.ThingID }, 80);
                    yield return new ScreenshotStep("arrived through native path follower", Array.Empty<string>(), 0);
                    yield return new WaitUntilStep("drain originating-window workers", _ => samples.Pending == 0,
                        new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
                    var allSamples = samples.Snapshot();
                    var rawSamples = allSamples.Where(value => value.PawnId == pawn.ThingID &&
                        value.Source is "vanilla-search" or "experimental-search").ToArray();
                    var totals = samples.Totals();
                    yield return new CheckpointStep("direct Stopwatch search timings and native route outcome", _ => new Dictionary<string, string>
                    {
                        ["scenario"] = geometry, ["experimental"] = experimental.ToString(),
                        ["worldSeed"] = Find.World.info.seedString, ["biome"] = map.Biome.defName,
                        ["tile"] = map.Tile.ToString(),
                        ["comparisonProtocol"] = map.Size.x >= 1000 ? "ABBA four alternating full crossings, two per mode" : "ABBA four round trips, four crossings per mode",
                        ["fixturePawn"] = "v3: new 25-year-old; normal cloth parka/pants/collar shirt; needs/health reset before each trip only; combat hazards removed; storyteller and wildlife spawning suppressed",
                        ["removedCombatHazards"] = removedHazards.ToString(),
                        ["temperatureAfter"] = GenTemperature.GetTemperatureForCell(pawn.Position, map).ToString(CultureInfo.InvariantCulture),
                        ["nativeSpeed"] = LargeMap ? "Superfast" : "Fast",
                        ["timingUnit"] = "milliseconds elapsed inside search; not walking ticks or thread CPU time",
                        ["timingScope"] = "all pawns on this map: native or replacement worker + shared gather/grid schedule/grid batches + per-pawn doors/providers; includes experimental profile-lock wait; excludes scheduler queue wait, remaining result delivery, setup and walking",
                        ["latencyScope"] = "stationary native StartPath entry to first movement-cost payment/cell transition; excludes pre-StartPath UI latency; no paused screenshot interval",
                        ["stopwatchFrequency"] = Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture),
                        ["allPawnSearchSamples"] = JsonSerializer.Serialize(allSamples),
                        ["allPawnTotalsBySource"] = JsonSerializer.Serialize(totals),
                        ["allPawnRecordedPathfindingMs"] = totals.Sum(value => value.ElapsedMilliseconds).ToString("R", CultureInfo.InvariantCulture),
                        ["allPawnPreparationMs"] = totals.Sum(value => value.PreparationMilliseconds).ToString("R", CultureInfo.InvariantCulture),
                        ["allPawnSearchExcludingPreparationMs"] = totals.Sum(value => value.SearchMilliseconds).ToString("R", CultureInfo.InvariantCulture),
                        ["movementSamples"] = JsonSerializer.Serialize(timing.Movements.Snapshot()),
                        ["movementOutcomes"] = JsonSerializer.Serialize(timing.Movements.Outcomes),
                        ["movementLatencyTotal"] = JsonSerializer.Serialize(timing.Movements.LatencyTotal),
                        ["pendingAtClose"] = samples.PendingAtClose.ToString()!,
                        ["lateCompletions"] = samples.LateCompletions.ToString(),
                        ["detailOverflowed"] = (samples.Overflowed || timing.Movements.Overflowed).ToString(),
                        ["pendingWorkers"] = samples.Pending.ToString(),
                        ["experimentalCoverage"] = "all native map-cell search requests; only follower requests are partial; emergency fallback counted separately; native preparation retained",
                        ["refinementCells"] = horizon.ToString(),
                        ["renewalThresholdNodes"] = PathfindingLab.Core.LabPolicy.RenewalNodes(horizon).ToString(),
                        ["addedPolicyRenewals"] = (PathRuntime.Bounds(pawn).Renewals - boundsBefore.Renewals).ToString(),
                        ["renewalScope"] = "added-policy counter only: native 15-cell partial-endpoint proximity can renew earlier, especially at horizon 25",
                        ["searchSamples"] = JsonSerializer.Serialize(rawSamples),
                        ["searchTotalMs"] = rawSamples.Sum(value => value.ElapsedMilliseconds).ToString("R", CultureInfo.InvariantCulture),
                        ["preparationTotalMs"] = rawSamples.Sum(value => value.PreparationMilliseconds).ToString("R", CultureInfo.InvariantCulture),
                        ["processWorkingSetBytes"] = Process.GetCurrentProcess().WorkingSet64.ToString(CultureInfo.InvariantCulture),
                        ["processPeakWorkingSetBytes"] = Process.GetCurrentProcess().PeakWorkingSet64.ToString(CultureInfo.InvariantCulture),
                        ["isolation"] = LargeMap ? "single-test generated-map environment" : "empty-map fixture",
                        ["travelTicks"] = (GenTicks.TicksGame - tick).ToString(),
                        ["destination"] = target.Position.ToString(), ["pawnPosition"] = pawn.Position.ToString(),
                        ["routing"] = PathRuntime.Diagnostics(map), ["mapSize"] = map.Size.ToString(),
                        ["pawnCountsBefore"] = before.ToString(), ["pawnCountsAfter"] = PathRuntime.Counts(pawn).ToString(),
                        ["renewalBounds"] = PathRuntime.Bounds(pawn).ToString(),
                        ["movementBefore"] = movementBefore, ["movementAfter"] = $"{pawn.TicksPerMoveCardinal}/{pawn.TicksPerMoveDiagonal}"
                    });
                    yield return new AssertionStep("every journey used the selected pathfinder", _ =>
                    {
                        var after = PathRuntime.Counts(pawn);
                        if (experimental && after.Handled > before.Handled)
                        {
                            experimentalJourneys++;
                            if (geometry == "barrier-after-edit") postEditExperimentalJourneys++;
                        }
                        EndToEndAssert.True(pawn.Drafted && !pawn.InMentalState && pawn.Position == target.Position,
                            "Completed command must remain a drafted arrival, not an unrelated post-arrival job.");
                        EndToEndAssert.True(rawSamples.Length > 0 && !samples.Overflowed && rawSamples.All(value => value.Succeeded),
                            "Every route must retain successful, non-truncated actual search measurements.");
                        EndToEndAssert.True(!timing.Movements.Overflowed && timing.Movements.Snapshot().Any(value =>
                            value.PawnId == pawn.ThingID && value.Outcome == "moved" && value.FirstMovementMilliseconds > 0),
                            "Native movement must have a positive first-movement latency, independent of arrival.");
                        long nativeReady = totals.Where(t => t.PawnId == pawn.ThingID && t.Source == "experimental-readiness-dispatch").Sum(t => t.Count);
                        EndToEndAssert.True(!experimental || rawSamples.Length == after.Handled - before.Handled + nativeReady,
                            "Each search is experimental or an explicitly attributed nonblocking readiness fallback.");
                        EndToEndAssert.True(experimental ? nativeReady > 0 || after.Handled - before.Handled >= (distance + horizon - 1) / horizon : after.Handled == before.Handled,
                            "Full native readiness routes or bounded experimental requests cover the trip. " + before + " -> " + after);
                        EndToEndAssert.True(after.Fallbacks == before.Fallbacks, "Controlled experimental trip must not hide a vanilla fallback.");
                        if (experimental && nativeReady == 0 && distance > horizon) EndToEndAssert.True(after.Partial > before.Partial, "Ready long trip must use partial refinement.");
                        if (experimental)
                        {
                            var bounds = PathRuntime.Bounds(pawn);
                            EndToEndAssert.True(bounds.MaxSteps <= horizon, "Every returned segment must obey the configured horizon. " + bounds);
                            // Native NeedNewPath renews within 15 cells of a partial endpoint.
                            // At horizon 25 that can preempt our 13-node policy; do not suppress it.
                            // Arrival, successful partial requests and full-distance coverage above
                            // still prove rolling refinement for that short-horizon case.
                            if (nativeReady == 0 && distance > horizon && horizon >= 50)
                                EndToEndAssert.True(bounds.Renewals > boundsBefore.Renewals && bounds.MinNodes > 1 && bounds.MaxNodes <= PathfindingLab.Core.LabPolicy.RenewalNodes(horizon),
                                    "The added half-horizon policy must renew before exhaustion in this long-horizon open fixture. " + bounds);
                        }
                    });
                }
            }
        }
        EndToEndAssert.True(experimentalJourneys > 0, "At least one native journey must use actual experimental routing, not only readiness fallback.");
        if (Barrier) EndToEndAssert.True(postEditExperimentalJourneys > 0, "Experimental routing must recover after the geometry edit.");
        if (!LargeMap && LabMod.Settings.RefinementCells < right.Position.DistanceTo(left.Position))
        {
            PrepareJourney();
            foreach (var step in SetMode(context, true)) yield return step;
            var before = PathRuntime.Counts(pawn);
            IntVec3 start = pawn.Position;
            var go = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, right.ThingID)
                .Single(option => !option.Disabled && option.Label == "GoHere".Translate().ToString());
            yield return new FloatMenuActionStep("start experimental order for active switch", pawn.ThingID, right.ThingID, go.StableId);
            yield return new TimeControlActionStep("walk initial partial segment", false, EndToEndGameSpeed.Fast);
            yield return new WaitUntilStep("observe movement before switch", _ => pawn.Position.DistanceTo(start) >= 10 && PathRuntime.Counts(pawn).Partial > before.Partial,
                new EndToEndDeadline(1000, 1000, TimeSpan.FromSeconds(15)));
            yield return new TimeControlActionStep("pause ongoing order", true, EndToEndGameSpeed.Normal);
            yield return new CameraActionStep("frame moving pawn", new[] { pawn.ThingID }, 80);
            yield return new ScreenshotStep("partial movement before native mode switch", Array.Empty<string>(), 0);
            foreach (var step in SetMode(context, false)) yield return step;
            var switched = PathRuntime.Counts(pawn);
            yield return new TimeControlActionStep("continue existing order without reissuing it", false, EndToEndGameSpeed.Fast);
            yield return new WaitUntilStep("vanilla finishes original destination", _ => pawn.Position == right.Position,
                new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(45)));
            yield return new TimeControlActionStep("pause after active switch", true, EndToEndGameSpeed.Normal);
            yield return new CameraActionStep("frame active-switch arrival", new[] { pawn.ThingID }, 80);
            yield return new ScreenshotStep("same order arrived after switching to vanilla", Array.Empty<string>(), 0);
            yield return new AssertionStep("no experimental request after mode switch", _ =>
                EndToEndAssert.True(PathRuntime.Counts(pawn).Handled == switched.Handled, "Vanilla must finish without further experimental requests."));
        }
        foreach (var step in SetMode(context, originalMode)) yield return step;
    }

    private void PrepareJourney()
    {
        // Benchmark preconditions, never a movement shortcut or part of the measured search.
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1f;
    }

    private void RequireUninterruptedPawn()
    {
        EndToEndAssert.True(pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Drafted && !pawn.InMentalState &&
            !pawn.health.hediffSet.hediffs.Any(value => value is Hediff_Injury),
            "Benchmark journey interrupted: injury, downing, death, loss of draft, mental state or despawn; reject timing, never heal in transit.");
    }

    private void PrepareQuietMap(IEndToEndContext context)
    {
        // Disposable benchmark preconditions, not product behavior or a gameplay shortcut.
        bool storytellerEnabled = DebugSettings.enableStoryteller;
        var storyteller = Find.Storyteller;
        var previousQueue = storyteller.incidentQueue;
        var harmony = new Harmony(QuietOwner);
        context.DeferCleanup(() =>
        {
            harmony.UnpatchAll(QuietOwner);
            suppressedWildlife = null;
            storyteller.incidentQueue = previousQueue;
            DebugSettings.enableStoryteller = storytellerEnabled;
        });
        DebugSettings.enableStoryteller = false;
        storyteller.incidentQueue = new IncidentQueue();
        var tick = AccessTools.DeclaredMethod(typeof(WildAnimalSpawner), "WildAnimalSpawnerTick", Type.EmptyTypes)
            ?? throw new MissingMethodException("WildAnimalSpawner.WildAnimalSpawnerTick");
        if (tick.IsStatic || tick.ReturnType != typeof(void) || tick.IsGenericMethod)
            throw new MissingMethodException("WildAnimalSpawner.WildAnimalSpawnerTick shape changed");
        suppressedWildlife = map.wildAnimalSpawner;
        harmony.Patch(tick, prefix: new HarmonyMethod(typeof(NativeRouteComparison), nameof(AllowWildlifeTick)));
        EndToEndAssert.True(DebugSettings.enableDamage && DebugSettings.enablePlayerDamage, "Benchmark must not disable ordinary damage.");

        foreach (var thing in map.listerThings.AllThings.ToArray())
        {
            if (thing is Pawn || thing.Destroyed) continue;
            bool spawner = thing is TunnelHiveSpawner or PawnGroundSpawner ||
                thing is ThingWithComps withComps && withComps.AllComps.Any(comp =>
                    comp is CompSpawnerPawn or CompSpawnerHives or CompPawnSpawnOnWakeup);
            if (!spawner && !thing.HostileTo(Faction.OfPlayer)) continue;
            thing.Destroy(DestroyMode.Vanish);
            removedHazards++;
        }
        var occupants = new List<Pawn>();
        ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Everything), occupants);
        foreach (var occupant in occupants.Where(value => value.Faction != Faction.OfPlayer).ToArray())
        {
            if (occupant.Destroyed) continue;
            occupant.Destroy(DestroyMode.Vanish);
            removedHazards++;
        }
    }

    private static bool AllowWildlifeTick(WildAnimalSpawner __instance) => !ReferenceEquals(__instance, suppressedWildlife);

    private IEnumerable<EndToEndStep> SetMode(IEndToEndContext context, bool experimental)
    {
        if (LabMod.Settings.Experimental == experimental) yield break;
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Label == "PL.Experimental".Translate().ToString());
        yield return new GizmoActionStep("switch pathfinder through player gizmo", new[] { pawn.ThingID }, toggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }
}
