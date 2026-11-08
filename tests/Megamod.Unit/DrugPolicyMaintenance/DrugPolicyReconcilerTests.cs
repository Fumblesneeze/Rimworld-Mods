using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;

namespace Megamod.Unit.DrugPolicyMaintenance;

[TestFixture]
public sealed class DrugPolicyReconcilerTests
{
    [Test]
    public void EmptyCatalogLeavesAValidEmptyPolicyEvenWithNullSavedRows()
    {
        var plan = DrugPolicyReconciler.Plan(null, []);
        Assert.That(plan.Rows, Is.Empty);
        Assert.That(plan.Changed, Is.False);
    }

    [Test]
    public void EqualNamedButReplacedDrugReferenceDoesNotRetainOldPermissions()
    {
        var oldIdentity = new DrugIdentity("SameDrug");
        var currentIdentity = new DrugIdentity("SameDrug");
        var row = new DrugPolicyRow(new object(), oldIdentity,
            new DrugPolicyChoices(true, true, true, 5f, 0.2f, 0.3f, 9));
        var plan = DrugPolicyReconciler.Plan([row], [new DrugCatalogEntry(currentIdentity, false, false)], _ => { });

        Assert.That(plan.Rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(plan.Rows[0].DrugIdentity, Is.SameAs(currentIdentity));
            Assert.That(plan.Rows[0].RowIdentity, Is.Null);
            Assert.That(plan.Rows[0].Choices, Is.EqualTo(new DrugPolicyChoices(false, false, false, 1f, 1f, 1f, 0)));
            Assert.That(plan.Removed, Is.EqualTo(1));
            Assert.That(plan.Added, Is.EqualTo(1));
        });
    }

    private sealed record DrugIdentity(string Name);

    [Test]
    public void CompletedPlanDoesNotShareTheOrderersMutableWorkingList()
    {
        List<DrugPolicyRow>? working = null;
        var drug = new object();
        var plan = DrugPolicyReconciler.Plan(null, [new DrugCatalogEntry(drug, false, false)],
            list => working = list);
        working!.Clear();

        Assert.That(plan.Rows, Has.Count.EqualTo(1));
        Assert.That(plan.Rows[0].DrugIdentity, Is.SameAs(drug));
    }

    [Test]
    public void UnsupportedOrderingCannotDropAPlannedDrugRow()
    {
        Assert.Throws<InvalidOperationException>(() => DrugPolicyReconciler.Plan(null,
            [new DrugCatalogEntry(new object(), false, false)], list => list.Clear()));
    }

    [Test]
    public void RepairCountsChangedRowsAndRepeatedPlanningIsQuiet()
    {
        var retainedDrug = new object();
        var addedDrug = new object();
        var row = new DrugPolicyRow(new object(), retainedDrug,
            new DrugPolicyChoices(true, true, true, 2f, 0.4f, 0.5f, 7));
        var catalog = new[] { new DrugCatalogEntry(retainedDrug, false, false), new DrugCatalogEntry(addedDrug, false, false) };
        var first = DrugPolicyReconciler.Plan([row, row, null], catalog, _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(first.Removed, Is.EqualTo(1));
            Assert.That(first.Duplicates, Is.EqualTo(1));
            Assert.That(first.Added, Is.EqualTo(1));
            Assert.That(first.Normalized, Is.EqualTo(1));
        });
        var repeated = DrugPolicyReconciler.Plan(first.Rows, catalog);
        Assert.That(repeated.Changed, Is.False);
        Assert.That(repeated.Rows, Is.EqualTo(first.Rows));
    }

    [Test]
    public void AdditionsUseTheCompleteNativeOrderWhileCleanupKeepsSurvivingOrder()
    {
        var earlier = new object();
        var later = new object();
        var choices = new DrugPolicyChoices(false, false, false, 1f, 1f, 1f, 0);
        var retained = new DrugPolicyRow(new object(), later, choices);
        var catalog = new[] { new DrugCatalogEntry(later, false, false), new DrugCatalogEntry(earlier, false, false) };

        var plan = DrugPolicyReconciler.Plan([retained], catalog,
            list => list.Sort((left, right) => ReferenceEquals(left.DrugIdentity, right.DrugIdentity)
                ? 0 : ReferenceEquals(left.DrugIdentity, earlier) ? -1 : 1));

        Assert.That(plan.Rows.Select(row => row.DrugIdentity), Is.EqualTo(new[] { earlier, later }));
        var cleanup = DrugPolicyReconciler.Plan(plan.Rows.Reverse().ToArray(), catalog,
            _ => throw new InvalidOperationException("Cleanup must preserve stored order."));
        Assert.That(cleanup.Rows.Select(row => row.DrugIdentity), Is.EqualTo(new[] { later, earlier }));
    }

    [Test]
    public void MissingRowsReceiveNativeDefaultsWithoutReDefaultingExistingChoices()
    {
        var existingDrug = new object();
        var newDrug = new object();
        var existingChoices = new DrugPolicyChoices(false, true, true, 4f, 0.3f, 0.5f, 6);
        var row = new DrugPolicyRow(new object(), existingDrug, existingChoices);

        var plan = DrugPolicyReconciler.Plan([row],
            [new DrugCatalogEntry(existingDrug, true, true), new DrugCatalogEntry(newDrug, true, true)],
            _ => { });

        Assert.That(plan.Rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(plan.Rows[0].Choices, Is.EqualTo(existingChoices));
            Assert.That(plan.Rows[1].DrugIdentity, Is.SameAs(newDrug));
            Assert.That(plan.Rows[1].RowIdentity, Is.Null);
            Assert.That(plan.Rows[1].Choices,
                Is.EqualTo(new DrugPolicyChoices(true, false, false, 1f, 1f, 1f, 0)));
        });
    }

    [Test]
    public void ChangedDrugPropertiesClearOnlyInapplicablePermissionsWithoutMutatingInput()
    {
        var drug = new object();
        var choices = new DrugPolicyChoices(true, true, true, 2.5f, 0.4f, 0.6f, 3);
        var row = new DrugPolicyRow(new object(), drug, choices);

        var plan = DrugPolicyReconciler.Plan([row], [new DrugCatalogEntry(drug, false, false)]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Rows[0].RowIdentity, Is.SameAs(row.RowIdentity));
            Assert.That(plan.Rows[0].Choices,
                Is.EqualTo(choices with { Addiction = false, Recreation = false }));
            Assert.That(row.Choices, Is.EqualTo(choices));
        });
    }

    [Test]
    public void CleanupRetainsFirstEffectiveRowAndItsChoices()
    {
        var drug = new object();
        var staleDrug = new object();
        var choices = new DrugPolicyChoices(true, true, true, 2.5f, 0.4f, 0.6f, 3);
        var first = new DrugPolicyRow(new object(), drug, choices);
        var duplicate = new DrugPolicyRow(new object(), drug, choices with { InventoryTarget = 20 });
        var stale = new DrugPolicyRow(new object(), staleDrug, choices);

        var plan = DrugPolicyReconciler.Plan(
            [stale, first, duplicate, null],
            [new DrugCatalogEntry(drug, true, true)]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Rows, Has.Count.EqualTo(1));
            Assert.That(plan.Rows[0].RowIdentity, Is.SameAs(first.RowIdentity));
            Assert.That(plan.Rows[0].Choices, Is.EqualTo(choices));
            Assert.That(plan.Removed, Is.EqualTo(2));
            Assert.That(plan.Duplicates, Is.EqualTo(1));
            Assert.That(first.Choices, Is.EqualTo(choices));
        });
    }
}
