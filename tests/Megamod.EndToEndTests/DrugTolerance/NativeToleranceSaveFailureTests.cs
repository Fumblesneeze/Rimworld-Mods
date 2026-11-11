using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HarmonyLib;
using Megamod.Features.Diagnostics;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// Run this ID in its own process: the intentionally induced product failure requires restart.
// Only a minimal successful native save/load baseline precedes the two native Overwrites.
[RimWorldEndToEndTest("megamod.drug-tolerance.save-failure-preserves-file", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 1200)]
[RimWorldEndToEndExpectedDiagnostic("s138-save-row-fault", "ERROR",
    "Exception while saving RimWorld.DrugPolicyEntry: System.InvalidOperationException: Expected S138 native-save fault fixture", 1)]
[RimWorldEndToEndExpectedDiagnostic("s138-save-safe-writer-abort", "WARN",
    "An exception was thrown during saving to \"{savedata}\\Saves\\Megamod-Tolerance-SaveFailure.rws.new\": System.InvalidOperationException: Owned drug-tolerance state could not be preserved. Saving/loading was aborted; restart before retrying.", 2,
    InnerCauses = new[] { " ---> System.InvalidOperationException: Expected S138 native-save fault fixture" })]
[RimWorldEndToEndExpectedDiagnostic("s138-save-game-abort", "ERROR",
    "Exception while saving game: System.InvalidOperationException: Owned drug-tolerance state could not be preserved. Saving/loading was aborted; restart before retrying.", 2,
    InnerCauses = new[] { " ---> System.InvalidOperationException: Expected S138 native-save fault fixture" })]
public sealed class NativeToleranceSaveFailureTests : IRimWorldEndToEndTest
{
    private const string Owner = "tests.megamod.tolerance-owned-scribe-failure";
    private const string Fault = "Expected S138 native-save fault fixture";
    private const string SaveName = "Megamod-Tolerance-SaveFailure";
    private const string WarningPrefix = "[fumblesneeze.megamod] Patch DT-POLICY-SCRIBE unavailable;";
    private const string ExpectedWarning = WarningPrefix + " target=RimWorld.DrugPolicyEntry.ExposeData; targetSpec=DT-POLICY-PERSISTENCE; reason=" + Fault +
        "; unavailable=owned tolerance persistence; fallback=tolerance editor/enforcement unavailable; save/load aborted until restart.";
    private static volatile bool armed;
    private static int injected, observedOwnedWrites;
    private static string lastOwnedPath = "";
    private readonly string policyMarker = "ToleranceSaveFault-" + Guid.NewGuid().ToString("N");
    private readonly HashSet<Window> ownedWindows = new();
    private IEndToEndContext context = null!;
    private Game initial = null!, game = null!;
    private ProtectedDiagnosticsRuntime protectedDiagnostics = null!;
    private readonly ConcurrentQueue<string> protectedReports = new();
    private int reportCount;
    private int mapId, policyId;
    private bool saveRequested;
    private string savePath = null!, protectedPath = "", protectedText = "", protectedHash = "";
    private EndToEndNativeWindow window = null!;
    private string pointer = null!, originalHash = null!;
    private Dialog_SaveFileList_Save saveDialog = null!;
    private Dialog_MessageBox failureNotice = null!, nativeError = null!;
    private int initialDiagnostics;
    private int initialAbortErrors;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context;
        initial = game = Current.Game;
        EndToEndAssert.True(initial.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && !initial.Info.permadeathMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Prefs.DevMode && !Find.WindowStack.IsOpen<LudeonTK.EditWindow_Log>());
        mapId = initial.CurrentMap.uniqueID;
        var speed = initial.tickManager.CurTimeSpeed;
        initial.tickManager.CurTimeSpeed = TimeSpeed.Paused;
        context.DeferCleanup(() => OwnedGame().tickManager.CurTimeSpeed = speed);
        var policy = initial.drugPolicyDatabase.MakeNewDrugPolicy();
        policyId = policy.id;
        policy.label = policyMarker;
        var beer = Enumerable.Range(0, policy.Count).Select(index => policy[index]).Single(entry => entry.drug.defName == "Beer");
        NativeDrugToleranceState.WriteEncoded(beer, "1:limit:0");
        context.DeferCleanup(() => OwnedGame().drugPolicyDatabase.AllPolicies.RemoveAll(item => item.id == policyId && item.label == policyMarker));
        savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        EndToEndAssert.False(File.Exists(savePath), "Only an absent disposable save in this isolated run may be created.");
        context.DeferCleanup(() => { if (saveRequested && File.Exists(savePath)) File.Delete(savePath); });
        protectedDiagnostics = typeof(MegamodMod).GetField("diagnostics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(LoadedModManager.GetMod<MegamodMod>()) as ProtectedDiagnosticsRuntime
            ?? throw new InvalidOperationException("The active protected diagnostics owner is absent.");
        ObserveProtectedReport();
        // Install before the real successful baseline and prove this exact native write boundary
        // is reached. A closed generic Look<string> detour added after warm-up was not observed
        // in the failed native run; metadata registration alone did not prove interception.
        var target = typeof(ScribeSaver).GetMethod(nameof(ScribeSaver.WriteElement),
            BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(string) }, null)!;
        EndToEndAssert.True(target != null && target.ReturnType == typeof(void) && !target.IsGenericMethod);
        var owner = new Harmony(Owner);
        context.DeferCleanup(() => { armed = false; owner.Unpatch(target, HarmonyPatchType.All, Owner); });
        armed = false;
        injected = observedOwnedWrites = 0;
        lastOwnedPath = "";
        owner.Patch(target, prefix: new HarmonyMethod(typeof(NativeToleranceSaveFailureTests), nameof(FailOwnedElement)));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        saveRequested = true;
        yield return new SaveLoadActionStep("create only the native successful save/load needed by the failure fixture", SaveName);
        yield return new CheckpointStep("native successful baseline reached the owned string write boundary", _ => new Dictionary<string, string>
        {
            ["faultBoundary"] = "Verse.ScribeSaver.WriteElement(string,string)",
            ["observedOwnedWrites"] = Volatile.Read(ref observedOwnedWrites).ToString(),
            ["lastOwnedPath"] = lastOwnedPath,
            ["injectedErrors"] = Volatile.Read(ref injected).ToString()
        });
        yield return new AssertionStep("arrange an owned-field serialization fault after a real successful save", _ =>
        {
            game = OwnedGame();
            EndToEndAssert.False(ReferenceEquals(game, initial), "The native baseline must have loaded a replacement Game.");
            var policy = game.drugPolicyDatabase.AllPolicies.Single(item => item.id == policyId && item.label == policyMarker);
            var beer = Enumerable.Range(0, policy.Count).Select(index => policy[index]).Single(entry => entry.drug.defName == "Beer");
            EndToEndAssert.Equal("1:limit:0", NativeDrugToleranceState.ReadEncoded(beer));
            EndToEndAssert.True(Volatile.Read(ref observedOwnedWrites) > 0,
                "The native baseline must execute the installed non-generic owned-field interceptor before it can be armed.");
            EndToEndAssert.Equal(0, Volatile.Read(ref injected));
            EndToEndAssert.True(File.Exists(savePath));
            originalHash = Hash(savePath);
            initialDiagnostics = Diagnostics();
            initialAbortErrors = AbortErrors();
            EndToEndAssert.Equal(0, initialDiagnostics);
            EndToEndAssert.Equal(0, initialAbortErrors);
            armed = true;
        });
        foreach (var step in Overwrite("first failed overwrite")) yield return step;
        yield return new WaitUntilStep("native overwrite serialization finishes before checking the injected fault", _ =>
            !LongEventHandler.AnyEventNowOrWaiting,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(90)));
        yield return new CheckpointStep("native overwrite fault and visible window diagnostics", _ => new Dictionary<string, string>
        {
            ["injectedOwnedFieldErrors"] = Volatile.Read(ref injected).ToString(),
            ["observedOwnedWrites"] = Volatile.Read(ref observedOwnedWrites).ToString(),
            ["lastOwnedPath"] = lastOwnedPath,
            ["ownedDiagnostics"] = Diagnostics().ToString(), ["nativeAbortErrors"] = AbortErrors().ToString(),
            ["protectedDiagnosticFile"] = protectedPath, ["protectedDiagnosticSha256"] = protectedHash,
            ["protectedDiagnosticText"] = protectedText,
            ["fileStillExists"] = File.Exists(savePath).ToString(),
            ["fileHash"] = File.Exists(savePath) ? Hash(savePath) : "absent",
            ["messageBoxes"] = string.Join(" | ", Find.WindowStack.Windows.OfType<Dialog_MessageBox>()
                .Take(8).Select(box => box.ID + ":" + box.title + ":" + new string(box.text.ToString().Take(160).ToArray())))
        });
        yield return new AssertionStep("native overwrite actually reached the armed owned-field fault", _ =>
            EndToEndAssert.True(Volatile.Read(ref injected) > 0,
                "The native Overwrite finished without executing the armed owned-field interceptor; inspect the preceding checkpoint instead of waiting for an impossible notice."));
        yield return new WaitUntilStep("owned restart notice appears after native saving finishes", _ =>
            !LongEventHandler.AnyEventNowOrWaiting && Notice() != null,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(90)));
        yield return new NativeDeveloperLogActionStep("close the automatic console before observing the owned restart notice", false);
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
            EndToEndAssert.Equal(initialAbortErrors + 1, AbortErrors());
        });
        yield return new NativePointerScopeBeginStep("own pointer for the exact restart notice", value => pointer = value);
        yield return Click("acknowledge only the bound owned restart notice", AcknowledgePoint(failureNotice));
        yield return new NativePointerScopeEndStep("restore pointer after the owned notice", pointer);
        foreach (var step in DismissNativeSaveError("first failed overwrite")) yield return step;
        foreach (var step in Overwrite("retry after failure")) yield return step;
        yield return new WaitUntilStep("native retry finishes without another owned dialog", _ =>
            !LongEventHandler.AnyEventNowOrWaiting,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(90)));
        yield return new AssertionStep("retry cannot silently omit choices or repeat the owned notice", _ =>
        {
            AssertFileUnchanged();
            EndToEndAssert.True(Notice() == null);
            EndToEndAssert.Equal(initialDiagnostics + 1, Diagnostics());
            EndToEndAssert.Equal(initialAbortErrors + 2, AbortErrors(), "Exactly the two native save attempts must reach the finalization guard.");
        });
        yield return new CheckpointStep("failed native save retained the exact successful file", _ => new Dictionary<string, string>
        {
            ["saveFile"] = Path.GetFileName(savePath), ["beforeSha256"] = originalHash,
            ["afterSha256"] = Hash(savePath), ["injectedOwnedFieldErrors"] = injected.ToString(),
            ["protectedWarningCount"] = Diagnostics().ToString(), ["protectedDiagnosticFile"] = protectedPath,
            ["protectedDiagnosticSha256"] = protectedHash, ["protectedDiagnosticText"] = protectedText,
            ["protectedReport"] = string.Join(" | ", protectedReports),
            ["deliveryVerification"] = "Root must verify the matching native or runtime-owned fallback file after shutdown; callback observation alone is not durable-delivery acceptance.",
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
        int index = files.FindIndex(file => string.Equals(file.FileInfo.FullName, savePath, StringComparison.OrdinalIgnoreCase));
        EndToEndAssert.True(index >= 0);
        float y = 18 + 34 + index * 40 - scroll.y + 20;
        float x = saveDialog.windowRect.width - 18 - 16 - 36 - 50;
        EndToEndAssert.True(y >= 18 + 34 && y < saveDialog.windowRect.height - 130,
            "The exact save row must be visible; no guessed scrolling or alternate file is allowed.");
        return new Vector2(x, y);
    }

    private IEnumerable<EndToEndStep> DismissNativeSaveError(string phase)
    {
        yield return new NativeDeveloperLogActionStep("close the automatic console before the native save error " + phase, false);
        yield return CaptureMessage("capture exact native SafeSaver error for " + phase,
            () => Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Single(box =>
                box.text.ToString().Contains(Path.GetFileName(savePath)) &&
                box.text.ToString().Contains("Owned drug-tolerance state could not be preserved.")),
            box => nativeError = box);
        yield return new ScreenshotStep("visible native SafeSaver error for " + phase, Array.Empty<string>(), 0);
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

    public static void FailOwnedElement(ScribeSaver __instance, string elementName)
    {
        if (elementName != "megamodTolerance" || Scribe.mode != LoadSaveMode.Saving) return;
        Interlocked.Increment(ref observedOwnedWrites);
        lastOwnedPath = new string((__instance.CurPath ?? "").Take(256).ToArray());
        if (!armed) return;
        Interlocked.Increment(ref injected);
        throw new InvalidOperationException(Fault);
    }

    private void AssertFileUnchanged()
    {
        EndToEndAssert.True(ReferenceEquals(game, Current.Game));
        EndToEndAssert.Equal(originalHash, Hash(savePath));
        EndToEndAssert.False(File.Exists(savePath + ".new"));
    }
    private int Diagnostics()
    {
        // Protected native delivery bypasses Verse.Log.Messages. Observe only the exact report
        // callback already invoked by the product, and retain its optional owned-file evidence.
        EndToEndAssert.Equal(0L, protectedDiagnostics.Sink.UndeliveredCount);
        EndToEndAssert.Equal(0, protectedDiagnostics.Sink.DeliveryFailures.Count);
        EndToEndAssert.Equal(0L, protectedDiagnostics.Sink.ReentrantCount);
        int count = Volatile.Read(ref reportCount);
        var reports = protectedReports.ToArray();
        EndToEndAssert.True(count <= 64 && reports.Length == count && reports.All(message => message == ExpectedWarning));
        string? path = protectedDiagnostics.FallbackFilePath;
        protectedPath = path == null ? "" : Path.GetFullPath(path);
        protectedText = protectedHash = "";
        if (path == null) return count;
        string fileName = Path.GetFileName(protectedPath);
        const string prefix = "Megamod-startup-";
        EndToEndAssert.True(string.Equals(Path.GetDirectoryName(protectedPath),
            Path.Combine(Path.GetFullPath(GenFilePaths.SaveDataFolderPath), "Logs"), StringComparison.OrdinalIgnoreCase) &&
            fileName.StartsWith(prefix, StringComparison.Ordinal) && fileName.EndsWith(".log", StringComparison.Ordinal) &&
            Guid.TryParseExact(fileName.Substring(prefix.Length, fileName.Length - prefix.Length - 4), "N", out _),
            "Read only this runtime's unique protected file inside the isolated save-data directory.");
        if (!File.Exists(protectedPath)) return count;
        using var stream = new FileStream(protectedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        EndToEndAssert.True(stream.Length <= 1024 * 1024, "Protected diagnostic evidence exceeds its read bound.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        EndToEndAssert.Equal(-1, stream.ReadByte(), "Do not accept a torn protected diagnostic snapshot.");
        protectedText = new UTF8Encoding(false, true).GetString(bytes);
        protectedHash = Convert.ToHexString(SHA256.HashData(bytes));
        var lines = protectedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(WarningPrefix, StringComparison.Ordinal)).ToArray();
        EndToEndAssert.True(lines.All(line => line == ExpectedWarning) && lines.Length <= count);
        return count;
    }

    private void ObserveProtectedReport()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var mod = LoadedModManager.GetMod<MegamodMod>();
        var ownerField = typeof(MegamodMod).GetField("drugTolerancePersistence", flags)
            ?? throw new InvalidOperationException("The exact persistence owner field is absent.");
        var reportField = typeof(DrugTolerancePersistencePatch).GetField("report", flags)
            ?? throw new InvalidOperationException("The exact owned report callback is absent.");
        EndToEndAssert.True(ownerField.FieldType == typeof(DrugTolerancePersistencePatch) && ownerField.IsInitOnly &&
            reportField.FieldType == typeof(Action<string>) && reportField.IsInitOnly);
        var owner = ownerField.GetValue(mod) as DrugTolerancePersistencePatch
            ?? throw new InvalidOperationException("The current persistence instance is absent.");
        var original = reportField.GetValue(owner) as Action<string>
            ?? throw new InvalidOperationException("The current protected reporting action is absent.");
        Action<string> observed = warning =>
        {
            original(warning);
            if (Interlocked.Increment(ref reportCount) <= 64)
                protectedReports.Enqueue(new string(warning.Take(4096).ToArray()));
        };
        reportField.SetValue(owner, observed);
        context.DeferCleanup(() =>
        {
            EndToEndAssert.True(ReferenceEquals(LoadedModManager.GetMod<MegamodMod>(), mod) &&
                ReferenceEquals(ownerField.GetValue(mod), owner) && ReferenceEquals(reportField.GetValue(owner), observed),
                "Refuse restoration if the exact protected-report owner or delegate changed.");
            reportField.SetValue(owner, original);
            EndToEndAssert.True(ReferenceEquals(reportField.GetValue(owner), original));
        });
        EndToEndAssert.True(ReferenceEquals(reportField.GetValue(owner), observed));
    }

    private Game OwnedGame()
    {
        var current = Current.Game;
        EndToEndAssert.True(current != null && current.PlayerHasControl && current.CurrentMap?.uniqueID == mapId,
            "Refuse missing, partial or unrelated game cleanup.");
        if (current == null) throw new InvalidOperationException("The owned current Game is absent.");
        if (ReferenceEquals(current, game)) return current;
        EndToEndAssert.True(ReferenceEquals(game, initial) && saveRequested &&
            current.drugPolicyDatabase.AllPolicies.Count(policy => policy.id == policyId && policy.label == policyMarker) == 1,
            "Only the first verified saved replacement Game belongs to this fixture.");
        game = current;
        return current;
    }
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
