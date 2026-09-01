using ImmersiveSignalFire.Effects;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class OptionalToolsPolicyTests
{
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void BlanketRenderingRequiresBothPackageAndExpectedMarker(
        bool packageActive,
        bool markerPresent,
        bool expected) =>
        Assert.That(OptionalToolsAdapter.ResolveEnabled(packageActive, markerPresent), Is.EqualTo(expected));

    [Test]
    public void SameNamedMarkerFromAnUnownedAssemblyDoesNotEnableBlankets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OptionalToolsAdapter.HasExpectedMarker(new[] { typeof(MorseCadence).Assembly }), Is.False,
                "A same-named type loaded by another mod must not satisfy the canonical package marker.");
            Assert.That(OptionalToolsAdapter.HasExpectedMarker(new[] { typeof(JobEffects.JobToolDef).Assembly }), Is.True,
                "The marker is valid only when it is present in the canonical package's own assembly list.");
        });
    }
}
