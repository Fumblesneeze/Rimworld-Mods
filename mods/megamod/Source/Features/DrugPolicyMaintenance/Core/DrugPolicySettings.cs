using Megamod.Settings;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

public static class DrugPolicySettings
{
    // Initial native factories precede any authoritative save. Freeze this shared choice at startup.
    public static readonly SettingDefinition<bool> Enabled = new("drug-policy.enabled", true,
        SettingScope.GlobalOnly, "drug-policy", 0, SettingApplicationTiming.Restart, SettingCodecs.Boolean);

    public static void Register(SettingsStore store) => store.Register(Enabled);
    public static bool CaptureStartupEnabled(SettingsStore store) => store.GetEffective(Enabled);
}
