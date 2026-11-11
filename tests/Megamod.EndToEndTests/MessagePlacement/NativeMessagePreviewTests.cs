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

// Native opening/reset/closing are the actions. The initial nonzero preference is fixture setup.
[RimWorldEndToEndTest("megamod.messages.native-preview-reset", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 900)]
public sealed class NativeMessagePreviewTests : IRimWorldEndToEndTest
{
    private SettingsRuntime settings = null!;
    private NativeDiagnosticSettingsView reset = null!, preview = null!, previewTop = null!;
    private readonly List<Dialog_ModSettings> ownedWindows = new();
    private List<Message> live = null!;
    private IArchivable[] archive = null!;
    private string pointer = null!;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        EndToEndAssert.NotNull(settings.CurrentBinding, "A loaded disposable game is required.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>());
        EndToEndAssert.True(Prefs.DevMode);
        EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.PositionOffset));
        EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.Enabled));
        EndToEndAssert.True(settings.Store.EffectiveOffReason(MessagePlacementSettings.Enabled) == null);
        var placement = typeof(MegamodMod).GetField("messagePlacement", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(LoadedModManager.GetMod<MegamodMod>()) as MessagePlacementRuntime;
        EndToEndAssert.True(placement?.Available == true, "Preview acceptance requires admitted real manual placement.");
        reset = new(() => pointer, MessagePlacementSettings.PositionOffset.Id, 148);
        // Bottom and top of the 260-unit owned preview section, measured without invoking its renderer.
        preview = new(() => pointer, "messages.preview", 178);
        previewTop = new(() => pointer, "messages.preview", -54);
        var scroll = reset.Scroll;
        var globals = settings.Store.ExportGlobals();
        var speed = Find.TickManager.CurTimeSpeed;
        live = (List<Message>)(typeof(Messages).GetField("liveMessages", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null) ?? throw new InvalidOperationException("Native messages are unavailable."));
        EndToEndAssert.Equal(0, live.Count, "The ordinary empty-map fixture must start without live notifications.");
        archive = Find.Archive.ArchivablesListForReading.ToArray();
        context.DeferCleanup(() =>
        {
            var failures = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            foreach (var window in ownedWindows.Distinct())
                Attempt(() => { if (Find.WindowStack.Windows.Contains(window)) window.Close(); });
            Attempt(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(); });
            Attempt(() => settings.Store.LoadGlobals(globals));
            Attempt(() => reset.RestoreScroll(scroll));
            Attempt(() => Find.TickManager.CurTimeSpeed = speed);
            Attempt(AssertNoNotifications);
            if (failures.Count != 0) throw new AggregateException("Preview fixture cleanup failed.", failures);
        });
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.Enabled, true).Succeeded);
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.PositionOffset, new(15, 20)).Succeeded);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ModSettingsActionStep("open settings to inspect the inert position preview", "fumblesneeze.megamod");
        yield return new AssertionStep("retain only the opened settings window", _ =>
            ownedWindows.Add(Find.WindowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Settings did not open.")));
        yield return new NativePointerScopeBeginStep("retain pointer while inspecting preview", handle => pointer = handle);
        foreach (var step in Expose(preview)) yield return step;
        yield return new AssertionStep("the complete labeled preview is visible without notifications", _ =>
        {
            previewTop.MeasureCheckbox(out bool topVisible);
            EndToEndAssert.True(topVisible, "Preview heading must be visible together with its bottom edge.");
            AssertNoNotifications();
        });
        yield return Shot("effective current-save preview with inherited nonzero offsets");
        Rect resetButton = default;
        foreach (var step in Expose(reset, rect => resetButton = rect)) yield return step;
        yield return Shot("before resetting both global preview offsets");
        yield return reset.Click("reset both offsets using the native global Reset button", resetButton.center);
        yield return new AssertionStep("native reset changes the inherited pair without posting", _ =>
        {
            EndToEndAssert.Equal(default(MessageOffsets), settings.Store.GetEffective(MessagePlacementSettings.PositionOffset));
            EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.PositionOffset));
            AssertNoNotifications();
        });
        foreach (var step in Expose(preview)) yield return step;
        yield return Shot("preview at zero offsets after the native Reset action");
        yield return new NativePointerScopeEndStep("restore pointer after preview inspection", pointer);
        yield return new WindowCancelActionStep("close settings and remove the preview", typeof(Dialog_ModSettings).FullName!);
        yield return new AssertionStep("only the settings preview disappeared", _ =>
        {
            EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>());
            AssertNoNotifications();
        });
        yield return Shot("ordinary play without preview or live sample notifications");
        yield return new NativeDeveloperLogActionStep("open native developer console after preview actions", true);
        yield return new AssertionStep("retain only the scenario-opened native console", _ =>
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Developer console did not open."));
        yield return Shot("native developer console after inert preview actions");
        yield return new NativeDeveloperLogActionStep("restore closed native developer console", false);
    }

    private IEnumerable<EndToEndStep> Expose(NativeDiagnosticSettingsView view, Action<Rect>? measured = null)
    {
        Rect target = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return new NativeWindowCaptureStep("capture current preview settings geometry", windows =>
            {
                var actual = Find.WindowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Settings closed unexpectedly.");
                var captured = windows.Single(window => window.RuntimeType == typeof(Dialog_ModSettings).FullName);
                view.Bind(captured, actual);
                previewTop.Bind(captured, actual);
            });
            yield return new CheckpointStep("measure the selected preview/settings surface", _ =>
            {
                target = view.MeasureCheckbox(out visible);
                return view.Evidence(target, visible);
            });
            if (visible) break;
            Vector2 point = default;
            yield return new AssertionStep("plan bounded native scroll", _ =>
            {
                EndToEndAssert.True(attempt < 5, "Native track clicks did not expose the preview surface.");
                point = view.PlanTrackClick(target);
            });
            yield return view.Click("scroll through the native settings track", point);
        }
        measured?.Invoke(target);
    }

    private void AssertNoNotifications()
    {
        EndToEndAssert.Equal(0, live.Count, "Preview must not post live messages.");
        EndToEndAssert.True(archive.SequenceEqual(Find.Archive.ArchivablesListForReading), "Preview must not add or remove archive entries.");
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
}
