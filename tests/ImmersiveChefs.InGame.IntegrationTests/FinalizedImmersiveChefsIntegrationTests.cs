using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class FinalizedImmersiveChefsIntegrationTests
{
    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CompletedDiningLeavesExactDirtySettingOnNativeTableCell()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var fixture = map.AllCells
            .Where(cell => CellRect.CenteredOn(cell, 3).Cells.All(candidate =>
                candidate.InBounds(map) && candidate.Standable(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var table = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            ThingDefOf.Steel);
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        DiningSession? session = null;

        try
        {
            GenSpawn.Spawn(table, fixture, map, Rot4.North);
            var tableCell = table.OccupiedRect().Cells.First(cell => cell.HasEatSurface(map));
            var diningCell = GenAdj.CellsAdjacentCardinal(table)
                .First(cell => cell.InBounds(map) && cell.Standable(map));
            GenSpawn.Spawn(pawn, diningCell, map);
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var job = JobMaker.MakeJob(JobDefOf.Ingest);
            job.SetTarget(TargetIndex.B, tableCell);
            session = new DiningSession(
                pawn,
                job,
                cutlery,
                null,
                null,
                DiningCutlerySource.Colony);
            session.PickupCutlery();
            session.CapturePlate(plate);

            session.Finish();

            IntegrationAssert.True(
                plate.Spawned && plate.Position == tableCell,
                "The exact used plate must remain on the native ingest job's selected table cell.");
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.Position == tableCell,
                "The exact used cutlery must remain beside the plate on the native selected table cell.");
            IntegrationAssert.True(
                table is Building { MaxItemsInCell: 2 } && tableCell.GetItemCount(map) == 2,
                "A dining surface must use the native two-item cell capacity without merging the exact setting.");
            IntegrationAssert.True(
                plate.DrawPos != cutlery.DrawPos,
                "RimWorld's native multiple-items-per-cell rendering must offset the plate and cutlery visibly.");
            IntegrationAssert.True(
                plate.GetComp<CompSanitation>().IsDirty &&
                cutlery.GetComp<CompSanitation>().IsDirty,
                "Both exact table-setting Things must become dirty only after completed dining.");
        }
        finally
        {
            foreach (var thing in new Thing[] { cutlery, plate, pawn, table })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void LaterCoveredCookingAdmissionMovesDirtySurfaceCookwareAsideIntact()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var fixture = map.AllCells
            .Where(cell => CellRect.CenteredOn(cell, 3).Cells.All(candidate =>
                candidate.InBounds(map) && candidate.Standable(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var stove = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("FueledStove"));
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var cookware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware"),
            ThingDefOf.Steel);
        var job = JobMaker.MakeJob(JobDefOf.DoBill, stove);
        var priorMode = ImmersiveChefsMod.Settings.WareRequirementMode;

        try
        {
            GenSpawn.Spawn(stove, fixture, map, Rot4.North);
            GenSpawn.Spawn(pawn, stove.InteractionCell, map);
            GenSpawn.Spawn(cookware, stove.Position, map);
            cookware.GetComp<CompSanitation>().MarkDirty();
            cookware.SetForbidden(true, warnOnFail: false);
            var thingId = cookware.ThingID;
            var hitPoints = cookware.HitPoints;
            var quality = cookware.TryGetComp<CompQuality>()?.Quality;
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Off;

            IntegrationAssert.True(
                CookingSessionRegistry.TryAttach(
                    pawn,
                    job,
                    stove,
                    DefDatabase<RecipeDef>.GetNamed("CookMealSimple"),
                    emergency: false,
                    out var missingReason),
                "A covered ware-exempt cooking attempt must be admitted: " + missingReason);

            IntegrationAssert.True(
                cookware.Spawned && !stove.OccupiedRect().Contains(cookware.Position),
                "Admitting the later cooking attempt must move prior dirty cookware off the stove surface.");
            IntegrationAssert.True(
                cookware.ThingID == thingId && cookware.stackCount == 1 &&
                cookware.HitPoints == hitPoints &&
                cookware.TryGetComp<CompQuality>()?.Quality == quality &&
                cookware.GetComp<CompSanitation>().IsDirty && cookware.IsForbidden(Faction.OfPlayer),
                "Moving cookware aside must preserve exact identity, unit count, condition, quality, sanitation and forbiddance.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = priorMode;
            CookingSessionRegistry.Cleanup(pawn, job);
            foreach (var thing in new Thing[] { cookware, pawn, stove })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CompletedCookingLeavesExactDirtyCookwareOnBillGiverSurface()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var fixture = map.AllCells
            .Where(cell => CellRect.CenteredOn(cell, 3).Cells.All(candidate =>
                candidate.InBounds(map) && candidate.Standable(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var stove = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("FueledStove"));
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var cookware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware"),
            ThingDefOf.Steel);
        var job = JobMaker.MakeJob(JobDefOf.DoBill, stove);

        try
        {
            GenSpawn.Spawn(stove, fixture, map, Rot4.North);
            GenSpawn.Spawn(pawn, stove.InteractionCell, map);
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cookware, canMergeWithExistingStacks: false),
                "The active-cooking fixture must retain the exact cookware in the cook inventory.");
            var session = new CookingSession(
                pawn,
                job,
                DefDatabase<RecipeDef>.GetNamed("CookMealSimple"),
                stove,
                new ReservedWarePortion(cookware, 1),
                Array.Empty<ReservedWarePortion>(),
                emergencyMissingWare: false,
                wareExempt: false);

            session.NotifyWorkTick();
            session.ReleaseAtEnd();

            IntegrationAssert.True(
                cookware.Spawned && stove.OccupiedRect().Contains(cookware.Position),
                "Active cooking completion must return the exact cookware to the stove surface.");
            IntegrationAssert.True(
                cookware.stackCount == 1 && cookware.GetComp<CompSanitation>().IsDirty,
                "The one exact returned cookware set must remain dirty and unconsumed.");
        }
        finally
        {
            foreach (var thing in new Thing[] { cookware, pawn, stove })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void SpawnedReservedWareReleaseAndEmptyDishCarrierAreIdempotent()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var ware = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware"),
            ThingDefOf.Steel);
        var cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
        GenSpawn.Spawn(ware, cell, map);
        Pawn? pawn = null;
        try
        {
            var before = map.listerThings.ThingsOfDef(ware.def).Sum(thing => thing.stackCount);
            IntegrationAssert.True(
                CookingSession.TryReleaseExactThing(ware, map.Center, map),
                "An already-spawned reserved ware portion must be accepted in place.");
            IntegrationAssert.True(ware.Spawned && ware.stackCount == 1,
                "The exact already-spawned ware Thing must remain one map unit.");
            IntegrationAssert.Equal(before,
                map.listerThings.ThingsOfDef(ware.def).Sum(thing => thing.stackCount),
                "Releasing an already-spawned reservation must not duplicate or lose ware.");

            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                canGeneratePawnRelations: false));
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(cell, map, 4), map);
            IntegrationAssert.True(pawn.carryTracker.CarriedThing is null,
                "The empty-carrier regression fixture must begin empty.");
            IntegrationAssert.True(!JobDriver_DoDishes.TryDropCarriedThingIfPresent(pawn),
                "Finishing after admission consumed the carried Thing must be a safe no-op.");
        }
        finally
        {
            if (pawn is { Destroyed: false }) pawn.Destroy(DestroyMode.Vanish);
            if (!ware.Destroyed) ware.Destroy(DestroyMode.Vanish);
        }
    }

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
    public static void TextureVariationSelectorIsAbsentWithoutOptionalPackages()
    {
        var optionalPackages = new[]
        {
            "OskarPotocki.VanillaFactionsExpanded.Core",
            "VanillaExpanded.VTEXVariations"
        };
        foreach (var packageId in optionalPackages)
        {
            IntegrationAssert.True(
                !LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                    mod.PackageId,
                    packageId,
                    StringComparison.OrdinalIgnoreCase)),
                packageId + " must be absent from the base texture-variation group.");
        }

        var expectedDefs = new[]
        {
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_ChefsKnife"
        };
        foreach (var defName in expectedDefs)
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                typeof(Graphic_Single),
                def.graphicData.graphicClass,
                defName + " must retain ordinary Graphic_Single when VTEX/VEF is absent.");
        }

        var selected = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def =>
                string.Equals(
                    def.modContentPack?.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase) &&
                def.graphicData?.graphicClass == typeof(Graphic_PortableKitchenwareVariation))
            .Select(def => def.defName)
            .ToArray();
        IntegrationAssert.Equal(
            0,
            selected.Length,
            "No finalized Immersive Chefs Def may retain the optional selector in the base group.");

        var buildingDefs = new[]
        {
            "ImmersiveChefs_Dishwasher",
            "ImmersiveChefs_IndustrialDishwasher",
            "ImmersiveChefs_PrepStation",
            "ImmersiveChefs_SauceStation",
            "ImmersiveChefs_MeatStation",
            "ImmersiveChefs_VegetableStation",
            "ImmersiveChefs_PastryStation",
            "ImmersiveChefs_Microwave"
        };
        foreach (var defName in buildingDefs)
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                typeof(Graphic_Multi),
                def.graphicData.graphicClass,
                defName + " must retain authored cardinal art after finalization.");
            IntegrationAssert.True(
                !def.comps.Any(properties =>
                    properties.GetType().FullName ==
                    "VEF.Buildings.CompProperties_RandomBuildingGraphic"),
                defName + " must not retain a VEF comp when the optional packages are absent.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void GameplayDefsAreFinalizedAndResearchGated()
    {
        var expectedThings = new[]
        {
            "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery",
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
    public static void FallbackMicrowaveFinalizesAsCountertopAppliance()
    {
        var microwave = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");

        IntegrationAssert.Equal(
            typeof(Building_Microwave),
            microwave.thingClass,
            "The fallback microwave must use its support-aware building class.");
        IntegrationAssert.Equal(
            AltitudeLayer.BuildingOnTop,
            microwave.altitudeLayer,
            "The fallback microwave must render on top of its supporting surface.");
        IntegrationAssert.True(
            microwave.building is { isEdifice: false } && !microwave.clearBuildingArea,
            "The fallback microwave must remain a non-edifice that does not clear its support.");
        IntegrationAssert.True(
            microwave.blocksAltitudes.Contains(AltitudeLayer.BuildingOnTop),
            "The fallback microwave must only reserve the countertop altitude layer.");
        IntegrationAssert.Equal(
            ThingDefOf.MinifiedThing,
            microwave.minifiedDef,
            "The fallback microwave must remain recoverable as a minified appliance.");
        IntegrationAssert.Equal(
            TickerType.Rare,
            microwave.tickerType,
            "The fallback microwave must periodically validate its support.");
        IntegrationAssert.True(
            microwave.placeWorkers?.Any(worker => worker == typeof(PlaceWorker_MicrowaveCountertop)) == true,
            "The finalized Def must retain the capability-based countertop placement worker.");
        IntegrationAssert.Equal(
            "ImmersiveChefs/Things/Building/Appliance/Microwave",
            microwave.graphicData.texPath,
            "The fallback microwave must use the reviewed custom countertop sprite.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CoreHeatingSourcesClassifyByFinalizedCapabilities()
    {
        var cases = new[]
        {
            new { DefName = "ElectricStove", Expected = MealHeatingSourceKind.Stove },
            new { DefName = "FueledStove", Expected = MealHeatingSourceKind.Stove },
            new { DefName = "Campfire", Expected = MealHeatingSourceKind.Campfire },
            new { DefName = "Heater", Expected = MealHeatingSourceKind.AmbientHeater }
        };

        foreach (var sourceCase in cases)
        {
            var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(sourceCase.DefName));
            var source = MealHeatingSource.TryCreate(thing);
            IntegrationAssert.NotNull(
                source,
                $"Core {sourceCase.DefName} must expose a supported heating capability.");
            IntegrationAssert.Equal(
                sourceCase.Expected,
                source!.Kind,
                $"Core {sourceCase.DefName} must retain its capability-derived heating tier.");
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CountertopMicrowaveAcceptsRealSurfacesAndRecoversAfterSupportLoss()
    {
        var map = Find.CurrentMap;
        var microwaveDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");
        var table = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Table1x2c"), ThingDefOf.Steel);
        var workbench = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TableMachining"));
        var shelf = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Shelf"), ThingDefOf.Steel);
        var microwave = ThingMaker.MakeThing(microwaveDef);
        MinifiedThing? recovered = null;

        var clearCandidates = map.AllCells
            .Where(cell => CellRect.CenteredOn(cell, 2).Cells.All(candidate =>
                candidate.InBounds(map) && candidate.Standable(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .ToArray();
        var separatedFixtureCells = new List<IntVec3>();
        foreach (var candidate in clearCandidates)
        {
            if (separatedFixtureCells.All(existing => existing.DistanceToSquared(candidate) > 36))
            {
                separatedFixtureCells.Add(candidate);
            }

            if (separatedFixtureCells.Count == 3)
            {
                break;
            }
        }

        var fixtureCells = separatedFixtureCells.ToArray();
        IntegrationAssert.Equal(3, fixtureCells.Length, "The map must provide three clear countertop fixture areas.");

        try
        {
            GenSpawn.Spawn(table, fixtureCells[0], map, Rot4.North);
            GenSpawn.Spawn(workbench, fixtureCells[1], map, Rot4.North);
            GenSpawn.Spawn(shelf, fixtureCells[2], map, Rot4.North);

            var worker = new PlaceWorker_MicrowaveCountertop();
            bool AcceptedAt(Thing support) => new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
                .Any(rotation =>
                worker.AllowsPlacing(microwaveDef, support.Position, rotation, map).Accepted);

            IntegrationAssert.True(
                AcceptedAt(table),
                "A completed finalized dining table must accept at least one non-obstructing microwave rotation.");
            IntegrationAssert.True(
                AcceptedAt(workbench),
                "A completed finalized workbench must accept at least one non-obstructing microwave rotation.");
            IntegrationAssert.True(
                !AcceptedAt(shelf),
                "A finalized storage shelf must be rejected despite exposing an item surface.");
            IntegrationAssert.True(
                !worker.AllowsPlacing(
                    microwaveDef,
                    fixtureCells[0] + new IntVec3(0, 0, 3),
                    Rot4.North,
                    map).Accepted,
                "An ordinary floor cell must not accept a countertop microwave.");

            GenSpawn.Spawn(microwave, table.Position, map, Rot4.East);
            IntegrationAssert.True(
                MicrowaveSupportRuntime.FindAt(microwave.Position, map, microwave) == table,
                "The spawned fallback microwave must recognize its exact finalized table support.");
            table.Destroy(DestroyMode.Vanish);
            ((Building_Microwave)microwave).TickRare();

            var recoveredMatches = map.listerThings.AllThings
                .OfType<MinifiedThing>()
                .Where(candidate => ReferenceEquals(candidate.InnerThing, microwave))
                .ToArray();
            IntegrationAssert.Equal(
                1,
                recoveredMatches.Length,
                "Support loss must leave exactly one recoverable minified microwave.");
            recovered = recoveredMatches[0];
            IntegrationAssert.True(
                !microwave.Spawned && recovered.Position.DistanceToSquared(fixtureCells[0]) <= 16,
                "The unsupported appliance must stop floating and remain at or near its former countertop.");
        }
        finally
        {
            foreach (var thing in new[] { recovered as Thing, microwave, table, workbench, shelf })
            {
                if (thing is not null && !thing.Destroyed && thing.holdingOwner is null)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedKitchenwareRecipesUseExactUnitCostsAndMatchingWorkTypes()
    {
        var primitive = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware");
        var medieval = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeMedievalCookware");
        var modern = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeModernCookware");
        var knife = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeChefsKnife");
        var softPlates = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeSoftPlates");
        var softCutlery = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeSoftCutlery");

        IntegrationAssert.NotNull(
            medieval.IngredientValueGetter,
            "The finalized medieval cookware recipe must instantiate its ingredient-value getter.");
        IntegrationAssert.Equal(
            typeof(IngredientValueGetter_Units),
            medieval.IngredientValueGetter!.GetType(),
            "Finalized kitchenware recipes must count resource units rather than vanilla Stuff volume.");
        IntegrationAssert.Equal(
            6f,
            medieval.ingredients[0].GetBaseCount(),
            "The finalized medieval cookware recipe must retain its Core-benchmarked six-unit material cost.");
        IntegrationAssert.Equal(
            5f,
            primitive.ingredients[0].GetBaseCount(),
            "Primitive cookware must cost one Core wall-equivalent of stony material.");
        IntegrationAssert.Equal(
            1f,
            primitive.ingredients[1].GetBaseCount(),
            "Primitive cookware must use one wood unit for its handles and utensils.");
        IntegrationAssert.Equal(
            6f,
            modern.ingredients[0].GetBaseCount(),
            "Modern cookware must cost six material units.");
        IntegrationAssert.Equal(
            6f,
            knife.ingredients[0].GetBaseCount(),
            "The non-weapon chef's knife set must cost one fifth of Core's thirty-unit combat knife.");
        IntegrationAssert.Equal(
            4f,
            softPlates.ingredients[0].GetBaseCount(),
            "Four plates must cost four material units.");
        IntegrationAssert.Equal(
            2f,
            softCutlery.ingredients[0].GetBaseCount(),
            "Four cutlery settings must cost two material units.");
        IntegrationAssert.Equal(
            1f,
            medieval.IngredientValueGetter.ValuePerUnitOf(ThingDefOf.Silver),
            "Small-volume silver must contribute one whole recipe unit per item.");
        IntegrationAssert.Equal(
            "5x any stony material",
            primitive.IngredientValueGetter!.BillRequirementsDescription(primitive, primitive.ingredients[0]),
            "The primitive recipe must expose semantic material text rather than an internal Root category.");
        IntegrationAssert.Equal(
            "1x wood",
            primitive.IngredientValueGetter.BillRequirementsDescription(primitive, primitive.ingredients[1]),
            "The handle requirement must be player-facing wood text.");
        IntegrationAssert.Equal(
            "6x any modern metal",
            modern.IngredientValueGetter!.BillRequirementsDescription(modern, modern.ingredients[0]),
            "The machining material requirement must remain semantic after finalization.");
        IntegrationAssert.Equal(
            "6x any eligible metal",
            knife.IngredientValueGetter!.BillRequirementsDescription(knife, knife.ingredients[0]),
            "The chef's knife requirement must describe both intermediate and modern eligible metals.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Crafting,
            primitive.requiredGiverWorkType,
            "Crafting-spot kitchenware must use the Crafting work giver.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Smithing,
            medieval.requiredGiverWorkType,
            "Smithy kitchenware must use the Smithing work giver.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Smithing,
            modern.requiredGiverWorkType,
            "Machining kitchenware must use the Smithing work giver.");

        var recipesWithNoWoodSlot = DefDatabase<RecipeDef>.AllDefsListForReading
            .Where(recipe => recipe.GetModExtension<KitchenwareRecipeExtension>() is not null)
            .Where(recipe => recipe.ingredients is { Count: > 0 } &&
                             recipe.ingredients.All(slot => !slot.filter.Allows(ThingDefOf.WoodLog)))
            .ToArray();
        IntegrationAssert.True(
            recipesWithNoWoodSlot.Length > 0,
            "The finalized kitchenware catalog must include recipes that do not accept wood.");
        IntegrationAssert.True(
            recipesWithNoWoodSlot.All(recipe =>
                recipe.defaultIngredientFilter is not null &&
                !recipe.defaultIngredientFilter.Allows(ThingDefOf.WoodLog) &&
                recipe.fixedIngredientFilter is not null &&
                !recipe.fixedIngredientFilter.Allows(ThingDefOf.WoodLog)),
            "Default and fixed bill filters must not expose wood unless an actual ingredient slot accepts it.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExistingSteelPlateInfoUsesItsActualMachiningMaterial()
    {
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        try
        {
            var request = StatRequest.For(plate);
            var ingredients = plate.def.SpecialDisplayStats(request)
                .Single(entry => entry.DisplayPriorityWithinCategory == 1102);
            var ingredientLinks = ingredients.GetHyperlinks(request)
                .Select(link => link.def)
                .ToArray();
            var primitive = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitivePlates");

            IntegrationAssert.Equal(
                "4x steel",
                ingredients.ValueString,
                "An existing steel plate must report its exact machining material in the native info card.");
            IntegrationAssert.True(
                ingredients.ValueString.IndexOf("stony", StringComparison.OrdinalIgnoreCase) < 0,
                "A steel plate must not inherit the first primitive producing recipe's material category.");
            IntegrationAssert.Equal(
                1,
                ingredientLinks.Length,
                "The corrected native row must expose exactly one material hyperlink for this one-slot recipe.");
            IntegrationAssert.Equal(
                ThingDefOf.Steel,
                ingredientLinks[0],
                "The corrected native row must link to actual steel, not a primitive recipe material.");
            IntegrationAssert.Equal(
                "4x any stony material",
                primitive.IngredientValueGetter!.BillRequirementsDescription(primitive, primitive.ingredients[0]),
                "The primitive bill editor must retain its broad semantic Stuff requirement.");
        }
        finally
        {
            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedPlateMaterialsRespectMealComplexityTiers()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var primitiveRecipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitivePlates");
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        var classifier = OptionalMaterialAdapter.CreateClassifier();
        KitchenMaterialKind Classify(ThingDef stuff)
        {
            var categories = stuff.stuffProps?.categories;
            var classification = classifier.Classify(
                new KitchenMaterialDescriptor(
                    stuff.defName,
                    categories?.Any(category => category.defName == "Metallic") == true,
                    categories?.Any(category => category.defName == "Woody") == true,
                    categories?.Any(category => category.defName == "Stony") == true),
                KitchenwareProduct.Plate);
            IntegrationAssert.NotNull(
                classification,
                $"Finalized Stuff {stuff.defName} must classify as a supported plate material.");
            return classification!.Kind;
        }

        IntegrationAssert.True(
            plateDef.stuffCategories?.Any(category => category.defName == "Stony") == true,
            "The finalized plate Def must accept actual stone-block Stuff.");
        IntegrationAssert.True(
            primitiveRecipe.recipeUsers?.Any(user => user.defName == "CraftingSpot") == true,
            "Primitive stone plates must be available at a finalized crafting spot.");
        IntegrationAssert.True(
            primitiveRecipe.ingredients is { Count: > 0 } &&
            primitiveRecipe.ingredients[0].filter.Allows(granite),
            "Primitive stone plates must accept finalized granite blocks.");
        IntegrationAssert.True(
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Simple, Classify(granite)) &&
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Simple, Classify(ThingDefOf.WoodLog)),
            "Simple meals must accept finalized granite and wood plate Stuff.");
        IntegrationAssert.True(
            !PlateMaterialEligibilityPolicy.Allows(MealComplexity.Advanced, Classify(granite)) &&
            !PlateMaterialEligibilityPolicy.Allows(MealComplexity.Advanced, Classify(ThingDefOf.WoodLog)) &&
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Advanced, Classify(ThingDefOf.Steel)),
            "Fine meals must reject primitive/wood Stuff and accept finalized metal Stuff.");
        IntegrationAssert.True(
            !PlateMaterialEligibilityPolicy.Allows(MealComplexity.Elaborate, Classify(ThingDefOf.Steel)) &&
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Elaborate, Classify(ThingDefOf.Silver)) &&
            PlateMaterialEligibilityPolicy.Allows(MealComplexity.Elaborate, Classify(ThingDefOf.Gold)),
            "Lavish meals must reject ordinary steel and accept finalized silver or gold Stuff.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedPrimitiveCookwareUsesEveryEligibleStonyStuffAndDistinctArt()
    {
        var primitiveDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PrimitiveCookware");
        var modernDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware");
        var stonyStuff = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(definition => definition.stuffProps?.categories?.Any(category => category.defName == "Stony") == true)
            .ToArray();

        IntegrationAssert.True(
            stonyStuff.Length > 0,
            "The finalized Def database must contain at least one stony Stuff comparator.");
        IntegrationAssert.True(
            stonyStuff.All(definition => recipe.ingredients[0].filter.Allows(definition)),
            "The primitive cookware material filter must accept every non-excluded finalized Stony Stuff.");
        IntegrationAssert.True(
            recipe.defaultIngredientFilter is not null &&
            stonyStuff.All(definition => recipe.defaultIngredientFilter.Allows(definition)),
            "The finalized default bill filter must keep every Stony Stuff enabled for player selection.");
        IntegrationAssert.Equal(
            primitiveDef,
            recipe.products.Single().thingDef,
            "The primitive bill must create the distinct primitive cookware Def.");
        IntegrationAssert.True(
            primitiveDef.stuffCategories?.Count == 1 &&
            primitiveDef.stuffCategories[0].defName == "Stony",
            "Primitive cookware must be stony-only.");
        IntegrationAssert.True(
            modernDef.stuffCategories?.Count == 1 &&
            modernDef.stuffCategories[0].defName == "Metallic",
            "Modern cookware must no longer expose the primitive stone path.");
        IntegrationAssert.True(
            !string.Equals(
                primitiveDef.graphicData?.texPath,
                modernDef.graphicData?.texPath,
                StringComparison.Ordinal),
            "Primitive cookware must own a distinct graphic path.");
        IntegrationAssert.Equal(
            KitchenMaterialKind.PrimitiveStone,
            primitiveDef.GetModExtension<KitchenwareExtension>()?.baseGraphicMaterialKind,
            "Finalized primitive cookware must treat its distinct art as the base stone family.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedPortableWareIsSellableAndTraderStockRemainsLowAndThematic()
    {
        var primitive = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PrimitiveCookware");
        var cookware = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var plate = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutlery = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var knife = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_ChefsKnife");
        var glitter = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware");

        foreach (var definition in new[] { primitive, cookware, plate, cutlery, knife, glitter })
        {
            IntegrationAssert.Equal(
                Tradeability.All,
                definition.tradeability,
                $"Portable ware {definition.defName} must be buyable and sellable.");
        }

        AssertClassifiedStock("Caravan_Neolithic_BulkGoods", primitive, -3, 1);
        AssertClassifiedStock("Base_Neolithic_Standard", primitive, -3, 1);
        AssertClassifiedStock("Caravan_Outlander_BulkGoods", cookware, -2, 1);
        AssertClassifiedStock("Base_Outlander_Standard", cookware, -2, 1);
        AssertClassifiedStock("Orbital_BulkGoods", knife, -3, 1);
        AssertSingleDefStock("Caravan_Outlander_Exotic", glitter, -3, 1);
        AssertSingleDefStock("Orbital_Exotic", glitter, -3, 1);

        static void AssertClassifiedStock(
            string traderDefName,
            ThingDef product,
            int expectedMinimum,
            int expectedMaximum)
        {
            var trader = DefDatabase<TraderKindDef>.GetNamed(traderDefName);
            var generator = trader.stockGenerators
                .OfType<StockGenerator_Kitchenware>()
                .Single(candidate => candidate.HandlesThingDef(product));
            var eligible = generator.EligibleStuffs().ToArray();
            IntegrationAssert.True(
                eligible.Length > 0,
                $"{traderDefName} must resolve eligible Stuff for {product.defName}.");
            IntegrationAssert.Equal(
                expectedMinimum,
                generator.countRange.min,
                $"{traderDefName} must retain the declared low-stock lower bound for {product.defName}.");
            IntegrationAssert.Equal(
                expectedMaximum,
                generator.countRange.max,
                $"{traderDefName} must retain the declared low-stock upper bound for {product.defName}.");
        }

        static void AssertSingleDefStock(
            string traderDefName,
            ThingDef product,
            int expectedMinimum,
            int expectedMaximum)
        {
            var trader = DefDatabase<TraderKindDef>.GetNamed(traderDefName);
            var generator = trader.stockGenerators
                .OfType<StockGenerator_SingleDef>()
                .Single(candidate => candidate.HandlesThingDef(product));
            IntegrationAssert.Equal(expectedMinimum, generator.countRange.min, traderDefName);
            IntegrationAssert.Equal(expectedMaximum, generator.countRange.max, traderDefName);
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedChefsKnifeIsBeltApparelWithoutSanitationState()
    {
        var knife = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_ChefsKnife");
        var recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeChefsKnife");

        IntegrationAssert.Equal(
            typeof(Apparel),
            knife.thingClass,
            "The chef's knife set must instantiate as apparel rather than weapon equipment.");
        IntegrationAssert.True(knife.IsApparel, "The finalized chef's knife Def must be classified as apparel.");
        IntegrationAssert.Equal(
            "None",
            knife.equipmentType.ToString(),
            "The finalized chef's knife Def must not declare a weapon equipment type.");
        IntegrationAssert.NotNull(knife.apparel, "The chef's knife set must finalize apparel properties.");
        IntegrationAssert.True(
            knife.apparel!.bodyPartGroups.Any(group => group.defName == "Waist") &&
            knife.apparel.layers.Any(layer => layer.defName == "Belt"),
            "The chef's knife set must occupy the waist belt layer.");
        IntegrationAssert.True(
            knife.comps.All(properties => properties.compClass != typeof(CompSanitation)),
            "A personal chef's knife must not acquire mutable dish-sanitation state.");
        IntegrationAssert.True(
            knife.comps.All(properties => properties.compClass != typeof(CompEquippable)),
            "A personal chef's knife must not finalize an equippable weapon component.");
        IntegrationAssert.Equal(
            6f,
            recipe.ingredients[0].GetBaseCount(),
            "The finalized machining recipe must consume six units of one eligible metal.");
        IntegrationAssert.True(
            recipe.recipeUsers.Any(user => user.defName == "TableMachining"),
            "The chef's knife recipe must remain on the machining table.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedGlitterworldCookwareIsTradeOnlyAndSelfCleaning()
    {
        var glitterworld = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware");
        var extension = glitterworld.GetModExtension<KitchenwareExtension>();

        IntegrationAssert.Equal(
            Tradeability.All,
            glitterworld.tradeability,
            "Glitterworld cookware must be eligible for trader stock and resale.");
        IntegrationAssert.True(
            glitterworld.generateCommonality > 0f,
            "Glitterworld cookware must remain eligible for generated trader and quest stock.");
        IntegrationAssert.Equal(
            TechLevel.Spacer,
            glitterworld.techLevel,
            "Glitterworld cookware must retain its imported spacer-tech identity.");
        IntegrationAssert.NotNull(extension, "Glitterworld cookware must finalize its kitchenware extension.");
        IntegrationAssert.True(
            extension!.fixedMaterialKind == KitchenMaterialKind.Glitterworld && extension.selfCleaning,
            "Glitterworld cookware must keep its fixed exceptional material profile and self-cleaning behavior.");
        IntegrationAssert.True(
            !DefDatabase<RecipeDef>.AllDefsListForReading.Any(recipe =>
                recipe.products?.Any(product => product.thingDef == glitterworld) == true),
            "No finalized recipe may manufacture glitterworld cookware.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void OptionalMasonryRecipeMatchesTheRealLoadedModSet()
    {
        var masonryLoaded = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(
                mod.PackageId,
                "argon.expandedmaterials.masonry",
                StringComparison.OrdinalIgnoreCase));
        var adobeRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_MakeAdobePlates");

        if (!masonryLoaded)
        {
            IntegrationAssert.Null(
                adobeRecipe,
                "The fixed adobe recipe must not exist when Expanded Materials - Masonry is absent.");
            return;
        }

        var adobeBricks = DefDatabase<ThingDef>.GetNamedSilentFail("EM_AdobeBricks");
        IntegrationAssert.NotNull(adobeRecipe, "The loaded masonry patch must add the adobe plate recipe.");
        IntegrationAssert.NotNull(adobeBricks, "The real masonry mod must provide EM_AdobeBricks.");
        IntegrationAssert.Null(
            adobeBricks!.stuffProps,
            "EM_AdobeBricks must remain a fixed ingredient rather than being misrepresented as Stuff.");
        IntegrationAssert.True(
            adobeRecipe!.recipeUsers.Any(user => user.defName == "CraftingSpot"),
            "The active adobe recipe must be available at the crafting spot.");
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
    public static void ImportedMealPlatingQueueAndDiningGateAreFinalized()
    {
        var job = DefDatabase<JobDef>.GetNamedSilentFail("ImmersiveChefs_PlateMeals");
        var workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail("ImmersiveChefs_PlateMeals");
        IntegrationAssert.NotNull(job, "The dedicated imported-meal plating JobDef must finalize.");
        IntegrationAssert.NotNull(workGiver, "The dedicated imported-meal plating WorkGiverDef must finalize.");
        IntegrationAssert.Equal(
            typeof(JobDriver_PlateMeals),
            job!.driverClass,
            "The plating JobDef must use the native plating driver.");
        IntegrationAssert.Equal(
            DefDatabase<WorkTypeDef>.GetNamed("Cooking"),
            workGiver!.workType,
            "Imported-meal plating must be governed by Cooking work.");
        IntegrationAssert.Equal(
            typeof(WorkGiver_PlateMeals),
            workGiver.giverClass,
            "The finalized WorkGiver must scan for imported unplated meals.");

        var diningBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "IsFoodSourceOnMapSociallyProper",
            new[] { typeof(Thing), typeof(Pawn), typeof(Pawn), typeof(bool) });
        IntegrationAssert.NotNull(
            diningBoundary,
            "The vanilla food-selection boundary must exist for the plating gate.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(diningBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name == "ImportedMealDiningGatePatch") == true,
            "Immersive Chefs must patch the finalized normal-dining selection boundary.");

        var optimalityBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "FoodOptimality",
            new[] { typeof(Pawn), typeof(Thing), typeof(ThingDef), typeof(float), typeof(bool) });
        IntegrationAssert.NotNull(
            optimalityBoundary,
            "The vanilla food-optimality boundary must exist for plated-meal precedence.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(optimalityBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name == "PlatedMealFoodOptimalityPatch") == true,
            "Immersive Chefs must install the finalized plated-meal tie breaker.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ImportedMealScannerDoesNotAdvertiseAnUnavailablePlatingJob()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var worker = map.mapPawns.FreeColonistsSpawned.First();
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var mealCell = CellFinder.RandomClosewalkCellNear(worker.Position, map, 4);
        var priorMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var priorFallback = ImmersiveChefsMod.Settings.DirtyWareFallback;
        var plateStates = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == KitchenwareProduct.Plate)
            .Select(thing => (Thing: thing, Forbidden: thing.IsForbidden(worker)))
            .ToList();
        var hungerStates = map.mapPawns.FreeColonistsSpawned
            .Where(pawn => pawn.needs?.food is not null)
            .Select(pawn => (Pawn: pawn, Level: pawn.needs.food.CurLevel))
            .ToList();

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            ImmersiveChefsMod.Settings.DirtyWareFallback = DirtyWareFallback.Never;
            foreach (var state in plateStates)
            {
                state.Thing.SetForbidden(true, warnOnFail: false);
            }

            foreach (var state in hungerStates)
            {
                state.Pawn.needs.food.CurLevel = state.Pawn.needs.food.MaxLevel;
            }

            GenSpawn.Spawn(meal, mealCell, map);
            var scanner = new WorkGiver_PlateMeals();
            var hasJob = scanner.HasJobOnThing(worker, meal);
            var job = scanner.JobOnThing(worker, meal);

            IntegrationAssert.True(!hasJob,
                "The scanner must not advertise an imported-meal target when no permitted plate is available.");
            IntegrationAssert.Null(job,
                "The paired JobOnThing call must agree with the scanner instead of triggering RimWorld's no-actual-job error.");
            IntegrationAssert.True(meal.GetComp<CompEmbeddedWare>().PlatingOpportunityFailed,
                "Prefer mode must still remember the failed real plating opportunity.");
        }
        finally
        {
            foreach (var state in hungerStates)
            {
                if (!state.Pawn.Destroyed)
                {
                    state.Pawn.needs.food.CurLevel = state.Level;
                }
            }

            foreach (var state in plateStates)
            {
                if (!state.Thing.Destroyed)
                {
                    state.Thing.SetForbidden(state.Forbidden, warnOnFail: false);
                }
            }

            ImmersiveChefsMod.Settings.WareRequirementMode = priorMode;
            ImmersiveChefsMod.Settings.DirtyWareFallback = priorFallback;
            if (!meal.Destroyed) meal.Destroy(DestroyMode.Vanish);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void GeneratedMealOriginsArePatchedAndPlateOnlyExplicitOrigins()
    {
        var pawnBoundary = AccessTools.Method(
            typeof(PawnInventoryGenerator),
            nameof(PawnInventoryGenerator.GenerateInventoryFor),
            new[] { typeof(Pawn), typeof(PawnGenerationRequest) });
        var traderBoundary = AccessTools.Method(
            typeof(ThingSetMaker_TraderStock),
            "Generate",
            new[] { typeof(ThingSetMakerParams), typeof(List<Thing>) });
        IntegrationAssert.NotNull(
            pawnBoundary,
            "The finalized pawn-inventory generation boundary must exist.");
        IntegrationAssert.NotNull(
            traderBoundary,
            "The finalized trader-stock generation boundary must exist.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(pawnBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name ==
                "GeneratedPawnInventoryMealPlatingPatch") == true,
            "Immersive Chefs must own the external-pawn inventory generation postfix.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(traderBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name ==
                "GeneratedTraderStockMealPlatingPatch") == true,
            "Immersive Chefs must own the trader-stock generation postfix.");

        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealFine);
        meal.stackCount = 2;
        try
        {
            var embedded = meal.GetComp<CompEmbeddedWare>();
            IntegrationAssert.NotNull(
                embedded,
                "A covered generated meal must expose physical plate bindings.");
            IntegrationAssert.Equal(
                0,
                GeneratedMealPlatingRuntime.EnsurePlated(
                    meal,
                    GeneratedMealOrigin.GenericOrUnknown),
                "Generic/debug/mod origin must preserve honest plate absence.");
            IntegrationAssert.Equal(
                0,
                embedded!.EmbeddedPlateCount,
                "A generic generated meal must remain visibly unplated.");
            IntegrationAssert.Equal(
                2,
                GeneratedMealPlatingRuntime.EnsurePlated(
                    meal,
                    GeneratedMealOrigin.TradeStock),
                "An explicit trade origin must attach one real plate per serving.");
            IntegrationAssert.Equal(
                0,
                GeneratedMealPlatingRuntime.EnsurePlated(
                    meal,
                    GeneratedMealOrigin.TradeStock),
                "Repeated origin handling must not duplicate existing plates.");
            IntegrationAssert.Equal(
                2,
                embedded.EmbeddedPlateCount,
                "The generated stack must retain exactly one plate per serving.");
            foreach (var binding in embedded.Bindings)
            {
                IntegrationAssert.True(
                    binding.Quality == (int)QualityCategory.Poor,
                    "Every generated origin plate must have Poor craftsmanship quality.");
                IntegrationAssert.True(
                    !binding.IsDirty,
                    "Every generated origin plate must begin clean.");
            }

            IntegrationAssert.True(
                PlateMaterialEligibilityRuntime.Allows(
                    embedded.PeekPlateThing()!,
                    MealComplexity.Advanced),
                "A generated Fine-meal plate must satisfy the finalized material tier.");
            IntegrationAssert.True(
                embedded.Bindings.All(binding => binding.StuffDefName == "Steel"),
                "With the exact Core-only material set, cheap Fine-meal origin plates must use steel rather than precious metal.");
        }
        finally
        {
            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void DishwasherCapacitiesAreMaterializedAtDefFinalization()
    {
        var expectedScale = ImmersiveChefsMod.Settings.DishwasherCapacityScale;
        var domestic = ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher.comps?
            .OfType<CompProperties_Dishwasher>()
            .SingleOrDefault();
        var industrial = ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher.comps?
            .OfType<CompProperties_Dishwasher>()
            .SingleOrDefault();

        IntegrationAssert.NotNull(
            domestic,
            "The domestic dishwasher must retain its finalized capacity properties.");
        IntegrationAssert.NotNull(
            industrial,
            "The industrial dishwasher must retain its finalized capacity properties.");
        IntegrationAssert.True(
            Math.Abs(domestic!.basePlateCapacity - (16f * expectedScale)) < 0.001f,
            "The domestic dishwasher must materialize the restart-only scale into its finalized Def.");
        IntegrationAssert.True(
            Math.Abs(industrial!.basePlateCapacity - (64f * expectedScale)) < 0.001f,
            "The industrial dishwasher must materialize the restart-only scale into its finalized Def.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodWorkGiverTargetsOnlyThePrepStation()
    {
        var workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(
            "ImmersiveChefs_PrepareIngredients");
        IntegrationAssert.NotNull(
            workGiver,
            "The dedicated prepared-food WorkGiverDef must finalize.");
        IntegrationAssert.Equal(
            typeof(WorkGiver_DoBill),
            workGiver!.giverClass,
            "Prepared ingredients must use RimWorld's native bill workgiver.");
        IntegrationAssert.Equal(
            DefDatabase<WorkTypeDef>.GetNamed("Cooking"),
            workGiver.workType,
            "Prepared-food bills must remain governed by Cooking work.");
        IntegrationAssert.Equal(
            1,
            workGiver.fixedBillGiverDefs?.Count ?? 0,
            "Prepared-food work must scan exactly one explicit bill-giver Def.");
        IntegrationAssert.Equal(
            "ImmersiveChefs_PrepStation",
            workGiver.fixedBillGiverDefs![0].defName,
            "Prepared-food work must target only the ingredient prep station.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodBillsEvaluateEveryHiddenSourceWithoutChangingOrdinaryFilters()
    {
        var ingredientBoundary = AccessTools.Method(
            typeof(Bill),
            nameof(Bill.IsFixedOrAllowedIngredient),
            new[] { typeof(Thing) });
        IntegrationAssert.True(
            Harmony.GetPatchInfo(ingredientBoundary)?.Owners.Contains(ImmersiveChefsMod.PackageId) == true,
            "The loaded mod must own the prepared-food bill ingredient boundary.");

        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var riceDef = DefDatabase<ThingDef>.GetNamed("RawRice");
        var humanMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Human");
        var prepared = ThingMaker.MakeThing(preparedDef);
        var preparedComp = (prepared as ThingWithComps)?.GetComp<CompPreparedFood>();
        IntegrationAssert.NotNull(preparedComp, "The finalized prepared-food Def must expose provenance.");
        preparedComp!.Initialize(new PreparedFoodState(
            new[]
            {
                new IngredientContribution(riceDef.defName, 0.025f, 1),
                new IngredientContribution(humanMeatDef.defName, 0.025f, 1)
            },
            preparationQuality: 50,
            preparerThingId: null,
            DietaryFlags.Plant | DietaryFlags.HumanMeat,
            exactSourcesHidden: true,
            ingredientPoisonChance: 0f));

        var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var ordinaryPrepared = ThingMaker.MakeThing(preparedDef);
        var ordinaryPreparedComp = (ordinaryPrepared as ThingWithComps)?.GetComp<CompPreparedFood>();
        IntegrationAssert.NotNull(
            ordinaryPreparedComp,
            "The ordinary prepared-food fixture must expose provenance.");
        ordinaryPreparedComp!.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution(riceDef.defName, 0.05f, 1) },
            preparationQuality: 50,
            preparerThingId: null,
            DietaryFlags.Plant | DietaryFlags.VegetarianCompatible,
            exactSourcesHidden: false,
            ingredientPoisonChance: 0f));
        var preparedOnlyBill = new Bill_Production(recipe);
        preparedOnlyBill.ingredientFilter.SetDisallowAll();
        preparedOnlyBill.ingredientFilter.SetAllow(preparedDef, true);
        IntegrationAssert.True(
            preparedOnlyBill.IsFixedOrAllowedIngredient(ordinaryPrepared),
            "A bill restricted to visible prepared food must not also require every visible raw source Def.");

        var bill = new Bill_Production(recipe);
        bill.ingredientFilter.SetAllow(preparedDef, true);
        bill.ingredientFilter.SetAllow(riceDef, true);
        bill.ingredientFilter.SetAllow(humanMeatDef, false);

        var ordinaryFilter = new ThingFilter();
        ordinaryFilter.SetAllow(preparedDef, true);
        IntegrationAssert.True(
            ordinaryFilter.Allows(prepared),
            "Prepared provenance must not alter ordinary stockpile-style ThingFilter evaluation.");
        IntegrationAssert.True(
            !bill.IsFixedOrAllowedIngredient(prepared),
            "One hidden disallowed source must reject the whole prepared stack from the bill.");

        bill.ingredientFilter.SetAllow(humanMeatDef, true);
        IntegrationAssert.True(
            bill.IsFixedOrAllowedIngredient(prepared),
            "The same prepared stack must become eligible when every hidden source is allowed.");

        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var mealIngredients = (meal as ThingWithComps)?.GetComp<CompIngredients>();
        var culinary = (meal as ThingWithComps)?.GetComp<CompCulinaryState>();
        IntegrationAssert.NotNull(mealIngredients, "A finalized covered meal must expose vanilla ingredients.");
        IntegrationAssert.NotNull(culinary, "A finalized covered meal must expose culinary serving state.");
        culinary!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                40f,
                ContaminationSources.None,
                0,
                0,
                new[] { riceDef.defName, humanMeatDef.defName },
                DietaryFlags.Plant | DietaryFlags.HumanMeat)
        });

        var foodPolicy = new FoodPolicy(9001, "Immersive Chefs integration policy");
        foodPolicy.filter.SetAllow(meal.def, true);
        foodPolicy.filter.SetAllow(riceDef, true);
        foodPolicy.filter.SetAllow(humanMeatDef, false);
        IntegrationAssert.True(
            !foodPolicy.Allows(meal),
            "A finished hidden-source meal must remain forbidden when one source is forbidden.");
        IntegrationAssert.True(
            !mealIngredients!.ingredients.Contains(humanMeatDef),
            "Food-policy evaluation must not reveal hidden source Defs through CompIngredients.");

        foodPolicy.filter.SetAllow(humanMeatDef, true);
        IntegrationAssert.True(
            foodPolicy.Allows(meal),
            "A finished hidden-source meal must become allowed when every source is allowed.");

        var thoughtBoundary = AccessTools.Method(
            typeof(FoodUtility),
            nameof(FoodUtility.ThoughtsFromIngesting),
            new[] { typeof(Pawn), typeof(Thing), typeof(ThingDef) });
        IntegrationAssert.True(
            Harmony.GetPatchInfo(thoughtBoundary)?.Owners.Contains(ImmersiveChefsMod.PackageId) == true,
            "The loaded mod must own the ingestion-thought provenance scope.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void HiddenPreparedSourcesDriveVanillaIngredientThoughtsAndRemainHiddenAfterward()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var humanMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Human");
        var mealIngredients = (meal as ThingWithComps)?.GetComp<CompIngredients>();
        var culinary = (meal as ThingWithComps)?.GetComp<CompCulinaryState>();
        IntegrationAssert.NotNull(mealIngredients, "The thought fixture meal must expose vanilla ingredients.");
        IntegrationAssert.NotNull(culinary, "The thought fixture meal must expose culinary state.");
        culinary!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                40f,
                ContaminationSources.None,
                0,
                0,
                new[] { humanMeatDef.defName },
                DietaryFlags.HumanMeat)
        });
        var ingredientThought = DefDatabase<ThoughtDef>.GetNamed("AteHumanlikeMeatAsIngredient");
        IntegrationAssert.True(
            !mealIngredients!.ingredients.Contains(humanMeatDef),
            "The hidden source must not be present before thought evaluation.");

        try
        {
            var thoughts = FoodUtility.ThoughtsFromIngesting(pawn, meal, meal.def);

            IntegrationAssert.True(
                thoughts.Any(thought => thought.thought == ingredientThought),
                "Vanilla thought evaluation must observe the hidden human-meat source.");
            IntegrationAssert.True(
                !mealIngredients.ingredients.Contains(humanMeatDef),
                "The hidden source must be removed immediately after thought evaluation.");
        }
        finally
        {
            pawn.Destroy(DestroyMode.Vanish);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodRoundTripsThroughTheRealScribePipeline()
    {
        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var original = (ThingWithComps)ThingMaker.MakeThing(preparedDef);
        original.stackCount = 7;
        var originalPrepared = original.GetComp<CompPreparedFood>();
        var originalRottable = original.GetComp<CompRottable>();
        IntegrationAssert.NotNull(originalPrepared, "The finalized prepared-food Def must expose provenance.");
        IntegrationAssert.NotNull(originalRottable, "The finalized prepared-food Def must expose rot state.");
        originalPrepared!.Initialize(new PreparedFoodState(
            new[]
            {
                new IngredientContribution("RawRice", 0.035f, 4, 72),
                new IngredientContribution("Meat_Human", 0.015f, 2, 31)
            },
            preparationQuality: 83,
            preparerThingId: "Thing_Preparer4242",
            dietaryFlags: DietaryFlags.Plant | DietaryFlags.Animal | DietaryFlags.HumanMeat,
            exactSourcesHidden: true,
            ingredientPoisonChance: 0.0375f));
        originalRottable!.RotProgress = 1234.5f;

        var path = Path.Combine(
            GenFilePaths.TempFolderPath,
            "immersive-chefs-prepared-food-roundtrip-" + Guid.NewGuid().ToString("N") + ".xml");
        Thing? loaded = null;
        try
        {
            Thing originalForScribe = original;
            try
            {
                Scribe.saver.InitSaving(path, "preparedFoodRoundTrip");
                Scribe_Deep.Look(ref originalForScribe, "thing");
                Scribe.saver.FinalizeSaving();
            }
            catch
            {
                Scribe.saver.ForceStop();
                throw;
            }

            try
            {
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref loaded, "thing");
                Scribe.loader.FinalizeLoading();
            }
            catch
            {
                Scribe.loader.ForceStop();
                throw;
            }

            var loadedWithComps = loaded as ThingWithComps;
            var loadedPrepared = loadedWithComps?.GetComp<CompPreparedFood>();
            var loadedRottable = loadedWithComps?.GetComp<CompRottable>();
            IntegrationAssert.NotNull(loadedWithComps, "Scribe must reconstruct a real prepared-food Thing.");
            IntegrationAssert.Equal(7, loadedWithComps!.stackCount, "Scribe must preserve the prepared stack count.");
            IntegrationAssert.NotNull(loadedPrepared, "Scribe must reconstruct the prepared-food component.");
            IntegrationAssert.NotNull(loadedRottable, "Scribe must reconstruct the rot component.");
            IntegrationAssert.Equal(83, loadedPrepared!.PreparationQuality);
            IntegrationAssert.Equal("Thing_Preparer4242", loadedPrepared.PreparerThingId);
            IntegrationAssert.Equal(
                DietaryFlags.Plant | DietaryFlags.Animal | DietaryFlags.HumanMeat,
                loadedPrepared.DietaryFlags);
            IntegrationAssert.True(loadedPrepared.ExactSourcesHidden);
            IntegrationAssert.True(Math.Abs(loadedPrepared.IngredientPoisonChance - 0.0375f) < 0.0001f);
            IntegrationAssert.True(Math.Abs(loadedPrepared.NutritionPerItem - 0.05f) < 0.0001f);
            IntegrationAssert.Equal(2, loadedPrepared.Contributions.Count);
            IntegrationAssert.True(
                loadedPrepared.Contributions.Any(value =>
                    value.DefName == "RawRice" &&
                    Math.Abs(value.Nutrition - 0.035f) < 0.0001f &&
                    value.SourceCount == 4 &&
                    value.CraftsmanshipScore == 72));
            IntegrationAssert.True(
                loadedPrepared.Contributions.Any(value =>
                    value.DefName == "Meat_Human" &&
                    Math.Abs(value.Nutrition - 0.015f) < 0.0001f &&
                    value.SourceCount == 2 &&
                    value.CraftsmanshipScore == 31));
            IntegrationAssert.True(Math.Abs(loadedRottable!.RotProgress - 1234.5f) < 0.01f);
            IntegrationAssert.True(
                loadedPrepared.CompInspectStringExtra().IndexOf(
                    "ImmersiveChefs_PreparedSource_NutrientPaste".Translate().ToString(),
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "The reconstructed hidden-source stack must remain opaque in ordinary inspection. " +
                "Actual: " + loadedPrepared.CompInspectStringExtra());
        }
        finally
        {
            if (!original.Destroyed)
            {
                original.Destroy(DestroyMode.Vanish);
            }

            if (loaded is not null && !loaded.Destroyed)
            {
                loaded.Destroy(DestroyMode.Vanish);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedTravelFoodsUseTheCoverageContract()
    {
        IntegrationAssert.True(
            MealCoveragePolicy.IsCovered(ThingDefOf.MealSimple),
            "A normal finalized meal must keep Immersive Chefs state while travelling.");
        foreach (var excludedFood in new[] { ThingDefOf.Pemmican, ThingDefOf.MealSurvivalPack })
        {
            IntegrationAssert.True(
                !MealCoveragePolicy.IsCovered(excludedFood),
                $"{excludedFood.defName} must remain a hand-eaten travel-food exclusion.");
            IntegrationAssert.True(
                excludedFood.comps.All(comp => comp.compClass != typeof(CompEmbeddedWare)),
                $"{excludedFood.defName} must not receive embedded serving ware.");
            IntegrationAssert.True(
                excludedFood.comps.All(comp => comp.compClass != typeof(CompCulinaryState)),
                $"{excludedFood.defName} must not receive culinary state or temperature handling.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedRecipeWorkUsesOnlyTheExactComplexityTable()
    {
        var workAmountMethod = AccessTools.Method(typeof(RecipeDef), nameof(RecipeDef.WorkAmountForStuff));
        var ownedPostfixes = Harmony.GetPatchInfo(workAmountMethod)?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(
            1,
            ownedPostfixes,
            "Recipe work amount must have exactly one Immersive Chefs Harmony postfix.");

        foreach (var defName in new[] { "CookMealSimple", "CookMealSimpleBulk" })
        {
            AssertWorkMultiplier(defName, 0.75f);
        }

        foreach (var defName in new[]
                 {
                     "CookMealFine", "CookMealFine_Veg", "CookMealFine_Meat",
                     "CookMealFineBulk", "CookMealFineBulk_Meat", "CookMealFineBulk_Veg"
                 })
        {
            AssertWorkMultiplier(defName, 2f);
        }

        foreach (var defName in new[]
                 {
                     "CookMealLavish", "CookMealLavish_Meat", "CookMealLavish_Veg",
                     "CookMealLavishBulk", "CookMealLavishBulk_Veg", "CookMealLavishBulk_Meat"
                 })
        {
            AssertWorkMultiplier(defName, 3f);
        }

        AssertWorkMultiplier("CookMealSurvival", 1f);
        AssertWorkMultiplier("Make_Pemmican", 1f);

        var vanillaCookingExpandedLoaded = IsPackageActive(
            MealClassificationCatalog.VanillaCookingExpandedPackageId);
        var simpleBake = DefDatabase<RecipeDef>.GetNamedSilentFail("VCE_CookBakeSimple");
        if (vanillaCookingExpandedLoaded)
        {
            IntegrationAssert.NotNull(
                simpleBake,
                "The active Vanilla Cooking Expanded matrix must finalize its simple-bake recipe.");
            AssertWorkMultiplier("VCE_CookBakeSimple", 0.75f);
        }
        else
        {
            IntegrationAssert.Null(
                simpleBake,
                "The base matrix must not invent a Vanilla Cooking Expanded recipe.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedOptionalMealRegistriesMatchTheExactLoadedDefs()
    {
        if (IsPackageActive(MealClassificationCatalog.VanillaCookingExpandedPackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[]
                {
                    "VCE_CookBakeSimple", "VCE_CookBakeSimpleBulk",
                    "VCE_CookGrillSimple", "VCE_CookGrillSimpleBulk", "VCE_CookSoupSimple"
                },
                new[]
                {
                    "VCE_SimpleBake", "VCE_SimpleGrill", "VCE_RuinedSimpleGrill",
                    "VCE_CookedSoupSimple"
                });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[]
                {
                    "VCE_CookBakeFine", "VCE_CookBakeFineBulk",
                    "VCE_CookGrillFine", "VCE_CookGrillFineBulk", "VCE_CookSoupFine"
                },
                new[]
                {
                    "VCE_FineBake", "VCE_FineGrill", "VCE_RuinedFineGrill",
                    "VCE_CookedSoupFine"
                });
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[]
                {
                    "VCE_CookBakeLavish", "VCE_CookBakeLavishBulk", "VCE_CookBakeGourmet",
                    "VCE_CookGrillLavish", "VCE_CookGrillLavishhBulk", "VCE_CookGrillGourmet",
                    "VCE_CookMealGourmet", "VCE_CookSoupLavish", "VCE_CookSoupGourmet"
                },
                new[]
                {
                    "VCE_LavishBake", "VCE_GourmetBake", "VCE_LavishGrill", "VCE_GourmetGrill",
                    "VCE_RuinedLavishGrill", "VCE_RuinedGourmetGrill", "VCE_MealGourmet",
                    "VCE_CookedSoupLavish", "VCE_CookedSoupGourmet"
                });
        }

        if (IsPackageActive(MealClassificationCatalog.VanillaCookingExpandedHautePackageId))
        {
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[] { "VCE_CookMealHaute" },
                new[] { "VCE_MealHaute" });
        }

        if (IsPackageActive(MealClassificationCatalog.VanillaCookingExpandedStewsPackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[] { "VCE_CookStewSimple" },
                new[] { "VCE_CookedStewSimple" });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[] { "VCE_CookStewFine" },
                new[] { "VCE_CookedStewFine" });
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[] { "VCE_CookStewLavish" },
                new[] { "VCE_CookedStewLavish" });
        }

        if (IsPackageActive(MealClassificationCatalog.VanillaCookingExpandedSushiPackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[]
                {
                    "VCE_CookChirashizushiSimple", "VCE_CookChirashizushiSimpleBulk",
                    "VCE_CookNorimakiSimple", "VCE_CookNorimakiSimpleBulk"
                },
                new[] { "VCE_Chirashizushi", "VCE_Norimaki" });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[]
                {
                    "VCE_CookUramakiFine", "VCE_CookUramakiFineBulk",
                    "VCE_CookNigiriFine", "VCE_CookNigiriFineBulk"
                },
                new[] { "VCE_Uramaki", "VCE_Nigiri" });
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[]
                {
                    "VCE_CookTemakiLavish", "VCE_CookTemakiLavishBulk",
                    "VCE_CookFutomakiLavish", "VCE_CookFutomakiLavishBulk",
                    "VCE_CookGunkanmakiGourmet", "VCE_CookOshizushiiGourmet"
                },
                new[] { "VCE_Temaki", "VCE_Futomaki", "VCE_Gunkanmaki", "VCE_Oshizushi" });
        }

        if (IsPackageActive(MealClassificationCatalog.FriedMealsPackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[] { "CookFritterSimple", "CookFritterSimpleBulk" },
                new[] { "ucp_SimpleFritter" });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[] { "CookFritterFine", "CookFritterFineBulk" },
                new[] { "ucp_FineFritter" });
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[] { "CookFritterLavish", "CookFritterLavishBulk" },
                new[] { "ucp_LavishFritter" });
            if (IsPackageActive(MealClassificationCatalog.VanillaCookingExpandedPackageId))
            {
                AssertMealRegistry(
                    MealComplexity.Elaborate,
                    new[] { "VCE_CookFritterGourmet" },
                    new[] { "ucp_GourmetFritter" });
            }
        }

        if (IsPackageActive(MealClassificationCatalog.FastMealsPackageId))
        {
            AssertFastMealRegistry(
                MealComplexity.Simple,
                new[] { "CM_CookFastMeal", "CM_CookFastMealBulk" },
                new[] { "CM_SimpleFastMeal" });
            AssertFastMealRegistry(
                MealComplexity.Advanced,
                new[]
                {
                    "CM_CookFastMealDeluxe", "CM_CookFastMealDeluxe_Meat",
                    "CM_CookFastMealDeluxe_Veg", "CM_CookFastMealDeluxeBulk",
                    "CM_CookFastMealDeluxeBulk_Meat", "CM_CookFastMealDeluxeBulk_Veg"
                },
                new[] { "CM_DeluxeFastMeal", "CM_DeluxeFastMeal_Meat", "CM_DeluxeFastMeal_Veg" });
        }

        if (IsPackageActive(MealClassificationCatalog.RimCuisineCorePackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[] { "CookThinPottage" },
                new[] { "RC2_ThinPottage" });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[] { "RC2_CookThickPottage" },
                new[] { "RC2_ThickPottage" });
        }

        if (IsPackageActive(MealClassificationCatalog.RimCuisineMealsPackageId))
        {
            AssertMealRegistry(
                MealComplexity.Simple,
                new[] { "RC2_CookRubaboo" },
                new[] { "RC2_Rubaboo" });
            AssertMealRegistry(
                MealComplexity.Advanced,
                new[] { "RC2_CookFineMealBulk" },
                Array.Empty<string>());
            AssertMealRegistry(
                MealComplexity.Elaborate,
                new[]
                {
                    "RC2_CookLavishMealBulk", "RC2_CookExtravagantMeal",
                    "RC2_CookExtravagantMealBulk"
                },
                new[] { "RC2_Pizza", "RC2_ExtravagantMeal" });
        }
    }

    private static void AssertMealRegistry(
        MealComplexity expected,
        IEnumerable<string> recipeDefNames,
        IEnumerable<string> mealDefNames)
    {
        foreach (var recipeDefName in recipeDefNames)
        {
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            IntegrationAssert.NotNull(recipe, $"The active package must finalize recipe {recipeDefName}.");
            IntegrationAssert.Equal(
                expected,
                MealClassificationRuntime.ClassifyRecipe(recipe)!.Value,
                $"{recipeDefName} must use its explicit compatibility tier.");
        }

        foreach (var mealDefName in mealDefNames)
        {
            var meal = DefDatabase<ThingDef>.GetNamedSilentFail(mealDefName);
            IntegrationAssert.NotNull(meal, $"The active package must finalize meal {mealDefName}.");
            IntegrationAssert.Equal(
                expected,
                MealComplexityRuntime.Classify(meal)!.Value,
                $"{mealDefName} must use its explicit compatibility tier.");
        }
    }

    private static void AssertFastMealRegistry(
        MealComplexity expected,
        IEnumerable<string> recipeDefNames,
        IEnumerable<string> mealDefNames)
    {
        AssertMealRegistry(expected, recipeDefNames, mealDefNames);
        foreach (var recipeDefName in recipeDefNames)
        {
            AssertWorkMultiplier(recipeDefName, 1f);
        }
    }

    private static bool IsPackageActive(string packageId)
    {
        return LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertWorkMultiplier(string defName, float expectedMultiplier)
    {
        var recipe = DefDatabase<RecipeDef>.GetNamed(defName);
        var actualMultiplier = RecipeWorkRuntime.MultiplierFor(recipe);
        IntegrationAssert.True(
            Math.Abs(actualMultiplier - expectedMultiplier) < 0.0001f,
            $"{defName} must retain the exact {expectedMultiplier:0.##}x complexity multiplier.");
        var baseWorkAmount = recipe.workAmount >= 0f
            ? recipe.workAmount
            : recipe.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake, null);
        var actualWorkAmount = recipe.WorkAmountForStuff(null);
        var expectedWorkAmount = baseWorkAmount * expectedMultiplier;
        IntegrationAssert.True(
            Math.Abs(actualWorkAmount - expectedWorkAmount) < 0.01f,
            $"{defName} must expose its Harmony-adjusted finalized work amount; " +
            $"expected {expectedWorkAmount:0.##} from base {baseWorkAmount:0.##}, actual {actualWorkAmount:0.##}.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactWareWashedInWildWater()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var originalPlateId = plate.ThingID;
        var originalCutleryId = cutlery.ThingID;

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var thermalStartTick = Math.Max(
                0,
                Find.TickManager.TicksGame - ThermalCalculator.TicksPerHour);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    60,
                    70f,
                    ContaminationSources.None,
                    0,
                    thermalStartTick)
            });
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The travel fixture must put its meal in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
                "The travel fixture must put its plate in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
                "The travel fixture must put its cutlery in the caravan inventory.");

            var tileAmbient = GenTemperature.GetTemperatureAtTile(caravan.Tile);
            var expectedTemperature = ThermalCalculator.TemperatureAfter(
                70f,
                tileAmbient,
                Find.TickManager.TicksGame - thermalStartTick,
                ImmersiveChefsMod.Settings.ThermalHalfLifeHours);
            var travelServing = meal.GetComp<CompCulinaryState>().PeekCurrentServing();
            IntegrationAssert.True(
                Math.Abs(meal.AmbientTemperature - tileAmbient) < 0.01f,
                "A held caravan meal must resolve RimWorld's current world-tile ambient temperature.");
            IntegrationAssert.True(
                travelServing is not null && Math.Abs(travelServing.TemperatureCelsius - expectedTemperature) < 0.01f,
                $"Caravan meal temperature must continue moving toward the current world-tile climate " +
                $"(expected {expectedTemperature:0.###}, actual {travelServing?.TemperatureCelsius:0.###}, " +
                $"ambient {tileAmbient:0.###}).");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            var returnedPlate = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalPlateId);
            var returnedCutlery = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalCutleryId);
            IntegrationAssert.True(
                ReferenceEquals(plate, returnedPlate),
                "Caravan dining must return the exact selected plate Thing without replacement or duplication.");
            IntegrationAssert.True(
                ReferenceEquals(cutlery, returnedCutlery),
                "Caravan dining must return the exact selected cutlery Thing without replacement or duplication.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed plates must retain the wild-water risk marker.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed cutlery must retain the wild-water risk marker.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactEmbeddedPlateOnlyAfterEating()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The fixture must begin with its exact plate contained by the meal.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The fixture must put its plated meal in caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
                "The fixture must put its cutlery in caravan inventory.");
            IntegrationAssert.True(
                !pawn.inventory.innerContainer.Contains(plate),
                "An embedded plate must not be a direct loose caravan inventory item before eating.");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "Eating must move the exact plate out of the consumed meal and into caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "Eating must return the exact selected cutlery to caravan inventory.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The returned embedded plate must receive caravan wild-water wash provenance.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CancelledCaravanIngestionRestoresUnusedWareWithoutWashing()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    55,
                    20f,
                    ContaminationSources.DirtyCookware,
                    0,
                    Find.TickManager.TicksGame)
            });
            pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false);

            DiningSessionRegistry.TryAttachTravel(pawn, meal);
            IntegrationAssert.True(
                ReferenceEquals(meal.GetComp<CompEmbeddedWare>().PeekPlateThing(), plate),
                "The cancellation fixture must import its exact loose caravan plate before rollback.");
            DiningSessionRegistry.BeginIngestion(pawn);
            DiningSessionRegistry.EndIngestion(pawn);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().PeekPlateThing() is null,
                "A cancelled travel attempt must detach the plate it imported into an unplated meal.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "A cancelled travel attempt must return the exact unused plate.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "A cancelled travel attempt must return the exact unused cutlery.");
            IntegrationAssert.Equal(
                ContaminationSources.DirtyCookware,
                meal.GetComp<CompCulinaryState>().PeekCurrentServing()!.Contamination,
                "A cancelled travel attempt must leave the uneaten meal's prior contamination unchanged.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "Cancellation must preserve a clean unused plate's sanitation state.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must preserve the unused plate's prior wild-water provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must not claim that the unused cutlery was washed in wild water.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanAnimalsDoNotUseOrDestroyTableware()
    {
        var animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { animal },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;
        var originalMicrowaveExtraPoisonChance = settings.MicrowaveExtraPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            settings.MicrowaveExtraPoisonChance = 5f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtyCutlery |
                    ContaminationSources.WildWaterCookware |
                    ContaminationSources.WildWaterPlate |
                    ContaminationSources.WildWaterCutlery,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The animal exclusion fixture must start with a plated meal.");
            animal.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            animal.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false);

            meal.Ingested(animal, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "An animal eating a meal must return its exact unused plate to caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "Animal ingestion must not select or consume caravan cutlery.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "An animal-excluded plate must retain its original wash provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Animal-excluded cutlery must retain its original wash provenance.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            settings.MicrowaveExtraPoisonChance = originalMicrowaveExtraPoisonChance;
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void MapAnimalsDoNotReserveUseOrDirtyTableware()
    {
        var map = Find.CurrentMap;
        var animal = PawnGenerator.GeneratePawn(
            DefDatabase<PawnKindDef>.GetNamed("Raccoon"),
            null);
        var animalCell = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell =>
            {
                var adjacent = new IntVec3(cell.x + 1, 0, cell.z);
                return adjacent.x < map.Size.x &&
                       adjacent.Standable(map) &&
                       adjacent.GetEdifice(map) is null &&
                       adjacent.GetThingList(map).Count == 0;
            });
        var mealCell = animalCell;
        var cutleryCell = new IntVec3(animalCell.x + 1, 0, animalCell.z);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Plasteel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtyCutlery,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The map animal exclusion fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(animal, animalCell, map);
            GenSpawn.Spawn(meal, mealCell, map);
            GenSpawn.Spawn(cutlery, cutleryCell, map);
            animal.needs.food.CurLevel = 0.01f;
            var originalCutleryPosition = cutlery.Position;
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            var cutleryWasReserved = false;

            animal.jobs.StartJob(ingestJob, JobCondition.InterruptForced);
            var chewMethod = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
            var chewOwners = Harmony.GetPatchInfo(chewMethod)?.Owners
                .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
            IntegrationAssert.Equal(
                1,
                chewOwners,
                "Animal map ingestion must run with exactly one Immersive Chefs chew-speed patch owner.");
            var plateSpeed = plate.GetComp<CompKitchenwareStats>().CurrentStats.CookingSpeedFactor;
            IntegrationAssert.True(
                Math.Abs(plateSpeed - 1f) > 0.1f,
                "The animal chew-speed fixture must use a plate with a distinguishable non-native factor.");
            var platedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            var releasedPlate = embedded.ReleasePlateThing();
            IntegrationAssert.True(
                ReferenceEquals(plate, releasedPlate),
                "The chew-speed fixture must temporarily release the exact embedded plate.");
            var unplatedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            IntegrationAssert.True(
                embedded.TryEmbedPlate(plate),
                "The chew-speed fixture must restore its exact plate before native ingestion.");
            IntegrationAssert.Equal(
                unplatedChew.defaultDuration,
                platedChew.defaultDuration,
                "A non-humanlike animal's native chew duration must ignore a distinguishable plate speed factor.");
            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                animal.jobs.JobTrackerTick();
                cutleryWasReserved |= map.reservationManager.IsReserved(cutlery);
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real animal JobDriver_Ingest must complete within the bounded fixture ticks.");
            IntegrationAssert.True(
                DiningSessionRegistry.CutleryFor(ingestJob) is null,
                "The real animal map-ingest job must not select nearby cutlery.");
            IntegrationAssert.True(
                DiningSessionRegistry.PlateFor(ingestJob) is null,
                "The real animal map-ingest job must not create a service-ware pickup session.");
            IntegrationAssert.True(
                !cutleryWasReserved,
                "The real animal map-ingest job must never reserve nearby cutlery.");

            IntegrationAssert.True(
                plate.Spawned && ReferenceEquals(plate.Map, map) && plate.Position == animal.Position,
                "Animal map ingestion must recover the exact embedded plate at the eating location.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The recovered animal plate must remain clean with unchanged safe provenance.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "The recovered animal plate must retain its clean sanitation flag.");
            IntegrationAssert.True(
                cutlery.Spawned && ReferenceEquals(cutlery.Map, map) &&
                cutlery.Position == originalCutleryPosition,
                "Nearby map cutlery must remain spawned at its original cell.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Nearby map cutlery must remain clean and untouched.");
            IntegrationAssert.True(
                !cutlery.GetComp<CompSanitation>().IsDirty,
                "Nearby map cutlery must retain its clean sanitation flag.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal map ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!cutlery.Destroyed)
            {
                cutlery.Destroy(DestroyMode.Vanish);
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveBiotechIndependentChildCompletesOrdinaryDiningWorkflow()
    {
        var biotechActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "ludeon.rimworld.biotech", StringComparison.OrdinalIgnoreCase));
        if (!biotechActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var fixtureCells = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, fixtureCells.Count, "The loaded child fixture needs two clear map cells.");
        var fixtureCell = fixtureCells[0];
        var child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 8f,
            fixedChronologicalAge: 8f,
            developmentalStages: DevelopmentalStage.Child,
            forceNoGear: true));
        var toddler = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 2f,
            fixedChronologicalAge: 2f,
            developmentalStages: DevelopmentalStage.Baby,
            forceNoGear: true));
        var meal = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("MealLavish"));
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Gold);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Gold);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var preexistingFoods = map.listerThings.AllThings
            .Where(thing => thing.Spawned && thing.def.IsNutritionGivingIngestible)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            foreach (var existing in preexistingFoods)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            child.Name = new NameSingle("Loaded Independent Child Diner");
            toddler.Name = new NameSingle("Loaded Toddler Requiring Feeding");
            child.inventory.innerContainer.ClearAndDestroyContents();
            toddler.inventory.innerContainer.ClearAndDestroyContents();
            IntegrationAssert.Equal(
                DevelopmentalStage.Child,
                child.DevelopmentalStage,
                "Biotech must generate a real child rather than an adult with a child label.");
            IntegrationAssert.True(
                child.RaceProps.Humanlike && child.needs?.food is not null && child.jobs is not null,
                "The real child pawn must expose the ordinary self-feeding trackers.");
            IntegrationAssert.Equal(
                DevelopmentalStage.Baby,
                toddler.DevelopmentalStage,
                "Biotech must generate a real toddler-age baby rather than a self-feeding child.");
            var childFood = child.needs!.food!;
            var childJobs = child.jobs!;

            plate.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
            cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    95,
                    70f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The child's lavish meal must begin with its exact clean plate embedded.");

            GenSpawn.Spawn(child, fixtureCell, map);
            GenSpawn.Spawn(toddler, fixtureCells[1], map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(cutlery, fixtureCell, map);
            childFood.CurLevelPercentage = 0.15f;
            toddler.needs!.food!.CurLevelPercentage = 0.15f;
            var mealId = meal.ThingID;
            var plateId = plate.ThingID;
            var cutleryId = cutlery.ThingID;

            var childFoodGiver = child.thinker.TryGetMainTreeThinkNode<JobGiver_GetFood>();
            IntegrationAssert.NotNull(
                childFoodGiver,
                "A real Biotech child must inherit the vanilla humanlike self-feeding job giver.");
            var childFoodResult = child.thinker.MainThinkNodeRoot.TryIssueJobPackage(child, default);
            IntegrationAssert.True(
                childFoodResult.IsValid && childFoodResult.Job.def == JobDefOf.Ingest &&
                childFoodResult.SourceNode is JobGiver_GetFood &&
                ReferenceEquals(childFoodResult.Job.GetTarget(TargetIndex.A).Thing, meal),
                "The child's full vanilla think tree must choose the exact plated fixture meal through JobGiver_GetFood.");
            IntegrationAssert.True(
                toddler.thinker.TryGetMainTreeThinkNode<JobGiver_GetFood>() is null,
                "A toddler-age baby must keep Biotech's assisted-feeding think tree without self-feeding jobs.");
            var toddlerThinkResult = toddler.thinker.MainThinkNodeRoot.TryIssueJobPackage(toddler, default);
            IntegrationAssert.True(
                !toddlerThinkResult.IsValid || toddlerThinkResult.Job.def != JobDefOf.Ingest,
                "Biotech's toddler think tree must not issue an ordinary self-feeding ingest job.");

            var ingestJob = childFoodResult.Job;
            childJobs.StartJob(
                ingestJob,
                JobCondition.InterruptForced,
                childFoodResult.SourceNode,
                thinkTree: child.thinker.MainThinkTree);
            IntegrationAssert.Equal(
                JobDefOf.Ingest,
                child.CurJobDef,
                "Vanilla must accept an ordinary ingest job for the independent child.");
            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.CutleryFor(ingestJob), cutlery),
                "Starting the real ingest job must reserve the exact clean fixture cutlery.");
            DiningSessionRegistry.Pickup(child);
            IntegrationAssert.True(
                ReferenceEquals(cutlery.holdingOwner, child.inventory.innerContainer),
                "The child dining session must acquire the exact clean cutlery before eating.");

            meal.Ingested(child, 0.9f);

            IntegrationAssert.True(meal.Destroyed, "The actual RimWorld ingestion boundary must consume the meal.");
            IntegrationAssert.Equal(mealId, meal.ThingID, "The native job must consume the exact fixture meal.");
            IntegrationAssert.True(
                plate.Spawned && plate.ThingID == plateId && ReferenceEquals(plate.Map, map),
                "The exact embedded plate must return to the map after the child eats.");
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.ThingID == cutleryId && ReferenceEquals(cutlery.Map, map),
                "The exact acquired cutlery must return to the map after the child eats.");
            IntegrationAssert.True(
                plate.GetComp<CompSanitation>().IsDirty && cutlery.GetComp<CompSanitation>().IsDirty,
                "The child's returned plate and cutlery must both become dirty through actual dining.");

            var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
            var culinaryThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_CulinaryQuality");
            var temperatureThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_MealTemperature");
            IntegrationAssert.Equal(
                0,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought)?.CurStageIndex ?? -1,
                "The child must receive the same proper-place-setting memory as an adult.");
            IntegrationAssert.Equal(
                6,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(culinaryThought)?.CurStageIndex ?? -1,
                "The child must receive the serving's legendary culinary-quality memory.");
            IntegrationAssert.Equal(
                0,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(temperatureThought)?.CurStageIndex ?? -1,
                "The child must receive the steaming-hot meal memory.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            foreach (var existing in preexistingFoods)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var thing in new Thing[] { meal, plate, cutlery, child, toddler })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CompletedMapDiningWithoutCutleryCreatesOneDirtEvent()
    {
        var map = Find.CurrentMap;
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var diningCell = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).All(thing => thing.def != ThingDefOf.Filth_Dirt))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => FilthMaker.CanMakeFilth(cell, map, ThingDefOf.Filth_Dirt));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingDirt = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .ToDictionary(filth => filth, filth => filth.thickness);

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    50,
                    35f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The missing-cutlery fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(pawn, diningCell, map);
            GenSpawn.Spawn(meal, diningCell, map);
            pawn.drafter.Drafted = true;
            pawn.needs.food.CurLevel = 0.01f;
            var dirtBefore = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            pawn.jobs.StartJob(ingestJob, JobCondition.InterruptForced);

            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                pawn.jobs.JobTrackerTick();
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real colonist JobDriver_Ingest must complete within the bounded fixture ticks.");
            var dirtAfter = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            IntegrationAssert.Equal(
                dirtBefore + 1,
                dirtAfter,
                "Completed eligible map dining without cutlery must add exactly one dirt thickness.");
            IntegrationAssert.True(
                pawn.Position.GetThingList(map).Any(thing => thing.def == ThingDefOf.Filth_Dirt),
                "The native dirt event must occur at the diner's actual final eating location.");

            var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
            var memory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought);
            IntegrationAssert.NotNull(memory, "The diner must receive the combined dining thought.");
            IntegrationAssert.Equal(
                1,
                memory!.CurStageIndex,
                "A plated meal without cutlery must select the missing-cutlery thought stage.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var filth in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                         .Cast<Filth>()
                         .ToList())
            {
                if (!preexistingDirt.TryGetValue(filth, out var originalThickness))
                {
                    filth.Destroy(DestroyMode.Vanish);
                    continue;
                }

                while (!filth.Destroyed && filth.thickness > originalThickness)
                {
                    filth.ThinFilth();
                }
            }

            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void NativePatientFeedingUsesCutleryAndAssignsConsequencesToThePatient()
    {
        var map = Find.CurrentMap;
        var bedCell = map.AllCells
            .Where(cell => GenAdj.OccupiedRect(cell, Rot4.North, ThingDefOf.Bed.size)
                .Cells.All(occupied => occupied.x >= 0 && occupied.z >= 0 &&
                                       occupied.x < map.Size.x && occupied.z < map.Size.z &&
                                       occupied.Standable(map) &&
                                       occupied.GetEdifice(map) is null) &&
                           FilthMaker.CanMakeFilth(
                               BedUtility.GetSleepingSlotPos(0, cell, Rot4.North, ThingDefOf.Bed.size),
                               map,
                               ThingDefOf.Filth_Dirt))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, bedCell, map, Rot4.North);
        bed.Medical = true;

        var patient = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var feeder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var patientStart = bed.GetSleepingSlotPos(0);
        var feederStart = patientStart;
        GenSpawn.Spawn(patient, patientStart, map);
        GenSpawn.Spawn(feeder, feederStart, map);

        var injuryPart = patient.health.hediffSet.GetNotMissingParts()
            .First(part => part.def == BodyPartDefOf.Torso);
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, patient, injuryPart);
        injury.Severity = 0.1f;
        patient.health.AddHediff(injury);
        IntegrationAssert.True(
            HealthAIUtility.ShouldSeekMedicalRest(patient),
            "The conscious assisted-feeding fixture must genuinely require medical rest.");
        var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        layDown.restUntilHealed = true;
        patient.jobs.StartJob(layDown, JobCondition.InterruptForced);
        for (var tick = 0; tick < 2000 && !patient.InBed(); tick++)
        {
            patient.jobs.JobTrackerTick();
        }

        IntegrationAssert.True(patient.InBed(), "The native patient must actually occupy the medical bed.");
        IntegrationAssert.True(patient.Awake(), "The first assisted-feeding pass must use a conscious patient.");

        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalAutoMicrowaveBelow = settings.AutoMicrowaveBelow;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingDirt = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .ToDictionary(filth => filth, filth => filth.thickness);
        var createdThings = new System.Collections.Generic.List<Thing> { bed, patient, feeder };
        var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            settings.MealTemperatureEnabled = true;
            settings.AutoMicrowaveBelow = 10f;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            var cutlery = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
                ThingDefOf.Steel);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var interruptedCutleryCell = map.AllCells
                .Where(cell => cell.Standable(map) &&
                               cell.GetEdifice(map) is null &&
                               feeder.CanReach(cell, PathEndMode.Touch, Danger.Some) &&
                               cell.DistanceToSquared(feeder.Position) >= 9)
                .OrderBy(cell => cell.DistanceToSquared(feeder.Position))
                .First();
            GenSpawn.Spawn(cutlery, interruptedCutleryCell, map);
            createdThings.Add(cutlery);

            var interruptedMeal = CreatePatientMeal(out var interruptedPlate);
            createdThings.Add(interruptedMeal);
            createdThings.Add(interruptedPlate);
            GenSpawn.Spawn(interruptedMeal, patient.Position, map);
            var interruptedJob = JobMaker.MakeJob(JobDefOf.FeedPatient, interruptedMeal, patient);
            interruptedJob.count = 1;
            interruptedJob.SetTarget(TargetIndex.C, feeder);
            feeder.jobs.StartJob(interruptedJob, JobCondition.InterruptForced);
            var interruptedSession = DiningSessionRegistry.Current(patient);
            IntegrationAssert.True(
                ReferenceEquals(interruptedSession?.Cutlery, cutlery),
                "The interruption fixture must select the exact reachable cutlery.");
            IntegrationAssert.True(
                map.reservationManager.ReservedBy(cutlery, feeder, interruptedJob),
                "The interruption fixture must reserve the selected cutlery for the native FeedPatient job.");
            feeder.jobs.JobTrackerTick();

            IntegrationAssert.True(
                ReferenceEquals(interruptedJob.GetTarget(TargetIndex.C).Thing, feeder),
                "Starting tableware pickup must not borrow FeedPatient's native food-holder target C.");
            IntegrationAssert.True(
                ReferenceEquals(feeder.CurJob, interruptedJob) &&
                feeder.pather.Moving &&
                ReferenceEquals(feeder.pather.Destination.Thing, cutlery),
                "The interruption fixture must have an active native path to the selected cutlery.");
            IntegrationAssert.True(
                cutlery.Spawned && !feeder.inventory.innerContainer.Contains(cutlery),
                "The interruption fixture must stop while the feeder is pathing to reserved cutlery.");
            feeder.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.Position == interruptedCutleryCell,
                "Interrupted patient feeding must leave unused cutlery at its map position.");
            IntegrationAssert.True(
                !cutlery.GetComp<CompSanitation>().IsDirty,
                "Interrupted patient feeding must leave unused cutlery clean.");
            IntegrationAssert.True(
                !map.reservationManager.IsReserved(cutlery),
                "Interrupted patient feeding must release the cutlery reservation.");
            interruptedMeal.Destroy(DestroyMode.Vanish);
            cutlery.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(cutlery, patient.Position, map);

            var microwaveCell = map.AllCells
                .Select(cell => new
                {
                    Cell = cell,
                    Interaction = new IntVec3(cell.x, cell.y, cell.z - 1)
                })
                .Where(candidate => candidate.Cell.Standable(map) &&
                                    candidate.Cell.GetEdifice(map) is null &&
                                    candidate.Interaction.x >= 0 &&
                                    candidate.Interaction.z >= 0 &&
                                    candidate.Interaction.x < map.Size.x &&
                                    candidate.Interaction.z < map.Size.z &&
                                    candidate.Interaction.Standable(map) &&
                                    candidate.Interaction.GetEdifice(map) is null &&
                                    feeder.CanReach(
                                        candidate.Interaction,
                                        PathEndMode.OnCell,
                                        Danger.Some))
                .OrderBy(candidate => candidate.Cell.DistanceToSquared(patient.Position))
                .First()
                .Cell;
            var microwave = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave"));
            var microwaveSupport = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("Table1x2c"),
                ThingDefOf.Steel);
            GenSpawn.Spawn(microwaveSupport, microwaveCell, map, Rot4.North);
            GenSpawn.Spawn(microwave, microwaveCell, map, Rot4.North);
            microwave.TryGetComp<CompPowerTrader>().PowerOn = true;
            createdThings.Add(microwaveSupport);
            createdThings.Add(microwave);

            var microwaveMeal = CreatePatientMeal(out var microwavePlate);
            microwaveMeal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    50,
                    -5f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            createdThings.Add(microwaveMeal);
            createdThings.Add(microwavePlate);
            GenSpawn.Spawn(microwaveMeal, patient.Position, map);
            settings.WareRequirementMode = WareRequirementMode.Off;
            var microwaveJob = JobMaker.MakeJob(JobDefOf.FeedPatient, microwaveMeal, patient);
            microwaveJob.count = 1;
            microwaveJob.SetTarget(TargetIndex.C, feeder);
            feeder.jobs.StartJob(microwaveJob, JobCondition.InterruptForced);
            var microwaveSession = DiningSessionRegistry.Current(patient);
            var microwaveComp = microwave.TryGetComp<CompMicrowave>();
            IntegrationAssert.True(
                ReferenceEquals(microwaveSession?.Microwave, microwave),
                "The cold assisted meal must select the real powered microwave. " +
                $"Session={microwaveSession is not null}; operational={microwaveComp?.Operational}; " +
                $"support={MicrowaveSupportRuntime.FindAt(microwave.Position, map, microwave)?.Label}; " +
                $"reachable={feeder.CanReach(microwave, PathEndMode.InteractionCell, Danger.Some)}; " +
                $"reserved={map.reservationManager.IsReserved(microwave)}; " +
                $"temperatureEnabled={settings.MealTemperatureEnabled}; " +
                $"autoMicrowaveBelow={settings.AutoMicrowaveBelow}; " +
                $"ownership={TemperatureOwnership.ImmersiveChefsFeaturesActive}; " +
                $"serving={microwaveMeal.GetComp<CompCulinaryState>().PeekCurrentServing()?.TemperatureCelsius}.");

            var microwaveDriver = feeder.jobs.curDriver;
            var microwaveToils = microwaveDriver is null
                ? null
                : Traverse.Create(microwaveDriver)
                    .Field("toils")
                    .GetValue<System.Collections.Generic.List<Toil>>();
            var heatingToils = microwaveToils?
                .Where(toil => toil.defaultCompleteMode == ToilCompleteMode.Delay &&
                               toil.defaultDuration == microwave.TryGetComp<CompMicrowave>().HeatingTicks)
                .ToList() ?? new System.Collections.Generic.List<Toil>();
            IntegrationAssert.Equal(
                1,
                heatingToils.Count,
                "The real patched FeedPatient driver must contain exactly one captured-microwave heating toil.");
            var heatingToil = heatingToils[0];
            IntegrationAssert.True(
                ReferenceEquals(microwaveJob.GetTarget(TargetIndex.C).Thing, feeder),
                "Microwave routing must not borrow FeedPatient's native target C.");
            IntegrationAssert.True(
                heatingToil.handlingFacing && heatingToil.tickAction is not null,
                "Microwave heating must visibly keep the feeder facing the captured appliance.");
            IntegrationAssert.True(
                heatingToil.finishActions?.Count > 0,
                "Microwave heating must retain a visible progress effect with cleanup.");

            microwaveDriver!.JumpToToil(heatingToil);
            IntegrationAssert.Equal(
                microwaveToils!.IndexOf(heatingToil),
                microwaveDriver.CurToilIndex,
                "The real FeedPatient driver must enter the captured-microwave heating toil.");
            microwaveDriver.DriverTick();

            var closure = heatingToil.tickAction!.Target;
            var effecterField = closure?.GetType()
                .GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic)
                .SingleOrDefault(field => typeof(Effecter).IsAssignableFrom(field.FieldType));
            var progressEffecter = effecterField?.GetValue(closure) as Effecter;
            var progressBar = progressEffecter?.children.OfType<SubEffecter_ProgressBar>().SingleOrDefault();
            IntegrationAssert.True(
                progressBar?.mote is { Spawned: true },
                "Active microwave heating must spawn a visible progress mote on the captured appliance.");

            var facingCell = feeder.Rotation.FacingCell;
            var microwaveDeltaX = microwave.Position.x - feeder.Position.x;
            var microwaveDeltaZ = microwave.Position.z - feeder.Position.z;
            IntegrationAssert.True(
                facingCell.x * microwaveDeltaX + facingCell.z * microwaveDeltaZ > 0,
                "Active microwave heating must face the feeder toward the captured appliance.");

            feeder.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            IntegrationAssert.True(
                progressBar!.mote.DestroyedOrNull(),
                "Interrupting active microwave heating must clean up its progress mote.");
            settings.WareRequirementMode = WareRequirementMode.Prefer;

            var servedMeal = CreatePatientMeal(out var servedPlate);
            createdThings.Add(servedMeal);
            createdThings.Add(servedPlate);
            GenSpawn.Spawn(servedMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeServed = DirtThickness(map);
            var carriedCutlery = RunNativeFeed(feeder, patient, servedMeal, cutlery);

            IntegrationAssert.True(
                carriedCutlery,
                "The native feeder must carry the exact reserved cutlery in their inventory before feeding.");
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.Position.DistanceToSquared(patient.Position) <= 4,
                "Completed assisted feeding must drop the exact cutlery beside the patient, not the feeder.");
            IntegrationAssert.True(
                cutlery.GetComp<CompSanitation>().IsDirty,
                "The cutlery used by the native feeder must become dirty after the patient eats.");
            IntegrationAssert.Equal(
                dirtBeforeServed,
                DirtThickness(map),
                "Feeding with cutlery must not create the missing-cutlery dirt event.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The feeder must never receive the patient's dining memory.");

            cutlery.Destroy(DestroyMode.Vanish);
            servedPlate.Destroy(DestroyMode.Vanish);
            patient.needs.mood.thoughts.memories.RemoveMemoriesOfDef(diningThought);

            var consciousMeal = CreatePatientMeal(out var consciousPlate);
            createdThings.Add(consciousMeal);
            createdThings.Add(consciousPlate);
            GenSpawn.Spawn(consciousMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeConscious = DirtThickness(map);
            RunNativeFeed(feeder, patient, consciousMeal, expectedCutlery: null);

            IntegrationAssert.Equal(
                dirtBeforeConscious + 1,
                DirtThickness(map),
                "A completed conscious feed without cutlery must create one vanilla dirt thickness.");
            var consciousMemory = patient.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought);
            IntegrationAssert.NotNull(consciousMemory, "The conscious patient must own the dining memory.");
            IntegrationAssert.Equal(
                1,
                consciousMemory!.CurStageIndex,
                "A conscious plated patient fed without cutlery must receive the missing-cutlery stage.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The conscious patient's feeder must not receive a missing-cutlery memory.");

            consciousPlate.Destroy(DestroyMode.Vanish);
            patient.needs.mood.thoughts.memories.RemoveMemoriesOfDef(diningThought);
            var anesthetic = patient.health.AddHediff(HediffDefOf.Anesthetic);
            IntegrationAssert.True(!patient.Awake(), "The final assisted-feeding pass must use an unconscious patient.");
            IntegrationAssert.True(
                !patient.health.capacities.CanBeAwake,
                "The unconscious fixture must be medically incapable of consciousness, not merely asleep.");
            IntegrationAssert.True(patient.InBed(), "The unconscious patient must remain in the native medical bed.");

            var unconsciousMeal = CreatePatientMeal(out var unconsciousPlate);
            createdThings.Add(unconsciousMeal);
            createdThings.Add(unconsciousPlate);
            GenSpawn.Spawn(unconsciousMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeUnconscious = DirtThickness(map);
            RunNativeFeed(feeder, patient, unconsciousMeal, expectedCutlery: null);

            IntegrationAssert.Equal(
                dirtBeforeUnconscious + 1,
                DirtThickness(map),
                "An unconscious patient fed without cutlery must still create the physical dirt event.");
            IntegrationAssert.True(
                patient.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "An unconscious patient must not receive the missing-cutlery dining memory.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The unconscious patient's feeder must not receive the dining memory either.");
            patient.health.RemoveHediff(anesthetic);
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.AutoMicrowaveBelow = originalAutoMicrowaveBelow;
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var filth in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                         .Cast<Filth>()
                         .ToList())
            {
                if (!preexistingDirt.TryGetValue(filth, out var originalThickness))
                {
                    filth.Destroy(DestroyMode.Vanish);
                    continue;
                }

                while (!filth.Destroyed && filth.thickness > originalThickness)
                {
                    filth.ThinFilth();
                }
            }

            foreach (var thing in createdThings.AsEnumerable().Reverse())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    private static ThingWithComps CreatePatientMeal(out ThingWithComps plate)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
        meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                35f,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        IntegrationAssert.True(
            meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
            "Every patient-feeding fixture meal must start with its exact clean embedded plate.");
        return meal;
    }

    private static bool RunNativeFeed(
        Pawn feeder,
        Pawn patient,
        Thing meal,
        Thing? expectedCutlery)
    {
        var job = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
        job.count = 1;
        var carriedCutlery = false;
        feeder.jobs.StartJob(job, JobCondition.InterruptForced);
        var session = DiningSessionRegistry.Current(patient);
        IntegrationAssert.NotNull(
            session,
            "Starting the real FeedPatient job must attach a dining session to the patient.");
        IntegrationAssert.True(
            session!.IsAssisted && ReferenceEquals(session.CarrierPawn, feeder),
            "The patient must own the dining outcome while the feeder owns tableware transport.");
        IntegrationAssert.True(
            ReferenceEquals(expectedCutlery, session.Cutlery),
            expectedCutlery is null
                ? "A no-cutlery patient feed must not retain tableware from an earlier feed."
                : "The assisted dining session must reserve the exact expected cutlery.");
        for (var tick = 0; tick < 6000 && !meal.Destroyed; tick++)
        {
            feeder.jobs.JobTrackerTick();
            carriedCutlery |= expectedCutlery is not null &&
                               feeder.inventory.innerContainer.Contains(expectedCutlery);
        }

        IntegrationAssert.True(
            meal.Destroyed,
            "The real JobDriver_FoodFeedPatient must complete within the bounded fixture ticks.");
        return carriedCutlery;
    }

    private static int DirtThickness(Map map) =>
        map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .Sum(filth => filth.thickness);

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void FeederCleanupDuringAssistedIngestionPreservesThePatientLifecycle()
    {
        var map = Find.CurrentMap;
        var fixtureCell = map.AllCells
            .Where(cell => cell.Standable(map) && cell.GetEdifice(map) is null)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var feeder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var patient = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var meal = CreatePatientMeal(out var plate);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            GenSpawn.Spawn(feeder, fixtureCell, map);
            GenSpawn.Spawn(patient, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(cutlery, fixtureCell, map);
            var job = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttachAssisted(feeder, patient, job, meal),
                "The regression fixture must attach the assisted dining session.");
            var session = DiningSessionRegistry.Current(patient);
            IntegrationAssert.NotNull(session, "The patient must own the attached dining session.");
            IntegrationAssert.True(
                ReferenceEquals(session!.Cutlery, cutlery),
                "The regression fixture must reserve its exact cutlery.");
            session.PickupCutlery();
            DiningSessionRegistry.BeginIngestion(patient);

            DiningSessionRegistry.Cleanup(feeder, job);

            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.Current(patient), session),
                "Feeder cleanup during Thing.Ingested must not cancel the patient's active lifecycle.");
            IntegrationAssert.True(
                feeder.inventory.innerContainer.Contains(cutlery),
                "Nested feeder cleanup must not prematurely return the patient's in-use cutlery.");
            DiningSessionRegistry.Complete(patient);
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.GetComp<CompSanitation>().IsDirty,
                "Patient completion after nested cleanup must still dirty and return the exact cutlery.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            feeder.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var thing in new Thing[] { meal, plate, cutlery, patient, feeder })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void SanitationStorageFiltersAreFinalizedAgainstKitchenware()
    {
        var cleanFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowCleanKitchenware");
        var dirtyFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowDirtyKitchenware");
        IntegrationAssert.NotNull(cleanFilter, "The clean kitchenware storage filter must finalize.");
        IntegrationAssert.NotNull(dirtyFilter, "The dirty kitchenware storage filter must finalize.");

        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        IntegrationAssert.NotNull(cleanFilter!.Worker, "The clean filter worker must instantiate.");
        IntegrationAssert.NotNull(dirtyFilter!.Worker, "The dirty filter worker must instantiate.");
        IntegrationAssert.True(
            cleanFilter.allowedByDefault && dirtyFilter.allowedByDefault,
            "Both sanitation filters must preserve vanilla storage behavior until a player disables one.");
        IntegrationAssert.True(
            cleanFilter.Worker.CanEverMatch(plateDef),
            "The finalized clean filter must recognize the plate Def.");
        IntegrationAssert.True(
            dirtyFilter.Worker.CanEverMatch(plateDef),
            "The finalized dirty filter must recognize the plate Def.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void UnpoweredDishwasherRejectsDirtyWareAtAdmission()
    {
        var map = Find.CurrentMap;
        var createdThings = new System.Collections.Generic.List<Thing>();
        var dishwasher = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var battery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Battery"));
        var previousPreference = ImmersiveChefsMod.Settings.PreferDishwashers;

        try
        {
            var sanitation = plate.GetComp<CompSanitation>();
            var power = dishwasher.GetComp<CompPowerTrader>();
            var dishwasherComp = dishwasher.GetComp<CompDishwasher>();
            var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
            var processorControlled = processorType is not null &&
                                      dishwasher.AllComps.Any(comp => processorType.IsInstanceOfType(comp));
            var processorWorkGiverType = processorControlled
                ? AccessTools.TypeByName("ProcessorFramework.WorkGiver_FillProcessor")
                : null;
            var processorWorkGiver = processorWorkGiverType is null
                ? null
                : Activator.CreateInstance(processorWorkGiverType);
            IntegrationAssert.NotNull(sanitation, "The finalized plate must expose sanitation state.");
            IntegrationAssert.NotNull(power, "The finalized dishwasher must expose its required power comp.");
            IntegrationAssert.NotNull(dishwasherComp, "The finalized dishwasher must expose its local cycle comp.");
            sanitation.MarkDirty();

            var fixtureCenter = map.AllCells
                .Where(cell => IsEmptyFixtureArea(map, cell, 4))
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
            for (var x = fixtureCenter.x - 3; x <= fixtureCenter.x + 3; x++)
            {
                var conduit = ThingMaker.MakeThing(conduitDef);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, new IntVec3(x, 0, fixtureCenter.z + 2), map);
                createdThings.Add(conduit);
            }

            dishwasher.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(
                dishwasher,
                new IntVec3(fixtureCenter.x - 2, 0, fixtureCenter.z + 2),
                map,
                Rot4.North);
            createdThings.Add(dishwasher);
            battery.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(
                battery,
                new IntVec3(fixtureCenter.x + 2, 0, fixtureCenter.z + 2),
                map,
                Rot4.North);
            createdThings.Add(battery);
            GenSpawn.Spawn(plate, new IntVec3(fixtureCenter.x, 0, fixtureCenter.z - 2), map);
            createdThings.Add(plate);
            plate.SetForbidden(true, warnOnFail: false);
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            pawn.Name = new NameSingle("Dishwasher Test Worker");
            pawn.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(pawn, fixtureCenter, map);
            pawn.drafter.Drafted = true;
            createdThings.Add(pawn);
            var batteryComp = battery.GetComp<CompPowerBattery>();
            var flick = dishwasher.GetComp<CompFlickable>();
            IntegrationAssert.NotNull(batteryComp, "The real power fixture must expose battery storage.");
            IntegrationAssert.NotNull(flick, "The finalized dishwasher must expose its native power switch.");
            batteryComp.SetStoredEnergyPct(1f);
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
            IntegrationAssert.NotNull(
                power.PowerNet,
                "The spawned dishwasher must attach to a real RimWorld power net.");
            IntegrationAssert.True(
                ReferenceEquals(power.PowerNet, batteryComp.PowerNet),
                "The spawned dishwasher and charged battery must share the same RimWorld power net.");
            for (var tick = 0; tick <= 200 && !power.PowerOn; tick++)
            {
                Find.TickManager.DoSingleTick();
            }
            IntegrationAssert.True(
                power.PowerOn,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "The charged connected battery must power the dishwasher through RimWorld's real power net " +
                    "(switch={0}, stored={1}, powerComps={2}, batteries={3}, activeSource={4}, output={5}).",
                    flick.SwitchIsOn,
                    batteryComp.StoredEnergy,
                    power.PowerNet.powerComps.Count,
                    power.PowerNet.batteryComps.Count,
                    power.PowerNet.HasActivePowerSource,
                    power.PowerOutput));

            flick.DoFlick();
            IntegrationAssert.True(
                !flick.SwitchIsOn && !power.PowerOn,
                "Using the native switch must leave the connected dishwasher genuinely unpowered.");
            IntegrationAssert.True(
                !dishwasherComp.CanAccept(plate),
                "An unpowered dishwasher must be ineligible for admission and Doing dishes selection.");

            ImmersiveChefsMod.Settings.PreferDishwashers = true;
            plate.SetForbidden(false, warnOnFail: false);
            var foundUnpowered = WorkGiver_DoDishes.TryFindDestination(pawn, plate, out var unpowered);
            IntegrationAssert.True(
                !foundUnpowered || !ReferenceEquals(unpowered.Target.Thing, dishwasher),
                "Doing dishes must exclude the unpowered dishwasher from destination selection.");
            if (processorControlled)
            {
                IntegrationAssert.NotNull(
                    processorWorkGiver,
                    "The active Processor Framework path must expose its fill work giver.");
                var processor = dishwasher.AllComps.Single(comp => processorType!.IsInstanceOfType(comp));
                var findIngredient = AccessTools.Method(processorWorkGiverType, "FindIngredient");
                var unpoweredIngredient = (Thing?)findIngredient!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, processor });
                IntegrationAssert.Null(
                    unpoweredIngredient,
                    "Processor Framework must not select dirty ware for an unpowered dishwasher.");
                var processorHasUnpoweredJob = (bool)AccessTools.Method(
                    processorWorkGiverType,
                    "HasJobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.False(
                    processorHasUnpoweredJob,
                    "Processor Framework must not admit dirty ware while the dishwasher is unpowered.");
            }

            plate.SetForbidden(true, warnOnFail: false);
            flick.DoFlick();
            for (var tick = 0; tick <= 200 && !power.PowerOn; tick++)
            {
                Find.TickManager.DoSingleTick();
            }
            IntegrationAssert.True(
                flick.SwitchIsOn && power.PowerOn,
                "Using the native switch must restore power from the unchanged connected battery.");
            plate.SetForbidden(false, warnOnFail: false);
            if (processorControlled)
            {
                IntegrationAssert.False(
                    dishwasherComp.CanAccept(plate),
                    "The local admission path must remain disabled while Processor Framework owns the appliance.");
                var processor = dishwasher.AllComps.Single(comp => processorType!.IsInstanceOfType(comp));
                var poweredIngredient = (Thing?)AccessTools.Method(
                    processorWorkGiverType,
                    "FindIngredient")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, processor });
                var enabled = (System.Collections.IDictionary)AccessTools.Field(
                    processorType,
                    "enabledProcesses")!.GetValue(processor);
                var processDiagnostics = string.Join(
                    ",",
                    enabled.Keys.Cast<object>().Select(process =>
                    {
                        var allows = (AccessTools.Field(process.GetType(), "ingredientFilter")!
                            .GetValue(process) as ThingFilter)?.Allows(plate.def) == true;
                        var space = AccessTools.Method(processorType, "SpaceLeftFor")!.Invoke(
                            processor,
                            new object[] { process, 1f });
                        return $"{((Def)process).defName}:allows={allows}:space={space}";
                    }));
                IntegrationAssert.True(
                    ReferenceEquals(poweredIngredient, plate),
                    $"The powered Processor dishwasher must select the exact dirty plate " +
                    $"(dirty={sanitation.IsDirty}, forbidden={plate.IsForbidden(pawn)}, " +
                    $"reachable={pawn.CanReach(plate, PathEndMode.Touch, Danger.Some)}, " +
                    $"reservable={pawn.CanReserve(plate)}, processes={processDiagnostics}).");
                var processorHasPoweredJob = (bool)AccessTools.Method(
                    processorWorkGiverType,
                    "HasJobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.True(
                    processorHasPoweredJob,
                    "The same powered dishwasher must accept dirty ware through Processor Framework.");
                var processorJob = (Job?)AccessTools.Method(
                    processorWorkGiverType,
                    "JobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.True(
                    processorJob is not null &&
                    ReferenceEquals(processorJob.GetTarget(TargetIndex.A).Thing, dishwasher) &&
                    ReferenceEquals(processorJob.GetTarget(TargetIndex.B).Thing, plate),
                    "The Processor job must persist the exact powered dishwasher and dirty ware targets.");
                var staleDriver = processorJob!.MakeDriver(pawn);
                sanitation.MarkClean(WashProvenance.Safe);
                IntegrationAssert.False(
                    staleDriver.TryMakePreToilReservations(errorOnFailed: true),
                    "A queued Processor fill job must fail before reserving or hauling ware that has since become clean.");
                IntegrationAssert.True(
                    plate.Spawned && !pawn.Map.reservationManager.IsReservedByAnyoneOf(plate, Faction.OfPlayer),
                    "Rejecting stale Processor work must leave the exact clean ware spawned and unreserved for storage.");
                sanitation.MarkDirty();
            }
            else
            {
                IntegrationAssert.True(
                    dishwasherComp.CanAccept(plate),
                    "The same powered dishwasher must accept the dirty plate when otherwise operational.");
                IntegrationAssert.True(
                    WorkGiver_DoDishes.TryFindDestination(pawn, plate, out var powered) &&
                    ReferenceEquals(powered.Target.Thing, dishwasher),
                    "Doing dishes must select the same reachable dishwasher once power is restored.");
                var job = new WorkGiver_DoDishes().JobOnThing(pawn, plate);
                IntegrationAssert.True(
                    job is not null && ReferenceEquals(job.GetTarget(TargetIndex.B).Thing, dishwasher),
                    "The native work giver job must persist the exact powered dishwasher destination.");
            }
        }
        finally
        {
            ImmersiveChefsMod.Settings.PreferDishwashers = previousPreference;
            for (var index = createdThings.Count - 1; index >= 0; index--)
            {
                if (!createdThings[index].Destroyed)
                {
                    createdThings[index].Destroy(DestroyMode.Vanish);
                }
            }
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
        }
    }

    private static bool IsEmptyFixtureArea(Map map, IntVec3 center, int radius)
    {
        for (var x = center.x - radius; x <= center.x + radius; x++)
        {
            for (var z = center.z - radius; z <= center.z + radius; z++)
            {
                var cell = new IntVec3(x, 0, z);
                if (x < 0 || z < 0 || x >= map.Size.x || z >= map.Size.z ||
                    !cell.Standable(map) ||
                    cell.GetThingList(map).Count != 0 ||
                    map.zoneManager.ZoneAt(cell) is not null)
                {
                    return false;
                }
            }
        }

        return true;
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
        ((ThingWithComps)plate).GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Embedding must retain the original Thing instance.");
        IntegrationAssert.Equal(
            WashProvenance.WildWater,
            embedded.Bindings.Single().WashProvenance,
            "The lightweight plate binding must retain sanitation provenance.");
        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "Releasing must return the exact original Thing instance.");
        IntegrationAssert.Equal(plateId, released!.ThingID, "The plate LoadID must remain unchanged.");

    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void TerminalMealDestructionUsesEffectivePlateFlammabilityAndConservesIdentity()
    {
        var map = Find.CurrentMap;
        var cells = map.AllCells
            .Where(cell =>
                cell.Standable(map) &&
                cell.GetThingList(map).Count == 0 &&
                map.zoneManager.ZoneAt(cell) is null)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(3)
            .ToList();
        IntegrationAssert.Equal(3, cells.Count, "The quickstart map must provide three terminal-meal cells.");

        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var allowedStuffs = GenStuff.AllowedStuffsFor(plateDef).ToList();
        var flammableStuff = allowedStuffs.FirstOrDefault(stuff =>
            plateDef.GetStatValueAbstract(StatDefOf.Flammability, stuff) > 0f);
        var nonflammableStuff = allowedStuffs.FirstOrDefault(stuff =>
            plateDef.GetStatValueAbstract(StatDefOf.Flammability, stuff) <= 0f);
        IntegrationAssert.NotNull(
            flammableStuff,
            "The finalized plate Def must allow a Stuff with positive effective Flammability.");
        IntegrationAssert.NotNull(
            nonflammableStuff,
            "The finalized plate Def must allow a Stuff with zero effective Flammability.");
        var created = new System.Collections.Generic.List<Thing>();
        var makeMeal = new Func<IntVec3, ThingDef, (ThingWithComps Meal, ThingWithComps Plate)>((cell, stuff) =>
        {
            var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, stuff);
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "A terminal fixture meal must accept its exact plate.");
            GenSpawn.Spawn(meal, cell, map);
            created.Add(meal);
            created.Add(plate);
            return (meal, plate);
        });

        try
        {
            var expired = makeMeal(cells[0], flammableStuff!);
            var flammable = makeMeal(cells[1], flammableStuff!);
            var nonflammable = makeMeal(cells[2], nonflammableStuff!);
            var expiredPlateId = expired.Plate.ThingID;
            var nonflammablePlateId = nonflammable.Plate.ThingID;

            expired.Meal.Destroy(DestroyMode.KillFinalize);
            IntegrationAssert.True(
                !expired.Plate.Destroyed && expired.Plate.Spawned && expired.Plate.Map == map,
                "Non-fire terminal destruction must return a flammable plate to the map.");
            IntegrationAssert.Equal(
                expiredPlateId,
                expired.Plate.ThingID,
                "Non-fire terminal destruction must conserve the exact plate identity.");
            IntegrationAssert.True(
                expired.Plate.GetComp<CompSanitation>().IsDirty,
                "A plate recovered from an expired meal must be dirty.");

            flammable.Meal.HitPoints = 1;
            flammable.Meal.TakeDamage(new DamageInfo(DamageDefOf.Flame, 100f));
            IntegrationAssert.True(
                flammable.Meal.Destroyed && flammable.Plate.Destroyed,
                "Fire must destroy a plate with positive effective Flammability.");

            nonflammable.Meal.HitPoints = 1;
            nonflammable.Meal.TakeDamage(new DamageInfo(DamageDefOf.Flame, 100f));
            IntegrationAssert.True(nonflammable.Meal.Destroyed, "Fire must terminally destroy the fixture meal.");
            IntegrationAssert.True(
                !nonflammable.Plate.Destroyed && nonflammable.Plate.Spawned && nonflammable.Plate.Map == map,
                "Fire must return an effectively nonflammable plate to the map.");
            IntegrationAssert.Equal(
                nonflammablePlateId,
                nonflammable.Plate.ThingID,
                "Fire recovery must conserve the exact nonflammable plate identity.");
            IntegrationAssert.True(
                nonflammable.Plate.GetComp<CompSanitation>().IsDirty,
                "A nonflammable plate recovered from fire must be dirty.");
        }
        finally
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                if (!created[index].Destroyed)
                {
                    created[index].Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void KitchenwareAlertOnlyReportsAbsentWareForRunnableOwnedKitchenBills()
    {
        var map = Find.CurrentMap;
        var cells = new System.Collections.Generic.List<IntVec3>();
        foreach (var cell in map.AllCells.OrderBy(cell => cell.DistanceToSquared(map.Center)))
        {
            if (!CellRect.CenteredOn(cell, 2).Cells.All(candidate =>
                    candidate.x >= 0 && candidate.z >= 0 &&
                    candidate.x < map.Size.x && candidate.z < map.Size.z &&
                    candidate.Standable(map) &&
                    candidate.GetThingList(map).Count == 0 &&
                    map.zoneManager.ZoneAt(candidate) is null) ||
                cells.Any(existing => existing.DistanceToSquared(cell) < 100f))
            {
                continue;
            }

            cells.Add(cell);
            if (cells.Count == 5)
            {
                break;
            }
        }

        IntegrationAssert.Equal(5, cells.Count, "The quickstart map must provide five isolated alert-fixture areas.");
        var previousMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var created = new System.Collections.Generic.List<Thing>();
        var draftedStates = new System.Collections.Generic.Dictionary<Pawn, bool>();

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
            var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
            var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
            IntegrationAssert.Equal(
                0,
                map.listerThings.AllThings.Count(thing =>
                    thing.def.GetModExtension<KitchenwareExtension>()?.product is
                        KitchenwareProduct.Cookware or KitchenwareProduct.Plate),
                "The isolated quickstart must start without cookware or plates for the absence proof.");

            var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
            Pawn? worker = null;
            for (var attempt = 0; attempt < 64 && worker is null; attempt++)
            {
                var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false));
                if (candidate.WorkTypeIsDisabled(cooking))
                {
                    candidate.Destroy(DestroyMode.Vanish);
                    continue;
                }

                worker = candidate;
            }

            IntegrationAssert.NotNull(worker, "The alert fixture must generate a Cooking-capable colonist.");
            worker!.workSettings.EnableAndInitialize();
            worker.workSettings.SetPriority(cooking, 1);
            GenSpawn.Spawn(worker, cells[0], map);
            created.Add(worker);

            var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var berries = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawBerries"));
            var campfireDef = DefDatabase<ThingDef>.GetNamed("Campfire");
            var campfire = ThingMaker.MakeThing(
                campfireDef,
                campfireDef.MadeFromStuff ? ThingDefOf.Steel : null);
            campfire.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(meal, cells[1], map);
            GenSpawn.Spawn(berries, cells[1] + IntVec3.East, map);
            GenSpawn.Spawn(campfire, cells[2], map, Rot4.North);
            campfire.TryGetComp<CompRefuelable>()?.Refuel(999f);
            var campfireBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1
            };
            ((IBillGiver)campfire).BillStack.AddBill(campfireBill);
            created.Add(meal);
            created.Add(berries);
            created.Add(campfire);

            var alert = new Alert_MissingKitchenware();
            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "Loose meals, raw berries, and a campfire bill must not advertise missing kitchenware.");
            IntegrationAssert.True(
                campfireDef.GetModExtension<KitchenwareAlertStationExtension>() is null,
                "The finalized Campfire Def must not opt into kitchenware alerts.");

            var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
            IntegrationAssert.True(
                stoveDef.GetModExtension<KitchenwareAlertStationExtension>()?.enabled == true,
                "The finalized fueled-stove Def must opt into kitchenware alerts.");
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, cells[3], map, Rot4.North);
            created.Add(stove);
            var stoveBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1
            };
            ((IBillGiver)stove).BillStack.AddBill(stoveBill);

            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "An unpowered or unfueled kitchen must not create a secondary kitchenware alert.");
            stove.TryGetComp<CompRefuelable>()?.Refuel(999f);
            IntegrationAssert.True(
                alert.GetReport().AnyCulpritValid,
                "A runnable strict bill on an owned fueled stove must report physically absent cookware and plates.");

            foreach (var cook in map.mapPawns.FreeColonistsSpawned.Where(pawn =>
                         pawn.workSettings?.WorkIsActive(cooking) == true &&
                         !pawn.Downed &&
                         !pawn.InMentalState))
            {
                draftedStates[cook] = cook.Drafted;
                cook.drafter.Drafted = true;
            }

            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "Drafting every eligible cook must suppress the alert during combat.");
            foreach (var pair in draftedStates)
            {
                pair.Key.drafter.Drafted = pair.Value;
            }
            draftedStates.Clear();
            IntegrationAssert.True(
                alert.GetReport().AnyCulpritValid,
                "Returning an eligible cook to ordinary work must restore the relevant absence alert.");

            var cookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
            cookware.GetComp<CompSanitation>().MarkDirty();
            GenSpawn.Spawn(cookware, cells[4], map);
            cookware.SetForbidden(true, warnOnFail: false);
            created.Add(cookware);
            var oneMissingExplanation = alert.GetExplanation().Resolve();
            IntegrationAssert.True(
                alert.GetReport().AnyCulpritValid &&
                oneMissingExplanation.IndexOf("plates", StringComparison.OrdinalIgnoreCase) >= 0,
                "Dirty forbidden cookware must count as existing while a physically absent plate remains alert-worthy.");

            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
            plate.GetComp<CompSanitation>().MarkDirty();
            IntegrationAssert.True(
                worker.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
                "The fixture must place the dirty plate in a real map-held pawn inventory.");
            created.Add(plate);
            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "Dirty forbidden cookware and a dirty inventory-held plate must suppress an inventory-level absence alert.");

            cookware.Destroy(DestroyMode.Vanish);
            plate.Destroy(DestroyMode.Vanish);
            stoveBill.suspended = true;
            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "A suspended bill must not request inventory alerts.");
            stoveBill.suspended = false;
            IntegrationAssert.True(
                alert.GetReport().AnyCulpritValid,
                "Resuming the runnable bill with no physical ware must restore the alert.");

            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            IntegrationAssert.True(
                !alert.GetReport().AnyCulpritValid,
                "Prefer mode must not raise an alert for ware that the recipe does not strictly require.");
        }
        finally
        {
            foreach (var pair in draftedStates)
            {
                if (!pair.Key.Destroyed)
                {
                    pair.Key.drafter.Drafted = pair.Value;
                }
            }

            ImmersiveChefsMod.Settings.WareRequirementMode = previousMode;
            foreach (var thing in created.AsEnumerable().Reverse())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void RealStorageFiltersAndStackingTrackSpawnedSanitationTransitions()
    {
        var map = Find.CurrentMap;
        var pawn = map.mapPawns.FreeColonistsSpawned.First();
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cleanSpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware");
        var dirtySpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware");
        var zoneCells = map.AllCells
            .Where(cell =>
                cell.Standable(map) &&
                map.zoneManager.ZoneAt(cell) is null &&
                cell.GetFirstItem(map) is null &&
                cell.GetEdifice(map) is null)
            .OrderBy(cell => cell.DistanceToSquared(pawn.Position))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, zoneCells.Count, "The quickstart map must provide two stockpile fixture cells.");

        var cleanZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        var dirtyZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(cleanZone);
        map.zoneManager.RegisterZone(dirtyZone);
        cleanZone.AddCell(zoneCells[0]);
        dirtyZone.AddCell(zoneCells[1]);
        ConfigureSanitationStockpile(cleanZone, plateDef, cleanSpecial, dirtySpecial, clean: true);
        ConfigureSanitationStockpile(dirtyZone, plateDef, cleanSpecial, dirtySpecial, clean: false);

        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var other = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        GenSpawn.Spawn(plate, zoneCells[0], map);

        try
        {
            var sanitation = plate.GetComp<CompSanitation>();
            var otherSanitation = other.GetComp<CompSanitation>();
            sanitation.MarkClean(WashProvenance.WildWater);
            otherSanitation.MarkClean(WashProvenance.Safe);

            IntegrationAssert.True(
                !sanitation.AllowStackWith(other),
                "Equal clean plates with different wash provenance must not stack and erase safety state.");

            var dirtyOnly = new ThingFilter();
            dirtyOnly.SetAllow(plateDef, allow: true);
            dirtyOnly.SetAllow(cleanSpecial, allow: false);
            dirtyOnly.SetAllow(dirtySpecial, allow: true);
            IntegrationAssert.True(!dirtyOnly.Allows(plate), "A clean plate must be excluded from dirty-only storage.");
            IntegrationAssert.True(
                !map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "A clean plate already in clean-only storage must not be queued for hauling.");

            sanitation.MarkDirty();
            IntegrationAssert.True(
                dirtyOnly.Allows(plate),
                "A spawned plate must enter dirty-only storage eligibility immediately after being dirtied.");
            IntegrationAssert.True(
                map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "Dirtifying a spawned plate must invalidate the haul cache for its clean-only stockpile.");
            var haulJob = HaulAIUtility.HaulToStorageJob(pawn, plate, forced: true);
            IntegrationAssert.NotNull(haulJob, "RimWorld must find the dirty-only stockpile after invalidation.");
            IntegrationAssert.Equal(
                zoneCells[1],
                haulJob!.GetTarget(Verse.AI.TargetIndex.B).Cell,
                "RimWorld's native hauling selector must route the dirty plate to dirty-only storage.");

            var cleanOnly = new ThingFilter();
            cleanOnly.SetAllow(plateDef, allow: true);
            cleanOnly.SetAllow(cleanSpecial, allow: true);
            cleanOnly.SetAllow(dirtySpecial, allow: false);
            IntegrationAssert.True(!cleanOnly.Allows(plate), "A dirty plate must be excluded from clean-only storage.");

            sanitation.MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                cleanOnly.Allows(plate),
                "A spawned plate must enter clean-only storage eligibility immediately after being washed.");
        }
        finally
        {
            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!other.Destroyed)
            {
                other.Destroy(DestroyMode.Vanish);
            }

            cleanZone.Delete();
            dirtyZone.Delete();
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void TypedWashSourcesWriteVanillaSerializableJobTargets()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var dirtyWare = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var fixture = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        try
        {
            var safe = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(fixture, WashProvenance.Safe));
            var wild = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(Find.CurrentMap.Center, WashProvenance.WildWater));

            IntegrationAssert.True(
                safe.GetTarget(Verse.AI.TargetIndex.C).HasThing,
                "A validated safe fixture must persist its provenance marker in vanilla Job target C.");
            IntegrationAssert.True(
                !wild.GetTarget(Verse.AI.TargetIndex.C).IsValid,
                "A wild-water destination must leave vanilla Job target C unset.");
        }
        finally
        {
            if (!dirtyWare.Destroyed)
            {
                dirtyWare.Destroy(DestroyMode.Vanish);
            }

            if (!fixture.Destroyed)
            {
                fixture.Destroy(DestroyMode.Vanish);
            }
        }
    }

    private static void ConfigureSanitationStockpile(
        Zone_Stockpile zone,
        ThingDef plateDef,
        SpecialThingFilterDef cleanSpecial,
        SpecialThingFilterDef dirtySpecial,
        bool clean)
    {
        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        zone.settings.filter.SetAllow(plateDef, allow: true);
        zone.settings.filter.SetAllow(cleanSpecial, allow: clean);
        zone.settings.filter.SetAllow(dirtySpecial, allow: !clean);
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
        var chew = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
        var feedReservations = AccessTools.Method(
            typeof(JobDriver_FoodFeedPatient),
            nameof(JobDriver_FoodFeedPatient.TryMakePreToilReservations));
        var feedToils = AccessTools.Method(typeof(JobDriver_FoodFeedPatient), "MakeNewToils");
        foreach (var method in new[] { cooking, ingest, ingestOutcome, chew, feedReservations, feedToils })
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
            var processorProperties = def.comps.Single(comp =>
                processorType!.IsAssignableFrom(comp.compClass));
            IntegrationAssert.True(
                (bool)AccessTools.Field(processorProperties.GetType(), "independentProcesses")!
                    .GetValue(processorProperties),
                $"{defName} must keep each later admission on its own persisted progress clock.");
            IntegrationAssert.True(
                (bool)AccessTools.Field(processorProperties.GetType(), "parallelProcesses")!
                    .GetValue(processorProperties),
                $"{defName} must keep shared free capacity open while earlier loads are washing.");
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => processorType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one Processor Framework component.");
            IntegrationAssert.Equal(
                DrawerType.MapMeshAndRealTime,
                def.drawerType,
                $"{defName} must render Processor Framework progress.");

            var processes = (AccessTools.Field(processorProperties.GetType(), "processes")?
                                 .GetValue(processorProperties) as System.Collections.IEnumerable)?
                .Cast<object>()
                .ToList();
            IntegrationAssert.NotNull(processes, $"{defName} must expose its Processor process list.");
            foreach (var ware in DefDatabase<ThingDef>.AllDefsListForReading.Where(candidate =>
                         candidate.GetModExtension<KitchenwareExtension>()?.product is
                             KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery))
            {
                var matches = processes!.Where(process =>
                        (AccessTools.Field(process.GetType(), "ingredientFilter")?.GetValue(process) as ThingFilter)?
                        .Allows(ware) == true)
                    .ToList();
                IntegrationAssert.Equal(
                    1,
                    matches.Count,
                    $"{defName} must expose exactly one process for {ware.defName}.");
                var actualFactor = Convert.ToSingle(
                    AccessTools.Field(matches[0].GetType(), "capacityFactor")!.GetValue(matches[0]));
                var expectedFactor = DishwasherCapacityPolicy.ProcessorCapacityFactor(
                    ware.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f);
                IntegrationAssert.True(
                    Math.Abs(actualFactor - expectedFactor) < 0.0001f,
                    $"{defName}/{ware.defName} must consume {expectedFactor} plate-equivalents, got {actualFactor}.");
            }

        }

        var initialize = AccessTools.Method(processorType, "Initialize");
        var addIngredient = AccessTools.Method(processorType, "AddIngredient");
        var spaceLeftFor = AccessTools.Method(processorType, "SpaceLeftFor");
        var takeOut = AccessTools.Method(processorType, "TakeOutProduct");
        var fillDriverType = AccessTools.TypeByName("ProcessorFramework.JobDriver_FillProcessor");
        var fillReservations = fillDriverType?.GetMethod(
            nameof(JobDriver.TryMakePreToilReservations),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(bool) },
            modifiers: null);
        var addIngredientPatches = Harmony.GetPatchInfo(addIngredient);
        var owners = Harmony.GetPatchInfo(takeOut)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, addIngredientPatches?.Prefixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor admission must have one Immersive Chefs utility/capacity prefix.");
        IntegrationAssert.Equal(1, addIngredientPatches?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor admission must have one Immersive Chefs per-load water-commit postfix.");
        IntegrationAssert.Equal(1, Harmony.GetPatchInfo(spaceLeftFor)?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor capacity must have one Immersive Chefs utility-affordability postfix.");
        IntegrationAssert.Equal(1, Harmony.GetPatchInfo(initialize)?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "New dishwashers must have one Immersive Chefs process-enablement postfix.");
        IntegrationAssert.Equal(1, owners, "Processor completion must have one Immersive Chefs identity bridge.");
        IntegrationAssert.Equal(1, Harmony.GetPatchInfo(fillReservations)?.Prefixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor fill jobs must have one Immersive Chefs stale-clean reservation guard.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveProcessorFrameworkEnablesEveryDishwasherWareProcess()
    {
        var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
        if (processorType is null)
        {
            return;
        }

        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var instance = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
            var processor = instance.AllComps.Single(comp => processorType.IsInstanceOfType(comp));
            var enabledProcesses = AccessTools.Field(processorType, "enabledProcesses")?
                .GetValue(processor) as System.Collections.IDictionary;
            var processProperties = processor.props;
            var processes = (AccessTools.Field(processProperties.GetType(), "processes")?
                                 .GetValue(processProperties) as System.Collections.IEnumerable)?
                .Cast<object>()
                .ToList();
            IntegrationAssert.NotNull(processes, $"A new {defName} must expose its Processor processes.");
            IntegrationAssert.NotNull(
                enabledProcesses,
                $"A new {defName} must expose its enabled Processor filters.");
            IntegrationAssert.Equal(
                processes!.Count,
                enabledProcesses!.Count,
                $"A new {defName} must enable every ware process independently of Processor Framework's global first-only default.");
        }
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
    public static void ActiveCommonSenseUsesItsExactPublicCleaningShape()
    {
        var commonSenseLoaded = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "avilmask.commonsense", StringComparison.OrdinalIgnoreCase));
        if (!commonSenseLoaded)
        {
            IntegrationAssert.True(
                ImmersiveChefsMod.Integrations?.IsActive(OptionalIntegration.CommonSense) == false,
                "An absent Common Sense package must remain inactive.");
            IntegrationAssert.True(
                !CommonSenseAdapter.Enabled,
                "The Common Sense adapter must not bind when its package is absent.");
            return;
        }

        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense),
            "The default Auto setting must enable an active Common Sense package.");
        IntegrationAssert.True(
            CommonSenseAdapter.Enabled,
            "The supported Common Sense matrix must bind the post-dining adapter.");

        var settingsType = AccessTools.TypeByName("CommonSense.Settings");
        var utilityType = AccessTools.TypeByName("CommonSense.Utility");
        IntegrationAssert.NotNull(settingsType, "Common Sense must expose public CommonSense.Settings.");
        IntegrationAssert.NotNull(utilityType, "Common Sense must expose public static CommonSense.Utility.");
        IntegrationAssert.Equal(
            "CommonSense",
            settingsType!.Assembly.GetName().Name,
            "The settings surface must come from the exact CommonSense assembly identity.");
        IntegrationAssert.Equal(
            settingsType.Assembly,
            utilityType!.Assembly,
            "The validated settings and cleaning utility must come from the same Common Sense assembly.");

        var ingestSetting = settingsType.GetField(
            "adv_cleaning_ingest",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        IntegrationAssert.NotNull(
            ingestSetting,
            "The supported Common Sense shape must retain its public static ingestion-cleaning setting.");
        IntegrationAssert.Equal(
            typeof(bool),
            ingestSetting!.FieldType,
            "Common Sense adv_cleaning_ingest must remain a Boolean setting.");

        var incapableMethod = utilityType.GetMethod(
            "IncapableOfCleaning",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Pawn) },
            modifiers: null);
        IntegrationAssert.NotNull(
            incapableMethod,
            "The supported Common Sense shape must retain public static bool IncapableOfCleaning(Pawn).");
        IntegrationAssert.Equal(
            typeof(bool),
            incapableMethod!.ReturnType,
            "Common Sense cleaning capability must remain a Boolean predicate.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveVanillaNutrientPasteExpandedUsesItsExactPipeBackedTap()
    {
        var activeIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToList();
        var vnpeActive = activeIds.Any(id =>
            string.Equals(id, "vanillaexpanded.vnutriente", StringComparison.OrdinalIgnoreCase));
        if (!vnpeActive)
        {
            return;
        }

        var phase = "active package and setting validation";
        try
        {
            IntegrationAssert.True(
                activeIds.Any(id => string.Equals(
                    id,
                    "oskarpotocki.vanillafactionsexpanded.core",
                    StringComparison.OrdinalIgnoreCase)),
                "The supported VNPE matrix must load Vanilla Expanded Framework first.");
            IntegrationAssert.True(
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.VanillaNutrientPasteExpanded),
                "The exact active VNPE+VEF matrix must enable its Auto integration setting.");
            IntegrationAssert.True(
                VanillaNutrientPasteExpandedAdapter.Enabled,
                "The finalized-Def bootstrap must validate and enable the VNPE adapter.");

            phase = "finalized tap Def and runtime type validation";
            var tapDef = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPasteTap");
            var tapType = AccessTools.TypeByName("VNPE.Building_NutrientPasteTap");
            var pipePropertiesType = AccessTools.TypeByName("PipeSystem.CompProperties_Resource");
            IntegrationAssert.NotNull(tapType, "Active VNPE must expose its exact nutrient-paste tap type.");
            IntegrationAssert.NotNull(pipePropertiesType, "Active VEF must expose the exact pipe resource component.");
            IntegrationAssert.Equal(
                "VNPE",
                tapType!.Assembly.GetName().Name,
                "The supported nutrient-paste tap type must come from VNPE.dll.");
            IntegrationAssert.Equal(
                typeof(Building_NutrientPasteDispenser),
                tapType.BaseType,
                "The validated VNPE tap must directly subclass the vanilla dispenser.");
            IntegrationAssert.Equal(
                tapType,
                tapDef.thingClass,
                "The finalized VNPE tap Def must retain the validated runtime type.");
            IntegrationAssert.NotNull(
                tapDef.comps,
                "The finalized VNPE tap Def must retain its component list.");
            IntegrationAssert.Equal(
                1,
                tapDef.comps!.Count(properties => pipePropertiesType!.IsInstanceOfType(properties)),
                "The finalized VNPE tap must retain exactly one pipe-resource component.");

            phase = "finalized exact dispenser classification";
            var vanillaDef = DefDatabase<ThingDef>.GetNamed("NutrientPasteDispenser");
            IntegrationAssert.True(
                VanillaNutrientPasteExpandedAdapter.Controls(tapDef, tapType),
                "The exact finalized VNPE tap must be owned by the guarded adapter.");
            IntegrationAssert.True(
                PasteDispenserAdapter.Supports(tapDef, tapType),
                "Prepared-paste dispensing must accept the exact finalized VNPE tap.");
            IntegrationAssert.True(
                PasteDispenserAdapter.Supports(vanillaDef, vanillaDef.thingClass),
                "The base vanilla dispenser must remain supported in the VNPE matrix.");

            phase = "live setting opt-out validation";
            var originalVnpeMode = ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded;
            try
            {
                ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded = OptionalIntegrationMode.Off;
                IntegrationAssert.False(
                    VanillaNutrientPasteExpandedAdapter.Controls(tapDef, tapType),
                    "Turning the VNPE integration off must immediately release the native tap.");
                IntegrationAssert.False(
                    PasteDispenserAdapter.Supports(tapDef, tapType),
                    "Prepared-paste dispensing must immediately reject the VNPE tap while its setting is off.");
                IntegrationAssert.True(
                    PasteDispenserAdapter.Supports(vanillaDef, vanillaDef.thingClass),
                    "Turning VNPE support off must not disable the base vanilla dispenser.");
            }
            finally
            {
                ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded = originalVnpeMode;
            }

            phase = "native Harmony patch metadata validation";
            var canDispense = AccessTools.PropertyGetter(
                typeof(Building_NutrientPasteDispenser),
                nameof(Building_NutrientPasteDispenser.CanDispenseNow));
            var tryDispense = AccessTools.Method(
                typeof(Building_NutrientPasteDispenser),
                nameof(Building_NutrientPasteDispenser.TryDispenseFood));
            IntegrationAssert.NotNull(
                canDispense,
                "The vanilla dispenser CanDispenseNow getter must exist in the active game.");
            IntegrationAssert.NotNull(
                tryDispense,
                "The vanilla dispenser TryDispenseFood method must exist in the active game.");
            var canDispensePatches = Harmony.GetPatchInfo(canDispense!);
            var tryDispensePatches = Harmony.GetPatchInfo(tryDispense!);
            IntegrationAssert.NotNull(
                canDispensePatches,
                "The active VNPE matrix must patch the vanilla CanDispenseNow getter.");
            IntegrationAssert.NotNull(
                tryDispensePatches,
                "The active VNPE and Immersive Chefs matrix must patch TryDispenseFood.");
            IntegrationAssert.True(
                canDispensePatches!.Prefixes.Any(patch =>
                    patch.PatchMethod?.DeclaringType?.FullName ==
                    "VNPE.Building_NutrientPasteDispenser_CanDispenseNow") == true,
                "VNPE's native CanDispenseNow pipe-network prefix must remain installed.");
            IntegrationAssert.True(
                tryDispensePatches!.Prefixes.Any(patch =>
                    patch.PatchMethod?.DeclaringType?.FullName ==
                    "VNPE.Building_NutrientPasteDispenser_TryDispenseFood") == true,
                "VNPE's native TryDispenseFood pipe-network prefix must remain installed.");
            IntegrationAssert.Equal(
                1,
                tryDispensePatches.Owners.Count(owner => owner == ImmersiveChefsMod.PackageId),
                "Plate attachment must retain one Immersive Chefs postfix on the shared native dispense method.");
        }
        catch (IntegrationTestAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            IntegrationAssert.Fail(
                $"The active VNPE contract threw {exception.GetType().FullName} during {phase}.");
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

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveGastronomyServedColonyCutleryReturnsToTheMap()
    {
        var gastronomyActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.gastronomy", StringComparison.OrdinalIgnoreCase));
        if (!gastronomyActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var cells = map.AllCells
            .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, cells.Count, "The Gastronomy fixture needs two empty walkable cells.");
        var patron = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var server = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var servedPlate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cancelledCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var completedCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var retainedPersonalCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var takeoverCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var cancelledJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        var completedJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);

        try
        {
            GenSpawn.Spawn(patron, cells[0], map);
            GenSpawn.Spawn(server, cells[1], map);
            cancelledCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            completedCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            retainedPersonalCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            takeoverCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            IntegrationAssert.True(
                embedded.TryEmbedPlate(servedPlate),
                "The waiter ownership fixture must begin with one exact embedded plate.");
            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(
                    retainedPersonalCutlery,
                    canMergeWithExistingStacks: false),
                "The waiter-takeover fixture must begin with exact personal cutlery.");
            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(takeoverCutlery, canMergeWithExistingStacks: false),
                "The waiter-takeover fixture must deliver exact colony cutlery.");
            var takeoverSession = new DiningSession(
                patron,
                JobMaker.MakeJob(JobDefOf.Ingest, meal),
                retainedPersonalCutlery,
                null,
                null,
                DiningCutlerySource.PersonalInventory,
                returnPlateToPersonalInventory: true,
                mealEmbeddedWare: embedded);
            takeoverSession.PickupCutlery();
            IntegrationAssert.True(
                retainedPersonalCutlery.GetComp<CompSanitation>().IsPersonalDiningWareFor(patron),
                "The picked personal cutlery must carry its temporary owner before waiter takeover.");
            takeoverSession.AcceptWaiterService(takeoverCutlery, server);
            IntegrationAssert.True(
                patron.inventory.innerContainer.Contains(retainedPersonalCutlery) &&
                !retainedPersonalCutlery.GetComp<CompSanitation>().IsDirty &&
                !retainedPersonalCutlery.GetComp<CompSanitation>().IsPersonalDiningWareFor(patron) &&
                !retainedPersonalCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Waiter takeover must retain clean personal cutlery and clear its temporary provenance.");
            takeoverSession.Cancel();
            IntegrationAssert.True(
                takeoverCutlery.Spawned &&
                !takeoverCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Cancelling waiter takeover must return colony cutlery and clear its recovery marker.");
            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(cancelledCutlery, canMergeWithExistingStacks: false),
                "The waiter-delivered cancellation fixture must begin in the patron inventory.");

            embedded.MarkPersonalPlateOwner(patron);
            cancelledCutlery.GetComp<CompSanitation>().MarkPersonalDiningOwner(patron);
            DiningSessionRegistry.TryAttachServed(patron, cancelledJob, meal, cancelledCutlery, server);
            IntegrationAssert.False(
                embedded.IsPersonalPlateFor(patron),
                "Waiter service must replace personal plate provenance with colony service ownership.");
            IntegrationAssert.False(
                cancelledCutlery.GetComp<CompSanitation>().IsPersonalDiningWareFor(patron),
                "Waiter service must replace personal cutlery provenance with colony service ownership.");
            IntegrationAssert.True(
                cancelledCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Waiter-delivered colony cutlery must be positively marked for interrupted-session recovery.");
            DiningSessionRegistry.Cleanup(patron, cancelledJob);
            IntegrationAssert.False(
                cancelledCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Cancelling waiter service must clear its temporary session-transfer marker.");

            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(completedCutlery, canMergeWithExistingStacks: false),
                "The waiter-delivered completion fixture must begin in the patron inventory.");
            embedded.MarkPersonalPlateOwner(patron);
            completedCutlery.GetComp<CompSanitation>().MarkPersonalDiningOwner(patron);
            DiningSessionRegistry.TryAttachServed(patron, completedJob, meal, completedCutlery, server);
            IntegrationAssert.False(
                embedded.IsPersonalPlateFor(patron),
                "Repeated waiter service must keep the embedded plate under colony ownership.");
            IntegrationAssert.False(
                completedCutlery.GetComp<CompSanitation>().IsPersonalDiningWareFor(patron),
                "Repeated waiter service must keep delivered cutlery under colony ownership.");
            DiningSessionRegistry.Complete(patron);

            IntegrationAssert.True(
                cancelledCutlery.Spawned && !patron.inventory.innerContainer.Contains(cancelledCutlery),
                "Cancelling served dining must return exact unused colony cutlery to the map.");
            IntegrationAssert.False(
                cancelledCutlery.GetComp<CompSanitation>().IsDirty,
                "Cancelling served dining must leave the unused colony cutlery clean.");
            IntegrationAssert.True(
                completedCutlery.Spawned && !patron.inventory.innerContainer.Contains(completedCutlery),
                "Completing served dining must return exact used colony cutlery to the map.");
            IntegrationAssert.True(
                completedCutlery.GetComp<CompSanitation>().IsDirty,
                "Completing served dining must return the used colony cutlery dirty.");
            IntegrationAssert.False(
                completedCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Completing waiter service must clear its temporary session-transfer marker.");
        }
        finally
        {
            DiningSessionRegistry.Cleanup(patron, cancelledJob);
            DiningSessionRegistry.Cleanup(patron, completedJob);
            foreach (var thing in new Thing[]
                     {
                         cancelledCutlery,
                         completedCutlery,
                         retainedPersonalCutlery,
                         takeoverCutlery,
                         meal,
                         servedPlate,
                         patron,
                         server
                     })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PendingMarkedWareRecoveryRunsWithoutAnActiveJob()
    {
        var map = Find.CurrentMap;
        var cell = map.AllCells
            .Where(candidate => candidate.Standable(map) && candidate.GetThingList(map).Count == 0)
            .OrderBy(candidate => candidate.DistanceToSquared(map.Center))
            .First();
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var activeCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        try
        {
            GenSpawn.Spawn(pawn, cell, map);
            pawn.jobs.StopAll();
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
                "The pending-recovery fixture must begin in the pawn's exact inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(activeCutlery, canMergeWithExistingStacks: false),
                "The pending-recovery fixture must retain a distinct active-session control.");
            cutlery.GetComp<CompSanitation>().MarkSessionTransferredWare();
            activeCutlery.GetComp<CompSanitation>().MarkSessionTransferredWare();
            GameComponent_ImmersiveChefsRecovery.ScheduleWareRecovery(pawn, cutlery);

            Current.Game.GetComponent<GameComponent_ImmersiveChefsRecovery>().GameComponentTick();

            IntegrationAssert.True(
                cutlery.Spawned && !pawn.inventory.innerContainer.Contains(cutlery),
                "Pending marked ware must return to the map without an active dining or cooking job.");
            IntegrationAssert.False(
                cutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Successful pending recovery must consume its exact transfer marker.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.Contains(activeCutlery) &&
                !activeCutlery.Spawned &&
                activeCutlery.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession,
                "Exact pending recovery must not drop a newly delivered active-session setting " +
                "that was not scheduled.");
        }
        finally
        {
            pawn.jobs?.StopAll();
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            foreach (var thing in new Thing[] { cutlery, activeCutlery, pawn })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ExceptionalIngestionCleanupReturnsTheExactCapturedPersonalPlate()
    {
        var map = Find.CurrentMap;
        var playerFaction = Faction.OfPlayer;
        var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
            faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
        var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            guestFaction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.WoodLog);
        var embedded = meal.GetComp<CompEmbeddedWare>();
        var job = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        var originalRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Off;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                embedded.TryEmbedPlate(plate),
                "The exceptional-ingestion fixture must begin with the exact clean embedded plate.");
            var fixtureCell = map.AllCells
                .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            GenSpawn.Spawn(guest, fixtureCell, map);
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The visiting pawn must bring its exact plated meal in personal inventory.");
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, job, meal),
                "The visiting pawn must attach a personal-meal dining session.");
            var throwingComp = new ThrowAfterEmbeddedWareComp { parent = meal };
            meal.AllComps.Add(throwingComp);
            var expectedFailureObserved = false;
            try
            {
                meal.Ingested(guest, meal.GetStatValue(StatDefOf.Nutrition));
            }
            catch (ExpectedLaterIngestionCompException)
            {
                expectedFailureObserved = true;
            }

            IntegrationAssert.True(
                expectedFailureObserved,
                "The real Thing.Ingested path must invoke the controlled later-comp failure.");
            IntegrationAssert.True(
                DiningSessionRegistry.Current(guest) is null,
                "The Harmony ingestion finalizer must cancel the failed dining session.");

            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(plate) &&
                !plate.Spawned &&
                plate.stackCount == 1,
                "Exceptional ingestion cleanup must return the exact single plate to personal inventory.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "Exceptional ingestion cleanup must restore the unused plate's clean sanitation state.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = originalRequirementMode;
            DiningSessionRegistry.EndIngestion(guest);
            DiningSessionRegistry.Cleanup(guest, job);
            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            guest.inventory.innerContainer.ClearAndDestroyContents();
            foreach (var thing in new Thing[] { meal, plate, guest })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    private sealed class ThrowAfterEmbeddedWareComp : ThingComp
    {
        public override void PostIngested(Pawn ingester)
        {
            throw new ExpectedLaterIngestionCompException();
        }
    }

    private sealed class ExpectedLaterIngestionCompException : Exception
    {
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PersonalCutleryStackSplitPreservesSanitationAcrossCancelAndCompletion()
    {
        var map = Find.CurrentMap;
        var playerFaction = Faction.OfPlayer;
        var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
            faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
        var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            guestFaction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var dirtyStack = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        ThingWithComps? completionStack = null;
        var cancellationJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        Job? completionJob = null;
        var originalRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var originalDirtyFallback = ImmersiveChefsMod.Settings.DirtyWareFallback;

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            ImmersiveChefsMod.Settings.DirtyWareFallback = DirtyWareFallback.Always;
            var fixtureCell = map.AllCells
                .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            GenSpawn.Spawn(guest, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);

            dirtyStack.stackCount = 2;
            dirtyStack.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            dirtyStack.GetComp<CompSanitation>().MarkDirty();
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(dirtyStack, canMergeWithExistingStacks: false),
                "The cancellation fixture must start as a two-item personal stack.");
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, cancellationJob, meal),
                "The guest must attach a personal-stack cancellation session.");
            var cancellationSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(cancellationSession?.Cutlery, dirtyStack),
                "The dirty personal stack must be the exact selected fallback.");
            cancellationSession!.PickupCutlery();
            var cancelledPiece = cancellationSession.CarriedCutlery as ThingWithComps;
            IntegrationAssert.NotNull(cancelledPiece, "Personal pickup must retain one exact split item.");
            IntegrationAssert.True(
                cancelledPiece!.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest),
                "Only the exact picked personal unit may carry the active dining-owner marker.");
            DiningSessionRegistry.Cleanup(guest, cancellationJob);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(cancelledPiece!) &&
                cancelledPiece!.GetComp<CompSanitation>().IsDirty &&
                cancelledPiece.GetComp<CompSanitation>().WashProvenance == WashProvenance.WildWater &&
                !cancelledPiece.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest) &&
                !cancelledPiece.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession &&
                !dirtyStack.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest),
                "Cancellation must preserve dirty wild-water sanitation and clear transient provenance " +
                "from both the split personal item and its source stack.");

            guest.inventory.innerContainer.ClearAndDestroyContents();
            completionStack = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
                ThingDefOf.Steel);
            completionStack.stackCount = 2;
            completionStack.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(completionStack, canMergeWithExistingStacks: false),
                "The completion fixture must start as a two-item personal stack.");
            completionJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, completionJob, meal),
                "The guest must attach a personal-stack completion session.");
            var completionSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(completionSession?.Cutlery, completionStack),
                "The clean personal stack must be the exact selected fallback.");
            completionSession!.PickupCutlery();
            var completedPiece = completionSession.CarriedCutlery as ThingWithComps;
            IntegrationAssert.NotNull(completedPiece, "Personal completion must retain one exact split item.");
            IntegrationAssert.True(
                completedPiece!.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest),
                "Only the exact picked completion unit may carry the active dining-owner marker.");
            DiningSessionRegistry.Complete(guest);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(completedPiece!) &&
                completedPiece!.GetComp<CompSanitation>().IsDirty &&
                completedPiece.GetComp<CompSanitation>().WashProvenance == WashProvenance.WildWater &&
                !completedPiece.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest) &&
                !completedPiece.GetComp<CompSanitation>().ReturnToMapAfterInterruptedSession &&
                !completionStack.GetComp<CompSanitation>().IsPersonalDiningWareFor(guest),
                "Completed dining must dirty the split personal item without losing wild-water provenance " +
                "and must clear all transient provenance.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = originalRequirementMode;
            ImmersiveChefsMod.Settings.DirtyWareFallback = originalDirtyFallback;
            DiningSessionRegistry.Cleanup(guest, cancellationJob);
            if (completionJob is not null)
            {
                DiningSessionRegistry.Cleanup(guest, completionJob);
            }

            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            guest.inventory.innerContainer.ClearAndDestroyContents();
            foreach (var thing in new Thing[] { dirtyStack, meal, guest })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            if (completionStack is not null && !completionStack.Destroyed)
            {
                completionStack.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveHospitalityValidatesTheExactArrivedGuestShape()
    {
        var hospitalityActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.hospitality", StringComparison.OrdinalIgnoreCase));
        if (!hospitalityActive)
        {
            return;
        }

        var utilityType = AccessTools.TypeByName("Hospitality.Utilities.GuestUtility");
        IntegrationAssert.NotNull(
            utilityType,
            "Active Hospitality must expose its public GuestUtility type.");
        IntegrationAssert.True(
            HospitalityAdapter.TryBind(utilityType, out var predicate, out var reason),
            "Active Hospitality must match the validated IsArrivedGuest(Pawn, out CompGuest) shape: " + reason);
        IntegrationAssert.NotNull(
            predicate,
            "A compatible active Hospitality assembly must produce an arrived-guest predicate.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Hospitality),
            "The default Auto setting must enable the shape-compatible active Hospitality adapter.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveHospitalityGuestPrefersColonyCutleryAndRetainsPersonalFallback()
    {
        var hospitalityActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.hospitality", StringComparison.OrdinalIgnoreCase));
        if (!hospitalityActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var playerFaction = Faction.OfPlayer;
        var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
            faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
        var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            guestFaction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        guest.Name = new NameSingle("Hospitality Ware Guest");
        guest.inventory.innerContainer.ClearAndDestroyContents();
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var colonyCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var personalCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var originalRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var firstJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        Job? fallbackJob = null;
        object? hospitalityMapComponent = null;
        Type? hospitalityMapComponentType = null;

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            colonyCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            personalCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The Hospitality fixture meal must retain its exact clean plate.");

            var fixtureCell = map.AllCells
                .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            GenSpawn.Spawn(guest, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(colonyCutlery, fixtureCell, map);
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(personalCutlery, canMergeWithExistingStacks: false),
                "The arrived guest must start with exact personal cutlery in its real inventory.");

            var compGuestType = AccessTools.TypeByName("Hospitality.CompGuest");
            IntegrationAssert.NotNull(compGuestType, "Active Hospitality must expose CompGuest.");
            var compGuest = guest.AllComps.FirstOrDefault(compGuestType!.IsInstanceOfType);
            IntegrationAssert.NotNull(
                compGuest,
                "Hospitality must attach CompGuest to the finalized human pawn Def.");
            hospitalityMapComponentType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            IntegrationAssert.NotNull(
                hospitalityMapComponentType,
                "Active Hospitality must expose its map-owned guest registry.");
            hospitalityMapComponent = map.components.FirstOrDefault(
                hospitalityMapComponentType!.IsInstanceOfType);
            IntegrationAssert.NotNull(
                hospitalityMapComponent,
                "Hospitality must construct its real map component before guest dining.");
            AccessTools.Method(hospitalityMapComponentType, "OnGuestJoinedLate")
                .Invoke(hospitalityMapComponent, new object[] { guest });
            AccessTools.Method(compGuestType, "Arrive").Invoke(compGuest, Array.Empty<object>());
            IntegrationAssert.True(
                HospitalityAdapter.IsArrivedGuest(guest),
                "The real active Hospitality utility must recognize the fixture pawn as arrived.");

            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, firstJob, meal),
                "The arrived guest must attach an ordinary dining session.");
            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.Current(guest)?.Cutlery, colonyCutlery),
                "Reachable colony cutlery must outrank the guest's eligible personal cutlery.");
            DiningSessionRegistry.Cleanup(guest, firstJob);
            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            colonyCutlery.Destroy(DestroyMode.Vanish);

            fallbackJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, fallbackJob, meal),
                "The arrived guest must attach its personal-fallback dining session.");
            var fallbackSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(fallbackSession?.Cutlery, personalCutlery),
                "With no eligible colony setting, the guest must select its exact personal cutlery.");
            fallbackSession!.PickupCutlery();
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery),
                "Personal cutlery must remain in the guest's inventory while in use.");
            DiningSessionRegistry.Cleanup(guest, fallbackJob);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery) &&
                !personalCutlery.GetComp<CompSanitation>().IsDirty &&
                !personalCutlery.Spawned,
                "Cancellation must retain the exact clean personal setting in guest inventory.");

            fallbackJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, fallbackJob, meal),
                "The arrived guest must reattach after a cancelled personal-fallback session.");
            fallbackSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(fallbackSession?.Cutlery, personalCutlery),
                "The replacement session must select the same exact personal setting.");
            fallbackSession!.PickupCutlery();
            DiningSessionRegistry.Complete(guest);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery) &&
                personalCutlery.GetComp<CompSanitation>().IsDirty &&
                !personalCutlery.Spawned,
                "The exact personal setting must return dirty to the guest inventory after dining.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = originalRequirementMode;
            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            if (fallbackJob is not null)
            {
                DiningSessionRegistry.Cleanup(guest, fallbackJob);
            }

            if (hospitalityMapComponent is not null && hospitalityMapComponentType is not null)
            {
                AccessTools.Method(hospitalityMapComponentType, "OnGuestAdopted")
                    .Invoke(hospitalityMapComponent, new object[] { guest });
            }

            foreach (var thing in new Thing[] { colonyCutlery, meal, plate, guest })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            if (!personalCutlery.Destroyed)
            {
                personalCutlery.holdingOwner?.Remove(personalCutlery);
                personalCutlery.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
