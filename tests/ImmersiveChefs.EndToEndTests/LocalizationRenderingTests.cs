using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.localization-rendering",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 2_400,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 120)]
public sealed class LocalizationRenderingTest : IRimWorldEndToEndTest
{
    private LocalizationRenderingFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = LocalizationRenderingFixture.Create(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause the exact-language localization fixture",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "verify the selected language and representative localized runtime values",
            _ => fixture.AssertLocalizedValues());

        yield return new ModSettingsActionStep(
            "open the exact Immersive Chefs native settings dialog",
            "fumblesneeze.immersivechefs");
        yield return new ScreenshotStep(
            "native product settings visibly render in the selected language",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WindowCancelActionStep(
            "close the exact native product settings dialog",
            "RimWorld.Dialog_ModSettings");

        yield return new SelectionActionStep(
            "select the dirty plate used by the localized dishwashing workflow",
            new[] { fixture.Plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the translated plate kitchen alert station and cleaner",
            fixture.MapFixtureIds,
            paddingPixels: 180);
        yield return ThingInfoCardActionStep.Open(
            "open the exact plate native info card",
            fixture.Plate.ThingID);
        yield return new AssertionStep(
            "the selected steel plate reports its actual machining material",
            _ => fixture.AssertActualPlateIngredientRow());
        yield return new ScreenshotStep(
            "native plate info card visibly renders translated Def and sanitation text",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Close(
            "close the exact plate native info card",
            fixture.Plate.ThingID);

        yield return new SelectionActionStep(
            "select the loaded dishwasher exposing its translated runtime action",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "native dishwasher inspector and eject action visibly render in the selected language",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new SelectionActionStep(
            "select the alert-worthy active cooking station",
            new[] { fixture.Stove.ThingID },
            additive: false);
        var alertRefreshFrame = context.FrameCount;
        yield return new TimeControlActionStep(
            "let RimWorld refresh the native alert readout",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the native alert refresh interval elapses with the shortage active",
            _ => fixture.AlertReadoutRefreshElapsed(context.FrameCount, alertRefreshFrame),
            new EndToEndDeadline(420, 2_000, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause on the refreshed localized alert readout",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "require the localized missing-kitchenware alert to be active",
            _ => fixture.AssertLocalizedAlert());
        yield return new CheckpointStep(
            "record the exact player-facing localized alert text",
            _ => fixture.AlertCheckpoint());
        yield return new ScreenshotStep(
            "the native alert readout visibly reports the active kitchenware shortage",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable ordinary Cleaning work for the localized job report",
            _ => HandwashingE2EFixture.ActivateCleaner(fixture.Cleaner));
        yield return new TimeControlActionStep(
            "run the ordinary dishwashing workgiver",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cleaner visibly starts the localized Doing dishes job",
            _ => fixture.IsDishwashingAtWater(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on the live localized work report",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner performing the localized job",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the native pawn inspector visibly renders the localized dishwashing report",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the live job report remains the exact selected-language text",
            _ => fixture.AssertLiveJobReport());
    }
}

internal sealed class LocalizationRenderingFixture
{
    private static readonly IReadOnlyDictionary<string, ExpectedLocalization> Expected =
        new Dictionary<string, ExpectedLocalization>(StringComparer.Ordinal)
        {
            ["English"] = new(
                "Restart-required classification and Def settings",
                "plate",
                "Doing dishes.",
                "Missing cookware sets",
                "Eject dishes"),
            ["German"] = new(
                "Klassifizierungs- und Def-Einstellungen mit Neustartpflicht",
                "Teller",
                "Spült Geschirr.",
                "Es fehlen Kochgeschirrsets",
                "Geschirr auswerfen"),
            ["Spanish"] = new(
                "Clasificación y Defs que requieren reinicio",
                "plato",
                "Lavando la vajilla.",
                "Faltan baterías de cocina",
                "Expulsar vajilla"),
            ["French"] = new(
                "Classification et Defs nécessitant un redémarrage",
                "assiette",
                "Fait la vaisselle.",
                "Éléments manquants : Batteries de cuisine",
                "Éjecter la vaisselle"),
            ["ChineseSimplified"] = new(
                "需重启的分类与定义设置",
                "餐盘",
                "正在洗碗。",
                "缺少炊具套装",
                "取出餐具"),
            ["Russian"] = new(
                "Классификация и Def-настройки, требующие перезапуска",
                "тарелка",
                "Моет посуду.",
                "Не хватает следующего: Наборы посуды для готовки",
                "Выгрузить посуду")
        };

    private readonly string language;
    private readonly ExpectedLocalization expected;
    private readonly IntVec3 waterCell;
    private readonly int arrangedTick;

    private LocalizationRenderingFixture(
        string language,
        ExpectedLocalization expected,
        Pawn cleaner,
        Pawn cook,
        ThingWithComps plate,
        ThingWithComps stove,
        ThingWithComps dishwasher,
        IntVec3 waterCell,
        int arrangedTick)
    {
        this.language = language;
        this.expected = expected;
        Cleaner = cleaner;
        Cook = cook;
        Plate = plate;
        Stove = stove;
        Dishwasher = dishwasher;
        this.waterCell = waterCell;
        this.arrangedTick = arrangedTick;
    }

    internal Pawn Cleaner { get; }

    internal Pawn Cook { get; }

    internal ThingWithComps Plate { get; }

    internal ThingWithComps Stove { get; }

    internal ThingWithComps Dishwasher { get; }

    internal IReadOnlyList<string> MapFixtureIds => new[]
    {
        Cleaner.ThingID,
        Cook.ThingID,
        Plate.ThingID,
        Stove.ThingID,
        Dishwasher.ThingID
    };

    internal static LocalizationRenderingFixture Create(IEndToEndContext context)
    {
        var language = LanguageDatabase.activeLanguage?.folderName ?? string.Empty;
        var requestedLanguage = Environment.GetEnvironmentVariable("RIMWORLD_E2E_EXPECTED_LANGUAGE") ?? string.Empty;
        EndToEndAssert.True(!string.IsNullOrWhiteSpace(requestedLanguage),
            "The isolated E2E launcher must bind localization acceptance to its explicitly requested locale.");
        EndToEndAssert.Equal(requestedLanguage, language,
            "RimWorld must activate the exact requested locale instead of silently falling back to English.");
        EndToEndAssert.True(Expected.TryGetValue(language, out var expected),
            "The isolated process must activate one exact required localization folder, not an English fallback.");
        EndToEndAssert.True(Prefs.DevMode,
            "Localization acceptance must run with RimWorld developer mode enabled.");

        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorPreferDishwashers = settings.PreferDishwashers;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorDishwashingWork = settings.DishwashingWorkScale;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.PreferDishwashers = priorPreferDishwashers;
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.DishwashingWorkScale = priorDishwashingWork;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.PreferDishwashers = false;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 4f;

        var map = Find.CurrentMap ?? throw new EndToEndAssertionException(
            "A playable current map is required for localization rendering.");
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        var waterCell = center + new IntVec3(0, 0, -3);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            map,
            waterCell,
            DefDatabase<TerrainDef>.GetNamed("WaterShallow"));

        var cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Localized dish cleaner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-3, 0, 0), map);

        var cook = CreateAlertCook();
        GenSpawn.Spawn(cook, center + new IntVec3(3, 0, 0), map);

        var plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -1), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center + new IntVec3(2, 0, 1), map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var dishwasherDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher");
        var dishwasher = (ThingWithComps)ThingMaker.MakeThing(
            dishwasherDef,
            dishwasherDef.MadeFromStuff ? ThingDefOf.Steel : null);
        dishwasher.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(dishwasher, center + new IntVec3(0, 0, 3), map, Rot4.South);
        var cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        cutlery.GetComp<CompSanitation>()!.MarkDirty();
        EndToEndAssert.True(
            dishwasher.GetComp<CompDishwasher>()!.GetDirectlyHeldThings().TryAdd(cutlery),
            "The localization fixture must load one exact dirty cutlery unit for the native Eject action.");

        return new LocalizationRenderingFixture(
            language,
            expected!,
            cleaner,
            cook,
            plate,
            stove,
            dishwasher,
            waterCell,
            Find.TickManager.TicksGame);
    }

    internal void AssertLocalizedValues()
    {
        var translationErrors = LanguageDatabase.activeLanguage.loadErrors
            .Concat(LanguageDatabase.activeLanguage.defInjections.SelectMany(package => package.loadErrors))
            .ToArray();
        EndToEndAssert.Equal(language, LanguageDatabase.activeLanguage.folderName,
            "The selected required language must remain active after map arrangement.");
        EndToEndAssert.False(LanguageDatabase.activeLanguage.anyError,
            "The selected locale must not retain an error flag: " + string.Join(" | ", translationErrors));
        EndToEndAssert.Equal(0, translationErrors.Length,
            "The selected locale must load without translation-data errors: " +
            string.Join(" | ", translationErrors));
        AssertExact(expected.SettingsHeading, "ImmersiveChefs_Settings_RestartHeading".Translate());
        AssertExact(expected.PlateLabel, Plate.def.label);
        AssertExact(expected.JobReport, ImmersiveChefsDefOf.ImmersiveChefs_DoDishes.reportString);
        AssertExact(expected.AlertLabel, new Alert_MissingKitchenware().GetLabel());
        AssertExact(expected.EjectAction, "ImmersiveChefs_Dishwasher_EjectLabel".Translate());
    }

    internal void AssertLocalizedAlert()
    {
        var alert = new Alert_MissingKitchenware();
        EndToEndAssert.True(alert.GetReport().AnyCulpritValid,
            "The native missing-kitchenware alert must be active for the owned strict stove bill.");
        AssertExact(expected.AlertLabel, alert.GetLabel());
    }

    internal void AssertActualPlateIngredientRow()
    {
        var request = StatRequest.For(Plate);
        var ingredients = Plate.def.SpecialDisplayStats(request)
            .Single(entry => entry.DisplayPriorityWithinCategory == 1102);
        var ingredientLinks = ingredients.GetHyperlinks(request)
            .Select(link => link.def)
            .ToArray();
        var expectedValue = "ImmersiveChefs_IngredientRequirement".Translate(
            4f,
            ThingDefOf.Steel.label).ToString();
        var primitive = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitivePlates");

        EndToEndAssert.Equal(expectedValue, ingredients.ValueString,
            "The native steel-plate info row must name steel rather than the first primitive recipe.");
        EndToEndAssert.Equal(1, ingredientLinks.Length,
            "The native one-slot plate row must expose one exact material hyperlink.");
        EndToEndAssert.Equal(ThingDefOf.Steel, ingredientLinks[0],
            "The native Ingredients hyperlink must open actual steel rather than primitive stone.");
        EndToEndAssert.Equal(
            "ImmersiveChefs_IngredientRequirement".Translate(
                4f,
                "ImmersiveChefs_Ingredient_AnyStonyMaterial".Translate()).ToString(),
            primitive.IngredientValueGetter!.BillRequirementsDescription(primitive, primitive.ingredients[0]),
            "The primitive bill must retain its broad translated stony-material requirement.");
    }

    internal bool AlertReadoutRefreshElapsed(long currentFrame, long startFrame) =>
        currentFrame - startFrame >= 300 &&
        Find.TickManager.TicksGame - arrangedTick >= 180 &&
        new Alert_MissingKitchenware().GetReport().AnyCulpritValid;

    internal IReadOnlyDictionary<string, string> AlertCheckpoint()
    {
        var alert = new Alert_MissingKitchenware();
        return new Dictionary<string, string>
        {
            ["language"] = language,
            ["label"] = alert.GetLabel(),
            ["explanation"] = alert.GetExplanation()
        };
    }

    internal bool IsDishwashingAtWater() =>
        HandwashingE2EFixture.IsDoingDishesAt(Cleaner, waterCell, Plate.ThingID);

    internal void AssertLiveJobReport()
    {
        EndToEndAssert.True(IsDishwashingAtWater(),
            "The selected pawn must still be performing the ordinary dishwashing job at natural water.");
        AssertExact(expected.JobReport, Cleaner.CurJob!.GetReport(Cleaner));
    }

    private static Pawn CreateAlertCook()
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist("Localized alert cook");
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f)
            {
                pawn.workSettings.SetPriority(cooking, 1);
                FoodSearchE2EFixture.SetHunger(pawn, 1f);
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

        throw new EndToEndAssertionException("Could not generate a Cooking-capable localization alert pawn.");
    }

    private static void AssertExact(string expectedText, string actualText)
    {
        EndToEndAssert.Equal(expectedText, actualText,
            "The required locale must render the exact contextual product translation.");
        EndToEndAssert.False(actualText.Contains("ImmersiveChefs_"),
            "A player-facing localization value must not expose a missing translation key.");
    }

    private sealed class ExpectedLocalization
    {
        internal ExpectedLocalization(
            string settingsHeading,
            string plateLabel,
            string jobReport,
            string alertLabel,
            string ejectAction)
        {
            SettingsHeading = settingsHeading;
            PlateLabel = plateLabel;
            JobReport = jobReport;
            AlertLabel = alertLabel;
            EjectAction = ejectAction;
        }

        internal string SettingsHeading { get; }

        internal string PlateLabel { get; }

        internal string JobReport { get; }

        internal string AlertLabel { get; }

        internal string EjectAction { get; }
    }
}
