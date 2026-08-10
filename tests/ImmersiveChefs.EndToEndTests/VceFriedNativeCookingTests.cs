using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.vce-fried-native-cooking",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VCookE",
    "VanillaExpanded.VCookEBakery",
    "VanillaExpanded.VCookEHaute",
    "VanillaExpanded.VCookEStews",
    "VanillaExpanded.VCEF",
    "VanillaExpanded.VCookESushi",
    "ucp.friedmeals",
    "rabiosus.AdaptiveMealBill",
    "binchcannon.overcookedmeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 210)]
public sealed class VceFriedNativeCookingTest : IRimWorldEndToEndTest
{
    private readonly List<CookingFixture> cookingFixtures = new();
    private ExclusionFixture exclusionFixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        CompleteResearch(context, "VCE_DeepFrying", "VCE_Canning");
        var previousRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var previousAutoAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = previousRequirementMode;
            ImmersiveChefsMod.Settings.AutoCallAssistants = previousAutoAssistants;
        });

        var centers = FindRoomCenters(map, 5);
        foreach (var center in centers)
        {
            BuildSealedRoom(map, center);
        }

        var workTables = new[]
        {
            SpawnWorkTable(map, centers[0], "FueledStove"),
            SpawnWorkTable(map, centers[1], "FueledStove"),
            SpawnWorkTable(map, centers[2], "FueledStove"),
            SpawnWorkTable(map, centers[3], "VCE_DeepFrier"),
            SpawnWorkTable(map, centers[4], "VCE_CanningMachine")
        };
        cookingFixtures.Add(CreateCookingFixture(
            map, centers[0], workTables[0], "VCE Simple bake kitchen",
            "VCE_CookBakeSimple", "VCE_SimpleBake", ThingDefOf.WoodLog, null, 0.75f));
        cookingFixtures.Add(CreateCookingFixture(
            map, centers[1], workTables[1], "VCE Fine bake kitchen",
            "VCE_CookBakeFine", "VCE_FineBake", ThingDefOf.Steel, ThingDefOf.WoodLog, 2f));
        cookingFixtures.Add(CreateCookingFixture(
            map, centers[2], workTables[2], "VCE Lavish bake kitchen",
            "VCE_CookBakeLavish", "VCE_LavishBake", ThingDefOf.Silver, ThingDefOf.Steel, 3f));
        cookingFixtures.Add(CreateCookingFixture(
            map, centers[3], workTables[3], "Fried Gourmet kitchen",
            "VCE_CookFritterGourmet", "ucp_GourmetFritter", ThingDefOf.Silver, ThingDefOf.Steel, 3f));
        exclusionFixture = CreateExclusionFixture(map, centers[4], workTables[4]);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var initialTargets = cookingFixtures
            .SelectMany(fixture => fixture.VisibleFixtureThings)
            .Concat(exclusionFixture.VisibleFixtureThings)
            .Select(thing => thing.ThingID)
            .ToArray();
        yield return new SelectionActionStep(
            "select VCE and Fried native cooking fixtures",
            initialTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame VCE and Fried native cooking fixtures",
            initialTargets,
            paddingPixels: 100);
        yield return new ScreenshotStep(
            "before native VCE and Fried cooking",
            initialTargets,
            paddingPixels: 160);
        yield return new AssertionStep(
            "VCE and Fried recipes expose their Harmony-adjusted work amounts",
            _ =>
            {
                foreach (var fixture in cookingFixtures)
                {
                    fixture.AssertFinalizedWorkAmount();
                }
            });
        yield return new TimeControlActionStep(
            "run ordinary VCE Fried and canning bills",
            paused: false,
            EndToEndGameSpeed.Superfast);
        foreach (var expectedFixture in cookingFixtures)
        {
            yield return new WaitUntilStep(
                expectedFixture.Recipe.defName + " enters its native bill",
                _ =>
                {
                    foreach (var fixture in cookingFixtures)
                    {
                        fixture.ObserveNativeBill();
                    }

                    exclusionFixture.ObserveNativeBill();
                    return expectedFixture.NativeBillObserved;
                },
                new EndToEndDeadline(2_000, 7_500, TimeSpan.FromSeconds(70)));
        }

        yield return new WaitUntilStep(
            exclusionFixture.Recipe.defName + " enters its native bill",
            _ =>
            {
                foreach (var fixture in cookingFixtures)
                {
                    fixture.ObserveNativeBill();
                }

                return exclusionFixture.ObserveNativeBill();
            },
            new EndToEndDeadline(2_000, 7_500, TimeSpan.FromSeconds(70)));
        yield return new ScreenshotStep(
            "native VCE Fried and canning work in progress",
            cookingFixtures.SelectMany(fixture => new[]
                {
                    fixture.Pawn.ThingID,
                    fixture.WorkTable.ThingID
                })
                .Concat(new[]
                {
                    exclusionFixture.Pawn.ThingID,
                    exclusionFixture.WorkTable.ThingID
                })
                .ToArray(),
            paddingPixels: 180);
        yield return new WaitUntilStep(
            "all native VCE Fried and canning bills finish",
            _ => cookingFixtures.All(fixture => fixture.TryResolveCompletedProduct()) &&
                 exclusionFixture.TryResolveCompletedProduct(),
            new EndToEndDeadline(6_500, 28_000, TimeSpan.FromSeconds(190)));
        yield return new TimeControlActionStep(
            "pause after VCE Fried and canning production",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "VCE Fried and excluded canning products preserve their exact owners",
            _ =>
            {
                foreach (var fixture in cookingFixtures)
                {
                    fixture.AssertCompleted();
                }

                exclusionFixture.AssertCompleted();
            });

        foreach (var fixture in cookingFixtures)
        {
            yield return new SelectionActionStep(
                "select freshly cooked " + fixture.ProductDef.label,
                new[] { fixture.Product!.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame freshly cooked " + fixture.ProductDef.label,
                new[]
                {
                    fixture.Product!.ThingID,
                    fixture.Pawn.ThingID,
                    fixture.WorkTable.ThingID,
                    fixture.Cookware.ThingID
                },
                paddingPixels: 180);
            yield return new ScreenshotStep(
                "observe plated " + fixture.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);
            yield return new SelectionActionStep(
                "select returned cookware after " + fixture.ProductDef.label,
                new[] { fixture.Cookware.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe dirty cookware after " + fixture.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);
        }

        yield return new SelectionActionStep(
            "select excluded canned meat",
            new[] { exclusionFixture.Product!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame excluded canning result and untouched ware",
            new[]
            {
                exclusionFixture.Product!.ThingID,
                exclusionFixture.Pawn.ThingID,
                exclusionFixture.WorkTable.ThingID,
                exclusionFixture.ControlCookware.ThingID,
                exclusionFixture.ControlPlate.ThingID
            },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe excluded canned meat and untouched ware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "VCE Fried native cooking result",
            _ => cookingFixtures.ToDictionary(
                    fixture => fixture.Name,
                    fixture =>
                        "recipe=" + fixture.Recipe.defName +
                        "; nativeBillObserved=" + fixture.NativeBillObserved +
                        "; product=" + fixture.Product?.def.defName +
                        "; multiplier=" + RecipeWorkRuntime.MultiplierFor(fixture.Recipe) +
                        "; workAmount=" + fixture.Recipe.WorkAmountForStuff(null) +
                        "; servings=" + fixture.Product?.stackCount +
                        "; embeddedPlates=" +
                        fixture.Product?.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount +
                        "; plateStuff=" + string.Join(",",
                            fixture.Product?.GetComp<CompEmbeddedWare>()?.Bindings
                                .Select(binding => binding.StuffDefName) ?? Array.Empty<string?>()) +
                        "; cookwareDirty=" +
                        (fixture.Cookware.GetComp<CompSanitation>()?.IsDirty == true) +
                        "; rejectedRemaining=" + (fixture.RejectedPlate?.stackCount ?? 0))
                .Append(new KeyValuePair<string, string>(
                    "VCE canning exclusion",
                    "recipe=" + exclusionFixture.Recipe.defName +
                    "; nativeBillObserved=" + exclusionFixture.NativeBillObserved +
                    "; product=" + exclusionFixture.Product?.def.defName +
                    "; embeddedComp=" +
                    (exclusionFixture.Product?.GetComp<CompEmbeddedWare>() is not null) +
                    "; controlCookwareClean=" +
                    (exclusionFixture.ControlCookware.GetComp<CompSanitation>()?.IsDirty == false) +
                    "; controlPlateClean=" +
                    (exclusionFixture.ControlPlate.GetComp<CompSanitation>()?.IsDirty == false)))
                .ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static CookingFixture CreateCookingFixture(
        Map map,
        IntVec3 center,
        Thing workTable,
        string name,
        string recipeDefName,
        string productDefName,
        ThingDef plateStuff,
        ThingDef? rejectedPlateStuff,
        float expectedWorkMultiplier)
    {
        var pawn = GenerateCook(name);
        GenSpawn.Spawn(pawn, center + (IntVec3.South * 4), map);
        var recipe = DefDatabase<RecipeDef>.GetNamed(recipeDefName);
        var productDef = DefDatabase<ThingDef>.GetNamed(productDefName);
        var expectedServings = recipe.products
            .Where(product => product.thingDef == productDef)
            .Sum(product => product.count);
        EndToEndAssert.True(expectedServings > 0,
            recipeDefName + " must retain a positive " + productDefName + " product count.");
        var bill = CreateBill(recipe, pawn);
        ((IBillGiver)workTable).BillStack.AddBill(bill);

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

        SpawnRecipeIngredients(map, center, recipe, bill);
        return new CookingFixture(
            name, center, pawn, workTable, recipe, productDef, cookware, plate,
            rejectedPlate, plateStuff, expectedServings, expectedWorkMultiplier);
    }

    private static ExclusionFixture CreateExclusionFixture(
        Map map,
        IntVec3 center,
        Thing workTable)
    {
        var pawn = GenerateCook("VCE canning exclusion kitchen");
        GenSpawn.Spawn(pawn, center + (IntVec3.South * 4), map);
        var recipe = DefDatabase<RecipeDef>.GetNamed("VCE_CanMeats");
        var productDef = DefDatabase<ThingDef>.GetNamed("VCE_CannedMeat");
        var expectedCount = recipe.products
            .Where(product => product.thingDef == productDef)
            .Sum(product => product.count);
        var bill = CreateBill(recipe, pawn);
        ((IBillGiver)workTable).BillStack.AddBill(bill);
        SpawnRecipeIngredients(map, center, recipe, bill);

        var cookware = MakeCleanWare("ImmersiveChefs_Cookware", ThingDefOf.Steel, 1);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 2), map);
        var plate = MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel, 1);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 3), map);
        return new ExclusionFixture(
            center, pawn, workTable, recipe, productDef, expectedCount, cookware, plate);
    }

    private static Thing SpawnWorkTable(Map map, IntVec3 center, string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var table = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        table.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(table, center, map, Rot4.North);
        table.TryGetComp<CompRefuelable>()?.Refuel(999f);
        if (table.TryGetComp<CompPowerTrader>() is { } power)
        {
            var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
            for (var x = center.x - 4; x <= center.x + 4; x++)
            {
                var conduit = ThingMaker.MakeThing(conduitDef);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 3), map);
            }

            var powerCellDef = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
            var powerCell = ThingMaker.MakeThing(
                powerCellDef,
                powerCellDef.MadeFromStuff ? ThingDefOf.Steel : null);
            powerCell.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(powerCell, center + new IntVec3(4, 0, 3), map);
            if (table.TryGetComp<CompFlickable>() is { SwitchIsOn: false } flick)
            {
                flick.DoFlick();
            }

            map.powerNetManager.UpdatePowerNetsAndConnections_First();
            for (var tick = 0; tick <= 200 && !power.PowerOn; tick++)
            {
                Find.TickManager.DoSingleTick();
            }

            EndToEndAssert.True(power.PowerOn, defName + " must be powered before native cooking starts.");
        }

        return table;
    }

    private static Bill_Production CreateBill(RecipeDef recipe, Pawn pawn)
    {
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 9f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(pawn);
        return bill;
    }

    private static void SpawnRecipeIngredients(
        Map map,
        IntVec3 center,
        RecipeDef recipe,
        Bill_Production bill)
    {
        var cells = new Queue<IntVec3>(
            from x in Enumerable.Range(-5, 11)
            from z in Enumerable.Range(-5, 11)
            let cell = center + new IntVec3(x, 0, z)
            where (Math.Abs(x) >= 2 || Math.Abs(z) >= 2) &&
                  cell.GetThingList(map).All(thing => thing.def.category != ThingCategory.Building)
            select cell);
        foreach (var ingredient in recipe.ingredients)
        {
            var candidate = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def =>
                    def.category == ThingCategory.Item &&
                    def.EverHaulable &&
                    def.stackLimit > 1 &&
                    !def.defName.StartsWith("ImmersiveChefs_", StringComparison.Ordinal) &&
                    ingredient.filter.Allows(def) &&
                    (IsCanningContainer(recipe, def) ||
                     recipe.fixedIngredientFilter is null ||
                     recipe.fixedIngredientFilter.Allows(def)) &&
                    (IsCanningContainer(recipe, def) || bill.ingredientFilter.Allows(def)))
                .OrderBy(def => def.defName, StringComparer.Ordinal)
                .FirstOrDefault();
            EndToEndAssert.NotNull(candidate,
                recipe.defName + " must expose one spawnable Def for every ingredient slot.");
            var remaining = 200;
            while (remaining > 0)
            {
                EndToEndAssert.True(cells.Count > 0,
                    recipe.defName + " ingredients must fit inside its sealed kitchen.");
                var stack = ThingMaker.MakeThing(candidate!);
                stack.stackCount = Math.Min(remaining, candidate!.stackLimit);
                remaining -= stack.stackCount;
                GenSpawn.Spawn(stack, cells.Dequeue(), map);
            }
        }
    }

    private static bool IsCanningContainer(RecipeDef recipe, ThingDef ingredient)
    {
        return recipe.defName == "VCE_CanMeats" && ingredient == ThingDefOf.Steel;
    }

    private static ThingWithComps MakeCleanWare(string defName, ThingDef stuff, int stackCount)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName),
            stuff);
        ware.stackCount = stackCount;
        ware.GetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
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

        throw new EndToEndAssertionException("Could not generate a capable VCE/Fried cook.");
    }

    private static void CompleteResearch(IEndToEndContext context, params string[] researchDefNames)
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
            research.FinishProject(project, false, null, false);
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
        for (var x = -88; x <= 88; x += 22)
        {
            for (var z = -88; z <= 88; z += 22)
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

        throw new EndToEndAssertionException("Could not find five separated VCE/Fried kitchens.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) || !cell.Walkable(map) ||
                    map.roofGrid.Roofed(cell) || cell.GetThingList(map).Count != 0)
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
            Thing workTable,
            RecipeDef recipe,
            ThingDef productDef,
            ThingWithComps cookware,
            ThingWithComps plate,
            ThingWithComps? rejectedPlate,
            ThingDef expectedPlateStuff,
            int expectedServings,
            float expectedWorkMultiplier)
        {
            Name = name;
            Center = center;
            Pawn = pawn;
            WorkTable = workTable;
            Recipe = recipe;
            ProductDef = productDef;
            Cookware = cookware;
            Plate = plate;
            RejectedPlate = rejectedPlate;
            ExpectedPlateStuff = expectedPlateStuff;
            ExpectedServings = expectedServings;
            ExpectedWorkMultiplier = expectedWorkMultiplier;
            RejectedPlateInitialCount = rejectedPlate?.stackCount ?? 0;
        }

        public string Name { get; }
        public IntVec3 Center { get; }
        public Pawn Pawn { get; }
        public Thing WorkTable { get; }
        public RecipeDef Recipe { get; }
        public ThingDef ProductDef { get; }
        public ThingWithComps Cookware { get; }
        public ThingWithComps Plate { get; }
        public ThingWithComps? RejectedPlate { get; }
        public ThingDef ExpectedPlateStuff { get; }
        public int ExpectedServings { get; }
        public float ExpectedWorkMultiplier { get; }
        public int RejectedPlateInitialCount { get; }
        public ThingWithComps? Product { get; private set; }
        public bool NativeBillObserved { get; private set; }

        public IEnumerable<Thing> VisibleFixtureThings => new Thing?[]
            {
                Pawn, WorkTable, Cookware, Plate, RejectedPlate
            }
            .Where(thing => thing is not null)
            .Cast<Thing>();

        public bool ObserveNativeBill()
        {
            NativeBillObserved |= Pawn.CurJobDef == JobDefOf.DoBill && Pawn.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        public void AssertFinalizedWorkAmount()
        {
            var baseWorkAmount = Recipe.workAmount >= 0f
                ? Recipe.workAmount
                : Recipe.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake, null);
            var expectedWorkAmount = baseWorkAmount * ExpectedWorkMultiplier;
            var actualWorkAmount = Recipe.WorkAmountForStuff(null);
            EndToEndAssert.True(
                Math.Abs(actualWorkAmount - expectedWorkAmount) < 0.01f,
                Name + " must expose its exact Harmony-adjusted work amount; expected " +
                expectedWorkAmount + " from base " + baseWorkAmount +
                ", actual " + actualWorkAmount + ".");
        }

        public bool TryResolveCompletedProduct()
        {
            ObserveNativeBill();
            Product ??= Pawn.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.Position.DistanceToSquared(Center) <= 100 &&
                    candidate.stackCount == ExpectedServings &&
                    candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == ExpectedServings);
            return Product is not null && NativeBillObserved && Cookware.Spawned &&
                   Cookware.GetComp<CompSanitation>()?.IsDirty == true;
        }

        public void AssertCompleted()
        {
            EndToEndAssert.True(NativeBillObserved, Name + " must use RimWorld's ordinary DoBill job.");
            EndToEndAssert.NotNull(Product, Name + " must produce its configured final meal.");
            EndToEndAssert.Equal(ExpectedServings, Product!.stackCount,
                Name + " must retain the upstream product count.");
            var embedded = Product.GetComp<CompEmbeddedWare>();
            EndToEndAssert.Equal(ExpectedServings, embedded?.EmbeddedPlateCount ?? -1,
                Name + " must bind one physical plate per serving.");
            var bindings = embedded?.Bindings ?? Array.Empty<PlateBinding>();
            EndToEndAssert.Equal(ExpectedServings, bindings.Count,
                Name + " must retain one independently inspectable plate binding per serving.");
            EndToEndAssert.True(
                bindings.All(binding => binding.StuffDefName == ExpectedPlateStuff.defName),
                Name + " must use its exact eligible plate material tier for every serving.");
            EndToEndAssert.Equal(ExpectedServings,
                Product.GetComp<CompCulinaryState>()?.Servings.Count ?? -1,
                Name + " must create one culinary record per serving.");
            EndToEndAssert.True(Cookware.GetComp<CompSanitation>()?.IsDirty == true,
                Name + " must return its exact cookware dirty after active cooking.");
            if (RejectedPlate is not null)
            {
                EndToEndAssert.True(RejectedPlate.Spawned && !RejectedPlate.Destroyed,
                    Name + " must leave its lower-tier plate stack untouched.");
                EndToEndAssert.Equal(RejectedPlateInitialCount, RejectedPlate.stackCount,
                    Name + " must not consume even part of its lower-tier plate stack.");
            }
        }
    }

    private sealed class ExclusionFixture
    {
        public ExclusionFixture(
            IntVec3 center,
            Pawn pawn,
            Thing workTable,
            RecipeDef recipe,
            ThingDef productDef,
            int expectedCount,
            ThingWithComps controlCookware,
            ThingWithComps controlPlate)
        {
            Center = center;
            Pawn = pawn;
            WorkTable = workTable;
            Recipe = recipe;
            ProductDef = productDef;
            ExpectedCount = expectedCount;
            ControlCookware = controlCookware;
            ControlPlate = controlPlate;
        }

        public IntVec3 Center { get; }
        public Pawn Pawn { get; }
        public Thing WorkTable { get; }
        public RecipeDef Recipe { get; }
        public ThingDef ProductDef { get; }
        public int ExpectedCount { get; }
        public ThingWithComps ControlCookware { get; }
        public ThingWithComps ControlPlate { get; }
        public ThingWithComps? Product { get; private set; }
        public bool NativeBillObserved { get; private set; }

        public IEnumerable<Thing> VisibleFixtureThings =>
            new Thing[] { Pawn, WorkTable, ControlCookware, ControlPlate };

        public bool ObserveNativeBill()
        {
            NativeBillObserved |= Pawn.CurJobDef == JobDefOf.DoBill && Pawn.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        public bool TryResolveCompletedProduct()
        {
            ObserveNativeBill();
            Product ??= Pawn.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.Position.DistanceToSquared(Center) <= 100 &&
                    candidate.stackCount == ExpectedCount);
            return Product is not null && NativeBillObserved;
        }

        public void AssertCompleted()
        {
            EndToEndAssert.True(NativeBillObserved,
                "VCE canning must use RimWorld's ordinary DoBill job.");
            EndToEndAssert.NotNull(Product, "VCE canning must produce its upstream canned meat.");
            EndToEndAssert.Equal(ExpectedCount, Product!.stackCount,
                "VCE canning must retain its upstream product count.");
            EndToEndAssert.True(Product.GetComp<CompEmbeddedWare>() is null,
                "Canned meat must not receive embedded ware.");
            EndToEndAssert.True(Product.GetComp<CompCulinaryState>() is null,
                "Canned meat must not receive culinary state.");
            EndToEndAssert.True(ControlCookware.Spawned &&
                                ControlCookware.GetComp<CompSanitation>()?.IsDirty == false,
                "The excluded canning bill must leave control cookware clean and untouched.");
            EndToEndAssert.True(ControlPlate.Spawned &&
                                ControlPlate.GetComp<CompSanitation>()?.IsDirty == false,
                "The excluded canning bill must leave the control plate clean and untouched.");
        }
    }
}
