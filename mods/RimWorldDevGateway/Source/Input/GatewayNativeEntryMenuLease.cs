namespace RimWorldDevGateway;

#if RIMWORLD_1_7

internal sealed record GatewayNativeEntryMenuSnapshot(object Root, object UiRoot, object Stack,
    object Dispatcher, object Host, object Language, bool DevMode, int PixelWidth, int PixelHeight,
    double UiScale, string Control, string Label, Delegate Callback, double X, double Y,
    double Width, double Height, bool Enabled, long LayoutVersion);

internal sealed class GatewayNativeEntryMenuLease
{
    public GatewayNativeEntryMenuLease(GatewayNativeEntryMenuSnapshot captured) => Captured = captured;
    public GatewayNativeEntryMenuSnapshot Captured { get; }
    public string? Validate(GatewayNativeEntryMenuSnapshot current, long requiredLayoutVersion)
    {
        if (!ReferenceEquals(Captured.Root, current.Root) || !ReferenceEquals(Captured.UiRoot, current.UiRoot) ||
            !ReferenceEquals(Captured.Stack, current.Stack) || !ReferenceEquals(Captured.Dispatcher, current.Dispatcher) ||
            !ReferenceEquals(Captured.Host, current.Host) || !ReferenceEquals(Captured.Language, current.Language) ||
            Captured.DevMode != current.DevMode) return "stale_entry_context";
        if (!ValidGeometry(Captured) || !ValidGeometry(current)) return "invalid_entry_geometry";
        if (Captured.PixelWidth != current.PixelWidth || Captured.PixelHeight != current.PixelHeight ||
            Captured.UiScale != current.UiScale || Captured.X != current.X || Captured.Y != current.Y ||
            Captured.Width != current.Width || Captured.Height != current.Height) return "stale_entry_geometry";
        if (!Captured.Enabled || !current.Enabled || Captured.Control is not ("options" or "dev-quicktest") ||
            Captured.Control != current.Control || Captured.Label != current.Label ||
            !Captured.Callback.Equals(current.Callback)) return "stale_entry_control";
        return current.LayoutVersion < requiredLayoutVersion ? "native_entry_layout_unobserved" : null;
    }

    private static bool ValidGeometry(GatewayNativeEntryMenuSnapshot value)
    {
        double x = value.X + value.Width / 2, y = value.Y + value.Height / 2;
        return double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Width) &&
            double.IsFinite(value.Height) && double.IsFinite(value.UiScale) && value.Width > 0 &&
            value.Height > 0 && value.UiScale > 0 && value.PixelWidth > 0 && value.PixelHeight > 0 &&
            x >= 0 && y >= 0 && x < value.PixelWidth && y < value.PixelHeight;
    }
}

internal sealed class GatewayNativeEntryMenuInventory
{
    private readonly Dictionary<string, GatewayNativeEntryMenuSnapshot?> pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GatewayNativeEntryMenuSnapshot> current = new(StringComparer.Ordinal);
    private long pendingVersion;
    private bool collecting;
    public void Begin(long version)
    { current.Clear(); pending.Clear(); pendingVersion = version; collecting = true; }
    public void Observe(GatewayNativeEntryMenuSnapshot control)
    {
        if (!collecting || control.LayoutVersion != pendingVersion || control.Control is not ("options" or "dev-quicktest")) return;
        if (pending.ContainsKey(control.Control)) pending[control.Control] = null;
        else pending.Add(control.Control, control);
    }
    public void Complete(long version, bool success)
    {
        if (!collecting || pendingVersion != version)
        { current.Clear(); pending.Clear(); collecting = false; return; }
        collecting = false;
        if (success)
            foreach (var pair in pending)
                if (pair.Value != null) current.Add(pair.Key, pair.Value);
        pending.Clear();
    }
    public GatewayNativeEntryMenuSnapshot? Find(string control)
        => current.TryGetValue(control, out var result) ? result : null;
}

// One active enclosing layout; native callbacks supply actual event/dispatcher provenance.
internal sealed class GatewayNativeEntryMenuObservation
{
    private readonly GatewayNativeEntryMenuInventory inventory;
    private readonly long version;
    private readonly object input, dispatcher, host;
    private readonly bool drawing;
    private readonly int type, raw;
    private bool menuReturned;
    private bool failed, pendingActive, pendingComplete;
    public GatewayNativeEntryMenuObservation(GatewayNativeEntryMenuInventory inventory, long version,
        object input, object dispatcher, object host, bool drawing, int type, int raw)
    {
        this.inventory = inventory; this.version = version; this.input = input;
        this.dispatcher = dispatcher; this.host = host; this.drawing = drawing;
        this.type = type; this.raw = raw; inventory.Begin(version);
    }
    public void Observe(GatewayNativeEntryMenuSnapshot control) => inventory.Observe(control);
    public void Fail() => failed = true;
    public void BeginPendingWindows()
    {
        if (pendingActive || pendingComplete || !menuReturned) Fail();
        pendingActive = true;
    }
    public void EndPendingWindows(bool success)
    {
        if (!pendingActive || !success) Fail();
        pendingActive = false; pendingComplete = success;
    }
    public void ClearPendingWindows() { if (!pendingActive) Fail(); }
    public void MenuReturned(bool original)
    {
        if (!original || menuReturned) Fail();
        menuReturned = true;
    }
    public void Complete(object input, object dispatcher, object host, bool drawing, int type, int raw,
        bool original, bool authority) => inventory.Complete(version, !failed && !pendingActive && pendingComplete &&
            original && menuReturned && authority &&
            ReferenceEquals(this.input, input) && ReferenceEquals(this.dispatcher, dispatcher) &&
            ReferenceEquals(this.host, host) && this.drawing == drawing && this.type == type && this.raw == raw);
}
#endif
