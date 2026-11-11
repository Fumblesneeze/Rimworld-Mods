using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.Features.ContentSelection.Core;
using Megamod.Features.ContentSelection.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.ContentSelection;

// Run alone with PrelaunchExcludedShipChunk staged beneath artifacts, without Def hot reload.
// Setup supplies only the inherited global exclusion; both save edits use actual native checkboxes.
[RimWorldEndToEndTest("megamod.content.native-incident-save-override", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 600)]
public sealed class NativeIncidentPersistenceTests : NativeIncidentPersistenceWorkflow
{
    public NativeIncidentPersistenceTests() : base(false) { }
}

[RimWorldEndToEndTest("megamod.content.native-master-off-save-reload", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 600)]
public sealed class NativeIncidentMasterSwitchTests : NativeIncidentPersistenceWorkflow
{
    public NativeIncidentMasterSwitchTests() : base(true) { }
}

public abstract class NativeIncidentPersistenceWorkflow : IRimWorldEndToEndTest
{
    private readonly bool disableMaster;
    protected NativeIncidentPersistenceWorkflow(bool disableMaster) => this.disableMaster = disableMaster;
    private readonly string saveName = "Megamod-Incident-" + Guid.NewGuid().ToString("N");
    private readonly HashSet<Thing> created = new();
    private SettingsRuntime settings = null!;
    private IncidentContentRuntime content = null!;
    private Game originalGame = null!;
    private SaveSettingsBinding originalBinding = null!;
    private SettingsLifecycleRequest<Game> originalReceipt = null!;
    private AppliedContentPolicy originalPolicy = null!;
    private GlobalSettingsSnapshot globalsBefore = null!;
    private SaveSettingsSnapshot saveBefore = null!;
    private IncidentDef incident = null!;
    private ContentKey incidentKey = null!;
    private string settingId = null!;
    private int mapId;
    private HashSet<string> chunksBefore = null!;
    private bool hadFired;
    private int firedTick;
    private bool awaitingActionCapture;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;
    private Dialog_ModSettings? ownedWindow;
    private ContentSelectionWindow? ownedBrowser;
    private EditWindow_Log? ownedLog;
    private Thing? landedChunk;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        originalGame = Current.Game ?? throw new InvalidOperationException("A disposable loaded game is required.");
        originalBinding = settings.CurrentBinding ?? throw new InvalidOperationException("The loaded save has no settings binding.");
        EndToEndAssert.True(settings.SaveLifecycleAvailable);
        // Read only the existing root-owned instance; never construct or pump a second runtime.
        content = (IncidentContentRuntime)(typeof(MegamodMod).GetField("incidents", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(LoadedModManager.GetMod<MegamodMod>()) ?? throw new InvalidOperationException("Owned incident runtime is unavailable."));
        originalReceipt = settings.LifecycleReceipt;
        originalPolicy = content.CurrentAppliedPolicy ?? throw new InvalidOperationException("Owned incident policy is unavailable.");
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A disposable playable map is required.");
        mapId = map.uniqueID;
        incident = DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop");
        incidentKey = new ContentKey(incident.modContentPack.PackageIdPlayerFacing, incident.GetType().FullName!, incident.defName);
        settingId = disableMaster ? ContentSettings.Enabled.Id : SettingId(incidentKey);
        globalsBefore = settings.Store.ExportGlobals();
        saveBefore = settings.Store.ExportSave(originalBinding);
        AssertAppliedPolicy(reloaded: false);
        EndToEndAssert.True(globalsBefore.Values.TryGetValue(SettingId(incidentKey), out string? encoded) && encoded == "false",
            "Stage PrelaunchExcludedShipChunk beneath artifacts and supply that directory as prelaunchConfigDirectory.");
        // The single excluded incident is the first visible row, regardless of translated labels.
        foreach (var entry in NativeContentCatalog.Capture().Catalog.Entries.Where(entry => entry.Identity is IncidentDef))
        {
            string id = SettingId(entry.Key);
            EndToEndAssert.True(!saveBefore.Values.ContainsKey(id), "Fresh save incident choices must have no dormant overrides.");
            if (id != SettingId(incidentKey))
                EndToEndAssert.True(!globalsBefore.Values.TryGetValue(id, out string? value) || value == "true",
                    "Only ShipChunkDrop may be globally excluded in this scenario.");
        }
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>(), "This must be the first settings opening.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<ContentSelectionWindow>());
        if (disableMaster)
        {
            EndToEndAssert.False(saveBefore.Values.ContainsKey(ContentSettings.Enabled.Id));
            EndToEndAssert.True(settings.Store.GetGlobal(ContentSettings.Enabled));
        }
        EndToEndAssert.False(Find.WindowStack.IsOpen<FloatMenu>());
        EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(Prefs.DevMode, "The isolated run must allow native console inspection.");
        EndToEndAssert.False(map.listerThings.ThingsOfDef(ThingDefOf.ShipChunkIncoming).Any(),
            "An unrelated incoming chunk would make the before/after evidence ambiguous.");
        chunksBefore = map.listerThings.ThingsOfDef(ThingDefOf.ShipChunk).Select(thing => thing.ThingID).ToHashSet(StringComparer.Ordinal);
        hadFired = map.StoryState.lastFireTicks.TryGetValue(incident, out firedTick);
        AssertPlacementAvailable();

        string path = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(path), "Never overwrite an existing save.");
        var speed = Find.TickManager.CurTimeSpeed;
        var positionField = typeof(CameraDriver).GetField("rootPos", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot retain the native camera position.");
        var position = (Vector3)positionField.GetValue(Find.CameraDriver)!;
        float size = Find.CameraDriver.RootSize;
        // Independent cleanup actions ensure one cleanup failure cannot skip the others.
        context.DeferCleanup(() => { if (Find.TickManager != null) Find.TickManager.CurTimeSpeed = speed; });
        context.DeferCleanup(() =>
        {
            if (Find.CurrentMap?.uniqueID == mapId) Find.CameraDriver.SetRootPosAndSize(position, size);
        });
        context.DeferCleanup(() => { if (File.Exists(path)) File.Delete(path); });
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.WindowStack.WindowOfType<ContentSelectionWindow>(), ownedBrowser)) ownedBrowser?.Close();
        });
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.WindowStack.WindowOfType<Dialog_ModSettings>(), ownedWindow)) ownedWindow?.Close();
        });
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(Find.WindowStack.WindowOfType<EditWindow_Log>(), ownedLog)) ownedLog?.Close();
        });
        context.DeferCleanup(() =>
        {
            var failures = new List<Exception>();
            try { Find.TickManager?.Pause(); }
            catch (Exception exception) { failures.Add(exception); }
            // If the native action failed its expected-result check, capture its paused side effects too.
            try { if (awaitingActionCapture) CaptureCreatedThings(); }
            catch (Exception exception) { failures.Add(exception); }
            foreach (var thing in created)
            {
                try { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); }
                catch (Exception exception) { failures.Add(exception); }
            }
            if (failures.Count != 0) throw new AggregateException("Could not clean up owned incident results.", failures);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before the inherited exclusion", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("map before the excluded ship-chunk request", Array.Empty<string>(), 0);
        foreach (var step in RefusedRequest("inherited global exclusion")) yield return step;
        yield return new ModSettingsActionStep("open native incident settings", "fumblesneeze.megamod");
        yield return new NativeWindowCaptureStep("capture the native settings window", windows =>
        {
            window = windows.Single(item => item.RuntimeType == typeof(Dialog_ModSettings).FullName);
            ownedWindow = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
            EndToEndAssert.NotNull(ownedWindow);
            EndToEndAssert.True(window.Width >= 700 && window.Height >= 500);
        });
        yield return new ScreenshotStep("ordinary settings before opening the content catalogue", Array.Empty<string>(), 0);
        yield return new NativePointerScopeBeginStep("retain the pointer for native save controls", handle => pointerScope = handle);
        if (disableMaster)
        {
            double masterColumn = (window.Width - 36 - 20 - 16) / 3;
            yield return Click("uncheck Use global for the content master", masterColumn / 2, 32 + 40 + 32 + 14);
            yield return new AssertionStep("native inheritance action retains the enabled master value", _ => AssertSaveChoice("true"));
            yield return Click("turn off content exclusions only for this save", masterColumn + 8 + masterColumn / 2, 32 + 40 + 54 + 14);
            yield return new AssertionStep("native master checkbox preserves individual exclusions and globals", _ => AssertSaveChoice("false"));
            yield return new ScreenshotStep("content master off is pending save and reload", Array.Empty<string>(), 0);
        }
        else
        {
        yield return Click("open Content selection through the native button", (window.Width - 36 - 20) / 2, 32 + 15);
        yield return new NativeWindowCaptureStep("capture the dedicated content browser", windows =>
        {
            window = windows.Single(item => item.RuntimeType == typeof(ContentSelectionWindow).FullName);
            ownedBrowser = Find.WindowStack.WindowOfType<ContentSelectionWindow>();
            EndToEndAssert.NotNull(ownedBrowser);
            EndToEndAssert.True(window.Width >= 700 && window.Height >= 500);
        });
        yield return new ScreenshotStep("ShipChunkDrop inherits the applied global exclusion in the browser", Array.Empty<string>(), 0);
        double column = (window.Width - 36 - 20 - 16) / 3;
        yield return Click("uncheck Use global for ShipChunkDrop", column / 2, 32 + 168 + 32 + 14);
        yield return new AssertionStep("native inheritance action seeds the excluded save value", _ => AssertSaveChoice("false"));
        yield return new ScreenshotStep("ShipChunkDrop current-save editor enabled and still excluded", Array.Empty<string>(), 0);
        yield return Click("allow ShipChunkDrop only in the current save", column + 8 + column / 2, 32 + 168 + 54 + 14);
        yield return new AssertionStep("native save checkbox changes the override without changing globals", _ => AssertSaveChoice("true"));
        }
        // The newly allowed row now sorts with allowed entries; do not claim it remains visible.
        yield return new NativePointerScopeEndStep("restore the pointer after the save edit", pointerScope);
        if (!disableMaster)
            yield return new WindowCancelActionStep("close the content browser before testing pending behavior", typeof(ContentSelectionWindow).FullName!);
        yield return new WindowCancelActionStep("close native settings before testing pending behavior", typeof(Dialog_ModSettings).FullName!);
        foreach (var step in RefusedRequest("pending save override before reload")) yield return step;
        yield return new SaveLoadActionStep("save and reload the native colony with its override", saveName);
        yield return new WaitUntilStep("observe the same save under a fresh native activation", _ =>
                !ReferenceEquals(Current.Game, originalGame) && settings.CurrentBinding is { } binding &&
                !ReferenceEquals(binding, originalBinding), new EndToEndDeadline(int.MaxValue, 600, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep("native serialization retained only the requested save override", _ =>
        {
            EndToEndAssert.Equal(saveBefore.SaveIdentity, settings.CurrentBinding!.SaveIdentity);
            EndToEndAssert.True(settings.CurrentBinding.Generation != originalBinding.Generation);
            AssertSaveChoice(disableMaster ? "false" : "true");
            AssertAppliedPolicy(reloaded: true);
            AssertRefusedSideEffects();
            AssertPlacementAvailable();
        });
        yield return new TimeControlActionStep("pause on the reloaded incident boundary", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("reloaded map before the now-allowed native request", Array.Empty<string>(), 0);
        awaitingActionCapture = true;
        yield return new IncidentActionStep("execute ShipChunkDrop after native save reload", "ShipChunkDrop");
        yield return new AssertionStep("retain exact objects created by this native incident", _ =>
        {
            CaptureCreatedThings();
            awaitingActionCapture = false;
            EndToEndAssert.True(created.Any(thing => thing.def == ThingDefOf.ShipChunkIncoming && thing.Spawned));
        });
        yield return new TimeControlActionStep("let the incident's physical chunk land", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("observe the owned chunk land through ordinary ticks", _ =>
        {
            landedChunk = created.FirstOrDefault(thing => thing.def == ThingDefOf.ShipChunk && thing.Spawned);
            return landedChunk != null;
        }, new EndToEndDeadline(int.MaxValue, 1800, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep("pause on the persisted policy result", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame the chunk created after reload", new[] { landedChunk!.ThingID }, 120);
        yield return new ScreenshotStep("physical ship chunk after the save override applies", new[] { landedChunk!.ThingID }, 120);
        yield return new AssertionStep("native result retains the incident and physical Def identities", _ =>
        {
            AssertAppliedPolicy(reloaded: true);
            EndToEndAssert.True(ReferenceEquals(incident, DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop")));
            EndToEndAssert.True(ReferenceEquals(landedChunk!.def, ThingDefOf.ShipChunk));
            EndToEndAssert.True(landedChunk.Spawned && !chunksBefore.Contains(landedChunk.ThingID));
            AssertSaveChoice(disableMaster ? "false" : "true");
        });
        yield return new NativeDeveloperLogActionStep("open the native console after the persistence workflow", true);
        yield return new AssertionStep("retain the native console for cleanup", _ =>
        {
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
            EndToEndAssert.NotNull(ownedLog);
        });
        yield return new ScreenshotStep("native console after incident save persistence", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("restore the closed native console", false);
    }

    private IEnumerable<EndToEndStep> RefusedRequest(string phase)
    {
        yield return new AssertionStep("confirm owned exclusion and native placement before " + phase, _ =>
        {
            AssertAppliedPolicy(reloaded: false);
            AssertPlacementAvailable();
        });
        awaitingActionCapture = true;
        yield return new IncidentActionStep("request ShipChunkDrop under " + phase, "ShipChunkDrop", expectRejected: true);
        yield return new AssertionStep("no native side effects under " + phase, _ =>
        {
            CaptureCreatedThings();
            awaitingActionCapture = false;
            AssertAppliedPolicy(reloaded: false);
            AssertRefusedSideEffects();
        });
        yield return new ScreenshotStep("visible refusal and unchanged map under " + phase, Array.Empty<string>(), 0);
    }

    private Map CurrentMap => Find.Maps.Single(map => map.uniqueID == mapId);
    private static string SettingId(ContentKey key) => "content.allowed/" + key.ToStorageToken();
    private NativeWindowClickActionStep Click(string name, double x, double y) => new(name, window.Handle,
        (window.X + 18 + x) * window.UiScale, (window.Y + 18 + 40 + y) * window.UiScale, null, pointerScope);

    private void AssertAppliedPolicy(bool reloaded)
    {
        var receipt = settings.LifecycleReceipt;
        EndToEndAssert.True(receipt.Available && receipt.Save != null && ReferenceEquals(receipt.Owner, Current.Game),
            "The owned application must have an available receipt for the actual current game.");
        EndToEndAssert.Equal(saveBefore.SaveIdentity, receipt.Save!.SaveIdentity);
        EndToEndAssert.True(content.PolicyAvailable, "Vanilla fallback is not an applied owned content policy.");
        // This public getter checks the publication's exact receipt and game owner internally.
        var policy = content.CurrentAppliedPolicy ?? throw new InvalidOperationException("Owned incident publication is unavailable.");
        EndToEndAssert.True(ReferenceEquals(content.Catalog.Resolve(incidentKey).Identity, incident));
        EndToEndAssert.True(policy.KeyDecisions.TryGetValue(incidentKey, out bool allowed) && allowed == reloaded,
            "ShipChunkDrop must have the expected explicit applied key decision, not an unknown-key default.");
        EndToEndAssert.Equal(reloaded, policy.IsAllowed(incident));
        if (reloaded)
        {
            EndToEndAssert.False(ReferenceEquals(Current.Game, originalGame));
            EndToEndAssert.False(ReferenceEquals(receipt, originalReceipt));
            EndToEndAssert.True(receipt.Generation != originalReceipt.Generation,
                "The reloaded policy must belong to a fresh native load generation.");
            EndToEndAssert.False(ReferenceEquals(policy, originalPolicy), "The original excluded publication must not survive the reload.");
        }
        else
        {
            EndToEndAssert.True(ReferenceEquals(receipt, originalReceipt) && ReferenceEquals(Current.Game, originalGame));
            EndToEndAssert.True(ReferenceEquals(policy, originalPolicy), "Pending edits must retain the original applied policy.");
        }
        EndToEndAssert.True(ReferenceEquals(receipt, settings.LifecycleReceipt) && ReferenceEquals(policy, content.CurrentAppliedPolicy),
            "Receipt and owned policy must remain current throughout this diagnostic read.");
    }

    private void AssertSaveChoice(string expected)
    {
        EndToEndAssert.True(globalsBefore.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            "A native save override must not alter any global default.");
        var current = settings.Store.ExportSave(settings.CurrentBinding!);
        EndToEndAssert.True(current.Values.TryGetValue(settingId, out var value) && !value.UseGlobal && value.Override == expected);
        EndToEndAssert.True(saveBefore.Values.Where(pair => pair.Key != settingId).OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(current.Values.Where(pair => pair.Key != settingId).OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            "Only the clicked incident's save entry may change.");
    }

    private void AssertPlacementAvailable() => EndToEndAssert.True(CellFinderLoose.TryFindSkyfallerCell(
        ThingDefOf.ShipChunkIncoming, CurrentMap, ThingDefOf.ShipChunk.terrainAffordanceNeeded,
        out _, 10, CurrentMap.Center, 999999), "Natural placement failure must not masquerade as an exclusion.");

    private void AssertRefusedSideEffects()
    {
        EndToEndAssert.False(CurrentMap.listerThings.ThingsOfDef(ThingDefOf.ShipChunkIncoming).Any());
        EndToEndAssert.True(chunksBefore.SetEquals(CurrentMap.listerThings.ThingsOfDef(ThingDefOf.ShipChunk).Select(thing => thing.ThingID)));
        bool nowFired = CurrentMap.StoryState.lastFireTicks.TryGetValue(incident, out int nowTick);
        EndToEndAssert.True(nowFired == hadFired && (!nowFired || nowTick == firedTick));
        EndToEndAssert.True(ReferenceEquals(incident, DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop")));
    }

    private void CaptureCreatedThings()
    {
        // Called while paused, immediately after this scenario's action; later cleanup uses these
        // exact references/contained chunks, never every new Thing after ordinary ticking resumes.
        foreach (var thing in CurrentMap.listerThings.AllThings.Where(thing =>
            thing.def == ThingDefOf.ShipChunkIncoming || thing.def == ThingDefOf.ShipChunk && !chunksBefore.Contains(thing.ThingID)))
        {
            created.Add(thing);
            if (thing is IThingHolder holder)
                foreach (var held in holder.GetDirectlyHeldThings())
                    if (held.def == ThingDefOf.ShipChunk) created.Add(held);
        }
    }
}
