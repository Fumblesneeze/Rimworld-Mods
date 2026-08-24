using GuestBedGizmo.Compatibility.Hospitality;
using NUnit.Framework;

namespace GuestBedGizmo.Tests;

[TestFixture]
public sealed class HospitalityPackagePolicyTests
{
    [Test]
    public void OnlyCanonicalHospitalityAndIdeologyPackagesActivateTheAdapter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HospitalityPackagePolicy.ShouldActivate(System.Array.Empty<string>()), Is.False);
            Assert.That(HospitalityPackagePolicy.ShouldActivate(new[] { "Orion.Hospitality" }), Is.False);
            Assert.That(HospitalityPackagePolicy.ShouldActivate(new[] { "Ludeon.RimWorld.Ideology" }), Is.False);
            Assert.That(HospitalityPackagePolicy.ShouldActivate(new[]
            {
                "orion.hospitality",
                "ludeon.rimworld.ideology",
            }), Is.True);
            Assert.That(HospitalityPackagePolicy.ShouldActivate(new[]
            {
                "orion.hospitality.patch",
                "ludeon.rimworld.ideology",
            }), Is.False);
        });
    }
}
