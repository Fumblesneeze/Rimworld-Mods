using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace GuestBedGizmo.EndToEndTests;

[RimWorldEndToEndTest("guest-bed-gizmo.hospitality-absent", "fumblesneeze.guestbedgizmo",
    "brrainz.harmony", "ludeon.rimworld", "ludeon.rimworld.ideology", "fumblesneeze.guestbedgizmo",
    MaxFrames = 1200, MaxGameTicks = 1200, MaxWallClockSeconds = 90)]
public sealed class HospitalityAbsentWorkflowTests : GuestBedAbsenceWorkflow { }

[RimWorldEndToEndTest("guest-bed-gizmo.ideology-absent", "fumblesneeze.guestbedgizmo",
    "brrainz.harmony", "ludeon.rimworld", "orion.hospitality", "fumblesneeze.guestbedgizmo",
    MaxFrames = 1200, MaxGameTicks = 1200, MaxWallClockSeconds = 90)]
public sealed class IdeologyAbsentWorkflowTests : GuestBedAbsenceWorkflow { }

public abstract class GuestBedAbsenceWorkflow : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 cell;
    private Building_Bed bed = null!;
    private readonly List<Thing> walls = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        cell = map.AllCells.Where(candidate => CellRect.CenteredOn(candidate, 3).Cells.All(part =>
                part.InBounds(map) && part.Standable(map) && !part.Fogged(map) && part.GetThingList(map).Count == 0))
            .OrderBy(candidate => candidate.DistanceToSquared(map.Center)).First();
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in map.thingGrid.ThingsListAt(cell).OfType<Building_Bed>().Cast<Thing>()
                         .Concat(walls).ToArray())
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        });
        foreach (IntVec3 position in CellRect.CenteredOn(cell, 3).EdgeCells)
        {
            Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
            wall.SetFactionDirect(Faction.OfPlayer);
            walls.Add(GenSpawn.Spawn(wall, position, map));
        }
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
        bed = (Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Bed"), ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, cell, map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep("select the ordinary bed with a prerequisite absent", new[] { bed.ThingID }, false);
        yield return new CameraActionStep("frame the unchanged native bed controls", new[] { bed.ThingID }, 260);
        yield return new AssertionStep("the product remains inactive without both prerequisites", _ =>
        {
            EndToEndAssert.False(GuestBedGizmoMod.IntegrationActive, "No partial product integration may be active.");
            EndToEndAssert.Equal("prerequisites_absent", GuestBedGizmoMod.IntegrationStatus, "Absence must be explicit.");
            EndToEndAssert.False(bed.GetGizmos().Any(gizmo => gizmo.GetType().FullName ==
                "GuestBedGizmo.Beds.Command_GuestAwareBedOwnerType"), "Native controls must not be replaced.");
        });
        yield return new ScreenshotStep("native bed controls before the action", Array.Empty<string>(), 0);
        var commands = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { bed.ThingID }, Array.Empty<string>());
        if (ModsConfig.IdeologyActive)
        {
            var command = commands.Single(option => option.RuntimeType == "Verse.Command_SetBedOwnerType");
            EndToEndAssert.Equal<EndToEndGizmoInteraction?>(EndToEndGizmoInteraction.Invoke, command.Interaction, "Vanilla ownership must expose the native action capability.");
            yield return new GizmoActionStep("open vanilla's owner menu without Hospitality", new[] { bed.ThingID },
                command.RuntimeType, EndToEndGizmoInteraction.Invoke, stableGizmoId: command.StableId);
            yield return new AssertionStep("vanilla still exposes its three owner choices", _ =>
            {
                var menu = Find.WindowStack.Windows.OfType<FloatMenu>().Single();
                var options = (List<FloatMenuOption>)typeof(FloatMenu).GetField("options",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(menu)!;
                var expected = new[] { "CommandBedSetForColonistsLabel", "CommandBedSetForPrisonersLabel", "CommandBedSetForSlavesLabel" }
                    .Select(key => key.Translate().ToString());
                EndToEndAssert.True(expected.SequenceEqual(options.Select(option => option.Label)), "Only the native three choices may appear.");
            });
            yield return new ScreenshotStep("vanilla three-choice owner menu", Array.Empty<string>(), 0);
            yield return new CurrentFloatMenuActionStep("choose prisoners through vanilla's menu", "CommandBedSetForPrisonersLabel".Translate());
            yield return new AssertionStep("vanilla ownership changes without replacing the bed", _ =>
                EndToEndAssert.True(!bed.Destroyed && bed.ForPrisoners, "The same bed must become a prisoner bed."));
        }
        else
        {
            var command = commands.Single(option => option.RuntimeType == "Verse.Command_Toggle" &&
                option.Label == "CommandBedSetAsGuestLabel".Translate().ToString());
            EndToEndAssert.Equal<EndToEndGizmoInteraction?>(EndToEndGizmoInteraction.Toggle, command.Interaction, "Hospitality keeps its native toggle capability.");
            yield return new GizmoActionStep("use Hospitality's unchanged standalone guest toggle", new[] { bed.ThingID },
                command.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: command.StableId);
            yield return new AssertionStep("Hospitality's native swap still works without Ideology", _ =>
                EndToEndAssert.True(bed.Destroyed && CurrentBed().GetType().FullName == "Hospitality.Building_GuestBed",
                    "Hospitality must own the normal guest-bed replacement."));
            yield return new SelectionActionStep("select the upstream guest-bed replacement", new[] { CurrentBed().ThingID }, false);
        }
        yield return new ScreenshotStep("native bed action outcome with the patch inactive", Array.Empty<string>(), 0);
    }

    private Building_Bed CurrentBed() => map.thingGrid.ThingsListAt(cell).OfType<Building_Bed>().Single();
}
