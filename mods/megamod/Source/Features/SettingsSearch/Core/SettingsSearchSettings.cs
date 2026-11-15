using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.SettingsSearch;

public static class SettingsSearchSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("settingsSearch.enabled", true,
        SettingScope.GlobalAndSave, "interface.navigation", 10, SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);

    public static void Register(SettingsStore store) => store.Register(Enabled);
}

public static class SettingsSearchProviderPolicy
{
    private const string Original = "zipender.needle";

    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, Original, StringComparison.OrdinalIgnoreCase))
            ? new Dictionary<SettingDefinition<bool>, string> { [SettingsSearchSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();

    public static IEnumerable<string> StartupWarnings(IEnumerable<string> activePackageIds)
    {
        if (Reasons(activePackageIds).Count != 0)
            yield return "[fumblesneeze.megamod] Active original " + Original + "; S141 settings search uses best-effort ordinary feature-off behavior. " +
                "Stored preferences and inheritance are retained. This does not establish coexistence compatibility.";
    }
}
