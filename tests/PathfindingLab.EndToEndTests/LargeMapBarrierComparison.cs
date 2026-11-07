using RimWorldDevGateway.EndToEndTesting;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.large-map-barrier", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 200000, MaxGameTicks = 400000, MaxWallClockSeconds = 2400)]
public sealed class LargeMapBarrierComparison : NativeRouteComparison, IEndToEndGeneratedMapFixture
{
    protected override bool LargeMap => true;
    protected override bool Barrier => true;
}
