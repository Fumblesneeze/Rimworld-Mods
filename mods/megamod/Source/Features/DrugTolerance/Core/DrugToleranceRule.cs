using System;

namespace Megamod.Features.DrugTolerance.Core;

public enum RoutineDrugUse { Scheduled, DirectRecreation, SocialDrink, PatientTreat }
public enum DrugToleranceDecision { Allowed, NativeRejected, LimitReached, NeedsReview, Unavailable }

/// <summary>A current, detached chemical fact. A failed reader is never an absent hediff.</summary>
public readonly struct DrugToleranceReading
{
    private DrugToleranceReading(bool available, float severity)
    {
        Available = available;
        Severity = severity;
    }

    public bool Available { get; }
    public float Severity { get; }
    public static DrugToleranceReading Absent => FromSeverity(0f);
    public static DrugToleranceReading Unavailable => default;
    public static DrugToleranceReading FromSeverity(float severity) =>
        float.IsFinite(severity) && severity >= 0f ? new(true, severity) : Unavailable;
}

public static class DrugToleranceRule
{
    // Route provenance belongs to the native caller, not the chemical comparison.
    public static DrugToleranceDecision Evaluate(
        bool nativeAllowed, bool enabled, RoutineDrugUse route, bool directRecreationException,
        DrugToleranceChoice choice, DrugToleranceReading current)
    {
        ArgumentNullException.ThrowIfNull(choice);
        if (!nativeAllowed)
            return DrugToleranceDecision.NativeRejected;
        if (!enabled)
            return DrugToleranceDecision.Allowed;
        if (route is not (RoutineDrugUse.Scheduled or RoutineDrugUse.DirectRecreation or
            RoutineDrugUse.SocialDrink or RoutineDrugUse.PatientTreat))
            return DrugToleranceDecision.Unavailable;
        if (route == RoutineDrugUse.DirectRecreation && directRecreationException)
            return DrugToleranceDecision.Allowed;
        if (!current.Available)
            return DrugToleranceDecision.Unavailable;
        if (choice.Kind == DrugToleranceChoiceKind.NeedsReview)
            return DrugToleranceDecision.NeedsReview;
        return choice.Kind == DrugToleranceChoiceKind.Limit && current.Severity >= choice.Limit
            ? DrugToleranceDecision.LimitReached : DrugToleranceDecision.Allowed;
    }
}
