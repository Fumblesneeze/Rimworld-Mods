using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.wildlife-wander", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 60000, MaxGameTicks = 20000, MaxWallClockSeconds = 300)]
public sealed class WildlifeWanderVerification : IRimWorldEndToEndTest
{
    private static WildlifeWanderVerification? active;
    private Map map = null!;
    private readonly List<Pawn> animals = new();
    private readonly Dictionary<Pawn, IntVec3> roots = new();
    private readonly Dictionary<Job, IntVec3> jobRoots = new();
    private readonly Dictionary<Pawn, int> selections = new();
    private int recycledRoutes;
    private int herdRoutes;
    private readonly Dictionary<Job, IntVec3[]> directJobs = new();
    private readonly List<object> transitions = new();
    private readonly List<object> routes = new();
    private readonly HashSet<Pawn> directMovers = new();
    private SearchTimingProbe probe = null!;
    private Thing food = null!;
    private Pawn eater = null!;
    private int eatenStart;
    private long purposefulSearches;
    private bool feeding;
    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        bool mode = LabMod.Settings.Experimental;
        context.DeferCleanup(() => LabMod.Settings.Experimental = mode);
        LabMod.Settings.Experimental = true;
        var area = new CellRect(90, 90, 75, 75);
        var terrain = area.ToDictionary(c => c, c => c.GetTerrain(map));
        context.DeferCleanup(() => { foreach (var pair in terrain) map.terrainGrid.SetTerrain(pair.Key, pair.Value); });
        foreach (var cell in area) { map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil); map.fogGrid.Unfog(cell); }
        // An explicit routing fixture, not a decorated colony. Wall is an actual obstacle.
        for (int z = 116; z <= 135; z++) GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Granite), new IntVec3(131, 0, z), map);
        for (int i = 0; i < 6; i++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDef.Named("Deer"));
            GenSpawn.Spawn(pawn, new IntVec3(124 + i % 2 * 2, 0, 121 + i / 2 * 2), map);
            foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1;
            animals.Add(pawn);
        }
        var patches = new Harmony("fumblesneeze.pathfindinglab.wildlife-test");
        active = this;
        context.DeferCleanup(() => { patches.UnpatchAll(patches.Id); active = null; });
        patches.Patch(AccessTools.Method(typeof(JobGiver_WanderHerd), "GetWanderRoot"), postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(RootSelected)));
        patches.Patch(AccessTools.Method(typeof(JobGiver_Wander), "TryGiveJob"), postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(JobSelected)));
        patches.Patch(AccessTools.Method(typeof(JobGiver_Wander), "GetExactWanderDest"),
            prefix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(SelectingRoot)) { priority = Priority.First },
            postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(SelectedRoot)));
        patches.Patch(AccessTools.Method(typeof(Job), nameof(Job.Clear)),
            prefix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(BeforeJobClear)) { priority = Priority.First },
            postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(AfterJobClear)));
        patches.Patch(AccessTools.Method(typeof(WildlifeWander), "PushPrefix"), postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(DirectPath)));
        patches.Patch(AccessTools.Method(typeof(PathFinder), "ParameterizePathJob"), postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(SearchStarted)));
        patches.Patch(AccessTools.Method(typeof(Pawn_PathFollower), "PatherTick"),
            prefix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(BeforeMovement)),
            postfix: new HarmonyMethod(typeof(WildlifeWanderVerification), nameof(AfterMovement)));
        probe = new SearchTimingProbe(map);
        context.DeferCleanup(probe.Dispose);
    }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause wildlife preconditions", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame herd beside stone obstacle", animals.Select(p => p.ThingID).ToArray(), 70);
        yield return new SelectionActionStep("inspect idle wild deer", new[] { animals[0].ThingID }, false);
        yield return new ScreenshotStep("herd before autonomous directional wandering", Array.Empty<string>(), 0);
        var samples = probe.BeginJourney();
        yield return new TimeControlActionStep("let ordinary wildlife think tree run", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep("observe several native direct wander paths and movement", _ =>
            routes.Count >= 6 && directMovers.Count >= 3 && recycledRoutes > 0 && herdRoutes > 0,
            new EndToEndDeadline(30000, 10000, TimeSpan.FromSeconds(120)));
        yield return new TimeControlActionStep("pause moving herd", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame native herd movement against boundary", animals.Select(p => p.ThingID).ToArray(), 80);
        yield return new ScreenshotStep("deer moved by native follower within selected herd circles", Array.Empty<string>(), 0);
        probe.EndJourney();
        yield return new CheckpointStep("direct idle path and herd-boundary evidence", _ => new Dictionary<string, string>
        {
            ["routes"] = JsonSerializer.Serialize(routes), ["movers"] = JsonSerializer.Serialize(directMovers.Select(p => p.ThingID)),
            ["timings"] = JsonSerializer.Serialize(samples.Export()), ["wander"] = JsonSerializer.Serialize(WildlifeWander.Diagnostics()),
            ["recycledRoutes"] = recycledRoutes.ToString(),
            ["herdRoutes"] = herdRoutes.ToString(),
            ["directTransitions"] = JsonSerializer.Serialize(transitions),
            ["positions"] = JsonSerializer.Serialize(animals.Select(p => new { p.ThingID, Position = p.Position.ToString(), Job = p.CurJobDef?.defName }))
        });
        EndToEndAssert.True(samples.Totals().Any(t => t.Source == "wildlife-direct" && t.Count > 0), "Direct handoffs must be separately timed.");

        // Hunger/food placement are disposable preconditions, not the claimed eating outcome.
        // Native unpause must choose an ingest job, request a purposeful path, walk and eat.
        eater = animals[0];
        eater.needs.food.CurLevelPercentage = .05f;
        food = ThingMaker.MakeThing(ThingDef.Named("RawCorn")); food.stackCount = 75;
        GenSpawn.Spawn(food, new IntVec3(100, 0, 120), map); food.SetForbidden(false, false);
        eatenStart = food.stackCount;
        feeding = true;
        var feedingSamples = probe.BeginJourney();
        yield return new CameraActionStep("frame hungry deer and distant food", new[] { eater.ThingID, food.ThingID }, 100);
        yield return new ScreenshotStep("food seeking preconditions before native unpause", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("resume ordinary food seeking", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep("observe native purposeful ingest movement", _ => purposefulSearches > 0 && eater.CurJobDef == JobDefOf.Ingest && eater.pather.Moving,
            new EndToEndDeadline(30000, 10000, TimeSpan.FromSeconds(90)));
        yield return new ScreenshotStep("hungry deer uses ordinary routing for food", Array.Empty<string>(), 0);
        yield return new WaitUntilStep("native deer consumes food", _ => eater.needs.food.CurLevelPercentage > .15f && (food.Destroyed || food.stackCount < eatenStart),
            new EndToEndDeadline(30000, 10000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after native ingestion", true, EndToEndGameSpeed.Normal);
        probe.EndJourney();
        yield return new CameraActionStep("inspect animal at consumed food", new[] { eater.ThingID }, 55);
        yield return new ScreenshotStep("purposeful eating completed outside idle shortcut", Array.Empty<string>(), 0);
        yield return new CheckpointStep("purposeful wildlife exclusion", _ => new Dictionary<string, string>
        {
            ["purposefulSearches"] = purposefulSearches.ToString(), ["foodBefore"] = eatenStart.ToString(),
            ["foodAfter"] = (food.Destroyed ? 0 : food.stackCount).ToString(), ["foodNeed"] = eater.needs.food.CurLevelPercentage.ToString(),
            ["timings"] = JsonSerializer.Serialize(feedingSamples.Export())
        });
        EndToEndAssert.True(PathRuntime.FallbackRequests == 0 && animals.All(p => p.Spawned && !p.Dead && !p.Downed), "Healthy animals and no emergency fallback.");
    }
    private static void RootSelected(Pawn pawn, IntVec3 __result)
    { if (active?.animals.Contains(pawn) == true) { active.roots[pawn] = __result; active.selections[pawn] = active.selections.GetValueOrDefault(pawn) + 1; } }
    private static void SelectingRoot(Pawn pawn) { if (active?.animals.Contains(pawn) == true) active.selections[pawn] = 0; }
    private static void SelectedRoot(Pawn pawn)
    { if (active?.animals.Contains(pawn) == true) EndToEndAssert.True(active.selections.GetValueOrDefault(pawn) <= 1, "Regrouping must not choose a second random herd root."); }
    private static bool Cached(Job job)
    {
        var table = AccessTools.Field(typeof(WildlifeWander), "jobs").GetValue(null)!;
        return (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, new object?[] { job, null })!;
    }
    private static void BeforeJobClear(Job __instance, out bool __state) => __state = active != null && Cached(__instance);
    private static void AfterJobClear(Job __instance, bool __state)
    {
        if (active == null) return;
        active.jobRoots.Remove(__instance);
        active.directJobs.Remove(__instance);
        if (!__state) return;
        EndToEndAssert.True(!Cached(__instance), "Recycled jobs must release idle-route/map metadata.");
        active.recycledRoutes++;
    }
    private static void JobSelected(Pawn pawn, Job? __result)
    { if (active != null && __result != null && active.roots.TryGetValue(pawn, out var root)) active.jobRoots[__result] = root; }
    private static void DirectPath(PathRequest request, bool __result)
    {
        var self = active;
        if (self == null || __result || request.pawn == null || !self.animals.Contains(request.pawn) || request.Found != true) return;
        EndToEndAssert.True(request.pawn.CurJobDef == JobDefOf.GotoWander, "Direct path is strictly an idle job.");
        request.TryGetPath(out var path);
        var cells = path.NodesReversed.ToArray();
        bool herd = request.pawn.CurJob.jobGiver?.GetType() == typeof(JobGiver_WanderHerd);
        var root = self.jobRoots.TryGetValue(request.pawn.CurJob, out var r) ? r : IntVec3.Invalid;
        EndToEndAssert.True(cells.Length is >= 2 and <= 11 && cells.All(c => c.WalkableBy(self.map, request.pawn) && !(c.x == 131 && c.z >= 116 && c.z <= 135)), "Bounded legal idle route avoids the stone obstacle.");
        if (herd) EndToEndAssert.True(root.IsValid && cells.All(c => DirectionalWalk.WithinCircle(self.map.Size.x,
            self.map.cellIndices.CellToIndex(c), self.map.cellIndices.CellToIndex(root), 5)), "Every herd step stays within the native five-cell circle.");
        if (herd) self.herdRoutes++;
        self.directJobs[request.pawn.CurJob] = cells;
        self.routes.Add(new { Pawn = request.pawn.ThingID, Herd = herd, Root = root.ToString(), Cells = cells.Select(c => c.ToString()).ToArray() });
    }
    private static void SearchStarted(PathRequest request)
    {
        if (active == null) return;
        EndToEndAssert.True(request.pawn?.CurJob == null || !active.directJobs.ContainsKey(request.pawn.CurJob),
            "An admitted idle job must never reach global path search/preparation.");
        if (active.feeding && request.pawn == active.eater && request.pawn.CurJobDef == JobDefOf.Ingest) active.purposefulSearches++;
    }
    private sealed record Movement(Job Job, IntVec3[] Route, IntVec3 From);
    private static void BeforeMovement(Pawn ___pawn, out Movement? __state)
    {
        __state = active != null && ___pawn.CurJob is { } job && active.directJobs.TryGetValue(job, out var route)
            ? new Movement(job, route, ___pawn.Position) : null;
    }
    private static void AfterMovement(Pawn ___pawn, Movement? __state)
    {
        if (active == null || __state == null || ___pawn.Position == __state.From || ___pawn.CurJob != __state.Job ||
            !active.directJobs.TryGetValue(__state.Job, out var route) || !ReferenceEquals(route, __state.Route)) return;
        int from = Array.IndexOf(route, __state.From), to = Array.IndexOf(route, ___pawn.Position);
        EndToEndAssert.True(from > 0 && to == from - 1, "Native movement follows the exact admitted reverse-node route.");
        active.directMovers.Add(___pawn);
        if (active.transitions.Count < 100) active.transitions.Add(new { Pawn = ___pawn.ThingID,
            Job = __state.Job.loadID, From = __state.From.ToString(), To = ___pawn.Position.ToString() });
    }
}
