using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace GuestBedGizmo.EndToEndTests;

[RimWorldEndToEndTest(
    "guest-bed-gizmo.unified-owner-menu",
    "fumblesneeze.guestbedgizmo",
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.ideology",
    "orion.hospitality",
    "fumblesneeze.guestbedgizmo",
    MaxFrames = 1_800,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 120)]
public sealed class GuestBedOwnerMenuWorkflowTests : IRimWorldEndToEndTest
{
    private const string UnifiedCommandType = "GuestBedGizmo.Beds.Command_GuestAwareBedOwnerType";
    private const string GuestBedType = "Hospitality.Building_GuestBed";
    private const string RentalGizmoType = "Hospitality.Gizmo_GuestBed";
    private const string LegacyActionType =
        "Hospitality.Patches.Building_Bed_Patch+GetGizmos+<>c__DisplayClass1_0";

    private static readonly FieldInfo? FloatMenuOptions = typeof(FloatMenu).GetField(
        "options",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private Map map = null!;
    private IntVec3 bedCell;
    private IntVec3 siblingBedCell;
    private Building_Bed originalBed = null!;
    private Building_Bed originalSiblingBed = null!;
    private BedPropertySnapshot firstProperties = null!;
    private BedPropertySnapshot siblingProperties = null!;
    private Pawn warningOwner = null!;
    private readonly List<Thing> roomFixtures = new();
    private string guestLabel = null!;
    private string prisonerLabel = null!;
    private string[] expectedMenuLabels = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        bedCell = map.AllCells
            .Where(cell => CellRect.CenteredOn(cell, 3).Cells.All(candidate =>
                candidate.InBounds(map) &&
                candidate.Standable(map) &&
                !candidate.Fogged(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        siblingBedCell = bedCell + IntVec3.East + IntVec3.East;

        var bedDef = DefDatabase<ThingDef>.GetNamed("Bed");
        foreach (IntVec3 wallCell in CellRect.CenteredOn(bedCell, 3).EdgeCells)
        {
            Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
            wall.SetFactionDirect(Faction.OfPlayer);
            roomFixtures.Add(GenSpawn.Spawn(wall, wallCell, map));
        }
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

        originalBed = (Building_Bed)ThingMaker.MakeThing(bedDef, ThingDefOf.Steel);
        originalBed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(originalBed, bedCell, map, Rot4.South);
        originalSiblingBed = (Building_Bed)ThingMaker.MakeThing(bedDef, ThingDefOf.WoodLog);
        originalSiblingBed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(originalSiblingBed, siblingBedCell, map, Rot4.South);
        originalBed.HitPoints = Math.Max(1, originalBed.MaxHitPoints - 17);
        originalSiblingBed.HitPoints = Math.Max(1, originalSiblingBed.MaxHitPoints - 31);
        originalBed.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
        originalSiblingBed.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Awful, ArtGenerationContext.Colony);
        originalBed.ChangePaint(ColorDefOf.PlanGray);
        firstProperties = BedPropertySnapshot.Capture(originalBed);
        siblingProperties = BedPropertySnapshot.Capture(originalSiblingBed);

        IntVec3 warningOwnerCell = map.AllCells
            .Where(cell => !CellRect.CenteredOn(bedCell, 3).Contains(cell) &&
                           cell.Standable(map) &&
                           !cell.Fogged(map) &&
                           cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(bedCell))
            .First();
        warningOwner = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        warningOwner.Name = new NameTriple("Guest", "Bed", "Reviewer");
        roomFixtures.Add(GenSpawn.Spawn(warningOwner, warningOwnerCell, map));

        guestLabel = "CommandBedSetAsGuestLabel".Translate().ToString();
        prisonerLabel = "CommandBedSetForPrisonersLabel".Translate().ToString();
        expectedMenuLabels = new[]
        {
            "CommandBedSetForColonistsLabel".Translate().ToString(),
            prisonerLabel,
            "CommandBedSetForSlavesLabel".Translate().ToString(),
            guestLabel,
        };

        context.DeferCleanup(() =>
        {
            foreach (Building_Bed bed in new[] { bedCell, siblingBedCell }
                         .SelectMany(cell => map.thingGrid.ThingsListAt(cell))
                         .OfType<Building_Bed>()
                         .Distinct()
                         .ToArray())
            {
                if (!bed.Destroyed)
                {
                    bed.Destroy(DestroyMode.Vanish);
                }
            }

            foreach (Thing fixture in roomFixtures.Where(fixture => !fixture.Destroyed).ToArray())
            {
                fixture.Destroy(DestroyMode.Vanish);
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select ordinary steel and wooden beds through the native selector",
            new[] { originalBed.ThingID, originalSiblingBed.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the selected ordinary beds",
            new[] { originalBed.ThingID, originalSiblingBed.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "before opening the unified owner command on an ordinary bed",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "ordinary bed owner-command discovery",
            _ => DescribeCommandDiscovery(context, originalBed));

        EndToEndGizmoOption vanillaCommand = FindUnifiedCommand(
            context,
            originalBed,
            new[] { originalBed.ThingID });
        yield return new GizmoActionStep(
            "open the unified native bed-owner menu",
            new[] { originalBed.ThingID },
            vanillaCommand.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: vanillaCommand.StableId);
        yield return AssertOpenMenu("ordinary bed menu exposes exactly four ordered owner choices");
        yield return new ScreenshotStep(
            "ordinary bed unified owner menu with the guest choice visible",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CurrentFloatMenuActionStep(
            "choose guests through the open native owner menu",
            guestLabel);
        yield return new WaitUntilStep(
            "Hospitality replaces both selected beds with guest-bed forms",
            _ => originalBed.Destroyed && originalSiblingBed.Destroyed &&
                 IsGuestBed(CurrentBed()) && IsGuestBed(CurrentSiblingBed()),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep(
            "both guest replacements remain selected with unified owner commands and rental controls",
            _ =>
            {
                AssertGuestState(CurrentBed(), expectedSelectedCount: 2);
                AssertGuestState(CurrentSiblingBed(), expectedSelectedCount: 2);
                firstProperties.AssertPreservedBy(CurrentBed(), "first guest replacement");
                siblingProperties.AssertPreservedBy(CurrentSiblingBed(), "sibling guest replacement");
            });
        yield return new CameraActionStep(
            "move to a materially wider guest-bed view",
            new[] { CurrentBed().ThingID, CurrentSiblingBed().ThingID },
            paddingPixels: 420);
        yield return new ScreenshotStep(
            "after choosing guests the selected Hospitality bed keeps its ordinary controls",
            Array.Empty<string>(),
            paddingPixels: 0);

        Building_Bed guestBed = CurrentBed();
        Building_Bed unselectedGuestSibling = CurrentSiblingBed();
        EndToEndAssert.True(ReferenceEquals(guestBed.GetRoom(), unselectedGuestSibling.GetRoom()),
            "Both guest beds must remain in the same enclosed room for native propagation.");
        yield return new SelectionActionStep(
            "select only the first guest bed before native prisoner room propagation",
            new[] { guestBed.ThingID },
            additive: false);
        unselectedGuestSibling.CompAssignableToPawn.ForceAddPawn(warningOwner);
        EndToEndAssert.True(unselectedGuestSibling.OwnersForReading.Contains(warningOwner),
            "The unselected guest sibling must carry one humanlike owner to exercise vanilla's confirmation path.");

        EndToEndGizmoOption guestCommand = FindUnifiedCommand(
            context,
            guestBed,
            new[] { guestBed.ThingID });
        yield return new GizmoActionStep(
            "open the unified owner menu before testing native cancellation",
            new[] { guestBed.ThingID },
            guestCommand.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: guestCommand.StableId);
        yield return AssertOpenMenu("guest bed menu preserves the same four ordered choices");
        yield return new ScreenshotStep(
            "guest bed unified owner menu before returning to a vanilla owner type",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CurrentFloatMenuActionStep(
            "choose prisoners through the open native owner menu",
            prisonerLabel);
        yield return new WaitUntilStep(
            "vanilla opens its owner-removal confirmation before any Hospitality swap",
            _ => Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Any(window => window.IsOpen),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "native owner-removal confirmation appears while both guest beds remain unchanged",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WindowCancelActionStep(
            "cancel the native owner-removal confirmation",
            "Verse.Dialog_MessageBox");
        yield return new AssertionStep(
            "cancelling the native warning leaves both exact guest beds and their owner unchanged",
            _ =>
            {
                EndToEndAssert.False(guestBed.Destroyed,
                    "The selected guest bed must not be swapped when the player cancels.");
                EndToEndAssert.False(unselectedGuestSibling.Destroyed,
                    "The unselected guest sibling must not be swapped when the player cancels.");
                EndToEndAssert.True(IsGuestBed(CurrentBed()) && IsGuestBed(CurrentSiblingBed()),
                    "Both cells must still contain Hospitality guest beds after cancellation.");
                EndToEndAssert.True(unselectedGuestSibling.OwnersForReading.Contains(warningOwner),
                    "Cancellation must preserve the owner that caused vanilla's warning.");
            });

        guestCommand = FindUnifiedCommand(
            context,
            guestBed,
            new[] { guestBed.ThingID });
        yield return new GizmoActionStep(
            "reopen the unified owner menu after the cancelled attempt",
            new[] { guestBed.ThingID },
            guestCommand.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: guestCommand.StableId);
        yield return AssertOpenMenu("guest bed menu remains available after cancellation");
        yield return new CurrentFloatMenuActionStep(
            "choose prisoners again through the open native owner menu",
            prisonerLabel);
        yield return new WaitUntilStep(
            "vanilla reopens the owner-removal confirmation for the committed attempt",
            _ => Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Any(window => window.IsOpen),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new WindowAcceptActionStep(
            "accept the native owner-removal confirmation",
            "Verse.Dialog_MessageBox");
        yield return new WaitUntilStep(
            "Hospitality returns the selected guest bed and its unselected room sibling to vanilla prisoner beds",
            _ => !IsGuestBed(CurrentBed()) && CurrentBed().ForPrisoners &&
                 !IsGuestBed(CurrentSiblingBed()) && CurrentSiblingBed().ForPrisoners,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep(
            "reverse conversion covers the full native room closure without selecting the sibling",
            _ =>
            {
                AssertVanillaPrisonerState(CurrentBed(), expectedSelected: true);
                AssertVanillaPrisonerState(CurrentSiblingBed(), expectedSelected: false);
                firstProperties.AssertPreservedBy(CurrentBed(), "first prisoner replacement");
                siblingProperties.AssertPreservedBy(CurrentSiblingBed(), "sibling prisoner replacement");
                EndToEndAssert.True(unselectedGuestSibling.Destroyed,
                    "The exact unselected Hospitality sibling must be replaced, not mutated into a guest/prisoner hybrid.");
            });
        yield return new ScreenshotStep(
            "after choosing prisoners the replacement bed remains selected",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "open the native developer console for reviewed log inspection",
            _ =>
            {
                EndToEndAssert.False(
                    Find.WindowStack.Windows.Any(window =>
                        string.Equals(window.GetType().FullName, "LudeonTK.EditWindow_Log", StringComparison.Ordinal)),
                    "The developer console must not already be open before this test-owned evidence step.");
                Find.WindowStack.Add(new LudeonTK.EditWindow_Log());
            });
        yield return new ScreenshotStep(
            "native developer console after the complete owner workflow",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WindowCancelActionStep(
            "close the test-owned native developer console",
            "LudeonTK.EditWindow_Log");
        yield return new CheckpointStep(
            "unified owner-menu conversion outcome",
            _ => new Dictionary<string, string>
            {
                ["menuLabels"] = string.Join(" | ", expectedMenuLabels),
                ["finalBedDef"] = CurrentBed().def.defName,
                ["finalOwnerType"] = CurrentBed().ForOwnerType.ToString(),
                ["roomSiblingBedDef"] = CurrentSiblingBed().def.defName,
                ["roomSiblingOwnerType"] = CurrentSiblingBed().ForOwnerType.ToString(),
                ["selectedCount"] = Find.Selector.SelectedObjects.Count.ToString(),
                ["legacyToggleCount"] = LegacyToggleCount(CurrentBed()).ToString(),
            });
    }

    private AssertionStep AssertOpenMenu(string name) => new(
        name,
        _ => EndToEndAssert.True(
            expectedMenuLabels.SequenceEqual(
                OpenMenuOptions().Select(option => option.Label)),
            "The real native FloatMenu must show colonists, prisoners, slaves, then guests exactly once."));

    private Building_Bed CurrentBed() => CurrentBedAt(bedCell);

    private Building_Bed CurrentSiblingBed() => CurrentBedAt(siblingBedCell);

    private Building_Bed CurrentBedAt(IntVec3 cell)
    {
        Building_Bed[] beds = map.thingGrid.ThingsListAt(cell)
            .OfType<Building_Bed>()
            .Where(bed => bed.Spawned && !bed.Destroyed)
            .ToArray();
        EndToEndAssert.Equal(1, beds.Length,
            "Exactly one replacement bed must occupy the original cell.");
        return beds[0];
    }

    private static EndToEndGizmoOption FindUnifiedCommand(
        IEndToEndContext context,
        Building_Bed diagnosticBed,
        IReadOnlyList<string> selectedBedIds)
    {
        IReadOnlyList<EndToEndGizmoOption> available = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(selectedBedIds, Array.Empty<string>());
        EndToEndGizmoOption[] matches = available
            .Where(option => option.RuntimeType == UnifiedCommandType)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "The selected bed must expose exactly one unified owner command. Gateway=" +
            string.Join(" | ", available.Select(option =>
                option.RuntimeType + ":" + option.Label + ":" + option.Interaction)) +
                "; direct=" + string.Join(" | ", diagnosticBed.GetGizmos().Select(gizmo =>
                gizmo.GetType().FullName + ":" + (gizmo as Command)?.Label)));
        EndToEndAssert.False(matches[0].Disabled,
            "The unified owner command must be enabled for a selected bed.");
        EndToEndAssert.Equal(EndToEndGizmoInteraction.Invoke, matches[0].Interaction,
            "The unified owner command must use RimWorld's native Command_Action path.");
        return matches[0];
    }

    private static IReadOnlyDictionary<string, string> DescribeCommandDiscovery(
        IEndToEndContext context,
        Building_Bed bed)
    {
        IReadOnlyList<EndToEndGizmoOption> available = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { bed.ThingID }, Array.Empty<string>());
        return new Dictionary<string, string>
        {
            ["directGizmos"] = string.Join("\n", bed.GetGizmos().Select(gizmo =>
            {
                var command = gizmo as Command;
                var action = gizmo as Command_Action;
                return string.Join(" | ", new[]
                {
                    gizmo.GetType().FullName,
                    command?.defaultLabel ?? string.Empty,
                    command?.defaultDesc ?? string.Empty,
                    action?.action?.Method.DeclaringType?.FullName ?? string.Empty,
                    action?.action?.Method.Name ?? string.Empty,
                });
            })),
            ["gatewayGizmos"] = string.Join("\n", available.Select(option =>
                option.RuntimeType + " | " + option.Label + " | " + option.Interaction)),
        };
    }

    private static FloatMenuOption[] OpenMenuOptions()
    {
        FloatMenu[] menus = Find.WindowStack.Windows.OfType<FloatMenu>()
            .Where(menu => menu.IsOpen)
            .ToArray();
        EndToEndAssert.Equal(1, menus.Length,
            "Exactly one native FloatMenu must be open for owner selection.");
        var options = FloatMenuOptions?.GetValue(menus[0]) as IEnumerable<FloatMenuOption>;
        EndToEndAssert.NotNull(options,
            "The inspected RimWorld 1.6 FloatMenu option shape must remain available.");
        return options!.ToArray();
    }

    private static bool IsGuestBed(Building_Bed bed) =>
        bed.GetType().FullName == GuestBedType;

    private static int LegacyToggleCount(Building_Bed bed) => bed.GetGizmos()
        .OfType<Command_Toggle>()
        .Count(toggle => toggle.toggleAction?.Method.DeclaringType?.FullName == LegacyActionType);

    private static void AssertGuestState(Building_Bed bed, int expectedSelectedCount)
    {
        Gizmo[] gizmos = bed.GetGizmos().ToArray();
        EndToEndAssert.True(IsGuestBed(bed),
            "Choosing guests must produce Hospitality's real guest-bed runtime type.");
        EndToEndAssert.Equal(expectedSelectedCount, Find.Selector.SelectedObjects.Count,
            "Selection restoration must retain every selected replacement object.");
        EndToEndAssert.True(Find.Selector.SelectedObjects.Contains(bed),
            "The exact replacement guest bed must remain in the selection.");
        EndToEndAssert.Equal(1, gizmos.Count(gizmo => gizmo.GetType().FullName == UnifiedCommandType),
            "The replacement guest bed must expose one unified owner command.");
        EndToEndAssert.Equal(1, gizmos.Count(gizmo => gizmo.GetType().FullName == RentalGizmoType),
            "Hospitality's unrelated guest rental control must remain available.");
        EndToEndAssert.Equal(0, LegacyToggleCount(bed),
            "Hospitality's separate legacy guest toggle must be absent.");
    }

    private static void AssertVanillaPrisonerState(Building_Bed bed, bool expectedSelected)
    {
        EndToEndAssert.False(IsGuestBed(bed),
            "Choosing prisoners must return to the vanilla bed runtime type.");
        EndToEndAssert.Equal(BedOwnerType.Prisoner, bed.ForOwnerType,
            "The reverse conversion must delegate to vanilla prisoner ownership.");
        EndToEndAssert.Equal(expectedSelected, Find.Selector.SelectedObjects.Contains(bed),
            "Only the originally selected replacement prisoner bed may remain selected.");
        EndToEndAssert.Equal(0, LegacyToggleCount(bed),
            "The separate legacy guest toggle must remain absent after reverse conversion.");
    }

    private sealed class BedPropertySnapshot
    {
        private BedPropertySnapshot(
            string stuff,
            int hitPoints,
            string quality,
            string artTitle,
            string artAuthor,
            string paint,
            string style,
            IntVec3 position,
            Rot4 rotation,
            string faction)
        {
            Stuff = stuff;
            HitPoints = hitPoints;
            Quality = quality;
            ArtTitle = artTitle;
            ArtAuthor = artAuthor;
            Paint = paint;
            Style = style;
            Position = position;
            Rotation = rotation;
            Faction = faction;
        }

        private string Stuff { get; }
        private int HitPoints { get; }
        private string Quality { get; }
        private string ArtTitle { get; }
        private string ArtAuthor { get; }
        private string Paint { get; }
        private string Style { get; }
        private IntVec3 Position { get; }
        private Rot4 Rotation { get; }
        private string Faction { get; }

        internal static BedPropertySnapshot Capture(Building_Bed bed)
        {
            CompArt? art = bed.TryGetComp<CompArt>();
            string artTitle = art?.Active == true ? art.Title : "<inactive>";
            string artAuthor = art?.Active == true ? art.AuthorName.ToString() : "<inactive>";
            return new BedPropertySnapshot(
                bed.Stuff?.defName ?? "<none>",
                bed.HitPoints,
                bed.TryGetComp<CompQuality>()?.Quality.ToString() ?? "<none>",
                artTitle,
                artAuthor,
                bed.PaintColorDef?.defName ?? "<none>",
                bed.StyleDef?.defName ?? "<none>",
                bed.Position,
                bed.Rotation,
                bed.Faction?.Name ?? "<none>");
        }

        internal void AssertPreservedBy(Building_Bed bed, string label)
        {
            BedPropertySnapshot actual = Capture(bed);
            EndToEndAssert.Equal(Stuff, actual.Stuff, label + " must preserve Stuff.");
            EndToEndAssert.Equal(HitPoints, actual.HitPoints, label + " must preserve hit points.");
            EndToEndAssert.Equal(Quality, actual.Quality, label + " must preserve quality.");
            EndToEndAssert.Equal(ArtTitle, actual.ArtTitle, label + " must preserve art title.");
            EndToEndAssert.Equal(ArtAuthor, actual.ArtAuthor, label + " must preserve art author.");
            EndToEndAssert.Equal(Paint, actual.Paint, label + " must preserve paint.");
            EndToEndAssert.Equal(Style, actual.Style, label + " must preserve style.");
            EndToEndAssert.Equal(Position, actual.Position, label + " must preserve position.");
            EndToEndAssert.Equal(Rotation, actual.Rotation, label + " must preserve rotation.");
            EndToEndAssert.Equal(Faction, actual.Faction, label + " must preserve faction.");
        }
    }
}

[RimWorldEndToEndTest(
    "guest-bed-gizmo.outdoor-rejection-and-mixed-selection",
    "fumblesneeze.guestbedgizmo",
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.ideology",
    "orion.hospitality",
    "fumblesneeze.guestbedgizmo",
    MaxFrames = 1_000,
    MaxGameTicks = 1_000,
    MaxWallClockSeconds = 90)]
public sealed class GuestBedOwnerMenuEdgeWorkflowTests : IRimWorldEndToEndTest
{
    private const string UnifiedCommandType = "GuestBedGizmo.Beds.Command_GuestAwareBedOwnerType";
    private const string GuestBedType = "Hospitality.Building_GuestBed";

    private Map map = null!;
    private IntVec3 ordinaryCell;
    private Building_Bed ordinaryBed = null!;
    private Building_Bed animalBed = null!;
    private Building_Bed foreignBed = null!;
    private string guestLabel = null!;
    private string prisonerLabel = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException("A playable map is required.");
        ordinaryCell = map.AllCells
            .Where(cell => IsClearOutdoorCell(cell) &&
                           IsClearOutdoorCell(cell + IntVec3.East * 4) &&
                           IsClearOutdoorCell(cell + IntVec3.East * 8))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        IntVec3 animalCell = ordinaryCell + IntVec3.East * 4;
        IntVec3 foreignCell = ordinaryCell + IntVec3.East * 8;

        ordinaryBed = SpawnBed("Bed", ordinaryCell, Faction.OfPlayer);
        animalBed = SpawnBed("AnimalSleepingBox", animalCell, Faction.OfPlayer);
        Faction foreignFaction = Find.FactionManager.AllFactionsListForReading
            .First(faction => faction != Faction.OfPlayer &&
                              !faction.IsPlayer &&
                              faction.def.humanlikeFaction);
        foreignBed = SpawnBed("Bed", foreignCell, foreignFaction);
        guestLabel = "CommandBedSetAsGuestLabel".Translate().ToString();
        prisonerLabel = "CommandBedSetForPrisonersLabel".Translate().ToString();

        context.DeferCleanup(() =>
        {
            foreach (IntVec3 cell in new[] { ordinaryCell, animalCell, foreignCell })
            {
                foreach (Building_Bed bed in map.thingGrid.ThingsListAt(cell)
                             .OfType<Building_Bed>()
                             .Where(bed => !bed.Destroyed)
                             .ToArray())
                {
                    bed.Destroy(DestroyMode.Vanish);
                }
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select one eligible bed together with player animal and foreign humanlike beds",
            new[] { ordinaryBed.ThingID, animalBed.ThingID, foreignBed.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the deliberately mixed bed selection",
            new[] { ordinaryBed.ThingID, animalBed.ThingID, foreignBed.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            "mixed selection before choosing guests",
            Array.Empty<string>(),
            paddingPixels: 0);

        EndToEndGizmoOption command = FindUnifiedCommand(context, ordinaryBed);
        yield return new GizmoActionStep(
            "open the eligible ordinary bed's unified owner menu while the mixed selection remains active",
            new[] { ordinaryBed.ThingID },
            command.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: command.StableId);
        yield return new CurrentFloatMenuActionStep(
            "choose guests through the native menu for the mixed selection",
            guestLabel);
        yield return new WaitUntilStep(
            "only the eligible player humanlike bed becomes a Hospitality guest bed",
            _ => ordinaryBed.Destroyed && IsGuestBed(CurrentOrdinaryBed()),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep(
            "mixed selection filters animal and foreign beds without partial conversion",
            _ =>
            {
                Building_Bed guestBed = CurrentOrdinaryBed();
                EndToEndAssert.True(ReferenceEquals(CurrentBedAt(animalBed.Position), animalBed),
                    "The player animal bed must remain the exact original object.");
                EndToEndAssert.True(ReferenceEquals(CurrentBedAt(foreignBed.Position), foreignBed),
                    "The foreign humanlike bed must remain the exact original object.");
                EndToEndAssert.False(animalBed.Destroyed || foreignBed.Destroyed,
                    "Unsupported mixed-selection beds must not be swapped.");
                EndToEndAssert.Equal(3, Find.Selector.SelectedObjects.Count,
                    "Selection restoration must retain the guest replacement and both untouched beds.");
                EndToEndAssert.True(Find.Selector.SelectedObjects.Contains(guestBed) &&
                                    Find.Selector.SelectedObjects.Contains(animalBed) &&
                                    Find.Selector.SelectedObjects.Contains(foreignBed),
                    "The restored mixed selection must contain the exact replacement and untouched objects.");
            });
        yield return new ScreenshotStep(
            "after mixed guest conversion only the eligible bed was replaced",
            Array.Empty<string>(),
            paddingPixels: 0);

        Building_Bed outdoorGuestBed = CurrentOrdinaryBed();
        yield return new SelectionActionStep(
            "select only the outdoor guest bed",
            new[] { outdoorGuestBed.ThingID },
            additive: false);
        command = FindUnifiedCommand(context, outdoorGuestBed);
        yield return new GizmoActionStep(
            "open the outdoor guest bed's unified owner menu",
            new[] { outdoorGuestBed.ThingID },
            command.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: command.StableId);
        yield return new CurrentFloatMenuActionStep(
            "choose prisoners through the native menu while the bed is outdoors",
            prisonerLabel);
        yield return new AssertionStep(
            "vanilla outdoor prisoner rejection happens before any Hospitality replacement",
            _ =>
            {
                EndToEndAssert.False(outdoorGuestBed.Destroyed,
                    "The exact outdoor guest bed must remain when vanilla rejects prisoner use.");
                EndToEndAssert.True(ReferenceEquals(CurrentOrdinaryBed(), outdoorGuestBed),
                    "Outdoor rejection must not swap the guest bed.");
                EndToEndAssert.False(outdoorGuestBed.ForPrisoners,
                    "Outdoor rejection must not leave a guest/prisoner hybrid.");
            });
        yield return new ScreenshotStep(
            "native outdoor-prisoner rejection leaves the guest bed unchanged",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "mixed selection and outdoor rejection outcome",
            _ => new Dictionary<string, string>
            {
                ["guestRuntimeType"] = CurrentOrdinaryBed().GetType().FullName,
                ["animalRuntimeType"] = animalBed.GetType().FullName,
                ["foreignFaction"] = foreignBed.Faction?.Name ?? string.Empty,
                ["expectedOutdoorRejection"] =
                    "CommandBedSetForPrisonersFailOutdoors".Translate().ToString(),
            });
    }

    private bool IsClearOutdoorCell(IntVec3 cell) =>
        cell.InBounds(map) &&
        cell.Standable(map) &&
        !cell.Fogged(map) &&
        !map.roofGrid.Roofed(cell) &&
        cell.GetThingList(map).Count == 0;

    private Building_Bed SpawnBed(string defName, IntVec3 cell, Faction faction)
    {
        ThingDef def = DefDatabase<ThingDef>.GetNamed(defName);
        var bed = (Building_Bed)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.WoodLog : null);
        bed.SetFactionDirect(faction);
        return (Building_Bed)GenSpawn.Spawn(bed, cell, map, Rot4.South);
    }

    private Building_Bed CurrentOrdinaryBed() => CurrentBedAt(ordinaryCell);

    private Building_Bed CurrentBedAt(IntVec3 cell)
    {
        Building_Bed[] beds = map.thingGrid.ThingsListAt(cell)
            .OfType<Building_Bed>()
            .Where(bed => bed.Spawned && !bed.Destroyed)
            .ToArray();
        EndToEndAssert.Equal(1, beds.Length,
            "Exactly one bed must occupy each mixed-selection fixture cell.");
        return beds[0];
    }

    private static EndToEndGizmoOption FindUnifiedCommand(
        IEndToEndContext context,
        Building_Bed bed)
    {
        IReadOnlyList<EndToEndGizmoOption> available = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { bed.ThingID }, Array.Empty<string>());
        EndToEndGizmoOption[] matches = available
            .Where(option => option.RuntimeType == UnifiedCommandType)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "The eligible player humanlike bed must expose exactly one unified owner command.");
        EndToEndAssert.False(matches[0].Disabled,
            "The unified owner command must be enabled.");
        return matches[0];
    }

    private static bool IsGuestBed(Building_Bed bed) =>
        bed.GetType().FullName == GuestBedType;
}
