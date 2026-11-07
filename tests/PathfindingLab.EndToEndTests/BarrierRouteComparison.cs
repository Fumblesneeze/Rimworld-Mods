using RimWorldDevGateway.EndToEndTesting;

namespace PathfindingLab.EndToEndTests;

[RimWorldEndToEndTest("pathfinding-lab.barrier-route-comparison", "fumblesneeze.pathfindinglab",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.pathfindinglab",
    MaxFrames = 60000, MaxGameTicks = 60000, MaxWallClockSeconds = 600)]
public sealed class BarrierRouteComparison : NativeRouteComparison
{
    protected override bool Barrier => true;
}
