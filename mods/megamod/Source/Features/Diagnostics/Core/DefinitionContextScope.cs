using System;
using System.Threading;

namespace Megamod.Features.Diagnostics;

public sealed record DefinitionContext(string? DefinitionName, string? DeclaredKind, string? ModName,
    string? PackageId, string? ModRootDirectory, string? SourceFilePath);

public sealed class DefinitionContextScope
{
    private readonly ThreadLocal<Exit?> current = new();

    public DefinitionContext? Current => current.Value is { } frame && frame.IsVisible() ? frame.Context : null;
    public bool HasActiveFrame => current.Value != null;
    public void InvalidateCurrent() { if (current.Value is { } frame) frame.Visible = false; }
    public IDisposable Enter(DefinitionContext context, Func<bool>? remainsValid = null)
    {
        var entered = new Exit(this, context, current.Value, remainsValid);
        current.Value = entered;
        return entered;
    }

    private sealed class Exit : IDisposable
    {
        private readonly DefinitionContextScope owner;
        private readonly Exit? previous;
        private bool disposed;
        private readonly Func<bool>? remainsValid;
        public DefinitionContext Context { get; }
        public bool Visible { get; set; } = true;

        public Exit(DefinitionContextScope owner, DefinitionContext context, Exit? previous, Func<bool>? remainsValid)
        {
            this.owner = owner;
            this.previous = previous;
            Context = context;
            this.remainsValid = remainsValid;
        }

        public bool IsVisible()
        {
            if (!Visible) return false;
            // The runtime supplies only an owned, lock-free immutable publication check.
            try { if (remainsValid?.Invoke() == false) Visible = false; }
            catch (Exception) { Visible = false; }
            return Visible;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner.current.Value = ReferenceEquals(owner.current.Value, this) ? previous : null;
        }
    }
}
