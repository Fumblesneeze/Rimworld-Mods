using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// Run this ID in its own process: the intentionally induced product failure requires restart.
// It shares the ordinary copy/save workflow, then clicks the actual native Overwrite button.
[RimWorldEndToEndTest("megamod.drug-tolerance.save-failure-preserves-file", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 1800)]
public sealed class NativeToleranceSaveFailureTests : IRimWorldEndToEndTest
{
    private const string Owner = "tests.megamod.tolerance-owned-scribe-failure";
    private const string Fault = "Expected S138 native-save fault fixture";
    private static bool armed;
    private static int injected;
    private readonly NativeTolerancePersistenceTests baseline = new();
    private readonly HashSet<Window> ownedWindows = new();
    private IEndToEndContext context = null!;
    private Game game = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!, originalHash = null!;
    private Dialog_SaveFileList_Save saveDialog = null!;
    private Dialog_MessageBox failureNotice = null!, nativeError = null!;
    private int initialDiagnostics;
    private int initialAbortErrors;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context;
        baseline.Arrange(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var workflow = baseline.Execute(context);
        while (workflow.MoveNext()) yield return workflow.Current;
        yield return new AssertionStep("arrange an owned-field serialization fault after a real successful save", _ =>
        {
            game = Current.Game;
            EndToEndAssert.True(File.Exists(baseline.SavePath));
            originalHash = Hash(baseline.SavePath);
            initialDiagnostics = Diagnostics();
            initialAbortErrors = AbortErrors();
            var target = typeof(Scribe_Values).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == "Look" && method.IsGenericMethodDefinition && method.GetParameters().Length == 4)
                .MakeGenericMethod(typeof(string));
            var owner = new Harmony(Owner);
            context.DeferCleanup(() => { armed = false; owner.Unpatch(target, HarmonyPatchType.All, Owner); });
            injected = 0;
            armed = true;
            owner.Patch(target, prefix: new HarmonyMethod(typeof(NativeToleranceSaveFailureTests), nameof(FailOwnedString)));
        });
        foreach (var step in Overwrite("first failed overwrite")) yield return step;
        yield return new WaitUntilStep("owned restart notice appears after native saving finishes", _ =>
            !LongEventHandler.AnyEventNowOrWaiting && Notice() != null,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(90)));
        yield return CaptureMessage("capture the exact owned serialization failure notice", () => Notice()!, box =>
        {
            failureNotice = box;
            EndToEndAssert.Equal("Megamod.DrugTolerance.PersistenceFailureTitle".Translate().ToString(), box.title);
            EndToEndAssert.Equal("Megamod.DrugTolerance.PersistenceFailureBody".Translate().ToString(), box.text.ToString());
        });
        yield return new ScreenshotStep("native overwrite failed with explicit restart guidance", Array.Empty<string>(), 0);
        yield return new AssertionStep("native save machinery retained the previous file after the swallowed row error", _ =>
        {
            EndToEndAssert.True(injected > 0);
            AssertFileUnchanged();
            EndToEndAssert.Equal(initialDiagnostics + 1, Diagnostics());
            EndToEndAssert.True(AbortErrors() > initialAbortErrors);
        });
        yield return new NativePointerScopeBeginStep("own pointer for the exact restart notice", value => pointer = value);
        yield return Click("acknowledge only the bound owned restart notice", AcknowledgePoint(failureNotice));
        yield return new NativePointerScopeEndStep("restore pointer after the owned notice", pointer);
        foreach (var step in DismissNativeSaveError("first failed overwrite")) yield return step;
        yield return new NativeDeveloperLogActionStep("close any automatic native error console before retrying", false);
        foreach (var step in Overwrite("retry after failure")) yield return step;
        yield return new WaitUntilStep("native retry finishes without another owned dialog", _ =>
            !LongEventHandler.AnyEventNowOrWaiting,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(90)));
        yield return new AssertionStep("retry cannot silently omit choices or repeat the owned notice", _ =>
        {
            AssertFileUnchanged();
            EndToEndAssert.True(Notice() == null);
            EndToEndAssert.Equal(initialDiagnostics + 1, Diagnostics());
            EndToEndAssert.True(AbortErrors() >= initialAbortErrors + 2, "Both native save attempts must reach the finalization guard.");
        });
        yield return new CheckpointStep("failed native save retained the exact successful file", _ => new Dictionary<string, string>
        {
            ["saveFile"] = Path.GetFileName(baseline.SavePath), ["beforeSha256"] = originalHash,
            ["afterSha256"] = Hash(baseline.SavePath), ["injectedOwnedFieldErrors"] = injected.ToString(),
            ["expectedLogErrors"] = "Scribe_Deep row failure and native SaveGame finalization-abort errors are intentional fault evidence."
        });
        foreach (var step in DismissNativeSaveError("retry after failure")) yield return step;
        yield return new EscapeMenuActionStep("close native menu after failed save attempts", false);
        yield return new NativeDeveloperLogActionStep("open native console for save-failure evidence", true);
        yield return Capture<LudeonTK.EditWindow_Log>("capture owned failure console", _ => { });
        yield return new ScreenshotStep("native console reports the failed save without replacing the file", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("close native failure console", false);
    }

    private IEnumerable<EndToEndStep> Overwrite(string phase)
    {
        yield return new EscapeMenuActionStep("open native menu for " + phase, true);
        yield return Capture<MainTabWindow_Menu>("capture Save menu control for " + phase, menu =>
        {
            EndToEndAssert.True(ReferenceEquals(game, Current.Game) && !game.Info.permadeathMode &&
                !GameDataSaveLoader.SavingIsTemporarilyDisabled && string.IsNullOrEmpty(menu.optionalTitle));
        });
        yield return new NativePointerScopeBeginStep("own pointer for " + phase, value => pointer = value);
        // Inspected native OptionListingUtility: Save is first, content margin 18, minimum row 45.
        yield return Click("open native save dialog for " + phase, new Vector2(18 + 85, 18 + 22));
        yield return Capture<Dialog_SaveFileList_Save>("capture the exact native save-file row", value => saveDialog = value);
        yield return new ScreenshotStep("native save list before " + phase, Array.Empty<string>(), 0);
        yield return Click("click native Overwrite for the disposable successful save", OverwritePoint());
        yield return new NativePointerScopeEndStep("restore pointer after " + phase, pointer);
        yield return new AssertionStep("native overwrite closed its dialog for " + phase, _ =>
            EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_SaveFileList_Save>()));
    }

    private Vector2 OverwritePoint()
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var files = (List<SaveFileInfo>)typeof(Dialog_FileList).GetField("files", fields)!.GetValue(saveDialog)!;
        var scroll = (Vector2)typeof(Dialog_FileList).GetField("scrollPosition", fields)!.GetValue(saveDialog)!;
        int index = files.FindIndex(file => string.Equals(file.FileInfo.FullName, baseline.SavePath, StringComparison.OrdinalIgnoreCase));
        EndToEndAssert.True(index >= 0);
        float y = 18 + 34 + index * 40 - scroll.y + 20;
        float x = saveDialog.windowRect.width - 18 - 16 - 36 - 50;
        EndToEndAssert.True(y >= 18 + 34 && y < saveDialog.windowRect.height - 130,
            "The exact save row must be visible; no guessed scrolling or alternate file is allowed.");
        return new Vector2(x, y);
    }

    private IEnumerable<EndToEndStep> DismissNativeSaveError(string phase)
    {
        yield return CaptureMessage("capture exact native SafeSaver error for " + phase,
            () => Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Single(box =>
                box.text.ToString().Contains(Path.GetFileName(baseline.SavePath)) &&
                box.text.ToString().Contains("Owned drug-tolerance state could not be preserved.")),
            box => nativeError = box);
        yield return new NativePointerScopeBeginStep("own pointer for native save error " + phase, value => pointer = value);
        yield return Click("acknowledge the bound native save error " + phase, AcknowledgePoint(nativeError));
        yield return new NativePointerScopeEndStep("restore pointer after native save error " + phase, pointer);
    }

    private static Vector2 AcknowledgePoint(Dialog_MessageBox box)
    {
        EndToEndAssert.True(box.buttonBText == null && box.buttonCText == null && box.interactionDelay <= 0);
        // Native one-button MessageBox places button A across the content bottom (35 high).
        return new Vector2(box.windowRect.width / 2, box.windowRect.height - 18 - 17.5f);
    }

    public static void FailOwnedString(string label)
    {
        if (!armed || label != "megamodTolerance" || Scribe.mode != LoadSaveMode.Saving) return;
        injected++;
        throw new InvalidOperationException(Fault);
    }

    private void AssertFileUnchanged()
    {
        EndToEndAssert.True(ReferenceEquals(game, Current.Game));
        EndToEndAssert.Equal(originalHash, Hash(baseline.SavePath));
        EndToEndAssert.False(File.Exists(baseline.SavePath + ".new"));
    }
    private static int Diagnostics() => Log.Messages.Where(message => message.text.StartsWith("[fumblesneeze.megamod] Patch DT-POLICY-SCRIBE unavailable;", StringComparison.Ordinal)).Sum(message => message.repeats);
    private static int AbortErrors() => Log.Messages.Where(message => message.type == LogMessageType.Error &&
        message.text.StartsWith("Exception while saving game:", StringComparison.Ordinal) &&
        message.text.Contains("Saving/loading was aborted; restart before retrying.")).Sum(message => message.repeats);
    private static Dialog_MessageBox? Notice() => Find.WindowStack.Windows.OfType<Dialog_MessageBox>()
        .SingleOrDefault(box => box.title == "Megamod.DrugTolerance.PersistenceFailureTitle".Translate().ToString());
    private static string Hash(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
    private NativeWindowClickActionStep Click(string label, Vector2 point) => new(label, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    private NativeWindowCaptureStep Capture<T>(string label, Action<T> retain) where T : Window => new(label, windows =>
    {
        var current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Missing native window: " + typeof(T));
        Own(current);
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
    private NativeWindowCaptureStep CaptureMessage(string label, Func<Dialog_MessageBox> select, Action<Dialog_MessageBox> retain) => new(label, windows =>
    {
        var current = select() ?? throw new InvalidOperationException("Missing exact native message box.");
        Own(current);
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
    private void Own(Window current)
    {
        if (ownedWindows.Add(current)) context.DeferCleanup(() =>
        {
            if (!ReferenceEquals(game, Current.Game)) throw new InvalidOperationException("Refuse unrelated game cleanup.");
            if (ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, current)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else if (Find.WindowStack.Windows.Contains(current)) current.Close(false);
        });
    }
}
