using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Megamod.Features.ContentSelection.Core;
using Megamod.Features.ContentSelection.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace Megamod.EndToEndTests.ContentSelection;

// Fresh disposable default profile; this must be the first opening of the settings page.
// Coordinates follow the inspected native dialog and owned renderer, not OS desktop input.
[RimWorldEndToEndTest("megamod.content.native-selector-global-choice", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = 1800, MaxGameTicks = 1800, MaxWallClockSeconds = 90)]
public sealed class NativeIncidentSettingsTests : IRimWorldEndToEndTest
{
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot save = null!;
    private string firstId = null!;
    private EndToEndNativeWindow window = null!;
    private string pointerScopeHandle = null!;
    private Dialog_ModSettings? ownedWindow;
    private ContentSelectionWindow? ownedBrowser;
    private FloatMenu? ownedMenu;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        binding = settings.CurrentBinding ?? throw new InvalidOperationException("A disposable loaded save is required.");
        globals = settings.Store.ExportGlobals();
        save = settings.Store.ExportSave(binding);
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>(), "The initial page must be closed.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<ContentSelectionWindow>());
        EndToEndAssert.False(Find.WindowStack.IsOpen<FloatMenu>(), "No pre-existing native menu may be owned by this scenario.");
        EndToEndAssert.True(Prefs.DevMode, "Native console inspection requires this isolated run's developer mode.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>(), "The native developer log must initially be closed.");
        var catalog = NativeContentCatalog.Capture().Catalog;
        var incidents = catalog.Entries.Where(entry => entry.Identity is IncidentDef).ToArray();
        EndToEndAssert.True(incidents.Length > 0);
        foreach (var entry in incidents)
        {
            string id = "content.allowed/" + entry.Key.ToStorageToken();
            EndToEndAssert.True(catalog.Resolve(entry.Key).Status == ContentResolutionStatus.Present);
            EndToEndAssert.True(!globals.Values.TryGetValue(id, out string? value) || value == "true",
                "All incident defaults must initially allow content; prepare a fresh isolated profile.");
            EndToEndAssert.True(!save.Values.TryGetValue(id, out var saved) || saved.UseGlobal,
                "The scenario requires inherited incident choices.");
        }
        var first = incidents.OrderBy(entry => ((IncidentDef)entry.Identity).LabelCap.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => ((IncidentDef)entry.Identity).modContentPack?.Name ?? entry.Key.SourcePackageId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Key.SourcePackageId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key.FullTypeName, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key.DefName, StringComparer.Ordinal).First();
        firstId = "content.allowed/" + first.Key.ToStorageToken();
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.WindowStack.WindowOfType<EditWindow_Log>(), ownedLog)) ownedLog?.Close();
            if (ReferenceEquals(Find.WindowStack.WindowOfType<FloatMenu>(), ownedMenu)) ownedMenu?.Close();
            if (ReferenceEquals(Find.WindowStack.WindowOfType<ContentSelectionWindow>(), ownedBrowser)) ownedBrowser?.Close();
            if (ReferenceEquals(Find.WindowStack.WindowOfType<Dialog_ModSettings>(), ownedWindow)) ownedWindow?.Close();
            Find.TickManager.CurTimeSpeed = speed;
            // Successful execution restores the choice through the same native checkbox.
            // An interrupted run's configuration belongs only to its disposable profile.
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ModSettingsActionStep("open the ordinary content settings page", "fumblesneeze.megamod");
        yield return new NativeWindowCaptureStep("capture the ordinary settings entry", windows =>
        {
            window = windows.Single(item => item.RuntimeType == typeof(Dialog_ModSettings).FullName);
            ownedWindow = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
            EndToEndAssert.NotNull(ownedWindow);
        });
        yield return new ScreenshotStep("general settings show one content browser entry", Array.Empty<string>(), 0);
        yield return new NativePointerScopeBeginStep("retain the native pointer while browsing content choices",
            handle => pointerScopeHandle = handle);
        yield return Click("open Content selection through its native button", ViewWidth / 2, 32 + 15);
        yield return CaptureSettings("capture the dedicated content browser");
        yield return new ScreenshotStep("incident catalogue inside the dedicated browser", Array.Empty<string>(), 0);
        yield return Click("open the native incident type selector", ViewWidth / 4, 32 + 48 + 14);
        yield return new AssertionStep("type selector opens a native FloatMenu without editing choices", _ =>
        {
            ownedMenu = Find.WindowStack.WindowOfType<FloatMenu>();
            EndToEndAssert.NotNull(ownedMenu);
            AssertUnchanged();
        });
        yield return new ScreenshotStep("native incident type choices", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("cancel the type menu through native input", typeof(FloatMenu).FullName!);
        yield return CaptureSettings("recapture the same settings window after type cancellation");
        yield return Click("open the native incident source selector", ViewWidth * 3 / 4, 32 + 48 + 14);
        yield return new AssertionStep("source selector opens without editing choices", _ =>
        {
            ownedMenu = Find.WindowStack.WindowOfType<FloatMenu>();
            EndToEndAssert.NotNull(ownedMenu);
            AssertUnchanged();
        });
        yield return new ScreenshotStep("native source names and canonical package identities", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("cancel the source menu through native input", typeof(FloatMenu).FullName!);
        yield return CaptureSettings("recapture settings before the real global checkbox action");
        double column = (ViewWidth - 16) / 3;
        double globalX = 2 * (column + 8) + column / 2;
        const double firstGlobalY = 32 + 168 + 54 + 14;
        yield return Click("turn the first incident global default off through its native checkbox", globalX, firstGlobalY);
        yield return new AssertionStep("native checkbox changed only the requested global choice", _ => AssertChoice("false"));
        yield return new ScreenshotStep("requested exclusion with the prior applied status and pending label", Array.Empty<string>(), 0);
        // Requested-excluded-first ordering keeps this same first row in place.
        yield return Click("restore the first global choice through the same native checkbox", globalX, firstGlobalY);
        yield return new AssertionStep("native checkbox restores the allowed choice without save overrides", _ => AssertChoice("true"));
        yield return new ScreenshotStep("restored incident choice and unchanged applied status", Array.Empty<string>(), 0);
        yield return new NativePointerScopeEndStep("restore the pointer after native content editing", pointerScopeHandle);
        yield return new WindowCancelActionStep("close the dedicated content browser", typeof(ContentSelectionWindow).FullName!);
        yield return new WindowCancelActionStep("close settings through native cancel", typeof(Dialog_ModSettings).FullName!);
        yield return new NativeDeveloperLogActionStep("open the real developer log after content editing", true);
        yield return new AssertionStep("native developer log is open", _ =>
        {
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
            EndToEndAssert.NotNull(ownedLog);
        });
        yield return new ScreenshotStep("native developer console after content editing", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("restore the closed native developer log", false);
    }

    private double ViewWidth => window.Width - 2 * 18 - 20;
    private NativeWindowClickActionStep Click(string name, double x, double y) => new(name, window.Handle,
        (window.X + 18 + x) * window.UiScale, (window.Y + 18 + 40 + y) * window.UiScale, null, pointerScopeHandle);

    private NativeWindowCaptureStep CaptureSettings(string name) => new(name, windows =>
    {
        window = windows.Single(item => item.RuntimeType == typeof(ContentSelectionWindow).FullName);
        ownedBrowser = Find.WindowStack.WindowOfType<ContentSelectionWindow>();
        EndToEndAssert.NotNull(ownedBrowser);
        EndToEndAssert.True(window.Width >= 700 && window.Height >= 500, "Viewport must expose the first row without scrolling.");
    });

    private void AssertUnchanged()
    {
        EndToEndAssert.True(globals.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)));
        AssertSaveUnchanged();
    }

    private void AssertChoice(string expected)
    {
        var current = settings.Store.ExportGlobals();
        EndToEndAssert.True(current.Values.TryGetValue(firstId, out string? actual) && actual == expected,
            "Observe the value written by the native checkbox, never set it in the scenario.");
        EndToEndAssert.True(globals.Values.Where(pair => pair.Key != firstId).OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(current.Values.Where(pair => pair.Key != firstId).OrderBy(pair => pair.Key, StringComparer.Ordinal)));
        AssertSaveUnchanged();
    }

    private void AssertSaveUnchanged()
    {
        EndToEndAssert.True(ReferenceEquals(settings.CurrentBinding, binding));
        EndToEndAssert.True(save.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)));
    }
}
