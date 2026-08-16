using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.Dubs.InGame.IntegrationTests;

public static class DubsWaterCapabilityIntegrationTests
{
    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PrepStationFinalizesWithPipedSinkCapabilityAndNativeDrinkDiscoveryPatches()
    {
        var prep = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PrepStation");
        IntegrationAssert.NotNull(
            prep.GetModExtension<IntegratedSinkExtension>(),
            "The visibly sink-bearing prep station must retain its explicit runtime capability.");
        IntegrationAssert.True(
            prep.comps?.Count(properties =>
                properties.GetType().FullName == "DubsBadHygiene.CompProperties_Pipe") == 1,
            "The finalized Dubs-active prep station must expose exactly one real pipe capability.");

        var dubsAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(assembly =>
            assembly.GetName().Name == "BadHygiene");
        var sanitationUtil = dubsAssembly.GetType("DubsBadHygiene.SanitationUtil", throwOnError: true);
        var closestSanitation = dubsAssembly.GetType("DubsBadHygiene.ClosestSanitation", throwOnError: true);
        var allFixtures = sanitationUtil.GetMethod(
            "AllFixtures",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Map) },
            null);
        var usablePatches = closestSanitation.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name is "IsEverUsable" or "UsableNow")
            .ToArray();

        IntegrationAssert.NotNull(allFixtures, "The installed Dubs fixture discovery seam must remain exact.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(allFixtures!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod.DeclaringType == typeof(DubsWaterAdapter)) == true,
            "Immersive Chefs must append integrated sinks through Dubs' native fixture enumeration.");
        IntegrationAssert.True(
            usablePatches.Length == 2 && usablePatches.All(method =>
                Harmony.GetPatchInfo(method)?.Postfixes.Any(patch =>
                    patch.owner == ImmersiveChefsMod.PackageId &&
                    patch.PatchMethod.DeclaringType == typeof(DubsWaterAdapter)) == true),
            "Both native Dubs drink eligibility seams must enforce the integrated sink's real supply.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CapabilityClassificationIgnoresDefNameAndOwnerAndRejectsAnimalOrToiletFixtures()
    {
        var stage = "basin construction";
        try
        {
            var basin = MakeBareThing("BasinStuff");
            stage = "bath construction";
            var bath = MakeBareThing("BathtubStuff");
            stage = "trough construction";
            var trough = MakeBareThingOrNull("WaterTrough");
            stage = "toilet construction";
            var toilet = MakeBareThing("ToiletStuff");
            stage = "foreign basin Def identity";
            basin.def = CloneWithForeignIdentity(basin.def);
            stage = "foreign bath Def identity";
            bath.def = CloneWithForeignIdentity(bath.def);

            stage = "basin capability";
            IntegrationAssert.True(
                DubsWaterAdapter.HasDrinkableFixtureCapability(basin),
                "A foreign-name/owner Dubs basin must be recognized from its live capability.");
            stage = "bath capability";
            IntegrationAssert.True(
                DubsWaterAdapter.HasDrinkableFixtureCapability(bath),
                "A foreign-name/owner Dubs bath that native drinking accepts must share the capability path.");
            stage = "animal fixture rejection";
            if (trough is not null)
            {
                IntegrationAssert.True(
                    !DubsWaterAdapter.HasDrinkableFixtureCapability(trough),
                    "An active animal-only drinking fixture must not become a colonist dishwashing source.");
            }
            stage = "toilet fixture rejection";
            IntegrationAssert.True(
                !DubsWaterAdapter.HasDrinkableFixtureCapability(toilet),
                "A plumbed non-drinkable toilet must not become a dishwashing source.");
        }
        catch (NullReferenceException)
        {
            IntegrationAssert.True(false, "Null reference while validating Dubs capability at stage: " + stage);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void AlreadyAdmittedUnplatedSelfAndPatientMealsKeepTheirNativeReservations()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var bedCell = map.AllCells
            .Where(cell => GenAdj.OccupiedRect(cell, Rot4.North, ThingDefOf.Bed.size)
                .Cells.All(occupied => occupied.InBounds(map) &&
                                       occupied.Standable(map) &&
                                       occupied.GetEdifice(map) is null))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, bedCell, map, Rot4.North);
        bed.Medical = true;
        var cells = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           !bed.OccupiedRect().ExpandedBy(1).Contains(cell))
            .OrderBy(cell => cell.DistanceToSquared(bedCell))
            .Take(2)
            .ToArray();
        var eater = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var feeder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var patient = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var mealDef = DefDatabase<ThingDef>.GetNamed("MealLavish");
        var selfMeal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        var patientMeal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        var priorMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var existingServiceWare = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToArray();

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
            foreach (var existing in existingServiceWare)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            GenSpawn.Spawn(eater, cells[0], map);
            GenSpawn.Spawn(feeder, cells[1], map);
            GenSpawn.Spawn(patient, bed.GetSleepingSlotPos(0), map);
            var torso = patient.health.hediffSet.GetNotMissingParts()
                .First(part => part.def == BodyPartDefOf.Torso);
            var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, patient, torso);
            injury.Severity = 0.1f;
            patient.health.AddHediff(injury);
            var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            layDown.restUntilHealed = true;
            patient.jobs.StartJob(layDown, JobCondition.InterruptForced);
            for (var tick = 0; tick < 2_000 && !patient.InBed(); tick++)
            {
                patient.jobs.JobTrackerTick();
            }
            IntegrationAssert.True(
                patient.InBed() && patient.Awake(),
                "The FeedPatient regression fixture must use a conscious pawn in a medical bed.");
            GenSpawn.Spawn(selfMeal, cells[0], map);
            GenSpawn.Spawn(patientMeal, patient.Position, map);

            IntegrationAssert.True(
                selfMeal.GetComp<CompEmbeddedWare>()?.PeekOne() is null &&
                patientMeal.GetComp<CompEmbeddedWare>()?.PeekOne() is null,
                "Externally spawned regression meals must begin without fabricated plates.");

            var ingest = JobMaker.MakeJob(JobDefOf.Ingest, selfMeal);
            ingest.count = 1;
            eater.jobs.StartJob(ingest, JobCondition.InterruptForced);
            IntegrationAssert.True(
                ReferenceEquals(eater.CurJob, ingest),
                "The successful vanilla Ingest reservation must remain successful after attachment.");
            var selfSession = DiningSessionRegistry.Current(eater);
            IntegrationAssert.True(
                selfSession is { Plate: null, Cutlery: null },
                "The admitted self-ingest job must retain an honest missing-ware session.");

            var feed = JobMaker.MakeJob(JobDefOf.FeedPatient, patientMeal, patient);
            feed.count = 1;
            feed.SetTarget(TargetIndex.C, feeder);
            IntegrationAssert.True(
                ReferenceEquals(feed.GetTarget(TargetIndex.A).Thing, patientMeal) &&
                ReferenceEquals(feed.GetTarget(TargetIndex.B).Pawn, patient),
                "The native FeedPatient fixture must retain meal A and patient B before admission.");
            var reservationMethod = AccessTools.DeclaredMethod(
                typeof(JobDriver_FoodFeedPatient),
                nameof(JobDriver_FoodFeedPatient.TryMakePreToilReservations));
            IntegrationAssert.True(
                Harmony.GetPatchInfo(reservationMethod)?.Postfixes.Any(patch =>
                    patch.owner == ImmersiveChefsMod.PackageId &&
                    patch.PatchMethod.DeclaringType == typeof(FeedPatientReservationPatch)) == true,
                "The exact FeedPatient reservation postfix must be installed in the loaded game.");
            feeder.jobs.StartJob(feed, JobCondition.InterruptForced);
            IntegrationAssert.True(
                ReferenceEquals(feeder.CurJob, feed),
                "The successful vanilla FeedPatient reservation must remain successful after attachment.");
            IntegrationAssert.True(
                ReferenceEquals(feed.GetTarget(TargetIndex.B).Pawn, patient),
                "Native FeedPatient admission must retain the patient in target B.");
            var assisted = DiningSessionRegistry.Current(patient);
            IntegrationAssert.NotNull(
                assisted,
                "The already-admitted FeedPatient job must attach its session to the patient.");
            IntegrationAssert.True(
                assisted!.IsAssisted,
                "The attached patient session must retain the feeder as its distinct carrier pawn.");
            IntegrationAssert.True(
                assisted.Plate is null && assisted.Cutlery is null,
                "The assisted missing-ware session must not fabricate a plate or cutlery.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = priorMode;
            foreach (var existing in existingServiceWare)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var pawn in new[] { eater, feeder, patient })
            {
                pawn.jobs.StopAll();
                pawn.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            }

            foreach (var thing in new Thing[] { patientMeal, selfMeal, patient, feeder, eater, bed })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void StrictOrdinaryFoodSearchRejectsInventoryUnplatedMealButEmergencySearchAdmitsIt()
    {
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var meal = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("MealLavish"));
        var priorMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var priorThreshold = ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
        var otherFood = map.listerThings.AllThings
            .Where(thing => thing.def.IsNutritionGivingIngestible)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToArray();
        var cell = map.AllCells
            .Where(candidate => candidate.Standable(map) && candidate.GetEdifice(map) is null)
            .OrderBy(candidate => candidate.DistanceToSquared(map.Center))
            .First();

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
            ImmersiveChefsMod.Settings.EmergencyHungerThreshold = 0.1f;
            foreach (var existing in otherFood)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            GenSpawn.Spawn(pawn, cell, map);
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal),
                "The regression meal must enter the pawn's ordinary inventory holder.");
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>()?.PeekOne() is null,
                "The externally supplied inventory meal must begin without a plate.");

            var foodGiver = pawn.thinker.TryGetMainTreeThinkNode<JobGiver_GetFood>();
            IntegrationAssert.NotNull(
                foodGiver,
                "The ordinary colonist think tree must expose the native food job giver.");
            var tryGiveJob = AccessTools.DeclaredMethod(typeof(JobGiver_GetFood), "TryGiveJob")
                ?? throw new MissingMethodException(typeof(JobGiver_GetFood).FullName, "TryGiveJob");

            pawn.needs!.food!.CurLevelPercentage = 0.5f;
            var ordinary = (Job?)tryGiveJob.Invoke(foodGiver, new object[] { pawn });
            IntegrationAssert.True(
                ordinary is null || !ReferenceEquals(ordinary.GetTarget(TargetIndex.A).Thing, meal),
                "Strict non-emergency food search must reject an unplated inventory meal before StartJob.");

            pawn.needs.food.CurLevelPercentage = 0.05f;
            var emergency = (Job?)tryGiveJob.Invoke(foodGiver, new object[] { pawn });
            IntegrationAssert.True(
                emergency is not null && ReferenceEquals(emergency.GetTarget(TargetIndex.A).Thing, meal),
                "Emergency native food search must still admit the same unplated inventory meal.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = priorMode;
            ImmersiveChefsMod.Settings.EmergencyHungerThreshold = priorThreshold;
            foreach (var existing in otherFood)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            pawn.jobs.StopAll();
            pawn.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    private static ThingDef CloneWithForeignIdentity(ThingDef original)
    {
        var memberwiseClone = typeof(object).GetMethod(
            "MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(object).FullName, "MemberwiseClone");
        var clone = (ThingDef)memberwiseClone.Invoke(original, Array.Empty<object>());
        clone.defName = "ForeignHydrationFixture_" + original.defName;
        clone.modContentPack = null;
        return clone;
    }

    private static Thing MakeBareThing(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        return MakeBareThing(def, defName);
    }

    private static Thing? MakeBareThingOrNull(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        return def is null ? null : MakeBareThing(def, defName);
    }

    private static Thing MakeBareThing(ThingDef def, string defName)
    {
        var thing = (Thing?)FormatterServices.GetUninitializedObject(def.thingClass);
        IntegrationAssert.NotNull(thing, $"The finalized {defName} thingClass must remain constructible.");
        thing!.def = def;
        return thing;
    }
}
