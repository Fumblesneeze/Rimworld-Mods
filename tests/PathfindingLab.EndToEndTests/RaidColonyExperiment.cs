using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI.Group;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.raid-colony-prepare", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 60000, MaxWallClockSeconds = 1500)]
public class RaidColonySetup : SavedColonyExperiment
{
    internal const string Root = "C:/Manuel/rimworld-mods/artifacts/PathfindingLab/RaidStress-20261024";
    internal const string SnapshotPath = Root + "/raid-colony.rws";
    internal const string ManifestPath = Root + "/input.json";
    protected override int Horizon => 0;
    protected virtual string OutputRoot => Root;
    protected virtual bool BeforeApproach => false;

    public override IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        string snapshotPath = OutputRoot + "/raid-colony.rws", manifestPath = OutputRoot + "/input.json";
        EndToEndAssert.True(!File.Exists(snapshotPath) && !File.Exists(manifestPath),
            "Refusing to replace an existing raid-stress snapshot.");
        foreach (var step in LoadColony()) yield return step;
        var colonists = map.mapPawns.FreeColonistsSpawned.ToArray();
        var center = new IntVec3((int)colonists.Average(p => p.Position.x), 0,
            (int)colonists.Average(p => p.Position.z));
        var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f =>
            !f.IsPlayer && !f.def.hidden && f.def.humanlikeFaction && !f.defeated &&
            !f.deactivated && f.HostileTo(Faction.OfPlayer) && f.def.defName == "Pirate")
            ?? throw new InvalidOperationException("Expected a live hostile Core Pirate faction.");
        yield return new CameraActionStep("colony before four native raids", colonists.Select(p => p.ThingID).ToArray(), 110);
        yield return new ScreenshotStep("before raid generation", Array.Empty<string>(), 0);
        var raids = new List<object>();
        int generationTick = GenTicks.TicksGame;
        for (int i = 0; i < 4; i++)
        {
            var before = new HashSet<string>(map.mapPawns.AllPawnsSpawned.Select(p => p.ThingID));
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            yield return new IncidentActionStep("spawn native enemy raid " + (i + 1), "RaidEnemy", faction.loadID,
                BeforeApproach ? "EdgeWalkIn" : null, BeforeApproach ? "ImmediateAttack" : null);
            if (map.listerThings.AllThings.Any(t => t is DropPodIncoming || t is ActiveTransporter))
            {
                yield return new TimeControlActionStep("allow native raid pods to land", false, EndToEndGameSpeed.Superfast);
                yield return new WaitUntilStep("wait for native raid pods to open", _ =>
                    !map.listerThings.AllThings.Any(t => t is DropPodIncoming || t is ActiveTransporter),
                    new EndToEndDeadline(30000, 3000, TimeSpan.FromSeconds(90)));
                yield return new TimeControlActionStep("pause after native landing", true, EndToEndGameSpeed.Normal);
            }
            var added = map.mapPawns.AllPawnsSpawned.Where(p => !before.Contains(p.ThingID)).ToArray();
            EndToEndAssert.True(added.Length > 0 && added.All(p => p.HostileTo(Faction.OfPlayer)),
                "Native incident must produce hostile map pawns.");
            raids.Add(new { Number = i + 1, Points = points, Count = added.Length,
                Pawns = added.Select(p => new { p.ThingID, Position = p.Position.ToString(),
                    LordJob = p.GetLord()?.LordJob?.GetType().FullName }).ToArray() });
        }
        var attackers = map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer)).ToArray();
        EndToEndAssert.True(attackers.Length is >= 40 and <= 2000,
            "Four-raid population outside declared diagnostic safety envelope40..2000.");
        Directory.CreateDirectory(OutputRoot);
        WriteNew(OutputRoot + "/generation.json", new { Source = OriginalMaster, SourceHash = OriginalHash,
            Tick = GenTicks.TicksGame, Center = center.ToString(), Faction = faction.GetUniqueLoadID(), Raids = raids });
        yield return new CameraActionStep("frame newly generated raid", new[] { attackers[0].ThingID }, 65);
        yield return new SelectionActionStep("inspect native raider", new[] { attackers[0].ThingID }, false);
        yield return new ScreenshotStep("native raiders before approach", Array.Empty<string>(), 0);
        if (!BeforeApproach)
        {
        yield return new TimeControlActionStep("allow native raid approach", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("wait for twenty attackers near colony", _ =>
            attackers.Count(p => p.Spawned && !p.Downed && p.Position.DistanceToSquared(center) <= 10000) >= 20,
            new EndToEndDeadline(200000, 40000, TimeSpan.FromSeconds(600)));
        yield return new TimeControlActionStep("pause raid-stress input", true, EndToEndGameSpeed.Normal);
        }
        else EndToEndAssert.True(GenTicks.TicksGame == generationTick && attackers.All(p =>
            p.Position.DistanceToSquared(center) > 200 * 200 && p.GetLord()?.LordJob is LordJob_AssaultColony),
            "Approach input must contain paused distant native immediate-attack raiders, not post-approach combat.");
        var nearest = attackers.Where(p => p.Spawned && !p.Downed).OrderBy(p => p.Position.DistanceToSquared(center)).First();
        yield return new CameraActionStep("frame approaching attack", new[] { nearest.ThingID }, 65);
        yield return new SelectionActionStep("inspect approaching raider", new[] { nearest.ThingID }, false);
        yield return new ScreenshotStep(BeforeApproach ? "attackers still at map edge before saving" : "attackers reached colony approaches", Array.Empty<string>(), 0);
        const string save = "PathfindingLab-raid-stress-input";
        string localSave = GenFilePaths.FilePathForSavedGame(save);
        context.DeferCleanup(() => File.Delete(localSave));
        int savedTick = GenTicks.TicksGame;
        string[] ids = attackers.Where(p => p.Spawned).Select(p => p.ThingID).ToArray();
        string selectedId = nearest.ThingID;
        yield return new SaveLoadActionStep("native save and reload raid snapshot", save);
        map = Find.CurrentMap;
        SavedColonyInput.ValidateLoadTick(savedTick, GenTicks.TicksGame);
        EndToEndAssert.True(ids.All(id => map.mapPawns.AllPawnsSpawned.Any(p => p.ThingID == id)),
            "Native save/load must preserve every still-spawned attacker identity.");
        string hash;
        using (var stream = File.OpenRead(localSave)) hash = Convert.ToHexString(SHA256.HashData(stream));
        SavedColonyInput.CopyVerified(localSave, snapshotPath, hash);
        File.SetAttributes(snapshotPath, File.GetAttributes(snapshotPath) | FileAttributes.ReadOnly);
        WriteNew(manifestPath, new { Source = OriginalMaster, SourceHash = OriginalHash,
            Hash = hash, SavedTick = savedTick, HostileIds = ids, Center = center.ToString(),
            BeforeApproach, GenerationTick = generationTick,
            HostilePositions = map.mapPawns.AllPawnsSpawned.Where(p => ids.Contains(p.ThingID)).Select(p => new
                { p.ThingID, Position = p.Position.ToString(), DistanceToColony = p.Position.DistanceTo(center) }).ToArray(),
            GameVersion = VersionControl.CurrentVersionStringWithRev });
        yield return new CameraActionStep("frame reloaded native attack", new[] { selectedId }, 65);
        yield return new SelectionActionStep("inspect reloaded raider", new[] { selectedId }, false);
        yield return new ScreenshotStep("temporary raid snapshot reloaded", Array.Empty<string>(), 0);
    }

    private static void WriteNew(string path, object value)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true });
    }
}

[RimWorldEndToEndTest("pathfinding-lab.raid-approach-prepare", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 60000, MaxWallClockSeconds = 1500)]
public sealed class RaidApproachSetup : RaidColonySetup
{
    internal const string ApproachRoot = "C:/Manuel/rimworld-mods/artifacts/PathfindingLab/RaidApproach-20261024";
    protected override string OutputRoot => ApproachRoot;
    protected override bool BeforeApproach => true;
}

[RimWorldEndToEndTest("pathfinding-lab.raid-approach-vanilla", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 100000, MaxWallClockSeconds = 1500)]
public sealed class RaidApproachVanilla : SavedColonyExperiment
{
    protected override int Horizon => 0;
    protected override bool RaidStress => true;
    protected override bool RaidApproach => true;
}

[RimWorldEndToEndTest("pathfinding-lab.raid-approach-h50", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 100000, MaxWallClockSeconds = 1500)]
public sealed class RaidApproachH50 : SavedColonyExperiment
{
    protected override int Horizon => 50;
    protected override bool RaidStress => true;
    protected override bool RaidApproach => true;
}

[RimWorldEndToEndTest("pathfinding-lab.raid-colony-vanilla", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 100000, MaxWallClockSeconds = 1500)]
public sealed class RaidColonyVanilla : SavedColonyExperiment
{
    protected override int Horizon => 0;
    protected override bool RaidStress => true;
}

[RimWorldEndToEndTest("pathfinding-lab.raid-colony-h50", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 300000, MaxGameTicks = 100000, MaxWallClockSeconds = 1500)]
public sealed class RaidColonyH50 : SavedColonyExperiment
{
    protected override int Horizon => 50;
    protected override bool RaidStress => true;
}
