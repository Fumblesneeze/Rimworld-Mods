using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.MessagePlacement;

public static class MessagePlacementProviderPolicy
{
    public static IReadOnlyDictionary<SettingDefinition<bool>, string> Reasons(IEnumerable<string> activePackageIds) =>
        OriginalActive(activePackageIds)
            ? new Dictionary<SettingDefinition<bool>, string> { [MessagePlacementSettings.Enabled] = "Megamod_Settings_OriginalProviderOff" }
            : new Dictionary<SettingDefinition<bool>, string>();

    public static IReadOnlyList<string> StartupWarnings(IEnumerable<string> activePackageIds) =>
        OriginalActive(activePackageIds)
            ? new[] { "[fumblesneeze.megamod] Active original mlie.bettermessageplacement; S071 message placement uses best-effort ordinary feature-off behavior; stored preferences are retained. This does not establish coexistence compatibility." }
            : Array.Empty<string>();

    public static bool OriginalActive(IEnumerable<string> activePackageIds) => activePackageIds.Any(id =>
        string.Equals(id.Trim(), "Mlie.BetterMessagePlacement", StringComparison.OrdinalIgnoreCase));
}
