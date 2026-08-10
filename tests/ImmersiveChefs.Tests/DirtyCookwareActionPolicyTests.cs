using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DirtyCookwareActionPolicyTests
{
    [Test]
    public void Exact_dirty_cookware_blocker_offers_override_and_prerequisite_wash()
    {
        var state = new DirtyCookwareActionState(
            requirementsActive: true,
            billOtherwiseRunnable: true,
            cookEligible: true,
            cleanPlatesAvailable: true,
            cleanCookwareAvailable: false,
            dirtyCookwareAvailable: true,
            washingDestinationAvailable: true);

        Assert.Multiple(() =>
        {
            Assert.That(DirtyCookwareActionPolicy.ShouldOfferOneJobOverride(state), Is.True);
            Assert.That(DirtyCookwareActionPolicy.ShouldRunPrerequisiteWash(state), Is.True);
        });
    }

    [TestCase(false, true, true, false, true, true)]
    [TestCase(true, false, true, false, true, true)]
    [TestCase(true, true, false, false, true, true)]
    [TestCase(true, true, true, false, false, true)]
    [TestCase(true, true, true, true, true, true)]
    public void Override_is_absent_unless_dirty_cookware_is_the_only_blocker(
        bool billOtherwiseRunnable,
        bool cookEligible,
        bool cleanPlatesAvailable,
        bool cleanCookwareAvailable,
        bool dirtyCookwareAvailable,
        bool requirementsActive)
    {
        var state = new DirtyCookwareActionState(
            requirementsActive,
            billOtherwiseRunnable,
            cookEligible,
            cleanPlatesAvailable,
            cleanCookwareAvailable,
            dirtyCookwareAvailable,
            washingDestinationAvailable: true);

        Assert.That(DirtyCookwareActionPolicy.ShouldOfferOneJobOverride(state), Is.False);
    }

    [Test]
    public void Missing_washing_destination_blocks_only_the_automatic_path()
    {
        var state = new DirtyCookwareActionState(
            requirementsActive: true,
            billOtherwiseRunnable: true,
            cookEligible: true,
            cleanPlatesAvailable: true,
            cleanCookwareAvailable: false,
            dirtyCookwareAvailable: true,
            washingDestinationAvailable: false);

        Assert.Multiple(() =>
        {
            Assert.That(DirtyCookwareActionPolicy.ShouldOfferOneJobOverride(state), Is.True);
            Assert.That(DirtyCookwareActionPolicy.ShouldRunPrerequisiteWash(state), Is.False);
        });
    }
}
