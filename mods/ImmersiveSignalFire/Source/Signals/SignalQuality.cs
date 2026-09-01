using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmersiveSignalFire.Signals;

public enum SignalOutcome
{
    Unnoticed = 0,
    Misunderstood = 1,
    Delayed = 2,
    Immediate = 3,
}

public readonly struct SignalSkills
{
    public SignalSkills(int melee, int social)
    {
        Melee = melee;
        Social = social;
    }

    public int Melee { get; }

    public int Social { get; }
}

public static class SignalQuality
{
    public static float Individual(int meleeLevel, int socialLevel) =>
        (ClampSkill(meleeLevel) + ClampSkill(socialLevel)) / 40f;

    public static float Group(IEnumerable<SignalSkills> participants)
    {
        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        SignalSkills[] copy = participants.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("At least one signal participant is required.", nameof(participants));
        }

        return copy.Average(participant => Individual(participant.Melee, participant.Social));
    }

    public static SignalOutcome Outcome(float quality)
    {
        float clamped = Math.Max(0f, Math.Min(1f, quality));
        if (clamped > 0.5f)
        {
            return SignalOutcome.Immediate;
        }

        if (clamped > 0.3f)
        {
            return SignalOutcome.Delayed;
        }

        return clamped >= 0.15f
            ? SignalOutcome.Misunderstood
            : SignalOutcome.Unnoticed;
    }

    private static int ClampSkill(int level) => Math.Max(0, Math.Min(20, level));
}
