namespace RimWorldDevGateway;

internal sealed class GatewayNativeTextControlLease
{
    // Native QuickSearch reserves its 18-unit icon plus 8-unit spacing only while text is active.
    // Keep the original capture as the baseline; accepting a width change never rebinds identity.
    private const double MaximumWidthChange = 26;
    private readonly object editor;
    private readonly int control, window;
    private readonly double x, y, width, height;
    public GatewayNativeTextControlLease(object editor, int control, int window, double x, double y, double width, double height)
    { this.editor = editor; this.control = control; this.window = window; this.x = x; this.y = y; this.width = width; this.height = height; }
    public string? Validate(object? currentEditor, int currentControl, int currentWindow, bool writable, bool multiline,
        double currentX, double currentY, double currentWidth, double currentHeight)
    {
        if (!writable || multiline) return "native_text_field_unavailable";
        return control != 0 && window >= 0 && ReferenceEquals(editor, currentEditor) && currentControl == control &&
            currentWindow == window && currentX == x && currentY == y && currentHeight == height &&
            Finite(currentWidth) && currentWidth > 0 && System.Math.Abs(currentWidth - width) <= MaximumWidthChange &&
            Finite(x) && Finite(y) && Finite(width) && Finite(height) && width > 0 && height > 0
            ? null : "stale_native_text_focus";
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
