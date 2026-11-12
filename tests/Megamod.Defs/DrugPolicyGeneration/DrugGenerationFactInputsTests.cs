using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyGeneration.Runtime;
using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationFactInputsTests
{
    private ThingDef drug = null!, other = null!;
    private ChemicalDef chemical = null!;
    private HediffDef tolerance = null!, high = null!, addiction = null!;
    private CompProperties_Drug properties = null!;
    private IngestionOutcomeDoer_GiveHediff dose = null!;
    private HediffGiver_RandomDrugEffect secondary = null!;
    private SimpleCurve effectCurve = null!;
    private DrugCatalogRead catalog = null!;
    private bool definitionsCurrent, throwLifetime, owns;
    private readonly object humanIdentity = new(), raceIdentity = new(), stageListIdentity = new();
    private readonly object adultEntryIdentity = new(), adultDefIdentity = new();

    [SetUp]
    public void SetUp()
    {
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<ChemicalDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<HediffDef>.AllDefsListForReading, Is.Empty);
        owns = true;
        definitionsCurrent = true;
        throwLifetime = false;
        tolerance = new HediffDef
        {
            defName = "FactInputsTolerance", hediffClass = typeof(HediffWithComps), maxSeverity = 1,
            comps = new List<HediffCompProperties> { new HediffCompProperties_SeverityPerDay { severityPerDay = -.02f } },
            stages = new List<HediffStage> { new() }
        };
        high = new HediffDef
        {
            defName = "FactInputsHigh", hediffClass = typeof(Hediff_High), maxSeverity = 1,
            comps = new List<HediffCompProperties> { new HediffCompProperties_SeverityPerDay { severityPerDay = -3f } }
        };
        addiction = new HediffDef { defName = "FactInputsAddiction", hediffClass = typeof(Hediff_Addiction) };
        chemical = new ChemicalDef { defName = "FactInputsChemical", toleranceHediff = tolerance, addictionHediff = addiction };
        properties = new CompProperties_Drug { chemical = chemical, addictiveness = .02f, minToleranceToAddict = .15f };
        tolerance.comps.Add(new HediffCompProperties_DrugEffectFactor { chemical = chemical });
        secondary = new HediffGiver_RandomDrugEffect
        {
            hediff = addiction, minSeverity = .5f, baseMtbDays = 180,
            severityToMtbDaysCurve = new SimpleCurve(new[] { new CurvePoint(.5f, 180), new CurvePoint(1, 135) })
        };
        tolerance.hediffGivers = new List<HediffGiver> { secondary };
        dose = new IngestionOutcomeDoer_GiveHediff { hediffDef = tolerance, severity = .03f, toleranceChemical = chemical };
        drug = new ThingDef
        {
            defName = "FactInputsDrug", category = ThingCategory.Item, thingClass = typeof(ThingWithComps),
            comps = new List<CompProperties> { properties, new CompProperties_Forbiddable() },
            ingestible = new IngestibleProperties
            {
                drugCategory = DrugCategory.Social, joy = .2f, defaultNumToIngestAtOnce = 1, maxNumToIngestAtOnce = 1,
                outcomeDoers = new List<IngestionOutcomeDoer>
                {
                    new IngestionOutcomeDoer_GiveHediff { hediffDef = high, severity = .5f, toleranceChemical = chemical }, dose
                }
            }
        };
        other = new ThingDef { defName = "FactInputsOther" };
        DefDatabase<ThingDef>.Add(new[] { drug, other });
        DefDatabase<ChemicalDef>.Add(chemical);
        DefDatabase<HediffDef>.Add(new[] { tolerance, high, addiction });
        // Existing catalog publication is supplied explicitly. This fixture does not claim to own
        // native finalization or the root catalog publisher, and never calls DefOf or loads a mod.
        catalog = new DrugCatalogRead(new[] { new DrugCatalogEntry(drug, true, true) },
            () => throwLifetime ? throw new InvalidOperationException("unavailable fixture lifetime") : definitionsCurrent);
        effectCurve = new SimpleCurve(new[] { new CurvePoint(0, 1), new CurvePoint(1, .25f) });
    }

    [TearDown]
    public void TearDown()
    {
        if (!owns) return;
        DefDatabase<ThingDef>.Clear();
        DefDatabase<ChemicalDef>.Clear();
        DefDatabase<HediffDef>.Clear();
        owns = false;
    }

    private HumanAdultInput FreshBaseline(float raceSize = 1, float adultFactor = 1,
        object? adultDef = null, float minimumAge = 18, int stage = (int)DevelopmentalStage.Adult) =>
        new(humanIdentity, raceIdentity, stageListIdentity, (int)Intelligence.Humanlike, raceSize,
            new[] { new HumanLifeStageInput(adultEntryIdentity, adultDef ?? adultDefIdentity, minimumAge, stage, adultFactor) }, adultEntryIdentity);
    private DrugGenerationFactInputHandles Fresh(IReadOnlyCollection<ThingDef>? partial = null,
        HumanAdultInput? baseline = null, SimpleCurve? curve = null) =>
        new(catalog, partial ?? Array.Empty<ThingDef>(), baseline ?? FreshBaseline(), curve ?? effectCurve);

    [Test]
    public void UnchangedSuppliedInputsMatchWithoutDiscoveringAnotherCatalog()
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        // A newly supplied detached human read is equal; it is not silently reused as native proof.
        Assert.That(read.MatchesCurrent(Fresh()), Is.True);
        var otherPublication = new DrugCatalogRead(catalog.Entries, () => true);
        Assert.That(read.MatchesCurrent(Fresh() with { Catalog = otherPublication }), Is.False);
        Assert.That(read.MatchesCurrent(Fresh()), Is.False, "A detected mismatch needs a new preview.");
    }


    [Test]
    public void EqualNumericalFactsCannotHideAReplacedOutcomeIdentity()
    {
        var facts = NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>());
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        drug.ingestible.outcomeDoers[1] = new IngestionOutcomeDoer_GiveHediff
        { hediffDef = tolerance, severity = dose.severity, toleranceChemical = chemical };
        Assert.That(NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>()), Is.EqualTo(facts));
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void GeneFlagChangeStalesTheReadEvenWhenOldFactProjectionCannotSeeIt()
    {
        var facts = NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>());
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        dose.multiplyByGeneToleranceFactors = !dose.multiplyByGeneToleranceFactors;
        Assert.That(NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>()), Is.EqualTo(facts));
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [TestCase("body flag")]
    [TestCase("comp identity")]
    [TestCase("comp class")]
    [TestCase("item class")]
    [TestCase("comp list identity")]
    [TestCase("outcome order")]
    [TestCase("outcome chance")]
    [TestCase("dose severity")]
    [TestCase("quantity")]
    [TestCase("catalog pleasure input")]
    [TestCase("ordering")]
    [TestCase("addiction identity")]
    [TestCase("hediff class")]
    [TestCase("decay factor")]
    [TestCase("stage identity")]
    [TestCase("stage minimum")]
    [TestCase("stage gain")]
    [TestCase("stage death branch")]
    [TestCase("stage thought branch")]
    [TestCase("post-add ego branch")]
    [TestCase("giver identity")]
    [TestCase("giver threshold")]
    [TestCase("secondary curve identity")]
    [TestCase("secondary curve point")]
    public void ConsumedShapeOrValueChangeRequiresARefreshedPreview(string change)
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        switch (change)
        {
            case "body flag":
                typeof(IngestionOutcomeDoer_GiveHediff).GetField("divideByBodySize", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dose, true); break;
            case "comp identity": drug.comps[0] = new CompProperties_Drug
                { chemical = chemical, addictiveness = properties.addictiveness, minToleranceToAddict = properties.minToleranceToAddict }; break;
            case "comp class": properties.compClass = typeof(UnknownDrugComp); break;
            case "item class": drug.thingClass = typeof(Thing); break;
            case "comp list identity": drug.comps = new List<CompProperties>(drug.comps); break;
            case "outcome order": drug.ingestible.outcomeDoers.Reverse(); break;
            case "outcome chance": dose.chance = .5f; break;
            case "dose severity": dose.severity += .01f; break;
            case "quantity": drug.ingestible.maxNumToIngestAtOnce = 2; break;
            case "catalog pleasure input": drug.ingestible.joy = 0; break;
            case "ordering": properties.listOrder += 1; break;
            case "addiction identity": chemical.addictionHediff = high; break;
            case "hediff class": tolerance.hediffClass = typeof(Hediff); break;
            case "decay factor": ((HediffCompProperties_SeverityPerDay)tolerance.comps[0]).mechanitorFactor = 2; break;
            case "stage identity": tolerance.stages[0] = new HediffStage(); break;
            case "stage minimum": tolerance.stages[0].minSeverity = .01f; break;
            case "stage gain": tolerance.stages[0].severityGainFactor = .5f; break;
            case "stage death branch": tolerance.stages[0].deathMtbDays = 1; break;
            case "stage thought branch": tolerance.stages[0].pctAllThoughtNullification = .5f; break;
            case "post-add ego branch": tolerance.clearsEgo = true; break;
            case "giver identity": tolerance.hediffGivers[0] = new HediffGiver_RandomDrugEffect
                { hediff = addiction, minSeverity = secondary.minSeverity, baseMtbDays = secondary.baseMtbDays,
                    severityToMtbDaysCurve = secondary.severityToMtbDaysCurve }; break;
            case "giver threshold": secondary.minSeverity += .1f; break;
            case "secondary curve identity": secondary.severityToMtbDaysCurve = new SimpleCurve(secondary.severityToMtbDaysCurve.Points); break;
            case "secondary curve point": secondary.severityToMtbDaysCurve.Points[0] = new CurvePoint(.6f, 180); break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        Assert.That(read.MatchesCurrent(Fresh()), Is.False, change);
    }

    [Test]
    public void OpaqueOutcomeNeverExecutesAndCannotBorrowPositiveShapeSupport()
    {
        drug.ingestible.outcomeDoers.Add(new UnknownOutcome());
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        Assert.That(read.MatchesCurrent(Fresh()), Is.True, "Stable inputs are not positive semantic admission.");
        Assert.That(NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>())[0].Support,
            Is.EqualTo(DrugGenerationSupport.UnsupportedEffects));
    }

    [Test]
    public void UnrelatedLabelsDoNotChangeTheConsumedPharmacologyInputs()
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        drug.label = "A translated drug label";
        tolerance.label = "A translated tolerance label";
        Assert.That(read.MatchesCurrent(Fresh()), Is.True);
    }

    [Test]
    public void NullAndEmptyNativeListsAreDistinctCapturedShapes()
    {
        tolerance.stages = null;
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        tolerance.stages = new List<HediffStage>();
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void ExactFloatBitsDoNotSilentlyNormalizeAChangedInput()
    {
        properties.largeOverdoseChance = 0f;
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        properties.largeOverdoseChance = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void PartialMembershipIsASetButAnyChangedIdentityRequiresRefresh()
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh(new[] { drug, other }));
        Assert.That(read.MatchesCurrent(Fresh(new[] { other, drug, drug })), Is.True);
        Assert.That(read.MatchesCurrent(Fresh(new[] { drug })), Is.False);
    }

    [TestCase("adult identity")]
    [TestCase("age boundary")]
    [TestCase("development stage")]
    [TestCase("compensating factors")]
    public void SuppliedHumanAdultSelectionAndIndividualFactorsMustRemainExact(string change)
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        var changed = change switch
        {
            "adult identity" => FreshBaseline(adultDef: new object()),
            "age boundary" => FreshBaseline(minimumAge: 20),
            "development stage" => FreshBaseline(stage: (int)DevelopmentalStage.Child),
            "compensating factors" => FreshBaseline(raceSize: 2, adultFactor: .5f),
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };
        Assert.That(read.MatchesCurrent(Fresh(baseline: changed)), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SuppliedEffectFactorCurveIdentityAndMutablePointsBothRequireRefresh(bool replace)
    {
        var facts = NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>());
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        if (replace) effectCurve = new SimpleCurve(effectCurve.Points);
        else effectCurve.Points[1] = new CurvePoint(1, .5f);
        Assert.That(NativeDrugGenerationFacts.Capture(catalog.Entries, Array.Empty<ThingDef>()), Is.EqualTo(facts));
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
        // This supplied fixture object was NEVER bound to the actual native static field.
    }

    [TestCase("human")]
    [TestCase("race")]
    [TestCase("list")]
    [TestCase("adult entry")]
    public void EqualBaselineNumbersCannotBorrowReplacedNativeIdentities(string changed)
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        object adult = changed == "adult entry" ? new object() : adultEntryIdentity;
        var baseline = new HumanAdultInput(changed == "human" ? new object() : humanIdentity,
            changed == "race" ? new object() : raceIdentity, changed == "list" ? new object() : stageListIdentity,
            (int)Intelligence.Humanlike, 1,
            new[] { new HumanLifeStageInput(adult, adultDefIdentity, 18, (int)DevelopmentalStage.Adult, 1) }, adult);
        Assert.That(read.MatchesCurrent(Fresh(baseline: baseline)), Is.False);
    }

    [Test]
    public void SuppliedHumanReadDetachesItsStageListWithoutClaimingNativeBinding()
    {
        var stages = new[] { new HumanLifeStageInput(adultEntryIdentity, adultDefIdentity, 18, (int)DevelopmentalStage.Adult, 1) };
        var baseline = new HumanAdultInput(humanIdentity, raceIdentity, stageListIdentity,
            (int)Intelligence.Humanlike, 1, stages, adultEntryIdentity);
        stages[0] = stages[0] with { BodySizeFactor = 2 };
        using var read = DrugGenerationFactInputs.Capture(Fresh(baseline: baseline));
        Assert.That(read.MatchesCurrent(Fresh()), Is.True);
        Assert.That(() => ((IList<HumanLifeStageInput>)baseline.LifeStages).Clear(), Throws.TypeOf<NotSupportedException>());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExpiredOrThrowingCatalogLifetimePermanentlyRevokesTheRead(bool throws)
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        definitionsCurrent = false;
        throwLifetime = throws;
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
        definitionsCurrent = true;
        throwLifetime = false;
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void DisposalRefusesFurtherMatches()
    {
        var read = DrugGenerationFactInputs.Capture(Fresh());
        read.Dispose();
        read.Dispose();
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void OversizedInputRefusesCaptureWithoutReturningATruncatedReceipt()
    {
        var oversized = new DrugCatalogRead(Enumerable.Repeat(catalog.Entries[0], 4097), () => true);
        Assert.That(() => DrugGenerationFactInputs.Capture(Fresh() with { Catalog = oversized }),
            Throws.InvalidOperationException.With.Message.Contains("capture bound"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableInitialLifetimeCannotPublishAReceipt(bool throws)
    {
        definitionsCurrent = false;
        throwLifetime = throws;
        Assert.That(() => DrugGenerationFactInputs.Capture(Fresh()), Throws.InvalidOperationException);
    }

    [Test]
    public void LifetimeExpiringAcrossCaptureOrComparisonCannotPublishAUsableRead()
    {
        int checks = 0;
        catalog = new DrugCatalogRead(catalog.Entries, () => ++checks < 2);
        Assert.That(() => DrugGenerationFactInputs.Capture(Fresh()), Throws.InvalidOperationException);
        checks = 0;
        catalog = new DrugCatalogRead(catalog.Entries, () => ++checks < 4);
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
        checks = 0;
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [TestCase("duplicate drug")]
    [TestCase("nonnative drug")]
    [TestCase("null partial member")]
    [TestCase("missing curve")]
    [TestCase("missing baseline")]
    public void MalformedSuppliedInputsCannotProduceAReceipt(string malformed)
    {
        var input = malformed switch
        {
            "duplicate drug" => Fresh() with { Catalog = new DrugCatalogRead(new[] { catalog.Entries[0], catalog.Entries[0] }, () => true) },
            "nonnative drug" => Fresh() with { Catalog = new DrugCatalogRead(new[] { new DrugCatalogEntry(new object(), true, true) }, () => true) },
            "null partial member" => Fresh(new ThingDef[] { null! }),
            "missing curve" => Fresh() with { EffectFactorCurve = null! },
            "missing baseline" => Fresh() with { HumanAdult = null! },
            _ => throw new ArgumentOutOfRangeException(nameof(malformed))
        };
        Assert.That(() => DrugGenerationFactInputs.Capture(input), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void AFailedCurrentReadCannotBecomeCurrentAgainAfterRepair()
    {
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        Assert.That(read.MatchesCurrent(Fresh() with { EffectFactorCurve = null! }), Is.False);
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
    }

    [Test]
    public void OneCollectionAcceptsItsCompleteLimitAndRefusesOneExtraEntry()
    {
        tolerance.removeWithTags = Enumerable.Repeat("not-applied-in-this-read", 4096).ToList();
        using var read = DrugGenerationFactInputs.Capture(Fresh());
        Assert.That(read.MatchesCurrent(Fresh()), Is.True, "Snapshot currentness does not admit this removal behavior.");
        tolerance.removeWithTags.Add("extra");
        Assert.That(read.MatchesCurrent(Fresh()), Is.False);
        Assert.That(() => DrugGenerationFactInputs.Capture(Fresh()), Throws.InvalidOperationException.With.Message.Contains("capture bound"));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void TotalEntryBudgetAcceptsItsExactLimitAndRefusesOneExtraEntry(int extra)
    {
        // 16 catalog entries + 1 supplied adult stage + 2 supplied curve points +
        // 15*4096 outcome entries + 4077 final outcome entries = 65536 captured entries.
        // These opaque unknown outcomes are data only; no valid pharmacology is claimed.
        var rows = new List<DrugCatalogEntry>();
        for (int index = 0; index < 16; index++)
        {
            var item = new ThingDef
            {
                defName = "FactInputBudget" + index, category = ThingCategory.Item, comps = null!,
                ingestible = new IngestibleProperties { outcomeDoers = Enumerable.Repeat<IngestionOutcomeDoer>(
                    new UnknownOutcome(), index < 15 ? 4096 : 4077 + extra).ToList() }
            };
            DefDatabase<ThingDef>.Add(item);
            rows.Add(new DrugCatalogEntry(item, false, false));
        }
        catalog = new DrugCatalogRead(rows, () => true);
        if (extra == 1)
            Assert.That(() => DrugGenerationFactInputs.Capture(Fresh()), Throws.InvalidOperationException.With.Message.Contains("capture bound"));
        else
        {
            using var read = DrugGenerationFactInputs.Capture(Fresh());
            Assert.That(read.MatchesCurrent(Fresh()), Is.True);
        }
    }

    [Test]
    public void HumanInputRequiresOneSelectedEntryAndDoesNotEnumerateBeyondItsBound()
    {
        var stage = new HumanLifeStageInput(adultEntryIdentity, adultDefIdentity, 18, (int)DevelopmentalStage.Adult, 1);
        Assert.That(() => new HumanAdultInput(humanIdentity, raceIdentity, stageListIdentity,
            (int)Intelligence.Humanlike, 1, Array.Empty<HumanLifeStageInput>(), adultEntryIdentity), Throws.ArgumentException);
        Assert.That(() => new HumanAdultInput(humanIdentity, raceIdentity, stageListIdentity,
            (int)Intelligence.Humanlike, 1, new[] { stage, stage }, adultEntryIdentity), Throws.ArgumentException);
        Assert.That(() => new HumanAdultInput(humanIdentity, raceIdentity, stageListIdentity,
            (int)Intelligence.Humanlike, 1, EndlessStages(stage), adultEntryIdentity), Throws.ArgumentException.With.Message.Contains("capture bound"));
    }

    private static IEnumerable<HumanLifeStageInput> EndlessStages(HumanLifeStageInput stage)
    {
        for (int count = 0; count < 4097; count++) yield return stage;
        throw new AssertionException("Human input enumerated beyond its declared bound.");
    }

    private sealed class UnknownDrugComp : CompDrug { }
    private sealed class UnknownOutcome : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount) =>
            throw new AssertionException("A snapshot must never execute an outcome.");
    }
}

