using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceRuleTests
{
    [TestCase(0f, 0f, DrugToleranceDecision.LimitReached)]
    [TestCase(0.2f, 0.2f, DrugToleranceDecision.LimitReached)]
    [TestCase(0.19999f, 0.2f, DrugToleranceDecision.Allowed)]
    [TestCase(1f, 1f, DrugToleranceDecision.LimitReached)]
    [TestCase(1.2f, 1f, DrugToleranceDecision.LimitReached)]
    public void RoutineUseUsesUnroundedStrictComparison(float severity, float limit, DrugToleranceDecision expected) =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.Scheduled, false,
            DrugToleranceChoice.Numeric(limit), DrugToleranceReading.FromSeverity(severity)), Is.EqualTo(expected));

    [TestCase(RoutineDrugUse.Scheduled)]
    [TestCase(RoutineDrugUse.DirectRecreation)]
    [TestCase(RoutineDrugUse.SocialDrink)]
    [TestCase(RoutineDrugUse.PatientTreat)]
    public void NativeRejectionIsNeverOverridden(RoutineDrugUse route) =>
        Assert.That(DrugToleranceRule.Evaluate(false, true, route, true,
            DrugToleranceChoice.Any, DrugToleranceReading.Absent), Is.EqualTo(DrugToleranceDecision.NativeRejected));

    [TestCase(RoutineDrugUse.Scheduled, DrugToleranceDecision.LimitReached)]
    [TestCase(RoutineDrugUse.DirectRecreation, DrugToleranceDecision.Allowed)]
    [TestCase(RoutineDrugUse.SocialDrink, DrugToleranceDecision.LimitReached)]
    [TestCase(RoutineDrugUse.PatientTreat, DrugToleranceDecision.LimitReached)]
    public void NativeDirectRecreationExceptionDoesNotEscapeItsRoute(RoutineDrugUse route, DrugToleranceDecision expected) =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, route, true,
            DrugToleranceChoice.Numeric(0f), DrugToleranceReading.Absent), Is.EqualTo(expected));

    [Test]
    public void AnyAddsNoRestrictionAtOrAboveOne() =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.Scheduled, false,
            DrugToleranceChoice.Any, DrugToleranceReading.FromSeverity(1.5f)), Is.EqualTo(DrugToleranceDecision.Allowed));

    [Test]
    public void DisabledFeatureAddsNoVetoAndDoesNotRepairStoredChoice()
    {
        var invalid = DrugToleranceChoice.Decode("future-value");
        Assert.That(DrugToleranceRule.Evaluate(true, false, RoutineDrugUse.Scheduled, false,
            invalid, DrugToleranceReading.Unavailable), Is.EqualTo(DrugToleranceDecision.Allowed));
        Assert.That(invalid.Encoding, Is.EqualTo("future-value"));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(-0.01f)]
    public void UnknownReaderResultIsUnavailableRatherThanAnAbsentHediff(float severity) =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.Scheduled, false,
            DrugToleranceChoice.Numeric(0.2f), DrugToleranceReading.FromSeverity(severity)),
            Is.EqualTo(DrugToleranceDecision.Unavailable));

    [Test]
    public void MissingHediffIsKnownZeroAndThereforeCannotPassNumericZero() =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.Scheduled, false,
            DrugToleranceChoice.Numeric(0f), DrugToleranceReading.Absent), Is.EqualTo(DrugToleranceDecision.LimitReached));

    [Test]
    public void MalformedChoiceWithAValidReaderBlocksUntilAnExplicitEdit()
    {
        var malformed = DrugToleranceChoice.Decode("1:limit:NaN");
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.SocialDrink, false,
            malformed, DrugToleranceReading.Absent), Is.EqualTo(DrugToleranceDecision.NeedsReview));
        Assert.That(DrugToleranceRule.Evaluate(true, true, RoutineDrugUse.SocialDrink, false,
            DrugToleranceChoice.Any, DrugToleranceReading.Absent), Is.EqualTo(DrugToleranceDecision.Allowed));
        Assert.That(malformed.Encoding, Is.EqualTo("1:limit:NaN"));
    }

    [Test]
    public void UnrecognizedRouteCannotClaimSupportedEnforcement() =>
        Assert.That(DrugToleranceRule.Evaluate(true, true, (RoutineDrugUse)999, false,
            DrugToleranceChoice.Numeric(0f), DrugToleranceReading.Absent), Is.EqualTo(DrugToleranceDecision.Unavailable));
}
