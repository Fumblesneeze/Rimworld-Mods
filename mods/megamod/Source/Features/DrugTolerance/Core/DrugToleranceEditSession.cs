using System;
using System.Globalization;

namespace Megamod.Features.DrugTolerance.Core;

public sealed class DrugToleranceEditSession : IDisposable
{
    private Func<bool>? current;
    private Action<DrugToleranceChoice>? commit;
    public DrugToleranceEditSession(DrugToleranceChoice original, CultureInfo culture, Func<bool> current, Action<DrugToleranceChoice> commit)
    { Draft = new(original, culture); this.current = current; this.commit = commit; }
    public DrugToleranceEditDraft Draft { get; }
    public bool CanApply => commit != null && current?.Invoke() == true && Draft.TryGetChoice(out _);
    public bool TryApply()
    {
        if (commit == null || !Draft.TryGetChoice(out var choice)) return false;
        if (current?.Invoke() != true) { Dispose(); return false; }
        var write = commit;
        Dispose(); // A commit callback cannot reenter or retain the exact game/window context.
        write(choice); return true;
    }
    public void Dispose() { current = null; commit = null; }
}
