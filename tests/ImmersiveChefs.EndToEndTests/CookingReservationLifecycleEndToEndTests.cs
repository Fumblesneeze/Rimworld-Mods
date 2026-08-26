using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.cooking-reservation-lifecycle",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_600,
    MaxGameTicks = 20_000,
    MaxWallClockSeconds = 240)]
public sealed class CookingReservationLifecycleEndToEndTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefs_CookingReservationLifecycle";
    private Map map = null!;
    private Pawn interruptedCook = null!;
    private Pawn replacementCook = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private string interruptedCookId = string.Empty;
    private string replacementCookId = string.Empty;
    private string stoveId = string.Empty;
    private string cookwareId = string.Empty;
    private string plateId = string.Empty;
    private string interruptedJobId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.AutoCallAssistants = priorAssistants;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.AutoCallAssistants = false;

        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        interruptedCook = CreateCook("Interrupted reservation cook", cooking);
        replacementCook = CreateCook("Replacement reservation cook", cooking);
        GenSpawn.Spawn(interruptedCook, center + (IntVec3.South * 4), map);
        GenSpawn.Spawn(replacementCook, center + (IntVec3.East * 4), map);
        interruptedCook.drafter.Drafted = true;
        replacementCook.drafter.Drafted = true;

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);

        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 12f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(interruptedCook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.North * 4), map);
        GenSpawn.Spawn(plate, center + (IntVec3.North * 3), map);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 30;
        GenSpawn.Spawn(rice, center + (IntVec3.West * 3), map);

        interruptedCookId = interruptedCook.ThingID;
        replacementCookId = replacementCook.ThingID;
        stoveId = stove.ThingID;
        cookwareId = cookware.ThingID;
        plateId = plate.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before cooking reservation lifecycle regression",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return ToggleDraft(
            context,
            interruptedCook,
            "undraft the first cook through the native command",
            expectedCurrentState: true);
        yield return new AssertionStep(
            "speculative native bill scans leave cookware and plates unreserved",
            _ => AssertSpeculativeScansAreNeutral());
        yield return new CameraActionStep(
            "frame the first cook and unreserved cooking ware",
            new[] { interruptedCookId, stoveId, cookwareId, plateId },
            paddingPixels: 150);
        yield return new SelectionActionStep(
            "select the clean cookware before prioritizing cooking",
            new[] { cookwareId },
            additive: false);
        yield return new ScreenshotStep(
            "clean cookware remains available after repeated work scans",
            Array.Empty<string>(),
            paddingPixels: 0);

        var firstPrioritize = FindPrioritizeCooking(context, interruptedCook);
        yield return new FloatMenuActionStep(
            "prioritize the first cook through the native stove menu",
            interruptedCookId,
            stoveId,
            firstPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run the accepted cooking job until its ware reservations commit",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the accepted job reserves exact ware before active cooking",
            _ => ObserveCommittedPreWorkReservation(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause after accepted cooking reserves the exact ware",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture the accepted pre-work reservation",
            _ =>
            {
                interruptedJobId = interruptedCook.CurJob?.GetUniqueLoadID() ?? string.Empty;
                EndToEndAssert.True(!interruptedJobId.NullOrEmpty(),
                    "The accepted native DoBill job must have an exact load ID before interruption.");
            });
        yield return new SelectionActionStep(
            "select the first cook while the accepted job owns ware",
            new[] { interruptedCookId },
            additive: false);
        yield return new ScreenshotStep(
            "first cook is visibly traveling for the accepted cooking job",
            new[] { interruptedCookId, stoveId, cookwareId, plateId },
            paddingPixels: 150);

        yield return ToggleDraft(
            context,
            interruptedCook,
            "draft the first cook through the native command",
            expectedCurrentState: false);
        yield return new AssertionStep(
            "drafting releases the interrupted job exact clean ware",
            _ => AssertInterruptedReservationReleased());
        yield return new AssertionStep(
            "hand the still-pending bill to the replacement cook",
            _ =>
            {
                var bill = ((IBillGiver)stove).BillStack.Bills
                    .OfType<Bill_Production>()
                    .Single();
                bill.SetPawnRestriction(replacementCook);
            });

        yield return new SaveLoadActionStep(
            "save and load after the pre-work cooking interruption",
            SaveName);
        yield return new AssertionStep(
            "loaded interrupted cooking retains no orphaned ware reservation",
            _ =>
            {
                ResolveLoadedThings();
                AssertInterruptedReservationReleased();
                EndToEndAssert.True(
                    ((IBillGiver)stove).BillStack.Bills
                        .OfType<Bill_Production>()
                        .Single().PawnRestriction == replacementCook,
                    "The pending native bill must remain assigned to the replacement cook after load.");
            });
        yield return new SelectionActionStep(
            "select the same clean cookware after save and load",
            new[] { cookwareId },
            additive: false);
        yield return new CameraActionStep(
            "frame the released cookware after save and load",
            new[] { interruptedCookId, replacementCookId, stoveId, cookwareId, plateId },
            paddingPixels: 150);
        yield return new ScreenshotStep(
            "same cookware remains clean and available after interruption and load",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return ToggleDraft(
            context,
            replacementCook,
            "undraft the replacement cook through the native command",
            expectedCurrentState: true);
        var replacementPrioritize = FindPrioritizeCooking(context, replacementCook);
        yield return new FloatMenuActionStep(
            "prioritize the replacement cook through the native stove menu",
            replacementCookId,
            stoveId,
            replacementPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run replacement cooking with the released exact ware",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the replacement cook visibly uses the same exact cookware",
            _ => CookingSessionRegistry.TryGetActiveWorkProp(
                     replacementCook,
                     out var activeCookware,
                     out var activeStove) &&
                 ReferenceEquals(activeCookware, cookware) &&
                 ReferenceEquals(activeStove, stove),
            new EndToEndDeadline(1_400, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause during replacement cooking with the released cookware",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "clear selection for the replacement cooking action",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "replacement cook visibly uses the exact formerly released cookware",
            new[] { replacementCookId, stoveId },
            paddingPixels: 105);

        yield return ToggleDraft(
            context,
            replacementCook,
            "draft the replacement cook after active cooking",
            expectedCurrentState: false);
        yield return new AssertionStep(
            "active-work interruption also releases the exact cookware reservation",
            _ =>
            {
                EndToEndAssert.False(map.reservationManager.IsReserved(cookware),
                    "No cooking reservation may survive the replacement cook's native draft interruption.");
                EndToEndAssert.True(cookware.Spawned && cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The same cookware must return dirty after real active cooking, without duplication.");
            });
        yield return new CheckpointStep(
            "cooking reservation lifecycle result",
            _ => new Dictionary<string, string>
            {
                ["interruptedCook"] = interruptedCookId,
                ["interruptedJob"] = interruptedJobId,
                ["replacementCook"] = replacementCookId,
                ["cookware"] = cookwareId,
                ["plate"] = plateId,
                ["candidateScans"] = "5",
                ["loadedReservation"] = map.reservationManager.IsReserved(cookware).ToString(),
                ["cookwareDirtyAfterActiveInterruption"] =
                    cookware.GetComp<CompSanitation>()!.IsDirty.ToString()
            });
    }

    private void AssertSpeculativeScansAreNeutral()
    {
        var workers = DefDatabase<WorkGiverDef>.AllDefsListForReading
            .Where(def => def.workType == DefDatabase<WorkTypeDef>.GetNamed("Cooking"))
            .Select(def => def.Worker)
            .OfType<WorkGiver_DoBill>()
            .ToArray();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = workers
                .Select(worker => worker.JobOnThing(interruptedCook, stove, forced: false))
                .FirstOrDefault(job => job?.def == JobDefOf.DoBill);
            EndToEndAssert.NotNull(candidate,
                "The native cooking work scan must construct a covered DoBill candidate.");
            EndToEndAssert.True(candidate!.startTick < 0,
                "A discarded candidate must remain never-started.");
            EndToEndAssert.False(map.reservationManager.IsReserved(cookware),
                "A discarded native candidate must not reserve cookware.");
            EndToEndAssert.False(map.reservationManager.IsReserved(plate),
                "A discarded native candidate must not reserve plates.");
        }
    }

    private bool ObserveCommittedPreWorkReservation()
    {
        var job = interruptedCook.CurJob;
        return job?.def == JobDefOf.DoBill &&
               cookware.Spawned &&
               !cookware.GetComp<CompSanitation>()!.IsDirty &&
               map.reservationManager.ReservedBy(cookware, interruptedCook, job) &&
               map.reservationManager.ReservedBy(plate, interruptedCook, job) &&
               !CookingSessionRegistry.TryGetActiveWorkProp(interruptedCook, out _, out _);
    }

    private void AssertInterruptedReservationReleased()
    {
        EndToEndAssert.True(interruptedCook.drafter?.Drafted == true,
            "The first cook must remain drafted after the native interruption and load.");
        EndToEndAssert.False(map.reservationManager.IsReserved(cookware),
            "The interrupted cooking job must leave no cookware reservation.");
        EndToEndAssert.False(map.reservationManager.IsReserved(plate),
            "The interrupted cooking job must leave no plate reservation.");
        EndToEndAssert.True(
            cookware.Spawned &&
            plate.Spawned &&
            !cookware.GetComp<CompSanitation>()!.IsDirty,
            "Pre-work interruption must preserve the same clean spawned cookware and plate.");
    }

    private void ResolveLoadedThings()
    {
        map = Current.Game.CurrentMap;
        interruptedCook = map.mapPawns.AllPawnsSpawned
            .Single(pawn => pawn.ThingID == interruptedCookId);
        replacementCook = map.mapPawns.AllPawnsSpawned
            .Single(pawn => pawn.ThingID == replacementCookId);
        stove = ResolveSpawned<ThingWithComps>(stoveId);
        cookware = ResolveSpawned<ThingWithComps>(cookwareId);
        plate = ResolveSpawned<ThingWithComps>(plateId);
    }

    private T ResolveSpawned<T>(string thingId) where T : Thing =>
        map.listerThings.AllThings
            .OfType<T>()
            .Single(thing => thing.ThingID == thingId);

    private EndToEndFloatMenuOption FindPrioritizeCooking(
        IEndToEndContext context,
        Pawn cook)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cook.ThingID, stove.ThingID);
        var matches = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("cook", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "The pawn-restricted stove must expose one enabled native Prioritize cooking option; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        return matches[0];
    }

    private static GizmoActionStep ToggleDraft(
        IEndToEndContext context,
        Pawn pawn,
        string description,
        bool expectedCurrentState)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The selected cook must expose one native draft toggle in the expected state.");
        return new GizmoActionStep(
            description,
            new[] { pawn.ThingID },
            options[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: options[0].StableId);
    }

    private static Pawn CreateCook(string name, WorkTypeDef cooking)
    {
        var pawn = CookForYourselfFixture.CreateCapableCook(name, cooking);
        pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        FoodSearchE2EFixture.SetHunger(pawn, 1f);
        if (pawn.needs?.rest is { } rest)
        {
            rest.CurLevelPercentage = 1f;
        }

        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.workSettings.SetPriority(cooking, 1);
        for (var hour = 0; hour < 24; hour++)
        {
            pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }

        return pawn;
    }

    private static IntVec3 FindRoomCenter(Map map)
    {
        for (var x = -54; x <= 54; x += 18)
        {
            for (var z = -54; z <= 54; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (candidate.InBounds(map) && SquareIsUsable(map, candidate, 6))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear cooking reservation regression room.");
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
}
