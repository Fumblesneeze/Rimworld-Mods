using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorld;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndFloatMenuActions
{
    GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step);
}

public sealed class GatewayEndToEndGatewayBackend : IGatewayEndToEndActionBackend
{
    private static int nextScreenshotId;
    private readonly GatewayGameControlController gameControl;
    private readonly GatewayThingController things;
    private readonly GatewayCameraController camera;
    private readonly GatewayGizmoRegistry gizmos;
    private readonly IGatewayEndToEndFloatMenuActions floatMenus;
    private readonly GatewayWindowsInput input;
    private readonly GatewayScreenshotService screenshots;
    private readonly string artifactDirectory;

    public GatewayEndToEndGatewayBackend(
        GatewayGameControlController gameControl,
        GatewayThingController things,
        GatewayCameraController camera,
        GatewayGizmoRegistry gizmos,
        IGatewayEndToEndFloatMenuActions floatMenus,
        GatewayWindowsInput input,
        GatewayScreenshotService screenshots,
        string artifactDirectory)
    {
        this.gameControl = gameControl ?? throw new ArgumentNullException(nameof(gameControl));
        this.things = things ?? throw new ArgumentNullException(nameof(things));
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        this.gizmos = gizmos ?? throw new ArgumentNullException(nameof(gizmos));
        this.floatMenus = floatMenus ?? throw new ArgumentNullException(nameof(floatMenus));
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

    public GatewayEndToEndStepOutcome ApplyFloatMenu(
        FloatMenuActionStep step,
        IEndToEndContext context) => floatMenus.Apply(step);

    public IGatewayEndToEndStepOperation BeginInput(
        ProcessInputActionStep step,
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
        var capture = screenshots.CaptureOperation(
            "e2e-" + fileName,
            request,
            TimeSpan.FromSeconds(30));
        var persistence = capture.Completion.ContinueWith(
            completed => PersistScreenshot(completed.GetAwaiter().GetResult(), filePath, fileName),
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        return new GatewayEndToEndTaskStepOperation(
            persistence,
            "screenshot_failed",
            "The E2E screenshot could not be captured and persisted.");
    }

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

    private void ApplyInput(ProcessInputActionStep step)
    {
        switch (step.InputKind)
        {
            case EndToEndProcessInputKind.Click:
                input.Click(Point(step.Start), MouseButton(step.MouseButton));
                return;
            case EndToEndProcessInputKind.Drag:
                input.Drag(
                    Point(step.Start),
                    Point(step.End),
                    MouseButton(step.MouseButton),
                    TimeSpan.FromMilliseconds(250),
                    steps: 12);
                return;
            case EndToEndProcessInputKind.Key:
                input.PressKey(step.Value!);
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

                input.SendChord(parts.Take(parts.Length - 1).ToArray(), parts[parts.Length - 1]);
                return;
            case EndToEndProcessInputKind.Text:
                input.SendText(step.Value ?? string.Empty);
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
    IEndToEndFloatMenuCatalog
{
    private static readonly FieldInfo? ActionField = typeof(FloatMenuOption).GetField(
        "action",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
