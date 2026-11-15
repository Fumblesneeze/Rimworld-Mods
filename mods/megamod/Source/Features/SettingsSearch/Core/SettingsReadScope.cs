using System;
using System.Threading;

namespace Megamod.Features.SettingsSearch.Core;

/// <summary>Tracks one synchronous settings read; native state isolation belongs to its caller.</summary>
public sealed class SettingsReadScope : IDisposable
{
    [ThreadStatic] private static SettingsReadScope? current;
    private readonly Thread owner = Thread.CurrentThread;
    private bool disposed;
    public static SettingsReadScope? Current => current;
    public bool Failed => FailureReason != null;
    public string? FailureReason { get; private set; }

    private SettingsReadScope() { }

    public static SettingsReadScope Begin()
    {
        if (current != null) throw current.Refuse("nested-settings-read");
        return current = new SettingsReadScope();
    }

    public InvalidOperationException Refuse(string fixedReason)
    {
        RequireOwner();
        if (disposed) throw new ObjectDisposedException(nameof(SettingsReadScope));
        FailureReason ??= fixedReason;
        return new InvalidOperationException(fixedReason);
    }

    public void Dispose()
    {
        RequireOwner();
        if (disposed) return;
        disposed = true;
        if (ReferenceEquals(current, this)) current = null;
    }

    private void RequireOwner()
    {
        if (!ReferenceEquals(Thread.CurrentThread, owner))
            throw new InvalidOperationException("settings-read-wrong-thread");
    }
}
