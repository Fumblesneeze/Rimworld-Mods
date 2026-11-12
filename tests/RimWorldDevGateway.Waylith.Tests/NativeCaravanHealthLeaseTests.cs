using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeCaravanHealthLeaseTests
{
    [TestCase("world")]
    [TestCase("window")]
    [TestCase("pawn")]
    [TestCase("geometry")]
    [TestCase("layout")]
    public void ReusedNativeIdNeverRevivesStaleContextOrGeometry(string changed)
    {
        var identity = new object();
        var snapshot = new GatewayNativeCaravanHealthSnapshot(identity, identity, identity, identity, identity,
            identity, identity, identity, identity, identity, identity, identity, identity, -235086,
            20, 30, 24, 24, 1, 1600, 900, true, 5);
        var current = changed switch
        {
            "world" => snapshot with { World = new object() },
            "window" => snapshot with { Window = new object() },
            "pawn" => snapshot with { Pawn = new object() },
            "geometry" => snapshot with { X = 21 },
            _ => snapshot with { LayoutVersion = 4 }
        };
        var lease = new GatewayNativeCaravanHealthLease(snapshot);
        Assert.That(lease.Validate(snapshot, 5), Is.Null);
        Assert.That(lease.Validate(current, 5), Is.Not.Null);
    }
}
