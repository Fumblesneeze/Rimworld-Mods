using System.Threading;
using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

public sealed record DiagnosticOptions(bool DefinitionContextEnabled);

public sealed class DiagnosticOptionsSource
{
    private readonly SettingsStore store;
    private DiagnosticOptions current;

    public DiagnosticOptionsSource(SettingsStore store)
    {
        this.store = store;
        current = new DiagnosticOptions(store.GetEffective(DiagnosticSettings.DefinitionContext));
        store.Changed += Publish;
    }

    public DiagnosticOptions Current => Volatile.Read(ref current);

    // SettingsStore invokes this only on its serialized startup/game-thread owner.
    private void Publish() => Interlocked.Exchange(ref current,
        new DiagnosticOptions(store.GetEffective(DiagnosticSettings.DefinitionContext)));
}
