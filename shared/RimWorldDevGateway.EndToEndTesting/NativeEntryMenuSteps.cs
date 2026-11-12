using System;
using System.Collections.Generic;

namespace RimWorldDevGateway.EndToEndTesting;

/// <summary>One observed inline Entry button; bounds are top-left rendered pixels.</summary>
public sealed class EndToEndNativeEntryMenuControl
{
    public EndToEndNativeEntryMenuControl(string handle, string control, string label, double x, double y,
        double width, double height, double uiScale, int pixelWidth, int pixelHeight)
    {
        Handle = handle; Control = control; Label = label; X = x; Y = y; Width = width; Height = height;
        UiScale = uiScale; PixelWidth = pixelWidth; PixelHeight = pixelHeight;
    }
    public string Handle { get; }
    public string Control { get; }
    public string Label { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double UiScale { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
}

public sealed class NativeEntryMenuCaptureStep : EndToEndStep
{
    public NativeEntryMenuCaptureStep(string name, Action<IReadOnlyList<EndToEndNativeEntryMenuControl>> receive)
        : base(name, EndToEndStepKind.Observe) => Receive = receive ?? throw new ArgumentNullException(nameof(receive));
    public Action<IReadOnlyList<EndToEndNativeEntryMenuControl>> Receive { get; }
}

public sealed class NativeEntryMenuClickActionStep : EndToEndStep
{
    public NativeEntryMenuClickActionStep(string name, string controlHandle, string? expectedFailureCode = null)
        : base(name, EndToEndStepKind.Act)
    {
        ControlHandle = StepValues.Required(controlHandle, nameof(controlHandle));
        ExpectedFailureCode = expectedFailureCode == null ? null : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
    }
    public string ControlHandle { get; }
    public string? ExpectedFailureCode { get; }
}
