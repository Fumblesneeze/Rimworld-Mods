namespace RimWorldDevGateway;

internal enum GatewayNativeCaravanRowGeometry { Unsupported, Clipped, Visible }

internal sealed class GatewayNativeCaravanDetailObservation
{
    private readonly object window;
    private readonly Func<bool> current;
    private Invocation? active;
    private bool entered, cardReturned, completed, failed;
    internal GatewayNativeCaravanDetailObservation(object window, Func<bool> current)
    { this.window = window; this.current = current; }
    internal Invocation? Enter(object candidate, bool original)
    {
        if (!ReferenceEquals(window, candidate)) return null;
        if (entered || !original || !current()) { failed = true; return null; }
        entered = true;
        return active = new(this);
    }
    internal void CardReturned() { if (active != null && current()) cardReturned = true; }
    internal bool Visible => completed && !failed && active == null && current();
    private void Returned(Invocation invocation, bool original, Exception? exception)
    {
        if (!ReferenceEquals(active, invocation) || !original || exception != null || !current()) failed = true;
        completed = !failed && cardReturned;
        active = null;
    }
    internal sealed class Invocation
    {
        private readonly GatewayNativeCaravanDetailObservation owner;
        internal Invocation(GatewayNativeCaravanDetailObservation owner) => this.owner = owner;
        internal void Complete(bool original, Exception? exception) => owner.Returned(this, original, exception);
    }
}

internal static class GatewayNativeCaravanGeometry
{
    internal static GatewayNativeCaravanRowGeometry Classify(double left, double top, double right, double bottom,
        double clipLeft, double clipTop, double clipRight, double clipBottom)
    {
        if (new[] { left, top, right, bottom, clipLeft, clipTop, clipRight, clipBottom }.Any(v => double.IsNaN(v) || double.IsInfinity(v)) ||
            right <= left || bottom <= top || clipRight < clipLeft || clipBottom < clipTop)
            return GatewayNativeCaravanRowGeometry.Unsupported;
        return left >= clipLeft && top >= clipTop && right <= clipRight && bottom <= clipBottom
            ? GatewayNativeCaravanRowGeometry.Visible : GatewayNativeCaravanRowGeometry.Clipped;
    }
}

internal sealed record GatewayNativeCaravanHealthSnapshot(object Game, object World, object Root, object Ui,
    object Stack, object Pane, object Caravan, object Tab, object Pawn, object Window, object Dispatcher,
    object Host, object Language, int WindowId, double X, double Y, double Width, double Height,
    double UiScale, int PixelWidth, int PixelHeight, bool Enabled, long LayoutVersion);

internal sealed class GatewayNativeCaravanHealthLease
{
    internal GatewayNativeCaravanHealthLease(GatewayNativeCaravanHealthSnapshot captured) => Captured = captured;
    internal GatewayNativeCaravanHealthSnapshot Captured { get; }
    internal string? Validate(GatewayNativeCaravanHealthSnapshot current, long requiredLayout)
    {
        var before = Captured;
        if (!ReferenceEquals(before.Game, current.Game) || !ReferenceEquals(before.World, current.World) ||
            !ReferenceEquals(before.Root, current.Root) || !ReferenceEquals(before.Ui, current.Ui) ||
            !ReferenceEquals(before.Stack, current.Stack) || !ReferenceEquals(before.Pane, current.Pane) ||
            !ReferenceEquals(before.Caravan, current.Caravan) || !ReferenceEquals(before.Tab, current.Tab) ||
            !ReferenceEquals(before.Pawn, current.Pawn) || !ReferenceEquals(before.Window, current.Window) ||
            !ReferenceEquals(before.Dispatcher, current.Dispatcher) || !ReferenceEquals(before.Host, current.Host) ||
            !ReferenceEquals(before.Language, current.Language) || before.WindowId != -235086 || current.WindowId != before.WindowId)
            return "stale_caravan_health_context";
        if (!Valid(before) || !Valid(current) || before.X != current.X || before.Y != current.Y ||
            before.Width != current.Width || before.Height != current.Height || before.UiScale != current.UiScale ||
            before.PixelWidth != current.PixelWidth || before.PixelHeight != current.PixelHeight)
            return "stale_caravan_health_geometry";
        if (!before.Enabled || !current.Enabled) return "native_caravan_health_disabled";
        return current.LayoutVersion < requiredLayout ? "native_caravan_health_layout_unobserved" : null;
    }

    private static bool Valid(GatewayNativeCaravanHealthSnapshot value) =>
        !double.IsNaN(value.X) && !double.IsInfinity(value.X) && !double.IsNaN(value.Y) && !double.IsInfinity(value.Y) &&
        !double.IsNaN(value.UiScale) && !double.IsInfinity(value.UiScale) && value.PixelWidth > 0 && value.PixelHeight > 0 &&
        value.X >= 0 && value.Y >= 0 && value.Width > 0 && value.Height > 0 && value.UiScale > 0 &&
        value.X + value.Width <= value.PixelWidth && value.Y + value.Height <= value.PixelHeight;
}

internal sealed class GatewayNativeCaravanLayoutProof
{
    private readonly object input, dispatcher, host;
    private readonly bool drawing;
    private readonly int type, raw;
    private bool completed;
    internal GatewayNativeCaravanLayoutProof(object input, object dispatcher, object host, bool drawing, int type, int raw)
    { this.input = input; this.dispatcher = dispatcher; this.host = host; this.drawing = drawing; this.type = type; this.raw = raw; }
    internal bool Failed { get; private set; }
    internal bool BodyCompleted { get; private set; }
    internal bool PendingActive { get; private set; }
    internal bool PendingComplete { get; private set; }
    internal void Fail() => Failed = true;
    internal bool ObserveRow(GatewayNativeCaravanRowGeometry geometry)
    {
        if (geometry == GatewayNativeCaravanRowGeometry.Unsupported) Fail();
        return geometry == GatewayNativeCaravanRowGeometry.Visible;
    }
    internal void BeginPending()
    { if (PendingActive || PendingComplete) Fail(); PendingActive = true; }
    internal void EndPending(bool success)
    { if (!PendingActive || !success) Fail(); PendingActive = false; PendingComplete = success; }
    internal void ClearPending() { if (!PendingActive) Fail(); }
    internal void BodyReturned(bool success)
    { if (!PendingActive || !success || BodyCompleted) Fail(); BodyCompleted = success; }
    internal bool Complete(object input, object dispatcher, object host, bool drawing, int type, int raw, bool original, bool authority)
    {
        bool result = !completed && !Failed && PendingComplete && !PendingActive && original && authority &&
            ReferenceEquals(this.input, input) && ReferenceEquals(this.dispatcher, dispatcher) && ReferenceEquals(this.host, host) &&
            this.drawing == drawing && this.type == type && this.raw == raw && type == raw;
        completed = true; return result;
    }
}
