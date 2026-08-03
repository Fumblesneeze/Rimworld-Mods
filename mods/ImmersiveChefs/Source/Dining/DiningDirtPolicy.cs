namespace ImmersiveChefs;

internal static class DiningDirtPolicy
{
    internal static bool ShouldCreate(
        bool coveredMeal,
        bool humanlikeDiner,
        WareRequirementMode requirementMode,
        bool ingestionCompleted,
        bool mapAvailable,
        bool hasSilverware) =>
        coveredMeal &&
        humanlikeDiner &&
        requirementMode != WareRequirementMode.Off &&
        ingestionCompleted &&
        mapAvailable &&
        !hasSilverware;
}
