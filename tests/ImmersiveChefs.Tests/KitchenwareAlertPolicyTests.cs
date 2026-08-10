using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class KitchenwareAlertPolicyTests
{
    [TestCase(WareRequirementMode.Strict, true, true, true, false, true, true, true, true)]
    [TestCase(WareRequirementMode.Prefer, true, true, true, false, true, true, true, false)]
    [TestCase(WareRequirementMode.Off, true, true, true, false, true, true, true, false)]
    [TestCase(WareRequirementMode.Strict, false, true, true, false, true, true, true, false)]
    [TestCase(WareRequirementMode.Strict, true, false, true, false, true, true, true, false)]
    [TestCase(WareRequirementMode.Strict, true, true, false, false, true, true, true, false)]
    [TestCase(WareRequirementMode.Strict, true, true, true, true, true, true, true, false)]
    [TestCase(WareRequirementMode.Strict, true, true, true, false, false, true, true, false)]
    [TestCase(WareRequirementMode.Strict, true, true, true, false, true, false, true, false)]
    [TestCase(WareRequirementMode.Strict, true, true, true, false, true, true, false, false)]
    public void Alert_intent_requires_a_runnable_strict_bill_on_an_opted_in_owned_kitchen(
        WareRequirementMode mode,
        bool playerOwnedStation,
        bool alertEnabledStation,
        bool stationOperational,
        bool billSuspended,
        bool billShouldDoNow,
        bool coveredRecipe,
        bool hasEligibleCook,
        bool expected)
    {
        Assert.That(
            KitchenwareAlertPolicy.IsRelevantBill(
                mode,
                playerOwnedStation,
                alertEnabledStation,
                stationOperational,
                billSuspended,
                billShouldDoNow,
                coveredRecipe,
                hasEligibleCook),
            Is.EqualTo(expected));
    }

    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    public void Absence_is_alert_worthy_only_after_a_complete_physical_inventory_scan(
        bool inventoryScanReliable,
        bool productPresent,
        bool expected)
    {
        Assert.That(
            KitchenwareAlertPolicy.IsAlertWorthyAbsence(
                inventoryScanReliable,
                productPresent),
            Is.EqualTo(expected));
    }

    [TestCase(false, false, false, true)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, true, false)]
    public void Alert_workflows_ignore_drafted_downed_and_mental_state_pawns(
        bool drafted,
        bool downed,
        bool inMentalState,
        bool expected)
    {
        Assert.That(
            KitchenwareAlertPolicy.IsEligibleAlertWorker(
                drafted,
                downed,
                inMentalState),
            Is.EqualTo(expected));
    }

    [Test]
    public void Alert_label_names_the_exact_missing_product_types()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                KitchenwareAlertRuntime.MissingProductTranslationKeys(new[] { KitchenwareProduct.Cookware }),
                Is.EqualTo(new[] { "ImmersiveChefs_Product_CookwarePlural" }));
            Assert.That(
                KitchenwareAlertRuntime.MissingProductTranslationKeys(new[] { KitchenwareProduct.Plate }),
                Is.EqualTo(new[] { "ImmersiveChefs_Product_Plates" }));
            Assert.That(
                KitchenwareAlertRuntime.MissingProductTranslationKeys(new[]
                {
                    KitchenwareProduct.Plate,
                    KitchenwareProduct.Cookware,
                    KitchenwareProduct.Plate
                }),
                Is.EqualTo(new[]
                {
                    "ImmersiveChefs_Product_CookwarePlural",
                    "ImmersiveChefs_Product_Plates"
                }));
        });
    }
}
