using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-mapped-meal-lifecycle",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_000,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class DmtrMappedMealLifecycleTest : IRimWorldEndToEndTest
{
    private MealTextureMappedLifecycleFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureMappedLifecycleFixture.Create(
            context,
            "DMTR",
            ("CookMealSimple", "MealSimple", "DynamicMealTextureReplacer.Graphic_IngredientsVariant"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.ftv-mapped-meal-lifecycle",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_000,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class FtvMappedMealLifecycleTest : IRimWorldEndToEndTest
{
    private MealTextureMappedLifecycleFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureMappedLifecycleFixture.Create(
            context,
            "FTV",
            ("FTV_CookMealSimple", "FTV_MealSimple", "FoodTextureVariety.Graphic_MealVariantsExpanded"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-ftv-mapped-meal-lifecycle",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 12_000,
    MaxGameTicks = 48_000,
    MaxWallClockSeconds = 480)]
public sealed class DmtrFtvMappedMealLifecycleTest : IRimWorldEndToEndTest
{
    private MealTextureMappedLifecycleFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureMappedLifecycleFixture.Create(
            context,
            "DMTR and FTV",
            ("CookMealSimple", "MealSimple", "DynamicMealTextureReplacer.Graphic_IngredientsVariant"),
            ("FTV_CookMealSimple", "FTV_MealSimple", "FoodTextureVariety.Graphic_MealVariantsExpanded"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

internal sealed class MealTextureMappedLifecycleFixture
{
    private const string DmtrOwner = "DynamicMealTextureReplacer.Graphic_IngredientsVariant";
    private const string FtvOwner = "FoodTextureVariety.Graphic_MealVariantsExpanded";
    private readonly string groupName;
    private readonly IReadOnlyList<MappedMealCase> cases;
    private readonly ThingDef plateDef;
    private readonly ThingDef cutleryDef;

    private MealTextureMappedLifecycleFixture(
        string groupName,
        IReadOnlyList<MappedMealCase> cases,
        ThingDef plateDef,
        ThingDef cutleryDef)
    {
        this.groupName = groupName;
        this.cases = cases;
        this.plateDef = plateDef;
        this.cutleryDef = cutleryDef;
    }

    internal static MealTextureMappedLifecycleFixture Create(
        IEndToEndContext context,
        string groupName,
        params (string RecipeDefName, string ProductDefName, string GraphicOwner)[] specs)
    {
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        PreserveThermalSettings(context);
        if (specs.Any(spec => spec.GraphicOwner == FtvOwner))
        {
            PreserveStrictFtvIngredientSelection(context);
        }

        var map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, specs.Length);
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var cases = new List<MappedMealCase>();

        for (var index = 0; index < specs.Length; index++)
        {
            var spec = specs[index];
            var center = centers[index];
            FoodSearchE2EFixture.BuildSealedRoom(map, center);

            DispenserE2EFixture.SpawnConduitGrid(map, center, 5, 5);
            DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(4, 0, 4), 1);

            var cook = GenerateCook(groupName + " mapped cook " + (index + 1));
            FoodSearchE2EFixture.SetHunger(cook, 1f);
            GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);

            var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, center, map, Rot4.North);
            stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

            var recipe = DefDatabase<RecipeDef>.GetNamed(spec.RecipeDefName);
            var bill = new Bill_Production(recipe)
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1,
                ingredientSearchRadius = 8f
            };
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            bill.SetPawnRestriction(cook);
            ((IBillGiver)stove).BillStack.AddBill(bill);

            var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
            rice.stackCount = 20;
            GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

            var cookware = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cookware",
                ThingDefOf.Steel);
            var plate = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Plate",
                ThingDefOf.Steel);
            var cutlery = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cutlery",
                ThingDefOf.Steel);
            GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
            GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
            GenSpawn.Spawn(cutlery, center + (IntVec3.West * 1), map);

            var table = (Building)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("Table1x2c"),
                ThingDefOf.Steel);
            table.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(table, center + (IntVec3.North * 3), map, Rot4.North);
            var microwaveDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");
            var microwaveCell = table.OccupiedRect().Cells.OrderBy(cell => cell.z).First();
            var placeWorker = new PlaceWorker_MicrowaveCountertop();
            var rotation = new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
                .First(candidate => placeWorker.AllowsPlacing(
                    microwaveDef,
                    microwaveCell,
                    candidate,
                    map).Accepted);
            var microwave = (Building_Microwave)ThingMaker.MakeThing(microwaveDef);
            microwave.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(microwave, microwaveCell, map, rotation);
            DispenserE2EFixture.SettlePower(map, new ThingWithComps[] { microwave }, 200);
            EndToEndAssert.True(microwave.GetComp<CompMicrowave>().Operational,
                "The mapped-meal fixture microwave must begin powered and supported.");

            EndToEndAssert.Equal(0, cook.workSettings.GetPriority(cooking),
                "Later mapped-meal cases must remain inert until activated.");
            cases.Add(new MappedMealCase(
                cook,
                stove,
                recipe,
                DefDatabase<ThingDef>.GetNamed(spec.ProductDefName),
                spec.GraphicOwner,
                rice,
                cookware,
                plate,
                cutlery,
                microwave));
        }

        return new MealTextureMappedLifecycleFixture(groupName, cases, plateDef, cutleryDef);
    }

    internal IEnumerable<EndToEndStep> Execute(IEndToEndContext context)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        foreach (var mappedCase in cases)
        {
            yield return new TimeControlActionStep(
                "pause before mapped rice bill for " + mappedCase.ProductDef.label,
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new SelectionActionStep(
                "select mapped rice ingredient for " + mappedCase.ProductDef.label,
                new[] { mappedCase.Rice.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame mapped rice kitchen for " + mappedCase.ProductDef.label,
                new[]
                {
                    mappedCase.Cook.ThingID,
                    mappedCase.Stove.ThingID,
                    mappedCase.Rice.ThingID,
                    mappedCase.Microwave.ThingID
                },
                paddingPixels: 170);

            mappedCase.Cook.workSettings.SetPriority(cooking, 1);
            mappedCase.Cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
            yield return new TimeControlActionStep(
                "run native mapped rice bill for " + mappedCase.ProductDef.label,
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "cook enters native mapped rice DoBill for " + mappedCase.ProductDef.defName,
                _ => mappedCase.ObserveNativeBill(),
                new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
            yield return new ScreenshotStep(
                "observe native mapped rice cooking for " + mappedCase.ProductDef.label,
                new[] { mappedCase.Cook.ThingID, mappedCase.Stove.ThingID },
                paddingPixels: 180);
            yield return new TimeControlActionStep(
                "finish native mapped rice cooking for " + mappedCase.ProductDef.label,
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "mapped rice " + mappedCase.ProductDef.defName + " is plated",
                _ => mappedCase.TryResolveProduct(),
                new EndToEndDeadline(1_200, 6_000, TimeSpan.FromSeconds(45)));
            yield return new TimeControlActionStep(
                "pause after mapped rice cooking for " + mappedCase.ProductDef.label,
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "mapped rice owner provenance and plate survive cooking",
                _ => AssertCookedState(mappedCase));
            yield return new SelectionActionStep(
                "select steaming mapped rice " + mappedCase.ProductDef.label,
                new[] { mappedCase.Product!.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame steaming mapped rice " + mappedCase.ProductDef.label,
                new[] { mappedCase.Product!.ThingID, mappedCase.Microwave.ThingID },
                paddingPixels: 190);
            yield return new ScreenshotStep(
                "observe steaming mapped rice texture for " + mappedCase.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);

            mappedCase.Cook.workSettings.SetPriority(cooking, 0);
            yield return new TimeControlActionStep(
                "cool mapped rice " + mappedCase.ProductDef.label + " through game time",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "mapped rice " + mappedCase.ProductDef.defName + " cools below microwave threshold",
                _ => mappedCase.CurrentTemperature() < 50f,
                new EndToEndDeadline(300, 1_000, TimeSpan.FromSeconds(15)));
            yield return new TimeControlActionStep(
                "pause cooled mapped rice " + mappedCase.ProductDef.label,
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "cooled meal retains exact mapped rice visual",
                _ => AssertMappedVisualUnchanged(mappedCase, "cooling"));
            yield return new ScreenshotStep(
                "observe cooled mapped rice texture for " + mappedCase.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);

            FoodSearchE2EFixture.SetHunger(mappedCase.Cook, 0.10f);
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                .Query(mappedCase.Cook.ThingID, mappedCase.Product!.ThingID);
            var consume = options.Where(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            EndToEndAssert.Equal(1, consume.Length,
                "Expected one enabled native Consume option for mapped " +
                mappedCase.ProductDef.defName + "; observed " +
                string.Join(", ", options.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})")));
            yield return new FloatMenuActionStep(
                "order native reheat and consumption of mapped " + mappedCase.ProductDef.label,
                mappedCase.Cook.ThingID,
                mappedCase.Product!.ThingID,
                consume[0].StableId);
            yield return new TimeControlActionStep(
                "run native mapped-meal reheat path",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "mapped meal reaches its captured microwave",
                _ => mappedCase.ObserveAtMicrowave(),
                new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
            yield return new SelectionActionStep(
                "select mapped-meal cook at microwave",
                new[] { mappedCase.Cook.ThingID, mappedCase.Microwave.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame native mapped-meal reheat",
                new[] { mappedCase.Cook.ThingID, mappedCase.Microwave.ThingID },
                paddingPixels: 180);
            yield return new ScreenshotStep(
                "observe native microwave work on mapped " + mappedCase.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);
            yield return new WaitUntilStep(
                "mapped rice serving completes one native microwave cycle",
                _ => mappedCase.ReheatCompleted(),
                new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
            yield return new TimeControlActionStep(
                "pause after mapped rice microwave cycle",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "reheated meal retains exact mapped rice visual",
                _ => AssertReheatedState(mappedCase));
            yield return new SelectionActionStep(
                "select cook carrying reheated mapped meal",
                new[] { mappedCase.Cook.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe reheated mapped meal continuing native ingestion",
                Array.Empty<string>(),
                paddingPixels: 0);

            yield return new TimeControlActionStep(
                "finish native mapped-meal ingestion",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "mapped meal returns its exact dirty setting",
                _ => mappedCase.Product!.Destroyed &&
                     mappedCase.Plate.Spawned &&
                     mappedCase.Cutlery.Spawned,
                new EndToEndDeadline(1_200, 6_000, TimeSpan.FromSeconds(45)));
            yield return new TimeControlActionStep(
                "pause after mapped-meal ingestion",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "mapped-meal ingestion conserves exact dirty ware once",
                _ => AssertConsumedState(mappedCase));
            yield return new SelectionActionStep(
                "select returned mapped-meal plate",
                new[] { mappedCase.Plate.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame returned mapped-meal setting",
                new[] { mappedCase.Plate.ThingID, mappedCase.Cutlery.ThingID },
                paddingPixels: 150);
            yield return new ScreenshotStep(
                "observe exact dirty plate after mapped-meal dining",
                Array.Empty<string>(),
                paddingPixels: 0);
            yield return new SelectionActionStep(
                "select returned mapped-meal cutlery",
                new[] { mappedCase.Cutlery.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe exact dirty cutlery after mapped-meal dining",
                Array.Empty<string>(),
                paddingPixels: 0);
        }

        yield return new CheckpointStep(
            groupName + " mapped-meal lifecycle result",
            _ => cases.ToDictionary(
                mappedCase => mappedCase.ProductDef.defName,
                mappedCase =>
                    "owner=" + mappedCase.GraphicOwner +
                    "; visual=" + mappedCase.MappedVisualKey +
                    "; nativeBill=" + mappedCase.NativeBillObserved +
                    "; microwave=" + mappedCase.MicrowaveObserved +
                    "; plateId=" + mappedCase.Plate.ThingID +
                    "; cutleryId=" + mappedCase.Cutlery.ThingID));
    }

    private void AssertCookedState(MappedMealCase mappedCase)
    {
        var product = mappedCase.Product!;
        EndToEndAssert.True(mappedCase.NativeBillObserved,
            product.def.defName + " must be made through RimWorld's ordinary DoBill job.");
        EndToEndAssert.True(mappedCase.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "Native mapped cooking must leave the exact cookware dirty.");
        AssertExactRiceProvenance(product, "Native cooking");
        EndToEndAssert.Equal(mappedCase.GraphicOwner, product.Graphic.GetType().FullName,
            product.def.defName + " must retain its finalized upstream graphic owner.");

        mappedCase.MappedVisualKey = ResolveMappedVisualKey(product, mappedCase.GraphicOwner);
        EndToEndAssert.True(!string.IsNullOrWhiteSpace(mappedCase.MappedVisualKey),
            "The upstream owner must resolve a concrete rice-mapped visual.");
        var serving = product.GetComp<CompCulinaryState>()!.PeekCurrentServing()!;
        mappedCase.QualityBeforeReheat = serving.QualityScore;
        EndToEndAssert.True(serving.TemperatureCelsius >= 55f,
            "The newly cooked mapped serving must begin visibly steaming hot.");

        var embedded = product.GetComp<CompEmbeddedWare>()!;
        EndToEndAssert.Equal(1, embedded.EmbeddedPlateCount,
            "The mapped serving must contain exactly one physical plate.");
        EndToEndAssert.True(ReferenceEquals(mappedCase.Plate, embedded.PeekPlateThing()),
            "The mapped serving must contain the exact reserved plate Thing.");
    }

    private static void AssertMappedVisualUnchanged(MappedMealCase mappedCase, string stage)
    {
        var product = mappedCase.Product!;
        AssertExactRiceProvenance(product, stage);
        EndToEndAssert.Equal(mappedCase.GraphicOwner, product.Graphic.GetType().FullName,
            stage + " must not replace the upstream graphic owner.");
        EndToEndAssert.Equal(
            mappedCase.MappedVisualKey,
            ResolveMappedVisualKey(product, mappedCase.GraphicOwner),
            stage + " must preserve the exact upstream rice-mapped visual.");
    }

    private static void AssertReheatedState(MappedMealCase mappedCase)
    {
        EndToEndAssert.True(mappedCase.MicrowaveObserved,
            "The pawn must visibly reach the captured microwave during its native ingest job.");
        var serving = mappedCase.Product!.GetComp<CompCulinaryState>()!
            .PeekCurrentServingWithoutThermalUpdate()!;
        EndToEndAssert.Equal(1, serving.MicrowaveReheatCount,
            "The mapped serving must complete exactly one microwave cycle.");
        EndToEndAssert.Equal(
            Math.Max(0, mappedCase.QualityBeforeReheat - ImmersiveChefsMod.Settings.MicrowaveQualityLoss),
            serving.QualityScore,
            "The native microwave must apply its configured quality loss exactly once.");
        EndToEndAssert.True(serving.TemperatureCelsius >= 55f,
            "The native microwave must restore the mapped meal to a steaming-hot band.");
        AssertMappedVisualUnchanged(mappedCase, "Native reheating");
        EndToEndAssert.True(ReferenceEquals(
                mappedCase.Plate,
                mappedCase.Product.GetComp<CompEmbeddedWare>()!.PeekPlateThing()),
            "Native reheating must retain the exact embedded plate.");
    }

    private void AssertConsumedState(MappedMealCase mappedCase)
    {
        EndToEndAssert.True(mappedCase.Product!.Destroyed,
            "The exact reheated mapped meal must be consumed.");
        EndToEndAssert.True(mappedCase.Plate.Spawned &&
                            mappedCase.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact mapped-meal plate must return dirty.");
        EndToEndAssert.True(mappedCase.Cutlery.Spawned &&
                            mappedCase.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact mapped-meal cutlery must return dirty.");
        EndToEndAssert.Equal(1, mappedCase.Plate.stackCount,
            "The returned exact plate must remain one physical unit.");
        EndToEndAssert.Equal(1, mappedCase.Cutlery.stackCount,
            "The returned exact cutlery must remain one physical unit.");
        EndToEndAssert.Equal(cases.Count, CountAllServiceWare(plateDef),
            "Mapped cooking and dining must neither duplicate nor lose a plate.");
        EndToEndAssert.Equal(cases.Count, CountAllServiceWare(cutleryDef),
            "Mapped dining must neither duplicate nor lose a cutlery setting.");
    }

    private int CountAllServiceWare(ThingDef def)
    {
        var map = Current.Game.CurrentMap;
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var held = cases.Sum(mappedCase =>
            (mappedCase.Cook.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (mappedCase.Cook.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = def == plateDef
            ? cases.Sum(mappedCase =>
                mappedCase.Product is { Destroyed: false }
                    ? mappedCase.Product.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
                    : 0)
            : 0;
        return spawned + held + embedded;
    }

    private static void AssertExactRiceProvenance(ThingWithComps product, string stage)
    {
        var ingredients = product.GetComp<CompIngredients>()!.ingredients;
        EndToEndAssert.Equal(1, ingredients.Count,
            stage + " must preserve exactly one public ingredient Def.");
        EndToEndAssert.Equal("RawRice", ingredients[0].defName,
            stage + " must preserve RawRice as the sole texture-facing ingredient.");
    }

    private static string ResolveMappedVisualKey(ThingWithComps product, string owner)
    {
        if (owner == FtvOwner)
        {
            var graphic = ((Graphic_StackCount)product.Graphic).SubGraphicFor(product);
            EndToEndAssert.True(
                graphic.path.IndexOf("rice", StringComparison.OrdinalIgnoreCase) >= 0,
                "FTV strict ingredient selection must resolve a rice-named texture, not a fallback.");
            return graphic.path;
        }

        if (owner != DmtrOwner)
        {
            throw new EndToEndAssertionException("Unsupported mapped graphic owner: " + owner);
        }

        var extension = product.def.modExtensions?.SingleOrDefault(candidate =>
            candidate.GetType().FullName ==
            "DynamicMealTextureReplacer.ModExtension_DynamicMealTextureReplacer");
        EndToEndAssert.NotNull(extension,
            "The finalized DMTR meal must retain its atlas mapping extension.");
        var filterType = extension!.GetType().Assembly.GetType(
            "DynamicMealTextureReplacer.MealAtlasIngredientFilter",
            throwOnError: false);
        var getRow = filterType?.GetMethod(
            "GetRow",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        EndToEndAssert.NotNull(getRow,
            "The inspected DMTR ingredient-row selection seam must remain available.");
        var row = (int)getRow!.Invoke(
            null,
            new object[] { extension, product.GetComp<CompIngredients>()! })!;
        EndToEndAssert.Equal(0, row,
            "DMTR must select the first explicit RawRice atlas row, not its attachment fallback.");
        return "dmtr-row:" + row;
    }

    private static void PreserveThermalSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorHalfLife = settings.ThermalHalfLifeHours;
        var priorThreshold = settings.AutoMicrowaveBelow;
        var priorQualityLoss = settings.MicrowaveQualityLoss;
        context.DeferCleanup(() =>
        {
            settings.MealTemperatureEnabled = priorTemperatureEnabled;
            settings.ThermalHalfLifeHours = priorHalfLife;
            settings.AutoMicrowaveBelow = priorThreshold;
            settings.MicrowaveQualityLoss = priorQualityLoss;
        });
        settings.MealTemperatureEnabled = true;
        settings.ThermalHalfLifeHours = 0.1f;
        settings.AutoMicrowaveBelow = 50f;
        settings.MicrowaveQualityLoss = 5;
    }

    private static void PreserveStrictFtvIngredientSelection(IEndToEndContext context)
    {
        var mainType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(
                "FoodTextureVariety.FoodTextureVarietyMain",
                throwOnError: false))
            .SingleOrDefault(type => type is not null);
        var settingsField = mainType?.GetField(
            "settings",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var settings = settingsField?.GetValue(null);
        EndToEndAssert.NotNull(settings,
            "The active FTV group must expose its inspected settings instance.");
        var settingsType = settings!.GetType();
        var useAll = settingsType.GetField("useAllTextures")!;
        var strict = settingsType.GetField("strictTextureMode")!;
        var fallback = settingsType.GetField("useFallbackMeal")!;
        var priorUseAll = (bool)useAll.GetValue(settings)!;
        var priorStrict = (bool)strict.GetValue(settings)!;
        var priorFallback = (bool)fallback.GetValue(settings)!;
        context.DeferCleanup(() =>
        {
            useAll.SetValue(settings, priorUseAll);
            strict.SetValue(settings, priorStrict);
            fallback.SetValue(settings, priorFallback);
        });
        useAll.SetValue(settings, false);
        strict.SetValue(settings, true);
        fallback.SetValue(settings, false);
    }

    private static Pawn GenerateCook(string name)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                if (pawn.needs?.rest is { } rest)
                {
                    rest.CurLevelPercentage = 1f;
                }

                for (var hour = 0; hour < 24; hour++)
                {
                    pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
                }

                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable mapped-meal cook.");
    }

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 70f, useCenter: true))
        {
            if (centers.Any(center => center.DistanceToSquared(candidate) < 225) ||
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

        throw new EndToEndAssertionException(
            "Could not find enough separated mapped-meal fixture rooms.");
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
                    cell.GetEdifice(map) is not null ||
                    cell.GetFirstPawn(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class MappedMealCase
    {
        internal MappedMealCase(
            Pawn cook,
            Thing stove,
            RecipeDef recipe,
            ThingDef productDef,
            string graphicOwner,
            Thing rice,
            ThingWithComps cookware,
            ThingWithComps plate,
            ThingWithComps cutlery,
            Building_Microwave microwave)
        {
            Cook = cook;
            Stove = stove;
            Recipe = recipe;
            ProductDef = productDef;
            GraphicOwner = graphicOwner;
            Rice = rice;
            Cookware = cookware;
            Plate = plate;
            Cutlery = cutlery;
            Microwave = microwave;
        }

        internal Pawn Cook { get; }
        internal Thing Stove { get; }
        internal RecipeDef Recipe { get; }
        internal ThingDef ProductDef { get; }
        internal string GraphicOwner { get; }
        internal Thing Rice { get; }
        internal ThingWithComps Cookware { get; }
        internal ThingWithComps Plate { get; }
        internal ThingWithComps Cutlery { get; }
        internal Building_Microwave Microwave { get; }
        internal ThingWithComps? Product { get; private set; }
        internal bool NativeBillObserved { get; private set; }
        internal bool MicrowaveObserved { get; private set; }
        internal string MappedVisualKey { get; set; } = string.Empty;
        internal int QualityBeforeReheat { get; set; }

        internal bool ObserveNativeBill()
        {
            NativeBillObserved |= Cook.CurJobDef == JobDefOf.DoBill &&
                                  Cook.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        internal bool TryResolveProduct()
        {
            Product ??= Cook.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == 1);
            return Product is not null;
        }

        internal float CurrentTemperature() =>
            Product!.GetComp<CompCulinaryState>()!.PeekCurrentServing()!.TemperatureCelsius;

        internal bool ObserveAtMicrowave()
        {
            MicrowaveObserved |= Product is { Destroyed: false } &&
                                 Cook.CurJobDef == JobDefOf.Ingest &&
                                 Cook.Position.DistanceToSquared(Microwave.Position) <= 4;
            return MicrowaveObserved;
        }

        internal bool ReheatCompleted() =>
            Product is { Destroyed: false } &&
            Product.GetComp<CompCulinaryState>()!
                .PeekCurrentServingWithoutThermalUpdate()!
                .MicrowaveReheatCount == 1;
    }
}
