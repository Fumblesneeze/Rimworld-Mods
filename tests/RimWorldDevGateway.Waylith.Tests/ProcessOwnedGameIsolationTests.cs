using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

public sealed class ProcessOwnedGameIsolationTests
{
    [Test]
    public void Replacement_world_camera_restores_prior_coordinates_within_new_map_bounds()
    {
        var original = new GatewayCameraSnapshot("map-0", new GatewayMapCell(125, 150), 30, "old",
            new GatewayMapRect(0, 0, 250, 250), 250, 250, 10, 60);
        var current = new GatewayCameraSnapshot("map-9", new GatewayMapCell(10, 10), 20, "new",
            new GatewayMapRect(0, 0, 100, 80), 100, 80, 10, 25);
        var request = VerseGatewayEndToEndIsolationOperations.CameraRestoreRequest(original, current, true);
        Assert.That(request.MapHandle, Is.EqualTo("map-9"));
        Assert.That(request.Center!.X, Is.EqualTo(99));
        Assert.That(request.Center.Z, Is.EqualTo(79));
        Assert.That(request.RootSize, Is.EqualTo(25));
        Assert.That(VerseGatewayEndToEndIsolationOperations.CameraRestoreRequest(original, current, false).MapHandle,
            Is.EqualTo("map-0"), "Ordinary isolation must still reject a replaced map.");
    }

    [Test]
    public void Ordinary_fixture_still_destroys_and_verifies_its_disposable_map()
    {
        var operations = new Operations();
        var isolation = new GatewayEndToEndTestIsolation(operations);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        isolation.Prepare(context);
        Assert.That(isolation.Cleanup(context), Is.True);
        Assert.That(operations.Destroyed, Is.True);
        Assert.That(operations.Verifications, Is.EqualTo(3));
    }

    [Test]
    public void Whole_game_is_selected_alone_and_keeps_world_until_process_shutdown()
    {
        Assert.That(EndToEndTestContract.UsesGeneratedMap(typeof(WholeGame), 1), Is.True);
        Assert.Throws<EndToEndContractException>(() => EndToEndTestContract.UsesGeneratedMap(typeof(WholeGame), 2));
        var operations = new Operations();
        var isolation = new GatewayEndToEndTestIsolation(operations);
        var context = new GatewayEndToEndTestContext(() => 0, () => 0, _ => null);
        isolation.PrepareProcessOwnedGame(context);
        Assert.That(isolation.Cleanup(context), Is.True);
        Assert.That(operations.Destroyed, Is.False, "The selected policy must not destroy the saved world.");
        Assert.That(operations.Resets, Is.EqualTo(2));
        Assert.That(operations.Verifications, Is.EqualTo(3));
        Assert.That(operations.Restored, Is.True);
    }

    private sealed class WholeGame : IEndToEndProcessOwnedGameFixture { }
    private sealed class Operations : IGatewayEndToEndIsolationOperations, IGatewayEndToEndProcessOwnedGameIsolationOperations
    {
        private bool processOwned;
        private readonly GatewayEndToEndIsolationBaseline baseline = new(new object());
        public bool IsReady => true;
        public bool Destroyed, Restored;
        public int Resets, Verifications;
        public GatewayEndToEndIsolationBaseline CaptureBaseline() => baseline;
        public GatewayEndToEndIsolationBaseline CaptureProcessOwnedGameBaseline() { processOwned = true; return baseline; }
        public void Pause() { }
        public void ResetTransientState() { Resets++; Destroyed |= !processOwned; }
        public bool VerifyEmpty() { Verifications++; return true; }
        public bool RestoreBaseline(GatewayEndToEndIsolationBaseline value) { Restored = ReferenceEquals(value, baseline); return Restored; }
    }
}
