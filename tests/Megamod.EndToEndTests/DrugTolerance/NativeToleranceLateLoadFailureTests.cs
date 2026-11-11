using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// Intentionally ends in native failed-load state. Run alone in a fresh process and restart after.
[RimWorldEndToEndTest("megamod.drug-tolerance.late-load-failure", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 900)]
[RimWorldEndToEndExpectedDiagnostic("s138-load-post-init-fault", "ERROR",
    "Could not do PostLoadInit on RimWorld.DrugPolicyEntry: System.InvalidOperationException: Expected S138 owned callback PostLoadInit fault fixture", 1)]
[RimWorldEndToEndExpectedDiagnostic("s138-load-asynchronous-abort", "ERROR",
    "Exception from asynchronous event: System.InvalidOperationException: Owned drug-tolerance state could not be preserved. Saving/loading was aborted; restart before retrying.", 1,
    InnerCauses = new[] { " ---> System.InvalidOperationException: Expected S138 owned callback PostLoadInit fault fixture" })]
public sealed class NativeToleranceLateLoadFailureTests : IRimWorldEndToEndTest
{
    private volatile bool armed;
    private int injected, savingHits, loadingHits, crossReferenceHits, postLoadHits;
    private readonly string marker = "ToleranceLate-" + Guid.NewGuid().ToString("N");
    private readonly string saveName = "Megamod-Tolerance-LateLoadFailure";
    private readonly HashSet<Window> windowsOwned = new();
    private IEndToEndContext context = null!;
    private Game initial = null!;
    private Game? baseline;
    private DrugPolicy policy = null!;
    private int policyId, mapId;
    private bool saveRequested, faultLoadRequested;
    private string savePath = null!, hash = null!, pointer = null!;
    private EndToEndNativeWindow window = null!;
    private Dialog_SaveFileList_Load loadDialog = null!;
    private Dialog_MessageBox notice = null!;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context;
        initial = Current.Game;
        EndToEndAssert.True(initial.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && !initial.Info.permadeathMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Prefs.DevMode);
        mapId = initial.CurrentMap.uniqueID;
        var speed = initial.tickManager.CurTimeSpeed;
        initial.tickManager.CurTimeSpeed = TimeSpeed.Paused;
        policy = initial.drugPolicyDatabase.MakeNewDrugPolicy();
        policyId = policy.id;
        policy.label = marker;
        var beer = Enumerable.Range(0, policy.Count).Select(index => policy[index]).Single(entry => entry.drug.defName == "Beer");
        NativeDrugToleranceState.WriteEncoded(beer, "1:limit:0");
        savePath = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(savePath), "Only an absent unique disposable save may be created.");
        context.DeferCleanup(() =>
        {
            // A rejected native load disposes the previous game. Never write through stale
            // objects or mutate the failed partial replacement merely to make cleanup pass.
            var game = Current.Game;
            if (game == null || (!ReferenceEquals(game, initial) && !ReferenceEquals(game, baseline))) return;
            if (!game.PlayerHasControl) return;
            EndToEndAssert.Equal(mapId, game.CurrentMap.uniqueID);
            game.drugPolicyDatabase.AllPolicies.RemoveAll(item => item.id == policyId && item.label == marker);
            game.tickManager.CurTimeSpeed = speed;
        });
        context.DeferCleanup(() =>
        {
            // File ownership is independent of the deliberately rejected replacement Game.
            if (saveRequested && File.Exists(savePath)) File.Delete(savePath);
        });
        WrapOwnedSerializer();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        saveRequested = true;
        yield return new SaveLoadActionStep("create a real native baseline before a late-phase fault", saveName);
        yield return new CheckpointStep("native baseline traversed the wrapped owned serialization phases", _ => PhaseEvidence());
        yield return new AssertionStep("bind the successfully loaded baseline and arrange only a PostLoadInit fault", _ =>
        {
            var game = Current.Game;
            EndToEndAssert.True(game.PlayerHasControl && !ReferenceEquals(game, initial) && game.CurrentMap.uniqueID == mapId);
            var restored = game.drugPolicyDatabase.AllPolicies.Single(item => item.id == policyId && item.label == marker);
            var beer = Enumerable.Range(0, restored.Count).Select(index => restored[index]).Single(entry => entry.drug.defName == "Beer");
            EndToEndAssert.Equal("1:limit:0", NativeDrugToleranceState.ReadEncoded(beer));
            baseline = game;
            hash = FileHash(savePath);
            EndToEndAssert.True(Volatile.Read(ref savingHits) > 0 && Volatile.Read(ref loadingHits) > 0 &&
                Volatile.Read(ref postLoadHits) > 0,
                "The real successful baseline must reach this exact owned callback in Saving, LoadingVars and PostLoadInit before arming it.");
            EndToEndAssert.Equal(0, Volatile.Read(ref injected));
            armed = true;
        });
        yield return new ScreenshotStep("successfully loaded colony before the native late-fault load", Array.Empty<string>(), 0);
        yield return new EscapeMenuActionStep("open native Load menu for the saved baseline", true);
        yield return Capture("capture native Load option", () => Find.WindowStack.WindowOfType<MainTabWindow_Menu>()!, _ =>
        {
            EndToEndAssert.True(!GameDataSaveLoader.SavingIsTemporarilyDisabled && !baseline!.Info.permadeathMode &&
                !GameDataSaveLoader.CurrentGameStateIsValuable, "Use the unchanged paused baseline without an unsaved-progress prompt.");
        });
        yield return new NativePointerScopeBeginStep("own pointer for native Load actions", value => pointer = value);
        // Native menu has Save first (45 + 7), then Load, with an 18-unit content inset.
        yield return Click("open the native load-file dialog", new Vector2(18 + 85, 18 + 52 + 22));
        yield return Capture("capture the exact native saved-file row", () => Find.WindowStack.WindowOfType<Dialog_SaveFileList_Load>()!, value => loadDialog = value);
        yield return new ScreenshotStep("native saved-file row before late-phase load failure", Array.Empty<string>(), 0);
        yield return new AssertionStep("bind the native fault-load action boundary", _ => faultLoadRequested = true);
        yield return Click("load only the disposable baseline through its native Load button", LoadPoint());
        yield return new NativePointerScopeEndStep("restore pointer after native load request", pointer);
        yield return new WaitUntilStep("native load attempt finishes before checking the injected late-phase fault", _ =>
            !LongEventHandler.AnyEventNowOrWaiting,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(180)));
        yield return new CheckpointStep("native late-load injection and resulting scene diagnostics", _ =>
        {
            var evidence = PhaseEvidence();
            evidence["nativeProgramState"] = Current.ProgramState.ToString();
            evidence["currentGamePresent"] = (Current.Game != null).ToString();
            evidence["sourceSha256"] = FileHash(savePath);
            evidence["messageBoxes"] = string.Join(" | ", Find.WindowStack.Windows.OfType<Dialog_MessageBox>()
                .Take(8).Select(box => box.ID + ":" + box.title + ":" + new string(box.text.ToString().Take(160).ToArray())));
            return evidence;
        });
        yield return new AssertionStep("native Load actually reached the armed owned PostLoadInit failure", _ =>
            EndToEndAssert.True(Volatile.Read(ref injected) > 0,
                "The native Load finished without the armed owned PostLoadInit callback failure; inspect the preceding phase checkpoint."));
        yield return new WaitUntilStep("native late-load rejection reaches its restart notice", _ =>
            !LongEventHandler.AnyEventNowOrWaiting && Notice() != null,
            new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(180)));
        yield return new NativeDeveloperLogActionStep("close the automatic console before observing the late-load restart notice", false);
        yield return Capture("capture the exact restart notice after PostLoadInit failure", () => Notice()!, value => notice = value);
        yield return new ScreenshotStep("late native load failed with visible restart guidance", Array.Empty<string>(), 0);
        yield return new AssertionStep("a swallowed PostLoadInit error cannot become a successful loaded colony", _ =>
        {
            EndToEndAssert.True(faultLoadRequested && injected > 0);
            EndToEndAssert.True(Current.Game == null || !Current.Game.PlayerHasControl || Current.ProgramState != ProgramState.Playing);
            EndToEndAssert.Equal(hash, FileHash(savePath));
            EndToEndAssert.True(Log.Messages.Any(message => message.text.Contains("Saving/loading was aborted; restart before retrying.")));
            foreach (var box in Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Where(box =>
                box.text.ToString().Contains("Saving/loading was aborted; restart before retrying."))) Own(box);
        });
        yield return new CheckpointStep("native late-phase failure and preserved source file", _ => new Dictionary<string, string>
        {
            ["postLoadFaultCount"] = injected.ToString(), ["savedFile"] = Path.GetFileName(savePath),
            ["beforeSha256"] = hash, ["afterSha256"] = FileHash(savePath),
            ["nativeProgramState"] = Current.ProgramState.ToString(), ["notice"] = notice.text.ToString(),
            ["expectedErrors"] = "Injected owned callback failure after its original PostLoadInit action, native loader abort and failed-load scene recovery. This is not a native parser failure."
        });
        yield return new NativePointerScopeBeginStep("own pointer for exact late-load notice", value => pointer = value);
        yield return Click("acknowledge the bound late-load restart notice", AcknowledgePoint(notice));
        yield return new NativePointerScopeEndStep("restore pointer after late-load notice", pointer);
        yield return new WaitUntilStep("the acknowledged late-load notice leaves the native stack", _ =>
            !Find.WindowStack.Windows.Any(current => ReferenceEquals(current, notice)),
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(5)));
        yield return new NativeDeveloperLogActionStep("open the native console after visible late-load failure evidence", true);
        yield return Capture("capture the native late-load error console", () => Find.WindowStack.WindowOfType<LudeonTK.EditWindow_Log>()!, _ => { });
        yield return new ScreenshotStep("native console after rejected late-phase load", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("close the native late-load error console", false);
    }

    private void WrapOwnedSerializer()
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var mod = LoadedModManager.GetMod<MegamodMod>();
        var ownerField = typeof(MegamodMod).GetField("drugTolerancePersistence", fields)
            ?? throw new InvalidOperationException("The exact owned persistence field is absent.");
        var exposeField = typeof(DrugTolerancePersistencePatch).GetField("expose", fields)
            ?? throw new InvalidOperationException("The exact owned serialization callback field is absent.");
        EndToEndAssert.True(ownerField.FieldType == typeof(DrugTolerancePersistencePatch) && ownerField.IsInitOnly &&
            exposeField.FieldType == typeof(Action<object>) && exposeField.IsInitOnly);
        var owner = ownerField.GetValue(mod) as DrugTolerancePersistencePatch
            ?? throw new InvalidOperationException("The active owned persistence instance is absent.");
        var original = exposeField.GetValue(owner) as Action<object>
            ?? throw new InvalidOperationException("The active owned serialization callback is absent.");
        armed = false;
        injected = savingHits = loadingHits = crossReferenceHits = postLoadHits = 0;
        Action<object> wrapper = entry =>
        {
            var phase = Scribe.mode;
            original(entry);
            switch (phase)
            {
                case LoadSaveMode.Saving: Interlocked.Increment(ref savingHits); break;
                case LoadSaveMode.LoadingVars: Interlocked.Increment(ref loadingHits); break;
                case LoadSaveMode.ResolvingCrossRefs: Interlocked.Increment(ref crossReferenceHits); break;
                case LoadSaveMode.PostLoadInit: Interlocked.Increment(ref postLoadHits); break;
            }
            if (!armed || phase != LoadSaveMode.PostLoadInit) return;
            Interlocked.Increment(ref injected);
            throw new InvalidOperationException("Expected S138 owned callback PostLoadInit fault fixture");
        };
        // Test-only fault setup at the actual owned serializer callback. No product method is
        // patched and no native row/phase is invoked directly to manufacture the failed load.
        exposeField.SetValue(owner, wrapper);
        context.DeferCleanup(() =>
        {
            armed = false;
            EndToEndAssert.True(ReferenceEquals(LoadedModManager.GetMod<MegamodMod>(), mod) &&
                ReferenceEquals(ownerField.GetValue(mod), owner) && ReferenceEquals(exposeField.GetValue(owner), wrapper),
                "Refuse to restore a callback whose exact owning instance or delegate identity changed.");
            exposeField.SetValue(owner, original);
            EndToEndAssert.True(ReferenceEquals(exposeField.GetValue(owner), original));
        });
        EndToEndAssert.True(ReferenceEquals(exposeField.GetValue(owner), wrapper));
    }

    private Dictionary<string, string> PhaseEvidence() => new()
    {
        ["faultBoundary"] = "Actual persistence Action<object> expose callback, after its original action",
        ["savingHits"] = Volatile.Read(ref savingHits).ToString(),
        ["loadingVarsHits"] = Volatile.Read(ref loadingHits).ToString(),
        ["crossReferenceHits"] = Volatile.Read(ref crossReferenceHits).ToString(),
        ["postLoadInitHits"] = Volatile.Read(ref postLoadHits).ToString(),
        ["injectedPostLoadErrors"] = Volatile.Read(ref injected).ToString()
    };

    private Vector2 LoadPoint()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var files = (List<SaveFileInfo>)typeof(Dialog_FileList).GetField("files", flags)!.GetValue(loadDialog)!;
        var scroll = (Vector2)typeof(Dialog_FileList).GetField("scrollPosition", flags)!.GetValue(loadDialog)!;
        int index = files.FindIndex(file => string.Equals(file.FileInfo.FullName, savePath, StringComparison.OrdinalIgnoreCase));
        EndToEndAssert.True(index >= 0);
        float y = 18 + 34 + index * 40 - scroll.y + 20;
        EndToEndAssert.True(y >= 52 && y < loadDialog.windowRect.height - 130, "The exact saved row must be visible.");
        return new Vector2(loadDialog.windowRect.width - 18 - 16 - 36 - 50, y);
    }
    private static Dialog_MessageBox? Notice() => Find.WindowStack.Windows.OfType<Dialog_MessageBox>().SingleOrDefault(box =>
        box.title == "Megamod.DrugTolerance.PersistenceFailureTitle".Translate().ToString());
    private static Vector2 AcknowledgePoint(Dialog_MessageBox box)
    {
        EndToEndAssert.True(box.buttonBText == null && box.buttonCText == null && box.interactionDelay <= 0);
        // Inspected 1.7.5005: margin18, two columns even without B, A in right column inset10.
        float columnWidth = (box.windowRect.width - 2 * 18) / 2;
        return new Vector2(18 + columnWidth + 10 + (columnWidth - 10) / 2,
            box.windowRect.height - 18 - 35f / 2);
    }
    private static string FileHash(string path)
    {
        using var sha = SHA256.Create();
        using var file = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
    }
    private NativeWindowClickActionStep Click(string label, Vector2 point) => new(label, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    private NativeWindowCaptureStep Capture<T>(string label, Func<T> select, Action<T> retain) where T : Window => new(label, windows =>
    {
        var current = select() ?? throw new InvalidOperationException("Missing exact native window for " + label);
        if (current is Dialog_MessageBox message)
        {
            var button = AcknowledgePoint(message) + message.windowRect.position;
            EndToEndAssert.True(Find.WindowStack.GetsInput(message) && ReferenceEquals(Find.WindowStack.GetWindowAt(button), message),
                "The exact notice must receive native input at its OK button before capturing visible evidence.");
        }
        Own(current);
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
    private void Own(Window current)
    {
        if (windowsOwned.Add(current)) context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.MainTabsRoot?.OpenTab?.TabWindow, current)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else if (Find.WindowStack.Windows.Contains(current)) current.Close(false);
        });
    }
}
