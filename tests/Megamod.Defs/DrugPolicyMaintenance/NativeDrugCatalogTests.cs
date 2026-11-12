using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyMaintenance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugCatalogTests
{
    [Test]
    public void InitialReadCapturesTheCompleteOrderedBaselineWithoutLaterDatabaseReads()
    {
        var first = Drug("Megamod_InitialFirst", 0.4f, 0f);
        var second = Drug("Megamod_InitialSecond", 0f, 0.2f);
        bool current = true;
        DrugCatalogRead read;
        using (var definitions = new ThingDefScope(first, second))
            read = NativeDrugCatalog.CaptureInitial(() => current);

        // A subsequent independent database cannot rewrite this detached initial publication.
        var replacement = Drug(first.defName, 0f, 0.2f);
        using var nextDefinitions = new ThingDefScope(replacement);
        var membership = read.ForPreset(() => true);
        Assert.That(read.Entries, Is.EqualTo(new[]
        {
            new DrugCatalogEntry(first, true, false), new DrugCatalogEntry(second, false, true)
        }));
        Assert.That(membership.Contains(replacement), Is.False);
        Assert.That(membership.Contains(first), Is.True);
        current = false;
        Assert.That(read.IsCurrent, Is.False);
        Assert.That(membership.IsCurrent, Is.False);
    }

    [Test]
    public void CaptureUsesRegisteredDrugItemIdentityAndNativePropertyFlags()
    {
        var drug = Drug("Megamod_TestDrug", 0.4f, 0.2f);
        var notAnItem = Drug("Megamod_NotAnItem", 0.4f, 0.2f);
        notAnItem.category = ThingCategory.Building;
        var notADrug = Drug("Megamod_NotADrug", 0.4f, 0.2f);
        notADrug.ingestible.drugCategory = DrugCategory.None;
        var notIngestible = new ThingDef { defName = "Megamod_NotIngestible", category = ThingCategory.Item };
        using var definitions = new ThingDefScope(drug, notAnItem, notADrug, notIngestible);

        var catalog = NativeDrugCatalog.Capture(_ => true);

        Assert.That(catalog, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(catalog[0].Identity, Is.SameAs(drug));
            Assert.That(catalog[0].Addictive, Is.True);
            Assert.That(catalog[0].Pleasure, Is.True);
        });
    }

    [Test]
    public void FinalizedExclusionSkipsUnavailableDrugBeforeCompAccess()
    {
        var allowed = Drug("Megamod_AllowedDrug", 0f, 0f);
        var excluded = Drug("Megamod_ExcludedDrug", 0.5f, 0.5f);
        excluded.comps = null!;
        using var definitions = new ThingDefScope(allowed, excluded);
        var queried = new List<ThingDef>();

        var catalog = NativeDrugCatalog.Capture(definition =>
        {
            queried.Add(definition);
            return !ReferenceEquals(definition, excluded);
        });

        Assert.Multiple(() =>
        {
            Assert.That(queried, Is.EqualTo(new[] { allowed, excluded }));
            Assert.That(catalog.Select(entry => entry.Identity), Is.EqualTo(new[] { allowed }));
            Assert.That(catalog[0].Addictive, Is.False);
            Assert.That(catalog[0].Pleasure, Is.False);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AvailableDrugWithoutNativeOrderingCompRejectsTheWholeCapture(bool nullComps)
    {
        var supported = Drug("Megamod_SupportedDrug", 0.4f, 0.2f);
        var unsupported = Drug("Megamod_UnsupportedDrug", 0.4f, 0.2f);
        unsupported.label = "A translated display name";
        unsupported.comps = nullComps ? null! : new List<CompProperties>();
        using var definitions = new ThingDefScope(supported, unsupported);

        var failure = Assert.Throws<InvalidOperationException>(() => NativeDrugCatalog.Capture(_ => true));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("ThingDef 'Megamod_UnsupportedDrug'"));
            Assert.That(failure.Message, Does.Contain("CompProperties_Drug"));
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.EqualTo(new[] { supported, unsupported }));
            Assert.That(supported.IsAddictiveDrug, Is.True);
            Assert.That(supported.IsPleasureDrug, Is.True);
        });
    }

    [Test]
    public void CallerAvailabilityIsRequiredEvenForAnEmptyDatabase()
    {
        using var definitions = new ThingDefScope();

        var failure = Assert.Throws<ArgumentNullException>(() => NativeDrugCatalog.Capture(null!));

        Assert.That(failure!.ParamName, Is.EqualTo("isAvailable"));
    }

    [Test]
    public void ReplacementDefWithTheSameNameUsesTheCurrentRegisteredIdentity()
    {
        var old = Drug("Megamod_ReplacedDrug", 0.5f, 0.5f);
        IReadOnlyList<DrugCatalogEntry> previous;
        using (var oldDefinitions = new ThingDefScope(old))
            previous = NativeDrugCatalog.Capture(_ => true);
        var current = Drug("Megamod_ReplacedDrug", 0f, 0f);
        using var currentDefinitions = new ThingDefScope(current);

        var catalog = NativeDrugCatalog.Capture(_ => true);
        var plan = DrugPolicyReconciler.Plan(new[]
        {
            new DrugPolicyRow(new DrugPolicyEntry { drug = old }, old,
                new DrugPolicyChoices(true, true, true, 2f, 0.3f, 0.4f, 5))
        }, catalog, rows => rows.SortBy(row => ((ThingDef)row.DrugIdentity!).GetCompProperties<CompProperties_Drug>().listOrder));

        Assert.Multiple(() =>
        {
            Assert.That(previous.Single().Identity, Is.SameAs(old));
            Assert.That(catalog.Single().Identity, Is.SameAs(current));
            Assert.That(plan.Removed, Is.EqualTo(1));
            Assert.That(plan.Added, Is.EqualTo(1));
            Assert.That(plan.Rows.Single().DrugIdentity, Is.SameAs(current));
            Assert.That(plan.Rows.Single().RowIdentity, Is.Null);
        });
    }

    [Test]
    public void SnapshotsKeepNativeEnumerationOrderAndDetachedPropertyValues()
    {
        var first = Drug("Megamod_Zulu", 0.4f, 0f);
        var second = Drug("Megamod_Alpha", 0f, 0.2f);
        first.GetCompProperties<CompProperties_Drug>().listOrder = 9f;
        second.GetCompProperties<CompProperties_Drug>().listOrder = 1f;
        using var definitions = new ThingDefScope(first, second);
        var before = NativeDrugCatalog.Capture(_ => true);
        first.GetCompProperties<CompProperties_Drug>().addictiveness = 0f;
        second.ingestible.joy = 0f;

        var after = NativeDrugCatalog.Capture(_ => true);

        Assert.Multiple(() =>
        {
            Assert.That(before.Select(entry => entry.Identity), Is.EqualTo(new[] { first, second }));
            Assert.That(after.Select(entry => entry.Identity), Is.EqualTo(new[] { first, second }));
            Assert.That(before[0].Addictive, Is.True);
            Assert.That(before[0].Pleasure, Is.False);
            Assert.That(before[1].Addictive, Is.False);
            Assert.That(before[1].Pleasure, Is.True);
            Assert.That(after.All(entry => !entry.Addictive && !entry.Pleasure), Is.True);
            Assert.That(((ICollection<DrugCatalogEntry>)before).IsReadOnly, Is.True);
        });
    }

    [Test]
    public void AvailabilityFailurePropagatesWithoutReturningAPartialCatalog()
    {
        var first = Drug("Megamod_FirstDrug", 0f, 0f);
        var second = Drug("Megamod_SecondDrug", 0f, 0f);
        using var definitions = new ThingDefScope(first, second);
        var expected = new InvalidOperationException("The caller's finalized availability is unavailable.");

        var failure = Assert.Throws<InvalidOperationException>(() => NativeDrugCatalog.Capture(definition =>
            ReferenceEquals(definition, second) ? throw expected : true));

        Assert.That(failure, Is.SameAs(expected));
    }

    private static ThingDef Drug(string name, float addictiveness, float joy) => new()
    {
        defName = name, category = ThingCategory.Item,
        ingestible = new IngestibleProperties { drugCategory = DrugCategory.Social, joy = joy },
        comps = new List<CompProperties> { new CompProperties_Drug { addictiveness = addictiveness, listOrder = 1f } }
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
