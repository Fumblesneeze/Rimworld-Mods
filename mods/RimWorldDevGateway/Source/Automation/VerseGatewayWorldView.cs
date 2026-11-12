using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

internal sealed class VerseGatewayWorldView : IGatewayWorldViewRuntime
{
    private readonly Func<bool> inputIdle;
    private readonly Func<long> completedFrame;
    internal VerseGatewayWorldView(Func<bool> inputIdle, Func<long> completedFrame)
    { this.inputIdle = inputIdle; this.completedFrame = completedFrame; }

    public GatewayAutomationAvailability Availability()
    {
#if RIMWORLD_1_7
        return new(VerseGatewayWorldViewSnapshot.ShapeAvailable, "native_world_contract_unavailable");
#else
        return new(false, "native_world_runtime_unverified");
#endif
    }

    public IGatewayWorldViewSession Capture()
    {
#if RIMWORLD_1_7
        if (!Availability().Available) throw Error("native_world_contract_unavailable");
        return new VerseGatewayWorldViewSnapshot(inputIdle, completedFrame);
#else
        throw Error("native_world_runtime_unverified");
#endif
    }

    internal static GatewayAutomationException Error(string code) => new(code,
        "The inspected native World workflow requires the exact current settled view and its owned restoration state.");
}

#if RIMWORLD_1_7
internal sealed class VerseGatewayWorldViewSnapshot : IGatewayWorldViewSession, IGatewayWorldViewEvidence
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    internal static readonly FieldInfo? Buttons = typeof(MainButtonsRoot).GetField("allButtonsInOrder", Fields);
    private static readonly FieldInfo? WorkerField = typeof(MainButtonDef).GetField("workerInt", Fields);
    private static readonly MethodInfo? WorldCommand = typeof(MainButtonWorker).GetMethod("InterfaceTryActivate",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
    private static readonly string[] CameraNames = { "config", "sphereRotation", "rotationVelocity", "desiredRotation", "desiredRotationRaw",
        "desiredAltitude", "altitude", "dragTimeStamps", "releasedLeftWhileHoldingMiddle", "layerAltitudeOffset", "layerOffsetVel",
        "layerOriginOffset", "layerOriginOffsetVel", "altitudeDecay", "altitudeDecayVel", "prevSelectedLayer", "cachedCamera",
        "mouseCoveredByUI", "mouseTouchingScreenBottomEdgeStartTime", "fixedTimeStepBuffer", "rotationAnimation_prevSphereRotation",
        "rotationAnimation_lerpFactor", "lastSelectedLayer" };
    private static readonly Type?[] CameraTypes = { typeof(WorldCameraConfig), typeof(Quaternion), typeof(Vector2), typeof(Vector2), typeof(Vector2),
        typeof(float), typeof(float), typeof(WorldCameraDriver).GetField("dragTimeStamps", Fields)?.FieldType,
        typeof(bool), typeof(float), typeof(float), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float), typeof(PlanetLayer), typeof(Camera),
        typeof(bool), typeof(float), typeof(float), typeof(Quaternion), typeof(float), typeof(PlanetLayer) };
    private static readonly FieldInfo[] CameraFields = Resolve(typeof(WorldCameraDriver), CameraNames, CameraTypes);
    private static readonly FieldInfo[] ConfigFields = Resolve(typeof(WorldCameraConfig),
        new[] { "dollyRateKeys", "dollyRateScreenEdge", "camRotationDecayFactor", "rotationSpeedScale", "zoomSpeed", "zoomPreserveFactor", "smoothZoom" },
        new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(bool) });
    private static readonly FieldInfo[] SelectorFields = Resolve(typeof(WorldSelector), new[] { "selectedLayer", "selectedTile" },
        new[] { typeof(PlanetLayer), typeof(PlanetTile) });
    private static readonly FieldInfo[] PaneFields = Resolve(typeof(WorldInspectPane), new[] { "openTabType", "<RecentHeight>k__BackingField" },
        new[] { typeof(Type), typeof(float) });
    internal static bool ShapeAvailable => Buttons?.FieldType == typeof(List<MainButtonDef>) && WorkerField?.FieldType == typeof(MainButtonWorker) &&
        WorldCommand is { IsGenericMethod: false, ContainsGenericParameters: false, ReturnType: var result } && result == typeof(void) &&
        WorldCommand.GetMethodBody() != null && CameraFields.Length == 23 && typeof(WorldCameraDriver).GetFields(Fields).Count(f => !f.IsStatic) == 23 &&
        ConfigFields.Length == 7 && typeof(WorldCameraConfig).GetFields(Fields).Length == 7 &&
        typeof(WorldCameraConfig_Normal).GetFields(Fields).Length == 0 && SelectorFields.Length == 2 && PaneFields.Length == 2 &&
        CameraFields[7].FieldType.IsGenericType && CameraFields[7].FieldType.GetGenericTypeDefinition() == typeof(List<>) &&
        CameraFields[7].FieldType.GetGenericArguments()[0].FullName == "Verse.CameraDriver+DragTimeStamp";

    internal readonly Game Game;
    internal readonly World World;
    internal readonly Map Map;
    internal readonly Root_Play Root;
    internal readonly UIRoot_Play Ui;
    internal readonly WindowStack Stack;
    internal readonly MainTabsRoot Tabs;
    internal readonly MainButtonsRoot Bar;
    internal readonly List<MainButtonDef> ButtonList;
    internal readonly MainButtonDef WorldButton;
    internal readonly MainButtonWorker_ToggleWorld Worker;
    internal readonly WorldInterface WorldUi;
    internal readonly WorldRenderer Renderer;
    internal readonly WorldSelector Selector;
    internal readonly WorldInspectPane Pane;
    private readonly WorldDragBox dragBox;
    private readonly WorldTargeter targeter;
    private readonly WorldRoutePlanner routePlanner;
    private readonly TilePicker tilePicker;
    private readonly WorldGrid grid;
    private readonly PlanetLayer surface;
    private readonly List<WorldObject> selection;
    private readonly Dictionary<WorldObject, float> selectTimes;
    private readonly Camera camera;
    private readonly Camera skybox;
    private readonly WorldCameraDriver driver;
    private readonly Transform transform;
    private readonly Transform? parent;
    private readonly Vector3 position, scale, skyboxPosition, skyboxScale;
    private readonly Quaternion rotation, skyboxRotation;
    private readonly bool enabled, skyboxActive;
    private readonly float fieldOfView, nearClip, farClip;
    private readonly bool orthographic, cameraEnabled;
    private readonly object config;
    private readonly object?[] cameraValues, configValues, selectorValues, paneValues;
    private readonly System.Collections.IList dragTimes;
    private readonly bool resetView, everReset;
    private readonly Vector2 dragStart;
    private readonly Rect paneRect;
    private readonly Func<bool> inputIdle;
    private readonly Func<long> completedFrame;
    private readonly HashSet<WorldObject> ownedSelections = new();
    private readonly HashSet<WITab_Caravan_Health> ownedHealthTabs = new();
    private int restorePhase;
    private long restoreFrame;
    private string? restoreError;
    private readonly Dictionary<string, string> restoreChecks = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, object?> BaselineEvidence { get; }

    internal VerseGatewayWorldViewSnapshot(Func<bool> inputIdle, Func<long> completedFrame)
    {
        RequireSettled();
        this.inputIdle = inputIdle; this.completedFrame = completedFrame;
        Game = Current.Game; World = Game.World; Map = Find.CurrentMap;
        Root = (Root_Play)Current.Root; Ui = (UIRoot_Play)Find.UIRoot; Stack = Find.WindowStack; Tabs = Find.MainTabsRoot;
        Bar = Ui.mainButtonsRoot;
        ButtonList = (List<MainButtonDef>)Buttons!.GetValue(Bar)!;
        if (ButtonList.Count > 128) throw Error("native_world_button_inventory_limit");
        var matching = ButtonList.Where(b => b?.workerClass == typeof(MainButtonWorker_ToggleWorld)).ToArray();
        if (matching.Length != 1 || WorkerField!.GetValue(matching[0]) is not MainButtonWorker_ToggleWorld worker || worker.GetType() != typeof(MainButtonWorker_ToggleWorld))
            throw Error("native_world_button_unavailable");
        WorldButton = matching[0]; Worker = worker;
        WorldUi = World.UI; Renderer = World.renderer; Selector = WorldUi.selector; Pane = WorldUi.inspectPane;
        targeter = WorldUi.targeter; routePlanner = WorldUi.routePlanner; tilePicker = WorldUi.tilePicker;
        grid = World.grid; surface = grid.Surface;
        dragBox = Selector.dragBox; selection = Selector.SelectedObjects; selectTimes = WorldSelectionDrawer.SelectTimes;
        camera = WorldCameraManager.WorldCamera; skybox = WorldCameraManager.WorldSkyboxCamera; driver = WorldCameraManager.WorldCameraDriver;
        if (camera == null || skybox == null || driver == null || driver.gameObject != camera.gameObject || camera.gameObject.activeSelf ||
            driver.config?.GetType() != typeof(WorldCameraConfig_Normal) || Tabs.OpenTab != null || Pane.IsOpen ||
            selection.Count != 0 || selectTimes.Count != 0 || Find.Selector.SelectedObjectsListForReading.Count != 0 || dragBox.active ||
            Renderer.wantedMode != WorldRenderMode.None || WorldRendererUtility.CurrentWorldRenderMode != WorldRenderMode.None ||
            !inputIdle() || completedFrame() <= 0) throw Error("native_world_baseline_unsupported");
        RequireCommands();
        transform = camera.transform; parent = transform.parent; position = transform.position; rotation = transform.rotation; scale = transform.localScale;
        if (skybox.transform.parent != transform) throw Error("native_world_skybox_owner_changed");
        skyboxPosition = skybox.transform.localPosition; skyboxRotation = skybox.transform.localRotation;
        skyboxScale = skybox.transform.localScale; skyboxActive = skybox.gameObject.activeSelf; enabled = driver.enabled;
        fieldOfView = camera.fieldOfView; nearClip = camera.nearClipPlane; farClip = camera.farClipPlane;
        orthographic = camera.orthographic; cameraEnabled = camera.enabled;
        config = driver.config; configValues = Read(ConfigFields, config); cameraValues = Read(CameraFields, driver);
        dragTimes = (System.Collections.IList)cameraValues[7]!;
        if (dragTimes.Count != 0) throw Error("native_world_camera_drag_active");
        selectorValues = Read(SelectorFields, Selector); paneValues = Read(PaneFields, Pane);
        resetView = Worker.resetViewNextTime; everReset = WorldUi.everReset; dragStart = dragBox.start; paneRect = Pane.windowRect;
        var evidence = CameraFields.Select((f, i) => new KeyValuePair<string, object?>("camera." + f.Name,
            cameraValues[i] == null ? null : cameraValues[i] is System.Collections.IList ? "same empty list" :
            cameraValues[i] is WorldCameraConfig ? "WorldCameraConfig_Normal" : cameraValues[i]!.ToString())).ToDictionary(p => p.Key, p => p.Value);
        evidence["driverFieldCount"] = CameraFields.Length; evidence["worldCameraActive"] = false;
        evidence["selectedCount"] = 0; evidence["selectionTimesCount"] = 0; evidence["rawSelectedLayer"] = selectorValues[0]?.ToString();
        evidence["rawSelectedTile"] = selectorValues[1]?.ToString(); evidence["resetViewNextTime"] = resetView;
        evidence["everReset"] = everReset; evidence["cameraPosition"] = position.ToString(); evidence["cameraRotation"] = rotation.ToString();
        BaselineEvidence = evidence;
    }

    internal bool CurrentOwner
    {
        get
        {
            if (!UnityData.IsInMainThread || Current.ProgramState != ProgramState.Playing || !ReferenceEquals(Current.Game, Game) ||
                !ReferenceEquals(Game.World, World) || !ReferenceEquals(Find.CurrentMap, Map) || !ReferenceEquals(Current.Root, Root) ||
                !ReferenceEquals(Find.UIRoot, Ui) || !ReferenceEquals(Find.WindowStack, Stack) || !ReferenceEquals(Find.MainTabsRoot, Tabs) ||
                !ReferenceEquals(Ui.mainButtonsRoot, Bar) || !ReferenceEquals(Buttons!.GetValue(Bar), ButtonList) ||
                ButtonList.Count > 128 || !ButtonList.Contains(WorldButton) || !ReferenceEquals(WorkerField!.GetValue(WorldButton), Worker) || !ReferenceEquals(Worker.def, WorldButton) || !ReferenceEquals(World.UI, WorldUi) ||
                !ReferenceEquals(World.renderer, Renderer) || !ReferenceEquals(WorldUi.selector, Selector) || !ReferenceEquals(WorldUi.inspectPane, Pane) ||
                !ReferenceEquals(WorldUi.targeter, targeter) || !ReferenceEquals(WorldUi.routePlanner, routePlanner) ||
                !ReferenceEquals(WorldUi.tilePicker, tilePicker) || !ReferenceEquals(World.grid, grid) || !ReferenceEquals(grid.Surface, surface) ||
                !ReferenceEquals(Selector.dragBox, dragBox) || !ReferenceEquals(Selector.SelectedObjects, selection) ||
                !ReferenceEquals(WorldSelectionDrawer.SelectTimes, selectTimes)) return false;
            return camera != null && skybox != null && driver != null && transform != null &&
                ReferenceEquals(WorldCameraManager.WorldCamera, camera) && ReferenceEquals(WorldCameraManager.WorldSkyboxCamera, skybox) &&
                ReferenceEquals(WorldCameraManager.WorldCameraDriver, driver) && ReferenceEquals(driver.config, config) &&
                ReferenceEquals(CameraFields[7].GetValue(driver), dragTimes) && transform.parent == parent && transform.localScale.Equals(scale) &&
                driver.enabled == enabled && skybox.transform.parent == transform && skybox.transform.localPosition.Equals(skyboxPosition) &&
                camera.fieldOfView == fieldOfView && camera.nearClipPlane == nearClip && camera.farClipPlane == farClip &&
                camera.orthographic == orthographic && camera.enabled == cameraEnabled &&
                skybox.transform.localRotation.Equals(skyboxRotation) && skybox.transform.localScale.Equals(skyboxScale) &&
                skybox.gameObject.activeSelf == skyboxActive && Equal(ConfigFields, config, configValues);
        }
    }

    public GatewayWorldViewState Observe() => !CurrentOwner ? new(false, null, false) :
        new(Renderer.wantedMode == WorldRenderMode.Planet && WorldRendererUtility.WorldSelected, Tabs.OpenTab?.defName, true, CanCommand);

    internal bool CanCommand
    { get { try { RequireCommands(); return true; } catch (GatewayAutomationException) { return false; } } }

    internal void RequireCommands()
    {
        RequireSettled();
        if (Stack.Windows.Count > 128 || Tabs.OpenTab != null && Tabs.OpenTab != MainButtonDefOf.Inspect ||
            WorldUi.targeter.IsTargeting || WorldUi.routePlanner.Active || WorldUi.tilePicker.Active || dragBox.active ||
            !Worker.Visible || Worker.Disabled || !Stack.GetsInput(null) || !inputIdle()) throw Error("native_world_input_blocked");
    }

    public void Activate(Action beforeNativeCommand)
    {
        if (!CurrentOwner) throw Error("native_world_context_changed");
        RequireCommands();
        if (Renderer.wantedMode == WorldRenderMode.Planet && Tabs.OpenTab != null && Tabs.OpenTab != MainButtonDefOf.Inspect)
            throw Error("native_world_unrelated_main_tab");
        if (WorldRendererUtility.CurrentWorldRenderMode != Renderer.wantedMode) throw Error("native_world_mode_unsupported");
        beforeNativeCommand();
        Worker.InterfaceTryActivate();
    }

    internal void OwnSelection(WorldObject value)
    {
        if (ownedSelections.Count >= 16 && !ownedSelections.Contains(value)) throw Error("native_world_owned_selection_limit");
        ownedSelections.Add(value);
    }

    internal void OwnHealthTab(WITab_Caravan_Health tab)
    {
        if (ownedHealthTabs.Contains(tab)) return;
        if (ownedHealthTabs.Count >= 16 || VerseGatewayCaravanInspection.DetailPawn!.GetValue(tab) != null)
            throw Error("native_caravan_health_baseline_unsupported");
        ownedHealthTabs.Add(tab);
    }

    public GatewayWorldRestoreState Restore()
    {
        if (!CurrentOwner) { restoreChecks["identity"] = "changed; no writes in this advance"; return Receipt(true, false, "identity", "native_world_context_changed"); }
        restoreChecks["identity"] = "same Game, World and complete captured owner chain";
        if (!inputIdle()) return Receipt(false, false, "wait-neutral-input");
        try { RequireSettled(); }
        catch (GatewayAutomationException exception) { return Receipt(true, false, "admission", exception.Code); }
        if (!GatewayWorldViewRestorePolicy.TryClose(targeter.IsTargeting, routePlanner.Active, tilePicker.Active, dragBox.active, () =>
            {
                if (restorePhase == 0) RestorePart("mode", () => { Renderer.wantedMode = WorldRenderMode.None; Renderer.CheckActivateWorldCamera(); });
            }))
        {
            restoreChecks["foreign-activity"] = "preserved active targeter, planner, picker or drag; no writes in this advance";
            return Receipt(true, false, "foreign-activity", "native_world_foreign_activity");
        }
        if (restorePhase == 0)
        {
            if (!CurrentOwner) return Receipt(true, false, "identity-after-deactivation", "native_world_context_changed");
            bool ownsSelection = selection.Count <= 16 && selectTimes.Count <= 16 &&
                selection.All(ownedSelections.Contains) && selectTimes.Keys.All(ownedSelections.Contains);
            if (ownsSelection) RestorePart("selection", () =>
            {
                selection.Clear(); selectTimes.Clear(); Write(SelectorFields, Selector, selectorValues);
                dragBox.active = false; dragBox.start = dragStart;
            });
            else { restoreChecks["selection"] = "skipped foreign selection"; restoreError ??= "native_world_foreign_selection"; }
            RestorePart("view-flags", () => { Worker.resetViewNextTime = resetView; WorldUi.everReset = everReset; PaneFields[0].SetValue(Pane, paneValues[0]); });
            bool detailClean = true;
            foreach (var tab in ownedHealthTabs)
            {
                var pawn = VerseGatewayCaravanInspection.DetailPawn!.GetValue(tab) as Pawn;
                if (pawn == null) continue;
                if (!ownedSelections.OfType<Caravan>().Any(c => c.PawnsListForReading.Count <= 64 && c.PawnsListForReading.Contains(pawn)))
                { detailClean = false; restoreError ??= "native_world_foreign_health_detail"; continue; }
                // Cleanup only: baseline null and exact caravan ownership were captured before
                // input. Native actions and observations never assign the detail field.
                RestorePart("health-detail", () => VerseGatewayCaravanInspection.DetailPawn.SetValue(tab, null));
            }
            restoreChecks["health-detail-baseline"] = detailClean && ownedHealthTabs.All(tab => VerseGatewayCaravanInspection.DetailPawn!.GetValue(tab) == null)
                ? "owned baseline null verified" : "foreign or unrestored detail";
            restorePhase = 1;
        }
        if (restorePhase == 1)
        {
            if (!camera.gameObject.activeSelf && !driver.isActiveAndEnabled)
            {
                if (!GatewayWorldViewRestorePolicy.DrainHistory(dragTimes))
                {
                    restoreChecks["camera.dragTimeStamps"] = "draining owned history; remaining=" + dragTimes.Count;
                    return Receipt(false, false, "drain-owned-camera-history");
                }
                for (int i = 0; i < CameraFields.Length; i++)
                {
                    int index = i;
                    if (index == 0) { restoreChecks["camera.config"] = "same config; unchanged parameters"; continue; }
                    RestorePart("camera." + CameraFields[index].Name, () =>
                    {
                        if (index != 7) CameraFields[index].SetValue(driver, cameraValues[index]);
                        // The same owned list is now empty; do not replace the captured reference.
                    });
                }
                RestorePart("camera.pose", () => transform.SetPositionAndRotation(position, rotation));
            }
            else { restoreChecks["camera"] = "skipped active driver"; restoreError ??= "native_world_camera_still_active"; }
            VerifyFields("immediate-camera", CameraFields, driver, cameraValues);
            restoreFrame = completedFrame(); restorePhase = 2;
            return Receipt(false, false, "wait-native-pane-close");
        }
        if (completedFrame() <= restoreFrame || Pane.IsOpen || Stack.Windows.Count > 128 || Stack.Windows.Any(w => w is ImmediateWindow &&
                (w.ID == -235086 || w.ID == -1439870015))) return Receipt(false, false, "wait-native-pane-close");
        if (restorePhase == 2)
        {
            RestorePart("closed-pane-geometry", () => { Pane.windowRect = paneRect; PaneFields[1].SetValue(Pane, paneValues[1]); });
            restoreFrame = completedFrame(); restorePhase = 3;
            return Receipt(false, false, "verify-later-native-frame");
        }
        bool cameraFields = VerifyFields("later-camera", CameraFields, driver, cameraValues);
        bool selectorFields = VerifyFields("later-selector", SelectorFields, Selector, selectorValues);
        bool paneFields = VerifyFields("later-pane", PaneFields, Pane, paneValues);
        bool restored = restoreError == null && !camera.gameObject.activeSelf && !driver.isActiveAndEnabled && Renderer.wantedMode == WorldRenderMode.None && !WorldRendererUtility.WorldSelected &&
            Tabs.OpenTab == null && Find.Selector.SelectedObjectsListForReading.Count == 0 &&
            selection.Count == 0 && selectTimes.Count == 0 && dragTimes.Count == 0 && !dragBox.active && dragBox.start.Equals(dragStart) &&
            Worker.resetViewNextTime == resetView && WorldUi.everReset == everReset && Pane.windowRect.Equals(paneRect) &&
            ownedHealthTabs.All(tab => VerseGatewayCaravanInspection.DetailPawn!.GetValue(tab) == null) &&
            selectorFields && paneFields && cameraFields && transform.position.Equals(position) && transform.rotation.Equals(rotation);
        restoreChecks["later-complete-view"] = restored ? "verified" : "mismatch or earlier partial failure";
        return Receipt(true, restored, "verified", restored ? null : restoreError ?? "native_world_restore_mismatch");
    }

    private void RestorePart(string name, Action restore)
    {
        try { restore(); restoreChecks[name] = "restored"; }
        catch (Exception exception) { restoreChecks[name] = "failed: " + exception.GetBaseException().GetType().Name;
            restoreError ??= exception is GatewayAutomationException known ? known.Code : "native_world_restore_write_failed"; }
    }
    private bool VerifyFields(string phase, FieldInfo[] fields, object target, object?[] values)
    {
        bool all = true;
        for (int i = 0; i < fields.Length; i++)
        {
            bool equal = Equals(fields[i].GetValue(target), values[i]) && (fields[i].Name != "dragTimeStamps" || dragTimes.Count == 0);
            restoreChecks[phase + "." + fields[i].Name] = equal ? "verified" : "mismatch"; all &= equal;
        }
        return all;
    }
    private GatewayWorldRestoreState Receipt(bool complete, bool restored, string phase, string? error = null) =>
        new(complete, restored, phase, error ?? restoreError) { Checks = new Dictionary<string, string>(restoreChecks, StringComparer.Ordinal) };

    private static void RequireSettled()
    {
        if (!UnityData.IsInMainThread || !PlayDataLoader.Loaded || LongEventHandler.AnyEventNowOrWaiting ||
            Current.ProgramState != ProgramState.Playing || Current.Game?.World == null || Current.Root is not Root_Play ||
            Find.UIRoot is not UIRoot_Play ui || ui.screenshotMode.Active || !Current.Game.PlayerHasControl ||
            Find.CurrentMap is not { } map || map.generatorDef.renderWorld || WorldComponent_GravshipController.CutsceneInProgress)
            throw Error("native_world_context_unavailable");
    }

    private static FieldInfo[] Resolve(Type type, string[] names, Type?[] types)
    {
        var result = names.Select(n => type.GetField(n, Fields)).ToArray();
        return result.Where((f, i) => f != null && f.FieldType == types[i]).Count() == names.Length ? result.Select(f => f!).ToArray() : Array.Empty<FieldInfo>();
    }
    private static object?[] Read(FieldInfo[] fields, object target) => fields.Select(f => f.GetValue(target)).ToArray();
    private static bool Equal(FieldInfo[] fields, object target, object?[] values) => fields.Select((f, i) => Equals(f.GetValue(target), values[i])).All(v => v);
    private static void Write(FieldInfo[] fields, object target, object?[] values)
    { for (int i = 0; i < fields.Length; i++) fields[i].SetValue(target, values[i]); }
    private static GatewayAutomationException Error(string code) => VerseGatewayWorldView.Error(code);
}
#endif
