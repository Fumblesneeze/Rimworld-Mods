using Megamod.Features.DrugPolicyMaintenance.Core;

namespace Megamod.Features.PracticalNeeds.Core;

public static class PotentialMoodRule
{
    public static ScheduledDrugReadResult Evaluate(
        bool enabled, bool hasMood, float threshold, float currentMood, float potentialMood)
    {
        if (!enabled || !hasMood) return ScheduledDrugReadResult.Allowed;
        if (!Percentage(threshold)) return ScheduledDrugReadResult.Unavailable;
        if (threshold == 1f) return ScheduledDrugReadResult.Allowed;
        if (!Percentage(currentMood) || !Percentage(potentialMood)) return ScheduledDrugReadResult.Unavailable;
        return currentMood >= threshold || potentialMood >= threshold
            ? ScheduledDrugReadResult.Rejected : ScheduledDrugReadResult.Allowed;
    }

    private static bool Percentage(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
}
