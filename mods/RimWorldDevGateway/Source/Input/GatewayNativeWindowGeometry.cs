namespace RimWorldDevGateway;

// Window bounds are GUI units; request coordinates and extent are rendered pixels.
internal sealed class GatewayNativeWindowGeometry
{
    public GatewayNativeWindowGeometry(double x, double y, double width, double height,
        double uiScale, int pixelWidth, int pixelHeight, double mouseOriginOffset = 0)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        UiScale = uiScale;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        MouseOriginOffset = mouseOriginOffset;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double UiScale { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public double MouseOriginOffset { get; }

    public GatewayNativeInputPoint ResolvePoint(double pixelX, double pixelY)
    {
        if (!Finite(X) || !Finite(Y) || !Finite(Width) || !Finite(Height) ||
            !Finite(X + Width) || !Finite(Y + Height) || !Finite(UiScale) ||
            Width <= 0 || Height <= 0 || UiScale <= 0 || PixelWidth <= 0 || PixelHeight <= 0 ||
            MouseOriginOffset is not (0 or 1))
            return new("invalid_window_geometry", 0, 0);
        double guiX = pixelX / UiScale;
        double guiY = pixelY / UiScale;
        // Positive containment also rejects NaN and infinities without clamping elsewhere.
        if (!(guiX >= X && guiX < X + Width && guiY >= Y && guiY < Y + Height))
            return new("point_outside_window", 0, 0);
        if (!(pixelX >= 0 && pixelX < PixelWidth && pixelY >= 0 && pixelY < PixelHeight))
            return new("point_outside_viewport", 0, 0);
        return new(null, pixelX, PixelHeight - MouseOriginOffset - pixelY);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

internal sealed class GatewayNativeInputPoint
{
    public GatewayNativeInputPoint(string? error, double inputX, double inputY)
    {
        Error = error;
        InputX = inputX;
        InputY = inputY;
    }

    public string? Error { get; }
    public double InputX { get; }
    public double InputY { get; }
}
