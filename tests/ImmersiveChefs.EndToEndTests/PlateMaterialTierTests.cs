using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.plate-material-tiers",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class PlateMaterialTierTest : IRimWorldEndToEndTest
{
    private readonly List<Fixture> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, 4);
        fixtures.Add(CreateFixture(
            map,
            centers[0],
            "Simple Wood Kitchen",
            ThingDefOf.MealSimple,
            ThingDefOf.WoodLog,
            Array.Empty<ThingDef>()));
        fixtures.Add(CreateFixture(
            map,
            centers[1],
            "Simple Granite Kitchen",
            ThingDefOf.MealSimple,
            DefDatabase<ThingDef>.GetNamed("BlocksGranite"),
            Array.Empty<ThingDef>()));
        fixtures.Add(CreateFixture(
            map,
            centers[2],
            "Fine Steel Kitchen",
            ThingDefOf.MealFine,
            ThingDefOf.Steel,
            new[]
            {
                ThingDefOf.WoodLog,
                DefDatabase<ThingDef>.GetNamed("BlocksGranite")
            }));
        fixtures.Add(CreateFixture(
            map,
            centers[3],
            "Lavish Silver Kitchen",
            DefDatabase<ThingDef>.GetNamed("MealLavish"),
            ThingDefOf.Silver,
            new[] { ThingDefOf.Steel }));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var allTargets = fixtures
            .SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Meal.ThingID,
                fixture.Plate.ThingID,
                fixture.Stove.ThingID
            }.Concat(fixture.RejectedPlates.Select(plate => plate.ThingID)))
            .ToArray();
        yield return new SelectionActionStep(
            "select tiered plating fixtures",
            allTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame all tiered plating fixtures",
            allTargets,
            paddingPixels: 100);
        yield return new ScreenshotStep(
            "before ordinary tiered plating",
            allTargets,
            paddingPixels: 100);
        yield return new TimeControlActionStep(
            "run ordinary Cooking work for tiered plating",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "a cook reaches the native plate-meals job",
            _ => fixtures.Any(fixture =>
                fixture.Pawn.CurJobDef?.defName == "ImmersiveChefs_PlateMeals"),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new ScreenshotStep(
            "ordinary tiered plating in progress",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 100);
        yield return new WaitUntilStep(
            "every meal receives its eligible plate",
            _ => fixtures.All(fixture =>
                fixture.Meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == 1 &&
                fixture.Meal.Spawned),
            new EndToEndDeadline(3_000, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after tiered plating",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "exact eligible plate materials were embedded",
            _ =>
            {
                foreach (var fixture in fixtures)
                {
                    var embedded = fixture.Meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing();
                    EndToEndAssert.NotNull(
                        embedded,
                        fixture.Name + " must retain one physical embedded plate.");
                    EndToEndAssert.Equal(
                        fixture.ExpectedStuff.defName,
                        embedded!.Stuff?.defName,
                        fixture.Name + " must retain the arranged eligible Stuff exactly.");
                    EndToEndAssert.True(
                        fixture.Plate.Destroyed || fixture.Plate.holdingOwner is not null,
                        fixture.Name + " must remove the loose plate from the room through the native job.");
                    foreach (var rejected in fixture.RejectedPlates)
                    {
                        EndToEndAssert.True(
                            !ReferenceEquals(embedded, rejected),
                            fixture.Name + " must not embed the exact ineligible " +
                            rejected.Stuff?.defName + " plate.");
                        EndToEndAssert.False(
                            rejected.Destroyed,
                            fixture.Name + " must not destroy the rejected " +
                            rejected.Stuff?.defName + " plate.");
                    }
                }
            });
        yield return new SelectionActionStep(
            "select plated meals after ordinary work",
            fixtures.Select(fixture => fixture.Meal.ThingID).ToArray(),
            additive: false);
        yield return new ScreenshotStep(
            "after ordinary tiered plating",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Meal.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 100);
        yield return new CheckpointStep(
            "tiered plating result",
            _ => fixtures.ToDictionary(
                fixture => fixture.Name,
                fixture =>
                    "embedded=" + (fixture.Meal.GetComp<CompEmbeddedWare>()
                        ?.PeekPlateThing()?.Stuff?.defName ?? "missing") +
                    "; rejected=" + string.Join(
                        ",",
                        fixture.RejectedPlates.Select(plate =>
                            (plate.Stuff?.defName ?? "missing") + ":" +
                            (plate.Spawned
                                ? "spawned"
                                : plate.holdingOwner?.GetType().Name ?? "unheld")))));
    }

    private static Fixture CreateFixture(
        Map map,
        IntVec3 center,
        string name,
        ThingDef mealDef,
        ThingDef plateStuff,
        IReadOnlyList<ThingDef> rejectedPlateStuffs)
    {
        BuildSealedRoom(map, center);
        var pawn = GenerateCook();
        pawn.Name = new NameSingle(name);
        pawn.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        if (pawn.needs?.food is { } food)
        {
            food.CurLevelPercentage = 1f;
        }

        GenSpawn.Spawn(pawn, center + (IntVec3.South * 3), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        var meal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 2), map);
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            plateStuff);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        var rejectedPlates = new List<Thing>();
        var rejectedOffsets = new[] { IntVec3.North * 2, IntVec3.South * 2 };
        for (var index = 0; index < rejectedPlateStuffs.Count; index++)
        {
            var rejected = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
                rejectedPlateStuffs[index]);
            GenSpawn.Spawn(rejected, center + rejectedOffsets[index], map);
            rejectedPlates.Add(rejected);
        }

        EndToEndAssert.Equal(
            0,
            meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? -1,
            name + " must begin as an imported unplated meal.");
        return new Fixture(name, pawn, meal, plate, rejectedPlates, stove, plateStuff);
    }

    private static Pawn GenerateCook()
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cooking))
            {
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a Cooking-capable tiered-plating pawn.");
    }

    private static void BuildSealedRoom(Map map, IntVec3 center)
    {
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        for (var offset = -5; offset <= 5; offset++)
        {
            SpawnWall(map, center + new IntVec3(offset, 0, -5), granite);
            SpawnWall(map, center + new IntVec3(offset, 0, 5), granite);
            if (offset is -5 or 5)
            {
                continue;
            }

            SpawnWall(map, center + new IntVec3(-5, 0, offset), granite);
            SpawnWall(map, center + new IntVec3(5, 0, offset), granite);
        }
    }

    private static void SpawnWall(Map map, IntVec3 cell, ThingDef stuff)
    {
        var wall = ThingMaker.MakeThing(ThingDefOf.Wall, stuff);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
    }

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        for (var x = -72; x <= 72; x += 18)
        {
            for (var z = -72; z <= 72; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (!candidate.InBounds(map) ||
                    centers.Any(center => center.DistanceToSquared(candidate) < 225) ||
                    !SquareIsUsable(map, candidate, 6))
                {
                    continue;
                }

                centers.Add(candidate);
                if (centers.Count == count)
                {
                    return centers;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find four separated tiered-plating fixture rooms.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) || !cell.Walkable(map) || cell.GetEdifice(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class Fixture
    {
        public Fixture(
            string name,
            Pawn pawn,
            ThingWithComps meal,
            Thing plate,
            IReadOnlyList<Thing> rejectedPlates,
            Thing stove,
            ThingDef expectedStuff)
        {
            Name = name;
            Pawn = pawn;
            Meal = meal;
            Plate = plate;
            RejectedPlates = rejectedPlates;
            Stove = stove;
            ExpectedStuff = expectedStuff;
        }

        public string Name { get; }
        public Pawn Pawn { get; }
        public ThingWithComps Meal { get; }
        public Thing Plate { get; }
        public IReadOnlyList<Thing> RejectedPlates { get; }
        public Thing Stove { get; }
        public ThingDef ExpectedStuff { get; }
    }
}
