using Megamod.Features.DrugPolicyGeneration.Runtime;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class HumanAdultInputTests
{
    [Test]
    public void FullGrownAnchorIsSelectedInsteadOfTheFirstAdultClassification()
    {
        var human = new object(); var race = new object(); var nativeList = new object();
        var teenager = new object(); var adult = new object(); var adultDefinition = new object();
        const int sameSuppliedAdultClassification = 8;
        var stages = new[]
        {
            new HumanLifeStageInput(teenager, new object(), 13f, sameSuppliedAdultClassification, .8f),
            new HumanLifeStageInput(adult, adultDefinition, 18f, sameSuppliedAdultClassification, 1f)
        };
        var result = HumanAdultInput.FromFullAdult(human, race, nativeList, 2, 1f, stages, adultDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(result.Input.AdultEntryIdentity, Is.SameAs(adult));
            Assert.That(result.BaselineBodySize, Is.EqualTo(1f));
            Assert.That(result.Input.HumanIdentity, Is.SameAs(human));
            Assert.That(result.Input.RaceIdentity, Is.SameAs(race));
            Assert.That(result.Input.LifeStagesIdentity, Is.SameAs(nativeList));
            Assert.That(result.Input.LifeStages.Select(stage => stage.EntryIdentity),
                Is.EqualTo(new[] { teenager, adult }));
            Assert.That(result.Input.LifeStages[0].BodySizeFactor, Is.EqualTo(.8f));
            Assert.That(result.Input.LifeStages[1].DefinitionIdentity, Is.SameAs(adultDefinition));
        });
    }

    [Test]
    public void DuplicateFullGrownDefinitionRefusesInsteadOfChoosingFirst()
    {
        var adultDefinition = new object();
        var stages = new[]
        {
            new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, 1f),
            new HumanLifeStageInput(new object(), adultDefinition, 30f, 8, 1.2f)
        };

        Assert.Throws<InvalidOperationException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, 1f, stages, adultDefinition));
    }

    [TestCase(0f, 1f)]
    [TestCase(-1f, 1f)]
    [TestCase(float.NaN, 1f)]
    [TestCase(float.PositiveInfinity, 1f)]
    [TestCase(float.NegativeInfinity, 1f)]
    [TestCase(1f, 0f)]
    [TestCase(1f, -1f)]
    [TestCase(1f, float.NaN)]
    [TestCase(1f, float.PositiveInfinity)]
    [TestCase(1f, float.NegativeInfinity)]
    [TestCase(-1f, -1f)]
    public void BaselineRequiresBothOperandsToBePositiveAndFinite(float stageFactor, float raceFactor)
    {
        var adultDefinition = new object();
        var stages = new[] { new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, stageFactor) };

        Assert.Throws<InvalidOperationException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, raceFactor, stages, adultDefinition));
    }

    [TestCase(float.Epsilon, .5f)]
    [TestCase(float.MaxValue, 2f)]
    public void PositiveFiniteOperandsStillRefuseAnUnderflowedOrOverflowedProduct(float stageFactor, float raceFactor)
    {
        var adultDefinition = new object();
        var stages = new[] { new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, stageFactor) };

        Assert.Throws<InvalidOperationException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, raceFactor, stages, adultDefinition));
    }

    [Test]
    public void OversizedReadRefusesAfterTheFirstOverflowEntry()
    {
        var adultDefinition = new object();
        int reads = 0;
        IEnumerable<HumanLifeStageInput> Stages()
        {
            for (int index = 0; index < 4097; index++)
            {
                reads++;
                yield return new HumanLifeStageInput(new object(), index == 0 ? adultDefinition : new object(),
                    index, 8, 1f);
            }
            throw new InvalidOperationException("The reader enumerated beyond the bounded overflow witness.");
        }

        Assert.Throws<ArgumentException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, 1f, Stages(), adultDefinition));
        Assert.That(reads, Is.EqualTo(4097));
    }

    [Test]
    public void NullFullGrownDefinitionIsAnInvalidArgument()
    {
        Assert.Throws<ArgumentNullException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, 1f, Array.Empty<HumanLifeStageInput>(), null!));
    }

    [Test]
    public void NullStageRetainsTheRawSnapshotsMalformedReadRefusal()
    {
        var adultDefinition = new object();
        var stages = new[] { null!, new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, 1f) };

        Assert.Throws<ArgumentException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, 1f, stages, adultDefinition));
    }

    [TestCase(1.25f, .8f, 1f)]
    [TestCase(.75f, 2f, 1.5f)]
    [TestCase(float.Epsilon, 1f, float.Epsilon)]
    public void CurrentOperandsDetermineTheSingleBaseline(float stageFactor, float raceFactor, float expected)
    {
        var adultDefinition = new object();
        var stages = new[] { new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, stageFactor) };
        var result = HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, raceFactor, stages, adultDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(result.BaselineBodySize, Is.EqualTo(expected));
            Assert.That(result.Input.RaceBaseBodySize, Is.EqualTo(raceFactor));
            Assert.That(result.Input.LifeStages[0].BodySizeFactor, Is.EqualTo(stageFactor));
        });
    }

    [Test]
    public void EqualLookingDefinitionsDoNotSubstituteForTheSuppliedAnchor()
    {
        var anchor = new Identity("full-grown");
        var impostor = new Identity("full-grown");
        var stages = new[] { new HumanLifeStageInput(new object(), impostor, 18f, 8, 1f) };

        Assert.That(impostor, Is.EqualTo(anchor));
        Assert.That(impostor, Is.Not.SameAs(anchor));
        Assert.Throws<InvalidOperationException>(() => HumanAdultInput.FromFullAdult(
            new object(), new object(), new object(), 2, 1f, stages, anchor));
    }

    [Test]
    public void AdditionalLaterStagesRetainAllOrderedInputsWithoutReplacingTheAnchor()
    {
        var adultDefinition = new object();
        var adultEntry = new object();
        var stages = new[]
        {
            new HumanLifeStageInput(new object(), new object(), 3f, 4, .3f),
            new HumanLifeStageInput(adultEntry, adultDefinition, 18f, 8, 1.2f),
            new HumanLifeStageInput(new object(), new object(), 65f, 16, 1.1f)
        };
        var result = HumanAdultInput.FromFullAdult(new object(), new object(), new object(),
            7, .5f, stages, adultDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(result.BaselineBodySize, Is.EqualTo(.6f));
            Assert.That(result.Input.RaceIntelligence, Is.EqualTo(7));
            Assert.That(result.Input.AdultEntryIdentity, Is.SameAs(adultEntry));
            Assert.That(result.Input.LifeStages, Is.EqualTo(stages));
        });
    }

    [Test]
    public void CapturedStagesDoNotChangeWhenTheCallerCollectionIsEdited()
    {
        var adultDefinition = new object();
        var original = new HumanLifeStageInput(new object(), adultDefinition, 18f, 8, 1f);
        var stages = new List<HumanLifeStageInput> { original };
        var result = HumanAdultInput.FromFullAdult(new object(), new object(), stages,
            2, 1f, stages, adultDefinition);
        stages[0] = original with { BodySizeFactor = 2f };
        stages.Add(new HumanLifeStageInput(new object(), new object(), 65f, 8, .9f));

        Assert.Multiple(() =>
        {
            Assert.That(result.Input.LifeStagesIdentity, Is.SameAs(stages));
            Assert.That(result.Input.LifeStages, Is.EqualTo(new[] { original }));
            Assert.That(result.BaselineBodySize, Is.EqualTo(1f));
        });
    }

    [Test]
    public void EqualBaselinesPreserveChangedOperandValuesAndIdentitiesInFreshReads()
    {
        var human = new object();
        var oldRace = new object(); var currentRace = new object();
        var oldDefinition = new object(); var currentDefinition = new object();
        var oldStages = new[] { new HumanLifeStageInput(new object(), oldDefinition, 18f, 8, 1f) };
        var currentStages = new[] { new HumanLifeStageInput(new object(), currentDefinition, 18f, 8, .5f) };
        var previous = HumanAdultInput.FromFullAdult(human, oldRace, oldStages, 2, 1f, oldStages, oldDefinition);
        var current = HumanAdultInput.FromFullAdult(human, currentRace, currentStages, 2, 2f, currentStages, currentDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(current.BaselineBodySize, Is.EqualTo(previous.BaselineBodySize));
            Assert.That(current.Input, Is.Not.SameAs(previous.Input));
            Assert.That(previous.Input.RaceIdentity, Is.SameAs(oldRace));
            Assert.That(current.Input.RaceIdentity, Is.SameAs(currentRace));
            Assert.That(previous.Input.LifeStagesIdentity, Is.SameAs(oldStages));
            Assert.That(current.Input.LifeStagesIdentity, Is.SameAs(currentStages));
            Assert.That(current.Input.AdultEntryIdentity, Is.SameAs(currentStages[0].EntryIdentity));
            Assert.That(current.Input.LifeStages[0].DefinitionIdentity, Is.SameAs(currentDefinition));
            Assert.That(previous.Input.RaceBaseBodySize, Is.EqualTo(1f));
            Assert.That(current.Input.RaceBaseBodySize, Is.EqualTo(2f));
            Assert.That(previous.Input.LifeStages[0].BodySizeFactor, Is.EqualTo(1f));
            Assert.That(current.Input.LifeStages[0].BodySizeFactor, Is.EqualTo(.5f));
        });
    }

    [Test]
    public void RawSnapshotStillRetainsMalformedScalarsForCurrentnessComparison()
    {
        var entry = new object();
        var stage = new HumanLifeStageInput(entry, new object(), float.NaN, -1, float.NegativeInfinity);
        var input = new HumanAdultInput(new object(), new object(), new object(), -1,
            float.PositiveInfinity, new[] { stage }, entry);

        Assert.Multiple(() =>
        {
            Assert.That(input.RaceIntelligence, Is.EqualTo(-1));
            Assert.That(input.RaceBaseBodySize, Is.EqualTo(float.PositiveInfinity));
            Assert.That(float.IsNaN(input.LifeStages[0].MinimumAge), Is.True);
            Assert.That(input.LifeStages[0].DevelopmentalStage, Is.EqualTo(-1));
            Assert.That(input.LifeStages[0].BodySizeFactor, Is.EqualTo(float.NegativeInfinity));
        });
    }

    [Test]
    public void ExactlyTheCollectionBoundRetainsEveryEntry()
    {
        var adultDefinition = new object();
        var stages = Enumerable.Range(0, 4096).Select(index => new HumanLifeStageInput(
            new object(), index == 2048 ? adultDefinition : new object(), index, 8, 1f)).ToArray();
        var result = HumanAdultInput.FromFullAdult(new object(), new object(), stages,
            2, 1f, stages, adultDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(result.Input.AdultEntryIdentity, Is.SameAs(stages[2048].EntryIdentity));
            Assert.That(result.Input.LifeStages, Is.EqualTo(stages));
        });
    }

    [Test]
    public void AnEmptyCurrentReadHasNoFullGrownBaseline()
    {
        Assert.Throws<InvalidOperationException>(() => HumanAdultInput.FromFullAdult(new object(),
            new object(), new object(), 2, 1f, Array.Empty<HumanLifeStageInput>(), new object()));
    }

    [Test]
    public void ACurrentReadRequiresANonnullCollection()
    {
        Assert.Throws<ArgumentNullException>(() => HumanAdultInput.FromFullAdult(new object(),
            new object(), new object(), 2, 1f, null!, new object()));
    }

    private sealed record Identity(string Name);
}
