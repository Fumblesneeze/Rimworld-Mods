using System;
using System.Collections.Generic;

namespace RimWorldDevGateway.EndToEndTesting;

/// <summary>A captured native dialog. Bounds use GUI units; click coordinates use rendered pixels.</summary>
public sealed class EndToEndNativeWindow
{
    public EndToEndNativeWindow(string handle, string runtimeType, int nativeId, double x, double y,
        double width, double height, double uiScale, int pixelWidth, int pixelHeight, string? focusedTextControlHandle = null)
    {
        Handle = handle; RuntimeType = runtimeType; NativeId = nativeId;
        X = x; Y = y; Width = width; Height = height; UiScale = uiScale;
        PixelWidth = pixelWidth; PixelHeight = pixelHeight;
        FocusedTextControlHandle = focusedTextControlHandle;
    }
    public string Handle { get; }
    public string RuntimeType { get; }
    public int NativeId { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double UiScale { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public string? FocusedTextControlHandle { get; }
}

public sealed class NativeWindowReplaceTextActionStep : EndToEndStep
{
    public NativeWindowReplaceTextActionStep(string name, string windowHandle, string controlHandle, string text,
        string? expectedFailureCode = null) : base(name, EndToEndStepKind.Act)
    {
        WindowHandle = StepValues.Required(windowHandle, nameof(windowHandle));
        ControlHandle = StepValues.Required(controlHandle, nameof(controlHandle));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ExpectedFailureCode = expectedFailureCode == null ? null : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
    }
    public string WindowHandle { get; }
    public string ControlHandle { get; }
    public string Text { get; }
    public string? ExpectedFailureCode { get; }
}

public sealed class NativeWindowCaptureStep : EndToEndStep
{
    public NativeWindowCaptureStep(string name, Action<IReadOnlyList<EndToEndNativeWindow>> receive)
        : base(name, EndToEndStepKind.Observe) => Receive = receive ?? throw new ArgumentNullException(nameof(receive));
    public Action<IReadOnlyList<EndToEndNativeWindow>> Receive { get; }
}

public sealed class NativeDeveloperLogActionStep : EndToEndStep
{
    public NativeDeveloperLogActionStep(string name, bool open) : base(name, EndToEndStepKind.Act) => Open = open;
    public bool Open { get; }
}

public sealed class NativeMainTabActionStep : EndToEndStep
{
    public NativeMainTabActionStep(string name, string defName, bool open, string? expectedFailureCode = null)
        : base(name, EndToEndStepKind.Act)
    {
        DefName = StepValues.Required(defName, nameof(defName));
        Open = open;
        ExpectedFailureCode = expectedFailureCode == null ? null : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
    }
    public string DefName { get; }
    public bool Open { get; }
    public string? ExpectedFailureCode { get; }
}

public sealed class NativePointerScopeBeginStep : EndToEndStep
{
    public NativePointerScopeBeginStep(string name, Action<string> receive)
        : base(name, EndToEndStepKind.Act) => Receive = receive ?? throw new ArgumentNullException(nameof(receive));
    public Action<string> Receive { get; }
}

public sealed class NativePointerScopeEndStep : EndToEndStep
{
    public NativePointerScopeEndStep(string name, string scopeHandle) : base(name, EndToEndStepKind.Act)
        => ScopeHandle = StepValues.Required(scopeHandle, nameof(scopeHandle));
    public string ScopeHandle { get; }
}

public sealed class NativeWindowDragActionStep : EndToEndStep
{
    public NativeWindowDragActionStep(string name, string windowHandle, double startPixelX, double startPixelY,
        double endPixelX, double endPixelY, string? expectedFailureCode = null, string? pointerScopeHandle = null)
        : base(name, EndToEndStepKind.Act)
    {
        WindowHandle = StepValues.Required(windowHandle, nameof(windowHandle));
        foreach (double value in new[] { startPixelX, startPixelY, endPixelX, endPixelY })
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(startPixelX), "Native drag coordinates must be finite.");
        StartPixelX = startPixelX; StartPixelY = startPixelY; EndPixelX = endPixelX; EndPixelY = endPixelY;
        ExpectedFailureCode = expectedFailureCode == null ? null : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
        PointerScopeHandle = pointerScopeHandle == null ? null : StepValues.Required(pointerScopeHandle, nameof(pointerScopeHandle));
    }
    public string WindowHandle { get; }
    public double StartPixelX { get; }
    public double StartPixelY { get; }
    public double EndPixelX { get; }
    public double EndPixelY { get; }
    public string? ExpectedFailureCode { get; }
    public string? PointerScopeHandle { get; }
}

public sealed class NativeWindowClickActionStep : EndToEndStep
{
    public NativeWindowClickActionStep(string name, string windowHandle, double pixelX, double pixelY)
        : this(name, windowHandle, pixelX, pixelY, null) { }

    public NativeWindowClickActionStep(string name, string windowHandle, double pixelX, double pixelY,
        string? expectedFailureCode)
        : this(name, windowHandle, pixelX, pixelY, expectedFailureCode, null) { }

    public NativeWindowClickActionStep(string name, string windowHandle, double pixelX, double pixelY,
        string? expectedFailureCode, string? pointerScopeHandle)
        : base(name, EndToEndStepKind.Act)
    {
        WindowHandle = StepValues.Required(windowHandle, nameof(windowHandle));
        if (double.IsNaN(pixelX) || double.IsInfinity(pixelX) || double.IsNaN(pixelY) || double.IsInfinity(pixelY))
            throw new ArgumentOutOfRangeException(nameof(pixelX), "Native click coordinates must be finite.");
        PixelX = pixelX; PixelY = pixelY;
        ExpectedFailureCode = expectedFailureCode == null ? null
            : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
        PointerScopeHandle = pointerScopeHandle == null ? null
            : StepValues.Required(pointerScopeHandle, nameof(pointerScopeHandle));
    }
    public string WindowHandle { get; }
    public double PixelX { get; }
    public double PixelY { get; }
    public string? ExpectedFailureCode { get; }
    public string? PointerScopeHandle { get; }
}
