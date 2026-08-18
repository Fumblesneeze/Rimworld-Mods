using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.cooking-work-prop-directional-depth",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_400,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 255)]
public sealed class CookingWorkPropDirectionalDepthTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private CookingFixture northSide = null!;
    private CookingFixture southSide = null!;
    private ThingWithComps? northMeal;
    private ThingWithComps? southMeal;
    private float northDirectionZ;
    private float southDirectionZ;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, 2);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[0]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[1]);

        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorTemperature = settings.MealTemperatureEnabled;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.MealTemperatureEnabled = priorTemperature;
            settings.AutoCallAssistants = priorAssistants;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.MealTemperatureEnabled = false;
        settings.AutoCallAssistants = false;

        northSide = CreateFixture(centers[0], "North-facing depth cook", Rot4.North);
        southSide = CreateFixture(centers[1], "South-facing depth cook", Rot4.South);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                string.Equals(
                    window.GetType().FullName,
                    "LudeonTK.EditWindow_Log",
                    StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before directional cooking",
                "LudeonTK.EditWindow_Log");
        }

        yield return new TimeControlActionStep(
            "pause before the north-side cooking order",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return UndraftStep(context, northSide, "permit north-facing ordinary cooking");
        var northPrioritize = FindPrioritizeCooking(context, northSide);
        yield return new FloatMenuActionStep(
            "prioritize the north-side ordinary cooking bill",
            northSide.Cook.ThingID,
            northSide.Stove.ThingID,
            northPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run ordinary cooking toward the north-side stove",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the north-facing cook visibly uses the exact reserved cookware",
            _ => ObserveActiveWorkProp(northSide, Rot4.North, out northDirectionZ),
            new EndToEndDeadline(1_600, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause during north-facing active cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "north-side work uses a behind-pawn draw altitude",
            _ => AssertDirectionalDepth(northSide, northDirectionZ, expectBehind: true));
        yield return new CameraActionStep(
            "frame the north-facing cook and stove closely",
            new[] { northSide.Cook.ThingID, northSide.Stove.ThingID },
            paddingPixels: 90);
        yield return new SelectionActionStep(
            "clear selection for the unobscured north-facing depth frame",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "exact cookware renders behind the north-facing cook",
            new[] { northSide.Cook.ThingID, northSide.Stove.ThingID },
            paddingPixels: 95);

        yield return new TimeControlActionStep(
            "finish the north-side ordinary bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the north-side bill completes with the same physical cookware",
            _ => TryResolveProduct(northSide, out northMeal) &&
                 northSide.Cookware.Spawned &&
                 northSide.Cookware.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_600, 6_000, TimeSpan.FromSeconds(65)));

        yield return new TimeControlActionStep(
            "pause before the south-side cooking order",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return UndraftStep(context, southSide, "permit south-facing ordinary cooking");
        var southPrioritize = FindPrioritizeCooking(context, southSide);
        yield return new FloatMenuActionStep(
            "prioritize the south-side ordinary cooking bill",
            southSide.Cook.ThingID,
            southSide.Stove.ThingID,
            southPrioritize.StableId);
        yield return new TimeControlActionStep(
            "run ordinary cooking toward the south-side stove",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the south-facing cook visibly uses the exact reserved cookware",
            _ => ObserveActiveWorkProp(southSide, Rot4.South, out southDirectionZ),
            new EndToEndDeadline(1_600, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause during south-facing active cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "south-side work uses an in-front-of-pawn draw altitude",
            _ => AssertDirectionalDepth(southSide, southDirectionZ, expectBehind: false));
        yield return new CameraActionStep(
            "frame the south-facing cook and stove closely",
            new[] { southSide.Cook.ThingID, southSide.Stove.ThingID },
            paddingPixels: 90);
        yield return new SelectionActionStep(
            "clear selection for the unobscured south-facing depth frame",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "exact cookware renders in front of the south-facing cook",
            new[] { southSide.Cook.ThingID, southSide.Stove.ThingID },
            paddingPixels: 95);

        yield return new TimeControlActionStep(
            "finish the south-side ordinary bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the south-side bill completes with the same physical cookware",
            _ => TryResolveProduct(southSide, out southMeal) &&
                 southSide.Cookware.Spawned &&
                 southSide.Cookware.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_600, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after both directional cooking workflows",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CheckpointStep(
            "directional active-cookware depth result",
            _ => new Dictionary<string, string>
            {
                ["northCook"] = northSide.Cook.ThingID,
                ["northCookware"] = northSide.Cookware.ThingID,
                ["northMeal"] = northMeal?.ThingID ?? "none",
                ["northDirectionZ"] = northDirectionZ.ToString("R", CultureInfo.InvariantCulture),
                ["northAltitudeOffset"] = CookingWorkPropPolicy.AltitudeOffsetFor(northDirectionZ)
                    .ToString("R", CultureInfo.InvariantCulture),
                ["southCook"] = southSide.Cook.ThingID,
                ["southCookware"] = southSide.Cookware.ThingID,
                ["southMeal"] = southMeal?.ThingID ?? "none",
                ["southDirectionZ"] = southDirectionZ.ToString("R", CultureInfo.InvariantCulture),
                ["southAltitudeOffset"] = CookingWorkPropPolicy.AltitudeOffsetFor(southDirectionZ)
                    .ToString("R", CultureInfo.InvariantCulture)
            });
    }

    private CookingFixture CreateFixture(IntVec3 center, string name, Rot4 stoveRotation)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        var cook = CookForYourselfFixture.CreateCapableCook(name, cooking);
        FoodSearchE2EFixture.SetHunger(cook, 1f);
        SetOnlyCooking(cook, cooking);
        GenSpawn.Spawn(cook, center - (stoveRotation.FacingCell * 3), map);
        cook.drafter.Drafted = true;

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, stoveRotation);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);

        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        var plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        return new CookingFixture(cook, stove, cookware, plate);
    }

    private static GizmoActionStep UndraftStep(
        IEndToEndContext context,
        CookingFixture fixture,
        string description)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { fixture.Cook.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The selected directional-depth cook must expose one enabled native Draft toggle.");
        return new GizmoActionStep(
            description,
            new[] { fixture.Cook.ThingID },
            options[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: options[0].StableId);
    }

    private static EndToEndFloatMenuOption FindPrioritizeCooking(
        IEndToEndContext context,
        CookingFixture fixture)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(fixture.Cook.ThingID, fixture.Stove.ThingID);
        var matches = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("cook", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "The exact pawn-restricted stove must expose one enabled native Prioritize cooking option; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        return matches[0];
    }

    private static bool ObserveActiveWorkProp(
        CookingFixture fixture,
        Rot4 expectedFacing,
        out float directionZ)
    {
        directionZ = 0f;
        if (fixture.Cook.CurJobDef != JobDefOf.DoBill ||
            !CookingSessionRegistry.TryGetActiveWorkProp(
                fixture.Cook,
                out var activeCookware,
                out var activeStation) ||
            !ReferenceEquals(activeCookware, fixture.Cookware) ||
            !ReferenceEquals(activeStation, fixture.Stove))
        {
            return false;
        }

        var forward = (fixture.Stove.DrawPos - fixture.Cook.DrawPos).normalized;
        directionZ = forward.z;
        return fixture.Cook.Rotation == expectedFacing;
    }

    private static void AssertDirectionalDepth(
        CookingFixture fixture,
        float directionZ,
        bool expectBehind)
    {
        EndToEndAssert.True(CookingSessionRegistry.TryGetActiveWorkProp(
                fixture.Cook,
                out var activeCookware,
                out var activeStation) &&
            ReferenceEquals(activeCookware, fixture.Cookware) &&
            ReferenceEquals(activeStation, fixture.Stove),
            "The exact reserved cookware must remain the active rendered work prop at capture time.");
        var offset = CookingWorkPropPolicy.AltitudeOffsetFor(directionZ);
        EndToEndAssert.True(expectBehind ? directionZ > 0f : directionZ < 0f,
            "The captured pawn-to-stove direction must match the intended north/south fixture.");
        EndToEndAssert.True(expectBehind ? offset < 0f : offset > 0f,
            "The active cookware altitude must sort on the intended side of the pawn for this work direction.");
        EndToEndAssert.True(!fixture.Cookware.Spawned,
            "The rendered work prop must remain the same held physical cookware rather than a spawned cosmetic duplicate.");
    }

    private bool TryResolveProduct(CookingFixture fixture, out ThingWithComps? product)
    {
        product = map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(meal => ReferenceEquals(
                meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                fixture.Plate));
        return product is not null;
    }

    private static void SetOnlyCooking(Pawn pawn, WorkTypeDef cooking)
    {
        foreach (var candidate in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(candidate))
            {
                pawn.workSettings.SetPriority(candidate, 0);
            }
        }

        pawn.workSettings.SetPriority(cooking, 1);
        for (var hour = 0; hour < 24; hour++)
        {
            pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }
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

        throw new EndToEndAssertionException("Could not find two separated directional cooking rooms.");
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

    private sealed class CookingFixture
    {
        internal CookingFixture(
            Pawn cook,
            ThingWithComps stove,
            ThingWithComps cookware,
            ThingWithComps plate)
        {
            Cook = cook;
            Stove = stove;
            Cookware = cookware;
            Plate = plate;
        }

        internal Pawn Cook { get; }
        internal ThingWithComps Stove { get; }
        internal ThingWithComps Cookware { get; }
        internal ThingWithComps Plate { get; }
    }
}
