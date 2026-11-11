using System.Reflection;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.MessagePlacement;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.MessagePlacement;

// The message/nonzero preferences are fixture preconditions. Only native settings clicks are
// acceptance actions. This does not certify ordinary gameplay message creation or automatic clearance.
[RimWorldEndToEndTest("megamod.messages.native-manual-reset-disable", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 900)]
public sealed class NativeMessageManualPlacementTests : IRimWorldEndToEndTest
{
    private SettingsRuntime settings = null!;
    private Message message = null!;
    private NativeDiagnosticSettingsView enabledView = null!, resetView = null!;
    private readonly List<Dialog_ModSettings> ownedWindows = new();
    private string pointer = null!;
    private Rect shifted, native;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        var binding = settings.CurrentBinding ?? throw new InvalidOperationException("A loaded disposable game is required.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>());
        EndToEndAssert.True(Prefs.DevMode);
        EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(settings.Store.EffectiveOffReason(MessagePlacementSettings.Enabled) == null);
        EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.Enabled) &&
            settings.Store.UsesGlobal(MessagePlacementSettings.PositionOffset), "Fresh inherited message settings are required.");
        var globals = settings.Store.ExportGlobals();
        var speed = Find.TickManager.CurTimeSpeed;
        enabledView = new(() => pointer, MessagePlacementSettings.Enabled.Id);
        resetView = new(() => pointer, MessagePlacementSettings.PositionOffset.Id, 148);
        var scroll = enabledView.Scroll;
        var live = (List<Message>)(typeof(Messages).GetField("liveMessages", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null) ?? throw new InvalidOperationException("Native message list is unavailable."));
        EndToEndAssert.True(live.Count < 12, "The fixture must not evict another native message.");
        message = new Message("Manual message placement verification", MessageTypeDefOf.NeutralEvent, new LookTargets(TargetInfo.Invalid));
        // This one disposable message remains visible while real native settings actions execute.
        typeof(Message).GetField("startingTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(message, RealTime.LastRealTime + 1200f);
        live.Add(message);
        context.DeferCleanup(() =>
        {
            var failures = new List<Exception>();
            void Attempt(Action action)
            {
                try { action(); } catch (Exception error) { failures.Add(error); }
            }
            foreach (var window in ownedWindows.Distinct())
                Attempt(() => { if (Find.WindowStack.Windows.Contains(window)) window.Close(); });
            Attempt(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(); });
            Attempt(() => live.Remove(message));
            Attempt(() => settings.Store.LoadGlobals(globals));
            Attempt(() => enabledView.RestoreScroll(scroll));
            Attempt(() => Find.TickManager.CurTimeSpeed = speed);
            Attempt(() => EndToEndAssert.False(live.Contains(message), "Disposable message cleanup must remove exactly the fixture."));
            Attempt(() => EndToEndAssert.True(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key)
                .SequenceEqual(globals.Values.OrderBy(pair => pair.Key)), "Cleanup must restore the initial global settings."));
            if (failures.Count != 0) throw new AggregateException("Message placement fixture cleanup failed.", failures);
        });
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.Enabled, true).Succeeded);
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.PositionOffset, new(10, 10)).Succeeded);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Shot("prepared native message with nonzero manual offsets");
        yield return new AssertionStep("retain actually rendered shifted rectangle", _ => shifted = ObserveRectangle());
        foreach (var step in ClickSetting(enabledView, "disable manual placement")) yield return step;
        yield return new AssertionStep("native checkbox disabled placement and retained offset pair", _ =>
        {
            EndToEndAssert.False(settings.Store.GetEffective(MessagePlacementSettings.Enabled));
            EndToEndAssert.Equal(new MessageOffsets(10, 10), settings.Store.GetEffective(MessagePlacementSettings.PositionOffset));
            native = ObserveRectangle();
            EndToEndAssert.True(Math.Abs(shifted.x - native.x - UI.screenWidth * 0.1f) < 2);
            EndToEndAssert.True(Math.Abs(shifted.y - native.y - UI.screenHeight * 0.1f) < 2);
        });
        foreach (var step in ClickSetting(enabledView, "reenable manual placement")) yield return step;
        yield return new AssertionStep("native reenable restores the retained offset pair", _ =>
        {
            EndToEndAssert.True(settings.Store.GetEffective(MessagePlacementSettings.Enabled));
            EndToEndAssert.Equal(shifted, ObserveRectangle());
        });
        foreach (var step in ClickSetting(resetView, "reset both global offsets to zero")) yield return step;
        yield return new AssertionStep("native reset restores native coordinates without disabling placement", _ =>
        {
            EndToEndAssert.True(settings.Store.GetEffective(MessagePlacementSettings.Enabled));
            EndToEndAssert.Equal(default(MessageOffsets), settings.Store.GetEffective(MessagePlacementSettings.PositionOffset));
            EndToEndAssert.Equal(native, ObserveRectangle());
        });
        yield return new CheckpointStep("manual movement action-to-result coordinates", _ => new Dictionary<string, string>
        {
            ["shifted"] = shifted.ToString(), ["native"] = native.ToString(), ["final"] = message.lastDrawRect.ToString(),
            ["scope"] = "native global checkbox/reset actions; message posting and automatic clearance not accepted"
        });
        yield return new NativeDeveloperLogActionStep("open native developer console after manual placement", true);
        yield return new AssertionStep("retain only the scenario-opened native console", _ =>
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Developer console did not open."));
        yield return Shot("native developer console after manual placement actions");
        yield return new NativeDeveloperLogActionStep("restore closed native developer console", false);
    }

    private IEnumerable<EndToEndStep> ClickSetting(NativeDiagnosticSettingsView view, string action)
    {
        yield return new ModSettingsActionStep("open settings to " + action, "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("retain pointer for " + action, handle => pointer = handle);
        Rect editor = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return new NativeWindowCaptureStep("capture native message settings geometry", windows =>
            {
                var actual = Find.WindowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Settings did not open.");
                ownedWindows.Add(actual);
                view.Bind(windows.Single(item => item.RuntimeType == typeof(Dialog_ModSettings).FullName), actual);
            });
            yield return new CheckpointStep("measure message settings control", _ =>
            {
                editor = view.MeasureCheckbox(out visible);
                return view.Evidence(editor, visible);
            });
            if (visible) break;
            Vector2 point = default;
            yield return new AssertionStep("plan bounded native scroll to message control", _ =>
            {
                EndToEndAssert.True(attempt < 3, "Native track clicks did not expose the control.");
                point = view.PlanTrackClick(editor);
            });
            yield return view.Click("scroll native settings to message control", point);
        }
        yield return Shot("before native action to " + action);
        yield return view.Click(action, editor.center);
        yield return Shot("settings after native action to " + action);
        yield return new NativePointerScopeEndStep("restore pointer after " + action, pointer);
        yield return new WindowCancelActionStep("close settings after " + action, typeof(Dialog_ModSettings).FullName!);
        yield return Shot("native message after " + action);
    }

    private Rect ObserveRectangle()
    {
        EndToEndAssert.True(Messages.IsLive(message) && !message.Expired);
        EndToEndAssert.True(message.lastDrawRect.width > 0 && message.lastDrawRect.height > 0);
        EndToEndAssert.True(Find.WindowStack.Windows.OfType<ImmediateWindow>().Any(window => window.windowRect == message.lastDrawRect),
            "Native message and native window hitbox must have the same rectangle.");
        return message.lastDrawRect;
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
}
