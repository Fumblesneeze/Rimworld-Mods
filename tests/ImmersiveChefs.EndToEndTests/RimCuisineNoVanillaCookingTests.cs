using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.rimcuisine-no-vanilla-native-cooking",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Mlie.RC2.Core",
    "Mlie.RC2.MaME",
    "Mlie.RC2.BaBE",
    "Mlie.RC2.SaSE",
    "Mlie.NoVanillaMeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 150)]
public sealed class RimCuisineNoVanillaCookingTest : IRimWorldEndToEndTest
{
    private readonly List<CookingFixture> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        CompleteResearch(context, "RC2_BasicCooking", "RC2_AdvancedCooking");
        var previousRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var previousAutoAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = previousRequirementMode;
            ImmersiveChefsMod.Settings.AutoCallAssistants = previousAutoAssistants;
        });

        var centers = FindRoomCenters(map, 2);
        fixtures.Add(CreateFixture(
            map,
            centers[0],
            "Thin pottage kitchen",
            "CookThinPottage",
            "RC2_ThinPottage",
            ThingDefOf.WoodLog,
            rejectedPlateStuff: null));
        fixtures.Add(CreateFixture(
            map,
            centers[1],
            "Extravagant meal kitchen",
            "RC2_CookExtravagantMeal",
            "RC2_ExtravagantMeal",
            ThingDefOf.Silver,
            ThingDefOf.Steel));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var initialTargets = fixtures
            .SelectMany(fixture => fixture.VisibleFixtureThings.Select(thing => thing.ThingID))
            .ToArray();
        yield return new SelectionActionStep(
            "select both RimCuisine cooking fixtures",
            initialTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame both RimCuisine kitchens",
            initialTargets,
            paddingPixels: 120);
        yield return new ScreenshotStep(
            "before native RimCuisine cooking",
            initialTargets,
            paddingPixels: 180);
        yield return new TimeControlActionStep(
            "run ordinary RimCuisine Cooking work",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "both cooks enter their native RimCuisine bills",
            _ => fixtures.All(fixture => fixture.ObserveNativeBill()),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(50)));
        yield return new ScreenshotStep(
            "native RimCuisine cooking in progress",
            fixtures.SelectMany(fixture => new[]
            {
                fixture.Pawn.ThingID,
                fixture.Stove.ThingID
            }).ToArray(),
            paddingPixels: 220);
        yield return new WaitUntilStep(
            "both RimCuisine bills finish plated products and dirty cookware",
            _ => fixtures.All(fixture => fixture.TryResolveCompletedProduct()),
            new EndToEndDeadline(4_200, 16_000, TimeSpan.FromSeconds(120)));
        yield return new TimeControlActionStep(
            "pause after RimCuisine production",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "RimCuisine products retain the intended service tier and cooking lifecycle",
            _ =>
            {
                foreach (var fixture in fixtures)
                {
                    fixture.AssertCompleted();
                }
            });

        foreach (var fixture in fixtures)
        {
            yield return new SelectionActionStep(
                "select the freshly cooked " + fixture.ProductDef.label,
                new[] { fixture.Product!.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame the freshly cooked " + fixture.ProductDef.label,
                new[]
                {
                    fixture.Product!.ThingID,
                    fixture.Pawn.ThingID,
                    fixture.Stove.ThingID,
                    fixture.Cookware.ThingID
                },
                paddingPixels: 220);
            yield return new ScreenshotStep(
                "observe plated " + fixture.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);
            yield return new SelectionActionStep(
                "select returned cookware for " + fixture.ProductDef.label,
                new[] { fixture.Cookware.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe dirty cookware after " + fixture.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);
        }

        yield return new CheckpointStep(
            "RimCuisine native cooking result",
            _ => fixtures.ToDictionary(
                fixture => fixture.Name,
                fixture =>
                    "recipe=" + fixture.Recipe.defName +
                    "; nativeBillObserved=" + fixture.NativeBillObserved +
                    "; product=" + fixture.Product?.def.defName +
                    "; servings=" + fixture.Product?.stackCount +
                    "; embeddedPlates=" +
                    fixture.Product?.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount +
                    "; plateStuff=" +
                    fixture.Product?.GetComp<CompEmbeddedWare>()?.PeekPlateThing()?.Stuff?.defName +
                    "; cookwareDirty=" +
                    (fixture.Cookware.GetComp<CompSanitation>()?.IsDirty == true) +
                    "; ingredients=" + string.Join(",", fixture.IngredientDefs.Select(def => def.defName)) +
                    "; rejectedPlateSpawned=" +
                    (fixture.RejectedPlate?.Spawned.ToString() ?? "not-arranged")));
    }

    private static CookingFixture CreateFixture(
        Map map,
        IntVec3 center,
        string name,
        string recipeDefName,
        string productDefName,
        ThingDef plateStuff,
        ThingDef? rejectedPlateStuff)
    {
        BuildSealedRoom(map, center);
        var pawn = GenerateCook(name);
        GenSpawn.Spawn(pawn, center + (IntVec3.South * 4), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        var recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName);
        var productDef = DefDatabase<ThingDef>.GetNamed(productDefName);
        var expectedServings = recipe.products
            .Where(product => product.thingDef == productDef)
            .Sum(product => product.count);
        EndToEndAssert.True(expectedServings > 0,
            recipeDefName + " must retain a positive " + productDefName + " product count.");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 9f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(pawn);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var cookware = MakeCleanWare("ImmersiveChefs_Cookware", ThingDefOf.Steel, 1);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 2), map);
        var plate = MakeCleanWare("ImmersiveChefs_Plate", plateStuff, expectedServings);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 3), map);
        ThingWithComps? rejectedPlate = null;
        if (rejectedPlateStuff is not null)
        {
            rejectedPlate = MakeCleanWare(
                "ImmersiveChefs_Plate",
                rejectedPlateStuff,
                expectedServings);
            GenSpawn.Spawn(rejectedPlate, center + (IntVec3.North * 3), map);
        }

        var ingredientDefs = SpawnRecipeIngredients(map, center, recipe, bill);
        return new CookingFixture(
            name,
            center,
            pawn,
            stove,
            recipe,
            productDef,
            cookware,
            plate,
            rejectedPlate,
            plateStuff,
            expectedServings,
            ingredientDefs);
    }

    private static IReadOnlyList<ThingDef> SpawnRecipeIngredients(
        Map map,
        IntVec3 center,
        RecipeDef recipe,
        Bill_Production bill)
    {
        var cells = new Queue<IntVec3>(
            from x in Enumerable.Range(-4, 9)
            from z in Enumerable.Range(-4, 9)
            let offset = new IntVec3(x, 0, z)
            where Math.Abs(x) >= 2 || Math.Abs(z) >= 2
            select center + offset);
        var selected = new List<ThingDef>();
        foreach (var ingredient in recipe.ingredients)
        {
            var candidate = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def =>
                    def.category == ThingCategory.Item &&
                    def.EverHaulable &&
                    def.stackLimit > 1 &&
                    def.ingestible is not null &&
                    !def.defName.StartsWith("ImmersiveChefs_", StringComparison.Ordinal) &&
                    ingredient.filter.Allows(def) &&
                    (recipe.fixedIngredientFilter is null || recipe.fixedIngredientFilter.Allows(def)) &&
                    bill.ingredientFilter.Allows(def) &&
                    recipe.IngredientValueGetter.ValuePerUnitOf(def) > 0f)
                .OrderBy(def => def.defName.StartsWith("RC2_", StringComparison.Ordinal) ? 1 : 0)
                .ThenBy(def => def.defName, StringComparer.Ordinal)
                .FirstOrDefault();
            EndToEndAssert.NotNull(candidate,
                recipe.defName + " must expose a spawnable ingredient for each finalized ingredient filter.");
            selected.Add(candidate!);

            var remaining = 200;
            while (remaining > 0)
            {
                EndToEndAssert.True(cells.Count > 0,
                    recipe.defName + " ingredient stacks must fit inside its sealed kitchen.");
                var stack = ThingMaker.MakeThing(candidate!);
                stack.stackCount = Math.Min(remaining, candidate!.stackLimit);
                remaining -= stack.stackCount;
                GenSpawn.Spawn(stack, cells.Dequeue(), map);
            }
        }

        return selected;
    }

    private static ThingWithComps MakeCleanWare(
        string defName,
        ThingDef stuff,
        int stackCount)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName),
            stuff);
        ware.stackCount = stackCount;
        ware.GetComp<CompQuality>()?.SetQuality(
            QualityCategory.Normal,
            ArtGenerationContext.Colony);
        ware.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
        return ware;
    }

    private static Pawn GenerateCook(string name)
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
            if (pawn.WorkTypeIsDisabled(cooking) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            HumanlikePawnFixture.SetName(pawn, name);
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(cooking, 1);
            pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
            for (var hour = 0; hour < 24; hour++)
            {
                pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
            }

            if (pawn.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            if (pawn.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable RimCuisine cook.");
    }

    private static void CompleteResearch(
        IEndToEndContext context,
        params string[] researchDefNames)
    {
        var progressField = typeof(ResearchManager).GetField(
            "progress",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new EndToEndAssertionException("Could not resolve RimWorld's research progress store.");
        var research = Find.ResearchManager;
        var progress = (Dictionary<ResearchProjectDef, float>)progressField.GetValue(research);
        foreach (var defName in researchDefNames)
        {
            var project = DefDatabase<ResearchProjectDef>.GetNamed(defName);
            if (project.IsFinished)
            {
                continue;
            }

            var hadProgress = progress.TryGetValue(project, out var originalProgress);
            research.FinishProject(
                project,
                doCompletionDialog: false,
                researcher: null,
                doCompletionLetter: false);
            context.DeferCleanup(() =>
            {
                if (hadProgress)
                {
                    progress[project] = originalProgress;
                }
                else
                {
                    progress.Remove(project);
                }
            });
        }
    }

    private static void BuildSealedRoom(Map map, IntVec3 center)
    {
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        for (var offset = -6; offset <= 6; offset++)
        {
            SpawnWall(map, center + new IntVec3(offset, 0, -6), granite);
            SpawnWall(map, center + new IntVec3(offset, 0, 6), granite);
            if (offset is -6 or 6)
            {
                continue;
            }

            SpawnWall(map, center + new IntVec3(-6, 0, offset), granite);
            SpawnWall(map, center + new IntVec3(6, 0, offset), granite);
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
        for (var x = -72; x <= 72; x += 22)
        {
            for (var z = -72; z <= 72; z += 22)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (!candidate.InBounds(map) ||
                    centers.Any(center => center.DistanceToSquared(candidate) < 400) ||
                    !SquareIsUsable(map, candidate, 7))
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

        throw new EndToEndAssertionException("Could not find two separated RimCuisine kitchens.");
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
                    map.roofGrid.Roofed(cell) ||
                    cell.GetThingList(map).Count != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class CookingFixture
    {
        public CookingFixture(
            string name,
            IntVec3 center,
            Pawn pawn,
            Thing stove,
            RecipeDef recipe,
            ThingDef productDef,
            ThingWithComps cookware,
            ThingWithComps plate,
            ThingWithComps? rejectedPlate,
            ThingDef expectedPlateStuff,
            int expectedServings,
            IReadOnlyList<ThingDef> ingredientDefs)
        {
            Name = name;
            Center = center;
            Pawn = pawn;
            Stove = stove;
            Recipe = recipe;
            ProductDef = productDef;
            Cookware = cookware;
            Plate = plate;
            RejectedPlate = rejectedPlate;
            ExpectedPlateStuff = expectedPlateStuff;
            ExpectedServings = expectedServings;
            IngredientDefs = ingredientDefs;
        }

        public string Name { get; }
        public IntVec3 Center { get; }
        public Pawn Pawn { get; }
        public Thing Stove { get; }
        public RecipeDef Recipe { get; }
        public ThingDef ProductDef { get; }
        public ThingWithComps Cookware { get; }
        public ThingWithComps Plate { get; }
        public ThingWithComps? RejectedPlate { get; }
        public ThingDef ExpectedPlateStuff { get; }
        public int ExpectedServings { get; }
        public IReadOnlyList<ThingDef> IngredientDefs { get; }
        public ThingWithComps? Product { get; private set; }
        public bool NativeBillObserved { get; private set; }

        public IEnumerable<Thing> VisibleFixtureThings => new Thing?[]
            {
                Pawn,
                Stove,
                Cookware,
                Plate,
                RejectedPlate
            }
            .Where(thing => thing is not null)
            .Cast<Thing>();

        public bool ObserveNativeBill()
        {
            NativeBillObserved |= Pawn.CurJobDef == JobDefOf.DoBill &&
                                  Pawn.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        public bool TryResolveCompletedProduct()
        {
            NativeBillObserved |= Pawn.CurJobDef == JobDefOf.DoBill &&
                                  Pawn.CurJob?.RecipeDef == Recipe;
            Product ??= Pawn.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.Position.DistanceToSquared(Center) <= 100 &&
                    candidate.stackCount == ExpectedServings &&
                    candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == ExpectedServings);
            return Product is not null &&
                   NativeBillObserved &&
                   Cookware.Spawned &&
                   Cookware.GetComp<CompSanitation>()?.IsDirty == true;
        }

        public void AssertCompleted()
        {
            EndToEndAssert.True(NativeBillObserved,
                Name + " must be performed by RimWorld's ordinary DoBill job.");
            EndToEndAssert.NotNull(Product,
                Name + " must produce its configured RimCuisine meal.");
            EndToEndAssert.Equal(ExpectedServings, Product!.stackCount,
                Name + " must retain the upstream product serving count.");
            var embedded = Product.GetComp<CompEmbeddedWare>();
            EndToEndAssert.NotNull(embedded,
                Name + " must receive finalized embedded service ware.");
            EndToEndAssert.Equal(ExpectedServings, embedded!.EmbeddedPlateCount,
                Name + " must bind one physical plate per upstream serving.");
            EndToEndAssert.Equal(
                ExpectedPlateStuff.defName,
                embedded.PeekPlateThing()?.Stuff?.defName,
                Name + " must bind its eligible plate material tier.");
            EndToEndAssert.Equal(
                ExpectedServings,
                Product.GetComp<CompCulinaryState>()?.Servings.Count ?? -1,
                Name + " must create one culinary record per upstream serving.");
            EndToEndAssert.True(Cookware.Spawned,
                Name + " must return its exact cookware to the kitchen.");
            EndToEndAssert.True(Cookware.GetComp<CompSanitation>()?.IsDirty == true,
                Name + " must return cookware dirty after real cooking work.");
            if (RejectedPlate is not null)
            {
                EndToEndAssert.True(RejectedPlate.Spawned && !RejectedPlate.Destroyed,
                    Name + " must leave its lower-tier plate stack untouched.");
                EndToEndAssert.False(ReferenceEquals(embedded.PeekPlateThing(), RejectedPlate),
                    Name + " must not bind its exact rejected lower-tier plate stack.");
            }
        }
    }
}
