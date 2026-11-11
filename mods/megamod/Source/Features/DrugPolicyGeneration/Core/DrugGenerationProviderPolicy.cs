using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.DrugPolicyGeneration.Core;

public static class DrugGenerationProviderPolicy
{
    private const string Original = "seohyeon.bestdrugpolicy";
    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, Original, StringComparison.OrdinalIgnoreCase))
            ? new Dictionary<SettingDefinition<bool>, string> { [DrugGenerationSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();
    public static IEnumerable<string> StartupWarnings(IEnumerable<string> activePackageIds)
    {
        if (Reasons(activePackageIds).Count != 0)
            yield return "[fumblesneeze.megamod] Active original " + Original + "; S139 drug policy generation uses best-effort ordinary feature-off behavior. " +
                "Stored preferences and existing policies are retained. This does not establish coexistence compatibility.";
    }
}
