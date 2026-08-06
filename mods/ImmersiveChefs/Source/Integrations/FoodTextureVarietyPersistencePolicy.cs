namespace ImmersiveChefs;

internal static class FoodTextureVarietyPersistencePolicy
{
    internal const int NoPersistedGroup = -1;
    internal const int GraphicsPerGroup = 3;

    internal static int FindUniqueGroupStart<T>(
        IReadOnlyList<T>? finalized,
        IReadOnlyList<T>? selected,
        IEqualityComparer<T>? comparer = null)
    {
        if (finalized is null || selected is null || selected.Count != GraphicsPerGroup)
        {
            return NoPersistedGroup;
        }

        comparer ??= EqualityComparer<T>.Default;
        var match = NoPersistedGroup;
        for (var start = 0; start + GraphicsPerGroup <= finalized.Count; start += GraphicsPerGroup)
        {
            var matches = true;
            for (var offset = 0; offset < GraphicsPerGroup; offset++)
            {
                if (!comparer.Equals(finalized[start + offset], selected[offset]))
                {
                    matches = false;
                    break;
                }
            }

            if (!matches)
            {
                continue;
            }

            if (match != NoPersistedGroup)
            {
                return NoPersistedGroup;
            }

            match = start;
        }

        return match;
    }

    internal static bool CanRestoreGroup(int start, int finalizedCount)
    {
        return start >= 0 && start % GraphicsPerGroup == 0 &&
               finalizedCount >= GraphicsPerGroup &&
               start <= finalizedCount - GraphicsPerGroup;
    }
}
