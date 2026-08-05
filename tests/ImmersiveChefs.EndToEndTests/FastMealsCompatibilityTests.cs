using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.fast-meals-native-cooking",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Argon.CheapMeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class FastMealsCompatibilityTest : IRimWorldEndToEndTest
{
    private readonly List<Fixture> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, 2);
        fixtures.Add(CreateFixture(
            map,
            centers[0],
            "Fast meal kitchen",
            "CM_CookFastMeal",
            "CM_SimpleFastMeal",
            ThingDefOf.WoodLog,
            Array.Empty<ThingDef>()));
        fixtures.Add(CreateFixture(
            map,
            centers[1],
            "Deluxe fast meal kitchen",
            "CM_CookFastMealDeluxe",
            "CM_DeluxeFastMeal",
            ThingDefOf.Steel,
            new[] { ThingDefOf.WoodLog }));

        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var beforeTargets = fixtures
            .SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Stove.ThingID,
                fixture.Cookware.ThingID,
                fixture.Plate.ThingID
            }.Concat(fixture.RejectedPlates.Select(plate => plate.ThingID)))
            .ToArray();
        yield return new SelectionActionStep(
            "select the Fast Meals cooking fixtures",
            beforeTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame both Fast Meals kitchens",
            beforeTargets,
            paddingPixels: 120);
        yield return new ScreenshotStep(
            "before ordinary Fast Meals bills",
            beforeTargets,
            paddingPixels: 180);
        yield return new AssertionStep(
            "Fast Meals retains its upstream work amounts",
            _ =>
            {
                EndToEndAssert.Equal(
                    100f,
                    fixtures[0].Recipe.WorkAmountForStuff(fixtures[0].Stove.Stuff),
                    "The ordinary Fast Meal recipe must remain a 100-work quick recipe.");
                EndToEndAssert.Equal(
                    150f,
                    fixtures[1].Recipe.WorkAmountForStuff(fixtures[1].Stove.Stuff),
                    "The Deluxe Fast Meal recipe must remain a 150-work quick recipe.");
            });
        yield return new TimeControlActionStep(
            "run ordinary Cooking work for Fast Meals",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "both cooks begin their native Fast Meals bills",
            _ => fixtures.All(fixture => fixture.ObserveNativeBill()),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new ScreenshotStep(
            "ordinary Fast Meals cooking in progress",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 180);
        yield return new WaitUntilStep(
            "both native Fast Meals bills produce plated meals",
            _ => fixtures.All(fixture => fixture.TryResolveCompletedProduct()),
            new EndToEndDeadline(3_000, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after Fast Meals production",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "Fast Meals uses the classified plate tiers and returns dirty cookware",
            _ =>
            {
                foreach (var fixture in fixtures)
                {
                    var product = fixture.Product;
                    EndToEndAssert.NotNull(
                        product,
                        fixture.Name + " must exist after the native bill completes.");
                    var embedded = product!.GetComp<CompEmbeddedWare>();
                    EndToEndAssert.NotNull(
                        embedded,
                        fixture.Name + " must receive the finalized embedded-ware component.");
                    EndToEndAssert.Equal(
                        1,
                        embedded!.EmbeddedPlateCount,
                        fixture.Name + " must contain exactly one physical plate.");
                    EndToEndAssert.Equal(
                        fixture.ExpectedPlateStuff.defName,
                        embedded.PeekPlateThing()?.Stuff?.defName,
                        fixture.Name + " must use its classified plate-material tier.");
                    EndToEndAssert.True(
                        fixture.Cookware.Spawned,
                        fixture.Name + " must return the exact cookware to the map.");
                    EndToEndAssert.True(
                        fixture.Cookware.GetComp<CompSanitation>()?.IsDirty == true,
                        fixture.Name + " must return the cookware dirty after actual cooking work.");
                    EndToEndAssert.Equal(
                        product.stackCount,
                        product.GetComp<CompCulinaryState>()?.Servings.Count ?? -1,
                        fixture.Name + " must contain one culinary record per produced serving.");

                    foreach (var rejected in fixture.RejectedPlates)
                    {
                        EndToEndAssert.False(
                            rejected.Destroyed,
                            fixture.Name + " must leave its ineligible " +
                            rejected.Stuff?.defName + " plate intact.");
                        EndToEndAssert.False(
                            ReferenceEquals(embedded.PeekPlateThing(), rejected),
                            fixture.Name + " must not embed its exact rejected plate.");
                        EndToEndAssert.True(
                            rejected.Spawned,
                            fixture.Name + " must leave its ineligible " +
                            rejected.Stuff?.defName + " plate untouched on the map.");
                    }
                }
            });
        yield return new SelectionActionStep(
            "select both freshly cooked Fast Meals products",
            fixtures.Select(fixture => fixture.Product!.ThingID).ToArray(),
            additive: false);
        yield return new CameraActionStep(
            "frame both freshly cooked Fast Meals products",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Product!.ThingID,
                fixture.Cookware.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 140);
        yield return new ScreenshotStep(
            "after ordinary Fast Meals cooking",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Product!.ThingID,
                fixture.Cookware.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 280);
        yield return new CheckpointStep(
            "Fast Meals native production result",
            _ => fixtures.ToDictionary(
                fixture => fixture.Name,
                fixture =>
                    "recipe=" + fixture.Recipe.defName +
                    "; work=" + fixture.Recipe.WorkAmountForStuff(fixture.Stove.Stuff) +
                    "; product=" + fixture.Product?.def.defName +
                    "; embeddedPlate=" +
                    fixture.Product?.GetComp<CompEmbeddedWare>()?.PeekPlateThing()?.Stuff?.defName +
                    "; cookwareDirty=" +
                    (fixture.Cookware.GetComp<CompSanitation>()?.IsDirty == true) +
                    "; rejected=" + string.Join(
                        ",",
                        fixture.RejectedPlates.Select(plate =>
                            (plate.Stuff?.defName ?? "missing") + ":" +
                            (plate.Spawned ? "spawned" : "unheld")))));
    }

    private static Fixture CreateFixture(
        Map map,
        IntVec3 center,
        string name,
        string recipeDefName,
        string productDefName,
        ThingDef expectedPlateStuff,
        IReadOnlyList<ThingDef> rejectedPlateStuffs)
    {
        BuildSealedRoom(map, center);
        var pawn = GenerateCook();
        pawn.Name = new NameSingle(name);
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
        pawn.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
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

        var recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName);
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(pawn);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var cookware = MakeCleanWare("ImmersiveChefs_Cookware", ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        var plate = MakeCleanWare("ImmersiveChefs_Plate", expectedPlateStuff);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        var rejectedPlates = new List<ThingWithComps>();
        for (var index = 0; index < rejectedPlateStuffs.Count; index++)
        {
            var rejected = MakeCleanWare("ImmersiveChefs_Plate", rejectedPlateStuffs[index]);
            GenSpawn.Spawn(rejected, center + (IntVec3.North * (index + 2)), map);
            rejectedPlates.Add(rejected);
        }

        SpawnIngredient(map, center + (IntVec3.East * 2), "RawRice", 20);
        if (recipeDefName.IndexOf("Deluxe", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SpawnIngredient(map, center + (IntVec3.East * 3), "Milk", 20);
        }

        return new Fixture(
            name,
            pawn,
            stove,
            recipe,
            DefDatabase<ThingDef>.GetNamed(productDefName),
            cookware,
            plate,
            rejectedPlates,
            expectedPlateStuff);
    }

    private static ThingWithComps MakeCleanWare(string defName, ThingDef stuff)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName),
            stuff);
        ware.GetComp<CompQuality>()?.SetQuality(
            QualityCategory.Normal,
            ArtGenerationContext.Colony);
        ware.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
        return ware;
    }

    private static void SpawnIngredient(Map map, IntVec3 cell, string defName, int count)
    {
        var ingredient = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
        ingredient.stackCount = count;
        GenSpawn.Spawn(ingredient, cell, map);
    }

    private static Pawn GenerateCook()
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable Fast Meals cook.");
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
        for (var x = -54; x <= 54; x += 18)
        {
            for (var z = -54; z <= 54; z += 18)
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

        throw new EndToEndAssertionException("Could not find two separated Fast Meals kitchens.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Walkable(map) ||
                    cell.GetEdifice(map) is not null)
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
            Thing stove,
            RecipeDef recipe,
            ThingDef productDef,
            ThingWithComps cookware,
            ThingWithComps plate,
            IReadOnlyList<ThingWithComps> rejectedPlates,
            ThingDef expectedPlateStuff)
        {
            Name = name;
            Pawn = pawn;
            Stove = stove;
            Recipe = recipe;
            ProductDef = productDef;
            Cookware = cookware;
            Plate = plate;
            RejectedPlates = rejectedPlates;
            ExpectedPlateStuff = expectedPlateStuff;
        }

        public string Name { get; }
        public Pawn Pawn { get; }
        public Thing Stove { get; }
        public RecipeDef Recipe { get; }
        public ThingDef ProductDef { get; }
        public ThingWithComps Cookware { get; }
        public ThingWithComps Plate { get; }
        public IReadOnlyList<ThingWithComps> RejectedPlates { get; }
        public ThingDef ExpectedPlateStuff { get; }
        public ThingWithComps? Product { get; private set; }
        public bool NativeBillObserved { get; private set; }

        public bool ObserveNativeBill()
        {
            NativeBillObserved |= Pawn.CurJobDef == JobDefOf.DoBill &&
                                  Pawn.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        public bool TryResolveCompletedProduct()
        {
            Product ??= Pawn.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ==
                    candidate.stackCount);
            return Product is not null;
        }
    }
}
