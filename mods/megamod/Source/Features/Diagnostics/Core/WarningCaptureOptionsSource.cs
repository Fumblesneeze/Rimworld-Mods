using System.Threading;
using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

public sealed record WarningCaptureOptions(bool MuteWarnings);

public sealed class WarningCaptureOptionsSource
{
    private readonly SettingsStore store;
    private WarningCaptureOptions current;
    public WarningCaptureOptionsSource(SettingsStore store)
    {
        this.store = store;
        current = new(store.GetEffective(WarningCaptureSettings.MuteWarnings));
        store.Changed += Publish;
    }
    public WarningCaptureOptions Current => Volatile.Read(ref current);
    private void Publish() => Interlocked.Exchange(ref current, new(store.GetEffective(WarningCaptureSettings.MuteWarnings)));
}
