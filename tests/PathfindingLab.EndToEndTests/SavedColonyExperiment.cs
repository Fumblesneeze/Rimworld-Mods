using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.saved-colony-vanilla", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 200000, MaxGameTicks = 10000, MaxWallClockSeconds = 900)]
public sealed class SavedColonyVanilla : SavedColonyExperiment { protected override int Horizon => 0; }

[RimWorldEndToEndTest("pathfinding-lab.saved-colony-h50", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 200000, MaxGameTicks = 10000, MaxWallClockSeconds = 900)]
public sealed class SavedColonyH50 : SavedColonyExperiment { protected override int Horizon => 50; }

[RimWorldEndToEndTest("pathfinding-lab.saved-colony-h200", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 200000, MaxGameTicks = 10000, MaxWallClockSeconds = 900)]
public sealed class SavedColonyH200 : SavedColonyExperiment { protected override int Horizon => 200; }

// Each stable ID is selected alone through e2e_run_start in a fresh process.
// Native load is fixture setup. Native time-control unpause and autonomous work are the workflow.
public abstract class SavedColonyExperiment : IRimWorldEndToEndTest, IEndToEndProcessOwnedGameFixture
{
    internal const string OriginalMaster = "C:/Manuel/RimWorldBenchmarkSaves/PathfindingLab/medium-size-colony-20261024.rws";
    internal const string OriginalHash = "ECACE785FB75FE4A0B8D140B24749B17DFF71048A282739A14F90F5589D77F97";
    private string Master = OriginalMaster, MasterHash = OriginalHash;
    private int savedTick = 909;
    private const string SaveName = "PathfindingLab-medium-colony-input";
    protected abstract int Horizon { get; }
    protected virtual bool RaidStress => false;
    protected virtual bool RaidApproach => false;
    private int WindowTicks => RaidApproach ? 2000 : RaidStress ? 600 : 300;
    private int WindowCount => RaidApproach ? 6 : RaidStress ? 4 : 3;
    private IntVec3 colonyCenter;
    protected Map map = null!;
    private Pawn[] trackedActors = Array.Empty<Pawn>();
    private SearchTimingProbe probe = null!;
    private string evidence = null!;

    public void Arrange(IEndToEndContext context)
    {
        if (RaidStress)
        {
            string rootPath = RaidApproach ? RaidApproachSetup.ApproachRoot : RaidColonySetup.Root;
            using var manifest = JsonDocument.Parse(File.ReadAllText(rootPath + "/input.json"));
            var root = manifest.RootElement;
            EndToEndAssert.True(root.GetProperty("SourceHash").GetString() == OriginalHash,
                "Raid snapshot must derive from the preserved colony.");
            Master = rootPath + "/raid-colony.rws";
            MasterHash = root.GetProperty("Hash").GetString()!;
            savedTick = root.GetProperty("SavedTick").GetInt32();
            colonyCenter = IntVec3.FromString(root.GetProperty("Center").GetString());
            if (RaidApproach) EndToEndAssert.True(root.GetProperty("BeforeApproach").GetBoolean(), "Expected a pre-approach snapshot.");
        }
        string runSave = GenFilePaths.FilePathForSavedGame(SaveName);
        // The runner owns a unique isolated savedata; never copy into the normal profile.
        string isolated = Path.GetFullPath(GenFilePaths.SaveDataFolderPath);
        EndToEndAssert.True(isolated.Contains(Path.DirectorySeparatorChar + "artifacts" + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase), "Saved-colony fixture requires repository-isolated savedata.");
        Directory.CreateDirectory(Path.GetDirectoryName(runSave)!);
        SavedColonyInput.CopyVerified(Master, runSave, MasterHash);
        context.DeferCleanup(() => File.Delete(runSave));
        bool pauseOnLoad = Prefs.PauseOnLoad, experimental = LabMod.Settings.Experimental;
        int horizon = LabMod.Settings.RefinementCells;
        context.DeferCleanup(() => { Prefs.PauseOnLoad = pauseOnLoad; LabMod.Settings.Experimental = experimental; LabMod.Settings.RefinementCells = horizon; });
        Prefs.PauseOnLoad = true;
        LabMod.Settings.Experimental = false; // No experimental cache warming during setup.
        evidence = Path.Combine(GenFilePaths.SaveDataFolderPath, "PathfindingLabMeasurements");
        Directory.CreateDirectory(evidence);
    }

    protected IEnumerable<EndToEndStep> LoadColony()
    {
        yield return new TimeControlActionStep("pause before loading archived colony copy", true, EndToEndGameSpeed.Normal);
        var original = Current.Game;
        GameDataSaveLoader.LoadGame(SaveName);
        yield return new WaitUntilStep("load immutable colony through native loader", _ =>
            !ReferenceEquals(Current.Game, original) && Current.Game?.PlayerHasControl == true &&
            Find.CurrentMap != null && !LongEventHandler.AnyEventNowOrWaiting,
            new EndToEndDeadline(100000, RaidStress ? 100000 : 10000, TimeSpan.FromSeconds(360)));
        yield return new TimeControlActionStep("keep loaded colony paused", true, EndToEndGameSpeed.Normal);
        map = Find.CurrentMap;
        EndToEndAssert.True(map.Size.x == 1000 && map.Size.z == 1000, "Expected the archived1000-square colony.");
        SavedColonyInput.ValidateLoadTick(savedTick, GenTicks.TicksGame);
    }

    public virtual IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in LoadColony()) yield return step;
        EndToEndAssert.True(LabMod.Ready, "Experimental hooks must be shape-validated in both arms.");
        LabMod.Settings.RefinementCells = Horizon == 0 ? 50 : Horizon;
        var colonists = map.mapPawns.FreeColonistsSpawned.ToArray();
        trackedActors = map.mapPawns.AllPawnsSpawned.Where(p => p.IsFreeColonist || p.HostileTo(Faction.OfPlayer)).ToArray();
        var hostiles = trackedActors.Where(p => p.HostileTo(Faction.OfPlayer)).ToArray();
        var hostileIds = new HashSet<string>(hostiles.Select(p => p.ThingID));
        var hostilePositions = hostiles.ToDictionary(p => p.ThingID, p => p.Position);
        long successfulHostileSearches = 0;
        bool hostileProgress = false;
        if (RaidStress) EndToEndAssert.True(hostiles.Length >= 20, "Raid input must retain an attacking population.");
        EndToEndAssert.True(colonists.Length > 0, "The supplied colony must contain native colonists.");
        var initiallyHealthy = colonists.Where(p => !p.Downed && !p.Dead).ToArray();
        var initialPositions = colonists.ToDictionary(p => p.ThingID, p => p.Position);
        var colonistIds = new HashSet<string>(colonists.Select(p => p.ThingID));
        long successfulColonistSearches = 0;
        bool colonistProgress = false;
        Write("input.json", new { Master, MasterHash, Horizon, Mode = Horizon == 0 ? "vanilla" : "experimental",
            ProductSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(LabMod).Assembly.Location))),
            GameVersion = VersionControl.CurrentVersionStringWithRev,
            Packages = LoadedModManager.RunningModsListForReading.Select(m => m.PackageIdPlayerFacing).ToArray(),
            Snapshot = Snapshot(), NativeLoadHandoffTicks = GenTicks.TicksGame - savedTick,
            DevMode = Prefs.DevMode, GodMode = DebugSettings.godMode,
            Storyteller = Find.Storyteller.def.defName, Difficulty = Find.Storyteller.difficultyDef?.defName,
            Instrumentation = "shared-graph-v5: proactive-v4 scopes; shared graph publication and workspace admission measured; LockWaitMilliseconds now denotes admission elapsed time, not blocking wait",
            Policy = $"{WindowCount} successive windows targeting {WindowTicks} ticks each from first unpause; native pause is frame-observed, actual ticks retained; eager paused preparation separate; native Fast speed; no simulation normalization; Stopwatch diagnostics, not CPU or causal net impact",
            RaidStress, RaidApproach, ColonyCenter = colonyCenter.ToString(), SavedTick = savedTick, InitialHostileIds = hostileIds.ToArray() });
        var initialActor = RaidStress ? hostiles.OrderBy(p => p.Position.DistanceToSquared(colonists[0].Position)).First() : colonists[0];
        yield return new CameraActionStep("frame user colony before autonomous work",
            RaidStress ? new[] { initialActor.ThingID } : colonists.Select(p => p.ThingID).ToArray(), RaidStress ? 65 : 110);
        yield return new SelectionActionStep("inspect initial native actor", new[] { initialActor.ThingID }, false);
        yield return new ScreenshotStep("saved colony before native unpause", Array.Empty<string>(), 0);
        probe = new SearchTimingProbe(map);
        context.DeferCleanup(probe.Dispose);
        var preparation = probe.BeginJourney(-1);
        int preparationTick = GenTicks.TicksGame;
        long preparationStarted = Stopwatch.GetTimestamp();
        LabMod.Settings.Experimental = Horizon != 0;
        if (Horizon != 0)
            yield return new WaitUntilStep("prepare all routing cohorts while paused before first unpause", _ =>
                PathRuntime.PreparationReady(map) && !PathRuntime.PreparationPending(map) && probe.FlushBackground(),
                new EndToEndDeadline(30000, 10000, TimeSpan.FromSeconds(90)));
        probe.EndJourney();
        EndToEndAssert.True(GenTicks.TicksGame == preparationTick && hostiles.All(p => p.Position == hostilePositions[p.ThingID]),
            "Preparation must not advance the approach.");
        EndToEndAssert.True(!preparation.Totals().Any(t => t.Source.EndsWith("-search", StringComparison.Ordinal)),
            "Preparation must not issue synthetic path searches.");
        Write("preparation.json", new { Window = -1, StartedTick = preparationTick, EndedTick = GenTicks.TicksGame,
            WallMilliseconds = Stopwatch.GetElapsedTime(preparationStarted).TotalMilliseconds,
            Ready = Horizon != 0 && PathRuntime.PreparationReady(map),
            Hierarchy = PathRuntime.HierarchyDiagnostics(map), Workspaces = PathRuntime.WorkspaceDiagnostics(map), Searches = preparation.Export(), Background = probe.Background.Export() });
        for (int window = 0; window < WindowCount; window++)
        {
            int index = window;
            var before = Snapshot();
            var routingBefore = Routing();
            var hierarchyBefore = PathRuntime.HierarchyDiagnostics(map);
            var workspacesBefore = PathRuntime.WorkspaceDiagnostics(map);
            var wanderBefore = WildlifeWander.Diagnostics();
            int startedTick = GenTicks.TicksGame;
            var samples = probe.BeginJourney(index);
            long started = Stopwatch.GetTimestamp();
            yield return new TimeControlActionStep("resume real colony window " + index, false, EndToEndGameSpeed.Fast);
            // A fixed cumulative target avoids accumulating frame-level pause overshoot.
            // The native scheduler observes this on a frame, not by directly ticking or
            // patching TickManager; retain the exact final boundary and denominator.
            yield return new TimeControlActionStep("pause at observation boundary " + index, true, EndToEndGameSpeed.Normal,
                preparationTick + (index + 1) * WindowTicks,
                new EndToEndDeadline(100000, 10000, TimeSpan.FromSeconds(180)));
            // Pause executes on a later runner advance. Keep measuring until that native boundary,
            // so workload snapshots and routing deltas cannot include unmeasured intervening ticks.
            int endedTick = GenTicks.TicksGame;
            double wallMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            probe.EndJourney();
            yield return new WaitUntilStep("drain originating window workers " + index, _ => samples.Pending == 0,
                new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
            Write("window-" + index + ".json", new { Window = index, Cold = false, Horizon,
                StartedTick = startedTick, EndedTick = endedTick, WallMilliseconds = wallMs,
                Before = before, After = Snapshot(), RoutingBefore = routingBefore, RoutingAfter = Routing(),
                HierarchyBefore = hierarchyBefore, HierarchyAfter = PathRuntime.HierarchyDiagnostics(map),
                WorkspacesBefore = workspacesBefore, WorkspacesAfter = PathRuntime.WorkspaceDiagnostics(map),
                WanderBefore = wanderBefore, WanderAfter = WildlifeWander.Diagnostics(),
                Searches = samples.Export(), DoorCoverage = samples.DoorCoverage(), MovementOutcomes = probe.Movements.Outcomes,
                MovementDetailsOverflowed = probe.Movements.Overflowed, probe.Movements.LatencyTotal,
                Movements = probe.Movements.Snapshot() });
            yield return new CheckpointStep("colony timing window " + index, _ => new Dictionary<string, string>
            {
                ["window"] = index.ToString(), ["startTick"] = startedTick.ToString(), ["endTick"] = endedTick.ToString(),
                ["wallMs"] = wallMs.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["routing"] = PathRuntime.Diagnostics(map), ["pending"] = samples.Pending.ToString(),
                ["rawDetailOverflow"] = samples.Overflowed.ToString(), ["evidence"] = "window-" + index + ".json"
            });
            var actor = colonists.FirstOrDefault(p => p.Spawned && p.pather.Moving) ?? colonists.FirstOrDefault(p => p.Spawned);
            if (RaidStress)
                actor = hostiles.Where(p => p.Spawned && !p.Downed && !p.Dead)
                    .OrderBy(p => p.Position.DistanceToSquared(colonists[0].Position)).FirstOrDefault() ?? actor;
            if (actor != null)
            {
                yield return new CameraActionStep("follow native colony work " + index, new[] { actor.ThingID }, 45);
                yield return new SelectionActionStep("inspect working pawn " + index, new[] { actor.ThingID }, false);
            }
            yield return new ScreenshotStep("native colony work after window " + index, Array.Empty<string>(), 0);
            successfulColonistSearches += samples.Totals().Where(t => t.Source.EndsWith("-search", StringComparison.Ordinal) &&
                t.PawnId != null && colonistIds.Contains(t.PawnId)).Sum(t => t.Count - t.Failed);
            colonistProgress |= colonists.Any(p => p.Spawned && p.Position != initialPositions[p.ThingID]);
            successfulHostileSearches += samples.Totals().Where(t => t.Source.EndsWith("-search", StringComparison.Ordinal) &&
                t.PawnId != null && hostileIds.Contains(t.PawnId)).Sum(t => t.Count - t.Failed);
            hostileProgress |= hostiles.Any(p => p.Spawned && p.Position != hostilePositions[p.ThingID]);
            EndToEndAssert.True(RaidStress || initiallyHealthy.All(p => !p.Dead && !p.Downed && p.Spawned),
                "A previously healthy colonist became unavailable; retain this run as an unhealthy diagnostic.");
            EndToEndAssert.True(samples.Pending == 0 && samples.Totals().Any(t => t.Source.EndsWith("-search", StringComparison.Ordinal)),
                "Window must contain completed actual native path requests.");
            EndToEndAssert.True(PathRuntime.FallbackRequests == 0, "Emergency fallback invalidates an experimental comparison.");
            EndToEndAssert.True(samples.DoorCoverage().Complete,
                "Incomplete door/provider timing; preserve raw evidence but reject full-scope comparison: " + JsonSerializer.Serialize(samples.DoorCoverage()));
        }
        EndToEndAssert.True(RaidStress || (successfulColonistSearches > 0 && colonistProgress),
            "Wildlife-only or failed-only searches cannot establish working-colony acceptance.");
        EndToEndAssert.True(!RaidStress || (successfulHostileSearches > 0 && hostileProgress),
            "Raid comparison needs successful hostile searches and native hostile progress.");
        yield return new WaitUntilStep("drain background hierarchy without advancing simulation", _ => !PathRuntime.PreparationPending(map) && probe.FlushBackground(),
            new EndToEndDeadline(10000, 10000, TimeSpan.FromSeconds(30)));
    }

    private object Snapshot()
    {
        var pawns = map.mapPawns.AllPawnsSpawned.ToArray();
        var records = new[] { "ThingsHauled", "PlantsHarvested", "PlantsSown", "ThingsConstructed", "ThingsCrafted", "MealsCooked" }
            .Select(n => DefDatabase<RecordDef>.GetNamed(n) ?? throw new InvalidOperationException("Missing required work record: " + n)).ToArray();
        return new { Tick = GenTicks.TicksGame, MapSize = map.Size.ToString(), Pawns = pawns.Length,
            DistanceRecord = "unavailable: Core 1.7 has no CellsTravelled record; positions are snapshots, not travel distance",
            Colonists = pawns.Count(p => p.IsFreeColonist), Wildlife = pawns.Count(p => p.RaceProps.Animal && p.Faction == null),
            Buildings = map.listerThings.AllThings.Count(t => t.def.category == ThingCategory.Building),
            TrackedActors = trackedActors.Select(p => new
                { p.ThingID, p.Dead, p.Downed, p.Spawned, Health = p.health.summaryHealth.SummaryHealthPercent,
                    Hostile = p.HostileTo(Faction.OfPlayer), Position = p.Position.ToString(),
                    DistanceToColony = RaidStress && p.Spawned ? (double?)p.Position.DistanceTo(colonyCenter) : null }).ToArray(),
            PawnStates = pawns.Select(p => new { p.ThingID, Kind = p.kindDef.defName, IsColonist = p.IsFreeColonist,
                Faction = p.Faction?.GetUniqueLoadID(), Hostile = p.HostileTo(Faction.OfPlayer),
                Position = p.Position.ToString(), Job = p.CurJobDef?.defName,
                p.Downed, p.Dead, p.Drafted, Moving = p.pather?.Moving ?? false,
                Records = p.IsFreeColonist ? records.ToDictionary(d => d.defName, d => p.records.GetValue(d)) : null }).ToArray() };
    }
    private static object Routing() => new { PathRuntime.HandledRequests, PathRuntime.PartialRequests,
        PathRuntime.FullRequests, PathRuntime.RepairRequests, PathRuntime.FallbackRequests, PathRuntime.ReadinessFallbackRequests };
    private void Write(string leaf, object value) => File.WriteAllText(Path.Combine(evidence, leaf),
        JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
}
