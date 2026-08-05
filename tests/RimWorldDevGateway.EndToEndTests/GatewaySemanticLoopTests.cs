using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest(
    "gateway.semantic-loop.first",
    EndToEndTestContract.GatewayPackageId,
    EndToEndTestContract.CorePackageId,
    MaxFrames = 900,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 45)]
public sealed class FirstSemanticLoopTest : IRimWorldEndToEndTest
{
    private string thingId = string.Empty;
    private int startTick;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var thing = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(thing, map.Center, map);
        thingId = thing.ThingID;
        startTick = Find.TickManager.TicksGame;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep("select spawned wood", new[] { thingId }, additive: false);
        yield return new CameraActionStep("frame spawned wood", new[] { thingId }, paddingPixels: 64);
        yield return new AssertionStep(
            "selection is observable",
            _ => EndToEndAssert.True(
                Find.Selector.SelectedObjectsListForReading.OfType<Thing>()
                    .Any(thing => thing.ThingID == thingId),
                "The native selector must contain the arranged Thing."));
        yield return new ScreenshotStep("selected wood", new[] { thingId }, paddingPixels: 64);
        yield return new TimeControlActionStep("run normal game time", paused: false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary ticks advance",
            _ => Find.TickManager.TicksGame >= startTick + 3,
            new EndToEndDeadline(300, 300, System.TimeSpan.FromSeconds(15)));
        yield return new TimeControlActionStep("pause after observation", paused: true, EndToEndGameSpeed.Normal);
    }
}

[RimWorldEndToEndTest(
    "gateway.semantic-loop.second",
    EndToEndTestContract.GatewayPackageId,
    EndToEndTestContract.CorePackageId,
    MaxFrames = 600,
    MaxGameTicks = 1_000,
    MaxWallClockSeconds = 30)]
public sealed class SecondSemanticLoopTest : IRimWorldEndToEndTest
{
    private string thingId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        EndToEndAssert.Equal(
            0,
            map.listerThings.AllThings.Count(thing =>
                thing.Spawned && !thing.Destroyed && thing.def.destroyable),
            "verified sequential empty disposable-map baseline");
        var thing = ThingMaker.MakeThing(ThingDefOf.Steel);
        GenSpawn.Spawn(thing, map.Center, map);
        thingId = thing.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep("select spawned steel", new[] { thingId }, additive: false);
        yield return new AssertionStep(
            "second fixture is observable",
            _ => EndToEndAssert.True(
                Current.Game.CurrentMap.listerThings.AllThings.Any(thing => thing.ThingID == thingId),
                "The second arranged Thing must remain spawned during its observation."));
        yield return new ScreenshotStep("selected steel", new[] { thingId }, paddingPixels: 64);
    }
}
