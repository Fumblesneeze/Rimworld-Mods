using System.Collections.Generic;
using RimWorld;
using RimWorldDevGateway.Contracts;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewayThingControlException : Exception
{
    public GatewayThingControlException(string code, string message)
        : base(message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

internal static class GatewayThingDiagnostics
{
    public static void ReportSelectedSummaryFailure(
        GatewayLogBuffer? diagnostics,
        string? handle,
        Exception exception)
    {
        if (diagnostics is null)
        {
            return;
        }

        var safeHandle = string.IsNullOrWhiteSpace(handle) ? "<unidentified>" : handle!;
        if (safeHandle.Length > GatewayThingController.MaximumHandleLength)
        {
            safeHandle = safeHandle.Substring(0, GatewayThingController.MaximumHandleLength);
        }

        try
        {
            diagnostics.Append(
                "Warning",
                $"Selected Thing summary for '{safeHandle}' was omitted after a mod exception.",
                exception.ToString(),
                requestId: GatewayRequestScope.CurrentRequestId);
        }
        catch (Exception)
        {
            // A diagnostic failure must not reintroduce the selection failure it describes.
        }
    }
}

public sealed class GatewayFactionSummary
{
    public GatewayFactionSummary(string name, string defName, string relation)
    {
        Name = name ?? string.Empty;
        DefName = defName ?? string.Empty;
        Relation = relation ?? string.Empty;
    }

    public string Name { get; }

    public string DefName { get; }

    public string Relation { get; }
}

public sealed class GatewayHitPointSummary
{
    public GatewayHitPointSummary(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    public int Current { get; }

    public int Maximum { get; }
}

public sealed class GatewayPawnFlags
{
    public GatewayPawnFlags(
        bool colonist,
        bool prisoner,
        bool humanlike,
        bool animal,
        bool mechanoid)
    {
        Colonist = colonist;
        Prisoner = prisoner;
        Humanlike = humanlike;
        Animal = animal;
        Mechanoid = mechanoid;
    }

    public bool Colonist { get; }

    public bool Prisoner { get; }

    public bool Humanlike { get; }

    public bool Animal { get; }

    public bool Mechanoid { get; }
}

public sealed class GatewayThingSummary
{
    public GatewayThingSummary(
        string handle,
        string label,
        string defName,
        string runtimeType,
        string kind,
        string mapHandle,
        GatewayMapCell position,
        GatewayMapRect occupiedRect,
        string rotation,
        int stackCount,
        GatewayFactionSummary? faction,
        GatewayHitPointSummary? hitPoints,
        string? quality,
        bool fogged,
        bool forbidden,
        bool selected,
        GatewayPawnFlags? pawnFlags,
        string? loadId = null,
        string? stuffDefName = null)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        Label = label ?? string.Empty;
        DefName = defName ?? string.Empty;
        RuntimeType = runtimeType ?? string.Empty;
        Kind = kind ?? string.Empty;
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        OccupiedRect = occupiedRect ?? throw new ArgumentNullException(nameof(occupiedRect));
        Rotation = rotation ?? string.Empty;
        StackCount = stackCount;
        Faction = faction;
        HitPoints = hitPoints;
        Quality = quality;
        Fogged = fogged;
        Forbidden = forbidden;
        Selected = selected;
        PawnFlags = pawnFlags;
        LoadId = loadId ?? handle;
        StuffDefName = stuffDefName;
    }

    public string Handle { get; }

    public string Label { get; }

    public string DefName { get; }

    public string RuntimeType { get; }

    public string Kind { get; }

    public string MapHandle { get; }

    public GatewayMapCell Position { get; }

    public GatewayMapRect OccupiedRect { get; }

    public string Rotation { get; }

    public int StackCount { get; }

    public GatewayFactionSummary? Faction { get; }

    public GatewayHitPointSummary? HitPoints { get; }

    public string? Quality { get; }

    public bool Fogged { get; }

    public bool Forbidden { get; }

    public bool Selected { get; }

    public GatewayPawnFlags? PawnFlags { get; }

    public string LoadId { get; }

    public string? StuffDefName { get; }
}

/// <summary>
/// Provides stable, inexpensive query metadata while deferring the complete
/// summary until a candidate can actually contribute to the requested page.
/// </summary>
public interface IGatewayThingCandidate
{
    string Handle { get; }

    GatewayMapRect OccupiedRect { get; }

    GatewayThingSummary Capture();
}

public sealed class GatewayThingWorldSnapshot
{
    private readonly IReadOnlyList<GatewayThingSummary>? things;
    private readonly GatewayLogBuffer? diagnostics;

    public GatewayThingWorldSnapshot(
        string mapHandle,
        GatewayMapRect? viewRect,
        IReadOnlyList<GatewayThingSummary> things,
        int excludedUnaddressableCount = 0)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        ViewRect = viewRect;
        this.things = things ?? throw new ArgumentNullException(nameof(things));
        Candidates = things.Select(thing => (IGatewayThingCandidate)new FixedCandidate(thing)).ToArray();
        ExcludedUnaddressableCount = excludedUnaddressableCount;
    }

    public GatewayThingWorldSnapshot(
        string mapHandle,
        GatewayMapRect? viewRect,
        IReadOnlyList<IGatewayThingCandidate> candidates,
        int excludedUnaddressableCount = 0,
        GatewayLogBuffer? diagnostics = null)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        ViewRect = viewRect;
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        ExcludedUnaddressableCount = excludedUnaddressableCount;
        this.diagnostics = diagnostics;
    }

    public string MapHandle { get; }

    public GatewayMapRect? ViewRect { get; }

    public IReadOnlyList<GatewayThingSummary> Things
    {
        get
        {
            if (things is not null)
            {
                return things;
            }

            var captured = new List<GatewayThingSummary>(Candidates.Count);
            foreach (var candidate in Candidates)
            {
                try
                {
                    var summary = candidate.Capture();
                    if (summary is not null)
                    {
                        captured.Add(summary);
                    }
                }
                catch (Exception exception)
                {
                    GatewayThingDiagnostics.ReportSelectedSummaryFailure(
                        diagnostics,
                        CandidateHandle(candidate),
                        exception);
                }
            }

            return captured.ToArray();
        }
    }

    public IReadOnlyList<IGatewayThingCandidate> Candidates { get; }

    public int ExcludedUnaddressableCount { get; }

    private static string CandidateHandle(IGatewayThingCandidate candidate)
    {
        try
        {
            return candidate.Handle;
        }
        catch (Exception)
        {
            return "<unidentified>";
        }
    }

    private sealed class FixedCandidate : IGatewayThingCandidate
    {
        private readonly GatewayThingSummary summary;

        public FixedCandidate(GatewayThingSummary summary)
        {
            this.summary = summary;
        }

        public string Handle => summary.Handle;

        public GatewayMapRect OccupiedRect => summary.OccupiedRect;

        public GatewayThingSummary Capture() => summary;
    }
}

public sealed class GatewayThingQueryResult
{
    public GatewayThingQueryResult(
        string mapHandle,
        string scope,
        IReadOnlyList<GatewayThingSummary> things,
        bool pageTruncated,
        int excludedUnaddressableCount)
    {
        MapHandle = mapHandle ?? throw new ArgumentNullException(nameof(mapHandle));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Things = things ?? throw new ArgumentNullException(nameof(things));
        PageTruncated = pageTruncated;
        ExcludedUnaddressableCount = excludedUnaddressableCount;
    }

    public string MapHandle { get; }

    public string Scope { get; }

    public IReadOnlyList<GatewayThingSummary> Things { get; }

    public bool PageTruncated { get; }

    public int ExcludedUnaddressableCount { get; }
}

public sealed class GatewayThingInspection
{
    public GatewayThingInspection(
        GatewayThingSummary summary,
        string description,
        string inspectText,
        IReadOnlyList<string> componentTypes,
        GatewayItemDetails? item,
        GatewayBuildingDetails? building,
        GatewayPawnDetails? pawn,
        IReadOnlyList<GatewayInspectionWarning> warnings)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Description = description ?? string.Empty;
        InspectText = inspectText ?? string.Empty;
        ComponentTypes = componentTypes ?? throw new ArgumentNullException(nameof(componentTypes));
        Item = item;
        Building = building;
        Pawn = pawn;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }

    public GatewayThingSummary Summary { get; }

    public string Description { get; }

    public string InspectText { get; }

    public IReadOnlyList<string> ComponentTypes { get; }

    public GatewayItemDetails? Item { get; }

    public GatewayBuildingDetails? Building { get; }

    public GatewayPawnDetails? Pawn { get; }

    public IReadOnlyList<GatewayInspectionWarning> Warnings { get; }
}

public sealed class GatewayItemDetails
{
    public GatewayItemDetails(string? stuffDefName, int stackLimit, float? marketValue, float? mass)
    {
        StuffDefName = stuffDefName;
        StackLimit = stackLimit;
        MarketValue = marketValue;
        Mass = mass;
    }

    public string? StuffDefName { get; }

    public int StackLimit { get; }

    public float? MarketValue { get; }

    public float? Mass { get; }
}

public sealed class GatewayBuildingDetails
{
    public GatewayBuildingDetails(int width, int height, string passability, bool? powerOn)
    {
        Width = width;
        Height = height;
        Passability = passability ?? string.Empty;
        PowerOn = powerOn;
    }

    public int Width { get; }

    public int Height { get; }

    public string Passability { get; }

    public bool? PowerOn { get; }
}

public sealed class GatewayPawnSkillSummary
{
    public GatewayPawnSkillSummary(string defName, int level)
    {
        DefName = defName ?? string.Empty;
        Level = level;
    }

    public string DefName { get; }

    public int Level { get; }
}

public sealed class GatewayPawnHealthConditionSummary
{
    public GatewayPawnHealthConditionSummary(string label, string defName, float? severity)
    {
        Label = label ?? string.Empty;
        DefName = defName ?? string.Empty;
        Severity = severity;
    }

    public string Label { get; }

    public string DefName { get; }

    public float? Severity { get; }
}

public sealed class GatewayPawnDetails
{
    public GatewayPawnDetails(
        string kindDefName,
        string raceDefName,
        string? factionDefName,
        string gender,
        float biologicalAgeYears,
        float chronologicalAgeYears,
        bool dead,
        bool downed,
        bool drafted,
        string? currentJobDefName,
        IReadOnlyList<GatewayPawnSkillSummary> skills,
        IReadOnlyList<GatewayPawnHealthConditionSummary> healthConditions)
    {
        KindDefName = kindDefName ?? string.Empty;
        RaceDefName = raceDefName ?? string.Empty;
        FactionDefName = factionDefName;
        Gender = gender ?? string.Empty;
        BiologicalAgeYears = biologicalAgeYears;
        ChronologicalAgeYears = chronologicalAgeYears;
        Dead = dead;
        Downed = downed;
        Drafted = drafted;
        CurrentJobDefName = currentJobDefName;
        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        HealthConditions = healthConditions ?? throw new ArgumentNullException(nameof(healthConditions));
    }

    public string KindDefName { get; }

    public string RaceDefName { get; }

    public string? FactionDefName { get; }

    public string Gender { get; }

    public float BiologicalAgeYears { get; }

    public float ChronologicalAgeYears { get; }

    public bool Dead { get; }

    public bool Downed { get; }

    public bool Drafted { get; }

    public string? CurrentJobDefName { get; }

    public IReadOnlyList<GatewayPawnSkillSummary> Skills { get; }

    public IReadOnlyList<GatewayPawnHealthConditionSummary> HealthConditions { get; }
}

public sealed class GatewayInspectionWarning
{
    public GatewayInspectionWarning(string field, string message)
    {
        Field = field ?? string.Empty;
        Message = message ?? string.Empty;
    }

    public string Field { get; }

    public string Message { get; }
}

public enum GatewaySelectionOperation
{
    Replace,
    Add,
    Remove,
    Toggle,
    Clear
}

public sealed class GatewaySelectionMutationResult
{
    public GatewaySelectionMutationResult(
        string operation,
        IReadOnlyList<string> requestedHandles,
        IReadOnlyList<GatewayThingSummary> before,
        IReadOnlyList<GatewayThingSummary> after)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        RequestedHandles = requestedHandles ?? throw new ArgumentNullException(nameof(requestedHandles));
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));
    }

    public string Operation { get; }

    public IReadOnlyList<string> RequestedHandles { get; }

    public IReadOnlyList<GatewayThingSummary> Before { get; }

    public IReadOnlyList<GatewayThingSummary> After { get; }
}

public interface IGatewayThingOperations
{
    GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect);

    GatewayThingInspection CaptureInspection(string handle);

    IReadOnlyList<GatewayThingSummary> CaptureSelection();

    void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles);
}

/// <summary>
/// Applies bounded query semantics to immutable main-thread snapshots.
/// </summary>
public sealed class GatewayThingController
{
    private const int MaximumResults = 1000;
    private const int MaximumFilterEntries = 64;
    private const int MaximumLabelFilterLength = 256;
    public const int MaximumHandleLength = 256;
    private const int MaximumScalarLength = 512;
    private const int MaximumInspectionTextLength = 8192;
    private const int MaximumComponents = 64;
    private const int MaximumSkills = 64;
    private const int MaximumHealthConditions = 128;
    private const int MaximumWarnings = 64;
    public const int MaximumSelectionHandles = 200;
    private const string TruncatedMarker = "[truncated]";
    private readonly IGatewayThingOperations operations;
    private readonly GatewayLogBuffer? diagnostics;

    public GatewayThingController(
        IGatewayThingOperations operations,
        GatewayLogBuffer? diagnostics = null)
    {
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
        this.diagnostics = diagnostics;
    }

    public GatewayThingQueryResult Query(GatewayThingQueryRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var scope = (request.Scope ?? string.Empty).Trim().ToLowerInvariant();
        if (scope != "map" && scope != "view")
        {
            throw new GatewayThingControlException(
                "invalid_thing_scope",
                "Thing query scope must be 'map' or 'view'.");
        }

        ValidateFilterList(request.Kinds, "kinds");
        ValidateFilterList(request.DefNames, "defNames");
        ValidateFilterList(request.RuntimeTypes, "runtimeTypes");
        ValidateFilterList(request.FactionNames, "factionNames");
        ValidateFilterList(request.FactionRelations, "factionRelations");
        if (request.LabelContains is not null && request.LabelContains.Length > MaximumLabelFilterLength)
        {
            throw new GatewayThingControlException(
                "thing_filter_limit_exceeded",
                $"labelContains cannot exceed {MaximumLabelFilterLength} characters.");
        }

        if (request.Limit < 1 || request.Limit > MaximumResults)
        {
            throw new GatewayThingControlException(
                "thing_query_limit_out_of_range",
                $"Thing query limit must be between 1 and {MaximumResults}.");
        }

        var world = operations.CaptureWorld(scope == "view");
        if (scope == "view" && world.ViewRect is null)
        {
            throw new GatewayThingControlException(
                "camera_unavailable",
                "View-scoped thing queries require RimWorld's map camera.");
        }

        var excludedUnaddressableCount = world.ExcludedUnaddressableCount;
        var candidates = new List<CandidateIndex>();
        foreach (var candidate in world.Candidates)
        {
            try
            {
                var handle = candidate.Handle;
                if (string.IsNullOrWhiteSpace(handle) || handle.Length > MaximumHandleLength)
                {
                    excludedUnaddressableCount++;
                    continue;
                }

                var occupiedRect = candidate.OccupiedRect;
                if (scope == "view" && !Intersects(occupiedRect, world.ViewRect!))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(request.After) &&
                    StringComparer.Ordinal.Compare(handle, request.After) <= 0)
                {
                    continue;
                }

                candidates.Add(new CandidateIndex(handle, candidate));
            }
            catch (Exception)
            {
                excludedUnaddressableCount++;
            }
        }

        var matches = new List<GatewayThingSummary>(request.Limit + 1);
        foreach (var candidate in candidates.OrderBy(item => item.Handle, StringComparer.Ordinal))
        {
            GatewayThingSummary summary;
            try
            {
                summary = candidate.Candidate.Capture();
            }
            catch (Exception)
            {
                // A single modded Thing must not make the complete map query fail.
                continue;
            }

            if (!IsAddressableSummary(summary) ||
                !string.Equals(summary.Handle, candidate.Handle, StringComparison.Ordinal))
            {
                excludedUnaddressableCount++;
                continue;
            }

            if (!Matches(summary, request))
            {
                continue;
            }

            matches.Add(BoundSummary(summary));
            if (matches.Count > request.Limit)
            {
                break;
            }
        }

        var pageTruncated = matches.Count > request.Limit;
        if (pageTruncated)
        {
            matches.RemoveAt(matches.Count - 1);
        }

        return new GatewayThingQueryResult(
            world.MapHandle,
            scope,
            matches,
            pageTruncated,
            excludedUnaddressableCount);
    }

    public GatewayThingInspection Inspect(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle.Length > MaximumHandleLength)
        {
            throw new GatewayThingControlException(
                "invalid_thing_handle",
                $"Thing handle must contain 1 to {MaximumHandleLength} characters.");
        }

        return BoundInspection(operations.CaptureInspection(handle));
    }

    public IReadOnlyList<GatewayThingSummary> CaptureSelection()
    {
        var selected = new List<GatewayThingSummary>();
        foreach (var summary in operations.CaptureSelection())
        {
            try
            {
                if (IsAddressableSummary(summary))
                {
                    selected.Add(BoundSummary(summary));
                    if (selected.Count == 256)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                GatewayThingDiagnostics.ReportSelectedSummaryFailure(
                    diagnostics,
                    summary?.Handle,
                    exception);
            }
        }

        return selected;
    }

    public GatewaySelectionMutationResult MutateSelection(GatewaySelectionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!Enum.GetNames(typeof(GatewaySelectionOperation)).Any(name =>
                string.Equals(name, request.Operation, StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse(request.Operation, ignoreCase: true, out GatewaySelectionOperation operation) ||
            !Enum.IsDefined(typeof(GatewaySelectionOperation), operation))
        {
            throw new GatewayThingControlException(
                "invalid_selection_operation",
                "Selection operation must be replace, add, remove, toggle, or clear.");
        }

        var handles = request.Handles ?? throw new GatewayThingControlException(
            "invalid_selection_handles",
            "Selection handles cannot be null.");
        if (handles.Count > MaximumSelectionHandles)
        {
            throw new GatewayThingControlException(
                "selection_handle_limit_exceeded",
                $"Selection requests cannot contain more than {MaximumSelectionHandles} handles.");
        }

        if (operation == GatewaySelectionOperation.Clear && handles.Count != 0)
        {
            throw new GatewayThingControlException(
                "invalid_selection_handles",
                "The clear operation does not accept thing handles.");
        }

        if (operation != GatewaySelectionOperation.Clear &&
            operation != GatewaySelectionOperation.Replace &&
            handles.Count == 0)
        {
            throw new GatewayThingControlException(
                "invalid_selection_handles",
                "This selection operation requires at least one thing handle.");
        }

        var requested = new List<string>();
        var requestedSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in handles)
        {
            if (string.IsNullOrWhiteSpace(handle) || handle.Length > MaximumHandleLength)
            {
                throw new GatewayThingControlException(
                    "invalid_thing_handle",
                    $"Thing handle must contain 1 to {MaximumHandleLength} characters.");
            }

            if (requestedSeen.Add(handle))
            {
                requested.Add(handle);
            }
        }

        var world = operations.CaptureWorld(includeViewRect: false);
        var resolved = new List<string>();
        var resolvedSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in requested)
        {
            var match = world.Things.FirstOrDefault(thing =>
                string.Equals(thing.Handle, handle, StringComparison.Ordinal));
            match ??= world.Things.FirstOrDefault(thing =>
                string.Equals(thing.LoadId, handle, StringComparison.Ordinal));
            if (match is null)
            {
                throw new GatewayThingControlException(
                    "thing_not_found",
                    $"Thing '{handle}' is not spawned on the current map.");
            }

            if (resolvedSeen.Add(match.Handle))
            {
                resolved.Add(match.Handle);
            }
        }

        var before = CaptureSelection();
        operations.ApplySelection(operation, resolved);
        return new GatewaySelectionMutationResult(
            operation.ToString().ToLowerInvariant(),
            resolved,
            before,
            CaptureSelection());
    }

    private static void ValidateFilterList(IReadOnlyCollection<string>? values, string name)
    {
        if (values is null)
        {
            throw new GatewayThingControlException(
                "invalid_thing_filter",
                $"Thing query filter '{name}' cannot be null.");
        }

        if (values.Count > MaximumFilterEntries)
        {
            throw new GatewayThingControlException(
                "thing_filter_limit_exceeded",
                $"Thing query filter '{name}' cannot contain more than {MaximumFilterEntries} entries.");
        }
    }

    private static bool Intersects(GatewayMapRect left, GatewayMapRect right) =>
        left.MinX <= right.MaxX && left.MaxX >= right.MinX &&
        left.MinZ <= right.MaxZ && left.MaxZ >= right.MinZ;

    private static bool IsAddressableSummary(GatewayThingSummary thing) =>
        !string.IsNullOrWhiteSpace(thing.Handle) &&
        thing.Handle.Length <= MaximumHandleLength;

    private static bool Matches(GatewayThingSummary thing, GatewayThingQueryRequest request) =>
        (request.DefNames.Count == 0 ||
         request.DefNames.Contains(thing.DefName, StringComparer.Ordinal)) &&
        (request.Kinds.Count == 0 ||
         request.Kinds.Contains(thing.Kind, StringComparer.OrdinalIgnoreCase)) &&
        (request.RuntimeTypes.Count == 0 ||
         request.RuntimeTypes.Contains(thing.RuntimeType, StringComparer.Ordinal)) &&
        (request.FactionNames.Count == 0 ||
         (thing.Faction is not null &&
          request.FactionNames.Contains(thing.Faction.Name, StringComparer.Ordinal))) &&
        (request.FactionRelations.Count == 0 ||
         (thing.Faction is not null &&
          request.FactionRelations.Contains(thing.Faction.Relation, StringComparer.Ordinal))) &&
        (string.IsNullOrEmpty(request.LabelContains) ||
         thing.Label.IndexOf(request.LabelContains, StringComparison.OrdinalIgnoreCase) >= 0) &&
        (!request.Fogged.HasValue || thing.Fogged == request.Fogged.Value) &&
        (!request.Selected.HasValue || thing.Selected == request.Selected.Value);

    private static GatewayThingInspection BoundInspection(GatewayThingInspection inspection)
    {
        var warnings = inspection.Warnings
            .Take(MaximumWarnings)
            .Select(warning => new GatewayInspectionWarning(
                Truncate(warning.Field, MaximumScalarLength),
                Truncate(warning.Message, MaximumScalarLength)))
            .ToList();
        var description = TruncateWithWarning(
            inspection.Description,
            MaximumInspectionTextLength,
            "description",
            warnings);
        var inspectText = TruncateWithWarning(
            inspection.InspectText,
            MaximumInspectionTextLength,
            "inspectText",
            warnings);
        if (inspection.ComponentTypes.Count > MaximumComponents)
        {
            AddWarning(warnings, "componentTypes", $"Only the first {MaximumComponents} component types were returned.");
        }

        GatewayPawnDetails? pawn = null;
        if (inspection.Pawn is not null)
        {
            if (inspection.Pawn.Skills.Count > MaximumSkills)
            {
                AddWarning(warnings, "pawn.skills", $"Only the first {MaximumSkills} skills were returned.");
            }

            if (inspection.Pawn.HealthConditions.Count > MaximumHealthConditions)
            {
                AddWarning(
                    warnings,
                    "pawn.healthConditions",
                    $"Only the first {MaximumHealthConditions} health conditions were returned.");
            }

            pawn = new GatewayPawnDetails(
                Truncate(inspection.Pawn.KindDefName, MaximumScalarLength),
                Truncate(inspection.Pawn.RaceDefName, MaximumScalarLength),
                TruncateNullable(inspection.Pawn.FactionDefName, MaximumScalarLength),
                Truncate(inspection.Pawn.Gender, MaximumScalarLength),
                inspection.Pawn.BiologicalAgeYears,
                inspection.Pawn.ChronologicalAgeYears,
                inspection.Pawn.Dead,
                inspection.Pawn.Downed,
                inspection.Pawn.Drafted,
                TruncateNullable(inspection.Pawn.CurrentJobDefName, MaximumScalarLength),
                inspection.Pawn.Skills.Take(MaximumSkills)
                    .Select(skill => new GatewayPawnSkillSummary(
                        Truncate(skill.DefName, MaximumScalarLength),
                        skill.Level))
                    .ToList(),
                inspection.Pawn.HealthConditions.Take(MaximumHealthConditions)
                    .Select(condition => new GatewayPawnHealthConditionSummary(
                        Truncate(condition.Label, MaximumScalarLength),
                        Truncate(condition.DefName, MaximumScalarLength),
                        condition.Severity))
                    .ToList());
        }

        var item = inspection.Item is null
            ? null
            : new GatewayItemDetails(
                TruncateNullable(inspection.Item.StuffDefName, MaximumScalarLength),
                inspection.Item.StackLimit,
                inspection.Item.MarketValue,
                inspection.Item.Mass);
        var building = inspection.Building is null
            ? null
            : new GatewayBuildingDetails(
                inspection.Building.Width,
                inspection.Building.Height,
                Truncate(inspection.Building.Passability, MaximumScalarLength),
                inspection.Building.PowerOn);
        return new GatewayThingInspection(
            BoundSummary(inspection.Summary),
            description,
            inspectText,
            inspection.ComponentTypes.Take(MaximumComponents)
                .Select(type => Truncate(type, MaximumScalarLength))
                .ToList(),
            item,
            building,
            pawn,
            warnings.Take(MaximumWarnings).ToList());
    }

    private static GatewayThingSummary BoundSummary(GatewayThingSummary summary)
    {
        if (!IsAddressableSummary(summary))
        {
            throw new GatewayThingControlException(
                "invalid_thing_handle",
                $"Thing handles must contain 1 to {MaximumHandleLength} characters and are never truncated.");
        }

        return new GatewayThingSummary(
            summary.Handle,
            Truncate(summary.Label, MaximumScalarLength),
            Truncate(summary.DefName, MaximumScalarLength),
            Truncate(summary.RuntimeType, MaximumScalarLength),
            Truncate(summary.Kind, MaximumScalarLength),
            Truncate(summary.MapHandle, MaximumScalarLength),
            summary.Position,
            summary.OccupiedRect,
            Truncate(summary.Rotation, MaximumScalarLength),
            summary.StackCount,
            summary.Faction is null
                ? null
                : new GatewayFactionSummary(
                    Truncate(summary.Faction.Name, MaximumScalarLength),
                    Truncate(summary.Faction.DefName, MaximumScalarLength),
                    Truncate(summary.Faction.Relation, MaximumScalarLength)),
            summary.HitPoints,
            TruncateNullable(summary.Quality, MaximumScalarLength),
            summary.Fogged,
            summary.Forbidden,
            summary.Selected,
            summary.PawnFlags,
            summary.LoadId.Length <= MaximumHandleLength ? summary.LoadId : string.Empty,
            TruncateNullable(summary.StuffDefName, MaximumScalarLength));
    }

    private static string TruncateWithWarning(
        string value,
        int maximum,
        string field,
        List<GatewayInspectionWarning> warnings)
    {
        if (value.Length <= maximum)
        {
            return value;
        }

        AddWarning(warnings, field, $"Value was truncated to {maximum} characters.");
        return Truncate(value, maximum);
    }

    private static void AddWarning(
        ICollection<GatewayInspectionWarning> warnings,
        string field,
        string message)
    {
        if (warnings.Count < MaximumWarnings)
        {
            warnings.Add(new GatewayInspectionWarning(field, message));
        }
    }

    private static string Truncate(string value, int maximum)
    {
        value ??= string.Empty;
        if (value.Length <= maximum)
        {
            return value;
        }

        return maximum <= TruncatedMarker.Length
            ? TruncatedMarker.Substring(0, maximum)
            : value.Substring(0, maximum - TruncatedMarker.Length) + TruncatedMarker;
    }

    private static string? TruncateNullable(string? value, int maximum) =>
        value is null ? null : Truncate(value, maximum);

    private sealed class CandidateIndex
    {
        public CandidateIndex(string handle, IGatewayThingCandidate candidate)
        {
            Handle = handle;
            Candidate = candidate;
        }

        public string Handle { get; }

        public IGatewayThingCandidate Candidate { get; }
    }
}

public sealed class VerseGatewayThingOperations : IGatewayThingOperations
{
    private readonly GatewayLogBuffer? diagnostics;

    public VerseGatewayThingOperations(GatewayLogBuffer? diagnostics = null)
    {
        this.diagnostics = diagnostics;
    }

    public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect)
    {
        var map = Current.Game?.CurrentMap ?? throw new GatewayThingControlException(
            "map_unavailable",
            "Thing queries require a current playable map.");
        GatewayMapRect? view = null;
        if (includeViewRect && Find.CameraDriver is null)
        {
            throw new GatewayThingControlException(
                "camera_unavailable",
                "View-scoped thing queries require RimWorld's map camera.");
        }

        if (includeViewRect)
        {
            var nativeView = CellRect.ViewRect(map).ClipInsideMap(map);
            view = new GatewayMapRect(
                nativeView.minX,
                nativeView.minZ,
                nativeView.maxX,
                nativeView.maxZ);
        }

        var mapHandle = "map-" + map.uniqueID;
        var excludedUnaddressableCount = 0;
        var candidates = new List<IGatewayThingCandidate>();
        foreach (var thing in map.listerThings.AllThings.ToArray())
        {
            try
            {
                if (!thing.Spawned || thing.Destroyed || thing.Map != map)
                {
                    continue;
                }

                if (!thing.def.HasThingIDNumber ||
                    string.IsNullOrWhiteSpace(thing.ThingID) ||
                    thing.ThingID.Length > GatewayThingController.MaximumHandleLength)
                {
                    excludedUnaddressableCount++;
                    continue;
                }

                candidates.Add(new VerseThingCandidate(thing, map, mapHandle));
            }
            catch (Exception)
            {
                excludedUnaddressableCount++;
            }
        }

        return new GatewayThingWorldSnapshot(
            mapHandle,
            view,
            candidates,
            excludedUnaddressableCount);
    }

    public GatewayThingInspection CaptureInspection(string handle)
    {
        var map = Current.Game?.CurrentMap ?? throw new GatewayThingControlException(
            "map_unavailable",
            "Thing inspection requires a current playable map.");
        var thing = ResolveCurrentMapThing(map, handle);
        var mapHandle = "map-" + map.uniqueID;
        var warnings = new List<GatewayInspectionWarning>();
        var description = CaptureField(
            "description",
            warnings,
            () => thing.def.description ?? string.Empty,
            string.Empty);
        var inspectText = CaptureField(
            "inspectText",
            warnings,
            () => thing.GetInspectString() ?? string.Empty,
            string.Empty);
        var componentTypes = CaptureField(
            "componentTypes",
            warnings,
            () => thing is ThingWithComps withComps
                ? withComps.AllComps
                    .ToArray()
                    .Select(comp => comp.GetType().FullName ?? comp.GetType().Name)
                    .ToList()
                : new List<string>(),
            new List<string>());
        var item = thing.def.category == ThingCategory.Item
            ? CaptureField<GatewayItemDetails?>(
                "item",
                warnings,
                () => new GatewayItemDetails(
                    thing.Stuff?.defName,
                    thing.def.stackLimit,
                    thing.GetStatValue(StatDefOf.MarketValue),
                    thing.GetStatValue(StatDefOf.Mass)),
                null)
            : null;
        var building = thing is Building nativeBuilding
            ? CaptureField<GatewayBuildingDetails?>(
                "building",
                warnings,
                () => new GatewayBuildingDetails(
                    nativeBuilding.def.size.x,
                    nativeBuilding.def.size.z,
                    nativeBuilding.def.passability.ToString(),
                    nativeBuilding.TryGetComp<CompPowerTrader>()?.PowerOn),
                null)
            : null;
        var pawn = thing is Pawn nativePawn
            ? CapturePawn(nativePawn, warnings)
            : null;
        return new GatewayThingInspection(
            Summarize(thing, map, mapHandle),
            description,
            inspectText,
            componentTypes,
            item,
            building,
            pawn,
            warnings);
    }

    public IReadOnlyList<GatewayThingSummary> CaptureSelection()
    {
        var map = Current.Game?.CurrentMap ?? throw new GatewayThingControlException(
            "map_unavailable",
            "Semantic selection requires a current playable map.");
        var selector = Find.Selector ?? throw new GatewayThingControlException(
            "selector_unavailable",
            "RimWorld's selector is unavailable.");
        var mapHandle = "map-" + map.uniqueID;
        var candidates = new List<IGatewayThingCandidate>();
        foreach (var selected in selector.SelectedObjectsListForReading.ToArray())
        {
            try
            {
                if (selected is Thing thing &&
                    thing.Spawned &&
                    !thing.Destroyed &&
                    thing.Map == map &&
                    thing.def.HasThingIDNumber)
                {
                    candidates.Add(new VerseThingCandidate(thing, map, mapHandle));
                }
            }
            catch (Exception exception)
            {
                GatewayThingDiagnostics.ReportSelectedSummaryFailure(
                    diagnostics,
                    SelectedObjectHandle(selected),
                    exception);
            }
        }

        return new GatewayThingWorldSnapshot(
            mapHandle,
            viewRect: null,
            candidates,
            diagnostics: diagnostics).Things;
    }

    private static string SelectedObjectHandle(object selected)
    {
        try
        {
            return selected is Thing thing
                ? thing.ThingID
                : selected.GetType().FullName ?? selected.GetType().Name;
        }
        catch (Exception)
        {
            return "<unidentified>";
        }
    }

    public void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles) =>
        ApplySelectionCore(operation, handles);

    private static void ApplySelectionCore(
        GatewaySelectionOperation operation,
        IReadOnlyList<string> handles)
    {
        var map = Current.Game?.CurrentMap ?? throw new GatewayThingControlException(
            "map_unavailable",
            "Semantic selection requires a current playable map.");
        var selector = Find.Selector ?? throw new GatewayThingControlException(
            "selector_unavailable",
            "RimWorld's selector is unavailable.");
        var addressable = map.listerThings.AllThings
            .ToArray()
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map && thing.def.HasThingIDNumber)
            .ToDictionary(thing => thing.ThingID, StringComparer.Ordinal);
        var requested = new List<Thing>();
        foreach (var handle in handles)
        {
            if (!addressable.TryGetValue(handle, out var thing))
            {
                throw new GatewayThingControlException(
                    "thing_not_found",
                    $"Thing '{handle}' is not spawned on the current map.");
            }

            requested.Add(thing);
        }

        var current = selector.SelectedObjectsListForReading
            .OfType<Thing>()
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map && thing.def.HasThingIDNumber)
            .ToList();
        var final = ComputeFinalSelection(operation, current, requested);
        if (final.Count > GatewayThingController.MaximumSelectionHandles)
        {
            throw new GatewayThingControlException(
                "selection_capacity_exceeded",
                $"RimWorld cannot select more than {GatewayThingController.MaximumSelectionHandles} objects at once.");
        }

        var priorObjects = selector.SelectedObjectsListForReading.ToArray();
        try
        {
            ReplaceNativeSelection(selector, final.Cast<object>());
            var actual = selector.SelectedObjectsListForReading
                .OfType<Thing>()
                .Select(thing => thing.ThingID)
                .ToArray();
            if (!actual.SequenceEqual(final.Select(thing => thing.ThingID), StringComparer.Ordinal))
            {
                throw new GatewayThingControlException(
                    "selection_rejected",
                    "RimWorld did not retain the complete requested selection.");
            }
        }
        catch
        {
            try
            {
                ReplaceNativeSelection(selector, priorObjects);
            }
            catch
            {
                // Preserve the original mutation failure; rollback is best effort in a mutable game host.
            }

            throw;
        }
    }

    private static List<Thing> ComputeFinalSelection(
        GatewaySelectionOperation operation,
        IReadOnlyList<Thing> current,
        IReadOnlyList<Thing> requested)
    {
        var final = operation == GatewaySelectionOperation.Replace ||
                    operation == GatewaySelectionOperation.Clear
            ? new List<Thing>()
            : current.ToList();
        switch (operation)
        {
            case GatewaySelectionOperation.Replace:
            case GatewaySelectionOperation.Add:
                foreach (var thing in requested)
                {
                    if (!final.Contains(thing))
                    {
                        final.Add(thing);
                    }
                }

                break;
            case GatewaySelectionOperation.Remove:
                final.RemoveAll(requested.Contains);
                break;
            case GatewaySelectionOperation.Toggle:
                foreach (var thing in requested)
                {
                    if (!final.Remove(thing))
                    {
                        final.Add(thing);
                    }
                }

                break;
            case GatewaySelectionOperation.Clear:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        return final;
    }

    private static void ReplaceNativeSelection(Selector selector, IEnumerable<object> objects)
    {
        selector.ClearSelection();
        foreach (var value in objects)
        {
            selector.Select(value, playSound: false, forceDesignatorDeselect: true);
        }
    }

    private static PawnDetailsCapture CapturePawnScalars(Pawn pawn)
    {
        return new PawnDetailsCapture(
            pawn.kindDef?.defName ?? string.Empty,
            pawn.def.defName,
            pawn.Faction?.def?.defName,
            pawn.gender.ToString(),
            pawn.ageTracker?.AgeBiologicalYearsFloat ?? 0f,
            pawn.ageTracker?.AgeChronologicalYearsFloat ?? 0f,
            pawn.Dead,
            pawn.Downed,
            pawn.Drafted,
            pawn.CurJobDef?.defName);
    }

    private static GatewayPawnDetails CapturePawn(
        Pawn pawn,
        List<GatewayInspectionWarning> warnings)
    {
        var scalar = CaptureField(
            "pawn",
            warnings,
            () => CapturePawnScalars(pawn),
            new PawnDetailsCapture(string.Empty, string.Empty, null, string.Empty, 0f, 0f, false, false, false, null));
        var skills = CaptureField(
            "pawn.skills",
            warnings,
            () => pawn.skills?.skills?.ToArray()
                .Select(skill => new GatewayPawnSkillSummary(skill.def.defName, skill.Level))
                .ToList() ?? new List<GatewayPawnSkillSummary>(),
            new List<GatewayPawnSkillSummary>());
        var health = CaptureField(
            "pawn.healthConditions",
            warnings,
            () => pawn.health?.hediffSet?.hediffs?.ToArray()
                .Select(condition => new GatewayPawnHealthConditionSummary(
                    condition.LabelCap.ToString(),
                    condition.def.defName,
                    condition.Severity))
                .ToList() ?? new List<GatewayPawnHealthConditionSummary>(),
            new List<GatewayPawnHealthConditionSummary>());
        return new GatewayPawnDetails(
            scalar.KindDefName,
            scalar.RaceDefName,
            scalar.FactionDefName,
            scalar.Gender,
            scalar.BiologicalAgeYears,
            scalar.ChronologicalAgeYears,
            scalar.Dead,
            scalar.Downed,
            scalar.Drafted,
            scalar.CurrentJobDefName,
            skills,
            health);
    }

    private static T CaptureField<T>(
        string field,
        ICollection<GatewayInspectionWarning> warnings,
        Func<T> capture,
        T fallback)
    {
        try
        {
            return capture();
        }
        catch (Exception exception)
        {
            warnings.Add(new GatewayInspectionWarning(
                field,
                exception.GetType().Name + ": " + exception.Message));
            return fallback;
        }
    }

    private static Thing ResolveCurrentMapThing(Map map, string handle)
    {
        var things = map.listerThings.AllThings
            .ToArray()
            .Where(thing => thing.Spawned && !thing.Destroyed && thing.Map == map && thing.def.HasThingIDNumber)
            .ToArray();
        var thing = things.FirstOrDefault(candidate =>
            string.Equals(candidate.ThingID, handle, StringComparison.Ordinal));
        thing ??= things.FirstOrDefault(candidate =>
            string.Equals(candidate.GetUniqueLoadID(), handle, StringComparison.Ordinal));
        return thing ?? throw new GatewayThingControlException(
            "thing_not_found",
            $"Thing '{handle}' is not spawned on the current map.");
    }

    private static GatewayThingSummary Summarize(Thing thing, Map map, string mapHandle)
    {
        var occupied = GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.size);
        GatewayFactionSummary? faction = null;
        if (thing.Faction is not null)
        {
            var player = Faction.OfPlayer;
            var relation = ReferenceEquals(thing.Faction, player)
                ? "Player"
                : player is null ? string.Empty : thing.Faction.RelationKindWith(player).ToString();
            faction = new GatewayFactionSummary(
                thing.Faction.Name ?? string.Empty,
                thing.Faction.def?.defName ?? string.Empty,
                relation);
        }

        GatewayHitPointSummary? hitPoints = thing.def.useHitPoints
            ? new GatewayHitPointSummary(thing.HitPoints, thing.MaxHitPoints)
            : null;
        var quality = thing.TryGetComp<CompQuality>()?.Quality.ToString();
        var pawn = thing as Pawn;
        var pawnFlags = pawn is null
            ? null
            : new GatewayPawnFlags(
                pawn.IsColonist,
                pawn.IsPrisonerOfColony,
                pawn.RaceProps.Humanlike,
                pawn.RaceProps.Animal,
                pawn.RaceProps.IsMechanoid);
        return new GatewayThingSummary(
            thing.ThingID,
            thing.LabelCap.ToString(),
            thing.def.defName,
            thing.GetType().FullName ?? thing.GetType().Name,
            Classify(thing),
            mapHandle,
            new GatewayMapCell(thing.Position.x, thing.Position.z),
            new GatewayMapRect(occupied.minX, occupied.minZ, occupied.maxX, occupied.maxZ),
            thing.Rotation.ToString(),
            thing.stackCount,
            faction,
            hitPoints,
            quality,
            thing.Position.Fogged(map),
            thing.IsForbidden(Faction.OfPlayer),
            Find.Selector?.IsSelected(thing) == true,
            pawnFlags,
            thing.GetUniqueLoadID(),
            thing.Stuff?.defName);
    }

    private static string Classify(Thing thing)
    {
        if (thing is Pawn)
        {
            return "pawn";
        }

        if (thing is Building)
        {
            return "building";
        }

        if (thing is Plant)
        {
            return "plant";
        }

        if (thing is Corpse)
        {
            return "corpse";
        }

        if (thing is Filth)
        {
            return "filth";
        }

        return thing.def.category == ThingCategory.Item
            ? "item"
            : thing.def.category.ToString().ToLowerInvariant();
    }

    private sealed class VerseThingCandidate : IGatewayThingCandidate
    {
        private readonly Thing thing;
        private readonly Map map;
        private readonly string mapHandle;

        public VerseThingCandidate(Thing thing, Map map, string mapHandle)
        {
            this.thing = thing;
            this.map = map;
            this.mapHandle = mapHandle;
        }

        public string Handle => thing.ThingID;

        public GatewayMapRect OccupiedRect
        {
            get
            {
                var occupied = GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.size);
                return new GatewayMapRect(
                    occupied.minX,
                    occupied.minZ,
                    occupied.maxX,
                    occupied.maxZ);
            }
        }

        public GatewayThingSummary Capture() => Summarize(thing, map, mapHandle);
    }

    private sealed class PawnDetailsCapture
    {
        public PawnDetailsCapture(
            string kindDefName,
            string raceDefName,
            string? factionDefName,
            string gender,
            float biologicalAgeYears,
            float chronologicalAgeYears,
            bool dead,
            bool downed,
            bool drafted,
            string? currentJobDefName)
        {
            KindDefName = kindDefName;
            RaceDefName = raceDefName;
            FactionDefName = factionDefName;
            Gender = gender;
            BiologicalAgeYears = biologicalAgeYears;
            ChronologicalAgeYears = chronologicalAgeYears;
            Dead = dead;
            Downed = downed;
            Drafted = drafted;
            CurrentJobDefName = currentJobDefName;
        }

        public string KindDefName { get; }
        public string RaceDefName { get; }
        public string? FactionDefName { get; }
        public string Gender { get; }
        public float BiologicalAgeYears { get; }
        public float ChronologicalAgeYears { get; }
        public bool Dead { get; }
        public bool Downed { get; }
        public bool Drafted { get; }
        public string? CurrentJobDefName { get; }
    }
}
