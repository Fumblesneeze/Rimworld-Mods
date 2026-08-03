using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class GuestWareSelectionPolicyTests
{
    [Test]
    public void Arrived_guest_prefers_colony_cutlery_before_personal_inventory()
    {
        var colony = new[]
        {
            new ServiceWareCandidate<string>("colony", isDirty: false, serviceScore: 20f)
        };
        var personal = new[]
        {
            new ServiceWareCandidate<string>("personal", isDirty: false, serviceScore: 100f)
        };

        var selected = GuestWareSelectionPolicy.Select(
            colony,
            personal,
            mayUsePersonalInventory: true,
            WareRequirementMode.Prefer,
            DirtyWareFallback.Always,
            isEmergency: false);

        Assert.That(selected, Is.EqualTo("colony"));
    }

    [TestCase(true, "personal")]
    [TestCase(false, null)]
    public void Personal_cutlery_is_a_fallback_only_for_an_eligible_guest(
        bool mayUsePersonalInventory,
        string? expected)
    {
        var personal = new[]
        {
            new ServiceWareCandidate<string>("personal", isDirty: false, serviceScore: 60f)
        };

        var selected = GuestWareSelectionPolicy.Select(
            Array.Empty<ServiceWareCandidate<string>>(),
            personal,
            mayUsePersonalInventory,
            WareRequirementMode.Prefer,
            DirtyWareFallback.Always,
            isEmergency: false);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    public void Personal_fallback_requires_an_ordinary_or_hospitality_guest(
        bool ordinaryNonHostileGuest,
        bool arrivedHospitalityGuest,
        bool expected)
    {
        Assert.That(
            GuestWareSelectionPolicy.MayUsePersonalInventory(
                ordinaryNonHostileGuest,
                arrivedHospitalityGuest),
            Is.EqualTo(expected));
    }
}
