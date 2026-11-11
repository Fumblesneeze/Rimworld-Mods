using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Settings;

namespace Megamod.Features.DrugPolicyMaintenance.Runtime;

public static class DrugPolicySettingsUi
{
    public static void Register(SettingsRenderer renderer) => renderer.AddBoolean(
        DrugPolicySettings.Enabled, "Megamod_DrugPolicy_Enabled", "Megamod_DrugPolicy_Category",
        "Megamod_DrugPolicy_EnabledHelp", "Megamod_DrugPolicy_GlobalOnlyReason");
}
