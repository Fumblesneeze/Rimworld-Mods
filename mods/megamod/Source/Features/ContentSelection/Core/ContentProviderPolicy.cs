using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

public sealed class ContentProviderPolicy
{
    private ContentProviderPolicy(SettingsEffectiveOff effectiveOff, IReadOnlyList<string> warnings)
    { EffectiveOff = effectiveOff; StartupWarnings = warnings; }

    public SettingsEffectiveOff EffectiveOff { get; }
    public IReadOnlyList<string> StartupWarnings { get; }

    public static ContentProviderPolicy FromActivePackages(IEnumerable<string> activePackageIds)
    {
        bool active = activePackageIds.Any(id => string.Equals(id, "Owlchemist.CherryPicker", StringComparison.OrdinalIgnoreCase));
        var reasons = new Dictionary<SettingDefinition<bool>, string>();
        var warnings = new List<string>();
        if (active)
        {
            reasons[ContentSettings.Enabled] = "Megamod_Settings_OriginalProviderOff";
            warnings.Add("[fumblesneeze.megamod] Active original Owlchemist.CherryPicker; S038 content selection uses " +
                "best-effort ordinary feature-off behavior at its new-game/load boundary; stored preferences are retained. " +
                "This does not establish coexistence compatibility.");
        }
        return new(new SettingsEffectiveOff(reasons), warnings.AsReadOnly());
    }
}
