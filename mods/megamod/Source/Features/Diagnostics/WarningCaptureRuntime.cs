using System;
using Megamod.Patching;
using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

// The settings owner publishes options. Log workers read only that immutable snapshot and the route.
public sealed class WarningCaptureRuntime
{
    private readonly WarningCaptureOptionsSource options;
    private NativeLogPatch? route;

    public WarningCaptureRuntime(SettingsStore store)
    {
        options = new WarningCaptureOptionsSource(store);
    }

    public bool NeedsWarningChannel => options.Current.MuteWarnings;
    public bool AllowWarning(string _) => !NeedsWarningChannel;
    public string StatusKey => !options.Current.MuteWarnings ? "Megamod_Diagnostics_WarningCaptureOff" :
        route?.CaptureAvailable == true ? "Megamod_Diagnostics_WarningCaptureOn" : "Megamod_Diagnostics_WarningCaptureUnavailable";

    internal void Bind(NativeLogPatch warning)
    {
        if (route != null) throw new InvalidOperationException("The warning capture owner already has its shared route.");
        route = warning;
    }

}
