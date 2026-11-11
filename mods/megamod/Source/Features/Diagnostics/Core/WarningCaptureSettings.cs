using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

public static class WarningCaptureSettings
{
    public static readonly SettingDefinition<bool> MuteWarnings = new("log.muteWarnings", false,
        SettingScope.GlobalOnly, "diagnostics.capture", 0, SettingApplicationTiming.NextEligibleOccurrence, SettingCodecs.Boolean);
    public static void Register(SettingsStore store) => store.Register(MuteWarnings);
}
