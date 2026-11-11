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
        owns = false;
    }

    [Test]
    public void PreparationCreatesCompleteFreshNativeRowsWithoutPublishingOrTouchingPriorPolicy()
    {
        var oldRows = new NativeDrugPolicyStorage(prior).Capture();
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order);
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

    [Test]
    public void ConfirmationPublishesThePreparedObjectOnceAndPreservesTheExistingDefault()
    {
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => true, Order);
        var prepared = confirmation.Prepare();
        Assert.That(confirmation.Confirm(), Is.SameAs(prepared));
        Assert.That(confirmation.Confirm(), Is.SameAs(prepared));
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, prepared }));
        Assert.That(prior.id, Is.EqualTo(7));
        Assert.That(prior[0].takeToInventoryTempBuffer, Is.EqualTo("007"));
    }

    [TestCase("receipt")]
    [TestCase("membership")]
    [TestCase("existing choice")]
    [TestCase("prepared choice")]
    [TestCase("prepared identity")]
    public void AChangedPreparationCannotPublishAndNeverOverwritesIndependentState(string change)
    {
        var current = true;
        var confirmation = new NativeDrugPolicyCreation(database, catalog, edits, prior.label, () => current, Order);
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
        });
        var created = confirmation.Confirm();
        Assert.That(database.AllPolicies, Is.EqualTo(new[] { prior, created }));
    }
}
