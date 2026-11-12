using System;

namespace RimWorldDevGateway.EndToEndTesting;

public interface IEndToEndNativeCaravanObservation
{
    // Passive publication from a completed native layout, never a draw or selection command.
    EndToEndCaravanHealthObservation ObserveHealth(string caravanHandle, string pawnLoadId);
}
public sealed class EndToEndCaravanHealthObservation
{
    public EndToEndCaravanHealthObservation(bool rowVisible, bool detailVisible, long layoutVersion)
    { RowVisible = rowVisible; DetailVisible = detailVisible; LayoutVersion = layoutVersion; }
    public bool RowVisible { get; }
    public bool DetailVisible { get; }
    public long LayoutVersion { get; }
}

public abstract class NativeCaravanStep : EndToEndStep
{
    protected NativeCaravanStep(string name, EndToEndStepKind kind) : base(name, kind) { }
}
public sealed class NativeWorldViewCaptureStep : NativeCaravanStep
{
    public NativeWorldViewCaptureStep(string name, Action<string> receive) : base(name, EndToEndStepKind.Observe)
        => Receive = receive ?? throw new ArgumentNullException(nameof(receive));
    public Action<string> Receive { get; }
}
public sealed class NativeWorldViewRestoreStep : NativeCaravanStep
{
    public NativeWorldViewRestoreStep(string name, string viewHandle) : base(name, EndToEndStepKind.Act)
        => ViewHandle = StepValues.Required(viewHandle, nameof(viewHandle));
    public string ViewHandle { get; }
}
public sealed class NativeWorldViewActionStep : NativeCaravanStep
{
    public NativeWorldViewActionStep(string name, string viewHandle, bool open) : base(name, EndToEndStepKind.Act)
    { ViewHandle = StepValues.Required(viewHandle, nameof(viewHandle)); Open = open; }
    public string ViewHandle { get; }
    public bool Open { get; }
}
public sealed class NativeCaravanCaptureStep : NativeCaravanStep
{
    public NativeCaravanCaptureStep(string name, string viewHandle, string worldObjectLoadId, Action<string> receive)
        : base(name, EndToEndStepKind.Observe)
    { ViewHandle = StepValues.Required(viewHandle, nameof(viewHandle)); WorldObjectLoadId = StepValues.Required(worldObjectLoadId, nameof(worldObjectLoadId));
      Receive = receive ?? throw new ArgumentNullException(nameof(receive)); }
    public string ViewHandle { get; }
    public string WorldObjectLoadId { get; }
    public Action<string> Receive { get; }
}
public sealed class NativeCaravanSelectStep : NativeCaravanStep
{
    public NativeCaravanSelectStep(string name, string caravanHandle) : base(name, EndToEndStepKind.Act)
        => CaravanHandle = StepValues.Required(caravanHandle, nameof(caravanHandle));
    public string CaravanHandle { get; }
}
public enum EndToEndCaravanTab { Items, Health }
public sealed class NativeCaravanTabStep : NativeCaravanStep
{
    public NativeCaravanTabStep(string name, string caravanHandle, EndToEndCaravanTab tab, bool open) : base(name, EndToEndStepKind.Act)
    {
        CaravanHandle = StepValues.Required(caravanHandle, nameof(caravanHandle));
        if (tab != EndToEndCaravanTab.Items && tab != EndToEndCaravanTab.Health) throw new ArgumentOutOfRangeException(nameof(tab));
        Tab = tab; Open = open;
    }
    public string CaravanHandle { get; }
    public EndToEndCaravanTab Tab { get; }
    public bool Open { get; }
}
public sealed class NativeCaravanHealthCaptureStep : NativeCaravanStep
{
    public NativeCaravanHealthCaptureStep(string name, string caravanHandle, string pawnLoadId, Action<string> receive)
        : base(name, EndToEndStepKind.Observe)
    { CaravanHandle = StepValues.Required(caravanHandle, nameof(caravanHandle)); PawnLoadId = StepValues.Required(pawnLoadId, nameof(pawnLoadId));
      Receive = receive ?? throw new ArgumentNullException(nameof(receive)); }
    public string CaravanHandle { get; }
    public string PawnLoadId { get; }
    public Action<string> Receive { get; }
}
public sealed class NativeCaravanHealthClickStep : NativeCaravanStep
{
    public NativeCaravanHealthClickStep(string name, string controlHandle, string? expectedFailureCode = null) : base(name, EndToEndStepKind.Act)
    { ControlHandle = StepValues.Required(controlHandle, nameof(controlHandle));
      ExpectedFailureCode = expectedFailureCode == null ? null : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode)); }
    public string ControlHandle { get; }
    public string? ExpectedFailureCode { get; }
}
