namespace ImmersiveChefs;

internal static class DiningDirtPolicy
{
    internal static bool ShouldCreate(
        bool coveredMeal,
        bool humanlikeDiner,
        WareRequirementMode requirementMode,
        bool ingestionCompleted,
        bool mapAvailable,
        bool hasCutlery) =>
        coveredMeal &&
        humanlikeDiner &&
        requirementMode != WareRequirementMode.Off &&
        ingestionCompleted &&
        mapAvailable &&
        !hasCutlery;
}
