using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyGeneration.Runtime;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class NativeDrugGenerationFactsTests
{
    private bool owns;
    private HediffDef tolerance = null!, high = null!, addiction = null!;
    private ChemicalDef chemical = null!;
    private ThingDef drug = null!;

    [SetUp]
    public void SetUp()
    {
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<ChemicalDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<HediffDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<NeedDef>.AllDefsListForReading, Is.Empty);
        Assert.That(DefDatabase<BodyPartDef>.AllDefsListForReading, Is.Empty);
        owns = true;
        tolerance = new HediffDef
        {
            defName = "GenerationToleranceFixture", hediffClass = typeof(HediffWithComps), maxSeverity = 1f,
            comps = new List<HediffCompProperties> { new HediffCompProperties_SeverityPerDay { severityPerDay = -0.02f } }
        };
        high = new HediffDef
        {
            defName = "GenerationHighFixture", hediffClass = typeof(Hediff_High), maxSeverity = 1f,
            comps = new List<HediffCompProperties> { new HediffCompProperties_SeverityPerDay { severityPerDay = -3f } }
        };
        addiction = new HediffDef { defName = "GenerationAddictionFixture", hediffClass = typeof(Hediff_Addiction) };
        chemical = new ChemicalDef { defName = "GenerationChemicalFixture", toleranceHediff = tolerance, addictionHediff = addiction };
        tolerance.comps.Add(new HediffCompProperties_DrugEffectFactor { chemical = chemical });
        drug = new ThingDef
        {
            defName = "UnrelatedDisplayName", category = ThingCategory.Item, thingClass = typeof(ThingWithComps),
            comps = new List<CompProperties>
            {
                new CompProperties_Drug { chemical = chemical, addictiveness = 0.02f, minToleranceToAddict = 0.15f },
                new CompProperties_Forbiddable()
            },
            ingestible = new IngestibleProperties
            {
                drugCategory = DrugCategory.Social, joy = 0.3f, defaultNumToIngestAtOnce = 1, maxNumToIngestAtOnce = 1,
                outcomeDoers = new List<IngestionOutcomeDoer>
                {
                    new IngestionOutcomeDoer_GiveHediff { hediffDef = high, severity = 0.5f, toleranceChemical = chemical },
                    new IngestionOutcomeDoer_GiveHediff { hediffDef = tolerance, severity = 0.032f, toleranceChemical = chemical }
                }
            }
        };
        DefDatabase<HediffDef>.Add(new[] { tolerance, high, addiction });
        DefDatabase<ChemicalDef>.Add(chemical);
        DefDatabase<ThingDef>.Add(drug);
    }

    [TearDown]
    public void TearDown()
    {
        if (!owns) return;
        DefDatabase<ThingDef>.Clear();
        DefDatabase<ChemicalDef>.Clear();
        DefDatabase<HediffDef>.Clear();
        DefDatabase<NeedDef>.Clear();
        DefDatabase<BodyPartDef>.Clear();
        owns = false;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WholeBodyOutcomeRejectsBodyPartOnlyPostMakeMetadata(bool sideHigh)
    {
        var part = new BodyPartDef { defName = "GenerationBodyPartFixture" };
        DefDatabase<BodyPartDef>.Add(part);
        (sideHigh ? high : tolerance).onlyLifeThreateningTo = new List<BodyPartDef> { part };
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.Multiple(() =>
        {
            Assert.That(facts[0].Support, Is.EqualTo(sideHigh
                ? DrugGenerationSupport.UnsupportedEffects : DrugGenerationSupport.UnsupportedDecay));
            Assert.That(DrugPolicySuggestions.Create(facts, 1).Rows[0].Choices.Scheduled, Is.False);
        });
    }

    [Test]
    public void CapturedNativeShapeProducesDetachedPositiveSuggestionWithoutCatalogRescan()
    {
        var catalog = NativeDrugCatalog.Capture(_ => true);
        // A later registered fixture is not part of this already captured catalog.
        DefDatabase<ThingDef>.Add(new ThingDef
        {
            defName = "NotInCapturedCatalog", category = ThingCategory.Item,
            comps = new List<CompProperties> { new CompProperties_Drug() },
            ingestible = new IngestibleProperties { drugCategory = DrugCategory.Social }
        });
        var facts = NativeDrugGenerationFacts.Capture(catalog, Array.Empty<ThingDef>());
        var suggestion = DrugPolicySuggestions.Create(facts, 1f).Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(facts.Count, Is.EqualTo(1));
            Assert.That(facts[0].Drug, Is.SameAs(catalog[0]));
            Assert.That(facts[0].ChemicalIdentity, Is.SameAs(chemical));
            Assert.That(facts[0].ToleranceIdentity, Is.SameAs(tolerance));
            Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.OrdinaryWholeDose));
            Assert.That(suggestion.Reason, Is.EqualTo(DrugSuggestionReason.ModeledSchedule));
            Assert.That(suggestion.Choices.DaysFrequency, Is.EqualTo(3f));
            Assert.That(suggestion.Choices.Addiction, Is.True);
            Assert.That(suggestion.Choices.Recreation, Is.False);
        });
        drug.ingestible.outcomeDoers.Clear();
        tolerance.comps.Clear();
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[0], Is.EqualTo(suggestion));
    }

    [TestCase("custom thing", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("extra comp", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("duplicate drug comp", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("custom drug comp", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("missing comp", DrugGenerationSupport.Unknown)]
    [TestCase("missing ingestible", DrugGenerationSupport.Unknown)]
    [TestCase("not drug", DrugGenerationSupport.Unknown)]
    [TestCase("quantity", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("partial", DrugGenerationSupport.UnresolvedPartialDose)]
    [TestCase("missing chemical", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("missing tolerance", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("missing addiction", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("missing giver", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("duplicate giver", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("custom giver", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("random giver", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("initial severity fallback", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("other modifier chemical", DrugGenerationSupport.UnsupportedTolerance)]
    [TestCase("unknown outcome", DrugGenerationSupport.UnsupportedEffects)]
    [TestCase("null outcome", DrugGenerationSupport.UnsupportedEffects)]
    public void UnsupportedQuantityOrEffectShapeRetainsRowAndReason(string change, DrugGenerationSupport expected)
    {
        var catalog = NativeDrugCatalog.Capture(_ => true);
        var giver = (IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[1];
        var props = drug.comps.OfType<CompProperties_Drug>().Single();
        switch (change)
        {
            case "custom thing": drug.thingClass = typeof(UnknownThing); break;
            case "extra comp": drug.comps.Add(new CompProperties { compClass = typeof(ThingComp) }); break;
            case "duplicate drug comp": drug.comps.Add(props); break;
            case "custom drug comp": props.compClass = typeof(UnknownDrugComp); break;
            case "missing comp": drug.comps.Clear(); break;
            case "missing ingestible": drug.ingestible = null; break;
            case "not drug": drug.ingestible.drugCategory = DrugCategory.None; break;
            case "quantity": drug.ingestible.defaultNumToIngestAtOnce = 2; break;
            case "missing chemical": props.chemical = null; break;
            case "missing tolerance": chemical.toleranceHediff = null; break;
            case "missing addiction": chemical.addictionHediff = null; break;
            case "missing giver": drug.ingestible.outcomeDoers.Remove(giver); break;
            case "duplicate giver": drug.ingestible.outcomeDoers.Add(giver); break;
            case "custom giver": drug.ingestible.outcomeDoers[1] = new UnknownGiver { hediffDef = tolerance, severity = 0.032f }; break;
            case "random giver": giver.chance = 0.5f; break;
            case "initial severity fallback": giver.severity = -1; break;
            case "other modifier chemical":
                giver.toleranceChemical = new ChemicalDef { defName = "OtherModifierFixture" };
                DefDatabase<ChemicalDef>.Add(giver.toleranceChemical);
                break;
            case "unknown outcome": drug.ingestible.outcomeDoers.Add(new UnknownOutcome()); break;
            case "null outcome": drug.ingestible.outcomeDoers.Add(null!); break;
        }
        var rows = NativeDrugGenerationFacts.Capture(catalog, change == "partial" ? new[] { drug } : Array.Empty<ThingDef>());
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].Drug, Is.SameAs(catalog[0]));
        Assert.That(rows[0].Support, Is.EqualTo(expected));
        Assert.That(DrugPolicySuggestions.Create(rows, 1f).Rows[0].Choices.Scheduled, Is.False);
    }

    private sealed class UnknownThing : ThingWithComps { }

    [TestCase("class")]
    [TestCase("minimum")]
    [TestCase("missing decay")]
    [TestCase("duplicate decay")]
    [TestCase("custom decay")]
    [TestCase("random decay")]
    [TestCase("reversed decay")]
    [TestCase("age gate")]
    [TestCase("mechanitor")]
    [TestCase("stage factor")]
    [TestCase("unknown comp")]
    [TestCase("other modifier")]
    [TestCase("null comp")]
    [TestCase("stage order")]
    [TestCase("side removal")]
    public void UnprovedToleranceOrDecayCannotBecomeAComputedSchedule(string change)
    {
        var decay = (HediffCompProperties_SeverityPerDay)tolerance.comps[0];
        switch (change)
        {
            case "class": tolerance.hediffClass = typeof(Hediff); break;
            case "minimum": tolerance.minSeverity = 0.01f; break;
            case "missing decay": tolerance.comps.RemoveAt(0); break;
            case "duplicate decay": tolerance.comps.Add(decay); break;
            case "custom decay": decay.compClass = typeof(UnknownDecay); break;
            case "random decay": decay.severityPerDayRange = new FloatRange(-0.01f, 0.01f); break;
            case "reversed decay": decay.reverseSeverityChangeChance = 0.1f; break;
            case "age gate": decay.minAge = 16; break;
            case "mechanitor": decay.mechanitorFactor = 2; break;
            case "stage factor": tolerance.stages = new List<HediffStage> { new() { severityGainFactor = 0.5f } }; break;
            case "unknown comp": tolerance.comps.Add(new HediffCompProperties { compClass = typeof(HediffComp) }); break;
            case "other modifier": ((HediffCompProperties_DrugEffectFactor)tolerance.comps[1]).chemical = null; break;
            case "null comp": tolerance.comps.Add(null!); break;
            case "stage order": tolerance.stages = new List<HediffStage> { new(), new() { minSeverity = 0.5f }, new() { minSeverity = 0.2f } }; break;
            case "side removal": tolerance.removeWithTags = new List<string> { "AnotherEffect" }; break;
        }
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.UnsupportedDecay));
        Assert.That(DrugPolicySuggestions.Create(facts, 1).Rows[0].Choices.Scheduled, Is.False);
    }

    private sealed class UnknownDecay : HediffComp_SeverityPerDay { }

    [Test]
    public void NativeRestOffsetAndRottableShapeKeepTheModeledSuggestion()
    {
        AddRestOffset();
        drug.comps.Add(new CompProperties_Rottable());
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.OrdinaryWholeDose));
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[0].Choices.Scheduled, Is.True);
    }

    [TestCase("high subclass")]
    [TestCase("duplicate high")]
    [TestCase("high random")]
    [TestCase("high initial fallback")]
    [TestCase("high body division")]
    [TestCase("high slow decay")]
    [TestCase("high unknown comp")]
    [TestCase("high root giver")]
    [TestCase("high stage giver")]
    [TestCase("high mental state")]
    [TestCase("high active effecter")]
    [TestCase("alcohol active hangover")]
    [TestCase("rest stack multiplier")]
    [TestCase("rest random")]
    [TestCase("rest missing need")]
    [TestCase("rest other need")]
    [TestCase("rest nonfinite")]
    public void UnprovedSideOutcomeIsNotBorrowedAsHarmless(string change)
    {
        var giver = (IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[0];
        switch (change)
        {
            case "high subclass": high.hediffClass = typeof(UnknownHigh); break;
            case "duplicate high": drug.ingestible.outcomeDoers.Add(giver); break;
            case "high random": giver.chance = 0.1f; break;
            case "high initial fallback": giver.severity = -1; break;
            case "high body division": SetBodyDivision(giver, true); break;
            case "high slow decay": ((HediffCompProperties_SeverityPerDay)high.comps[0]).severityPerDay = -0.01f; break;
            case "high unknown comp": high.comps.Add(new HediffCompProperties { compClass = typeof(HediffComp) }); break;
            case "high root giver": high.hediffGivers = new List<HediffGiver> { new HediffGiver_Random { hediff = tolerance } }; break;
            case "high stage giver": high.stages = new List<HediffStage> { new() { hediffGivers = new List<HediffGiver> { new HediffGiver_Random { hediff = tolerance } } } }; break;
            case "high mental state": high.stages = new List<HediffStage> { new() { mentalBreakMtbDays = 1 } }; break;
            case "high active effecter": high.comps.Add(new HediffCompProperties_Effecter()); break;
            case "alcohol active hangover":
                high.hediffClass = typeof(Hediff_Alcohol);
                high.stages = new List<HediffStage> { new(), new() { minSeverity = .1f }, new() { minSeverity = .2f }, new() { minSeverity = .3f } };
                break;
            default:
                var offset = AddRestOffset();
                if (change == "rest stack multiplier") offset.perIngested = true;
                if (change == "rest random") offset.chance = 0.5f;
                if (change == "rest missing need") offset.need = null;
                if (change == "rest other need") offset.need.needClass = typeof(Need_Food);
                if (change == "rest nonfinite") offset.offset = float.NaN;
                break;
        }
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.UnsupportedEffects));
    }

    private IngestionOutcomeDoer_OffsetNeed AddRestOffset()
    {
        var rest = new NeedDef { defName = "RestShapeFixture", needClass = typeof(Need_Rest) };
        DefDatabase<NeedDef>.Add(rest);
        var offset = new IngestionOutcomeDoer_OffsetNeed { need = rest, offset = .1f, toleranceChemical = chemical };
        drug.ingestible.outcomeDoers.Add(offset);
        return offset;
    }

    private static void SetBodyDivision(IngestionOutcomeDoer_GiveHediff giver, bool value) =>
        typeof(IngestionOutcomeDoer_GiveHediff).GetField("divideByBodySize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(giver, value);

    private sealed class UnknownHigh : Hediff_High { }

    [TestCase("curve equality", true)]
    [TestCase("curve above", true)]
    [TestCase("minimum equality", true)]
    [TestCase("reachable", false)]
    [TestCase("empty curve", false)]
    [TestCase("descending x", false)]
    [TestCase("duplicate x", false)]
    [TestCase("increasing y", false)]
    [TestCase("nan x", false)]
    [TestCase("zero y", false)]
    [TestCase("minimum below", false)]
    [TestCase("custom", false)]
    [TestCase("null", false)]
    [TestCase("active stage giver", false)]
    [TestCase("inactive stage giver", true)]
    public void SecondaryToleranceEffectsRequireTheExactModeledEarlyReturnBound(string shape, bool supported)
    {
        var giver = new HediffGiver_RandomDrugEffect
        {
            hediff = tolerance, minSeverity = 0.15f, baseMtbDays = 60,
            severityToMtbDaysCurve = new SimpleCurve(new[] { new CurvePoint(.15f, 100), new CurvePoint(1, 10) })
        };
        tolerance.hediffGivers = new List<HediffGiver> { giver };
        switch (shape)
        {
            case "curve above": giver.severityToMtbDaysCurve.Points[0] = new CurvePoint(.45f, 100); break;
            case "minimum equality": giver.severityToMtbDaysCurve = null; break;
            case "reachable": giver.severityToMtbDaysCurve.Points[0] = new CurvePoint(.02f, 100); break;
            case "empty curve": giver.severityToMtbDaysCurve.Points.Clear(); break;
            case "descending x": giver.severityToMtbDaysCurve.Points.Add(new CurvePoint(.1f, 5)); break;
            case "duplicate x": giver.severityToMtbDaysCurve.Points.Add(new CurvePoint(1, 5)); break;
            case "increasing y": giver.severityToMtbDaysCurve.Points[1] = new CurvePoint(1, 200); break;
            case "nan x": giver.severityToMtbDaysCurve.Points[0] = new CurvePoint(float.NaN, 100); break;
            case "zero y": giver.severityToMtbDaysCurve.Points[1] = new CurvePoint(1, 0); break;
            case "minimum below": giver.severityToMtbDaysCurve = null; giver.minSeverity = .02f; break;
            case "custom": tolerance.hediffGivers[0] = new UnknownSecondaryGiver(); break;
            case "null": tolerance.hediffGivers[0] = null!; break;
            case "active stage giver":
            case "inactive stage giver":
                tolerance.stages = new List<HediffStage> { new(), new()
                {
                    minSeverity = shape == "active stage giver" ? .01f : .15f,
                    hediffGivers = new List<HediffGiver> { new HediffGiver_Random { hediff = tolerance } }
                } };
                break;
        }
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support, Is.EqualTo(supported ? DrugGenerationSupport.OrdinaryWholeDose : DrugGenerationSupport.UnsupportedDecay));
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[0].Choices.Scheduled, Is.EqualTo(supported));
    }

    private sealed class UnknownSecondaryGiver : HediffGiver_RandomDrugEffect { }

    [Test]
    public void CapturedCatalogRequiresDistinctNativeIdentitiesAndCurrentFlags()
    {
        var catalog = NativeDrugCatalog.Capture(_ => true);
        Assert.That(() => NativeDrugGenerationFacts.Capture(new[] { catalog[0], catalog[0] }, Array.Empty<ThingDef>()), Throws.ArgumentException);
        Assert.That(() => NativeDrugGenerationFacts.Capture(new[] { new DrugCatalogEntry(new object(), true, true) }, Array.Empty<ThingDef>()), Throws.ArgumentException);
        drug.ingestible.joy = 0;
        Assert.That(NativeDrugGenerationFacts.Capture(catalog, Array.Empty<ThingDef>())[0].Support, Is.EqualTo(DrugGenerationSupport.Unknown));
    }

    [TestCase("beer", .016f, -.016f, .25f, .15f, -.75f, 3)]
    [TestCase("tea", .03f, -.015f, .10f, .75f, -3f, 4)]
    [TestCase("smokeleaf", .03f, -.015f, .15f, .5f, -1f, 4)]
    [TestCase("ambrosia", .032f, -.02f, .15f, .5f, -.75f, 3)]
    public void FourSourceComparatorShapesSupportTheIndependentC1Result(string profile, float gain, float decay, float threshold,
        float highGain, float highDecay, int expectedDays)
    {
        // These are constructed native shapes, never loaded Core Defs or name-based product rules.
        var drugProperties = drug.comps.OfType<CompProperties_Drug>().Single();
        drugProperties.minToleranceToAddict = threshold;
        ((IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[0]).severity = highGain;
        var toleranceGiver = (IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[1];
        toleranceGiver.severity = gain;
        toleranceGiver.multiplyByGeneToleranceFactors = true;
        SetBodyDivision(toleranceGiver, true);
        ((HediffCompProperties_SeverityPerDay)tolerance.comps[0]).severityPerDay = decay;
        ((HediffCompProperties_SeverityPerDay)high.comps[0]).severityPerDay = highDecay;
        if (profile != "ambrosia")
            tolerance.hediffGivers = new List<HediffGiver> { new HediffGiver_RandomDrugEffect
            {
                hediff = addiction,
                severityToMtbDaysCurve = new SimpleCurve(new[] { new CurvePoint(.45f, 99999), new CurvePoint(.5f, 180), new CurvePoint(1, 135) })
            } };
        if (profile == "tea" || profile == "smokeleaf") AddRestOffset();
        if (profile == "ambrosia") drug.comps.Add(new CompProperties_Rottable());
        if (profile == "beer")
        {
            high.hediffClass = typeof(Hediff_Alcohol);
            high.stages = new List<HediffStage> { new(), new() { minSeverity = .25f }, new() { minSeverity = .4f }, new() { minSeverity = .7f },
                new() { minSeverity = .9f, hediffGivers = new List<HediffGiver> { new HediffGiver_Random { hediff = addiction } } } };
            high.comps.Add(new HediffCompProperties_Effecter { severityIndices = new IntRange(3, 5) });
        }
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        var result = DrugPolicySuggestions.Create(facts, 1).Rows[0];
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.OrdinaryWholeDose));
        Assert.That(result.Reason, Is.EqualTo(DrugSuggestionReason.ModeledSchedule));
        Assert.That(result.Choices.DaysFrequency, Is.EqualTo(expectedDays));
        Assert.That(facts[0].DivideByBodySize, Is.True);
        Assert.That(DrugPolicySuggestions.Create(facts, .05f).Rows[0].Choices.Scheduled, Is.False);
    }
    [TestCase(297, true)]
    [TestCase(298, false)]
    public void SideHighClearanceUsesTheDerivedEventBoundWithoutRoundingDown(int events, bool supported)
    {
        // Exact quarter-unit subtraction isolates the event boundary; these are not balance values.
        high.maxSeverity = 100;
        ((IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[0]).severity = events * .25f;
        ((HediffCompProperties_SeverityPerDay)high.comps[0]).severityPerDay = -75f;
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support == DrugGenerationSupport.OrdinaryWholeDose, Is.EqualTo(supported));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-.5f)]
    public void InvalidRawRiskMetadataIsRetainedAndCannotProduceAPositiveSuggestion(float risk)
    {
        drug.comps.OfType<CompProperties_Drug>().Single().largeOverdoseChance = risk;
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].LargeOverdoseChance, Is.EqualTo(risk));
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[0].Reason, Is.EqualTo(DrugSuggestionReason.InvalidFacts));
    }

    [Test]
    public void CapturedOrderAndActualChannelIdentitySurvivePartialClassification()
    {
        var second = new ThingDef
        {
            defName = "SecondIdentitySameLabel", label = drug.label, category = ThingCategory.Item,
            thingClass = typeof(ThingWithComps), comps = drug.comps, ingestible = drug.ingestible
        };
        DefDatabase<ThingDef>.Add(second);
        var catalog = NativeDrugCatalog.Capture(_ => true).Reverse().ToArray();
        var facts = NativeDrugGenerationFacts.Capture(catalog, new[] { second });
        Assert.That(facts.Select(row => row.Drug.Identity), Is.EqualTo(new[] { second, drug }));
        Assert.That(facts.All(row => ReferenceEquals(row.ChemicalIdentity, chemical) && ReferenceEquals(row.ToleranceIdentity, tolerance)), Is.True);
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.UnresolvedPartialDose));
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[1].Choices.Scheduled, Is.True);
        Assert.That(() => ((IList<DrugGenerationFacts>)facts)[0] = facts[1], Throws.TypeOf<NotSupportedException>());
        var fullDose = NativeDrugGenerationFacts.Capture(catalog, Array.Empty<ThingDef>());
        Assert.That(DrugPolicySuggestions.Create(fullDose, 1f).Rows.All(row => row.Reason == DrugSuggestionReason.SharedExposure), Is.True);
    }

    [Test]
    public void DoseAtTheAddictionThresholdDoesNotBorrowSecondaryEffectInactivity()
    {
        ((IngestionOutcomeDoer_GiveHediff)drug.ingestible.outcomeDoers[1]).severity = .15f;
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(DrugPolicySuggestions.Create(facts, 1f).Rows[0].Reason, Is.EqualTo(DrugSuggestionReason.BaselineOutsideModel));
    }

    [TestCase("chemical need")]
    [TestCase("nonfinite initial")]
    [TestCase("nonfinite lethal")]
    public void ExtraToleranceLifecycleBehaviorIsNotAdmittedAsPlainDecay(string change)
    {
        if (change == "chemical need")
        {
            tolerance.chemicalNeed = new NeedDef { defName = "UnexpectedToleranceNeed", needClass = typeof(Need_Rest) };
            DefDatabase<NeedDef>.Add(tolerance.chemicalNeed);
        }
        if (change == "nonfinite initial") tolerance.initialSeverity = float.NaN;
        if (change == "nonfinite lethal") tolerance.lethalSeverity = float.NaN;
        var facts = NativeDrugGenerationFacts.Capture(NativeDrugCatalog.Capture(_ => true), Array.Empty<ThingDef>());
        Assert.That(facts[0].Support, Is.EqualTo(DrugGenerationSupport.UnsupportedDecay));
    }

    private sealed class UnknownDrugComp : CompDrug { }
    private sealed class UnknownGiver : IngestionOutcomeDoer_GiveHediff { }
    private sealed class UnknownOutcome : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
            => throw new InvalidOperationException("Projection must not invoke outcomes.");
    }
}
