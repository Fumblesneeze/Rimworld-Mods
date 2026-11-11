using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.DrugTolerance.Core;

public static class DrugToleranceSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("drugTolerance.enabled", true,
        SettingScope.GlobalAndSave, "drug-policy", 10, SettingApplicationTiming.NextEligibleOccurrence, SettingCodecs.Boolean);
    public static void Register(SettingsStore store) => store.Register(Enabled);
}

public static class DrugToleranceProviderPolicy
{
    private const string Original = "seohyeon.onlyiftolerancebelow";
    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, Original, StringComparison.OrdinalIgnoreCase))
            ? new Dictionary<SettingDefinition<bool>, string> { [DrugToleranceSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();
    public static IEnumerable<string> StartupWarnings(IEnumerable<string> activePackageIds)
    {
        if (Reasons(activePackageIds).Count != 0)
            yield return "[fumblesneeze.megamod] Active original " + Original + "; S138 drug tolerance uses best-effort ordinary feature-off behavior. " +
                "Stored preferences and policy choices are retained. This does not establish coexistence compatibility.";
    }
}
