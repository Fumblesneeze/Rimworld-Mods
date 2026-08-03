namespace ImmersiveChefs;

internal static class AssistedDiningPolicy
{
    internal static bool ShouldRecordDiningMemory(
        bool assisted,
        bool dinerConscious,
        WareRequirementMode cutleryRequirement,
        bool hasCutlery) =>
        !assisted ||
        dinerConscious ||
        cutleryRequirement == WareRequirementMode.Off ||
        hasCutlery;
}
