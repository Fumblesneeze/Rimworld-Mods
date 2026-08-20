using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CommonSenseAdapterTests
{
    [Test]
    public void Installed_public_shape_is_supported()
    {
        var supported = CommonSenseCompatibility.IsSupported(
            assemblyName: "CommonSense",
            settingsTypeName: "CommonSense.Settings",
            settingsIsPublic: true,
            ingestSettingIsPublicStaticBoolean: true,
            utilityTypeName: "CommonSense.Utility",
            utilityIsPublicStatic: true,
            incapableMethodIsPublicStaticPawnBoolean: true);

        Assert.That(supported, Is.True);
    }

    [TestCase("Lookalike", "CommonSense.Settings", true, true, "CommonSense.Utility", true, true)]
    [TestCase("CommonSense", "Changed.Settings", true, true, "CommonSense.Utility", true, true)]
    [TestCase("CommonSense", "CommonSense.Settings", false, true, "CommonSense.Utility", true, true)]
    [TestCase("CommonSense", "CommonSense.Settings", true, false, "CommonSense.Utility", true, true)]
    [TestCase("CommonSense", "CommonSense.Settings", true, true, "Changed.Utility", true, true)]
    [TestCase("CommonSense", "CommonSense.Settings", true, true, "CommonSense.Utility", false, true)]
    [TestCase("CommonSense", "CommonSense.Settings", true, true, "CommonSense.Utility", true, false)]
    public void Changed_or_lookalike_shape_fails_closed(
        string assemblyName,
        string settingsTypeName,
        bool settingsIsPublic,
        bool ingestSettingIsPublicStaticBoolean,
        string utilityTypeName,
        bool utilityIsPublicStatic,
        bool incapableMethodIsPublicStaticPawnBoolean)
    {
        var supported = CommonSenseCompatibility.IsSupported(
            assemblyName,
            settingsTypeName,
            settingsIsPublic,
            ingestSettingIsPublicStaticBoolean,
            utilityTypeName,
            utilityIsPublicStatic,
            incapableMethodIsPublicStaticPawnBoolean);

        Assert.That(supported, Is.False);
    }

    [TestCase(true, true, true, false, false, true)]
    [TestCase(false, true, true, false, false, false)]
    [TestCase(true, false, true, false, false, false)]
    [TestCase(true, true, false, false, false, false)]
    [TestCase(true, true, true, true, false, false)]
    [TestCase(true, true, true, false, true, false)]
    public void Handoff_starts_only_for_completed_map_dining_owned_by_a_capable_non_gastronomy_pawn(
        bool integrationEnabled,
        bool ingestionCompleted,
        bool mapAvailable,
        bool caravanDining,
        bool gastronomyOwned,
        bool expected)
    {
        Assert.That(
            CommonSenseHandoffPolicy.ShouldClaim(
                integrationEnabled,
                ingestionCompleted,
                mapAvailable,
                caravanDining,
                gastronomyOwned,
                pawnCanClean: true),
            Is.EqualTo(expected));
    }

    [Test]
    public void Incapable_pawn_does_not_claim_dirty_ware()
    {
        Assert.That(
            CommonSenseHandoffPolicy.ShouldClaim(
                integrationEnabled: true,
                ingestionCompleted: true,
                mapAvailable: true,
                caravanDining: false,
                gastronomyOwned: false,
                pawnCanClean: false),
            Is.False);
    }

    [TestCase(true, true, true, true, true)]
    [TestCase(false, true, true, true, false)]
    [TestCase(true, false, true, true, false)]
    [TestCase(true, true, false, true, false)]
    [TestCase(true, true, true, false, false)]
    public void Post_cooking_handoff_requires_eligible_cleanup_a_finalized_covered_product_and_exact_dirty_cookware(
        bool cleanupEligible,
        bool productsCompleted,
        bool coveredProductFinalized,
        bool exactCookwareReturnedDirty,
        bool expected)
    {
        Assert.That(
            CommonSenseCookingCleanupPolicy.ShouldQueue(
                cleanupEligible,
                productsCompleted,
                coveredProductFinalized,
                exactCookwareReturnedDirty),
            Is.EqualTo(expected));
    }

    [TestCase(true, true, true, true)]
    [TestCase(false, true, true, false)]
    [TestCase(true, false, true, false)]
    [TestCase(true, true, false, false)]
    public void Direct_Cook_for_Yourself_followup_is_replaced_only_by_a_created_cleanup_job(
        bool cookForYourselfDriver,
        bool currentJobSucceeded,
        bool cleanupJobCreated,
        bool expected)
    {
        Assert.That(
            CommonSenseCookingCleanupPolicy.ShouldReplaceImmediateFollowup(
                cookForYourselfDriver,
                currentJobSucceeded,
                cleanupJobCreated),
            Is.EqualTo(expected));
    }
}
