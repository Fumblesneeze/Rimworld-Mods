namespace RimWorldDevGateway;

internal sealed record GatewayCaravanState(string LoadId, string Label, string[] PawnLoadIds,
    bool Current, bool Selected, bool PaneOpen, string? Tab, string? DetailPawnLoadId, bool Ready = true);
internal interface IGatewayCaravanInspectionRuntime
{
    GatewayAutomationAvailability Availability();
    IGatewayCaravanInspection Capture(IGatewayWorldViewSession view, string loadId);
}
internal interface IGatewayCaravanInspection
{
    GatewayCaravanState Observe();
    void Select(Action admission);
    void ToggleTab(string tab, Action admission);
}

internal sealed class GatewayCaravanInspectionAutomation
{
    private readonly GatewayWorldViewAutomation worlds;
    private readonly Dictionary<string, (string View, IGatewayCaravanInspection Caravan)> captures = new(StringComparer.Ordinal);
    private string? captureView;
    internal GatewayCaravanInspectionAutomation(GatewayAutomationRegistry registry, GatewayWorldViewAutomation worlds,
        IGatewayCaravanInspectionRuntime runtime)
    {
        this.worlds = worlds;
        Register("native-caravan.capture", new[] { "viewHandle", "worldObjectLoadId" }, false, (context, args) =>
        {
            string view = (string)args["viewHandle"]!;
            var owner = worlds.Require(view);
            if (captureView != view) { captures.Clear(); captureView = view; }
            if (captures.Count >= 16) throw Error("native_caravan_capture_limit");
            var caravan = runtime.Capture(owner, (string)args["worldObjectLoadId"]!);
            var observed = caravan.Observe();
            if (!observed.Current) throw Error("stale_caravan_handle");
            string handle = Guid.NewGuid().ToString("N");
            captures.Add(handle, (view, caravan));
            return new Dictionary<string, object?> { ["caravanHandle"] = handle, ["state"] = observed };
        });
        Register("native-caravan.select", new[] { "caravanHandle" }, true, (context, args) =>
        {
            var caravan = Require((string)args["caravanHandle"]!);
            return Act(context, caravan, () => caravan.Select(() => Admission(context, "select")), after => after.Selected);
        });
        Register("native-caravan.tab", new[] { "caravanHandle", "tab", "open" }, true, (context, args) =>
        {
            string tab = (string)args["tab"]!;
            if (tab is not ("items" or "health")) throw Error("invalid_native_caravan_arguments");
            bool open = (bool)args["open"]!;
            var caravan = Require((string)args["caravanHandle"]!);
            var before = caravan.Observe();
            if (!before.Selected || !before.PaneOpen || !before.Ready) throw Error("native_caravan_pane_unavailable");
            bool invoke = open ? before.Tab != tab : before.Tab == tab;
            return Act(context, caravan, () => { if (invoke) caravan.ToggleTab(tab, () => Admission(context, "tab")); },
                after => after.Selected && after.PaneOpen && (open ? after.Tab == tab : after.Tab != tab));
        });

        void Register(string name, string[] keys, bool mutating, GatewayAutomationHandler handler) =>
            registry.RegisterBuiltIn(new GatewayAutomationDescriptor(name, "1.0",
                "Exact owned caravan inspection through the ordinary native World command path.", GatewayCaravanArguments.Schema(keys),
                new[] { "current-world-view-lease", "exact-player-caravan" }, mutating), (context, args) =>
                { GatewayCaravanArguments.Validate(args, keys); return handler(context, args); }, runtime.Availability);
    }

    internal IGatewayCaravanInspection Require(string handle)
    {
        if (!captures.TryGetValue(handle, out var capture)) throw Error("stale_caravan_handle");
        worlds.Require(capture.View);
        if (!capture.Caravan.Observe().Current) throw Error("stale_caravan_handle");
        return capture.Caravan;
    }

    internal IGatewayCaravanInspection? CurrentSelected()
    {
        foreach (var capture in captures.Values.Reverse())
        {
            try
            {
                worlds.Require(capture.View);
                if (capture.Caravan.Observe() is { Current: true, Selected: true }) return capture.Caravan;
            }
            catch (GatewayAutomationException) { }
        }
        return null;
    }

    private static GatewayCaravanState Act(GatewayAutomationContext context, IGatewayCaravanInspection caravan,
        Action command, Func<GatewayCaravanState, bool> observed)
    {
        context.AddArtifact("native-caravan-before", "native-caravan-state", caravan.Observe());
        try { command(); }
        catch
        {
            try { context.AddArtifact("native-caravan-after", "native-caravan-state", caravan.Observe()); }
            catch { context.AddArtifact("native-caravan-after-unavailable", "native-observation-error", true); }
            throw;
        }
        var after = caravan.Observe();
        context.AddArtifact("native-caravan-after", "native-caravan-state", after);
        if (!after.Current || !observed(after)) throw Error("native_caravan_state_not_observed");
        return after;
    }
    private static void Admission(GatewayAutomationContext context, string command)
    {
        context.RunStep("native-caravan-command-admission", () => { });
        context.AddArtifact("native-caravan-invocation", "native-caravan-command", new Dictionary<string, object?>
            { ["command"] = command, ["attempted"] = true });
    }
    private static GatewayAutomationException Error(string code) => new(code, "The exact current owned caravan and native inspection state are required.");
}
