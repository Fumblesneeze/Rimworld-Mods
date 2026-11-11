using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.Diagnostics;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.MessagePlacement;

// Run alone with the PrelaunchDisabled fixture staged into the exact isolated process.
// Setup creates only the message; enabling and the resulting movement use the native checkbox.
[RimWorldEndToEndTest("megamod.messages.native-startup-off-enable", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 600)]
public sealed class NativeMessageStartupOffTests : IRimWorldEndToEndTest
{
    private SettingsRuntime settings = null!;
    private MegamodMod mod = null!;
    private FieldInfo runtimeField = null!;
    private Message message = null!;
    private NativeDiagnosticSettingsView enabledView = null!;
    private readonly List<Dialog_ModSettings> ownedWindows = new();
    private EditWindow_Log? ownedLog;
    private string pointer = null!;
    private Rect native;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        EndToEndAssert.NotNull(settings.CurrentBinding, "A loaded disposable game is required.");
        EndToEndAssert.True(Prefs.DevMode);
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>() || Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.Enabled) && settings.Store.UsesGlobal(MessagePlacementSettings.PositionOffset));
        EndToEndAssert.True(settings.Store.EffectiveOffReason(MessagePlacementSettings.Enabled) == null);
        var globals = settings.Store.ExportGlobals();
        EndToEndAssert.True(globals.Values.TryGetValue(MessagePlacementSettings.Enabled.Id, out var enabled) && enabled == "false",
            "Run this scenario alone with the explicit PrelaunchDisabled XML fixture.");
        EndToEndAssert.False(settings.Store.GetEffective(MessagePlacementSettings.Enabled));
        EndToEndAssert.Equal(new MessageOffsets(10, 10), settings.Store.GetEffective(MessagePlacementSettings.PositionOffset));
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtimeField = typeof(MegamodMod).GetField("messagePlacement", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Message runtime observation is unavailable.");
        EndToEndAssert.True(runtimeField.GetValue(mod) == null, "Startup-off must not construct or admit the placement runtime.");
        EndToEndAssert.True(typeof(MessagePlacementRuntime).GetField("route", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null,
            "Startup-off must leave the owned native forwarding route absent.");
        var admission = (AdmissionCoordinator)typeof(MegamodMod).GetField("admission", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mod)!;
        var results = (IDictionary)typeof(AdmissionCoordinator).GetField("results", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(admission)!;
        EndToEndAssert.False(results.Contains("messages.native-placement"), "Startup-off must not attempt patch admission.");
        AssertNoMessagePatchWarnings();
        enabledView = new(() => pointer, MessagePlacementSettings.Enabled.Id);
        var scroll = enabledView.Scroll;
        var originalGame = Current.Game;
        var originalStack = Find.WindowStack;
        var originalTicks = Find.TickManager;
        var speed = originalTicks.CurTimeSpeed;
        var live = (List<Message>)(typeof(Messages).GetField("liveMessages", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null) ?? throw new InvalidOperationException("Native message list is unavailable."));
        EndToEndAssert.Equal(0, live.Count, "The empty-map fixture must provide one unambiguous message stack.");
        message = new Message("Startup-off placement verification", MessageTypeDefOf.NeutralEvent, new LookTargets(TargetInfo.Invalid));
        typeof(Message).GetField("startingTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(message, RealTime.LastRealTime + 900f);
        live.Add(message);
        context.DeferCleanup(() =>
        {
            if (!ReferenceEquals(originalGame, Current.Game) || !ReferenceEquals(originalStack, Find.WindowStack) ||
                !ReferenceEquals(originalTicks, Find.TickManager))
                throw new InvalidOperationException("Startup-off scenario ownership changed; cleanup refused replacement state.");
            var failures = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            foreach (var window in ownedWindows.Distinct())
                Attempt(() => { if (Find.WindowStack.Windows.Contains(window)) window.Close(); });
            Attempt(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(); });
            Attempt(() => live.Remove(message));
            Attempt(() => settings.Store.LoadGlobals(globals));
            Attempt(() => enabledView.RestoreScroll(scroll));
            Attempt(() => Find.TickManager.CurTimeSpeed = speed);
            Attempt(() => EndToEndAssert.False(live.Contains(message)));
            Attempt(() => EndToEndAssert.True(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key)
                .SequenceEqual(globals.Values.OrderBy(pair => pair.Key)), "Restore exact prelaunch preferences."));
            if (failures.Count != 0) throw new AggregateException("Startup-off message fixture cleanup failed.", failures);
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Shot("native message at startup with placement disabled and no owned route");
        yield return new AssertionStep("retain native rectangle before the enabling action", _ => native = ObserveRectangle());
        yield return new ModSettingsActionStep("open native settings to enable message placement", "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("retain pointer for native enable action", handle => pointer = handle);
        Rect editor = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return new NativeWindowCaptureStep("capture current native enable-control geometry", windows =>
            {
                var actual = Find.WindowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Settings did not open.");
                ownedWindows.Add(actual);
                enabledView.Bind(windows.Single(window => window.RuntimeType == typeof(Dialog_ModSettings).FullName), actual);
            });
            yield return new CheckpointStep("measure native global message enable checkbox", _ =>
            {
                editor = enabledView.MeasureCheckbox(out visible);
                return enabledView.Evidence(editor, visible);
            });
            if (visible) break;
            Vector2 point = default;
            yield return new AssertionStep("plan bounded native scroll", _ =>
            {
                EndToEndAssert.True(attempt < 3);
                point = enabledView.PlanTrackClick(editor);
            });
            yield return enabledView.Click("scroll native settings to enable checkbox", point);
        }
        yield return Shot("disabled global checkbox before native enable");
        yield return enabledView.Click("enable placement through native global checkbox", editor.center);
        yield return new WaitUntilStep("ordinary lifecycle admits placement after native enable", _ =>
                settings.Store.GetEffective(MessagePlacementSettings.Enabled) && Runtime?.Available == true,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(30)));
        yield return Shot("enabled global checkbox after native admission");
        yield return new NativePointerScopeEndStep("restore pointer after native enable", pointer);
        yield return new WindowCancelActionStep("close native settings after enabling placement", typeof(Dialog_ModSettings).FullName!);
        yield return Shot("same native message relocated by the newly enabled runtime");
        yield return new AssertionStep("native enable causally admits and moves the existing message", _ =>
        {
            EndToEndAssert.True(Runtime?.Available == true);
            EndToEndAssert.Equal(new MessageOffsets(10, 10), settings.Store.GetEffective(MessagePlacementSettings.PositionOffset));
            Rect actual = ObserveRectangle();
            var sample = new[] { new MessageBounds(native.x, native.y, native.width, native.height) };
            EndToEndAssert.True(Megamod.Features.MessagePlacement.MessagePlacement.TryCalculate(sample,
                UI.screenWidth, UI.screenHeight, null, new(10, 10), out var shift));
            EndToEndAssert.True(Math.Abs(actual.x - native.x - shift.X) < 2 && Math.Abs(actual.y - native.y - shift.Y) < 2);
            EndToEndAssert.True(Math.Abs(actual.x - native.x) > 1 || Math.Abs(actual.y - native.y) > 1,
                "The prepared message must visibly move after the real enabling action.");
            AssertNoMessagePatchWarnings();
        });
        yield return new CheckpointStep("startup-off native action-to-result evidence", _ => new Dictionary<string, string>
        {
            ["before"] = native.ToString(), ["after"] = message.lastDrawRect.ToString(),
            ["runtimeAvailable"] = Runtime?.Available.ToString() ?? "missing",
            ["scope"] = "native enable from explicit prelaunch off; message posting and automatic geometry remain outside this tracer"
        });
        yield return new NativeDeveloperLogActionStep("open native console after startup-off enable", true);
        yield return new AssertionStep("retain scenario-owned console", _ =>
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Console did not open."));
        yield return Shot("native console after startup-off enable and relocation");
        yield return new NativeDeveloperLogActionStep("restore closed native console", false);
    }

    private MessagePlacementRuntime? Runtime => runtimeField.GetValue(mod) as MessagePlacementRuntime;

    private Rect ObserveRectangle()
    {
        EndToEndAssert.True(Messages.IsLive(message) && !message.Expired);
        EndToEndAssert.True(message.lastDrawRect.width > 0 && message.lastDrawRect.height > 0);
        EndToEndAssert.True(Find.WindowStack.Windows.OfType<ImmediateWindow>().Any(window => window.windowRect == message.lastDrawRect));
        return message.lastDrawRect;
    }

    private void AssertNoMessagePatchWarnings()
    {
        const string prefix = "[fumblesneeze.megamod] Patch MSG-";
        EndToEndAssert.False(Log.Messages.Any(entry => entry.text.Contains(prefix, StringComparison.Ordinal)),
            "Disabled placement must not be reported as a failed enabled patch.");
        var diagnostics = (ProtectedDiagnosticsRuntime)typeof(MegamodMod).GetField("diagnostics", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mod)!;
        EndToEndAssert.Equal(0L, diagnostics.Sink.UndeliveredCount, "Missing diagnostics cannot establish warning absence.");
        string path = diagnostics.FallbackFilePath ?? throw new InvalidOperationException("The owned warning fallback is unavailable.");
        EndToEndAssert.Equal(Path.GetFullPath(Path.Combine(GenFilePaths.SaveDataFolderPath, "Logs")), Path.GetDirectoryName(Path.GetFullPath(path)));
        if (!File.Exists(path)) return;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        EndToEndAssert.True(stream.Length <= 1024 * 1024, "Startup warning observation exceeds its bound.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        EndToEndAssert.Equal(-1, stream.ReadByte(), "Warning file changed during bounded observation.");
        EndToEndAssert.False(new UTF8Encoding(false, true).GetString(bytes).Contains(prefix, StringComparison.Ordinal),
            "Owned fallback must not report disabled placement as a failed patch.");
    }

    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
}
