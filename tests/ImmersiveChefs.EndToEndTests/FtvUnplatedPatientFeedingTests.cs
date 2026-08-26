using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.ftv-unplated-patient-feeding",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 20_000,
    MaxWallClockSeconds = 210)]
public sealed class FtvUnplatedPatientFeedingTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn feeder = null!;
    private Pawn patient = null!;
    private Building_Bed bed = null!;
    private ThingWithComps meal = null!;
    private EndToEndFloatMenuOption? feedOption;
    private float initialHunger;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorThreshold = settings.EmergencyHungerThreshold;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.EmergencyHungerThreshold = priorThreshold;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.EmergencyHungerThreshold = 0.1f;

        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, center + (IntVec3.North * 2), map, Rot4.North);
        bed.Medical = true;

        patient = FoodSearchE2EFixture.CreateColonist("Conscious patient");
        initialHunger = 0.05f;
        FoodSearchE2EFixture.SetHunger(patient, initialHunger);
        GenSpawn.Spawn(patient, center + IntVec3.East, map);
        var torso = patient.health.hediffSet.GetNotMissingParts()
            .First(part => part.def == BodyPartDefOf.Torso);
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, patient, torso);
        injury.Severity = 0.1f;
        patient.health.AddHediff(injury);
        var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        layDown.restUntilHealed = true;
        patient.jobs.StartJob(layDown, JobCondition.InterruptForced);

        feeder = FoodSearchE2EFixture.CreateColonist("Patient feeder");
        GenSpawn.Spawn(feeder, center + (IntVec3.South * 2), map);
        feeder.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!feeder.WorkTypeIsDisabled(workType))
            {
                feeder.workSettings.SetPriority(workType, 0);
            }
        }

        feeder.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
        feeder.drafter.Drafted = true;

        meal = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("FTV_MealLavish"));
        GenSpawn.Spawn(meal, center + IntVec3.West, map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "allow the conscious patient to enter the medical bed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the conscious patient is ready for ordinary feeding",
            _ => patient.InBed() && patient.Awake() && FeedPatientUtility.ShouldBeFed(patient),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(40)));
        yield return new TimeControlActionStep(
            "pause before the native feeding order becomes eligible",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the external FTV meal begins unplated with no available service ware",
            _ => AssertUnplatedPrecondition());
        yield return new SelectionActionStep(
            "select the drafted feeder before ordinary doctor work",
            new[] { feeder.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame feeder patient bed and exact unplated FTV meal",
            new[] { feeder.ThingID, patient.ThingID, meal.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "conscious patient visibly waits beside the exact unplated FTV meal",
            Array.Empty<string>(),
            paddingPixels: 0);

        var undraft = RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft the feeder through the native colonist gizmo",
            new[] { feeder.ThingID },
            undraft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: undraft.StableId);
        yield return new WaitUntilStep(
            "the native patient-feeding order becomes available after undrafting",
            _ => TryCaptureFeedPatientOption(context),
            new EndToEndDeadline(180, 600, TimeSpan.FromSeconds(10)));
        yield return new FloatMenuActionStep(
            "order native patient feeding through the right-click menu",
            feeder.ThingID,
            patient.ThingID,
            feedOption!.StableId);
        yield return new TimeControlActionStep(
            "run the native player-ordered patient feeding job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the feeder enters native FeedPatient with the exact FTV meal",
            _ => IsNativeFeedingActive(),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause during native unplated patient feeding",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the feeder performing native patient feeding",
            new[] { feeder.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "ordinary FeedPatient visibly proceeds without fabricated tableware",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish native unplated patient feeding",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact FTV meal is consumed by the conscious patient",
            _ => meal.Destroyed &&
                 patient.needs?.food?.CurLevelPercentage > initialHunger + 0.05f,
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after native unplated patient feeding",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the patient owns the missing-tableware consequence and the feeder does not",
            _ => AssertCompletedFeeding());
        yield return new SelectionActionStep(
            "select the fed conscious patient",
            new[] { patient.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the fed patient and empty meal location",
            new[] { patient.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "the conscious patient is visibly fed with no returned fabricated ware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "FTV unplated patient feeding result",
            _ => new Dictionary<string, string>
            {
                ["feeder"] = feeder.ThingID,
                ["patient"] = patient.ThingID,
                ["meal"] = meal.ThingID,
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["patientConscious"] = patient.Awake().ToString(),
                ["patientDiningMemories"] = MemoryCount(patient).ToString(),
                ["feederDiningMemories"] = MemoryCount(feeder).ToString(),
                ["serviceWareUnits"] = ServiceWareUnits().ToString()
            });
    }

    private void AssertUnplatedPrecondition()
    {
        EndToEndAssert.True(patient.InBed() && patient.Awake(),
            "The recipient must be conscious in the medical bed.");
        EndToEndAssert.True(FeedPatientUtility.ShouldBeFed(patient),
            "The recipient must remain eligible for vanilla patient feeding.");
        EndToEndAssert.True(meal.GetComp<CompEmbeddedWare>()?.PeekOne() is null,
            "The external FTV meal must not contain a fabricated plate.");
        EndToEndAssert.Equal(0, ServiceWareUnits(),
            "The isolated room must contain no plate or cutlery to reserve.");
    }

    private bool IsNativeFeedingActive()
    {
        if (feeder.CurJobDef != JobDefOf.FeedPatient ||
            !ReferenceEquals(feeder.CurJob?.GetTarget(TargetIndex.A).Thing, meal) ||
            !ReferenceEquals(feeder.CurJob?.GetTarget(TargetIndex.B).Pawn, patient))
        {
            return false;
        }

        var session = DiningSessionRegistry.Current(patient);
        EndToEndAssert.True(session is { IsAssisted: true, Plate: null, Cutlery: null },
            "The admitted native patient job must attach an honest missing-ware session to the patient.");
        EndToEndAssert.Equal(0, ServiceWareUnits(),
            "Starting FeedPatient must not fabricate service ware.");
        return true;
    }

    private void AssertCompletedFeeding()
    {
        EndToEndAssert.True(meal.Destroyed,
            "The exact FTV meal must be consumed through native patient feeding.");
        EndToEndAssert.True(patient.Awake(),
            "The patient must remain conscious for consequence ownership.");
        var patientMemory = DiningMemory(patient);
        EndToEndAssert.NotNull(patientMemory,
            "The conscious patient must receive the missing-tableware dining outcome.");
        EndToEndAssert.Equal(3, patientMemory!.CurStageIndex,
            "An unplated meal with no cutlery must produce the exact missing-tableware stage.");
        EndToEndAssert.Equal(0, MemoryCount(feeder),
            "The feeder must not receive the patient's dining outcome.");
        EndToEndAssert.Equal(0, ServiceWareUnits(),
            "Unplated patient feeding must not return or fabricate a plate or cutlery.");
    }

    private EndToEndGizmoOption RequiredDraftToggle(IEndToEndContext context)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { feeder.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The drafted feeder must expose one native Undraft toggle.");
        return options[0];
    }

    private bool TryCaptureFeedPatientOption(IEndToEndContext context)
    {
        var allOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(feeder.ThingID, patient.ThingID)
            .ToArray();
        var options = allOptions
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("feed", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        if (options.Length == 0)
        {
            return false;
        }

        EndToEndAssert.Equal(1, options.Length,
            "The conscious hungry patient must expose at most one enabled native feeding order. " +
            "Observed options: " + string.Join(" || ", allOptions.Select(option =>
                option.Label + " [disabled=" + option.Disabled + "]")) + ".");
        feedOption = options[0];
        return true;
    }

    private int ServiceWareUnits()
    {
        var mapUnits = map.listerThings.AllThings
            .Where(IsServiceWare)
            .Sum(thing => thing.stackCount);
        var pawnUnits = new[] { feeder, patient }.Sum(pawn =>
            (pawn.inventory?.innerContainer.Where(IsServiceWare).Sum(thing => thing.stackCount) ?? 0) +
            CarriedServiceWareUnits(pawn));
        return mapUnits + pawnUnits;
    }

    private static int CarriedServiceWareUnits(Pawn pawn)
    {
        var carried = pawn.carryTracker?.CarriedThing;
        return IsServiceWare(carried) ? carried!.stackCount : 0;
    }

    private static bool IsServiceWare(Thing? thing) =>
        thing?.def.GetModExtension<KitchenwareExtension>()?.product is
            KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;

    private static Thought_Memory? DiningMemory(Pawn pawn)
    {
        var def = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
        return pawn.needs?.mood?.thoughts?.memories.GetFirstMemoryOfDef(def);
    }

    private static int MemoryCount(Pawn pawn)
    {
        var def = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
        return pawn.needs?.mood?.thoughts?.memories.Memories.Count(memory => memory.def == def) ?? 0;
    }
}
