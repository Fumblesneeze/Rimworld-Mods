using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.domestic-dishwasher-contents-hidden",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "syrchalis.processor.framework", "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs", MaxFrames = 6000, MaxGameTicks = 16000, MaxWallClockSeconds = 180)]
public sealed class DomesticDishwasherContentsHiddenTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true,
            requirePickUpAndHaul: false, hiddenConduits: true);
        ImmersiveChefsMod.Settings.DishwashingWorkScale = 2f;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before domestic admission", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame the empty domestic appliance", fixture.VisibleThingIds, 220);
        yield return new ScreenshotStep("domestic appliance before native admission", Array.Empty<string>(), 0);
        yield return new AssertionStep("enable ordinary appliance loading", _ => fixture.ActivateProcessorHaulingOnly());
        yield return new TimeControlActionStep("native jobs load domestic dishes", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("all real dishes enter the closed appliance", _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(2400, 10000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep("pause with the actual held load", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("closed domestic appliance suppresses only its own product icon", _ =>
        {
            fixture.DisableCleanerWork();
            var processor = fixture.Dishwasher.AllComps.Single(comp => comp.GetType().FullName == "ProcessorFramework.CompProcessor");
            EndToEndAssert.Equal(false, (bool)processor.props.GetType().GetField("showProductIcon")!.GetValue(processor.props),
                "A closed domestic appliance must not show held dishes on its casing.");
            var global = processor.GetType().Assembly.GetType("ProcessorFramework.PF_Settings")!
                .GetField("showProcessIconGlobal")!;
            EndToEndAssert.Equal(true, (bool)global.GetValue(null), "Other Processor buildings retain global icons.");
        });
        yield return new SelectionActionStep("inspect the actual domestic held load", new[] { fixture.Dishwasher.ThingID }, false);
        yield return new ScreenshotStep("native-loaded domestic appliance has no floating product icon", Array.Empty<string>(), 0);
        foreach (var step in DishwasherOcclusionViews.Capture(context, fixture.Dishwasher, "domestic loaded")) yield return step;
        yield return new AssertionStep("viewing the casing preserves its actual contents", _ => fixture.AssertLoadedConservation());
    }
}

internal static class DishwasherOcclusionViews
{
    internal static IEnumerable<EndToEndStep> Capture(IEndToEndContext context, ThingWithComps appliance, string phase)
    {
        var originalRotation = appliance.Rotation;
        var originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() =>
        {
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
            if (!appliance.Destroyed && appliance.Spawned)
            {
                appliance.Rotation = originalRotation;
                appliance.DirtyMapMesh(appliance.Map);
            }
        });
        yield return new SupportingSceneTimeActionStep("set noon " + phase, "map-" + appliance.Map.uniqueID, 720);
        yield return new SelectionActionStep("clear selection " + phase, Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide interface " + phase, true);
        for (var rotation = 0; rotation < 4; rotation++)
        {
            var facing = new Rot4(rotation);
            var settleAt = 0f;
            // Only this supporting view changes direction. Native jobs establish held contents
            // and operating state; neither is fabricated for these directional captures.
            yield return new AssertionStep("supporting cardinal view " + phase + " " + facing, _ =>
            {
                appliance.Rotation = facing;
                appliance.DirtyMapMesh(appliance.Map);
                settleAt = Time.realtimeSinceStartup + .35f;
            });
            yield return new WaitUntilStep("settle printed hood " + phase + " " + facing,
                _ => Time.realtimeSinceStartup >= settleAt,
                new EndToEndDeadline(1200, 1000, TimeSpan.FromSeconds(10)));
            foreach (var zoom in new[] { 11f, 16f, 24f })
            {
                var padding = (int)Math.Round(Screen.height * (1f - appliance.OccupiedRect().Height / (2f * zoom)) / 2f);
                yield return new CameraActionStep("frame " + phase + " " + facing + " " + zoom,
                    new[] { appliance.ThingID }, padding);
                yield return new ScreenshotStep(phase + " " + facing + " zoom " + zoom, Array.Empty<string>(), 0);
            }
        }
        yield return new AssertionStep("restore original direction " + phase, _ =>
        {
            appliance.Rotation = originalRotation;
            appliance.DirtyMapMesh(appliance.Map);
        });
        yield return new ScreenshotModeActionStep("restore interface " + phase, originalScreenshotMode);
    }
}
