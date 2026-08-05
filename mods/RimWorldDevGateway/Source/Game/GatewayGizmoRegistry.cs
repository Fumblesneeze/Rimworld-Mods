using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway;

public sealed class GatewayGizmoException : Exception
{
    public GatewayGizmoException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

public enum GatewayGizmoOwnerScope
{
    Selection,
    ExplicitOwners
}

public enum GatewayGizmoSource
{
    Selection,
    ExplicitOwner,
    Architect
}

public enum GatewayGizmoInteractionKind
{
    Immediate,
    Toggle,
    Target,
    Placement,
    Drag,
    Unsupported
}

public enum GatewayInteractionInputKind
{
    Thing,
    Cell,
    Cells,
    Line,
    Rectangle
}

public enum GatewayInteractionTargetKind
{
    Thing,
    Cell
}

public sealed class GatewayGizmoQuery
{
    private GatewayGizmoQuery(
        GatewayGizmoOwnerScope ownerScope,
        IReadOnlyList<string> ownerHandles,
        IReadOnlyList<string> architectCategoryDefNames,
        int limit)
    {
        OwnerScope = ownerScope;
        OwnerHandles = ownerHandles;
        ArchitectCategoryDefNames = architectCategoryDefNames;
        Limit = limit;
    }

    public GatewayGizmoOwnerScope OwnerScope { get; }

    public IReadOnlyList<string> OwnerHandles { get; }

    public IReadOnlyList<string> ArchitectCategoryDefNames { get; }

    public int Limit { get; }

    public static GatewayGizmoQuery ForSelection(
        int limit = GatewayGizmoRegistry.MaximumResults,
        IEnumerable<string>? architectCategoryDefNames = null) =>
        new(
            GatewayGizmoOwnerScope.Selection,
            Array.Empty<string>(),
            Copy(architectCategoryDefNames),
            limit);

    public static GatewayGizmoQuery ForOwners(
        IEnumerable<string> ownerHandles,
        int limit = GatewayGizmoRegistry.MaximumResults,
        IEnumerable<string>? architectCategoryDefNames = null) =>
        new(
            GatewayGizmoOwnerScope.ExplicitOwners,
            Copy(ownerHandles),
            Copy(architectCategoryDefNames),
            limit);

    private static IReadOnlyList<string> Copy(IEnumerable<string>? values) =>
        new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).ToList());
}

public sealed class GatewayGizmoSourceQuery
{
    public GatewayGizmoSourceQuery(
        GatewayGizmoOwnerScope ownerScope,
        IReadOnlyList<string> ownerHandles,
        IReadOnlyList<string> architectCategoryDefNames,
        int maximumCandidates)
    {
        OwnerScope = ownerScope;
        OwnerHandles = ownerHandles ?? throw new ArgumentNullException(nameof(ownerHandles));
        ArchitectCategoryDefNames = architectCategoryDefNames ??
            throw new ArgumentNullException(nameof(architectCategoryDefNames));
        MaximumCandidates = maximumCandidates;
    }

    public GatewayGizmoOwnerScope OwnerScope { get; }

    public IReadOnlyList<string> OwnerHandles { get; }

    public IReadOnlyList<string> ArchitectCategoryDefNames { get; }

    public int MaximumCandidates { get; }
}

public sealed class GatewayGizmoDiscovery
{
    public GatewayGizmoDiscovery(
        string mapHandle,
        IReadOnlyList<string> resolvedOwnerHandles,
        IReadOnlyList<IGatewayGizmoCandidate> candidates,
        bool pageTruncated)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        ResolvedOwnerHandles = resolvedOwnerHandles ??
            throw new ArgumentNullException(nameof(resolvedOwnerHandles));
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        PageTruncated = pageTruncated;
    }

    public string MapHandle { get; }

    public IReadOnlyList<string> ResolvedOwnerHandles { get; }

    public IReadOnlyList<IGatewayGizmoCandidate> Candidates { get; }

    public bool PageTruncated { get; }
}

public sealed class GatewayGizmoCandidateSnapshot
{
    public GatewayGizmoCandidateSnapshot(
        string identity,
        GatewayGizmoSource source,
        IReadOnlyList<string> ownerHandles,
        string runtimeType,
        string label,
        string description,
        float order,
        bool disabled,
        string? disabledReason,
        string? hotKey,
        int groupKey,
        GatewayGizmoInteractionKind interactionKind,
        bool? toggleState,
        IReadOnlyList<GatewayInteractionInputKind> acceptedInputs,
        string? buildableDefName = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Source = source;
        OwnerHandles = ownerHandles ?? throw new ArgumentNullException(nameof(ownerHandles));
        RuntimeType = runtimeType ?? throw new ArgumentNullException(nameof(runtimeType));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Order = order;
        Disabled = disabled;
        DisabledReason = disabledReason;
        HotKey = hotKey;
        GroupKey = groupKey;
        InteractionKind = interactionKind;
        ToggleState = toggleState;
        AcceptedInputs = acceptedInputs ?? throw new ArgumentNullException(nameof(acceptedInputs));
        BuildableDefName = string.IsNullOrWhiteSpace(buildableDefName) ? null : buildableDefName;
    }

    public string Identity { get; }

    public GatewayGizmoSource Source { get; }

    public IReadOnlyList<string> OwnerHandles { get; }

    public string RuntimeType { get; }

    public string Label { get; }

    public string Description { get; }

    public float Order { get; }

    public bool Disabled { get; }

    public string? DisabledReason { get; }

    public string? HotKey { get; }

    public int GroupKey { get; }

    public GatewayGizmoInteractionKind InteractionKind { get; }

    public bool? ToggleState { get; }

    public IReadOnlyList<GatewayInteractionInputKind> AcceptedInputs { get; }

    public string? BuildableDefName { get; }
}

public interface IGatewayGizmoCandidate
{
    GatewayGizmoCandidateSnapshot Capture();

    void Invoke();

    GatewayTargetAcceptance Preflight(GatewayInteractionTarget target);

    GatewayNativeApplyResult Apply(IReadOnlyList<GatewayInteractionTarget> targets);

    void Cancel();
}

public interface IGatewayGizmoSource
{
    GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query);
}

public sealed class GatewayGizmoDescriptor
{
    internal GatewayGizmoDescriptor(
        string handle,
        string revision,
        GatewayGizmoCandidateSnapshot snapshot)
    {
        Handle = handle;
        Revision = revision;
        Identity = snapshot.Identity;
        Source = snapshot.Source;
        OwnerHandles = snapshot.OwnerHandles;
        RuntimeType = snapshot.RuntimeType;
        Label = snapshot.Label;
        Description = snapshot.Description;
        Order = snapshot.Order;
        Disabled = snapshot.Disabled;
        DisabledReason = snapshot.DisabledReason;
        HotKey = snapshot.HotKey;
        GroupKey = snapshot.GroupKey;
        InteractionKind = snapshot.InteractionKind;
        ToggleState = snapshot.ToggleState;
        AcceptedInputs = snapshot.AcceptedInputs;
        BuildableDefName = snapshot.BuildableDefName;
    }

    public string Handle { get; }

    public string Revision { get; }

    public string Identity { get; }

    public GatewayGizmoSource Source { get; }

    public IReadOnlyList<string> OwnerHandles { get; }

    public string RuntimeType { get; }

    public string Label { get; }

    public string Description { get; }

    public float Order { get; }

    public bool Disabled { get; }

    public string? DisabledReason { get; }

    public string? HotKey { get; }

    public int GroupKey { get; }

    public GatewayGizmoInteractionKind InteractionKind { get; }

    public bool? ToggleState { get; }

    public IReadOnlyList<GatewayInteractionInputKind> AcceptedInputs { get; }

    public string? BuildableDefName { get; }
}

public sealed class GatewayGizmoQueryResult
{
    public GatewayGizmoQueryResult(
        string revision,
        IReadOnlyList<GatewayGizmoDescriptor> items,
        bool pageTruncated)
    {
        Revision = revision;
        Items = items;
        PageTruncated = pageTruncated;
    }

    public string Revision { get; }

    public IReadOnlyList<GatewayGizmoDescriptor> Items { get; }

    public bool PageTruncated { get; }
}

public sealed class GatewayGizmoInvocationResult
{
    public GatewayGizmoInvocationResult(
        string handle,
        GatewayGizmoInteractionKind interactionKind,
        bool completed,
        bool? toggleBefore,
        bool? toggleAfter,
        GatewayInteractionDescriptor? interaction)
    {
        Handle = handle;
        InteractionKind = interactionKind;
        Completed = completed;
        ToggleBefore = toggleBefore;
        ToggleAfter = toggleAfter;
        Interaction = interaction;
    }

    public string Handle { get; }

    public GatewayGizmoInteractionKind InteractionKind { get; }

    public bool Completed { get; }

    public bool? ToggleBefore { get; }

    public bool? ToggleAfter { get; }

    public GatewayInteractionDescriptor? Interaction { get; }
}

public sealed class GatewayInteractionDescriptor
{
    internal GatewayInteractionDescriptor(
        string handle,
        string sourceHandle,
        GatewayGizmoInteractionKind kind,
        IReadOnlyList<GatewayInteractionInputKind> acceptedInputs,
        string mapHandle,
        IReadOnlyList<string> ownerHandles,
        string revision)
    {
        Handle = handle;
        SourceHandle = sourceHandle;
        Kind = kind;
        AcceptedInputs = acceptedInputs;
        MapHandle = mapHandle;
        OwnerHandles = ownerHandles;
        Revision = revision;
    }

    public string Handle { get; }

    public string SourceHandle { get; }

    public GatewayGizmoInteractionKind Kind { get; }

    public IReadOnlyList<GatewayInteractionInputKind> AcceptedInputs { get; }

    public string MapHandle { get; }

    public IReadOnlyList<string> OwnerHandles { get; }

    public string Revision { get; }
}

public sealed class GatewayInteractionTarget
{
    private GatewayInteractionTarget(
        GatewayInteractionTargetKind kind,
        string? thingHandle,
        GatewayMapCell? cell)
    {
        Kind = kind;
        ThingHandle = thingHandle;
        Cell = cell;
    }

    public GatewayInteractionTargetKind Kind { get; }

    public string? ThingHandle { get; }

    public GatewayMapCell? Cell { get; }

    public static GatewayInteractionTarget ForThing(string handle) =>
        new(GatewayInteractionTargetKind.Thing, handle, null);

    public static GatewayInteractionTarget ForCell(GatewayMapCell cell) =>
        new(GatewayInteractionTargetKind.Cell, null, cell);
}

public sealed class GatewayInteractionInput
{
    private GatewayInteractionInput(
        GatewayInteractionInputKind kind,
        string? thingHandle,
        IReadOnlyList<GatewayMapCell> cells)
    {
        Kind = kind;
        ThingHandle = thingHandle;
        Cells = cells;
    }

    public GatewayInteractionInputKind Kind { get; }

    public string? ThingHandle { get; }

    public IReadOnlyList<GatewayMapCell> Cells { get; }

    public static GatewayInteractionInput ForThing(string thingHandle) =>
        new(
            GatewayInteractionInputKind.Thing,
            thingHandle ?? throw new ArgumentNullException(nameof(thingHandle)),
            Array.Empty<GatewayMapCell>());

    public static GatewayInteractionInput ForCell(GatewayMapCell cell) =>
        new(
            GatewayInteractionInputKind.Cell,
            null,
            new[] { cell ?? throw new ArgumentNullException(nameof(cell)) });

    public static GatewayInteractionInput ForCells(IEnumerable<GatewayMapCell> cells) =>
        new(
            GatewayInteractionInputKind.Cells,
            null,
            CopyCells(cells));

    public static GatewayInteractionInput ForLine(GatewayMapCell start, GatewayMapCell end) =>
        new(
            GatewayInteractionInputKind.Line,
            null,
            new[]
            {
                start ?? throw new ArgumentNullException(nameof(start)),
                end ?? throw new ArgumentNullException(nameof(end))
            });

    public static GatewayInteractionInput ForRectangle(GatewayMapCell cornerA, GatewayMapCell cornerB) =>
        new(
            GatewayInteractionInputKind.Rectangle,
            null,
            new[]
            {
                cornerA ?? throw new ArgumentNullException(nameof(cornerA)),
                cornerB ?? throw new ArgumentNullException(nameof(cornerB))
            });

    private static IReadOnlyList<GatewayMapCell> CopyCells(IEnumerable<GatewayMapCell> cells)
    {
        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        var copy = cells.ToList();
        if (copy.Any(cell => cell is null))
        {
            throw new ArgumentException("Interaction cells cannot contain null.", nameof(cells));
        }

        return new ReadOnlyCollection<GatewayMapCell>(copy);
    }
}

public sealed class GatewayTargetAcceptance
{
    public GatewayTargetAcceptance(bool accepted, string? reason = null)
    {
        Accepted = accepted;
        Reason = reason;
    }

    public bool Accepted { get; }

    public string? Reason { get; }
}

public sealed class GatewayNativeApplyResult
{
    public GatewayNativeApplyResult(bool completed)
    {
        Completed = completed;
    }

    public bool Completed { get; }
}

public sealed class GatewayRejectedInteractionTarget
{
    public GatewayRejectedInteractionTarget(GatewayInteractionTarget target, string? reason)
    {
        Target = target;
        Reason = reason;
    }

    public GatewayInteractionTarget Target { get; }

    public string? Reason { get; }
}

public sealed class GatewayInteractionApplyResult
{
    public GatewayInteractionApplyResult(
        string interactionHandle,
        IReadOnlyList<GatewayInteractionTarget> accepted,
        IReadOnlyList<GatewayRejectedInteractionTarget> rejected,
        bool completed)
    {
        InteractionHandle = interactionHandle;
        Accepted = accepted;
        Rejected = rejected;
        Completed = completed;
    }

    public string InteractionHandle { get; }

    public IReadOnlyList<GatewayInteractionTarget> Accepted { get; }

    public IReadOnlyList<GatewayRejectedInteractionTarget> Rejected { get; }

    public bool Completed { get; }
}

public sealed class GatewayInteractionCancelResult
{
    public GatewayInteractionCancelResult(string interactionHandle, bool cancelled)
    {
        InteractionHandle = interactionHandle;
        Cancelled = cancelled;
    }

    public string InteractionHandle { get; }

    public bool Cancelled { get; }
}

public sealed class GatewayGizmoRegistry
{
    public const int MaximumOwners = 256;
    public const int MaximumArchitectCategories = 64;
    public const int MaximumResults = 1000;
    public const int MaximumInteractionTargets = 4096;
    private const int MaximumRegistrations = 4096;

    private readonly IGatewayGizmoSource source;
    private readonly Dictionary<string, Registration> registrations = new(StringComparer.Ordinal);
    private readonly List<string> registrationOrder = new();
    private ActiveInteraction? activeInteraction;
    private long nextInteractionId;

    public GatewayGizmoRegistry(IGatewayGizmoSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public GatewayInteractionDescriptor? CurrentInteraction
    {
        get
        {
            if (activeInteraction is null)
            {
                return null;
            }

            try
            {
                Revalidate(activeInteraction.Registration, out _);
                return activeInteraction.Descriptor;
            }
            catch (GatewayGizmoException exception) when (
                exception.Code == "stale_gizmo_handle")
            {
                activeInteraction = null;
                throw new GatewayGizmoException(
                    "stale_interaction",
                    "The interaction source changed after it was started.",
                    exception);
            }
        }
    }

    public GatewayGizmoQueryResult Query(GatewayGizmoQuery query)
    {
        ValidateQuery(query);
        var sourceQuery = new GatewayGizmoSourceQuery(
            query.OwnerScope,
            query.OwnerHandles,
            query.ArchitectCategoryDefNames,
            query.Limit + 1);
        var discovery = source.Discover(sourceQuery);
        ValidateDiscovery(discovery);
        var candidates = discovery.Candidates.Take(query.Limit + 1).ToList();
        var capturedCandidates = CaptureCandidates(candidates);
        var captures = capturedCandidates.Select(candidate => candidate.Snapshot).ToList();
        var revision = Fingerprint(discovery, captures);
        var descriptors = capturedCandidates
            .Take(query.Limit)
            .Select((candidate, index) =>
            {
                var snapshot = candidate.Snapshot;
                var handle = CreateHandle(revision, index, snapshot.Identity);
                Remember(new Registration(
                    handle,
                    query,
                    revision,
                    index,
                    discovery.MapHandle,
                    discovery.ResolvedOwnerHandles));
                return new GatewayGizmoDescriptor(handle, revision, snapshot);
            })
            .ToList();

        return new GatewayGizmoQueryResult(
            revision,
            new ReadOnlyCollection<GatewayGizmoDescriptor>(descriptors),
            discovery.PageTruncated || capturedCandidates.Count > query.Limit);
    }

    public GatewayGizmoInvocationResult Invoke(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || !registrations.TryGetValue(handle, out var registration))
        {
            throw new GatewayGizmoException("gizmo_not_found", "The gizmo handle is unknown.");
        }

        var candidate = Revalidate(registration, out var snapshot);
        if (snapshot.Disabled)
        {
            throw new GatewayGizmoException(
                "gizmo_disabled",
                snapshot.DisabledReason ?? $"Gizmo '{snapshot.Label}' is disabled.");
        }

        if (snapshot.InteractionKind == GatewayGizmoInteractionKind.Target ||
            snapshot.InteractionKind == GatewayGizmoInteractionKind.Placement ||
            snapshot.InteractionKind == GatewayGizmoInteractionKind.Drag)
        {
            if (activeInteraction is not null)
            {
                throw new GatewayGizmoException(
                    "interaction_in_progress",
                    "Another semantic interaction is already active.");
            }

            var interactionHandle = "interaction_" + (++nextInteractionId).ToString(
                CultureInfo.InvariantCulture);
            var descriptor = new GatewayInteractionDescriptor(
                interactionHandle,
                handle,
                snapshot.InteractionKind,
                snapshot.AcceptedInputs,
                registration.MapHandle,
                registration.OwnerHandles,
                registration.Revision);
            activeInteraction = new ActiveInteraction(descriptor, registration);
            return new GatewayGizmoInvocationResult(
                handle,
                snapshot.InteractionKind,
                completed: false,
                toggleBefore: null,
                toggleAfter: null,
                descriptor);
        }

        if (snapshot.InteractionKind != GatewayGizmoInteractionKind.Immediate &&
            snapshot.InteractionKind != GatewayGizmoInteractionKind.Toggle)
        {
            throw new GatewayGizmoException(
                "unsupported_gizmo",
                $"Gizmo '{snapshot.Label}' does not expose a supported direct invocation.");
        }

        var toggleBefore = snapshot.InteractionKind == GatewayGizmoInteractionKind.Toggle
            ? snapshot.ToggleState
            : null;
        candidate.Invoke();
        var toggleAfter = snapshot.InteractionKind == GatewayGizmoInteractionKind.Toggle
            ? candidate.Capture().ToggleState
            : null;
        return new GatewayGizmoInvocationResult(
            handle,
            snapshot.InteractionKind,
            completed: true,
            toggleBefore,
            toggleAfter,
            interaction: null);
    }

    public GatewayInteractionApplyResult Apply(
        string interactionHandle,
        GatewayInteractionInput input)
    {
        if (activeInteraction is null ||
            !string.Equals(activeInteraction.Descriptor.Handle, interactionHandle, StringComparison.Ordinal))
        {
            throw new GatewayGizmoException(
                "interaction_not_current",
                "The supplied interaction is not the active semantic interaction.");
        }

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        IGatewayGizmoCandidate candidate;
        try
        {
            candidate = Revalidate(activeInteraction.Registration, out _);
        }
        catch (GatewayGizmoException exception) when (exception.Code == "stale_gizmo_handle")
        {
            activeInteraction = null;
            throw new GatewayGizmoException(
                "stale_interaction",
                "The interaction source changed after it was started.",
                exception);
        }

        if (!activeInteraction.Descriptor.AcceptedInputs.Contains(input.Kind))
        {
            throw new GatewayGizmoException(
                "interaction_target_mismatch",
                $"The active interaction does not accept {input.Kind} input.");
        }

        var targets = Expand(input);
        var accepted = new List<GatewayInteractionTarget>(targets.Count);
        var rejected = new List<GatewayRejectedInteractionTarget>();
        foreach (var target in targets)
        {
            var acceptance = candidate.Preflight(target);
            if (acceptance.Accepted)
            {
                accepted.Add(target);
            }
            else
            {
                rejected.Add(new GatewayRejectedInteractionTarget(target, acceptance.Reason));
            }
        }

        var completed = false;
        if (accepted.Count > 0)
        {
            completed = candidate.Apply(new ReadOnlyCollection<GatewayInteractionTarget>(accepted)).Completed;
            if (completed)
            {
                activeInteraction = null;
            }
        }

        return new GatewayInteractionApplyResult(
            interactionHandle,
            new ReadOnlyCollection<GatewayInteractionTarget>(accepted),
            new ReadOnlyCollection<GatewayRejectedInteractionTarget>(rejected),
            completed);
    }

    public GatewayInteractionCancelResult Cancel(string interactionHandle)
    {
        if (activeInteraction is null ||
            !string.Equals(activeInteraction.Descriptor.Handle, interactionHandle, StringComparison.Ordinal))
        {
            throw new GatewayGizmoException(
                "interaction_not_current",
                "The supplied interaction is not the active semantic interaction.");
        }

        IGatewayGizmoCandidate candidate;
        try
        {
            candidate = Revalidate(activeInteraction.Registration, out _);
        }
        catch (GatewayGizmoException exception) when (exception.Code == "stale_gizmo_handle")
        {
            activeInteraction = null;
            throw new GatewayGizmoException(
                "stale_interaction",
                "The interaction source changed after it was started.",
                exception);
        }

        try
        {
            candidate.Cancel();
        }
        finally
        {
            activeInteraction = null;
        }

        return new GatewayInteractionCancelResult(interactionHandle, cancelled: true);
    }

    private static IReadOnlyList<GatewayInteractionTarget> Expand(GatewayInteractionInput input)
    {
        switch (input.Kind)
        {
            case GatewayInteractionInputKind.Thing:
                return new[] { GatewayInteractionTarget.ForThing(input.ThingHandle!) };
            case GatewayInteractionInputKind.Cell:
                return new[] { GatewayInteractionTarget.ForCell(input.Cells[0]) };
            case GatewayInteractionInputKind.Cells:
                return UniqueCells(input.Cells);
            case GatewayInteractionInputKind.Line:
                return ExpandLine(input.Cells[0], input.Cells[1]);
            case GatewayInteractionInputKind.Rectangle:
                return ExpandRectangle(input.Cells[0], input.Cells[1]);
            default:
                throw new GatewayGizmoException(
                    "interaction_target_mismatch",
                    $"Input shape {input.Kind} is unsupported.");
        }
    }

    private static IReadOnlyList<GatewayInteractionTarget> UniqueCells(
        IEnumerable<GatewayMapCell> cells)
    {
        var unique = new HashSet<long>();
        var targets = new List<GatewayInteractionTarget>();
        foreach (var cell in cells)
        {
            if (!unique.Add(CellKey(cell.X, cell.Z)))
            {
                continue;
            }

            if (targets.Count == MaximumInteractionTargets)
            {
                throw TooManyInteractionTargets();
            }

            targets.Add(GatewayInteractionTarget.ForCell(cell));
        }

        if (targets.Count == 0)
        {
            throw new GatewayGizmoException(
                "empty_interaction_target",
                "At least one interaction target is required.");
        }

        return new ReadOnlyCollection<GatewayInteractionTarget>(targets);
    }

    private static IReadOnlyList<GatewayInteractionTarget> ExpandLine(
        GatewayMapCell start,
        GatewayMapCell end)
    {
        var span = Math.Max(
            Math.Abs((long)end.X - start.X),
            Math.Abs((long)end.Z - start.Z)) + 1L;
        if (span > MaximumInteractionTargets)
        {
            throw TooManyInteractionTargets();
        }

        var cells = new List<GatewayMapCell>((int)span);
        var x = start.X;
        var z = start.Z;
        var dx = Math.Abs(end.X - start.X);
        var dz = Math.Abs(end.Z - start.Z);
        var stepX = start.X < end.X ? 1 : -1;
        var stepZ = start.Z < end.Z ? 1 : -1;
        var error = dx - dz;
        while (true)
        {
            cells.Add(new GatewayMapCell(x, z));
            if (x == end.X && z == end.Z)
            {
                break;
            }

            var doubled = 2 * error;
            if (doubled > -dz)
            {
                error -= dz;
                x += stepX;
            }

            if (doubled < dx)
            {
                error += dx;
                z += stepZ;
            }
        }

        return UniqueCells(cells);
    }

    private static IReadOnlyList<GatewayInteractionTarget> ExpandRectangle(
        GatewayMapCell cornerA,
        GatewayMapCell cornerB)
    {
        var minX = Math.Min(cornerA.X, cornerB.X);
        var maxX = Math.Max(cornerA.X, cornerB.X);
        var minZ = Math.Min(cornerA.Z, cornerB.Z);
        var maxZ = Math.Max(cornerA.Z, cornerB.Z);
        var width = (long)maxX - minX + 1L;
        var height = (long)maxZ - minZ + 1L;
        if (width > MaximumInteractionTargets || height > MaximumInteractionTargets ||
            width * height > MaximumInteractionTargets)
        {
            throw TooManyInteractionTargets();
        }

        var count = width * height;
        var cells = new List<GatewayMapCell>((int)count);
        for (var z = (long)minZ; z <= maxZ; z++)
        {
            for (var x = (long)minX; x <= maxX; x++)
            {
                cells.Add(new GatewayMapCell((int)x, (int)z));
            }
        }

        return UniqueCells(cells);
    }

    private static long CellKey(int x, int z) => ((long)x << 32) ^ (uint)z;

    private static GatewayGizmoException TooManyInteractionTargets() => new(
        "interaction_target_limit",
        $"An interaction may resolve to at most {MaximumInteractionTargets} unique targets.");

    private void Remember(Registration registration)
    {
        if (!registrations.ContainsKey(registration.Handle))
        {
            while (registrations.Count >= MaximumRegistrations)
            {
                registrations.Remove(registrationOrder[0]);
                registrationOrder.RemoveAt(0);
            }

            registrationOrder.Add(registration.Handle);
        }

        registrations[registration.Handle] = registration;
    }

    private IGatewayGizmoCandidate Revalidate(
        Registration registration,
        out GatewayGizmoCandidateSnapshot snapshot)
    {
        var query = registration.Query;
        GatewayGizmoDiscovery discovery;
        try
        {
            discovery = source.Discover(new GatewayGizmoSourceQuery(
                query.OwnerScope,
                query.OwnerHandles,
                query.ArchitectCategoryDefNames,
                query.Limit + 1));
            ValidateDiscovery(discovery);
        }
        catch (Exception exception) when (exception is not GatewayGizmoException gizmoException ||
                                          gizmoException.Code != "stale_gizmo_handle")
        {
            throw new GatewayGizmoException(
                "stale_gizmo_handle",
                "The gizmo source can no longer be resolved: " + exception.Message,
                exception);
        }

        var candidates = discovery.Candidates.Take(query.Limit + 1).ToList();
        var capturedCandidates = CaptureCandidates(candidates);
        var captures = capturedCandidates.Select(candidate => candidate.Snapshot).ToList();
        var revision = Fingerprint(discovery, captures);
        if (!string.Equals(revision, registration.Revision, StringComparison.Ordinal) ||
            registration.Index >= capturedCandidates.Count)
        {
            throw new GatewayGizmoException(
                "stale_gizmo_handle",
                "The map, owners, or ordered gizmo list changed after discovery.");
        }

        var capturedCandidate = capturedCandidates[registration.Index];
        snapshot = capturedCandidate.Snapshot;
        if (!string.Equals(
                CreateHandle(revision, registration.Index, snapshot.Identity),
                registration.Handle,
                StringComparison.Ordinal))
        {
            throw new GatewayGizmoException(
                "stale_gizmo_handle",
                "The gizmo identity changed after discovery.");
        }

        return capturedCandidate.Candidate;
    }

    private static IReadOnlyList<CapturedCandidate> CaptureCandidates(
        IEnumerable<IGatewayGizmoCandidate> candidates)
    {
        var captures = new List<CapturedCandidate>();
        foreach (var candidate in candidates)
        {
            try
            {
                captures.Add(new CapturedCandidate(candidate, candidate.Capture()));
            }
            catch (Exception)
            {
                // A modded gizmo may throw while exposing optional UI metadata. Keep
                // healthy candidates usable; repeating this filter during revalidation
                // preserves deterministic handles for a stable discovery result.
            }
        }

        return captures;
    }

    private static void ValidateQuery(GatewayGizmoQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (query.Limit <= 0 || query.Limit > MaximumResults)
        {
            throw new GatewayGizmoException(
                "invalid_gizmo_limit",
                $"Gizmo query limit must be between 1 and {MaximumResults}.");
        }

        if (query.OwnerHandles.Count > MaximumOwners)
        {
            throw new GatewayGizmoException(
                "too_many_gizmo_owners",
                $"A gizmo query accepts at most {MaximumOwners} owner handles.");
        }

        if (query.ArchitectCategoryDefNames.Count > MaximumArchitectCategories)
        {
            throw new GatewayGizmoException(
                "too_many_architect_categories",
                $"A gizmo query accepts at most {MaximumArchitectCategories} architect categories.");
        }
    }

    private static void ValidateDiscovery(GatewayGizmoDiscovery discovery)
    {
        if (discovery.ResolvedOwnerHandles.Count > MaximumOwners)
        {
            throw new GatewayGizmoException(
                "too_many_gizmo_owners",
                $"Gizmo discovery resolved more than {MaximumOwners} owners.");
        }
    }

    private static string Fingerprint(
        GatewayGizmoDiscovery discovery,
        IReadOnlyList<GatewayGizmoCandidateSnapshot> captures)
    {
        var text = new StringBuilder();
        Append(text, discovery.MapHandle);
        foreach (var owner in discovery.ResolvedOwnerHandles)
        {
            Append(text, owner);
        }

        for (var index = 0; index < captures.Count; index++)
        {
            var capture = captures[index];
            Append(text, index.ToString(CultureInfo.InvariantCulture));
            Append(text, capture.Identity);
            Append(text, capture.Source.ToString());
            Append(text, capture.RuntimeType);
            Append(text, capture.Label);
            Append(text, capture.Description);
            Append(text, capture.Order.ToString("R", CultureInfo.InvariantCulture));
            Append(text, capture.HotKey ?? string.Empty);
            Append(text, capture.GroupKey.ToString(CultureInfo.InvariantCulture));
            Append(text, capture.InteractionKind.ToString());
            Append(text, capture.BuildableDefName ?? string.Empty);
            foreach (var acceptedInput in capture.AcceptedInputs)
            {
                Append(text, acceptedInput.ToString());
            }
            foreach (var owner in capture.OwnerHandles)
            {
                Append(text, owner);
            }
        }

        return Hash(text.ToString()).Substring(0, 32);
    }

    private static string CreateHandle(string revision, int index, string identity) =>
        "gizmo_" + Hash(revision + ":" + index.ToString(CultureInfo.InvariantCulture) +
            ":" + identity).Substring(0, 24);

    private static void Append(StringBuilder target, string value)
    {
        target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        target.Append(':');
        target.Append(value);
        target.Append(';');
    }

    private static string Hash(string value)
    {
        using var algorithm = SHA256.Create();
        var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
        var text = new StringBuilder(bytes.Length * 2);
        foreach (var item in bytes)
        {
            text.Append(item.ToString("x2", CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    private sealed class Registration
    {
        public Registration(
            string handle,
            GatewayGizmoQuery query,
            string revision,
            int index,
            string mapHandle,
            IReadOnlyList<string> ownerHandles)
        {
            Handle = handle;
            Query = query;
            Revision = revision;
            Index = index;
            MapHandle = mapHandle;
            OwnerHandles = new ReadOnlyCollection<string>(ownerHandles.ToList());
        }

        public string Handle { get; }

        public GatewayGizmoQuery Query { get; }

        public string Revision { get; }

        public int Index { get; }

        public string MapHandle { get; }

        public IReadOnlyList<string> OwnerHandles { get; }
    }

    private sealed class CapturedCandidate
    {
        public CapturedCandidate(
            IGatewayGizmoCandidate candidate,
            GatewayGizmoCandidateSnapshot snapshot)
        {
            Candidate = candidate;
            Snapshot = snapshot;
        }

        public IGatewayGizmoCandidate Candidate { get; }

        public GatewayGizmoCandidateSnapshot Snapshot { get; }
    }

    private sealed class ActiveInteraction
    {
        public ActiveInteraction(GatewayInteractionDescriptor descriptor, Registration registration)
        {
            Descriptor = descriptor;
            Registration = registration;
        }

        public GatewayInteractionDescriptor Descriptor { get; }

        public Registration Registration { get; }
    }
}
