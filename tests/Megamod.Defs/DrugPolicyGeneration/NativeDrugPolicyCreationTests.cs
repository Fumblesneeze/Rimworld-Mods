using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyGeneration.Runtime;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class NativeDrugPolicyCreationTests
{
    private bool owns;
    private ThingDef first = null!, second = null!;
    private DrugPolicyDatabase database = null!;
    private DrugPolicy prior = null!;
    private IReadOnlyList<DrugCatalogEntry> catalog = null!;
    private DrugGenerationEdit[] edits = null!;

    [SetUp]
    public void SetUp()
    {
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<DrugPolicyDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<OrderedTakeGroupDef>.AllDefsListForReading, Is.Empty);
        owns = true;
        first = Drug("First", 20); second = Drug("Second", 5);
        DefDatabase<ThingDef>.Add(new[] { first, second });
        catalog = NativeDrugCatalog.Capture(_ => true);
        database = new DrugPolicyDatabase(); // Empty declared PolicyDef database: no startup presets.
        prior = new DrugPolicy { id = 7, label = "Same editable label" };
        new NativeDrugPolicyStorage(prior).Apply(DrugPolicyReconciler.Plan(null, catalog, Order));
        prior[0].takeToInventoryTempBuffer = "007";
        database.AllPolicies.Add(prior);
        edits = new[]
        {
            new DrugGenerationEdit(first, new(false, false, true, 4, .3f, .7f, 0), DrugToleranceChoice.Numeric(.2f)),
            new DrugGenerationEdit(second, new(true, true, false, 1, 1, 1, 0), DrugToleranceChoice.Any)
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (!owns) return;
        DefDatabase<ThingDef>.Clear();
        DefDatabase<DrugPolicyDef>.Clear();
        DefDatabase<OrderedTakeGroupDef>.Clear();
        owns = false;
    }

    [Test]
    public void PreparationCreatesCompleteFreshNativeRowsWithoutPublishingOrTouchingPriorPolicy()
    {
        var oldRows = new NativeDrugPolicyStorage(prior).Capture();
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        var prepared = confirmation.Prepare();
        Assert.Multiple(() =>
        {
            Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
            Assert.That(prepared, Is.Not.SameAs(prior));
            Assert.That(prepared.id, Is.EqualTo(8));
            Assert.That(prepared.sourceDef, Is.Null);
            Assert.That(prepared.label, Is.EqualTo(prior.label));
            Assert.That(prepared.Count, Is.EqualTo(2));
            Assert.That(prepared[0].drug, Is.SameAs(second));
            Assert.That(prepared[1].drug, Is.SameAs(first));
            Assert.That(new NativeDrugPolicyStorage(prepared).Capture().Rows!.Select(row => row!.Choices),
                Is.EqualTo(new[] { edits[1].Choices, edits[0].Choices }));
            Assert.That(NativeDrugToleranceState.Read(prepared[first]).Limit, Is.EqualTo(.2f));
            Assert.That(NativeDrugToleranceState.Read(prepared[second]).Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(prepared[0], Is.Not.SameAs(prior[0]));
            Assert.That(new NativeDrugPolicyStorage(prior).Capture().StorageIdentity, Is.SameAs(oldRows.StorageIdentity));
            Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
        });
    }

    private static ThingDef Drug(string name, float order) => new()
    {
        defName = name, category = ThingCategory.Item,
        ingestible = new IngestibleProperties { drugCategory = DrugCategory.Social, joy = .2f },
        comps = new List<CompProperties> { new CompProperties_Drug { addictiveness = .1f, listOrder = order } }
    };

    private static void Order(List<DrugPolicyRow> rows) =>
        rows.SortBy(row => ((ThingDef)row.DrugIdentity!).GetCompProperties<CompProperties_Drug>().listOrder);

    [TestCase("empty-name")]
    [TestCase("long-name")]
    [TestCase("name-punctuation")]
    [TestCase("name-newline")]
    [TestCase("missing-edit")]
    [TestCase("extra-edit")]
    [TestCase("duplicate-edit")]
    [TestCase("empty-catalog")]
    [TestCase("stale-capability")]
    [TestCase("inapplicable-addiction")]
    [TestCase("inapplicable-recreation")]
    [TestCase("frequency-low")]
    [TestCase("frequency-high")]
    [TestCase("frequency-nan")]
    [TestCase("mood-low")]
    [TestCase("joy-high")]
    [TestCase("joy-infinite")]
    [TestCase("inventory-negative")]
    [TestCase("inventory-over-native")]
    [TestCase("unresolved-tolerance")]
    public void InvalidFinalInputNeverPublishesOrChangesPriorState(string mode)
    {
        string name = "Reviewed name";
        var choices = edits[0].Choices;
        var tolerance = edits[0].Tolerance;
        switch (mode)
        {
            case "empty-name": name = ""; break;
            case "long-name": name = new string('n', 28); break;
            case "name-punctuation": name = "bad/name"; break;
            case "name-newline": name = "Reviewed\n"; break;
            case "frequency-low": choices = choices with { DaysFrequency = .09f }; break;
            case "frequency-high": choices = choices with { DaysFrequency = 25.1f }; break;
            case "frequency-nan": choices = choices with { DaysFrequency = float.NaN }; break;
            case "mood-low": choices = choices with { MoodThreshold = -.01f }; break;
            case "joy-high": choices = choices with { RecreationThreshold = 1.01f }; break;
            case "joy-infinite": choices = choices with { RecreationThreshold = float.PositiveInfinity }; break;
            case "inventory-negative": choices = choices with { InventoryTarget = -1 }; break;
            case "inventory-over-native": choices = choices with { InventoryTarget = 1 }; break;
            case "unresolved-tolerance": tolerance = DrugToleranceChoice.Decode("future:choice"); break;
            case "stale-capability": first.ingestible.joy = 0; break;
            case "inapplicable-addiction":
                first.GetCompProperties<CompProperties_Drug>().addictiveness = 0;
                catalog = NativeDrugCatalog.Capture(_ => true);
                choices = choices with { Addiction = true }; break;
            case "inapplicable-recreation":
                first.ingestible.joy = 0;
                catalog = NativeDrugCatalog.Capture(_ => true);
                choices = choices with { Recreation = true }; break;
        }
        edits[0] = edits[0] with { Choices = choices, Tolerance = tolerance };
        if (mode == "missing-edit") edits = new[] { edits[0] };
        if (mode == "extra-edit") edits = edits.Append(new DrugGenerationEdit(new object(), choices, tolerance)).ToArray();
        if (mode == "duplicate-edit") edits = edits.Append(edits[0]).ToArray();
        if (mode == "empty-catalog") { catalog = Array.Empty<DrugCatalogEntry>(); edits = Array.Empty<DrugGenerationEdit>(); }
        var rows = new NativeDrugPolicyStorage(prior).Capture();
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, name, () => true, Order, true);
        Assert.That(() => confirmation.Confirm(), Throws.Exception);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
        Assert.That(new NativeDrugPolicyStorage(prior).Capture().StorageIdentity, Is.SameAs(rows.StorageIdentity));
        Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
    }

    [Test]
    public void ConfirmationPublishesThePreparedObjectOnceAndPreservesTheExistingDefault()
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        var prepared = confirmation.Prepare();
        Assert.That(confirmation.Confirm(), Is.SameAs(prepared));
        Assert.That(confirmation.Confirm(), Is.SameAs(prepared));
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, prepared }));
        Assert.That(prior.id, Is.EqualTo(7));
        Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
    }

    [Test]
    public void NumericToleranceCannotBePublishedWhenCurrentSupportIsUnavailable()
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, false);
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
    }

    [TestCase("wrong-choice")]
    [TestCase("wrong-tolerance")]
    [TestCase("missing")]
    [TestCase("duplicate")]
    [TestCase("borrowed")]
    [TestCase("reordered")]
    [TestCase("wrong-identity")]
    [TestCase("wrong-label")]
    [TestCase("wrong-drug")]
    [TestCase("extra")]
    [TestCase("throw-before")]
    [TestCase("throw-after")]
    public void PreparationMustProveTheExactIntendedFreshPayload(string fault)
    {
        var original = new NativeDrugPolicyStorage(prior).Capture();
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true,
            candidate => new PreparationFault(candidate, prior, fault));
        Assert.That(() => confirmation.Confirm(), Throws.Exception);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
        Assert.That(new NativeDrugPolicyStorage(prior).Capture().StorageIdentity, Is.SameAs(original.StorageIdentity));
        Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
    }

    private sealed class PreparationFault(DrugPolicy policy, DrugPolicy prior, string fault) : IDrugPolicyStorage
    {
        private readonly NativeDrugPolicyStorage storage = new(policy);
        public DrugPolicyStorageSnapshot Capture() => storage.Capture();
        public void Restore(DrugPolicyStorageSnapshot snapshot) => storage.Restore(snapshot);
        public void Apply(DrugPolicyPlan plan)
        {
            if (fault == "throw-before") throw new InvalidOperationException("Owned preparation fault.");
            storage.Apply(plan);
            var entries = (List<DrugPolicyEntry>)storage.Capture().StorageIdentity!;
            if (fault == "wrong-choice") entries[0].daysFrequency += 1;
            if (fault == "wrong-tolerance") NativeDrugToleranceState.Write(entries[0], DrugToleranceChoice.Numeric(.9f));
            if (fault == "missing") entries.RemoveAt(0);
            if (fault == "duplicate") entries[1] = entries[0];
            if (fault == "borrowed") entries[0] = prior[0];
            if (fault == "reordered") entries.Reverse();
            if (fault == "wrong-identity") policy.id = prior.id;
            if (fault == "wrong-label") policy.label = "Unreviewed label";
            if (fault == "wrong-drug") entries[0].drug = entries[1].drug;
            if (fault == "extra") entries.Add(new DrugPolicyEntry { drug = entries[0].drug });
            if (fault == "throw-after") throw new InvalidOperationException("Owned preparation fault.");
        }
    }

    [TestCase("before")]
    [TestCase("prepared")]
    [TestCase("during-ordering")]
    [TestCase("during-currentness")]
    [TestCase("published")]
    public void DisposalInvalidatesTheConfirmationWithoutDeletingPublishedOrIndependentState(string phase)
    {
        NativeDrugPolicyCreation? confirmation = null;
        DrugPolicy? published = null;
        confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () =>
        {
            if (phase == "during-currentness") confirmation!.Dispose();
            return true;
        }, rows =>
        {
            if (phase == "during-ordering") confirmation!.Dispose();
            Order(rows);
        }, true);
        if (phase == "prepared") confirmation.Prepare();
        if (phase == "published") published = confirmation.Confirm();
        if (!phase.StartsWith("during", StringComparison.Ordinal)) confirmation.Dispose();
        Assert.That(() => confirmation.Confirm(), Throws.Exception);
        confirmation.Dispose();
        Assert.That(() => confirmation.Prepare(), Throws.Exception);
        Assert.That(database.AllPolicies, Is.EqualTo(published == null ? new[] { prior } : new[] { prior, published }));
        Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
    }

    [Test]
    public void DisposalReleasesAnUnpublishedPreparedPolicy()
    {
        var retained = PrepareWeakPolicy(database, catalog, edits);
        retained.Confirmation.Dispose();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.That(retained.Policy.IsAlive, Is.False);
        GC.KeepAlive(retained.Confirmation);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PublicationPostcheckRollsBackOnlyItsOwnedTailAndRefusesIndependentConflicts(bool independentConflict)
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        var candidate = confirmation.Prepare();
        var publication = confirmation.CreatePublication();
        publication.Append();
        candidate[first].daysFrequency += 1;
        if (independentConflict) prior[0].takeToInventory = 19;
        if (independentConflict)
        {
            Assert.That(() => publication.VerifyOrRollback(), Throws.TypeOf<AggregateException>());
            Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, candidate }));
            Assert.That(prior[0].takeToInventory, Is.EqualTo(19));
        }
        else
        {
            Assert.That(() => publication.VerifyOrRollback(), Throws.InvalidOperationException);
            Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
            Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
        }
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(() => publication.Append(), Throws.InvalidOperationException);
    }

    [TestCase(-7, 1)]
    [TestCase(0, 1)]
    [TestCase(100003, 100004)]
    public void CreationUsesOneMoreThanTheNonnegativeMaximumId(int existing, int expected)
    {
        prior.id = existing;
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        Assert.That(confirmation.Confirm().id, Is.EqualTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CreationRefusesEmptyNativeDefaultsOrExhaustedIds(bool exhausted)
    {
        if (exhausted) prior.id = int.MaxValue;
        else database.AllPolicies.Clear();
        var before = database.AllPolicies.ToArray();
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        Assert.That(() => confirmation.Confirm(), Throws.Exception);
        Assert.That(database.AllPolicies, Is.EqualTo(before));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OrderingFailureCannotPublishOrUndoIndependentChanges(bool independentChange)
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, _ =>
        {
            if (independentChange) prior[0].takeToInventory = 17;
            throw new InvalidOperationException("Owned ordering fault.");
        }, true);
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
        Assert.That(prior[0].takeToInventory, Is.EqualTo(independentChange ? 17 : 0));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChangedNativeCapabilityIsRefusedAfterPreparationOrOrdering(bool duringOrdering)
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, rows =>
        {
            Order(rows);
            if (duringOrdering) first.ingestible.joy = 0;
        }, true);
        if (!duringOrdering)
        {
            confirmation.Prepare();
            first.ingestible.joy = 0;
        }
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
        Assert.That(first.ingestible.joy, Is.Zero, "The independent changed definition must not be restored.");
    }

    [TestCase(.1f)]
    [TestCase(.5f)]
    [TestCase(1.5f)]
    [TestCase(25f)]
    public void ExactReviewedNativeBoundaryChoicesArePreserved(float frequency)
    {
        var group = new OrderedTakeGroupDef { defName = "OwnedPickup", max = 7 };
        DefDatabase<OrderedTakeGroupDef>.Add(group);
        first.orderedTakeGroup = group;
        edits[0] = edits[0] with { Choices = edits[0].Choices with
        {
            DaysFrequency = frequency, MoodThreshold = 0, RecreationThreshold = 1, InventoryTarget = 7
        }, Tolerance = DrugToleranceChoice.Numeric(1) };
        edits[1] = edits[1] with { Tolerance = DrugToleranceChoice.Numeric(0) };
        var name = new string('é', 27);
        using var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, name, () => true, Order, true);
        var result = confirmation.Confirm();
        Assert.That(result.label, Is.EqualTo(name));
        Assert.That(result[first].daysFrequency, Is.EqualTo(frequency), "Commit must not round the reviewed native slider value.");
        Assert.That(result[first].takeToInventory, Is.EqualTo(7));
        Assert.That(result[first].onlyIfMoodBelow, Is.Zero);
        Assert.That(result[first].onlyIfJoyBelow, Is.EqualTo(1));
        Assert.That(NativeDrugToleranceState.Read(result[first]).Limit, Is.EqualTo(1));
        Assert.That(NativeDrugToleranceState.Read(result[second]).Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        Assert.That(NativeDrugToleranceState.Read(result[second]).Limit, Is.Zero);
    }

    [Test]
    public void CompleteUnknownOffSchedulePreviewCanPublishWithAnyWhileToleranceIsUnavailable()
    {
        var facts = NativeDrugGenerationFacts.Capture(catalog, Array.Empty<ThingDef>());
        var suggestions = DrugPolicySuggestions.Create(facts, 1);
        Assert.That(suggestions.Rows.All(row => !row.Choices.Scheduled), Is.True);
        var complete = suggestions.Rows.Select(row => new DrugGenerationEdit(row.Facts.Drug.Identity, row.Choices, row.Tolerance)).ToArray();
        using var confirmation = new NativeDrugPolicyCreation(database, catalog, complete, " ", () => true, Order, false);
        var result = confirmation.Confirm();
        Assert.That(result.label, Is.EqualTo(" "), "Native spaces are retained rather than silently trimmed.");
        Assert.That(result.Count, Is.EqualTo(catalog.Count));
        foreach (var edit in complete)
        {
            var row = result[(ThingDef)edit.DrugIdentity];
            Assert.That(row.daysFrequency, Is.EqualTo(1));
            Assert.That(row.onlyIfMoodBelow, Is.EqualTo(1));
            Assert.That(row.onlyIfJoyBelow, Is.EqualTo(1));
            Assert.That(row.takeToInventory, Is.Zero);
            Assert.That(NativeDrugToleranceState.Read(row).Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        }
    }

    [Test]
    public void LoweredCurrentInventoryBoundInvalidatesAnAlreadyPreparedChoice()
    {
        var group = new OrderedTakeGroupDef { defName = "OwnedPickup", max = 7 };
        DefDatabase<OrderedTakeGroupDef>.Add(group);
        first.orderedTakeGroup = group;
        edits[0] = edits[0] with { Choices = edits[0].Choices with { InventoryTarget = 7 } };
        using var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        confirmation.Prepare();
        group.max = 6;
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior }));
        Assert.That(group.max, Is.EqualTo(6));
    }

    [Test]
    public void NextIdUsesTheCompleteExistingMembershipWithoutChangingItsDefaultOrOrder()
    {
        var other = new DrugPolicy { id = 100003, label = "Other" };
        database.AllPolicies.Add(other);
        using var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order, true);
        var result = confirmation.Confirm();
        Assert.That(result.id, Is.EqualTo(100004));
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, other, result }));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (NativeDrugPolicyCreation Confirmation, WeakReference Policy) PrepareWeakPolicy(
        DrugPolicyDatabase database, IReadOnlyList<DrugCatalogEntry> catalog, DrugGenerationEdit[] edits)
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, "Disposable", () => true, Order, true);
        return (confirmation, new WeakReference(confirmation.Prepare()));
    }

    [TestCase("receipt")]
    [TestCase("membership")]
    [TestCase("existing choice")]
    [TestCase("prepared choice")]
    [TestCase("prepared identity")]
    public void AChangedPreparationCannotPublishAndNeverOverwritesIndependentState(string change)
    {
        var current = true;
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => current, Order, true);
        var prepared = confirmation.Prepare();
        if (change == "receipt") current = false;
        if (change == "membership") database.AllPolicies.Add(new DrugPolicy { id = 15, label = "Independent" });
        if (change == "existing choice") prior[0].takeToInventory = 9;
        if (change == "prepared choice") prepared[first].daysFrequency = 1;
        if (change == "prepared identity") prepared.id = 7;
        Assert.That(() => confirmation.Confirm(), Throws.InvalidOperationException);
        Assert.That(database.AllPolicies.Contains(prepared), Is.False);
        if (change == "membership") Assert.That(database.AllPolicies.Count, Is.EqualTo(2));
        if (change == "existing choice") Assert.That(prior[0].takeToInventory, Is.EqualTo(9));
    }

    [Test]
    public void ReentrantConfirmationDuringOrderingIsRefusedWithoutCreatingADuplicate()
    {
        NativeDrugPolicyCreation? confirmation = null;
        var attempted = false;
        confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, rows =>
        {
            if (!attempted)
            {
                attempted = true;
                Assert.That(() => confirmation!.Confirm(), Throws.InvalidOperationException);
            }
            Order(rows);
        }, true);
        var created = confirmation.Confirm();
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, created }));
    }
}
