using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.industrial-dishwasher-local-presentation",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "Dubwise.DubsBadHygiene", "fumblesneeze.immersivechefs",
    MaxFrames = 12000, MaxGameTicks = 24000, MaxWallClockSeconds = 300)]
public sealed class IndustrialDishwasherLocalPresentationTest : IRimWorldEndToEndTest
{
    private readonly IndustrialDishwasherPresentationScenario scenario = new(false);
    public void Arrange(IEndToEndContext context) => scenario.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => scenario.Execute(context);
}

[RimWorldEndToEndTest(
    "immersive-chefs.industrial-dishwasher-processor-presentation",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "syrchalis.processor.framework", "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 12000, MaxGameTicks = 24000, MaxWallClockSeconds = 300)]
public sealed class IndustrialDishwasherProcessorPresentationTest : IRimWorldEndToEndTest
{
    private readonly IndustrialDishwasherPresentationScenario scenario = new(true);
    public void Arrange(IEndToEndContext context) => scenario.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => scenario.Execute(context);
}

[RimWorldEndToEndTest(
    "immersive-chefs.industrial-dishwasher-processor-variation-presentation",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VTEXVariations", "syrchalis.processor.framework", "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 18000, MaxGameTicks = 24000, MaxWallClockSeconds = 300)]
public sealed class IndustrialDishwasherProcessorVariationPresentationTest : IRimWorldEndToEndTest
{
    private readonly IndustrialDishwasherPresentationScenario scenario = new(true, variations: true);
    public void Arrange(IEndToEndContext context) => scenario.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => scenario.Execute(context);
}

internal sealed class IndustrialDishwasherPresentationScenario
{
    private readonly bool processor;
    private readonly bool variations;
    private PickUpAndHaulDishwasherFixture fixture = null!;
    private CompIndustrialDishwasherPresentation presentation = null!;
    private string selectedFamily = string.Empty;
    private ThingWithComps stove = null!;
    private ThingWithComps butcher = null!;
    private ThingWithComps mixedPlates = null!;
    private string[] initialPlateUnits = Array.Empty<string>();

    internal IndustrialDishwasherPresentationScenario(bool processor, bool variations = false)
    {
        this.processor = processor;
        this.variations = variations;
    }

    internal void Arrange(IEndToEndContext context)
    {
        var originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() => Find.ScreenshotModeHandler.Active = originalScreenshotMode);
        fixture = PickUpAndHaulDishwasherFixture.Create(context, processor,
            plateCount: 3,
            requirePickUpAndHaul: false, dishwasherDefName: "ImmersiveChefs_IndustrialDishwasher",
            hiddenConduits: true);
        mixedPlates = fixture.PhysicalWare.Single(thing =>
            thing.def.GetModExtension<KitchenwareExtension>()?.product == KitchenwareProduct.Plate);
        // Retain the fixture's original three-unit count and identity while arranging three
        // real materials. Native admission, washing and output must preserve this pile.
        mixedPlates.SplitOff(2).Destroy(DestroyMode.Vanish);
        foreach (var (stuff, hitPoints) in new[] { (ThingDefOf.Gold, 37), (ThingDefOf.WoodLog, 27) })
        {
            var unit = HandwashingE2EFixture.MakeDirtyPlate(stuff);
            unit.HitPoints = hitPoints;
            EndToEndAssert.True(mixedPlates.TryAbsorbStack(unit, true),
                "Supporting fixture must merge each actual material into the dirty plate pile.");
        }
        initialPlateUnits = PlateUnitSignatures();
        // Admit the pile first so all three visible slots must resolve its distinct units.
        foreach (var ware in fixture.PhysicalWare.Where(thing => thing != mixedPlates))
            ware.SetForbidden(true, false);
        // An ordinary supported setting gives the player time to flick power after three admissions.
        ImmersiveChefsMod.Settings.DishwashingWorkScale = 2f;
        presentation = fixture.Dishwasher.GetComp<CompIndustrialDishwasherPresentation>();
        EndToEndAssert.NotNull(presentation, "The industrial appliance must have its presentation comp.");
        selectedFamily = fixture.Dishwasher.Graphic.path;
        var map = fixture.Dishwasher.Map;
        stove = DispenserE2EFixture.SpawnBuilding(map, "ElectricStove",
            fixture.Dishwasher.Position + new IntVec3(-4, 0, 2));
        butcher = DispenserE2EFixture.SpawnBuilding(map, "TableButcher",
            fixture.Dishwasher.Position + new IntVec3(-4, 0, -4));
        context.DeferCleanup(() =>
        {
            if (!stove.Destroyed) stove.Destroy(DestroyMode.Vanish);
            if (!butcher.Destroyed) butcher.Destroy(DestroyMode.Vanish);
        });
        DispenserE2EFixture.SettlePower(map, new[] { stove, fixture.Dishwasher }, 400);
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Pause("pause before the industrial wash cycle");
        if (variations)
            foreach (var step in SelectOriginalCoreComparators(context)) yield return step;
        yield return new SupportingSceneTimeActionStep("set local noon for the wash-state evidence",
            "map-" + fixture.Dishwasher.Map.uniqueID, 720);
        yield return new CameraActionStep("frame the appliance and real dirty ware", fixture.VisibleThingIds, 160);
        yield return new SelectionActionStep("inspect the empty industrial dishwasher",
            new[] { fixture.Dishwasher.ThingID }, false);
        yield return Screenshot("raised hood and empty basket before native admission");
        foreach (var step in CaptureZooms(context, "empty before admission")) yield return step;
        if (variations)
        {
            foreach (var step in SelectFamily(context, "before native admission",
                         fixture.Dishwasher.def.graphicData.texPath + "_Variant01")) yield return step;
            yield return Screenshot("selected alternate finish immediately before native admission");
        }
        yield return new AssertionStep("enable ordinary dishwasher work", _ => ActivateWork());
        yield return Run("let the colonist admit the mixed steel gold and wood plate pile first");
        yield return Wait("the mixed plate pile arrives through a real dishwasher job", () =>
            fixture.HeldDishwasherWare.Any(thing => ReferenceEquals(thing, mixedPlates)));
        yield return Pause("pause after native mixed-pile admission");
        yield return new AssertionStep("release the other dirty ware after the mixed pile is held", _ =>
        {
            AssertMixedPlateConservation();
            foreach (var ware in fixture.PhysicalWare.Where(thing => thing != mixedPlates))
                ware.SetForbidden(false, false);
        });
        yield return Run("let the colonist carry the remaining dirty ware to the appliance");
        yield return Wait("all three exact stacks arrive through real dishwasher jobs", fixture.AllWareOwnedByDishwasher);
        yield return Pause("pause after the last native admission");
        yield return new AssertionStep("suspend further hauling while examining the held load", _ => fixture.DisableCleanerWork());
        yield return new CheckpointStep("observe actual presentation inputs after native admission", _ =>
        {
            var visible = new List<Thing>();
            var sampledState = fixture.Dishwasher.GetComp<CompDishwasher>().ReadPresentation(visible);
            return new Dictionary<string, string>
            {
                ["expectedFamily"] = selectedFamily,
                ["actualFamily"] = fixture.Dishwasher.Graphic.path,
                ["displayPath"] = presentation.DisplayGraphic?.path ?? string.Empty,
                ["cachedState"] = presentation.State.ToString(),
                ["sampledState"] = sampledState.ToString(),
                ["observedHeldWare"] = string.Join(",", fixture.HeldDishwasherWare.Select(thing =>
                    thing.ThingID + ":" + thing.stackCount + ":" + thing.holdingOwner?.GetType().FullName)),
                ["progressByWare"] = processor ? string.Join(",", fixture.PhysicalWare.Select(thing =>
                    thing.ThingID + ":" + ProcessorFrameworkAdapter.ProgressPercent(fixture.Dishwasher, thing)
                        .ToString("R", CultureInfo.InvariantCulture))) : "local",
                ["powerOn"] = fixture.Dishwasher.GetComp<CompPowerTrader>().PowerOn.ToString(),
                ["switchOn"] = fixture.Dishwasher.GetComp<CompFlickable>().SwitchIsOn.ToString(),
                ["broken"] = (fixture.Dishwasher.TryGetComp<CompBreakdownable>()?.BrokenDown ?? false).ToString(),
                ["suppliedConnection"] = DubsWaterAdapter.HasSuppliedConnection(fixture.Dishwasher).ToString(),
                ["networkWater"] = HandwashingE2EFixture.ReadDubsNetworkWater(fixture.Dishwasher)
                    .ToString("R", CultureInfo.InvariantCulture)
            };
        });
        yield return Screenshot("native admitted load before family assertion");
        yield return new AssertionStep("native admission preserves the selected cosmetic family", _ =>
            EndToEndAssert.Equal(selectedFamily, fixture.Dishwasher.Graphic.path,
                "Native admission must retain the player's selected cosmetic family."));
        yield return Wait("the loaded powered appliance displays its closed family", () =>
            presentation.State == DishwasherPresentationState.Washing &&
            presentation.DisplayGraphic?.path == selectedFamily + "_Closed");
        yield return Screenshot("real admitted loads close the hood");
        foreach (var step in CaptureZooms(context, "washing after native admission")) yield return step;
        if (processor)
            yield return new AssertionStep("only the industrial contained-ware renderer owns product appearance", _ =>
            {
                var comp = fixture.Dishwasher.AllComps.Single(candidate =>
                    candidate.GetType().FullName == "ProcessorFramework.CompProcessor");
                var field = comp.props.GetType().GetField("showProductIcon");
                EndToEndAssert.NotNull(field, "The supported per-building Processor icon option must exist.");
                var globalIcons = comp.GetType().Assembly.GetType("ProcessorFramework.PF_Settings")
                    ?.GetField("showProcessIconGlobal");
                EndToEndAssert.NotNull(globalIcons, "The live global Processor icon setting must be observable.");
                EndToEndAssert.Equal(true, (bool)globalIcons!.GetValue(null),
                    "Global product icons must remain enabled while only the industrial appliance suppresses them.");
                EndToEndAssert.Equal(false, (bool)field!.GetValue(comp.props),
                    "Generic process icons must not draw over the industrial hood or its actual material-aware contents.");
                var domestic = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher").comps
                    .Single(properties => properties.GetType() == comp.props.GetType());
                EndToEndAssert.Equal(true, (bool)field.GetValue(domestic),
                    "The domestic appliance must retain its existing Processor icon setting.");
            });

        foreach (var step in Flick(context, on: false)) yield return step;
        yield return Wait("the unpowered held load raises the hood", () =>
            presentation.State == DishwasherPresentationState.OpenLoaded &&
            presentation.DisplayGraphic?.path == selectedFamily);
        yield return new CheckpointStep("actual mixed-pile slot materials after native power off", _ =>
            ReadMixedSlotEvidence());
        yield return Screenshot("native power-off exposes only the exact held ware");
        foreach (var step in CaptureZooms(context, "held ware after native power off")) yield return step;
        yield return new AssertionStep("paused presentation retains actual dirty items", _ =>
        {
            EndToEndAssert.True(fixture.AllWareOwnedByDishwasher(), "The appliance retains all original stack identities.");
            EndToEndAssert.Equal(5, fixture.HeldDishwasherWare.Sum(thing => thing.stackCount),
                "The held mixed pile, cutlery and cookware must total five physical units.");
            EndToEndAssert.True(fixture.PhysicalWare.All(thing => thing.GetComp<CompSanitation>().IsDirty),
                "Power-off must pause the actual dirty load.");
            AssertMixedPlateConservation();
            var visible = new List<Thing>();
            var state = fixture.Dishwasher.GetComp<CompDishwasher>().ReadPresentation(visible);
            EndToEndAssert.Equal(DishwasherPresentationState.OpenLoaded, state, "Power-off must retain the open loaded state.");
            EndToEndAssert.True(visible.Count == 3 && visible.All(thing => ReferenceEquals(thing, mixedPlates)),
                "All three slots must display individual units from the same actual mixed pile.");
        });
        foreach (var step in Flick(context, on: true)) yield return step;
        yield return Wait("restored power closes the same washing load", () =>
            presentation.State == DishwasherPresentationState.Washing &&
            presentation.DisplayGraphic?.path == selectedFamily + "_Closed");
        yield return Screenshot("native power restoration lowers the same hood");
        foreach (var step in CaptureZooms(context, "washing after native power restoration")) yield return step;

        if (processor)
        {
            yield return Run("allow the real Processor loads to finish with output work disabled");
            yield return Wait("all actual Processor loads complete naturally", fixture.AllProcessorOutputsNaturallyComplete);
            yield return Pause("pause before completed Processor output is hauled");
            yield return Wait("completed held contents open the hood", () => presentation.State == DishwasherPresentationState.OpenLoaded);
            yield return new CheckpointStep("completed mixed-pile slot materials before native output hauling", _ =>
                ReadMixedSlotEvidence());
            yield return Screenshot("completed held Processor output remains visible under the raised hood");
            foreach (var step in CaptureZooms(context, "completed held Processor output")) yield return step;
        }
        if (variations)
        {
            foreach (var step in SelectFamily(context, "before native clean storage",
                         fixture.Dishwasher.def.graphicData.texPath + "_Variant01")) yield return step;
            yield return Screenshot("selected alternate finish immediately before native clean storage");
        }
        yield return new AssertionStep("allow ordinary clean storage and dishwasher work", _ =>
        {
            fixture.EnableCleanStorage();
            ActivateWork();
        });
        yield return Run("finish washing and haul the real clean ware to storage");
        yield return Wait("every exact item reaches clean storage", fixture.ExactWareStoredClean);
        yield return Pause("pause after native clean output storage");
        yield return Screenshot("actual finish after native clean storage before family assertion");
        yield return new AssertionStep("native emptying preserves the selected cosmetic family", _ =>
            EndToEndAssert.Equal(selectedFamily, fixture.Dishwasher.Graphic.path,
                "Native emptying must retain the player's selected cosmetic family."));
        yield return Wait("the emptied appliance displays the raised empty state", () =>
            presentation.State == DishwasherPresentationState.Empty && presentation.DisplayGraphic?.path == selectedFamily);
        yield return Screenshot("clean stored ware and the now-empty raised dishwasher");
        foreach (var step in CaptureZooms(context, "empty after native clean storage")) yield return step;
        yield return new AssertionStep("the full player workflow conserves each physical item and mixed material", _ =>
        {
            fixture.AssertUntrackedOutputConservation();
            AssertMixedPlateConservation();
        });
        yield return new SelectionActionStep("inspect the exact mixed plate pile after native washing and hauling",
            new[] { mixedPlates.ThingID }, false);
        yield return new CameraActionStep("frame stored clean mixed plates and industrial dishwasher",
            new[] { mixedPlates.ThingID, fixture.Dishwasher.ThingID }, 220);
        yield return Screenshot("clean stored mixed plates retain all three actual materials and original durability");
    }

    private string[] PlateUnitSignatures() => mixedPlates.GetComp<CompTablewareStack>().Units
        .Select(unit => unit.MaterialDefName + ":" + unit.HitPoints).ToArray();

    private void AssertMixedPlateConservation()
    {
        EndToEndAssert.True(!mixedPlates.Destroyed && mixedPlates.stackCount == 3 &&
            PlateUnitSignatures().SequenceEqual(initialPlateUnits),
            "The same physical mixed pile must preserve its steel, gold and wood units with exact original HP.");
    }

    private Dictionary<string, string> ReadMixedSlotEvidence()
    {
        AssertMixedPlateConservation();
        var visible = new List<Thing>();
        var state = fixture.Dishwasher.GetComp<CompDishwasher>().ReadPresentation(visible);
        EndToEndAssert.Equal(DishwasherPresentationState.OpenLoaded, state, "Material evidence requires the real open load.");
        EndToEndAssert.True(visible.Count == 3 && visible.All(thing => ReferenceEquals(thing, mixedPlates)),
            "The renderer's three sampled slots must all belong to the admitted mixed pile.");
        var pile = mixedPlates.GetComp<CompTablewareStack>();
        var units = Enumerable.Range(0, 3).Select(index => pile.UnitView(index)).ToArray();
        var materials = units.Select(unit => unit.Graphic.MatSingleFor(unit)).ToArray();
        EndToEndAssert.True(units.Select(unit => unit.Stuff.defName).SequenceEqual(new[] { "Steel", "Gold", "WoodLog" }),
            "Slot order must resolve the actual steel, gold and wooden units.");
        EndToEndAssert.Equal(3, materials.Select(material => material.color).Distinct().Count(),
            "Actual graphics materials must retain three distinct Stuff colors.");
        return new Dictionary<string, string>
        {
            ["mixedPileThingId"] = mixedPlates.ThingID,
            ["unitLedger"] = string.Join(",", PlateUnitSignatures()),
            ["slotStuff"] = string.Join(",", units.Select(unit => unit.Stuff.defName)),
            ["slotTextures"] = string.Join(",", materials.Select(material => material.mainTexture.name)),
            ["slotColors"] = string.Join(";", materials.Select(material => material.color.ToString())),
            ["dirty"] = mixedPlates.GetComp<CompSanitation>().IsDirty.ToString(),
            ["processingOwner"] = processor ? "ProcessorFramework" : "ImmersiveChefs"
        };
    }

    private IEnumerable<EndToEndStep> SelectOriginalCoreComparators(IEndToEndContext context)
    {
        foreach (var (thing, corePath) in new[]
                 {
                     (stove, "Things/Building/Production/TableStoveElectric"),
                     (butcher, "Things/Building/Production/TableButcher")
                 })
        {
            for (var attempt = 0; attempt < 64 && thing.Graphic.path != corePath; attempt++)
                yield return ChangeGraphic(context, thing, "select original Core comparator " + attempt);
            EndToEndAssert.Equal(corePath, thing.Graphic.path, "Comparators must retain original Core art.");
        }
    }

    private IEnumerable<EndToEndStep> CaptureZooms(IEndToEndContext context, string phase)
    {
        for (var finish = 0; finish < (variations ? 2 : 1); finish++)
        {
            var label = phase + (variations ? " family " + selectedFamily : string.Empty);
            foreach (var step in CaptureCurrentZooms(label)) yield return step;
            if (variations && (phase == "empty before admission" || phase == "washing after native admission"))
            {
                foreach (var ratio in new[] { .55f, .15f })
                {
                    yield return new SupportingHitPointFixtureActionStep("arrange damage " + label + " " + ratio,
                        new[] { new EndToEndHitPointFixture(fixture.Dishwasher.ThingID, ratio) });
                    foreach (var step in CaptureCurrentZooms(label + " damage " + ratio)) yield return step;
                }
                yield return new AssertionStep("restore supporting health fixture " + label, _ =>
                {
                    fixture.Dishwasher.HitPoints = fixture.Dishwasher.MaxHitPoints;
                    fixture.Dishwasher.DirtyMapMesh(fixture.Dishwasher.Map);
                    EndToEndAssert.Equal(fixture.Dishwasher.MaxHitPoints, fixture.Dishwasher.HitPoints,
                        "The disposable damage fixture must be restored before the next finish or native job.");
                });
            }
            if (!variations || finish != 0) continue;
            var targetFamily = selectedFamily == fixture.Dishwasher.def.graphicData.texPath
                ? fixture.Dishwasher.def.graphicData.texPath + "_Variant01"
                : fixture.Dishwasher.def.graphicData.texPath;
            foreach (var step in SelectFamily(context, phase, targetFamily)) yield return step;
        }
    }

    private IEnumerable<EndToEndStep> SelectFamily(IEndToEndContext context, string phase, string targetFamily)
    {
        var expectedState = presentation.State;
        var heldBefore = fixture.HeldDishwasherWare.OrderBy(thing => thing.ThingID).ToArray();
        var wareBefore = fixture.PhysicalWare.Select(thing =>
            (Thing: thing, Owner: thing.holdingOwner, Count: thing.stackCount,
             Spawned: thing.Spawned, Cell: thing.Position)).ToArray();
        for (var attempt = 0; attempt < 16 && fixture.Dishwasher.Graphic.path != targetFamily; attempt++)
        {
            yield return ChangeGraphic(context, fixture.Dishwasher, "native alternate finish " + phase + " " + attempt);
            yield return new CheckpointStep("actual ware unchanged after cosmetic command " + phase + " " + attempt, _ =>
            {
                var heldAfter = fixture.HeldDishwasherWare.OrderBy(thing => thing.ThingID).ToArray();
                EndToEndAssert.True(heldAfter.SequenceEqual(heldBefore),
                    "A cosmetic command must retain the exact actual holder contents, including empty states.");
                EndToEndAssert.True(wareBefore.All(item => !item.Thing.Destroyed &&
                    ReferenceEquals(item.Thing.holdingOwner, item.Owner) && item.Thing.stackCount == item.Count &&
                    item.Thing.Spawned == item.Spawned && item.Thing.Position == item.Cell),
                    "Every physical fixture must retain its actual owner, stack count and map position.");
                return new Dictionary<string, string>
                {
                    ["observedHeldWareIds"] = string.Join(",", heldAfter.Select(thing => thing.ThingID)),
                    ["observedPhysicalCounts"] = string.Join(",", fixture.PhysicalWare.Select(thing =>
                        thing.ThingID + ":" + thing.stackCount)),
                    ["selectedFamily"] = fixture.Dishwasher.Graphic.path
                };
            });
        }
        selectedFamily = fixture.Dishwasher.Graphic.path;
        EndToEndAssert.Equal(targetFamily, selectedFamily,
            "The native cosmetic command must select the exact requested finish.");
        yield return Wait("observe alternate finish with unchanged real contents " + phase, () =>
            presentation.State == expectedState && presentation.DisplayGraphic?.path ==
            selectedFamily + (expectedState == DishwasherPresentationState.Washing ? "_Closed" : string.Empty));
    }

    private static EndToEndStep ChangeGraphic(IEndToEndContext context, ThingWithComps thing, string label)
    {
        var change = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { thing.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Invoke &&
                option.Label == "VFE_ChangeGraphic".Translate().ToString());
        return new GizmoActionStep(label, new[] { thing.ThingID }, change.RuntimeType,
            EndToEndGizmoInteraction.Invoke, stableGizmoId: change.StableId,
            architectCategoryDefNames: Array.Empty<string>());
    }

    private IEnumerable<EndToEndStep> CaptureCurrentZooms(string phase)
    {
        var things = new[] { fixture.Dishwasher, stove, butcher };
        yield return new SupportingSceneTimeActionStep("set comparison noon " + phase,
            "map-" + fixture.Dishwasher.Map.uniqueID, 720);
        yield return new SelectionActionStep("clear visual comparison selection " + phase,
            Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide interface for comparison " + phase, true);
        var previousRootSize = 0f;
        var previousOrthographicSize = 0f;
        foreach (var (zoom, desiredRootSize) in new[] { ("close", 11f), ("ordinary", 16f), ("far", 24f) })
        {
            var rectangles = things.Select(thing => thing.OccupiedRect()).ToArray();
            var height = rectangles.Max(rectangle => rectangle.maxZ) -
                         rectangles.Min(rectangle => rectangle.minZ) + 1;
            var padding = zoom == "close" ? 100 :
                (int)Math.Round(UnityEngine.Screen.height * (1f - height / (2f * desiredRootSize)) / 2f);
            yield return new CameraActionStep("frame " + zoom + " " + phase,
                things.Select(thing => thing.ThingID).ToArray(), padding);
            yield return Screenshot(zoom + " comparison " + phase);
            yield return new CheckpointStep(zoom + " comparison identity " + phase, _ =>
            {
                EndToEndAssert.True(Math.Abs(Find.CameraDriver.RootSize - desiredRootSize) <= .5f,
                    "Comparison camera must reach its declared useful map zoom.");
                if (previousRootSize > 0f)
                    EndToEndAssert.True(Find.CameraDriver.RootSize >= previousRootSize * 1.3f &&
                        Find.Camera.orthographicSize >= previousOrthographicSize * 1.3f,
                        "Successive comparisons must change both actual camera scales by at least 1.3.");
                previousRootSize = Find.CameraDriver.RootSize;
                previousOrthographicSize = Find.Camera.orthographicSize;
                EndToEndAssert.True(things.All(thing => thing.Rotation == Rot4.North),
                    "All comparison workstations must share the measured North orientation.");
                EndToEndAssert.Equal("Things/Building/Production/TableStoveElectric", stove.Graphic.path,
                    "The stove must retain original Core art.");
                EndToEndAssert.Equal("Things/Building/Production/TableButcher", butcher.Graphic.path,
                    "The butcher table must retain original Core art.");
                EndToEndAssert.Equal(ThingDefOf.Steel, butcher.Stuff, "The butcher reference uses actual Core Steel.");
                EndToEndAssert.True(stove.GetComp<CompPowerTrader>().PowerOn,
                    "Native power must suppress the stove's needs-power overlay.");
                return new Dictionary<string, string>
                {
                    ["phase"] = phase,
                    ["state"] = presentation.State.ToString(),
                    ["graphicPath"] = presentation.DisplayGraphic?.path ?? string.Empty,
                    ["selectedFamily"] = fixture.Dishwasher.Graphic.path,
                    ["hitPoints"] = fixture.Dishwasher.HitPoints.ToString(CultureInfo.InvariantCulture),
                    ["maxHitPoints"] = fixture.Dishwasher.MaxHitPoints.ToString(CultureInfo.InvariantCulture),
                    ["dishwasherId"] = fixture.Dishwasher.ThingID,
                    ["stoveId"] = stove.ThingID,
                    ["butcherId"] = butcher.ThingID,
                    ["expectedFixtureWareIds"] = string.Join(",", fixture.WareIds),
                    ["observedHeldWareIds"] = string.Join(",", fixture.HeldDishwasherWare.Select(thing => thing.ThingID)),
                    ["rootSize"] = Find.CameraDriver.RootSize.ToString(CultureInfo.InvariantCulture),
                    ["orthographicSize"] = Find.Camera.orthographicSize.ToString(CultureInfo.InvariantCulture),
                    ["localHour"] = GenLocalDate.HourOfDay(fixture.Dishwasher.Map).ToString(CultureInfo.InvariantCulture)
                };
            });
        }
        yield return new ScreenshotModeActionStep("restore interface " + phase, false);
        yield return new CameraActionStep("restore workflow framing " + phase,
            fixture.NativeTransferVisibleThingIds, 160);
        yield return new SelectionActionStep("restore appliance inspection " + phase,
            new[] { fixture.Dishwasher.ThingID }, false);
    }

    private IEnumerable<EndToEndStep> Flick(IEndToEndContext context, bool on)
    {
        var appliance = fixture.Dishwasher;
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { appliance.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == !on && option.HotKeyDefName == "Command_TogglePower");
        yield return new GizmoActionStep(on ? "designate power on" : "designate power off",
            new[] { appliance.ThingID }, toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() is not null)
            yield return new WindowAcceptActionStep("acknowledge the native flick designation tutorial", "Verse.Dialog_MessageBox");
        yield return new AssertionStep("enable the colonist's ordinary basic work", _ =>
            fixture.Cleaner.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("BasicWorker"), 1));
        var order = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(fixture.Cleaner.ThingID, appliance.ThingID)
            .Single(option => !option.Disabled && option.Label.IndexOf("flick", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new FloatMenuActionStep("prioritize the native flick job", fixture.Cleaner.ThingID, appliance.ThingID, order.StableId);
        yield return Run("let the colonist operate the real power switch");
        yield return Wait("the colonist completes the flick and the power network settles", () =>
            appliance.GetComp<CompFlickable>().SwitchIsOn == on &&
            appliance.GetComp<CompPowerTrader>().PowerOn == on &&
            appliance.Map.designationManager.DesignationOn(appliance, DefDatabase<DesignationDef>.GetNamed("Flick")) is null);
        yield return Pause("pause after the physical switch operation");
        yield return new AssertionStep("suspend the operator after flicking", _ => fixture.DisableCleanerWork());
    }

    private void ActivateWork()
    {
        if (processor) fixture.ActivateProcessorHaulingOnly();
        else
        {
            fixture.ActivateCleaning();
            fixture.Cleaner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        }
    }

    private EndToEndStep Screenshot(string name) => new ScreenshotStep(name, Array.Empty<string>(), 0);
    private static EndToEndStep Pause(string name) => new TimeControlActionStep(name, true, EndToEndGameSpeed.Normal);
    private static EndToEndStep Run(string name) => new TimeControlActionStep(name, false, EndToEndGameSpeed.Superfast);
    private static EndToEndStep Wait(string name, Func<bool> predicate) => new WaitUntilStep(name, _ => predicate(),
        new EndToEndDeadline(3600, 12000, TimeSpan.FromSeconds(90)));
}
