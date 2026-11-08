using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;

namespace Megamod.Unit.DrugPolicyMaintenance;

[TestFixture]
public sealed class DrugPolicyBatchTests
{
    [Test]
    public void PreparationFailureDoesNotWriteOrRestoreAnyPolicy()
    {
        var drug = new object();
        var choices = new DrugPolicyChoices(true, true, false, 1f, 1f, 1f, 0);
        var row = new StoredRow(drug, choices);
        var original = new List<StoredRow> { row };
        var first = new Storage(original);
        var second = new Storage([]);
        var failure = new InvalidOperationException("Cannot read second policy.");
        first.AfterApply = () => Assert.Fail("All policies must be prepared before any write.");
        first.BeforeRestore = () => Assert.Fail("There was no write to restore.");
        second.AfterCapture = () => throw failure;

        Assert.That(Assert.Throws<InvalidOperationException>(() => DrugPolicyBatch.Apply(
            [first, second], [new DrugCatalogEntry(drug, false, false)], () => true)), Is.SameAs(failure));
        Assert.That(first.Rows, Is.SameAs(original));
        Assert.That(row.Choices, Is.EqualTo(choices));
    }

    [Test]
    public void SuccessfulEmptyCatalogStillMaterializesNullPolicyStorage()
    {
        var target = new Storage(null);
        var plans = DrugPolicyBatch.Apply([target], [], () => true);

        Assert.Multiple(() =>
        {
            Assert.That(target.Rows, Is.Not.Null.And.Empty);
            Assert.That(plans, Has.Count.EqualTo(1));
            Assert.That(plans[0].Changed, Is.False);
        });
    }

    [TestCase("prepare", 0)]
    [TestCase("first-write", 1)]
    [TestCase("last-write", 2)]
    [TestCase("guard-throws", 1)]
    public void StaleCatalogOrSaveGenerationStopsWritesAndRestoresAttemptedPolicies(string stage, int expectedWrites)
    {
        var drug = new object();
        var choices = new DrugPolicyChoices(true, true, false, 1f, 1f, 1f, 0);
        var firstRow = new StoredRow(drug, choices);
        var secondRow = new StoredRow(drug, choices);
        var firstList = new List<StoredRow> { firstRow };
        var secondList = new List<StoredRow> { secondRow };
        var first = new Storage(firstList);
        var second = new Storage(secondList);
        var current = true;
        var writes = 0;
        second.AfterCapture = () => { if (stage == "prepare") current = false; };
        first.AfterApply = () => { writes++; if (stage == "first-write" || stage == "guard-throws") current = false; };
        second.AfterApply = () => { writes++; if (stage == "last-write") current = false; };

        Assert.Throws<InvalidOperationException>(() => DrugPolicyBatch.Apply(
            [first, second], [new DrugCatalogEntry(drug, false, false)], () =>
                !current && stage == "guard-throws" ? throw new InvalidOperationException("Cannot validate generation.") : current));

        Assert.Multiple(() =>
        {
            Assert.That(writes, Is.EqualTo(expectedWrites));
            Assert.That(first.Rows, Is.SameAs(firstList));
            Assert.That(second.Rows, Is.SameAs(secondList));
            Assert.That(firstRow.Choices, Is.EqualTo(choices));
            Assert.That(secondRow.Choices, Is.EqualTo(choices));
        });
    }

    [Test]
    public void RollbackFailureIsDistinctAndDoesNotPreventRestoringEarlierPolicies()
    {
        var drug = new object();
        var choices = new DrugPolicyChoices(true, true, false, 1f, 1f, 1f, 0);
        var firstRow = new StoredRow(drug, choices);
        var firstList = new List<StoredRow> { firstRow };
        var first = new Storage(firstList);
        var second = new Storage([new StoredRow(drug, choices)]);
        var applyFailure = new InvalidOperationException("apply failed");
        var restoreFailure = new InvalidOperationException("restore failed");
        var order = new List<string>();
        first.BeforeRestore = () => order.Add("first");
        second.BeforeRestore = () => { order.Add("second"); throw restoreFailure; };
        second.AfterApply = () => throw applyFailure;

        var failure = Assert.Throws<DrugPolicyRollbackException>(() => DrugPolicyBatch.Apply(
            [first, second], [new DrugCatalogEntry(drug, false, false)], () => true));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerExceptions, Is.EqualTo(new[] { applyFailure, restoreFailure }));
            Assert.That(order, Is.EqualTo(new[] { "second", "first" }));
            Assert.That(first.Rows, Is.SameAs(firstList));
            Assert.That(firstRow.Choices, Is.EqualTo(choices));
        });
    }

    [Test]
    public void LaterPartialWriteRestoresItsOwnRowsAndEveryEarlierPolicy()
    {
        var retainedDrug = new object();
        var removedDrug = new object();
        var originalChoices = new DrugPolicyChoices(true, true, true, 4f, 0.2f, 0.6f, 9);
        var firstRow = new StoredRow(retainedDrug, originalChoices);
        var secondRow = new StoredRow(retainedDrug, originalChoices with { InventoryTarget = 6 });
        var firstList = new List<StoredRow> { firstRow, new(removedDrug, originalChoices) };
        var secondList = new List<StoredRow> { secondRow, new(removedDrug, originalChoices) };
        var first = new Storage(firstList);
        var second = new Storage(secondList);
        var failure = new InvalidOperationException("Native write failed after changing rows.");
        var observedPartialWrites = false;
        second.AfterApply = () =>
        {
            observedPartialWrites = first.Rows!.Count == 1 && second.Rows!.Count == 1 &&
                !firstRow.Choices.Addiction && !secondRow.Choices.Recreation;
            throw failure;
        };

        var thrown = Assert.Throws<InvalidOperationException>(() => DrugPolicyBatch.Apply(
            [first, second], [new DrugCatalogEntry(retainedDrug, false, false)], () => true));

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(failure));
            Assert.That(observedPartialWrites, Is.True);
            Assert.That(first.Rows, Is.SameAs(firstList));
            Assert.That(second.Rows, Is.SameAs(secondList));
            Assert.That(first.Rows, Has.Count.EqualTo(2));
            Assert.That(second.Rows, Has.Count.EqualTo(2));
            Assert.That(first.Rows![0], Is.SameAs(firstRow));
            Assert.That(second.Rows![0], Is.SameAs(secondRow));
            Assert.That(firstRow.Choices, Is.EqualTo(originalChoices));
            Assert.That(secondRow.Choices, Is.EqualTo(originalChoices with { InventoryTarget = 6 }));
        });
    }

    // This double is the engine-storage boundary, not a simulated loaded mod or Def database.
    private sealed class StoredRow(object drugIdentity, DrugPolicyChoices choices)
    {
        public object DrugIdentity { get; } = drugIdentity;
        public DrugPolicyChoices Choices { get; set; } = choices;
    }

    private sealed class Storage(List<StoredRow>? rows) : IDrugPolicyStorage
    {
        public List<StoredRow>? Rows { get; private set; } = rows;
        public Action? AfterApply { get; set; }
        public Action? AfterCapture { get; set; }
        public Action? BeforeRestore { get; set; }

        public DrugPolicyStorageSnapshot Capture()
        {
            var snapshot = new DrugPolicyStorageSnapshot(Rows,
                Rows?.Select(row => new DrugPolicyRow(row, row.DrugIdentity, row.Choices)).ToArray());
            AfterCapture?.Invoke();
            return snapshot;
        }

        public void Apply(DrugPolicyPlan plan)
        {
            var next = new List<StoredRow>();
            foreach (var row in plan.Rows)
            {
                var stored = (StoredRow?)row.RowIdentity ?? new StoredRow(row.DrugIdentity!, row.Choices);
                stored.Choices = row.Choices;
                next.Add(stored);
            }
            Rows = next;
            AfterApply?.Invoke();
        }

        public void Restore(DrugPolicyStorageSnapshot snapshot)
        {
            BeforeRestore?.Invoke();
            foreach (var row in snapshot.Rows ?? [])
                ((StoredRow)row!.RowIdentity!).Choices = row.Choices;
            Rows = (List<StoredRow>?)snapshot.StorageIdentity;
        }
    }
}
