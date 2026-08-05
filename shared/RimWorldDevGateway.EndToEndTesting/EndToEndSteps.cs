using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        IEnumerable<string>? architectCategoryDefNames = null)
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
    }

    public IReadOnlyList<string> TargetRuntimeIds { get; }

    public IReadOnlyList<string> ArchitectCategoryDefNames { get; }

    public string GizmoType { get; }

    public EndToEndGizmoInteraction Interaction { get; }

    public string? StableGizmoId { get; }

    public EndToEndMapCell? StartCell { get; }

    public EndToEndMapCell? EndCell { get; }

    private static string Required(string value, string parameterName) => StepValues.Required(value, parameterName);
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

public sealed class TimeControlActionStep : EndToEndStep
{
    public TimeControlActionStep(string name, bool paused, EndToEndGameSpeed speed)
        : base(name, EndToEndStepKind.Act)
    {
        if (!Enum.IsDefined(typeof(EndToEndGameSpeed), speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        Paused = paused;
        Speed = speed;
    }

    public bool Paused { get; }

    public EndToEndGameSpeed Speed { get; }
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

public sealed class ProcessInputActionStep : EndToEndStep
{
    private ProcessInputActionStep(
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

    public static ProcessInputActionStep Click(
        string name,
        EndToEndScreenPoint point,
        EndToEndMouseButton mouseButton)
    {
        return new ProcessInputActionStep(name, EndToEndProcessInputKind.Click, point, null, mouseButton, null);
    }

    public static ProcessInputActionStep Drag(
        string name,
        EndToEndScreenPoint start,
        EndToEndScreenPoint end,
        EndToEndMouseButton mouseButton)
    {
        return new ProcessInputActionStep(name, EndToEndProcessInputKind.Drag, start, end, mouseButton, null);
    }

    public static ProcessInputActionStep Key(string name, string key)
    {
        return new ProcessInputActionStep(
            name,
            EndToEndProcessInputKind.Key,
            null,
            null,
            null,
            StepValues.Required(key, nameof(key)));
    }

    public static ProcessInputActionStep Chord(string name, string chord)
    {
        return new ProcessInputActionStep(
            name,
            EndToEndProcessInputKind.Chord,
            null,
            null,
            null,
            StepValues.Required(chord, nameof(chord)));
    }

    public static ProcessInputActionStep Text(string name, string text)
    {
        return new ProcessInputActionStep(name, EndToEndProcessInputKind.Text, null, null, null, text ?? string.Empty);
    }
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
    public CheckpointStep(string name, Func<IEndToEndContext, IReadOnlyDictionary<string, string>> capture)
        : base(name, EndToEndStepKind.Observe)
    {
        Capture = capture ?? throw new ArgumentNullException(nameof(capture));
    }

    public Func<IEndToEndContext, IReadOnlyDictionary<string, string>> Capture { get; }
}

internal static class StepValues
{
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
}
