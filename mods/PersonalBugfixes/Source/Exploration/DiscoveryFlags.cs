using System.Collections.Generic;

namespace PersonalBugfixes.Exploration;

public static class DiscoveryFlags
{
    public static bool Read(List<bool> flags, int index)
    {
        // Keep the original exception for null lists and negative indexes.
        while (flags.Count <= index) flags.Add(false);
        return flags[index];
    }
}
