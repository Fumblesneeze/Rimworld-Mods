using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class FinalizedImmersiveChefsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsModInitializedFromTheRealActiveSet()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(
                mod => string.Equals(
                    mod.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must be an actually loaded ModContentPack, not merely a referenced assembly.");
        IntegrationAssert.NotNull(
            ImmersiveChefsMod.Integrations,
            "The real Immersive Chefs Mod constructor must initialize its optional-integration snapshot.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void GameplayDefsAreFinalizedAndResearchGated()
    {
        var expectedThings = new[]
        {
            "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Silverware",
            "ImmersiveChefs_ChefsKnife", "ImmersiveChefs_Dishwasher",
            "ImmersiveChefs_IndustrialDishwasher", "ImmersiveChefs_PreparedFood",
            "ImmersiveChefs_PrepStation", "ImmersiveChefs_SauceStation",
            "ImmersiveChefs_MeatStation", "ImmersiveChefs_VegetableStation",
            "ImmersiveChefs_PastryStation", "ImmersiveChefs_Microwave"
        };
        foreach (var defName in expectedThings)
        {
            IntegrationAssert.NotNull(DefDatabase<ThingDef>.GetNamedSilentFail(defName), $"Missing ThingDef {defName}.");
        }

        var professionalKitchens = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
            "ImmersiveChefs_ProfessionalKitchens");
        IntegrationAssert.NotNull(professionalKitchens, "Professional Kitchens research must finalize.");
        var prerequisites = professionalKitchens!.prerequisites.Select(value => value.defName).ToList();
        IntegrationAssert.True(
            prerequisites.Contains("ImmersiveChefs_Dishwashing") && prerequisites.Contains("Machining"),
            "Professional Kitchens must require both Dishwashing and vanilla Machining.");
        IntegrationAssert.NotNull(
            DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_PrepareIngredients"),
            "Prepared-food recipe must finalize.");
        IntegrationAssert.True(
            !DefDatabase<ThingDef>.AllDefsListForReading.Any(def =>
                def.defName.StartsWith("ImmersiveChefs_Ceramic", StringComparison.OrdinalIgnoreCase) ||
                def.defName.StartsWith("ImmersiveChefs_Porcelain", StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must not invent ceramic or porcelain content in this release.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedMealsContainRuntimeStateWithoutReplacingIngredients()
    {
        var meal = DefDatabase<ThingDef>.GetNamed("MealSimple");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompEmbeddedWare)),
            "MealSimple must receive embedded plate state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompCulinaryState)),
            "MealSimple must receive culinary serving state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompIngredients)),
            "MealSimple must retain vanilla CompIngredients for variety compatibility.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealOwnsAndReleasesTheExactPlateThing()
    {
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var plateId = plate.ThingID;
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(embedded.TryEmbedPlate(plate), "The finalized meal must accept one physical plate.");
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Embedding must retain the original Thing instance.");
        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "Releasing must return the exact original Thing instance.");
        IntegrationAssert.Equal(plateId, released!.ThingID, "The plate LoadID must remain unchanged.");

    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealTransfersTheExactPlateFromPawnInventory()
    {
        var pawn = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
            "The integration fixture must put the physical plate in a real pawn inventory.");
        IntegrationAssert.True(
            embedded.TryEmbedPlate(plate),
            "A cooking product must transfer its reserved plate out of Pawn_InventoryTracker.");
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Inventory transfer must retain the exact physical plate instance.");

        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "The exact inventory-sourced plate must remain recoverable after embedding.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CoreHarmonyOwnersAreInstalledExactlyOnce()
    {
        var cooking = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing));
        var ingest = AccessTools.Method(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.TryMakePreToilReservations));
        var ingestOutcome = AccessTools.Method(
            typeof(Thing),
            nameof(Thing.Ingested),
            new[] { typeof(Pawn), typeof(float) });
        foreach (var method in new[] { cooking, ingest, ingestOutcome })
        {
            var owners = Harmony.GetPatchInfo(method)?.Owners
                .Where(owner => owner == ImmersiveChefsMod.PackageId)
                .ToList() ?? new System.Collections.Generic.List<string>();
            IntegrationAssert.Equal(1, owners.Count, $"Expected one Immersive Chefs patch owner on {method.Name}.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsXmlProbeContainsItsFinalPatch()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        var probe = steel?.GetModExtension<ImmersiveChefsIntegrationProbeExtension>();

        IntegrationAssert.NotNull(
            probe,
            "Finalized Core Steel must contain the Immersive Chefs XML-patched mod extension.");
        IntegrationAssert.Equal(
            "patched-by-immersive-chefs-xml",
            probe!.marker,
            "The finalized probe must contain the exact Immersive Chefs PatchOperation result.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveProcessorFrameworkUsesOneIdentityPreservingBridge()
    {
        var processorActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "syrchalis.processor.framework", StringComparison.OrdinalIgnoreCase));
        if (!processorActive)
        {
            return;
        }

        var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
        IntegrationAssert.NotNull(processorType, "Active Processor Framework must expose CompProcessor.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => processorType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one Processor Framework component.");
            IntegrationAssert.Equal(
                DrawerType.MapMeshAndRealTime,
                def.drawerType,
                $"{defName} must render Processor Framework progress.");
        }

        var takeOut = AccessTools.Method(processorType, "TakeOutProduct");
        var owners = Harmony.GetPatchInfo(takeOut)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, owners, "Processor completion must have one Immersive Chefs identity bridge.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveDubsAddsOneValidatedPipeToEachDishwasher()
    {
        var dubsActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "dubwise.dubsbadhygiene", StringComparison.OrdinalIgnoreCase));
        if (!dubsActive)
        {
            return;
        }

        var pipeType = AccessTools.TypeByName("DubsBadHygiene.CompPipe");
        IntegrationAssert.NotNull(pipeType, "Active Dubs Bad Hygiene must expose CompPipe.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => pipeType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one validated Dubs pipe component.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveGastronomyInstallsOneGuardedWaiterBridge()
    {
        var activeIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToList();
        var gastronomyActive = activeIds.Any(id =>
            string.Equals(id, "orion.gastronomy", StringComparison.OrdinalIgnoreCase));
        if (!gastronomyActive)
        {
            return;
        }

        IntegrationAssert.True(
            activeIds.Any(id => string.Equals(id, "orion.cashregister", StringComparison.OrdinalIgnoreCase)),
            "The supported Gastronomy matrix must load Cash Register first.");
        var serveType = AccessTools.TypeByName("Gastronomy.Waiting.JobDriver_Serve");
        IntegrationAssert.NotNull(serveType, "Active Gastronomy must expose its waiter driver.");
        var makeToils = AccessTools.Method(serveType, "MakeNewToils");
        var owners = Harmony.GetPatchInfo(makeToils)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, owners, "Gastronomy serving must have one guarded Immersive Chefs bridge.");
    }
}
