using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndCameraViewport
{
    public GatewayEndToEndCameraViewport(
        string mapHandle,
        int mapWidth,
        int mapHeight,
        int screenWidth,
        int screenHeight,
        float minimumRootSize,
        float maximumRootSize)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        MapWidth = mapWidth;
        MapHeight = mapHeight;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        MinimumRootSize = minimumRootSize;
        MaximumRootSize = maximumRootSize;
    }

    public string MapHandle { get; }

    public int MapWidth { get; }

    public int MapHeight { get; }

    public int ScreenWidth { get; }

    public int ScreenHeight { get; }

    public float MinimumRootSize { get; }

    public float MaximumRootSize { get; }
}

public sealed class GatewayEndToEndTargetBounds
{
    public GatewayEndToEndTargetBounds(string mapHandle, GatewayMapRect occupiedRect)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        OccupiedRect = occupiedRect ?? throw new ArgumentNullException(nameof(occupiedRect));
    }

    public string MapHandle { get; }

    public GatewayMapRect OccupiedRect { get; }
}

public interface IGatewayEndToEndActionBackend
{
    void SetTime(bool paused, EndToEndGameSpeed speed);

    void SetSelection(IReadOnlyList<string> handles, bool additive);

    GatewayEndToEndCameraViewport CaptureCameraViewport();

    GatewayEndToEndTargetBounds ResolveTarget(string runtimeId);

    void SetCamera(string mapHandle, GatewayMapCell center, float rootSize);

    IReadOnlyList<GatewayGizmoDescriptor> QueryGizmos(
        IReadOnlyList<string> targetRuntimeIds,
        IReadOnlyList<string> architectCategoryDefNames);

    GatewayGizmoInvocationResult InvokeGizmo(string handle);

    GatewayInteractionApplyResult ApplyGizmo(
        string interactionHandle,
        GatewayInteractionInput input);

    void CancelGizmo(string interactionHandle);

    GatewayEndToEndStepOutcome ApplyFloatMenu(
        FloatMenuActionStep step,
        IEndToEndContext context);

    GatewayEndToEndStepOutcome ApplySettlementTrade(SettlementTradeActionStep step);

    GatewayEndToEndStepOutcome ApplyIncident(IncidentActionStep step);

    GatewayEndToEndStepOutcome ApplyTradeDialog(TradeDialogActionStep step);

    IGatewayEndToEndStepOperation BeginInput(
        ProcessInputActionStep step,
        IEndToEndContext context);

    IGatewayEndToEndStepOperation BeginSaveLoad(
        SaveLoadActionStep step,
        IEndToEndContext context);

    IGatewayEndToEndStepOperation BeginScreenshot(
        ScreenshotStep step,
        IEndToEndContext context);
}

internal interface IGatewayEndToEndDialogConfirmationBackend
{
    GatewayEndToEndStepOutcome ApplyDialogConfirmation(DialogConfirmationActionStep step);
}

internal interface IGatewayEndToEndArchitectCategoryBackend
{
    GatewayEndToEndStepOutcome ApplyArchitectCategory(ArchitectCategoryActionStep step);
}

public sealed class GatewayEndToEndNativeActions :
    IGatewayEndToEndNativeActions,
    IGatewayEndToEndDialogConfirmationNativeActions,
    IGatewayEndToEndArchitectCategoryNativeActions
{
    private readonly IGatewayEndToEndActionBackend backend;

    public GatewayEndToEndNativeActions(IGatewayEndToEndActionBackend backend) =>
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));

    public GatewayEndToEndStepOutcome Apply(TimeControlActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        backend.SetTime(step.Paused, step.Speed);
        return GatewayEndToEndStepOutcome.Pass();
    }

    public GatewayEndToEndStepOutcome Apply(SelectionActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        backend.SetSelection(step.TargetRuntimeIds, step.Additive);
        return GatewayEndToEndStepOutcome.Pass();
    }

    public GatewayEndToEndStepOutcome Apply(CameraActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        var viewport = backend.CaptureCameraViewport();
        if (viewport.ScreenWidth <= 0 || viewport.ScreenHeight <= 0)
        {
            return Fail("camera_view_unavailable", "The rendered game viewport has no usable size.");
        }

        var bounds = step.TargetRuntimeIds.Select(backend.ResolveTarget).ToArray();
        if (bounds.Any(target =>
                !string.Equals(target.MapHandle, viewport.MapHandle, StringComparison.Ordinal)))
        {
            return Fail(
                "camera_targets_cross_maps",
                "Every camera target must be present on the current map.");
        }

        var minX = bounds.Min(target => target.OccupiedRect.MinX);
        var minZ = bounds.Min(target => target.OccupiedRect.MinZ);
        var maxX = bounds.Max(target => target.OccupiedRect.MaxX);
        var maxZ = bounds.Max(target => target.OccupiedRect.MaxZ);
        var horizontalUsable = 1f - ((2f * step.PaddingPixels) / viewport.ScreenWidth);
        var verticalUsable = 1f - ((2f * step.PaddingPixels) / viewport.ScreenHeight);
        if (horizontalUsable <= 0f || verticalUsable <= 0f)
        {
            return Fail(
                "camera_padding_too_large",
                "Camera padding must leave a visible viewport interior.");
        }

        var aspectRatio = (float)viewport.ScreenWidth / viewport.ScreenHeight;
        var halfWidth = (maxX - minX + 1) / 2f;
        var halfHeight = (maxZ - minZ + 1) / 2f;
        var requiredRootSize = Math.Max(
            halfHeight / verticalUsable,
            halfWidth / (aspectRatio * horizontalUsable));
        if (requiredRootSize > viewport.MaximumRootSize)
        {
            return Fail(
                "camera_targets_too_large",
                "The camera cannot contain every target at its maximum zoom-out level.");
        }

        var rootSize = Math.Max(viewport.MinimumRootSize, requiredRootSize);
        var center = new GatewayMapCell(
            Clamp((minX + maxX) / 2, 0, viewport.MapWidth - 1),
            Clamp((minZ + maxZ) / 2, 0, viewport.MapHeight - 1));
        backend.SetCamera(viewport.MapHandle, center, rootSize);
        return GatewayEndToEndStepOutcome.Pass();
    }

    public GatewayEndToEndStepOutcome Apply(GizmoActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        var matches = backend.QueryGizmos(
                step.TargetRuntimeIds,
                step.ArchitectCategoryDefNames)
            .Where(item => string.Equals(item.RuntimeType, step.GizmoType, StringComparison.Ordinal))
            .Where(item => step.StableGizmoId is null ||
                           string.Equals(item.Identity, step.StableGizmoId, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0)
        {
            return Fail("gizmo_not_found", "No gizmo matched the declared stable selector.");
        }

        if (matches.Length != 1)
        {
            return Fail("ambiguous_gizmo", "The declared gizmo selector matched more than one action.");
        }

        var gizmo = matches[0];
        var expectedKind = ExpectedGizmoKind(step.Interaction);
        if (gizmo.InteractionKind != expectedKind)
        {
            return Fail(
                "gizmo_interaction_mismatch",
                "The selected gizmo does not expose the declared interaction kind.");
        }

        var invocation = backend.InvokeGizmo(gizmo.Handle);
        if (step.Interaction is EndToEndGizmoInteraction.Invoke or EndToEndGizmoInteraction.Toggle)
        {
            return invocation.Completed
                ? GatewayEndToEndStepOutcome.Pass()
                : Fail("gizmo_not_completed", "The direct gizmo action did not complete.");
        }

        if (invocation.Interaction is null)
        {
            return Fail("gizmo_interaction_missing", "The gizmo did not start a semantic interaction.");
        }

        var interactionHandle = invocation.Interaction.Handle;
        var completed = false;
        try
        {
            var input = CreateInteractionInput(step, invocation.Interaction);
            var result = backend.ApplyGizmo(interactionHandle, input);
            completed = result.Completed;
            if (step.ExpectRejected)
            {
                return !result.Completed && result.Accepted.Count == 0 && result.Rejected.Count > 0
                    ? GatewayEndToEndStepOutcome.Pass()
                    : Fail(
                        "gizmo_target_not_rejected",
                        "The semantic gizmo target was expected to be rejected by native preflight.");
            }

            if (completed)
            {
                return GatewayEndToEndStepOutcome.Pass();
            }

            if (result.Accepted.Count == 0 && result.Rejected.Count > 0)
            {
                var reasons = string.Join(
                    "; ",
                    result.Rejected
                        .Select(item => item.Reason)
                        .Where(reason => !string.IsNullOrWhiteSpace(reason))
                        .Distinct(StringComparer.Ordinal)
                        .Take(4));
                return Fail(
                    "gizmo_target_rejected",
                    string.IsNullOrWhiteSpace(reasons)
                        ? "RimWorld's native gizmo preflight rejected the semantic target."
                        : "RimWorld's native gizmo preflight rejected the semantic target: " + reasons);
            }

            return Fail("gizmo_not_completed", "The semantic gizmo interaction did not complete.");
        }
        finally
        {
            if (!completed)
            {
                backend.CancelGizmo(interactionHandle);
            }
        }
    }

    public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        return backend.ApplyFloatMenu(step, context) ??
               throw new InvalidOperationException("The float-menu backend returned no outcome.");
    }

    public GatewayEndToEndStepOutcome Apply(
        SettlementTradeActionStep step,
        IEndToEndContext context)
    {
        Require(step, context);
        return backend.ApplySettlementTrade(step) ??
               throw new InvalidOperationException("The settlement-trade backend returned no outcome.");
    }

    public GatewayEndToEndStepOutcome Apply(
        IncidentActionStep step,
        IEndToEndContext context)
    {
        Require(step, context);
        return backend.ApplyIncident(step) ??
               throw new InvalidOperationException("The incident backend returned no outcome.");
    }

    public GatewayEndToEndStepOutcome Apply(
        TradeDialogActionStep step,
        IEndToEndContext context)
    {
        Require(step, context);
        return backend.ApplyTradeDialog(step) ??
               throw new InvalidOperationException("The trade-dialog backend returned no outcome.");
    }

    GatewayEndToEndStepOutcome IGatewayEndToEndDialogConfirmationNativeActions.Apply(
        DialogConfirmationActionStep step,
        IEndToEndContext context)
    {
        Require(step, context);
        return backend is IGatewayEndToEndDialogConfirmationBackend dialogBackend
            ? dialogBackend.ApplyDialogConfirmation(step) ??
              throw new InvalidOperationException("The dialog-confirmation backend returned no outcome.")
            : Fail(
                "unsupported_e2e_step",
                "The configured E2E backend does not support dialog confirmation.");
    }

    GatewayEndToEndStepOutcome IGatewayEndToEndArchitectCategoryNativeActions.Apply(
        ArchitectCategoryActionStep step,
        IEndToEndContext context)
    {
        Require(step, context);
        return backend is IGatewayEndToEndArchitectCategoryBackend architectBackend
            ? architectBackend.ApplyArchitectCategory(step) ??
              throw new InvalidOperationException("The Architect-category backend returned no outcome.")
            : Fail(
                "unsupported_e2e_step",
                "The configured E2E backend does not support Architect-category actions.");
    }

    public IGatewayEndToEndStepOperation Begin(ProcessInputActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        return backend.BeginInput(step, context) ??
               throw new InvalidOperationException("The input backend returned no operation.");
    }

    public IGatewayEndToEndStepOperation Begin(SaveLoadActionStep step, IEndToEndContext context)
    {
        Require(step, context);
        return backend.BeginSaveLoad(step, context) ??
               throw new InvalidOperationException("The save/load backend returned no operation.");
    }

    public IGatewayEndToEndStepOperation Begin(ScreenshotStep step, IEndToEndContext context)
    {
        Require(step, context);
        return backend.BeginScreenshot(step, context) ??
               throw new InvalidOperationException("The screenshot backend returned no operation.");
    }

    private static GatewayInteractionInput CreateInteractionInput(
        GizmoActionStep step,
        GatewayInteractionDescriptor interaction)
    {
        var start = step.StartCell ?? throw new InvalidOperationException(
            "A placement or drag E2E step requires a start cell.");
        var first = new GatewayMapCell(start.X, start.Z);
        if (step.Interaction == EndToEndGizmoInteraction.Place)
        {
            if (!interaction.AcceptedInputs.Contains(GatewayInteractionInputKind.Cell))
            {
                throw new InvalidOperationException("The placement gizmo does not accept cell input.");
            }

            return GatewayInteractionInput.ForCell(
                first,
                step.Rotation.HasValue
                    ? (GatewayCardinalRotation)(int)step.Rotation.Value
                    : null);
        }

        var end = step.EndCell ?? throw new InvalidOperationException(
            "A drag E2E step requires an end cell.");
        var last = new GatewayMapCell(end.X, end.Z);
        if (interaction.AcceptedInputs.Contains(GatewayInteractionInputKind.Rectangle))
        {
            return GatewayInteractionInput.ForRectangle(first, last);
        }

        if (interaction.AcceptedInputs.Contains(GatewayInteractionInputKind.Line))
        {
            return GatewayInteractionInput.ForLine(first, last);
        }

        throw new InvalidOperationException("The drag gizmo does not accept line or rectangle input.");
    }

    private static GatewayGizmoInteractionKind ExpectedGizmoKind(EndToEndGizmoInteraction interaction) =>
        interaction switch
        {
            EndToEndGizmoInteraction.Invoke => GatewayGizmoInteractionKind.Immediate,
            EndToEndGizmoInteraction.Toggle => GatewayGizmoInteractionKind.Toggle,
            EndToEndGizmoInteraction.Place => GatewayGizmoInteractionKind.Placement,
            EndToEndGizmoInteraction.Drag => GatewayGizmoInteractionKind.Drag,
            _ => throw new ArgumentOutOfRangeException(nameof(interaction))
        };

    private static void Require(object step, IEndToEndContext context)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }
    }

    private static GatewayEndToEndStepOutcome Fail(string code, string message) =>
        GatewayEndToEndStepOutcome.Fail(code, message);

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));
}
