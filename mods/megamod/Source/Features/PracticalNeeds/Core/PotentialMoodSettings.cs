using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.PracticalNeeds.Core;

public static class PotentialMoodSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("practicalNeeds.scheduledPotentialMood", true,
        SettingScope.GlobalAndSave, "recreation-care", 10, SettingApplicationTiming.NextEligibleOccurrence, SettingCodecs.Boolean);
    public static void Register(SettingsStore store) => store.Register(Enabled);
}

public static class PotentialMoodProviderPolicy
{
    private const string Original = "avilmask.commonsense";
    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        activePackageIds.Any(id => string.Equals(id, Original, StringComparison.OrdinalIgnoreCase))
            ? new Dictionary<SettingDefinition<bool>, string> { [PotentialMoodSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();
    public static IEnumerable<string> StartupWarnings(IEnumerable<string> activePackageIds)
    {
        if (Reasons(activePackageIds).Count != 0)
            yield return "[fumblesneeze.megamod] Active original " + Original + "; S024 scheduled potential mood uses best-effort ordinary feature-off behavior. " +
                "Stored preferences and inheritance are retained. This does not establish coexistence compatibility.";
    }
}
