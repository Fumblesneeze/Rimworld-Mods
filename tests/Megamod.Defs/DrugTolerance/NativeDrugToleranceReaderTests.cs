using Megamod.Features.DrugTolerance.Runtime;
using System.Reflection;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugToleranceReaderTests
{
    private bool owns;
    private HediffDef tolerance = null!;
    private ChemicalDef chemical = null!;
    private ThingDef first = null!, second = null!;

    [SetUp]
    public void SetUp()
    {
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<ChemicalDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<HediffDef>.AllDefsListForReading, Is.Empty);
        owns = true;
        tolerance = new HediffDef { defName = "Megamod_TestTolerance", minSeverity = -2f, maxSeverity = 2f };
        chemical = new ChemicalDef { defName = "Megamod_TestChemical", toleranceHediff = tolerance };
        first = Drug("Megamod_TestDrugFirst"); second = Drug("Megamod_TestDrugSecond");
        DefDatabase<HediffDef>.Add(tolerance);
        DefDatabase<ChemicalDef>.Add(chemical);
        DefDatabase<ThingDef>.Add(new[] { first, second });
    }

    [TearDown]
    public void TearDown()
    {
        if (!owns) return;
        DefDatabase<ThingDef>.Clear(); DefDatabase<ChemicalDef>.Clear(); DefDatabase<HediffDef>.Clear();
        owns = false;
    }

    [Test]
    public void DifferentDrugsWithOneRecognizedChemicalReadTheSameCurrentTolerance()
    {
        var hediff = new Hediff { def = tolerance, Severity = 0.271f };
        Assert.That(NativeDrugToleranceReader.Resolve(first), Is.SameAs(tolerance));
        Assert.That(NativeDrugToleranceReader.Resolve(second), Is.SameAs(tolerance));
        var firstRead = NativeDrugToleranceReader.Read(first, new[] { hediff });
        var secondRead = NativeDrugToleranceReader.Read(second, new[] { hediff });
        Assert.That(firstRead.Available && secondRead.Available, Is.True);
        Assert.That(firstRead.Severity, Is.EqualTo(0.271f));
        Assert.That(secondRead.Severity, Is.EqualTo(firstRead.Severity));
    }

    [Test]
    public void KnownAbsentToleranceIsZeroButAbsentHealthDataIsUnavailable()
    {
        var absent = NativeDrugToleranceReader.Read(first, Array.Empty<Hediff>());
        Assert.That(absent.Available, Is.True);
        Assert.That(absent.Severity, Is.Zero);
        Assert.That(NativeDrugToleranceReader.Read(first, null).Available, Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void UnrecognizedOrNonaddictiveDrugsDoNotAcquireInventedTolerance(int missing)
    {
        if (missing == 0) first.ingestible.outcomeDoers.Clear();
        if (missing == 1) chemical.toleranceHediff = null;
        if (missing == 2) first.GetCompProperties<CompProperties_Drug>().addictiveness = 0;
        if (missing == 3) first.ingestible.drugCategory = DrugCategory.None;
        Assert.That(NativeDrugToleranceReader.Resolve(first), Is.Null);
        Assert.That(NativeDrugToleranceReader.Read(first, Array.Empty<Hediff>()).Available, Is.False);
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(-0.1f)]
    public void InvalidNativeSeverityRemainsUnavailableRatherThanZero(float value)
    {
        var hediff = new Hediff { def = tolerance };
        // Malformed native saved state is a fixture precondition; bypass only the severity setter
        // which would otherwise clamp it before the reader could exercise its failure contract.
        typeof(Hediff).GetField("severityInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(hediff, value);
        Assert.That(NativeDrugToleranceReader.Read(first, new[] { hediff }).Available, Is.False);
    }

    [Test]
    public void MalformedNullHealthEntryDoesNotBecomeKnownAbsentTolerance()
    {
        Assert.That(NativeDrugToleranceReader.Read(first, new Hediff[] { null! }).Available, Is.False);
    }

    [Test]
    public void EachSelectionReadsTheCurrentNativeSeverityAndKeepsItsFirstMatchingHediff()
    {
        var current = new Hediff { def = tolerance, Severity = 0.2f };
        var later = new Hediff { def = tolerance, Severity = 0.8f };
        Assert.That(NativeDrugToleranceReader.Read(first, new[] { current, later }).Severity, Is.EqualTo(0.2f));
        current.Severity = 0.6f;
        Assert.That(NativeDrugToleranceReader.Read(first, new[] { current, later }).Severity, Is.EqualTo(0.6f));
    }

    private ThingDef Drug(string name) => new()
    {
        defName = name, category = ThingCategory.Item,
        comps = new List<CompProperties> { new CompProperties_Drug { chemical = chemical, addictiveness = 0.1f } },
        ingestible = new IngestibleProperties
        {
            drugCategory = DrugCategory.Social,
            outcomeDoers = new List<IngestionOutcomeDoer> { new IngestionOutcomeDoer_GiveHediff { hediffDef = tolerance } }
        }
    };
}
