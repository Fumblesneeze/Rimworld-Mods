using System.Reflection;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugToleranceStateTests
{
    private static readonly FieldInfo Entries = typeof(DrugPolicy).GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Test]
    public void PreparedPolicyRestoresCapturedChoiceRatherThanReadingALaterEdit()
    {
        var drug = new ThingDef { defName = "Megamod_ToleranceDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug);
        var entry = new DrugPolicyEntry { drug = drug };
        NativeDrugToleranceState.Write(entry, DrugToleranceChoice.Numeric(0.25f));
        var policy = new DrugPolicy();
        Entries.SetValue(policy, new List<DrugPolicyEntry> { entry });
        var storage = new NativeDrugPolicyStorage(policy);
        var snapshot = storage.Capture();
        var plan = DrugPolicyReconciler.Plan(snapshot.Rows, new[] { new DrugCatalogEntry(drug, true, true) });
        NativeDrugToleranceState.Write(entry, DrugToleranceChoice.Numeric(0.8f));

        storage.Apply(plan);

        Assert.That(NativeDrugToleranceState.Read(entry).Limit, Is.EqualTo(0.25f));
    }

    [Test]
    public void NativeRowCopiesAreIndependentAndRetainUnknownEncoding()
    {
        var source = new DrugPolicyEntry();
        var copy = new DrugPolicyEntry();
        NativeDrugToleranceState.WriteEncoded(source, "2:future:raw-content");
        NativeDrugToleranceState.Copy(source, copy);
        Assert.That(NativeDrugToleranceState.ReadEncoded(copy), Is.EqualTo("2:future:raw-content"));
        NativeDrugToleranceState.Write(copy, DrugToleranceChoice.Numeric(0f));
        Assert.That(NativeDrugToleranceState.Read(source).Kind, Is.EqualTo(DrugToleranceChoiceKind.NeedsReview));
        Assert.That(NativeDrugToleranceState.Read(copy).Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(NativeDrugToleranceState.Read(copy).Limit, Is.Zero);
    }

    [Test]
    public void MissingAndExplicitAnyDecodeWithoutBorrowingAnotherRowsChoice()
    {
        var first = new DrugPolicyEntry();
        var second = new DrugPolicyEntry();
        NativeDrugToleranceState.Write(first, DrugToleranceChoice.Numeric(1f));
        NativeDrugToleranceState.WriteEncoded(second, null);
        Assert.That(NativeDrugToleranceState.Read(first).Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(NativeDrugToleranceState.ReadEncoded(second), Is.Null);
        Assert.That(NativeDrugToleranceState.Read(second).Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        NativeDrugToleranceState.WriteEncoded(second, "1:any");
        Assert.That(NativeDrugToleranceState.ReadEncoded(second), Is.EqualTo("1:any"));
    }

    [Test]
    public void ReconciliationRetainsFirstDuplicateAndNewRowsDoNotReviveRemovedChoices()
    {
        var drug = new ThingDef { defName = "Megamod_ToleranceDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug);
        var winner = new DrugPolicyEntry { drug = drug, allowedForAddiction = true };
        var duplicate = new DrugPolicyEntry { drug = drug };
        NativeDrugToleranceState.Write(winner, DrugToleranceChoice.Numeric(0f));
        NativeDrugToleranceState.Write(duplicate, DrugToleranceChoice.Numeric(0.8f));
        var policy = new DrugPolicy();
        Entries.SetValue(policy, new List<DrugPolicyEntry> { winner, duplicate });
        var storage = new NativeDrugPolicyStorage(policy);
        DrugPolicyBatch.Apply(new[] { storage }, new[] { new DrugCatalogEntry(drug, false, false) }, () => true);
        Assert.That(policy.Count, Is.EqualTo(1));
        Assert.That(policy[0], Is.SameAs(winner));
        Assert.That(winner.allowedForAddiction, Is.False);
        Assert.That(NativeDrugToleranceState.Read(winner).Limit, Is.Zero);
        Assert.That(NativeDrugToleranceState.Read(winner).Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));

        DrugPolicyBatch.Apply(new[] { storage }, Array.Empty<DrugCatalogEntry>(), () => true);
        DrugPolicyBatch.Apply(new[] { storage }, new[] { new DrugCatalogEntry(drug, true, true) }, () => true, _ => { });

        Assert.That(policy[0], Is.Not.SameAs(winner));
        Assert.That(NativeDrugToleranceState.Read(policy[0]).Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
    }

    [Test]
    public void LaterPolicyFailureRestoresRawChoicesAndBothNativeMemberships()
    {
        var drug = new ThingDef { defName = "Megamod_ToleranceDrug", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug);
        var firstEntry = new DrugPolicyEntry { drug = drug };
        var secondEntry = new DrugPolicyEntry { drug = drug };
        NativeDrugToleranceState.WriteEncoded(firstEntry, "2:future:untouched");
        NativeDrugToleranceState.Write(secondEntry, DrugToleranceChoice.Numeric(0f));
        var first = new DrugPolicy();
        var second = new DrugPolicy();
        var firstRows = new List<DrugPolicyEntry> { firstEntry };
        var secondRows = new List<DrugPolicyEntry> { secondEntry };
        Entries.SetValue(first, firstRows);
        Entries.SetValue(second, secondRows);
        var failure = new InvalidOperationException("Failure after publishing both native rows.");
        var failed = new FailingStorage(new NativeDrugPolicyStorage(second), () =>
        {
            NativeDrugToleranceState.Write(firstEntry, DrugToleranceChoice.Any);
            NativeDrugToleranceState.Write(secondEntry, DrugToleranceChoice.Numeric(1f));
            throw failure;
        });

        Assert.That(Assert.Throws<InvalidOperationException>(() => DrugPolicyBatch.Apply(
            new IDrugPolicyStorage[] { new NativeDrugPolicyStorage(first), failed },
            new[] { new DrugCatalogEntry(drug, true, true) }, () => true)), Is.SameAs(failure));

        Assert.That(Entries.GetValue(first), Is.SameAs(firstRows));
        Assert.That(Entries.GetValue(second), Is.SameAs(secondRows));
        Assert.That(NativeDrugToleranceState.ReadEncoded(firstEntry), Is.EqualTo("2:future:untouched"));
        Assert.That(NativeDrugToleranceState.Read(secondEntry).Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(NativeDrugToleranceState.Read(secondEntry).Limit, Is.Zero);
    }

    private sealed class FailingStorage(NativeDrugPolicyStorage inner, Action failAfterWrite) : IDrugPolicyStorage
    {
        public DrugPolicyStorageSnapshot Capture() => inner.Capture();
        public void Apply(DrugPolicyPlan plan) { inner.Apply(plan); failAfterWrite(); }
        public void Restore(DrugPolicyStorageSnapshot snapshot) => inner.Restore(snapshot);
    }

    private sealed class ThingDefScope : IDisposable
    {
        public ThingDefScope(params ThingDef[] definitions)
        {
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
            try { DefDatabase<ThingDef>.Add(definitions); }
            catch { DefDatabase<ThingDef>.Clear(); throw; }
        }

        public void Dispose() => DefDatabase<ThingDef>.Clear();
    }
}
