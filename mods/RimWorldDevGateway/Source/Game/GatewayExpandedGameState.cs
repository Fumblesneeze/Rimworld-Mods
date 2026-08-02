using System.Collections.ObjectModel;

namespace RimWorldDevGateway;

public sealed class GatewayExpandedGameStateSnapshot
{
    public const int MaximumSelectedThings = 256;

    public GatewayExpandedGameStateSnapshot(
        GatewayGameStateSnapshot control,
        GatewayCameraSnapshot? camera,
        IEnumerable<GatewayThingSummary>? selection)
    {
        if (control is null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        var selected = (selection ?? Array.Empty<GatewayThingSummary>()).ToArray();
        DevMode = control.DevMode;
        GodMode = control.GodMode;
        EffectiveGodMode = control.EffectiveGodMode;
        DevModePermanentlyDisabled = control.DevModePermanentlyDisabled;
        Paused = control.Paused;
        ForcePaused = control.ForcePaused;
        Speed = control.Speed;
        Camera = camera;
        CurrentMapHandle = camera?.MapHandle ?? selected.FirstOrDefault()?.MapHandle;
        Selection = new ReadOnlyCollection<GatewayThingSummary>(
            selected.Take(MaximumSelectedThings).ToArray());
        SelectionTruncated = selected.Length > MaximumSelectedThings;
    }

    public bool DevMode { get; }

    public bool GodMode { get; }

    public bool EffectiveGodMode { get; }

    public bool DevModePermanentlyDisabled { get; }

    public bool? Paused { get; }

    public bool? ForcePaused { get; }

    public GatewayGameSpeed? Speed { get; }

    public string? CurrentMapHandle { get; }

    public GatewayCameraSnapshot? Camera { get; }

    public IReadOnlyList<GatewayThingSummary> Selection { get; }

    public bool SelectionTruncated { get; }
}
