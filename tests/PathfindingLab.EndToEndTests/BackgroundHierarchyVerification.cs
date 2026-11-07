using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.background-readiness", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 60000, MaxGameTicks = 20000, MaxWallClockSeconds = 300)]
public sealed class BackgroundHierarchyVerification : IRimWorldEndToEndTest
{
    private static BackgroundHierarchyVerification? active;
    private static readonly ManualResetEventSlim release = new(true);
    private static int blocked, mainThread, wrongThread;
    private Map map = null!;
    private Pawn pawn = null!;
    private Thing left = null!, right = null!;
    private SearchTimingProbe probe = null!;
    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        bool mode = LabMod.Settings.Experimental;
        context.DeferCleanup(() => LabMod.Settings.Experimental = mode);
        LabMod.Settings.Experimental = true;
        var area = new CellRect(65, 85, 111, 31);
        var terrain = area.ToDictionary(c => c, c => c.GetTerrain(map));
        context.DeferCleanup(() => { foreach (var pair in terrain) map.terrainGrid.SetTerrain(pair.Key, pair.Value); });
        foreach (var cell in area) { map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil); map.fogGrid.Unfog(cell); }
        pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, allowAddictions: false, fixedBiologicalAge: 25, fixedChronologicalAge: 25));
        foreach (var need in pawn.needs.AllNeeds) need.CurLevelPercentage = 1;
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 100), map);
        left = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.WoodLog), new IntVec3(70, 0, 100), map);
        right = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Steel), new IntVec3(170, 0, 100), map);
        left.SetForbidden(true, false); right.SetForbidden(true, false);
        var patches = new Harmony("fumblesneeze.pathfindinglab.async-test");
        mainThread = Environment.CurrentManagedThreadId; blocked = wrongThread = 0; release.Reset();
        active = this;
        context.DeferCleanup(() => { release.Set(); patches.UnpatchAll(patches.Id); active = null; });
        patches.Patch(AccessTools.Method(typeof(BackgroundHierarchy), "Build"), prefix: new HarmonyMethod(typeof(BackgroundHierarchyVerification), nameof(HoldBuilder)));
        probe = new SearchTimingProbe(map);
        context.DeferCleanup(probe.Dispose);
    }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause background-readiness fixture", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select native mover", new[] { pawn.ThingID }, false);
        var draft = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Single(g => !g.Disabled && g.ToggleState == false && g.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("draft native mover", new[] { pawn.ThingID }, draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: draft.StableId);
        yield return new CameraActionStep("frame route and starting pawn", new[] { pawn.ThingID, right.ThingID }, 140);
        yield return new ScreenshotStep("before cold hierarchy order", Array.Empty<string>(), 0);
        for (int phase = 0; phase < 3; phase++)
        {
            int blockedBefore = phase == 0 ? 0 : Volatile.Read(ref blocked);
            if (phase < 2) release.Reset();
            if (phase == 1)
                for (int z = 95; z <= 105; z++) GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Granite), new IntVec3(120, 0, z), map);
            Thing target = phase == 1 ? left : right;
            if (phase == 2)
            {
                // No demand query: map lifecycle prepares all cohorts while paused.
                yield return new WaitUntilStep("prepare latest geometry while paused without a path query", _ => PathRuntime.PreparationReady(map) && probe.FlushBackground(),
                    new EndToEndDeadline(10000, 5000, TimeSpan.FromSeconds(30)));
            }
            var start = pawn.Position;
            var countsBefore = PathRuntime.Counts(pawn);
            var go = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID)
                .Single(g => !g.Disabled && g.Label == "GoHere".Translate().ToString());
            var samples = probe.BeginJourney();
            if (phase < 2) yield return new TimeControlActionStep("run native movement phase " + phase, false, EndToEndGameSpeed.Fast);
            yield return new FloatMenuActionStep("native Go here while cold, invalidated or ready", pawn.ThingID, target.ThingID, go.StableId);
            if (phase == 2) yield return new TimeControlActionStep("run prepared native movement phase", false, EndToEndGameSpeed.Fast);
            if (phase < 2)
            {
                yield return new WaitUntilStep("pawn moves while background builder is held", _ => pawn.Position != start &&
                    samples.Totals().Any(t => t.PawnId == pawn.ThingID && t.Source == "experimental-readiness-dispatch" && t.Count > 0) &&
                    Volatile.Read(ref blocked) > blockedBefore,
                    new EndToEndDeadline(10000, 5000, TimeSpan.FromSeconds(12)));
                yield return new ScreenshotStep("native movement continues during held background build", Array.Empty<string>(), 0);
                EndToEndAssert.True(!release.IsSet && wrongThread == 0, "Movement happened before releasing the off-main-thread builder.");
                release.Set();
            }
            yield return new WaitUntilStep("arrive at ordered endpoint", _ => pawn.Position == target.Position && !pawn.pather.Moving,
                new EndToEndDeadline(30000, 15000, TimeSpan.FromSeconds(75)));
            yield return new TimeControlActionStep("pause native arrival phase " + phase, true, EndToEndGameSpeed.Normal);
            probe.EndJourney();
            yield return new WaitUntilStep("background completion without main-thread wait", _ => !PathRuntime.PreparationPending(map) && probe.FlushBackground() && samples.Pending == 0,
                new EndToEndDeadline(10000, 5000, TimeSpan.FromSeconds(30)));
            yield return new CameraActionStep("frame arrival and obstacle", new[] { pawn.ThingID, left.ThingID, right.ThingID }, 140);
            yield return new ScreenshotStep("native arrival after readiness phase " + phase, Array.Empty<string>(), 0);
            EndToEndAssert.True(pawn.Drafted && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && PathRuntime.FallbackRequests == 0, "Healthy native movement without exceptional fallback.");
            yield return new CheckpointStep("background readiness phase " + phase, _ => new Dictionary<string, string>
            {
                ["phase"] = phase.ToString(), ["artificialHold"] = (phase < 2).ToString(),
                ["pawn"] = pawn.ThingID,
                ["readinessFallback"] = samples.Totals().Where(t => t.PawnId == pawn.ThingID && t.Source == "experimental-readiness-dispatch").Sum(t => t.Count).ToString(),
                ["experimentalHandled"] = (PathRuntime.Counts(pawn).Handled - countsBefore.Handled).ToString(),
                ["background"] = JsonSerializer.Serialize(probe.Background.Export()),
                ["searches"] = JsonSerializer.Serialize(samples.Export()), ["hierarchy"] = JsonSerializer.Serialize(PathRuntime.HierarchyDiagnostics(map)),
                ["interpretation"] = "fault-injection correctness workflow; held background durations are not benchmark measurements"
            });
            if (phase == 2) EndToEndAssert.True(PathRuntime.Counts(pawn).Handled > countsBefore.Handled &&
                samples.Totals().Any(t => t.PawnId == pawn.ThingID && t.Source == "experimental-search" && t.Count > t.Failed),
                "Ready guide must serve the selected pawn's warm native order.");
        }
    }
    private static void HoldBuilder(BackgroundHierarchy __instance)
    {
        if (active == null) return;
        if (Environment.CurrentManagedThreadId == mainThread) { Interlocked.Increment(ref wrongThread); return; }
        if (release.IsSet) return;
        Interlocked.Increment(ref blocked);
        if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Test did not release the background worker in time.");
    }
}
