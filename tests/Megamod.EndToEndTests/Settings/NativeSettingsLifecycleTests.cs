using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace Megamod.EndToEndTests.Settings;

// This proves native page opening/closing and persistence of the existing configuration only.
// Editing controls and menu-only restrictions need the pending minimized widget-input capability.
[RimWorldEndToEndTest("megamod.settings.native-page-lifecycle", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = 2400, MaxGameTicks = 3000, MaxWallClockSeconds = 120)]
public sealed class NativeSettingsLifecycleTests : IRimWorldEndToEndTest
{
    private readonly string saveName = "Megamod-Settings-" + Guid.NewGuid().ToString("N");
    private SettingsRuntime runtime = null!;
    private Game originalGame = null!;
    private GlobalSettingsSnapshot globalsBefore = null!;
    private SaveSettingsSnapshot saveBefore = null!;
    private SaveSettingsBinding originalBinding = null!;
    private Dialog_ModSettings? ownedWindow;

    public void Arrange(IEndToEndContext context)
    {
        runtime = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings runtime is unavailable.");
        originalGame = Current.Game ?? throw new InvalidOperationException("A loaded disposable game is required.");
        EndToEndAssert.True(runtime.SaveLifecycleAvailable, "Save lifecycle must be admitted for this scenario.");
        originalBinding = runtime.CurrentBinding ?? throw new InvalidOperationException("The loaded game has no active settings binding.");
        globalsBefore = runtime.Store.ExportGlobals();
        saveBefore = runtime.Store.ExportSave(originalBinding);
        string path = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(path), "The unique disposable save path must not exist.");
        TimeSpeed speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.WindowStack.WindowOfType<Dialog_ModSettings>(), ownedWindow))
                ownedWindow?.Close();
            if (File.Exists(path)) File.Delete(path);
            if (Find.TickManager != null) Find.TickManager.CurTimeSpeed = speed;
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ScreenshotStep("before opening the native megamod settings page", Array.Empty<string>(), 0);
        yield return new ModSettingsActionStep("open the selected mod's native settings dialog", "fumblesneeze.megamod");
        yield return new AssertionStep("native owned settings page is open without mutating preferences", _ =>
        {
            ownedWindow = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
            EndToEndAssert.NotNull(ownedWindow, "Expected the native mod settings window.");
            AssertPreferencesUnchanged();
        });
        yield return new ScreenshotStep("global and current-save controls in the native settings page", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close settings through native cancel and WriteSettings lifecycle", typeof(Dialog_ModSettings).FullName!);
        yield return new AssertionStep("closing native settings preserves the current scope", _ =>
        {
            EndToEndAssert.True(Find.WindowStack.WindowOfType<Dialog_ModSettings>() == null, "Settings window must close.");
            AssertPreferencesUnchanged();
        });
        yield return new SaveLoadActionStep("save and reload the colony through the native game command", saveName);
        yield return new WaitUntilStep("the reloaded game publishes a fresh settings binding", _ =>
                !ReferenceEquals(Current.Game, originalGame) && runtime.CurrentBinding != null &&
                !ReferenceEquals(runtime.CurrentBinding, originalBinding),
            new EndToEndDeadline(600, 600, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep("native reload retains saved inheritance and values under a fresh binding", _ =>
        {
            AssertPreferencesUnchanged();
            EndToEndAssert.Equal(saveBefore.SaveIdentity, runtime.CurrentBinding!.SaveIdentity,
                "Save identity must survive native serialization.");
            EndToEndAssert.True(runtime.CurrentBinding.Generation != originalBinding.Generation,
                "Runtime activation generation must change across reload.");
        });
        yield return new ModSettingsActionStep("reopen the native settings page after loading", "fumblesneeze.megamod");
        yield return new AssertionStep("the reloaded native page is available", _ =>
        {
            ownedWindow = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
            EndToEndAssert.NotNull(ownedWindow);
        });
        yield return new ScreenshotStep("same saved preferences after native save and load", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close the reloaded native page", typeof(Dialog_ModSettings).FullName!);
        yield return new ScreenshotStep("ordinary play restored after settings roundtrip", Array.Empty<string>(), 0);
    }

    private void AssertPreferencesUnchanged()
    {
        EndToEndAssert.True(globalsBefore.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(runtime.Store.ExportGlobals().Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            "Browsing must not change global settings.");
        var current = runtime.Store.ExportSave(runtime.CurrentBinding!);
        EndToEndAssert.True(saveBefore.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(current.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            "Native browsing and reload must preserve save inheritance and values.");
    }
}
