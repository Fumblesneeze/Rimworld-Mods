using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndFloatMenuActions
{
    GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step);
}

internal interface IGatewayEndToEndCurrentFloatMenuActions
{
    GatewayEndToEndStepOutcome Apply(CurrentFloatMenuActionStep step);
}

internal interface IGatewayEndToEndMapFloatMenuActions
{
    GatewayEndToEndStepOutcome Apply(MapFloatMenuOpenActionStep step);
}

internal interface IGatewayEndToEndNoPawnMapRightClickActions
{
    GatewayEndToEndStepOutcome Apply(NoPawnMapRightClickActionStep step);
}

public sealed class GatewayEndToEndGatewayBackend :
    IGatewayEndToEndActionBackend,
    IGatewayEndToEndDialogConfirmationBackend,
    IGatewayEndToEndArchitectCategoryBackend,
    IGatewayEndToEndEscapeMenuBackend,
    IGatewayEndToEndCurrentFloatMenuBackend,
    IGatewayEndToEndMapFloatMenuBackend,
    IGatewayEndToEndNoPawnMapRightClickBackend,
    IGatewayEndToEndDesignatorSessionBackend,
    IGatewayEndToEndInspectionBackend
{
    private static int nextScreenshotId;
    private readonly GatewayGameControlController gameControl;
    private readonly GatewayThingController things;
    private readonly GatewayCameraController camera;
    private readonly GatewayGizmoRegistry gizmos;
    private readonly IGatewayEndToEndFloatMenuActions floatMenus;
    private readonly IGatewayEndToEndSettlementTradeActions settlementTrade;
    private readonly IGatewayEndToEndTradeDialogActions tradeDialogs;
    private readonly GatewayWindowsInput input;
    private readonly GatewayScreenshotService screenshots;
    private readonly string artifactDirectory;

    public GatewayEndToEndGatewayBackend(
        GatewayGameControlController gameControl,
        GatewayThingController things,
        GatewayCameraController camera,
        GatewayGizmoRegistry gizmos,
        IGatewayEndToEndFloatMenuActions floatMenus,
        IGatewayEndToEndSettlementTradeActions settlementTrade,
        IGatewayEndToEndTradeDialogActions tradeDialogs,
        GatewayWindowsInput input,
        GatewayScreenshotService screenshots,
        string artifactDirectory)
    {
        this.gameControl = gameControl ?? throw new ArgumentNullException(nameof(gameControl));
        this.things = things ?? throw new ArgumentNullException(nameof(things));
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        this.gizmos = gizmos ?? throw new ArgumentNullException(nameof(gizmos));
        this.floatMenus = floatMenus ?? throw new ArgumentNullException(nameof(floatMenus));
        this.settlementTrade = settlementTrade ?? throw new ArgumentNullException(nameof(settlementTrade));
        this.tradeDialogs = tradeDialogs ?? throw new ArgumentNullException(nameof(tradeDialogs));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        this.screenshots = screenshots ?? throw new ArgumentNullException(nameof(screenshots));
        if (string.IsNullOrWhiteSpace(artifactDirectory))
        {
            throw new ArgumentException("An E2E artifact directory is required.", nameof(artifactDirectory));
        }

        this.artifactDirectory = Path.GetFullPath(artifactDirectory);
    }

    public void SetTime(bool paused, EndToEndGameSpeed speed)
    {
        gameControl.Mutate(new GatewayGameStateMutationRequest
        {
            Paused = paused,
            Speed = paused ? GatewayGameSpeed.Paused.ToString() : speed.ToString()
        });
    }

    public void SetSelection(IReadOnlyList<string> handles, bool additive)
    {
        things.MutateSelection(new GatewaySelectionRequest
        {
            Operation = additive ? "add" : "replace",
            Handles = handles.ToList()
        });
    }

    public void SetScreenshotMode(bool enabled)
    {
        if (Find.ScreenshotModeHandler is null)
        {
            throw new InvalidOperationException("RimWorld screenshot mode is unavailable without an initialized UI root.");
        }

        Find.ScreenshotModeHandler.Active = enabled;
    }

    public void SetShadowRendering(bool enabled)
    {
        if (Current.Game?.CurrentMap is null)
        {
            throw new InvalidOperationException("Shadow rendering cannot be changed without a playable current map.");
        }

        if (DebugViewSettings.drawShadows == enabled)
        {
            return;
        }

        DebugViewSettings.drawShadows = enabled;
        DebugViewSettings.drawShadowsToggled();
    }

    public GatewayEndToEndStepOutcome ApplySupportingHitPointFixture(
        SupportingHitPointFixtureActionStep step)
    {
        var map = Current.Game?.CurrentMap;
        if (map is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "supporting_fixture_map_unavailable",
                "A playable current map is required for a supporting damage fixture.");
        }

        var currentThings = map.listerThings.AllThings
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map)
            .ToArray();
        var resolved = new List<(Thing Thing, float Ratio)>();
        foreach (var target in step.Targets)
        {
            var matches = currentThings
                .Where(thing =>
                    string.Equals(thing.ThingID, target.RuntimeId, StringComparison.Ordinal) ||
                    string.Equals(thing.GetUniqueLoadID(), target.RuntimeId, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "supporting_fixture_target_missing",
                    "Every supporting damage target must resolve to exactly one spawned Thing on the current map.");
            }

            var thing = matches[0];
            if (!thing.def.useHitPoints || thing.MaxHitPoints <= 1)
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "supporting_fixture_target_has_no_hit_points",
                    "Every supporting damage target must use hit points and have a damageable maximum.");
            }

            resolved.Add((thing, target.RemainingHitPointRatio));
        }

        foreach (var target in resolved)
        {
            target.Thing.HitPoints = Math.Max(
                1,
                (int)Math.Floor(target.Thing.MaxHitPoints * target.Ratio));
        }

        return GatewayEndToEndStepOutcome.Pass(
            new Dictionary<string, string>
            {
                ["supportingFixture"] = "direct-hit-point-setup-only",
                ["targetCount"] = resolved.Count.ToString()
            });
    }

    public GatewayEndToEndCameraViewport CaptureCameraViewport()
    {
        var snapshot = camera.Capture();
        return new GatewayEndToEndCameraViewport(
            snapshot.MapHandle,
            snapshot.MapWidth,
            snapshot.MapHeight,
            Screen.width,
            Screen.height,
            snapshot.MinimumRootSize,
            snapshot.MaximumRootSize);
    }

    public GatewayEndToEndTargetBounds ResolveTarget(string runtimeId)
    {
        var summary = things.Inspect(runtimeId).Summary;
        return new GatewayEndToEndTargetBounds(summary.MapHandle, summary.OccupiedRect);
    }

    public void SetCamera(string mapHandle, GatewayMapCell center, float rootSize)
    {
        camera.Mutate(new GatewayCameraMutationRequest
        {
            MapHandle = mapHandle,
            Center = new GatewayMapCellRequest { X = center.X, Z = center.Z },
            RootSize = rootSize
        });
    }

    public IReadOnlyList<GatewayGizmoDescriptor> QueryGizmos(
        IReadOnlyList<string> targetRuntimeIds,
        IReadOnlyList<string> architectCategoryDefNames)
    {
        return gizmos.Query(GatewayGizmoQuery.ForOwners(
            targetRuntimeIds,
            architectCategoryDefNames: architectCategoryDefNames)).Items;
    }

    public GatewayGizmoInvocationResult InvokeGizmo(string handle) => gizmos.Invoke(handle);

    public GatewayInteractionApplyResult ApplyGizmo(
        string interactionHandle,
        GatewayInteractionInput interactionInput) =>
        gizmos.Apply(interactionHandle, interactionInput);

    public void CancelGizmo(string interactionHandle) => gizmos.Cancel(interactionHandle);

    void IGatewayEndToEndDesignatorSessionBackend.BeginDesignatorPreview(
        string gizmoHandle,
        EndToEndMapCell hoverCell,
        string? stuffDefName) =>
        gizmos.BeginDesignatorPreview(
            gizmoHandle,
            new GatewayMapCell(hoverCell.X, hoverCell.Z),
            stuffDefName);

    void IGatewayEndToEndDesignatorSessionBackend.RotateDesignatorPreview(
        GatewayDesignatorRotationDirection direction) =>
        gizmos.RotateDesignatorPreview(direction);

    GatewayDesignatorCommitResult IGatewayEndToEndDesignatorSessionBackend.CommitDesignatorPreview(bool keepActive) =>
        gizmos.CommitDesignatorPreview(keepActive);

    void IGatewayEndToEndDesignatorSessionBackend.CancelDesignatorPreview() =>
        gizmos.CancelDesignatorPreview();

    public GatewayEndToEndStepOutcome ApplyFloatMenu(
        FloatMenuActionStep step,
        IEndToEndContext context) => floatMenus.Apply(step);

    GatewayEndToEndStepOutcome IGatewayEndToEndCurrentFloatMenuBackend.ApplyCurrentFloatMenu(
        CurrentFloatMenuActionStep step) =>
        floatMenus is IGatewayEndToEndCurrentFloatMenuActions currentMenuActions
            ? currentMenuActions.Apply(step)
            : GatewayEndToEndStepOutcome.Fail(
                "unsupported_e2e_step",
                "The configured float-menu adapter cannot operate an already-open menu.");

    GatewayEndToEndStepOutcome IGatewayEndToEndMapFloatMenuBackend.ApplyMapFloatMenu(
        MapFloatMenuOpenActionStep step) =>
        floatMenus is IGatewayEndToEndMapFloatMenuActions mapFloatMenuActions
            ? mapFloatMenuActions.Apply(step)
            : GatewayEndToEndStepOutcome.Fail(
                "unsupported_e2e_step",
                "The configured float-menu adapter cannot open an exact map menu.");

    GatewayEndToEndStepOutcome IGatewayEndToEndNoPawnMapRightClickBackend.ApplyNoPawnMapRightClick(
        NoPawnMapRightClickActionStep step) =>
        floatMenus is IGatewayEndToEndNoPawnMapRightClickActions noPawnRightClickActions
            ? noPawnRightClickActions.Apply(step)
            : GatewayEndToEndStepOutcome.Fail(
                "unsupported_e2e_step",
                "The configured float-menu adapter cannot perform a minimized no-pawn map right-click.");

    public GatewayEndToEndStepOutcome ApplySettlementTrade(SettlementTradeActionStep step) =>
        settlementTrade.Apply(step);

    public GatewayEndToEndStepOutcome ApplyIncident(IncidentActionStep step)
    {
        var map = Current.Game?.CurrentMap;
        if (map is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "incident_map_unavailable",
                "A playable current map is required to execute an incident.");
        }

        var incident = DefDatabase<IncidentDef>.GetNamedSilentFail(step.IncidentDefName);
        if (incident is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "incident_def_missing",
                "The requested exact IncidentDef is not loaded.");
        }

        Faction? faction = null;
        if (step.FactionLoadId is { } factionLoadId)
        {
            var matches = Find.FactionManager.AllFactionsListForReading
                .Where(candidate => candidate.loadID == factionLoadId)
                .ToArray();
            if (matches.Length != 1)
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "incident_faction_missing",
                    "The requested exact faction load ID is not present exactly once.");
            }

            faction = matches[0];
        }

        try
        {
            var parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            parms.forced = true;
            parms.faction = faction;
            if (!incident.Worker.TryExecute(parms))
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "incident_rejected",
                    "RimWorld's native incident worker rejected the requested incident.");
            }

            return GatewayEndToEndStepOutcome.Pass(
                new Dictionary<string, string>
                {
                    ["incidentDef"] = incident.defName,
                    ["factionLoadId"] = faction?.loadID.ToString() ?? string.Empty
                });
        }
        catch (Exception exception)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "incident_execution_failed",
                "RimWorld's native incident worker threw " + exception.GetType().Name + ".");
        }
    }

    public GatewayEndToEndStepOutcome ApplyTradeDialog(TradeDialogActionStep step) =>
        tradeDialogs.Apply(step);

    GatewayEndToEndStepOutcome IGatewayEndToEndDialogConfirmationBackend.ApplyDialogConfirmation(
        DialogConfirmationActionStep step) =>
            VerseGatewayEndToEndDialogConfirmationActions.Apply(step);

    GatewayEndToEndStepOutcome IGatewayEndToEndArchitectCategoryBackend.ApplyArchitectCategory(
        ArchitectCategoryActionStep step) =>
        VerseGatewayEndToEndArchitectCategoryActions.Apply(
            step,
            new VerseGatewayEndToEndArchitectCategoryRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndEscapeMenuBackend.ApplyEscapeMenu(
        EscapeMenuActionStep step) =>
        VerseGatewayEndToEndEscapeMenuActions.Apply(
            step,
            new VerseGatewayEndToEndEscapeMenuRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyPawnInspectTab(
        PawnInspectTabActionStep step) =>
        VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyThingInfoCard(
        ThingInfoCardActionStep step) =>
        VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyInspectPaneClose(
        InspectPaneCloseActionStep step) =>
        VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyWindowCancel(
        WindowCancelActionStep step)
    {
        GatewayEndToEndStepOutcome outcome = VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());
        if (outcome.Passed)
        {
            FloatMenuAutomationLease.ReleaseIfNotOpen(
                Find.WindowStack?.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen) ??
                Enumerable.Empty<FloatMenu>());
        }

        return outcome;
    }

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyWindowAccept(
        WindowAcceptActionStep step) =>
        VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());

    GatewayEndToEndStepOutcome IGatewayEndToEndInspectionBackend.ApplyModSettings(
        ModSettingsActionStep step) =>
        VerseGatewayEndToEndInspectionActions.Apply(
            step,
            new VerseGatewayEndToEndInspectionRuntime());

    public IGatewayEndToEndStepOperation BeginMayMaximizeWindowInput(
        MayMaximizeWindowInputActionStep step,
        IEndToEndContext context)
    {
        var task = Task.Run(() =>
        {
            ApplyInput(step);
            return GatewayEndToEndStepOutcome.Pass();
        });
        return new GatewayEndToEndTaskStepOperation(
            task,
            "process_input_failed",
            "The process-scoped input action failed.");
    }

    public IGatewayEndToEndStepOperation BeginSaveLoad(
        SaveLoadActionStep step,
        IEndToEndContext context) =>
        new GatewayEndToEndSaveLoadStepOperation(
            new VerseGatewayEndToEndSaveLoadRuntime(),
            step.SaveName,
            context.DeferCleanup);

    public IGatewayEndToEndStepOperation BeginScreenshot(
        ScreenshotStep step,
        IEndToEndContext context)
    {
        var fileName = "e2e-screenshot-" +
                       Interlocked.Increment(ref nextScreenshotId).ToString("D6") + ".png";
        var filePath = Path.Combine(artifactDirectory, fileName);
        var request = new GatewayScreenshotRequest
        {
            ThingHandles = step.TargetRuntimeIds.ToList(),
            PaddingPixels = step.TargetRuntimeIds.Count == 0 ? null : step.PaddingPixels
        };
        var persistence = CaptureAndPersistScreenshot(request, filePath, fileName);
        return new GatewayEndToEndTaskStepOperation(
            persistence,
            "screenshot_failed",
            "The E2E screenshot could not be captured and persisted.");
    }

    private async Task<GatewayEndToEndStepOutcome> CaptureAndPersistScreenshot(
        GatewayScreenshotRequest request,
        string filePath,
        string fileName)
    {
        byte[] png;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var capture = screenshots.CaptureOperation(
                    "e2e-" + fileName + "-attempt-" + attempt,
                    request,
                    TimeSpan.FromSeconds(30));
                png = (await capture.Completion.ConfigureAwait(false)).Png;
                break;
            }
            catch (Exception exception) when (
                attempt == 1 && IsTransientScreenshotFailure(exception))
            {
                await Task.Yield();
            }
        }

        return PersistScreenshot(png, filePath, fileName);
    }

    private static bool IsTransientScreenshotFailure(Exception exception) =>
        exception is TimeoutException ||
        exception is GatewayScreenshotException screenshot &&
        screenshot.Code is "capture_busy" or "capture_failed" or "invalid_png";

    private GatewayEndToEndStepOutcome PersistScreenshot(
        byte[] png,
        string filePath,
        string fileName)
    {
        Directory.CreateDirectory(artifactDirectory);
        using (var stream = new FileStream(
                   filePath,
                   FileMode.CreateNew,
                   FileAccess.Write,
                   FileShare.Read,
                   81920,
                   FileOptions.WriteThrough))
        {
            stream.Write(png, 0, png.Length);
            stream.Flush(flushToDisk: true);
        }

        return GatewayEndToEndStepOutcome.Pass(
            new Dictionary<string, string> { ["screenshot"] = fileName });
    }

    private void ApplyInput(MayMaximizeWindowInputActionStep step)
    {
        switch (step.InputKind)
        {
            case EndToEndProcessInputKind.Click:
                input.MayMaximizeWindowClick(
                    Point(step.Start),
                    MouseButton(step.MouseButton));
                return;
            case EndToEndProcessInputKind.Drag:
                input.MayMaximizeWindowDrag(
                    Point(step.Start),
                    Point(step.End),
                    MouseButton(step.MouseButton),
                    TimeSpan.FromMilliseconds(250),
                    steps: 12);
                return;
            case EndToEndProcessInputKind.Key:
                input.MayMaximizeWindowPressKey(step.Value!);
                return;
            case EndToEndProcessInputKind.Chord:
                var parts = step.Value!.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .Where(value => value.Length > 0)
                    .ToArray();
                if (parts.Length < 2)
                {
                    throw new InvalidOperationException("An E2E chord requires a modifier and a key.");
                }

                input.MayMaximizeWindowSendChord(
                    parts.Take(parts.Length - 1).ToArray(),
                    parts[parts.Length - 1]);
                return;
            case EndToEndProcessInputKind.Text:
                input.MayMaximizeWindowSendText(step.Value ?? string.Empty);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(step.InputKind));
        }
    }

    private static GatewayClientPoint Point(EndToEndScreenPoint? point)
    {
        var value = point ?? throw new InvalidOperationException("The E2E input point is missing.");
        return new GatewayClientPoint(value.X, value.Y);
    }

    private static GatewayMouseButton MouseButton(EndToEndMouseButton? button) =>
        button switch
        {
            EndToEndMouseButton.Left => GatewayMouseButton.Left,
            EndToEndMouseButton.Right => GatewayMouseButton.Right,
            EndToEndMouseButton.Middle => GatewayMouseButton.Middle,
            _ => throw new InvalidOperationException("The E2E mouse button is missing or unsupported.")
        };
}

public sealed class VerseGatewayEndToEndFloatMenuActions :
    IGatewayEndToEndFloatMenuActions,
    IGatewayEndToEndCurrentFloatMenuActions,
    IGatewayEndToEndMapFloatMenuActions,
    IGatewayEndToEndNoPawnMapRightClickActions,
    IEndToEndFloatMenuCatalog
{
    private static readonly FieldInfo? ActionField = typeof(FloatMenuOption).GetField(
        "action",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? OptionsField = typeof(FloatMenu).GetField(
        "options",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? SelectorLowPriorityInput = typeof(Selector).GetMethod(
        "SelectorOnGUI",
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);

    public GatewayEndToEndStepOutcome Apply(MapFloatMenuOpenActionStep step)
    {
        Game? game = Current.Game;
        Map? map = Current.Game?.CurrentMap;
        WindowStack? windowStack = Find.WindowStack;
        if (!HasPlayerControlledMap(
                map is not null,
                windowStack is not null,
                GenScene.InPlayScene,
                WorldRendererUtility.DrawingMap,
                LongEventHandler.AnyEventNowOrWaiting,
                game?.PlayerHasControl == true,
                windowStack?.AnyWindowAbsorbingAllInput == true) ||
            map is null ||
            windowStack is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "map_float_menu_player_control_required",
                "A visible, unblocked, settled current map with player control is required.");
        }

        FloatMenuAutomationLease.ReleaseIfNotOpen(
            windowStack.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen));
        if (FloatMenuAutomationLease.CapturedForAutomation is not null ||
            windowStack.Windows.OfType<FloatMenu>().Any(menu => menu.IsOpen))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "map_float_menu_already_open",
                "Another native float menu or automation lease is already active.");
        }

        if (!TryResolveExactThing(map, step.TargetRuntimeId, out Thing? target))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "map_float_menu_target_invalid",
                "The exact map float-menu target is not one unique spawned current-map Thing.");
        }

        var actors = new List<Pawn>();
        foreach (string actorRuntimeId in step.ActorRuntimeIds)
        {
            if (!TryResolveExactThing(map, actorRuntimeId, out Thing? actorThing) || actorThing is not Pawn actor)
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "map_float_menu_actor_invalid",
                    "Every map float-menu actor must resolve to one unique spawned current-map pawn.");
            }

            if (IsDuplicateResolvedActor(actors, actor))
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "map_float_menu_actor_duplicate",
                    "Map float-menu actor runtime IDs must resolve to distinct spawned pawns.");
            }

            actors.Add(actor);
        }

        List<FloatMenuOption> options = FloatMenuMakerMap.GetOptions(
            actors,
            target!.DrawPos,
            out _);
        if (options.Count == 0)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "map_float_menu_empty",
                "RimWorld generated no native map float-menu options for the exact target and actors.");
        }

        FloatMenu[] before = windowStack.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen).ToArray();
        var opened = new FloatMenu(options)
        {
            givesColonistOrders = actors.Count > 0,
            vanishIfMouseDistant = false,
        };
        windowStack.Add(opened);
        FloatMenu[] after = windowStack.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen).ToArray();
        FloatMenuAutomationLease.CaptureNewlyOpened(before, after);
        if (!ReferenceEquals(FloatMenuAutomationLease.CapturedForAutomation, opened))
        {
            windowStack.TryRemove(opened, doCloseSound: false);
            FloatMenuAutomationLease.Clear();
            return GatewayEndToEndStepOutcome.Fail(
                "map_float_menu_capture_failed",
                "The exact newly opened native map float menu could not be captured.");
        }

        return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
        {
            ["targetRuntimeId"] = step.TargetRuntimeId,
            ["actorCount"] = actors.Count.ToString(),
            ["optionCount"] = options.Count.ToString(),
        });
    }

    public GatewayEndToEndStepOutcome Apply(NoPawnMapRightClickActionStep step)
    {
        Game? game = Current.Game;
        Map? map = game?.CurrentMap;
        WindowStack? windowStack = Find.WindowStack;
        Selector? selector = Find.Selector;
        if (!HasPlayerControlledMap(
                map is not null,
                windowStack is not null,
                GenScene.InPlayScene,
                WorldRendererUtility.DrawingMap,
                LongEventHandler.AnyEventNowOrWaiting,
                game?.PlayerHasControl == true,
                windowStack?.AnyWindowAbsorbingAllInput == true) ||
            selector is null ||
            map is null ||
            windowStack is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_player_control_required",
                "A visible, unblocked, settled current map with player control is required.");
        }

        FloatMenuAutomationLease.ReleaseIfNotOpen(
            windowStack.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen));
        if (FloatMenuAutomationLease.CapturedForAutomation is not null ||
            windowStack.Windows.OfType<FloatMenu>().Any(menu => menu.IsOpen))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_menu_active",
                "Another native float menu or automation lease is already active.");
        }

        if (selector.SelectedPawns.Count > 0)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_selection_active",
                "The minimized no-pawn map right-click requires zero selected pawns.");
        }

        if (!TryResolveExactThing(map, step.TargetRuntimeId, out Thing? target))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_target_invalid",
                "The exact target is not one unique spawned Thing on the current map.");
        }

        if (SelectorLowPriorityInput is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_seam_unavailable",
                "RimWorld's exact no-pawn selector GUI seam is unavailable.");
        }

        Vector2 mousePosition = target!.DrawPos.MapToUIPosition();
        Vector2 screenPoint = UI.GUIToScreenPoint(mousePosition);
        if (windowStack.GetWindowAt(screenPoint) is not null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_target_obscured",
                "An open native window covers the target-centered selector event.");
        }

        var syntheticEvent = new Event
        {
            type = EventType.MouseUp,
            button = 1,
            mousePosition = mousePosition,
        };
        Event? priorEvent = Event.current;
        try
        {
            Event.current = syntheticEvent;
            SelectorLowPriorityInput.Invoke(selector, parameters: null);
        }
        catch (TargetInvocationException exception)
        {
            Exception failure = exception.InnerException ?? exception;
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_invocation_failed",
                $"The exact selector input seam failed: {failure.GetType().Name}: {failure.Message}");
        }
        finally
        {
            Event.current = priorEvent;
        }

        if (syntheticEvent.type != EventType.Used)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "no_pawn_map_right_click_unconsumed",
                "The exact selector input seam did not consume the target-centered right-click event.");
        }

        return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
        {
            ["targetRuntimeId"] = step.TargetRuntimeId,
            ["mouseX"] = mousePosition.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ["mouseY"] = mousePosition.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ["eventConsumed"] = bool.TrueString,
            ["desktopInput"] = bool.FalseString,
        });
    }

    internal static bool HasPlayerControlledMap(
        bool hasMap,
        bool hasWindowStack,
        bool inPlayScene,
        bool drawingMap,
        bool longEventPending,
        bool playerHasControl,
        bool inputBlocked) =>
        hasMap &&
        hasWindowStack &&
        inPlayScene &&
        drawingMap &&
        !longEventPending &&
        playerHasControl &&
        !inputBlocked;

    internal static bool IsDuplicateResolvedActor(IReadOnlyList<Pawn> actors, Pawn candidate) =>
        actors.Any(actor => ReferenceEquals(actor, candidate));

    public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step)
    {
        if (!TryGetOptions(
                step.ActorRuntimeId,
                step.TargetRuntimeId,
                out var options,
                out var failureCode,
                out var failureMessage))
        {
            return GatewayEndToEndStepOutcome.Fail(failureCode, failureMessage);
        }

        var matches = options
            .Where(option => string.Equals(StableIdentity(option), step.StableOptionId, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "float_menu_option_not_found",
                "No float-menu option matched the declared stable identity.");
        }

        if (matches.Length != 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "ambiguous_float_menu_option",
                "The declared stable identity matched more than one float-menu option.");
        }

        if (matches[0].Disabled)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "float_menu_option_disabled",
                "The selected float-menu option is disabled.");
        }

        matches[0].Chosen(colonistOrdering: true, floatMenu: null);
        return GatewayEndToEndStepOutcome.Pass();
    }

    public IReadOnlyList<EndToEndFloatMenuOption> Query(string actorRuntimeId, string targetRuntimeId)
    {
        if (!TryGetOptions(
                actorRuntimeId,
                targetRuntimeId,
                out var options,
                out _,
                out var failureMessage))
        {
            throw new EndToEndContractException(failureMessage);
        }

        return ProjectOptions(options);
    }

    public GatewayEndToEndStepOutcome Apply(CurrentFloatMenuActionStep step)
    {
        WindowStack? windowStack = Find.WindowStack;
        if (windowStack == null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_missing",
                "The native window stack is unavailable.");
        }

        FloatMenu[] openMenus = windowStack.Windows
            .OfType<FloatMenu>()
            .Where(menu => menu.IsOpen)
            .ToArray();
        if (step.CaptureSoleUnownedMenu)
        {
            GatewayEndToEndStepOutcome capture = CaptureSoleUnownedMenu(openMenus, out _);
            if (!capture.Passed)
            {
                return capture;
            }
        }

        GatewayEndToEndStepOutcome menuResolution = ResolveExactCapturedMenu(
            openMenus,
            FloatMenuAutomationLease.CapturedForAutomation,
            out FloatMenu? menu);
        if (!menuResolution.Passed)
        {
            return menuResolution;
        }

        if (OptionsField?.GetValue(menu!) is not IEnumerable<FloatMenuOption> options)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_shape_changed",
                "The current RimWorld FloatMenu option shape is unavailable.");
        }

        GatewayEndToEndStepOutcome resolution = ResolveExactEnabledOption(
            options,
            step.ExactOptionLabel,
            out FloatMenuOption? option);
        if (!resolution.Passed)
        {
            return resolution;
        }

        bool tutorAllowed = RunChosenLifecycle(
            option!,
            menu!,
            () => windowStack.TryRemove(menu!, doCloseSound: true));
        if (!tutorAllowed)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_tutor_rejected",
                "RimWorld's tutorial system rejected the native FloatMenu option.");
        }

        if (menu!.IsOpen)
        {
            FloatMenuAutomationLease.Clear();
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_did_not_close",
                "The selected native FloatMenu option did not close its source menu.");
        }

        if (step.ExpectReplacementMenu)
        {
            FloatMenu[] openAfter = windowStack.Windows
                .OfType<FloatMenu>()
                .Where(candidate => candidate.IsOpen)
                .ToArray();
            GatewayEndToEndStepOutcome replacementResolution = ResolveReplacementMenu(
                menu,
                menu.IsOpen,
                openAfter,
                out FloatMenu? replacement);
            if (!replacementResolution.Passed)
            {
                FloatMenuAutomationLease.Clear();
                return replacementResolution;
            }

            FloatMenuAutomationLease.CaptureNewlyOpened(new[] { menu }, new[] { replacement! });
            if (!ReferenceEquals(FloatMenuAutomationLease.CapturedForAutomation, replacement))
            {
                FloatMenuAutomationLease.Clear();
                return GatewayEndToEndStepOutcome.Fail(
                    "replacement_float_menu_capture_failed",
                    "The exact replacement submenu could not acquire the automation lease.");
            }

            return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
            {
                ["optionLabel"] = step.ExactOptionLabel,
                ["replacementMenu"] = replacement!.GetType().FullName,
            });
        }

        FloatMenuAutomationLease.Consume(menu);
        return GatewayEndToEndStepOutcome.Pass(
            new Dictionary<string, string> { ["optionLabel"] = step.ExactOptionLabel });
    }

    internal static GatewayEndToEndStepOutcome CaptureSoleUnownedMenu(
        IEnumerable<FloatMenu> openMenus,
        out FloatMenu? captured)
    {
        captured = null;
        FloatMenu[] candidates = openMenus.Distinct().ToArray();
        FloatMenuAutomationLease.ReleaseIfNotOpen(candidates);
        if (FloatMenuAutomationLease.CapturedForAutomation is not null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_capture_owned",
                "A native FloatMenu automation lease is already active.");
        }

        if (candidates.Length != 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_capture_ambiguous",
                $"Exactly one process-opened native FloatMenu is required; observed {candidates.Length}.");
        }

        FloatMenuAutomationLease.CaptureNewlyOpened(Array.Empty<FloatMenu>(), candidates);
        if (!ReferenceEquals(FloatMenuAutomationLease.CapturedForAutomation, candidates[0]))
        {
            FloatMenuAutomationLease.Clear();
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_capture_failed",
                "The sole process-opened native FloatMenu could not acquire the automation lease.");
        }

        captured = candidates[0];
        return GatewayEndToEndStepOutcome.Pass();
    }

    internal static GatewayEndToEndStepOutcome ResolveReplacementMenu(
        FloatMenu source,
        bool sourceIsOpen,
        IEnumerable<FloatMenu> openMenus,
        out FloatMenu? replacement)
    {
        replacement = null;
        if (sourceIsOpen)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "replacement_float_menu_source_open",
                "The source native float menu remained open after choosing its option.");
        }

        FloatMenu[] candidates = openMenus
            .Where(menu => !ReferenceEquals(menu, source))
            .Distinct()
            .ToArray();
        if (candidates.Length == 0)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "replacement_float_menu_missing",
                "The chosen option did not open the required replacement submenu.");
        }

        if (candidates.Length != 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "replacement_float_menu_ambiguous",
                $"The chosen option opened {candidates.Length} replacement float menus instead of exactly one.");
        }

        replacement = candidates[0];
        return GatewayEndToEndStepOutcome.Pass();
    }

    internal static GatewayEndToEndStepOutcome ResolveExactCapturedMenu(
        IEnumerable<FloatMenu> openMenus,
        FloatMenu? captured,
        out FloatMenu? menu)
    {
        menu = null;
        if (captured is null)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_missing",
                "No native FloatMenu was captured from the preceding semantic gizmo action.");
        }

        FloatMenu[] menus = openMenus.ToArray();
        if (menus.Length > 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_ambiguous",
                "Exactly one open native FloatMenu is required.");
        }

        if (menus.Length == 0 || !ReferenceEquals(menus[0], captured))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_stale",
                "The FloatMenu captured from the preceding gizmo action is no longer the sole open menu.");
        }

        menu = captured;
        return GatewayEndToEndStepOutcome.Pass();
    }

    internal static bool RunChosenLifecycle(
        FloatMenuOption option,
        FloatMenu menu,
        Action close) =>
        RunChosenLifecycle(
            option,
            menu,
            close,
            tutorTag => TutorSystem.AllowAction((EventPack)tutorTag),
            tutorTag => TutorSystem.Notify_Event((EventPack)tutorTag));

    internal static bool RunChosenLifecycle(
        FloatMenuOption option,
        FloatMenu menu,
        Action close,
        Func<string, bool> allowTutorAction,
        Action<string> notifyTutorEvent)
    {
        string? tutorTag = option.tutorTag;
        if (tutorTag is not null && !allowTutorAction(tutorTag))
        {
            return false;
        }

        option.Chosen(GetColonistOrdering(menu), menu);
        if (tutorTag is not null)
        {
            notifyTutorEvent(tutorTag);
        }

        close();
        return true;
    }

    internal static bool GetColonistOrdering(FloatMenu menu) => menu.givesColonistOrders;

    internal static GatewayEndToEndStepOutcome ResolveExactEnabledOption(
        IEnumerable<FloatMenuOption> options,
        string exactLabel,
        out FloatMenuOption? option)
    {
        option = null;
        FloatMenuOption[] matches = options
            .Where(candidate => string.Equals(candidate.Label, exactLabel, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                matches.Length == 0
                    ? "current_float_menu_option_not_found"
                    : "current_float_menu_option_ambiguous",
                "The open FloatMenu must contain exactly one option with the requested visible label.");
        }

        if (matches[0].Disabled)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "current_float_menu_option_disabled",
                "The requested open FloatMenu option is disabled.");
        }

        option = matches[0];
        return GatewayEndToEndStepOutcome.Pass();
    }

    public static IReadOnlyList<EndToEndFloatMenuOption> ProjectOptions(
        IEnumerable<FloatMenuOption> options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return options
            .Select(option => new EndToEndFloatMenuOption(
                StableIdentity(option),
                option.Label,
                option.Disabled))
            .ToArray();
    }

    public static string StableIdentity(FloatMenuOption option)
    {
        if (option is null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        var action = ActionField?.GetValue(option) as Delegate;
        var method = action?.Method;
        var source = string.Join("|", new[]
        {
            option.GetType().FullName ?? option.GetType().Name,
            method?.DeclaringType?.FullName ?? string.Empty,
            method?.Name ?? string.Empty,
            action?.Target?.GetType().FullName ?? string.Empty,
            option.Label ?? string.Empty
        });
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        return "float-" + string.Concat(hash.Take(12).Select(value => value.ToString("x2")));
    }

    private static Thing? ResolveThing(Map map, string runtimeId)
    {
        return map.listerThings.AllThings
            .ToArray()
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map)
            .FirstOrDefault(thing =>
                string.Equals(thing.ThingID, runtimeId, StringComparison.Ordinal) ||
                string.Equals(thing.GetUniqueLoadID(), runtimeId, StringComparison.Ordinal));
    }

    private static bool TryResolveExactThing(Map map, string runtimeId, out Thing? resolved)
    {
        Thing[] matches = map.listerThings.AllThings
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map)
            .Where(thing =>
                string.Equals(thing.ThingID, runtimeId, StringComparison.Ordinal) ||
                string.Equals(thing.GetUniqueLoadID(), runtimeId, StringComparison.Ordinal))
            .ToArray();
        resolved = matches.Length == 1 ? matches[0] : null;
        return resolved is not null;
    }

    private static bool TryGetOptions(
        string actorRuntimeId,
        string targetRuntimeId,
        out IReadOnlyList<FloatMenuOption> options,
        out string failureCode,
        out string failureMessage)
    {
        options = Array.Empty<FloatMenuOption>();
        var map = Current.Game?.CurrentMap;
        if (map is null)
        {
            failureCode = "map_unavailable";
            failureMessage = "A playable map is required for a float-menu action.";
            return false;
        }

        var actor = ResolveThing(map, actorRuntimeId) as Pawn;
        if (actor is null)
        {
            failureCode = "float_menu_actor_invalid";
            failureMessage = "The float-menu actor is not a current-map pawn.";
            return false;
        }

        var target = ResolveThing(map, targetRuntimeId);
        if (target is null)
        {
            failureCode = "float_menu_target_missing";
            failureMessage = "The float-menu target is not present on the current map.";
            return false;
        }

        options = FloatMenuMakerMap.GetOptions(
            new List<Pawn> { actor },
            target.Position.ToVector3Shifted(),
            out _);
        failureCode = string.Empty;
        failureMessage = string.Empty;
        return true;
    }
}
