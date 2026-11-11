using System.Collections.Generic;
using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

// The bootstrap owns active-package discovery and emission of these finite startup warnings.
public sealed class DiagnosticOriginalFeatures
{
    private DiagnosticOriginalFeatures(SettingsEffectiveOff effectiveOff, IReadOnlyList<string> warnings)
    {
        EffectiveOff = effectiveOff;
        StartupWarnings = warnings;
    }

    public SettingsEffectiveOff EffectiveOff { get; }
    public IReadOnlyList<string> StartupWarnings { get; }

    public static DiagnosticOriginalFeatures FromActivePackages(IEnumerable<string> activePackageIds)
    {
        var active = new HashSet<string>(activePackageIds, System.StringComparer.OrdinalIgnoreCase);
        var reasons = new Dictionary<SettingDefinition<bool>, string>();
        var warnings = new List<string>();
        void Map(string packageId, SettingDefinition<bool> definition, string feature)
        {
            if (!active.Contains(packageId)) return;
            reasons[definition] = "Megamod_Settings_OriginalProviderOff";
            warnings.Add($"[fumblesneeze.megamod] Active original {packageId}; " +
                $"{feature} uses best-effort ordinary feature-off behavior; " +
                "stored preferences are retained. This does not establish coexistence compatibility.");
        }
        Map("mlie.logafterdeferror", DiagnosticSettings.DefinitionContext, "S094 definition diagnostic context");
        Map("ordpus.logafterdeferror", DiagnosticSettings.DefinitionContext, "S094 definition diagnostic context");
        Map("bs.betterlog", WarningCaptureSettings.MuteWarnings, "S035 warning muting");
        return new(new SettingsEffectiveOff(reasons), warnings.AsReadOnly());
    }
}
