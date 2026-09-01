using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace RimWorldDevGateway.EndToEndTesting;

public enum EndToEndStepKind
{
    Act = 0,
    Wait = 1,
    Observe = 2
}

public enum EndToEndGizmoInteraction
{
    Invoke = 0,
    Toggle = 1,
    Place = 2,
    Drag = 3
}

public enum EndToEndDesignatorSessionAction
{
    Begin = 0,
    RotateLeft = 1,
    RotateRight = 2,
    Commit = 3,
    Cancel = 4
}

public enum EndToEndCardinalRotation
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public readonly struct EndToEndBuildMaterial
{
    public EndToEndBuildMaterial(string defName)
    {
        DefName = StepValues.Required(defName, nameof(defName));
    }

    public string DefName { get; }
}

public enum EndToEndGameSpeed
{
    Normal = 1,
    Fast = 2,
    Superfast = 3,
    Ultrafast = 4
}

public enum EndToEndMouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}

public enum EndToEndProcessInputKind
{
    Click = 0,
    Drag = 1,
    Key = 2,
    Chord = 3,
    Text = 4
}

public readonly struct EndToEndScreenPoint
{
    public EndToEndScreenPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

public readonly struct EndToEndMapCell
{
    public EndToEndMapCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }

    public int Z { get; }
}

public sealed class EndToEndDeadline
{
    public EndToEndDeadline(int maxFrames, int maxGameTicks, TimeSpan maxWallClock)
    {
        if (maxFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFrames));
        }

        if (maxGameTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGameTicks));
        }

        if (maxWallClock <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWallClock));
        }

        MaxFrames = maxFrames;
        MaxGameTicks = maxGameTicks;
        MaxWallClock = maxWallClock;
    }

    public int MaxFrames { get; }

    public int MaxGameTicks { get; }

    public TimeSpan MaxWallClock { get; }
}

public abstract class EndToEndStep
{
    protected EndToEndStep(string name, EndToEndStepKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An E2E step name is required.", nameof(name));
        }

        Name = name.Trim();
        Kind = kind;
    }

    public string Name { get; }

    public EndToEndStepKind Kind { get; }
}

public sealed class GizmoActionStep : EndToEndStep
{
    public GizmoActionStep(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        EndToEndGizmoInteraction interaction,
        string? stableGizmoId = null,
        EndToEndMapCell? startCell = null,
        EndToEndMapCell? endCell = null,
        IEnumerable<string>? architectCategoryDefNames = null,
        bool expectRejected = false)
        : this(
            name,
            targetRuntimeIds,
            gizmoType,
            interaction,
            rotation: null,
            stableGizmoId,
            startCell,
            endCell,
            architectCategoryDefNames,
            expectRejected,
            stuffDefName: null,
            useCardinalOverload: false)
    {
    }

    public GizmoActionStep(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        EndToEndGizmoInteraction interaction,
        EndToEndCardinalRotation rotation,
        string? stableGizmoId = null,
        EndToEndMapCell? startCell = null,
        EndToEndMapCell? endCell = null,
        IEnumerable<string>? architectCategoryDefNames = null,
        bool expectRejected = false)
        : this(
            name,
            targetRuntimeIds,
            gizmoType,
            interaction,
            rotation,
            stableGizmoId,
            startCell,
            endCell,
            architectCategoryDefNames,
            expectRejected,
            stuffDefName: null,
            useCardinalOverload: true)
    {
    }

    public GizmoActionStep(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        EndToEndGizmoInteraction interaction,
        EndToEndBuildMaterial material,
        string? stableGizmoId = null,
        EndToEndMapCell? startCell = null,
        EndToEndMapCell? endCell = null,
        IEnumerable<string>? architectCategoryDefNames = null,
        bool expectRejected = false)
        : this(
            name,
            targetRuntimeIds,
            gizmoType,
            interaction,
            rotation: null,
            stableGizmoId,
            startCell,
            endCell,
            architectCategoryDefNames,
            expectRejected,
            MaterialName(material, interaction),
            useCardinalOverload: false)
    {
    }

    public GizmoActionStep(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        EndToEndGizmoInteraction interaction,
        EndToEndCardinalRotation rotation,
        EndToEndBuildMaterial material,
        string? stableGizmoId = null,
        EndToEndMapCell? startCell = null,
        EndToEndMapCell? endCell = null,
        IEnumerable<string>? architectCategoryDefNames = null,
        bool expectRejected = false)
        : this(
            name,
            targetRuntimeIds,
            gizmoType,
            interaction,
            rotation,
            stableGizmoId,
            startCell,
            endCell,
            architectCategoryDefNames,
            expectRejected,
            MaterialName(material, interaction),
            useCardinalOverload: true)
    {
    }

    private GizmoActionStep(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        EndToEndGizmoInteraction interaction,
        EndToEndCardinalRotation? rotation,
        string? stableGizmoId,
        EndToEndMapCell? startCell,
        EndToEndMapCell? endCell,
        IEnumerable<string>? architectCategoryDefNames,
        bool expectRejected,
        string? stuffDefName,
        bool useCardinalOverload)
        : base(name, EndToEndStepKind.Act)
    {
        TargetRuntimeIds = StepValues.CopyIds(targetRuntimeIds, nameof(targetRuntimeIds));
        ArchitectCategoryDefNames = StepValues.CopyIds(
            architectCategoryDefNames ?? Array.Empty<string>(),
            nameof(architectCategoryDefNames));
        if (TargetRuntimeIds.Count == 0 && ArchitectCategoryDefNames.Count == 0)
        {
            throw new ArgumentException(
                "At least one target runtime ID or architect category Def name is required.",
                nameof(targetRuntimeIds));
        }

        GizmoType = Required(gizmoType, nameof(gizmoType));
        Interaction = interaction;
        StableGizmoId = string.IsNullOrWhiteSpace(stableGizmoId) ? null : stableGizmoId!.Trim();
        StartCell = startCell;
        EndCell = endCell;
        ExpectRejected = expectRejected;
        if (rotation.HasValue && !Enum.IsDefined(typeof(EndToEndCardinalRotation), rotation.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }

        if (rotation.HasValue &&
            interaction != EndToEndGizmoInteraction.Place &&
            interaction != EndToEndGizmoInteraction.Drag)
        {
            throw new ArgumentException(
                "A cardinal rotation is valid only for a native Place or Drag gizmo step.",
                nameof(rotation));
        }


        if (rotation.HasValue && interaction == EndToEndGizmoInteraction.Drag &&
            startCell.HasValue && endCell.HasValue &&
            startCell.Value.X != endCell.Value.X && startCell.Value.Z != endCell.Value.Z)
        {
            throw new ArgumentException(
                "A rotated native Drag gizmo step must describe one cardinal line.",
                nameof(rotation));
        }

        Rotation = rotation;
        StuffDefName = stuffDefName;
    }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public IReadOnlyList<string> ArchitectCategoryDefNames { get; }

    public string GizmoType { get; }

    public EndToEndGizmoInteraction Interaction { get; }

    public string? StableGizmoId { get; }

    public EndToEndMapCell? StartCell { get; }

    public EndToEndMapCell? EndCell { get; }

    public bool ExpectRejected { get; }

    public EndToEndCardinalRotation? Rotation { get; }

    public string? StuffDefName { get; }

    private static string MaterialName(
        EndToEndBuildMaterial material,
        EndToEndGizmoInteraction interaction)
    {
        if (interaction != EndToEndGizmoInteraction.Place &&
            interaction != EndToEndGizmoInteraction.Drag)
        {
            throw new ArgumentException(
                "A build material is valid only for a native Place or Drag gizmo step.",
                nameof(interaction));
        }

        return StepValues.Required(material.DefName, nameof(material));
    }

    private static string Required(string value, string parameterName) => StepValues.Required(value, parameterName);
}

public sealed class DesignatorSessionActionStep : EndToEndStep
{
    private DesignatorSessionActionStep(
        string name,
        EndToEndDesignatorSessionAction action,
        IEnumerable<string>? targetRuntimeIds = null,
        string? gizmoType = null,
        string? stableGizmoId = null,
        IEnumerable<string>? architectCategoryDefNames = null,
        EndToEndMapCell? hoverCell = null,
        string? stuffDefName = null,
        bool expectRejected = false)
        : base(name, EndToEndStepKind.Act)
    {
        Action = action;
        TargetRuntimeIds = StepValues.CopyIds(targetRuntimeIds ?? Array.Empty<string>(), nameof(targetRuntimeIds));
        ArchitectCategoryDefNames = StepValues.CopyIds(
            architectCategoryDefNames ?? Array.Empty<string>(),
            nameof(architectCategoryDefNames));
        GizmoType = gizmoType;
        StableGizmoId = string.IsNullOrWhiteSpace(stableGizmoId) ? null : stableGizmoId!.Trim();
        HoverCell = hoverCell;
        StuffDefName = string.IsNullOrWhiteSpace(stuffDefName) ? null : stuffDefName!.Trim();
        ExpectRejected = expectRejected;
    }

    public EndToEndDesignatorSessionAction Action { get; }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public IReadOnlyList<string> ArchitectCategoryDefNames { get; }

    public string? GizmoType { get; }

    public string? StableGizmoId { get; }

    public EndToEndMapCell? HoverCell { get; }

    public string? StuffDefName { get; }

    public bool ExpectRejected { get; }

    public static DesignatorSessionActionStep Begin(
        string name,
        IEnumerable<string> targetRuntimeIds,
        string gizmoType,
        string stableGizmoId,
        IEnumerable<string> architectCategoryDefNames,
        EndToEndMapCell hoverCell,
        EndToEndBuildMaterial? material = null) =>
        new(
            name,
            EndToEndDesignatorSessionAction.Begin,
            targetRuntimeIds,
            StepValues.Required(gizmoType, nameof(gizmoType)),
            StepValues.Required(stableGizmoId, nameof(stableGizmoId)),
            architectCategoryDefNames,
            hoverCell: hoverCell,
            stuffDefName: material?.DefName);

    public static DesignatorSessionActionStep RotateLeft(string name) =>
        new(name, EndToEndDesignatorSessionAction.RotateLeft);

    public static DesignatorSessionActionStep RotateRight(string name) =>
        new(name, EndToEndDesignatorSessionAction.RotateRight);

    public static DesignatorSessionActionStep Commit(string name, bool expectRejected = false) =>
        new(name, EndToEndDesignatorSessionAction.Commit, expectRejected: expectRejected);

    public static DesignatorSessionActionStep Cancel(string name) =>
        new(name, EndToEndDesignatorSessionAction.Cancel);
}

public sealed class FloatMenuActionStep : EndToEndStep
{
    public FloatMenuActionStep(string name, string actorRuntimeId, string targetRuntimeId, string stableOptionId)
        : base(name, EndToEndStepKind.Act)
    {
        ActorRuntimeId = StepValues.Required(actorRuntimeId, nameof(actorRuntimeId));
        TargetRuntimeId = StepValues.Required(targetRuntimeId, nameof(targetRuntimeId));
        StableOptionId = StepValues.Required(stableOptionId, nameof(stableOptionId));
    }

    public string ActorRuntimeId { get; }

    public string TargetRuntimeId { get; }

    public string StableOptionId { get; }
}

public sealed class MapFloatMenuOpenActionStep : EndToEndStep
{
    public const int MaximumActors = 16;

    public MapFloatMenuOpenActionStep(
        string name,
        string targetRuntimeId,
        IEnumerable<string> actorRuntimeIds)
        : base(name, EndToEndStepKind.Act)
    {
        TargetRuntimeId = StepValues.Required(targetRuntimeId, nameof(targetRuntimeId));
        ActorRuntimeIds = StepValues.CopyIds(actorRuntimeIds, nameof(actorRuntimeIds));
        if (ActorRuntimeIds.Count > MaximumActors)
        {
            throw new ArgumentException(
                $"A map float menu may use at most {MaximumActors} pawn actors.",
                nameof(actorRuntimeIds));
        }

        var unique = new HashSet<string>(ActorRuntimeIds, StringComparer.Ordinal);
        if (unique.Count != ActorRuntimeIds.Count)
        {
            throw new ArgumentException(
                "Map float-menu actor runtime IDs must be unique.",
                nameof(actorRuntimeIds));
        }
    }

    public string TargetRuntimeId { get; }

    public IReadOnlyList<string> ActorRuntimeIds { get; }
}

public sealed class NoPawnMapRightClickActionStep : EndToEndStep
{
    public NoPawnMapRightClickActionStep(string name, string targetRuntimeId)
        : base(name, EndToEndStepKind.Act)
    {
        TargetRuntimeId = StepValues.Required(targetRuntimeId, nameof(targetRuntimeId));
    }

    public string TargetRuntimeId { get; }
}

public sealed class CurrentFloatMenuActionStep : EndToEndStep
{
    public CurrentFloatMenuActionStep(
        string name,
        string exactOptionLabel,
        bool expectReplacementMenu = false,
        bool captureSoleUnownedMenu = false)
        : base(name, EndToEndStepKind.Act)
    {
        ExactOptionLabel = StepValues.Required(exactOptionLabel, nameof(exactOptionLabel));
        ExpectReplacementMenu = expectReplacementMenu;
        CaptureSoleUnownedMenu = captureSoleUnownedMenu;
    }

    public string ExactOptionLabel { get; }

    public bool ExpectReplacementMenu { get; }

    public bool CaptureSoleUnownedMenu { get; }
}

public sealed class SettlementTradeActionStep : EndToEndStep
{
    public SettlementTradeActionStep(
        string name,
        int settlementWorldObjectId,
        int caravanWorldObjectId,
        string? expectedFailureCode = null)
        : base(name, EndToEndStepKind.Act)
    {
        if (settlementWorldObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settlementWorldObjectId));
        }

        if (caravanWorldObjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(caravanWorldObjectId));
        }

        SettlementWorldObjectId = settlementWorldObjectId;
        CaravanWorldObjectId = caravanWorldObjectId;
        ExpectedFailureCode = expectedFailureCode is null
            ? null
            : StepValues.Required(expectedFailureCode, nameof(expectedFailureCode));
    }

    public int SettlementWorldObjectId { get; }

    public int CaravanWorldObjectId { get; }

    public string? ExpectedFailureCode { get; }
}

public sealed class IncidentActionStep : EndToEndStep
{
    public IncidentActionStep(
        string name,
        string incidentDefName,
        int? factionLoadId = null)
        : base(name, EndToEndStepKind.Act)
    {
        IncidentDefName = StepValues.Required(incidentDefName, nameof(incidentDefName));
        FactionLoadId = factionLoadId;
    }

    public string IncidentDefName { get; }

    public int? FactionLoadId { get; }
}

public sealed class TimeControlActionStep : EndToEndStep
{
    public TimeControlActionStep(string name, bool paused, EndToEndGameSpeed speed)
        : this(name, paused, speed, atGameTick: null, deadline: null, scheduled: false)
    {
    }

    public TimeControlActionStep(
        string name,
        bool paused,
        EndToEndGameSpeed speed,
        int atGameTick,
        EndToEndDeadline deadline)
        : this(
            name,
            paused,
            speed,
            atGameTick >= 0
                ? atGameTick
                : throw new ArgumentOutOfRangeException(nameof(atGameTick)),
            deadline ?? throw new ArgumentNullException(nameof(deadline)),
            scheduled: true)
    {
    }

    private TimeControlActionStep(
        string name,
        bool paused,
        EndToEndGameSpeed speed,
        int? atGameTick,
        EndToEndDeadline? deadline,
        bool scheduled)
        : base(name, EndToEndStepKind.Act)
    {
        if (scheduled && (!atGameTick.HasValue || deadline is null))
        {
            throw new ArgumentException("Scheduled time control requires a target game tick and deadline.");
        }

        if (!Enum.IsDefined(typeof(EndToEndGameSpeed), speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        Paused = paused;
        Speed = speed;
        AtGameTick = atGameTick;
        Deadline = deadline;
    }

    public bool Paused { get; }

    public EndToEndGameSpeed Speed { get; }

    public int? AtGameTick { get; }

    public EndToEndDeadline? Deadline { get; }
}

public sealed class SaveLoadActionStep : EndToEndStep
{
    public const int MaxSaveNameLength = 240;

    public SaveLoadActionStep(string name, string saveName)
        : base(name, EndToEndStepKind.Act)
    {
        SaveName = StepValues.SafeFileName(saveName, nameof(saveName));
    }

    public string SaveName { get; }
}

public sealed class SelectionActionStep : EndToEndStep
{
    public SelectionActionStep(string name, IEnumerable<string> targetRuntimeIds, bool additive)
        : base(name, EndToEndStepKind.Act)
    {
        TargetRuntimeIds = StepValues.CopyIds(targetRuntimeIds, nameof(targetRuntimeIds));
        Additive = additive;
    }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public bool Additive { get; }
}

public readonly struct EndToEndHitPointFixture
{
    public EndToEndHitPointFixture(string runtimeId, float remainingHitPointRatio)
    {
        RuntimeId = StepValues.Required(runtimeId, nameof(runtimeId));
        if (float.IsNaN(remainingHitPointRatio) ||
            float.IsInfinity(remainingHitPointRatio) ||
            remainingHitPointRatio <= 0f ||
            remainingHitPointRatio >= 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(remainingHitPointRatio),
                "A supporting damage fixture ratio must be finite and strictly between zero and one.");
        }

        RemainingHitPointRatio = remainingHitPointRatio;
    }

    public string RuntimeId { get; }

    public float RemainingHitPointRatio { get; }
}

public sealed class SupportingHitPointFixtureActionStep : EndToEndStep
{
    public const int MaximumTargets = 64;

    public SupportingHitPointFixtureActionStep(
        string name,
        IEnumerable<EndToEndHitPointFixture> targets)
        : base(name, EndToEndStepKind.Act)
    {
        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        var copy = new List<EndToEndHitPointFixture>();
        var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            if (target.RuntimeId is null)
            {
                throw new ArgumentException("Every supporting damage target must be initialized.", nameof(targets));
            }

            if (!runtimeIds.Add(target.RuntimeId))
            {
                throw new ArgumentException(
                    "A supporting damage fixture cannot target the same runtime ID more than once.",
                    nameof(targets));
            }

            copy.Add(target);
            if (copy.Count > MaximumTargets)
            {
                throw new ArgumentException(
                    $"A supporting damage fixture can target at most {MaximumTargets} Things.",
                    nameof(targets));
            }
        }

        if (copy.Count == 0)
        {
            throw new ArgumentException("At least one supporting damage target is required.", nameof(targets));
        }

        Targets = new ReadOnlyCollection<EndToEndHitPointFixture>(copy);
    }

    public IReadOnlyList<EndToEndHitPointFixture> Targets { get; }
}

public enum EndToEndPawnInspectTab
{
    Gear = 0,
    Needs = 1,
    Health = 2
}

public sealed class PawnInspectTabActionStep : EndToEndStep
{
    public PawnInspectTabActionStep(
        string name,
        string pawnRuntimeId,
        EndToEndPawnInspectTab tab)
        : base(name, EndToEndStepKind.Act)
    {
        if (!Enum.IsDefined(typeof(EndToEndPawnInspectTab), tab))
        {
            throw new ArgumentOutOfRangeException(nameof(tab));
        }

        PawnRuntimeId = StepValues.Required(pawnRuntimeId, nameof(pawnRuntimeId));
        Tab = tab;
    }

    public string PawnRuntimeId { get; }

    public EndToEndPawnInspectTab Tab { get; }
}

public sealed class ThingInfoCardActionStep : EndToEndStep
{
    private ThingInfoCardActionStep(string name, string thingRuntimeId, bool open)
        : base(name, EndToEndStepKind.Act)
    {
        ThingRuntimeId = StepValues.Required(thingRuntimeId, nameof(thingRuntimeId));
        IsOpenAction = open;
    }

    public string ThingRuntimeId { get; }

    public bool IsOpenAction { get; }

    public static ThingInfoCardActionStep Open(string name, string thingRuntimeId) =>
        new(name, thingRuntimeId, open: true);

    public static ThingInfoCardActionStep Close(string name, string thingRuntimeId) =>
        new(name, thingRuntimeId, open: false);
}

public sealed class InspectPaneCloseActionStep : EndToEndStep
{
    public InspectPaneCloseActionStep(
        string name,
        string selectedThingRuntimeId,
        string expectedTabRuntimeType)
        : base(name, EndToEndStepKind.Act)
    {
        SelectedThingRuntimeId = StepValues.Required(
            selectedThingRuntimeId,
            nameof(selectedThingRuntimeId));
        ExpectedTabRuntimeType = StepValues.Required(
            expectedTabRuntimeType,
            nameof(expectedTabRuntimeType));
    }

    public string SelectedThingRuntimeId { get; }

    public string ExpectedTabRuntimeType { get; }
}

public sealed class WindowCancelActionStep : EndToEndStep
{
    public WindowCancelActionStep(string name, string expectedWindowRuntimeType)
        : base(name, EndToEndStepKind.Act)
    {
        ExpectedWindowRuntimeType = StepValues.Required(
            expectedWindowRuntimeType,
            nameof(expectedWindowRuntimeType));
    }

    public string ExpectedWindowRuntimeType { get; }
}

public sealed class WindowAcceptActionStep : EndToEndStep
{
    public WindowAcceptActionStep(string name, string expectedWindowRuntimeType)
        : base(name, EndToEndStepKind.Act)
    {
        ExpectedWindowRuntimeType = StepValues.Required(
            expectedWindowRuntimeType,
            nameof(expectedWindowRuntimeType));
    }

    public string ExpectedWindowRuntimeType { get; }
}

public sealed class ModSettingsActionStep : EndToEndStep
{
    public ModSettingsActionStep(string name, string packageId)
        : base(name, EndToEndStepKind.Act)
    {
        PackageId = StepValues.Required(packageId, nameof(packageId));
    }

    public string PackageId { get; }
}

public sealed class ArchitectCategoryActionStep : EndToEndStep
{
    public ArchitectCategoryActionStep(string name, string categoryDefName, bool open)
        : base(name, EndToEndStepKind.Act)
    {
        CategoryDefName = StepValues.Required(categoryDefName, nameof(categoryDefName));
        Open = open;
    }

    public string CategoryDefName { get; }

    public bool Open { get; }
}

public sealed class EscapeMenuActionStep : EndToEndStep
{
    public EscapeMenuActionStep(string name, bool open)
        : base(name, EndToEndStepKind.Act) => Open = open;

    public bool Open { get; }
}

public sealed class CameraActionStep : EndToEndStep
{
    public CameraActionStep(string name, IEnumerable<string> targetRuntimeIds, int paddingPixels)
        : base(name, EndToEndStepKind.Act)
    {
        if (paddingPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paddingPixels));
        }

        TargetRuntimeIds = StepValues.CopyIds(targetRuntimeIds, nameof(targetRuntimeIds));
        if (TargetRuntimeIds.Count == 0)
        {
            throw new ArgumentException("At least one camera target runtime ID is required.", nameof(targetRuntimeIds));
        }

        PaddingPixels = paddingPixels;
    }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public int PaddingPixels { get; }
}

public sealed class ScreenshotModeActionStep : EndToEndStep
{
    public ScreenshotModeActionStep(string name, bool enabled)
        : base(name, EndToEndStepKind.Act) => Enabled = enabled;

    public bool Enabled { get; }
}

public sealed class ShadowRenderingActionStep : EndToEndStep
{
    public ShadowRenderingActionStep(string name, bool enabled)
        : base(name, EndToEndStepKind.Act) => Enabled = enabled;

    public bool Enabled { get; }
}

public sealed class MayMaximizeWindowInputActionStep : EndToEndStep
{
    private MayMaximizeWindowInputActionStep(
        string name,
        EndToEndProcessInputKind inputKind,
        EndToEndScreenPoint? start,
        EndToEndScreenPoint? end,
        EndToEndMouseButton? mouseButton,
        string? value)
        : base(name, EndToEndStepKind.Act)
    {
        InputKind = inputKind;
        Start = start;
        End = end;
        MouseButton = mouseButton;
        Value = value;
    }

    public EndToEndProcessInputKind InputKind { get; }

    public EndToEndScreenPoint? Start { get; }

    public EndToEndScreenPoint? End { get; }

    public EndToEndMouseButton? MouseButton { get; }

    public string? Value { get; }

    public static MayMaximizeWindowInputActionStep Click(
        string name,
        EndToEndScreenPoint point,
        EndToEndMouseButton mouseButton)
    {
        return new MayMaximizeWindowInputActionStep(
            name,
            EndToEndProcessInputKind.Click,
            point,
            null,
            mouseButton,
            null);
    }

    public static MayMaximizeWindowInputActionStep Drag(
        string name,
        EndToEndScreenPoint start,
        EndToEndScreenPoint end,
        EndToEndMouseButton mouseButton)
    {
        return new MayMaximizeWindowInputActionStep(
            name,
            EndToEndProcessInputKind.Drag,
            start,
            end,
            mouseButton,
            null);
    }

    public static MayMaximizeWindowInputActionStep Key(string name, string key)
    {
        return new MayMaximizeWindowInputActionStep(
            name,
            EndToEndProcessInputKind.Key,
            null,
            null,
            null,
            StepValues.Required(key, nameof(key)));
    }

    public static MayMaximizeWindowInputActionStep Chord(string name, string chord)
    {
        return new MayMaximizeWindowInputActionStep(
            name,
            EndToEndProcessInputKind.Chord,
            null,
            null,
            null,
            StepValues.Required(chord, nameof(chord)));
    }

    public static MayMaximizeWindowInputActionStep Text(string name, string text)
    {
        return new MayMaximizeWindowInputActionStep(
            name,
            EndToEndProcessInputKind.Text,
            null,
            null,
            null,
            text ?? string.Empty);
    }
}

public enum EndToEndTradeDialogAction
{
    AdjustTransfer = 0,
    Accept = 1
}

public sealed class TradeDialogActionStep : EndToEndStep
{
    private TradeDialogActionStep(
        string name,
        EndToEndTradeDialogAction action,
        string? thingRuntimeId,
        int countDelta)
        : base(name, EndToEndStepKind.Act)
    {
        Action = action;
        ThingRuntimeId = thingRuntimeId;
        CountDelta = countDelta;
    }

    public EndToEndTradeDialogAction Action { get; }

    public string? ThingRuntimeId { get; }

    public int CountDelta { get; }

    public static TradeDialogActionStep AdjustTransfer(
        string name,
        string thingRuntimeId,
        int countDelta)
    {
        if (countDelta == 0 || countDelta is < -10_000 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(countDelta));
        }

        return new TradeDialogActionStep(
            name,
            EndToEndTradeDialogAction.AdjustTransfer,
            StepValues.Required(thingRuntimeId, nameof(thingRuntimeId)),
            countDelta);
    }

    public static TradeDialogActionStep Accept(string name) =>
        new(name, EndToEndTradeDialogAction.Accept, null, 0);
}

public sealed class DialogConfirmationActionStep : EndToEndStep
{
    public DialogConfirmationActionStep(
        string name,
        string expectedWindowTypeName)
        : base(name, EndToEndStepKind.Act)
    {
        ExpectedWindowTypeName = StepValues.Required(
            expectedWindowTypeName,
            nameof(expectedWindowTypeName));
    }

    public string ExpectedWindowTypeName { get; }
}

public sealed class WaitUntilStep : EndToEndStep
{
    public WaitUntilStep(string name, Func<IEndToEndContext, bool> predicate, EndToEndDeadline deadline)
        : base(name, EndToEndStepKind.Wait)
    {
        Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Deadline = deadline ?? throw new ArgumentNullException(nameof(deadline));
    }

    public Func<IEndToEndContext, bool> Predicate { get; }

    public EndToEndDeadline Deadline { get; }
}

public sealed class AssertionStep : EndToEndStep
{
    public AssertionStep(string name, Action<IEndToEndContext> assertion)
        : base(name, EndToEndStepKind.Observe)
    {
        Assertion = assertion ?? throw new ArgumentNullException(nameof(assertion));
    }

    public Action<IEndToEndContext> Assertion { get; }
}

public sealed class ScreenshotStep : EndToEndStep
{
    public ScreenshotStep(string name, IEnumerable<string> targetRuntimeIds, int paddingPixels)
        : base(name, EndToEndStepKind.Observe)
    {
        if (paddingPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paddingPixels));
        }

        TargetRuntimeIds = StepValues.CopyIds(targetRuntimeIds, nameof(targetRuntimeIds));
        PaddingPixels = paddingPixels;
    }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public int PaddingPixels { get; }
}

public sealed class CheckpointStep : EndToEndStep
{
    public const int MaximumArtifactFields = 256;
    public const int MaximumArtifactKeyUtf8Bytes = 1024;
    public const int MaximumArtifactValueUtf8Bytes = 1024 * 1024;
    public const int MaximumArtifactAggregateUtf8Bytes = 16 * 1024 * 1024;

    public CheckpointStep(string name, Func<IEndToEndContext, IReadOnlyDictionary<string, string>> capture)
        : base(name, EndToEndStepKind.Observe)
    {
        if (capture is null) throw new ArgumentNullException(nameof(capture));
        Capture = context => CopyBounded(capture(context));
    }

    public Func<IEndToEndContext, IReadOnlyDictionary<string, string>> Capture { get; }

    private static IReadOnlyDictionary<string, string> CopyBounded(
        IReadOnlyDictionary<string, string>? source)
    {
        if (source is null) throw new InvalidOperationException("The E2E checkpoint returned null.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var aggregateBytes = 0L;
        foreach (var pair in source)
        {
            if (result.Count == MaximumArtifactFields)
                throw new InvalidOperationException(
                    $"An E2E checkpoint may contain at most {MaximumArtifactFields} artifact fields.");
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
                throw new InvalidOperationException("E2E checkpoint artifact keys and values must be non-null and keys must be nonblank.");
            var keyBytes = Encoding.UTF8.GetByteCount(pair.Key);
            var valueBytes = Encoding.UTF8.GetByteCount(pair.Value);
            if (keyBytes > MaximumArtifactKeyUtf8Bytes)
                throw new InvalidOperationException(
                    $"An E2E checkpoint artifact key exceeds {MaximumArtifactKeyUtf8Bytes} UTF-8 bytes.");
            if (valueBytes > MaximumArtifactValueUtf8Bytes)
                throw new InvalidOperationException(
                    $"An E2E checkpoint artifact value exceeds {MaximumArtifactValueUtf8Bytes} UTF-8 bytes.");
            aggregateBytes += keyBytes + valueBytes;
            if (aggregateBytes > MaximumArtifactAggregateUtf8Bytes)
                throw new InvalidOperationException(
                    $"E2E checkpoint artifacts exceed {MaximumArtifactAggregateUtf8Bytes} aggregate UTF-8 bytes.");
            if (result.ContainsKey(pair.Key))
                throw new InvalidOperationException("E2E checkpoint artifact keys must be unique.");
            result.Add(pair.Key, pair.Value);
        }
        return result;
    }
}

internal static class StepValues
{
    // Windows limits a single path component to 255 UTF-16 code units. Leave
    // room for RimWorld's .rws and recovery suffixes rather than relying on a
    // later filesystem exception from the Unity main thread.
    private static readonly HashSet<string> ReservedWindowsDeviceNames = new(
        new[]
        {
            "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "COM¹", "COM²", "COM³",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "LPT¹", "LPT²", "LPT³"
        },
        StringComparer.OrdinalIgnoreCase);

    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }

    public static IReadOnlyList<string> CopyIds(IEnumerable<string> values, string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = new List<string>();
        foreach (var value in values)
        {
            copy.Add(Required(value, parameterName));
        }

        return new ReadOnlyCollection<string>(copy);
    }

    public static string SafeFileName(string value, string parameterName)
    {
        var candidate = Required(value, parameterName);
        var deviceBaseName = candidate.Split('.')[0];
        if (!string.Equals(value, candidate, StringComparison.Ordinal) ||
            candidate.Length > SaveLoadActionStep.MaxSaveNameLength ||
            candidate is "." or ".." ||
            candidate.EndsWith(".", StringComparison.Ordinal) ||
            candidate.EndsWith(" ", StringComparison.Ordinal) ||
            ReservedWindowsDeviceNames.Contains(deviceBaseName) ||
            candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !string.Equals(Path.GetFileName(candidate), candidate, StringComparison.Ordinal))
        {
            throw new ArgumentException("A safe leaf save name is required.", parameterName);
        }

        return candidate;
    }
}
