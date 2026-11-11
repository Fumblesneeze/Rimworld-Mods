using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

public sealed class DrugPolicyProviderPolicy
{
    private DrugPolicyProviderPolicy(SettingsEffectiveOff effectiveOff, IReadOnlyList<string> warnings)
    { EffectiveOff = effectiveOff; StartupWarnings = warnings; }

    public SettingsEffectiveOff EffectiveOff { get; }
    public IReadOnlyList<string> StartupWarnings { get; }

    public static DrugPolicyProviderPolicy FromActivePackages(IEnumerable<string> activePackageIds)
    {
        bool active = activePackageIds.Any(id => string.Equals(id, "Mlie.DrugPolicyFix", StringComparison.OrdinalIgnoreCase));
        var reasons = new Dictionary<SettingDefinition<bool>, string>();
        var warnings = new List<string>();
        if (active)
        {
            reasons[DrugPolicySettings.Enabled] = "Megamod_Settings_OriginalProviderOff";
            warnings.Add("[fumblesneeze.megamod] Active original Mlie.DrugPolicyFix; S029 drug-policy maintenance uses " +
                "best-effort ordinary feature-off behavior at startup; stored preferences are retained. " +
                "This does not establish coexistence compatibility.");
        }
        return new(new SettingsEffectiveOff(reasons), warnings.AsReadOnly());
    }
}
