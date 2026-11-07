using RimWorldDevGateway.EndToEndTesting;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.large-map", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 200000, MaxGameTicks = 400000, MaxWallClockSeconds = 2400)]
public sealed class LargeMapNativeMovement : NativeRouteComparison, IEndToEndGeneratedMapFixture
{
    protected override bool LargeMap => true;
}
