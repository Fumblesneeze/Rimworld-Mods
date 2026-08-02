using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class WareSelectionPolicyTests
{
    [Test]
    public void Selection_prefers_clean_and_applies_strict_prefer_off_emergency_rules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Strict, DirtyWareFallback.Always,
                isEmergency: false, cleanAvailable: true, dirtyAvailable: true).Use,
                Is.EqualTo(WareUse.Clean));
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Strict, DirtyWareFallback.UrgentOnly,
                isEmergency: false, cleanAvailable: false, dirtyAvailable: true).Admission,
                Is.EqualTo(WareAdmission.Blocked));
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Strict, DirtyWareFallback.UrgentOnly,
                isEmergency: true, cleanAvailable: false, dirtyAvailable: true).Use,
                Is.EqualTo(WareUse.Dirty));
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Strict, DirtyWareFallback.Never,
                isEmergency: true, cleanAvailable: false, dirtyAvailable: false).Use,
                Is.EqualTo(WareUse.MissingEmergency));
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Prefer, DirtyWareFallback.Never,
                isEmergency: false, cleanAvailable: false, dirtyAvailable: true).Use,
                Is.EqualTo(WareUse.Missing));
            Assert.That(WareSelectionPolicy.Select(WareRequirementMode.Off, DirtyWareFallback.Always,
                isEmergency: false, cleanAvailable: true, dirtyAvailable: true).Use,
                Is.EqualTo(WareUse.Exempt));
        });
    }
}
