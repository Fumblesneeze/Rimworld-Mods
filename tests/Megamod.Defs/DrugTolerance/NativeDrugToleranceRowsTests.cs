using System.Reflection;
using Megamod.Features.DrugTolerance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugToleranceRowsTests
{
    private static readonly FieldInfo Entries = typeof(DrugPolicy).GetField("entriesInt", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private ThingDef drug = null!;
    private bool owns;
    [SetUp]
    public void SetUp()
    {
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        owns = true;
        drug = new ThingDef { defName = "Megamod_ToleranceContextDrug", category = ThingCategory.Item };
        DefDatabase<ThingDef>.Add(drug);
    }
    [TearDown]
    public void TearDown() { if (owns) DefDatabase<ThingDef>.Clear(); owns = false; }
    [Test]
    public void AmbiguousRowsCannotBorrowTheFirstRowsChoice()
    {
        var policy = new DrugPolicy(); var first = new DrugPolicyEntry { drug = drug };
        Entries.SetValue(policy, new List<DrugPolicyEntry> { first, new() { drug = drug } });
        Assert.That(NativeDrugToleranceRows.FindUnique(policy, drug), Is.Null);
        Assert.That(NativeDrugToleranceRows.IsCurrent(policy, first, drug), Is.False);
    }
    [Test]
    public void ReplacementRowsInvalidateTheOriginalIdentityEvenForTheSameDrug()
    {
        var policy = new DrugPolicy(); var first = new DrugPolicyEntry { drug = drug };
        Entries.SetValue(policy, new List<DrugPolicyEntry> { first });
        Assert.That(NativeDrugToleranceRows.IsCurrent(policy, first, drug), Is.True);
        var replacement = new DrugPolicyEntry { drug = drug };
        Entries.SetValue(policy, new List<DrugPolicyEntry> { replacement });
        Assert.That(NativeDrugToleranceRows.IsCurrent(policy, first, drug), Is.False);
        Assert.That(NativeDrugToleranceRows.FindUnique(policy, drug), Is.SameAs(replacement));
    }
    [Test]
    public void MissingStorageAndRemovedRowsRemainUnavailable()
    {
        var policy = new DrugPolicy(); var row = new DrugPolicyEntry { drug = drug };
        Assert.That(NativeDrugToleranceRows.FindUnique(policy, drug), Is.Null);
        var rows = new List<DrugPolicyEntry> { row }; Entries.SetValue(policy, rows);
        rows.Clear();
        Assert.That(NativeDrugToleranceRows.IsCurrent(policy, row, drug), Is.False);
    }
}
