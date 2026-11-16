using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.ModSettingsNavigation;

public static class ModSettingsNavigationSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("modSettingsNavigation.enabled", true,
        SettingScope.GlobalAndSave, "interface.navigation", 0, SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);

    public static void Register(SettingsStore store) => store.Register(Enabled);
}

public static class ModSettingsNavigationProviderPolicy
{
    private const string Original = "superniquito.modoptionssort";

    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, Original, StringComparison.OrdinalIgnoreCase))
            ? new Dictionary<SettingDefinition<bool>, string> { [ModSettingsNavigationSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();

    public static IEnumerable<string> StartupWarnings(IEnumerable<string> activePackageIds)
    {
        if (Reasons(activePackageIds).Count != 0)
            yield return "[fumblesneeze.megamod] Active original " + Original + "; S047 settings navigation uses best-effort ordinary feature-off behavior. " +
                "Stored preferences and inheritance are retained. This does not establish coexistence compatibility.";
    }
}
