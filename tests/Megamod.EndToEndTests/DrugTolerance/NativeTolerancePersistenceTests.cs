using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// The native Duplicate control and native save/load manufacture the copy/restored rows.
// Hidden tolerance values are supporting component evidence until the owned editor is delivered.
[RimWorldEndToEndTest("megamod.drug-tolerance.native-copy-save", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 1200)]
public sealed class NativeTolerancePersistenceTests : IRimWorldEndToEndTest
{
    private readonly string marker = "Tolerance-" + Guid.NewGuid().ToString("N");
    private readonly string saveName = "Megamod-Tolerance-" + Guid.NewGuid().ToString("N");
    private readonly NativeDrugPolicyView view = new();
    private readonly HashSet<Window> ownedWindows = new();
    private IEndToEndContext context = null!;
    private Game initialGame = null!;
    private Game? loadedGame;
    private int mapId, sourceId, copyId;
    private DrugPolicy source = null!, copy = null!;
    private Pawn pawn = null!;
    private string pawnId = null!;
    private bool saveRequested;
    private string? expectedCopyLabel;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies dialog = null!;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;
    private static readonly (string Drug, string Encoding)[] Values =
    {
        ("Beer", "1:limit:0"), ("SmokeleafJoint", "1:limit:1"), ("Yayo", "2:unknown:retain-exactly")
    };
    internal string SavePath => GenFilePaths.FilePathForSavedGame(saveName);

    public void Arrange(IEndToEndContext context)
    {
        this.context = context;
        initialGame = Current.Game;
        var map = initialGame.CurrentMap;
        EndToEndAssert.True(initialGame.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>());
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>());
        mapId = map.uniqueID;
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => OwnedGame().tickManager.CurTimeSpeed = speed);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        source = initialGame.drugPolicyDatabase.MakeNewDrugPolicy();
        sourceId = source.id;
        source.label = marker;
        context.DeferCleanup(() =>
        {
            var policies = OwnedGame().drugPolicyDatabase.AllPolicies;
            var ownedSource = policies.SingleOrDefault(policy => policy.id == sourceId && policy.label == marker);
            if (ownedSource != null) policies.Remove(ownedSource);
        });
        context.DeferCleanup(() =>
        {
            var game = OwnedGame();
            if (copyId != 0) game.drugPolicyDatabase.AllPolicies.RemoveAll(policy => policy.id == copyId && policy.label == expectedCopyLabel);
        });
        foreach (var (drug, encoding) in Values) NativeDrugToleranceState.WriteEncoded(Entry(source, drug), encoding);
        Entry(source, "Beer").allowedForJoy = false;
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameSingle(marker);
        pawnId = pawn.ThingID;
        context.DeferCleanup(() =>
        {
            var current = OwnedGame().CurrentMap.mapPawns.AllPawnsSpawned.SingleOrDefault(item => item.ThingID == pawnId && item.Name?.ToStringFull == marker);
            if (current != null && !current.Destroyed) current.Destroy(DestroyMode.Vanish);
        });
        GenSpawn.Spawn(pawn, CellRect.CenteredOn(map.Center, 20).First(cell => cell.InBounds(map) && cell.Walkable(map)), map);
        string savePath = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(savePath));
        context.DeferCleanup(() =>
        {
            if (saveRequested && File.Exists(savePath)) { _ = OwnedGame(); File.Delete(savePath); }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativeMainTabActionStep("open Assign for native tolerance persistence", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture Assign policy manager entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("own pointer for native policy duplication", value => pointerScope = value);
        yield return Click("open the native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native source policy list", value => dialog = value);
        yield return Click("select the disposable source policy", view.PolicyRow(dialog, source).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture selected source before duplication", value => dialog = value);
        yield return Shot("source policy before native duplicate");
        yield return new CheckpointStep("source owned choices before native copy", _ => Evidence(source));
        yield return new AssertionStep("bind the next native policy identity for interrupted-copy cleanup", _ =>
        {
            // Native MakeNewDrugPolicy allocates max(id)+1 and this default name. Bind before
            // the click so a failed capture cannot orphan the exact newly created fixture.
            copyId = OwnedGame().drugPolicyDatabase.AllPolicies.Max(policy => policy.id) + 1;
            expectedCopyLabel = "DrugPolicy".Translate() + " " + copyId;
        });
        yield return Click("duplicate the source with the native Copy icon", DuplicateButton(dialog).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the newly selected native copy", value => dialog = value);
        yield return new AssertionStep("native duplicate created separate rows and preserved all owned encodings", _ =>
        {
            copy = view.Selected(dialog) ?? throw new InvalidOperationException("Native duplicate did not select a policy.");
            EndToEndAssert.False(ReferenceEquals(copy, source));
            EndToEndAssert.Equal(copyId, copy.id);
            EndToEndAssert.Equal(expectedCopyLabel, copy.label);
            AssertChoices(source); AssertChoices(copy);
            foreach (var (drug, _) in Values) EndToEndAssert.False(ReferenceEquals(Entry(source, drug), Entry(copy, drug)));
        });
        yield return Shot("native copy selected with source still listed");
        yield return Click("edit only the copied Beer recreation checkbox", view.BeerJoyCheckbox(dialog, copy).center);
        yield return new AssertionStep("native edit distinguishes the independent copied policy", _ =>
        {
            EndToEndAssert.False(Entry(source, "Beer").allowedForJoy);
            EndToEndAssert.True(Entry(copy, "Beer").allowedForJoy);
            AssertChoices(source); AssertChoices(copy);
        });
        yield return Shot("copied policy has its independent native checkbox edit");
        yield return new CheckpointStep("copied choices before native save", _ => Evidence(copy));
        yield return new WindowCancelActionStep("close policy manager before save", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer before native save/load", pointerScope);
        yield return new NativeMainTabActionStep("close Assign before native save/load", "Assign", false);
        saveRequested = true;
        yield return new SaveLoadActionStep("save and reload native copied policies", saveName);
        yield return new AssertionStep("new native rows retain exact owned values after loading", _ =>
        {
            var game = OwnedGame();
            EndToEndAssert.False(ReferenceEquals(game, initialGame));
            var restoredSource = game.drugPolicyDatabase.AllPolicies.Single(policy => policy.id == sourceId && policy.label == marker);
            var restoredCopy = game.drugPolicyDatabase.AllPolicies.Single(policy => policy.id == copyId);
            EndToEndAssert.False(ReferenceEquals(restoredSource, source) || ReferenceEquals(restoredCopy, copy));
            source = restoredSource; copy = restoredCopy;
            AssertChoices(source); AssertChoices(copy);
            EndToEndAssert.False(Entry(source, "Beer").allowedForJoy);
            EndToEndAssert.True(Entry(copy, "Beer").allowedForJoy);
        });
        yield return new NativeMainTabActionStep("reopen Assign after native save/load", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture loaded policy manager entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("own pointer for loaded native policy controls", value => pointerScope = value);
        yield return Click("reopen native policy manager in loaded game", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture loaded native policy list", value => dialog = value);
        yield return Click("select the persisted native copy", view.PolicyRow(dialog, copy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture persisted copy checkbox", value => dialog = value);
        yield return Shot("native copied policy and checkbox edit survive save reload");
        yield return new CheckpointStep("loaded source and copy encoded choices", _ => new Dictionary<string, string>
        {
            ["source"] = string.Join(";", Evidence(source).Select(pair => pair.Key + "=" + pair.Value)),
            ["copy"] = string.Join(";", Evidence(copy).Select(pair => pair.Key + "=" + pair.Value)),
            ["note"] = "Hidden tolerance fields are supporting evidence; no tolerance editor/enforcement accepted."
        });
        yield return new WindowCancelActionStep("close loaded policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer after loaded policy inspection", pointerScope);
        yield return new NativeMainTabActionStep("close Assign after persistence proof", "Assign", false);
        yield return new NativeDeveloperLogActionStep("open native console after tolerance copy/save", true);
        yield return Capture<EditWindow_Log>("capture owned console", _ => { });
        yield return Shot("native console after tolerance copy save reload");
        yield return new NativeDeveloperLogActionStep("close native console after persistence proof", false);
    }

    private Game OwnedGame()
    {
        var game = Current.Game ?? throw new InvalidOperationException("Owned game is absent.");
        var map = game.CurrentMap ?? throw new InvalidOperationException("Owned map is absent.");
        EndToEndAssert.True(game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && map.uniqueID == mapId);
        if (!ReferenceEquals(game, initialGame) && !ReferenceEquals(game, loadedGame))
        {
            EndToEndAssert.True(loadedGame == null, "Refuse an unrelated second replacement game.");
            EndToEndAssert.True(saveRequested && game.drugPolicyDatabase.AllPolicies.Any(policy => policy.id == sourceId && policy.label == marker));
            EndToEndAssert.True(map.mapPawns.AllPawnsSpawned.Any(item => item.ThingID == pawnId && item.Name?.ToStringFull == marker));
            loadedGame = game;
        }
        return game;
    }

    private static DrugPolicyEntry Entry(DrugPolicy policy, string defName) =>
        Enumerable.Range(0, policy.Count).Select(i => policy[i]).Single(entry => entry.drug.defName == defName);
    private static void AssertChoices(DrugPolicy policy)
    {
        foreach (var (drug, encoding) in Values) EndToEndAssert.Equal(encoding, NativeDrugToleranceState.ReadEncoded(Entry(policy, drug)));
    }
    private static Dictionary<string, string> Evidence(DrugPolicy policy) => Values.ToDictionary(value => value.Drug,
        value => policy.id + ":" + NativeDrugToleranceState.ReadEncoded(Entry(policy, value.Drug)));
    private static Rect DuplicateButton(Dialog_ManageDrugPolicies dialog)
    {
        // Native manager rect7: rightmost Delete (32), ten-unit gap, then Duplicate (32).
        var button = new Rect(dialog.windowRect.width - 18 - 32 - 10 - 32, 18 + 74, 32, 32);
        EndToEndAssert.True(button.xMin >= 0 && button.xMax <= dialog.windowRect.width && button.yMax <= dialog.windowRect.height);
        return button;
    }
    private static ScreenshotStep Shot(string label) => new(label, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string label, Vector2 point) => new(label, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointerScope);
    private NativeWindowCaptureStep Capture<T>(string label, Action<T> retain) where T : Window => new(label, windows =>
    {
        var current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Missing native window: " + typeof(T));
        if (ownedWindows.Add(current)) context.DeferCleanup(() =>
        {
            if (current is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, current))
            { _ = OwnedGame(); Find.MainTabsRoot.EscapeCurrentTab(false); }
            else if (Find.WindowStack.Windows.Contains(current)) { _ = OwnedGame(); current.Close(false); }
        });
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
}
