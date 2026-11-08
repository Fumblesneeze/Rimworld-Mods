using System.Reflection;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyMaintenance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugPolicyStorageTests
{
    private static readonly FieldInfo Entries = typeof(DrugPolicy).GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Test]
    public void CleanupRetainsTheFirstNativeEntryWithEveryChoiceAndItsEditBuffer()
    {
        var drug = new ThingDef { defName = "Megamod_TestDrug", category = ThingCategory.Item };
        var nonDrug = new ThingDef { defName = "Megamod_TestNonDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug, nonDrug);
        var winner = new DrugPolicyEntry
        {
            drug = drug, allowedForAddiction = true, allowedForJoy = false, allowScheduled = true,
            daysFrequency = 3.5f, onlyIfMoodBelow = 0.31f, onlyIfJoyBelow = 0.67f,
            takeToInventory = 9, takeToInventoryTempBuffer = "009"
        };
        var duplicate = new DrugPolicyEntry
        {
            drug = drug, allowedForAddiction = false, allowedForJoy = true, allowScheduled = false,
            daysFrequency = 1f, onlyIfMoodBelow = 1f, onlyIfJoyBelow = 1f, takeToInventory = 2
        };
        var policy = new DrugPolicy();
        var original = new List<DrugPolicyEntry> { null!, new() { drug = nonDrug }, winner, duplicate, new() { drug = null! } };
        Entries.SetValue(policy, original);

        var plans = DrugPolicyBatch.Apply(new[] { new NativeDrugPolicyStorage(policy) },
            new[] { new DrugCatalogEntry(drug, true, true) }, () => true);

        Assert.Multiple(() =>
        {
            Assert.That(policy.Count, Is.EqualTo(1));
            Assert.That(policy[0], Is.SameAs(winner));
            Assert.That(policy[drug], Is.SameAs(winner));
            Assert.That(winner.drug, Is.SameAs(drug));
            Assert.That(winner.allowedForAddiction, Is.True);
            Assert.That(winner.allowedForJoy, Is.False);
            Assert.That(winner.allowScheduled, Is.True);
            Assert.That(winner.daysFrequency, Is.EqualTo(3.5f));
            Assert.That(winner.onlyIfMoodBelow, Is.EqualTo(0.31f));
            Assert.That(winner.onlyIfJoyBelow, Is.EqualTo(0.67f));
            Assert.That(winner.takeToInventory, Is.EqualTo(9));
            Assert.That(winner.takeToInventoryTempBuffer, Is.EqualTo("009"));
            Assert.That(original, Has.Count.EqualTo(5), "Publishing membership must not edit the captured list.");
            Assert.That(plans.Single().Removed, Is.EqualTo(3));
            Assert.That(plans.Single().Duplicates, Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizationWritesOnlyTheInapplicablePermissionsOnTheRetainedEntry()
    {
        var drug = new ThingDef { defName = "Megamod_TestDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug);
        var entry = new DrugPolicyEntry
        {
            drug = drug, allowedForAddiction = true, allowedForJoy = true, allowScheduled = true,
            daysFrequency = 2.25f, onlyIfMoodBelow = 0.42f, onlyIfJoyBelow = 0.81f,
            takeToInventory = 7, takeToInventoryTempBuffer = "007"
        };
        var policy = new DrugPolicy();
        Entries.SetValue(policy, new List<DrugPolicyEntry> { entry });

        var plans = DrugPolicyBatch.Apply(new[] { new NativeDrugPolicyStorage(policy) },
            new[] { new DrugCatalogEntry(drug, false, false) }, () => true);

        Assert.Multiple(() =>
        {
            Assert.That(policy[drug], Is.SameAs(entry));
            Assert.That(entry.allowedForAddiction, Is.False);
            Assert.That(entry.allowedForJoy, Is.False);
            Assert.That(entry.allowScheduled, Is.True);
            Assert.That(entry.daysFrequency, Is.EqualTo(2.25f));
            Assert.That(entry.onlyIfMoodBelow, Is.EqualTo(0.42f));
            Assert.That(entry.onlyIfJoyBelow, Is.EqualTo(0.81f));
            Assert.That(entry.takeToInventory, Is.EqualTo(7));
            Assert.That(entry.takeToInventoryTempBuffer, Is.EqualTo("007"));
            Assert.That(plans.Single().Normalized, Is.EqualTo(1));
        });
    }

    [Test]
    public void RestoreUsesDetachedChoicesAndOriginalListAndEntryIdentities()
    {
        var drug = new ThingDef { defName = "Megamod_TestDrug", category = ThingCategory.Item };
        var other = new ThingDef { defName = "Megamod_OtherDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug, other);
        var first = new DrugPolicyEntry
        {
            drug = drug, allowedForAddiction = true, allowedForJoy = false, allowScheduled = true,
            daysFrequency = 3.5f, onlyIfMoodBelow = 0.31f, onlyIfJoyBelow = 0.67f,
            takeToInventory = 9, takeToInventoryTempBuffer = "009"
        };
        var duplicate = new DrugPolicyEntry { drug = drug, takeToInventory = 2 };
        var invalid = new DrugPolicyEntry { drug = null!, takeToInventory = 6 };
        var original = new List<DrugPolicyEntry> { null!, first, duplicate, invalid };
        var policy = new DrugPolicy();
        Entries.SetValue(policy, original);
        var storage = new NativeDrugPolicyStorage(policy);
        var snapshot = storage.Capture();
        storage.Apply(DrugPolicyReconciler.Plan(snapshot.Rows, new[] { new DrugCatalogEntry(drug, false, false) }));
        first.drug = other;
        first.allowedForAddiction = false;
        first.allowedForJoy = true;
        first.allowScheduled = false;
        first.daysFrequency = 8f;
        first.onlyIfMoodBelow = 0.9f;
        first.onlyIfJoyBelow = 0.2f;
        first.takeToInventory = 1;
        original.Clear();

        storage.Restore(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(Entries.GetValue(policy), Is.SameAs(original));
            Assert.That(original, Has.Count.EqualTo(4));
            Assert.That(original[0], Is.Null);
            Assert.That(original[1], Is.SameAs(first));
            Assert.That(original[2], Is.SameAs(duplicate));
            Assert.That(original[3], Is.SameAs(invalid));
            Assert.That(invalid.drug, Is.Null);
            Assert.That(invalid.takeToInventory, Is.EqualTo(6));
            Assert.That(first.drug, Is.SameAs(drug));
            Assert.That(first.allowedForAddiction, Is.True);
            Assert.That(first.allowedForJoy, Is.False);
            Assert.That(first.allowScheduled, Is.True);
            Assert.That(first.daysFrequency, Is.EqualTo(3.5f));
            Assert.That(first.onlyIfMoodBelow, Is.EqualTo(0.31f));
            Assert.That(first.onlyIfJoyBelow, Is.EqualTo(0.67f));
            Assert.That(first.takeToInventory, Is.EqualTo(9));
            Assert.That(first.takeToInventoryTempBuffer, Is.EqualTo("009"));
            Assert.That(duplicate.takeToInventory, Is.EqualTo(2));
        });
    }

    [Test]
    public void MissingRowsUseNativeDefaultsAndTheSingleCallerSuppliedNativeOrder()
    {
        var retainedDrug = OrderedDrug("Megamod_RetainedDrug", 20);
        var addictive = OrderedDrug("Megamod_AddictiveDrug", 5);
        var nonAddictive = OrderedDrug("Megamod_NonAddictiveDrug", 1);
        using var definitions = new ThingDefScope(retainedDrug, addictive, nonAddictive);
        var retained = new DrugPolicyEntry { drug = retainedDrug, takeToInventory = 4 };
        var policy = new DrugPolicy();
        Entries.SetValue(policy, new List<DrugPolicyEntry> { retained });
        var orderingCalls = 0;

        var plans = DrugPolicyBatch.Apply(new[] { new NativeDrugPolicyStorage(policy) }, new[]
        {
            new DrugCatalogEntry(retainedDrug, true, true),
            new DrugCatalogEntry(addictive, true, true),
            new DrugCatalogEntry(nonAddictive, false, false)
        }, () => true, rows =>
        {
            orderingCalls++;
            rows.SortBy(row => ((ThingDef)row.DrugIdentity!).GetCompProperties<CompProperties_Drug>().listOrder);
        });

        Assert.Multiple(() =>
        {
            Assert.That(orderingCalls, Is.EqualTo(1));
            Assert.That(policy.Count, Is.EqualTo(3));
            Assert.That(policy[0].drug, Is.SameAs(nonAddictive));
            Assert.That(policy[1].drug, Is.SameAs(addictive));
            Assert.That(policy[2], Is.SameAs(retained));
            Assert.That(retained.takeToInventory, Is.EqualTo(4));
            Assert.That(policy[nonAddictive].allowedForAddiction, Is.False);
            Assert.That(policy[addictive].allowedForAddiction, Is.True);
            foreach (var drug in new[] { addictive, nonAddictive })
            {
                var entry = policy[drug];
                Assert.That(entry.allowedForJoy, Is.False);
                Assert.That(entry.allowScheduled, Is.False);
                Assert.That(entry.daysFrequency, Is.EqualTo(1f));
                Assert.That(entry.onlyIfMoodBelow, Is.EqualTo(1f));
                Assert.That(entry.onlyIfJoyBelow, Is.EqualTo(1f));
                Assert.That(entry.takeToInventory, Is.Zero);
                Assert.That(entry.takeToInventoryTempBuffer, Is.Null);
            }
            Assert.That(plans.Single().Added, Is.EqualTo(2));
        });
    }

    [Test]
    public void NullStorageMaterializesValidEmptyMembershipEvenWhenThePlanIsUnchanged()
    {
        using var definitions = new ThingDefScope();
        var policy = new DrugPolicy();
        var storage = new NativeDrugPolicyStorage(policy);
        var before = storage.Capture();

        var plans = DrugPolicyBatch.Apply(new[] { storage }, Array.Empty<DrugCatalogEntry>(), () => true);

        Assert.Multiple(() =>
        {
            Assert.That(before.StorageIdentity, Is.Null);
            Assert.That(before.Rows, Is.Null);
            Assert.That(plans.Single().Changed, Is.False);
            Assert.That(policy.Count, Is.Zero);
            Assert.That(storage.Capture().Rows, Is.Empty);
        });

        storage.Restore(before);
        Assert.That(Entries.GetValue(policy), Is.Null);
    }

    [Test]
    public void BatchRestoresThePartiallyWrittenCurrentAndEarlierNativeStorage()
    {
        var drug = OrderedDrug("Megamod_TestDrug", 1);
        using var definitions = new ThingDefScope(drug);
        var first = new DrugPolicy();
        var winner = new DrugPolicyEntry
        {
            drug = drug, allowedForAddiction = true, allowedForJoy = true,
            takeToInventory = 6, takeToInventoryTempBuffer = "006"
        };
        var duplicate = new DrugPolicyEntry { drug = drug, takeToInventory = 2 };
        var original = new List<DrugPolicyEntry> { winner, null!, duplicate };
        Entries.SetValue(first, original);
        var second = new DrugPolicy();
        var failure = new InvalidOperationException("Fail after the current native list was published.");
        var secondStorage = new FailAfterNativeWrite(new NativeDrugPolicyStorage(second), failure);

        var observed = Assert.Throws<InvalidOperationException>(() => DrugPolicyBatch.Apply(
            new IDrugPolicyStorage[] { new NativeDrugPolicyStorage(first), secondStorage },
            new[] { new DrugCatalogEntry(drug, false, false) }, () => true,
            rows => rows.SortBy(row => ((ThingDef)row.DrugIdentity!).GetCompProperties<CompProperties_Drug>().listOrder)));

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.SameAs(failure));
            Assert.That(secondStorage.ObservedWrittenRows, Is.EqualTo(1));
            Assert.That(Entries.GetValue(second), Is.Null);
            Assert.That(Entries.GetValue(first), Is.SameAs(original));
            Assert.That(first.Count, Is.EqualTo(3));
            Assert.That(first[0], Is.SameAs(winner));
            Assert.That(first[1], Is.Null);
            Assert.That(first[2], Is.SameAs(duplicate));
            Assert.That(winner.allowedForAddiction, Is.True);
            Assert.That(winner.allowedForJoy, Is.True);
            Assert.That(winner.takeToInventory, Is.EqualTo(6));
            Assert.That(winner.takeToInventoryTempBuffer, Is.EqualTo("006"));
        });
    }

    private sealed class FailAfterNativeWrite : IDrugPolicyStorage
    {
        private readonly NativeDrugPolicyStorage storage;
        private readonly Exception failure;

        public FailAfterNativeWrite(NativeDrugPolicyStorage storage, Exception failure)
        {
            this.storage = storage;
            this.failure = failure;
        }

        public int ObservedWrittenRows { get; private set; }
        public DrugPolicyStorageSnapshot Capture() => storage.Capture();
        public void Apply(DrugPolicyPlan plan)
        {
            storage.Apply(plan);
            ObservedWrittenRows = storage.Capture().Rows!.Count;
            throw failure;
        }
        public void Restore(DrugPolicyStorageSnapshot snapshot) => storage.Restore(snapshot);
    }

    private static ThingDef OrderedDrug(string name, float order) => new()
    {
        defName = name, category = ThingCategory.Item,
        comps = new List<CompProperties> { new CompProperties_Drug { listOrder = order } }
    };

    private sealed class ThingDefScope : IDisposable
    {
        public ThingDefScope(params ThingDef[] definitions)
        {
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty,
                "This fixture must exclusively own an initially empty ThingDef database.");
            try { DefDatabase<ThingDef>.Add(definitions); }
            catch { DefDatabase<ThingDef>.Clear(); throw; }
        }

        public void Dispose() => DefDatabase<ThingDef>.Clear();
    }
}
