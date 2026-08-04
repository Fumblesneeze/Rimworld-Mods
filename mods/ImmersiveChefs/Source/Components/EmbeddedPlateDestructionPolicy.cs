namespace ImmersiveChefs;

internal static class EmbeddedPlateDestructionPolicy
{
    internal static bool ShouldDestroy(bool causedByFire, float effectiveFlammability) =>
        causedByFire && effectiveFlammability > 0f;
}
