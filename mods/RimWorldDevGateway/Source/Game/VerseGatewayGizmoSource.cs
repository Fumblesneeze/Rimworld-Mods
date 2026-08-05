using System.Collections.ObjectModel;
using System.Globalization;
using RimWorld;
using Verse;

namespace RimWorldDevGateway;

/// <summary>
/// Discovers only native gizmo contracts that can be invoked without fabricating an Event.current.
/// All members must be called on RimWorld's main thread.
/// </summary>
public sealed class VerseGatewayGizmoSource : IGatewayGizmoSource
{
    private readonly GatewayLogBuffer? diagnostics;

    public VerseGatewayGizmoSource(GatewayLogBuffer? diagnostics = null)
    {
        this.diagnostics = diagnostics;
    }

    public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var map = Current.Game?.CurrentMap ?? throw new GatewayGizmoException(
            "map_unavailable",
            "Gizmo discovery requires a current playable map.");
        var owners = ResolveOwners(query, map);
        var candidates = new List<IGatewayGizmoCandidate>();
        var truncated = false;
        DiscoverOwnersIndependently(
            owners,
            owner =>
            {
                AddOwnerGizmos(query, map, owner, candidates, ref truncated);
                return !truncated;
            },
            owner => owner.ThingID);

        if (!truncated)
        {
            AddArchitectDesignators(query, map, candidates, ref truncated);
        }

        return new GatewayGizmoDiscovery(
            "map-" + map.uniqueID.ToString(CultureInfo.InvariantCulture),
            new ReadOnlyCollection<string>(owners.Select(owner => owner.ThingID).ToList()),
            new ReadOnlyCollection<IGatewayGizmoCandidate>(candidates),
            truncated);
    }

    internal void DiscoverOwnersIndependently<T>(
        IEnumerable<T> owners,
        Func<T, bool> discover,
        Func<T, string> identify)
    {
        foreach (var owner in owners)
        {
            try
            {
                if (!discover(owner))
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                ReportOwnerDiscoveryFailure(identify(owner), exception);
            }
        }
    }

    private void ReportOwnerDiscoveryFailure(string ownerHandle, Exception exception)
    {
        if (diagnostics is null)
        {
            return;
        }

        var requestId = GatewayRequestScope.CurrentRequestId;
        diagnostics.Append(
            "Warning",
            $"Gizmo discovery skipped owner '{ownerHandle}' because GetGizmos or a reverse-designator provider threw.",
            exception.ToString(),
            requestId: requestId);
    }

    private static List<Thing> ResolveOwners(GatewayGizmoSourceQuery query, Map map)
    {
        if (query.OwnerScope == GatewayGizmoOwnerScope.Selection)
        {
            var selectedOwners = (Find.Selector?.SelectedObjects ?? new List<object>())
                .OfType<Thing>()
                .Where(thing =>
                    thing.Spawned && !thing.Destroyed && thing.Map == map &&
                    thing.def.HasThingIDNumber)
                .Take(GatewayGizmoRegistry.MaximumOwners + 1)
                .ToList();
            if (selectedOwners.Count > GatewayGizmoRegistry.MaximumOwners)
            {
                throw new GatewayGizmoException(
                    "too_many_gizmo_owners",
                    $"Selection gizmo discovery accepts at most {GatewayGizmoRegistry.MaximumOwners} Thing owners.");
            }

            return selectedOwners;
        }

        if (query.OwnerHandles.Count > GatewayGizmoRegistry.MaximumOwners)
        {
            throw new GatewayGizmoException(
                "too_many_gizmo_owners",
                $"A gizmo query accepts at most {GatewayGizmoRegistry.MaximumOwners} owners.");
        }

        var addressable = map.listerThings.AllThings
            .ToArray()
            .Where(thing =>
                thing.Spawned && !thing.Destroyed && thing.Map == map &&
                thing.def.HasThingIDNumber)
            .ToArray();
        var owners = new List<Thing>(query.OwnerHandles.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in query.OwnerHandles)
        {
            if (string.IsNullOrWhiteSpace(handle))
            {
                continue;
            }

            var thing = ResolveThing(addressable, handle);
            if (thing is null)
            {
                throw new GatewayGizmoException(
                    "gizmo_owner_not_found",
                    $"Current-map gizmo owner '{handle}' was not found.");
            }

            if (seen.Add(thing.ThingID))
            {
                owners.Add(thing);
            }
        }

        return owners;
    }

    private static void AddOwnerGizmos(
        GatewayGizmoSourceQuery query,
        Map map,
        Thing owner,
        ICollection<IGatewayGizmoCandidate> target,
        ref bool truncated)
    {
        var source = query.OwnerScope == GatewayGizmoOwnerScope.Selection
            ? GatewayGizmoSource.Selection
            : GatewayGizmoSource.ExplicitOwner;
        var ownerHandles = new[] { owner.ThingID };
        var ordinal = 0;
        try
        {
            foreach (var gizmo in owner.GetGizmos())
            {
                if (gizmo is not null && gizmo.Visible &&
                    !TryAdd(query, map, gizmo, source, ownerHandles,
                        "owner:" + owner.ThingID + ":" + ordinal, target))
                {
                    truncated = true;
                    return;
                }

                ordinal++;
            }

            foreach (var designator in Find.ReverseDesignatorDatabase.AllDesignators)
            {
                var gizmo = designator.CreateReverseDesignationGizmo(owner);
                if (gizmo is not null && gizmo.Visible &&
                    !TryAdd(query, map, gizmo, source, ownerHandles,
                        "reverse:" + owner.ThingID + ":" + ordinal, target))
                {
                    truncated = true;
                    return;
                }

                ordinal++;
            }
        }
        catch (Exception exception)
        {
            throw DiscoveryFailure(owner.ThingID, exception);
        }
    }

    private static void AddArchitectDesignators(
        GatewayGizmoSourceQuery query,
        Map map,
        ICollection<IGatewayGizmoCandidate> target,
        ref bool truncated)
    {
        var categoryNames = query.ArchitectCategoryDefNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var categoryName in categoryNames)
        {
            var category = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryName);
            if (category is null)
            {
                throw new GatewayGizmoException(
                    "architect_category_not_found",
                    $"Architect category '{categoryName}' was not found.");
            }

            var ordinal = 0;
            try
            {
                foreach (var designator in category.ResolvedAllowedDesignators)
                {
                    if (designator is Designator_Dropdown dropdown)
                    {
                        var elementOrdinal = 0;
                        foreach (var element in dropdown.Elements)
                        {
                            if (element.Visible && !TryAdd(
                                    query,
                                    map,
                                    element,
                                    GatewayGizmoSource.Architect,
                                    Array.Empty<string>(),
                                    "architect:" + categoryName + ":" + ordinal +
                                    ":element:" + elementOrdinal,
                                    target))
                            {
                                truncated = true;
                                return;
                            }

                            elementOrdinal++;
                        }
                    }
                    else if (designator.Visible && !TryAdd(
                                 query,
                                 map,
                                 designator,
                                 GatewayGizmoSource.Architect,
                                 Array.Empty<string>(),
                                 "architect:" + categoryName + ":" + ordinal,
                                 target))
                    {
                        truncated = true;
                        return;
                    }

                    ordinal++;
                }
            }
            catch (Exception exception)
            {
                throw DiscoveryFailure("architect category " + categoryName, exception);
            }
        }
    }

    private static bool TryAdd(
        GatewayGizmoSourceQuery query,
        Map map,
        Gizmo gizmo,
        GatewayGizmoSource source,
        IReadOnlyList<string> owners,
        string ordinalIdentity,
        ICollection<IGatewayGizmoCandidate> target)
    {
        if (target.Count >= query.MaximumCandidates)
        {
            return false;
        }

        var identity = ordinalIdentity + "|" + NativeIdentity(gizmo);
        target.Add(new VerseCandidate(map, gizmo, source, owners, identity));
        return true;
    }

    private static string NativeIdentity(Gizmo gizmo)
    {
        var command = gizmo as Command;
        var identity = gizmo.GetType().FullName + "|" + (command?.Label ?? string.Empty) + "|" +
            gizmo.Order.ToString("R", CultureInfo.InvariantCulture) + "|" +
            (command?.hotKey?.defName ?? string.Empty) + "|" +
            (command?.groupKey.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (gizmo is Designator_Build build)
        {
            identity += "|build:" + build.PlacingDef?.defName;
        }

        return identity;
    }

    private static GatewayGizmoException DiscoveryFailure(string source, Exception exception)
    {
        var message = exception.Message ?? exception.GetType().Name;
        if (message.Length > 512)
        {
            message = message.Substring(0, 512);
        }

        return new GatewayGizmoException(
            "gizmo_discovery_failed",
            $"Gizmo discovery failed for {source}: {message}",
            exception);
    }

    private sealed class VerseCandidate : IGatewayGizmoCandidate
    {
        private static readonly IReadOnlyList<GatewayInteractionInputKind> NoInputs =
            Array.Empty<GatewayInteractionInputKind>();
        private static readonly IReadOnlyList<GatewayInteractionInputKind> TargetInputs =
            new[] { GatewayInteractionInputKind.Thing, GatewayInteractionInputKind.Cell };
        private static readonly IReadOnlyList<GatewayInteractionInputKind> PlacementInputs =
            new[] { GatewayInteractionInputKind.Cell };
        private static readonly IReadOnlyList<GatewayInteractionInputKind> DragInputs =
            new[]
            {
                GatewayInteractionInputKind.Cell,
                GatewayInteractionInputKind.Cells,
                GatewayInteractionInputKind.Line,
                GatewayInteractionInputKind.Rectangle
            };

        private readonly Map map;
        private readonly Gizmo gizmo;
        private readonly GatewayGizmoSource source;
        private readonly IReadOnlyList<string> owners;
        private readonly string identity;

        public VerseCandidate(
            Map map,
            Gizmo gizmo,
            GatewayGizmoSource source,
            IReadOnlyList<string> owners,
            string identity)
        {
            this.map = map;
            this.gizmo = gizmo;
            this.source = source;
            this.owners = owners;
            this.identity = identity;
        }

        public GatewayGizmoCandidateSnapshot Capture()
        {
            try
            {
                var command = gizmo as Command;
                var kind = KindOf(gizmo);
                return new GatewayGizmoCandidateSnapshot(
                    identity,
                    source,
                    owners,
                    gizmo.GetType().FullName ?? gizmo.GetType().Name,
                    Bound(command?.Label ?? gizmo.GetType().Name, 256),
                    Bound(command?.Desc ?? string.Empty, 1024),
                    gizmo.Order,
                    gizmo.Disabled,
                    BoundNullable(gizmo.disabledReason, 512),
                    command?.hotKey?.defName,
                    command?.groupKey ?? -1,
                    kind,
                    gizmo is Command_Toggle toggle && toggle.isActive is not null
                        ? toggle.isActive()
                        : null,
                    InputsFor(gizmo, kind),
                    (gizmo as Designator_Build)?.PlacingDef?.defName);
            }
            catch (Exception exception)
            {
                throw DiscoveryFailure(identity, exception);
            }
        }

        public void Invoke()
        {
            switch (gizmo)
            {
                case Command_Action action when action.action is not null:
                    action.action();
                    return;
                case Command_Toggle toggle when toggle.toggleAction is not null:
                    toggle.toggleAction();
                    return;
                default:
                    throw new GatewayGizmoException(
                        "unsupported_gizmo",
                        $"Gizmo type '{gizmo.GetType().FullName}' has no safe direct adapter.");
            }
        }

        public GatewayTargetAcceptance Preflight(GatewayInteractionTarget target)
        {
            if (gizmo is Command_Target command)
            {
                if (command.targetingParams is null || command.action is null)
                {
                    return new GatewayTargetAcceptance(false, "The target command is incomplete.");
                }

                var targetInfo = ResolveTargetInfo(target, out var failure);
                if (failure is not null)
                {
                    return new GatewayTargetAcceptance(false, failure);
                }

                return command.targetingParams.CanTarget(targetInfo)
                    ? new GatewayTargetAcceptance(true)
                    : new GatewayTargetAcceptance(false, "RimWorld's target validator rejected the target.");
            }

            if (gizmo is Designator designator && target.Kind == GatewayInteractionTargetKind.Cell)
            {
                var cell = ToIntVec3(target.Cell!);
                if (!cell.InBounds(map))
                {
                    return new GatewayTargetAcceptance(false, "The cell is outside the current map.");
                }

                var report = designator.CanDesignateCell(cell);
                return report.Accepted
                    ? new GatewayTargetAcceptance(true)
                    : new GatewayTargetAcceptance(false, report.Reason);
            }

            return new GatewayTargetAcceptance(false, "The gizmo does not accept this target kind.");
        }

        public GatewayNativeApplyResult Apply(IReadOnlyList<GatewayInteractionTarget> targets)
        {
            if (gizmo is Command_Target command)
            {
                if (targets.Count != 1)
                {
                    throw new GatewayGizmoException(
                        "interaction_target_mismatch",
                        "A native target command accepts exactly one thing or cell.");
                }

                var targetInfo = ResolveTargetInfo(targets[0], out var failure);
                if (failure is not null || command.action is null)
                {
                    throw new GatewayGizmoException(
                        "target_rejected",
                        failure ?? "The target command is incomplete.");
                }

                command.action((LocalTargetInfo)targetInfo);
                return new GatewayNativeApplyResult(completed: true);
            }

            if (gizmo is Designator designator)
            {
                designator.DesignateMultiCell(targets.Select(target => ToIntVec3(target.Cell!)));
                return new GatewayNativeApplyResult(completed: true);
            }

            throw new GatewayGizmoException(
                "unsupported_gizmo",
                $"Gizmo type '{gizmo.GetType().FullName}' has no safe interaction adapter.");
        }

        public void Cancel()
        {
            // Semantic adapters do not select RimWorld's global Targeter or DesignatorManager.
        }

        private TargetInfo ResolveTargetInfo(
            GatewayInteractionTarget target,
            out string? failure)
        {
            failure = null;
            if (target.Kind == GatewayInteractionTargetKind.Thing)
            {
                var thing = ResolveThing(
                    map.listerThings.AllThings
                        .ToArray()
                        .Where(candidate =>
                            candidate.Spawned && !candidate.Destroyed && candidate.Map == map &&
                            candidate.def.HasThingIDNumber),
                    target.ThingHandle!);
                if (thing is null)
                {
                    failure = "The target thing no longer exists on the current map.";
                    return TargetInfo.Invalid;
                }

                return new TargetInfo(thing);
            }

            var cell = ToIntVec3(target.Cell!);
            if (!cell.InBounds(map))
            {
                failure = "The target cell is outside the current map.";
                return TargetInfo.Invalid;
            }

            return new TargetInfo(cell, map);
        }

        private static GatewayGizmoInteractionKind KindOf(Gizmo value)
        {
            if (value is Command_Action action && action.action is not null)
            {
                return GatewayGizmoInteractionKind.Immediate;
            }

            if (value is Command_Toggle toggle &&
                toggle.toggleAction is not null && toggle.isActive is not null)
            {
                return GatewayGizmoInteractionKind.Toggle;
            }

            if (value is Command_Target target &&
                target.action is not null && target.targetingParams is not null)
            {
                return GatewayGizmoInteractionKind.Target;
            }

            if (value is Designator designator)
            {
                return SupportsDrag(designator)
                    ? GatewayGizmoInteractionKind.Drag
                    : GatewayGizmoInteractionKind.Placement;
            }

            return GatewayGizmoInteractionKind.Unsupported;
        }

        private static IReadOnlyList<GatewayInteractionInputKind> InputsFor(
            Gizmo value,
            GatewayGizmoInteractionKind kind) => kind switch
            {
                GatewayGizmoInteractionKind.Target => TargetInputs,
                GatewayGizmoInteractionKind.Placement => PlacementInputs,
                GatewayGizmoInteractionKind.Drag when value is Designator => DragInputs,
                _ => NoInputs
            };

        private static bool SupportsDrag(Designator designator) =>
            designator.DrawStyleCategory?.styles?.Any(style => !style.DrawStyleWorker.SingleCell) == true;

        private static IntVec3 ToIntVec3(GatewayMapCell cell) => new(cell.X, 0, cell.Z);

        private static string Bound(string? value, int maximumLength)
        {
            var text = value ?? string.Empty;
            return text.Length <= maximumLength ? text : text.Substring(0, maximumLength);
        }

        private static string? BoundNullable(string? value, int maximumLength) =>
            value is null ? null : Bound(value, maximumLength);
    }

    private static Thing? ResolveThing(IEnumerable<Thing> candidates, string handle) =>
        candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.ThingID, handle, StringComparison.Ordinal)) ??
        candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.GetUniqueLoadID(), handle, StringComparison.Ordinal));
}
