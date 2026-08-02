using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using LudeonTK;

namespace RimWorldDevGateway;

public enum GatewayDebugActionMode
{
    Immediate,
    MapPointer,
    PawnPointer,
    WorldPointer,
    Unsupported
}

public sealed class GatewayDebugActionException : Exception
{
    public GatewayDebugActionException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

public sealed class GatewayDebugActionCandidateSnapshot
{
    public GatewayDebugActionCandidateSnapshot(
        string identity,
        string path,
        string label,
        string category,
        string allowedGameStates,
        string runtimeType,
        GatewayDebugActionMode mode,
        bool visible,
        bool active,
        bool on,
        string? sourceType = null,
        string? discoveryError = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Category = category ?? string.Empty;
        AllowedGameStates = allowedGameStates ?? string.Empty;
        RuntimeType = runtimeType ?? throw new ArgumentNullException(nameof(runtimeType));
        SourceType = sourceType ?? string.Empty;
        DiscoveryError = discoveryError;
        Mode = mode;
        Visible = visible;
        Active = active;
        On = on;
    }

    public string Identity { get; }
    public string Path { get; }
    public string Label { get; }
    public string Category { get; }
    public string AllowedGameStates { get; }
    public string RuntimeType { get; }
    public string SourceType { get; }
    public string? DiscoveryError { get; }
    public GatewayDebugActionMode Mode { get; }
    public bool Visible { get; }
    public bool Active { get; }
    public bool On { get; }
}

public interface IGatewayDebugActionCandidate
{
    GatewayDebugActionCandidateSnapshot Capture();

    void Invoke();
}

public interface IGatewayDebugActionSource
{
    IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates);
}

public sealed class GatewayDebugActionQuery
{
    public GatewayDebugActionQuery(
        string? search,
        IEnumerable<string>? categories,
        IEnumerable<GatewayDebugActionMode>? modes,
        string? after,
        int limit,
        IEnumerable<string>? allowedGameStates = null)
    {
        if (search is not null && search.Length > 256)
        {
            throw new GatewayDebugActionException(
                "debug_action_filter_too_large",
                "Debug-action search text is limited to 256 characters.");
        }

        if (limit < 1 || limit > 1000)
        {
            throw new GatewayDebugActionException(
                "debug_action_limit_out_of_bounds",
                "Debug-action limit must be between 1 and 1000.");
        }

        Search = search;
        Categories = CopyBounded(categories, "categories");
        Modes = new ReadOnlyCollection<GatewayDebugActionMode>(
            (modes ?? Array.Empty<GatewayDebugActionMode>()).Distinct().ToArray());
        AllowedGameStates = CopyBounded(allowedGameStates, "allowedGameStates");
        After = after;
        Limit = limit;
    }

    public string? Search { get; }
    public IReadOnlyList<string> Categories { get; }
    public IReadOnlyList<GatewayDebugActionMode> Modes { get; }
    public IReadOnlyList<string> AllowedGameStates { get; }
    public string? After { get; }
    public int Limit { get; }

    private static IReadOnlyList<string> CopyBounded(IEnumerable<string>? values, string name)
    {
        var copy = (values ?? Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (copy.Length > 64)
        {
            throw new GatewayDebugActionException(
                "debug_action_filter_too_large",
                $"Debug-action filter '{name}' is limited to 64 values.");
        }

        return new ReadOnlyCollection<string>(copy);
    }
}

public sealed class GatewayDebugActionDescriptor
{
    public GatewayDebugActionDescriptor(string handle, GatewayDebugActionCandidateSnapshot snapshot)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        Path = snapshot.Path;
        Label = snapshot.Label;
        Category = snapshot.Category;
        AllowedGameStates = snapshot.AllowedGameStates;
        RuntimeType = snapshot.RuntimeType;
        SourceType = snapshot.SourceType;
        DiscoveryError = snapshot.DiscoveryError;
        Mode = snapshot.Mode;
        Visible = snapshot.Visible;
        Active = snapshot.Active;
        On = snapshot.On;
    }

    public string Handle { get; }
    public string Path { get; }
    public string Label { get; }
    public string Category { get; }
    public string AllowedGameStates { get; }
    public string RuntimeType { get; }
    public string SourceType { get; }
    public string? DiscoveryError { get; }
    public GatewayDebugActionMode Mode { get; }
    public bool Visible { get; }
    public bool Active { get; }
    public bool On { get; }
}

public sealed class GatewayDebugActionPage
{
    public GatewayDebugActionPage(
        IReadOnlyList<GatewayDebugActionDescriptor> items,
        bool pageTruncated)
    {
        Items = new ReadOnlyCollection<GatewayDebugActionDescriptor>(items.ToArray());
        PageTruncated = pageTruncated;
    }

    public IReadOnlyList<GatewayDebugActionDescriptor> Items { get; }
    public bool PageTruncated { get; }
}

public sealed class GatewayDebugActionInvocationResult
{
    public GatewayDebugActionInvocationResult(
        string handle,
        string path,
        GatewayDebugActionMode mode,
        bool completed,
        bool pointerRequired)
    {
        Handle = handle;
        Path = path;
        Mode = mode;
        Completed = completed;
        PointerRequired = pointerRequired;
    }

    public string Handle { get; }
    public string Path { get; }
    public GatewayDebugActionMode Mode { get; }
    public bool Completed { get; }
    public bool PointerRequired { get; }
}

/// <summary>
/// Discovers ephemeral native debug leaves and always re-resolves a handle before invocation.
/// </summary>
public sealed class GatewayDebugActionRegistry
{
    private const int MaximumDiscoveryCandidates = 10_000;
    private readonly IGatewayDebugActionSource source;

    public GatewayDebugActionRegistry(IGatewayDebugActionSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public GatewayDebugActionPage Query(GatewayDebugActionQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var all = Describe(source.Discover(MaximumDiscoveryCandidates))
            .Where(item => Matches(item, query))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Handle, StringComparer.Ordinal)
            .ToArray();
        var start = 0;
        if (!string.IsNullOrEmpty(query.After))
        {
            var found = Array.FindIndex(all, item =>
                string.Equals(item.Handle, query.After, StringComparison.Ordinal));
            if (found < 0)
            {
                throw new GatewayDebugActionException(
                    "stale_debug_action_cursor",
                    "The debug-action pagination cursor no longer exists in this filtered tree.");
            }

            start = found + 1;
        }

        var items = all.Skip(start).Take(query.Limit).ToArray();
        return new GatewayDebugActionPage(items, start + items.Length < all.Length);
    }

    public GatewayDebugActionInvocationResult Invoke(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
        {
            throw new GatewayDebugActionException(
                "invalid_debug_action_handle",
                "A debug-action handle is required.");
        }

        var matches = source.Discover(MaximumDiscoveryCandidates)
            .Select(candidate => new CandidateWithDescriptor(candidate, Describe(candidate.Capture())))
            .Where(item => string.Equals(item.Descriptor.Handle, handle, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new GatewayDebugActionException(
                "stale_debug_action_handle",
                "The debug-action handle no longer resolves to exactly one native leaf.");
        }

        var match = matches[0];
        if (!match.Descriptor.Visible || !match.Descriptor.Active)
        {
            throw new GatewayDebugActionException(
                "debug_action_unavailable",
                $"Debug action '{match.Descriptor.Path}' is not currently visible and active.");
        }

        if (match.Descriptor.Mode == GatewayDebugActionMode.Unsupported)
        {
            throw new GatewayDebugActionException(
                "debug_action_unsupported",
                $"Debug action '{match.Descriptor.Path}' has an unsupported native action type.");
        }

        match.Candidate.Invoke();
        var pointerRequired = match.Descriptor.Mode != GatewayDebugActionMode.Immediate;
        return new GatewayDebugActionInvocationResult(
            handle,
            match.Descriptor.Path,
            match.Descriptor.Mode,
            completed: !pointerRequired,
            pointerRequired: pointerRequired);
    }

    private static GatewayDebugActionDescriptor[] Describe(
        IEnumerable<IGatewayDebugActionCandidate> candidates) =>
        candidates.Select(candidate => Describe(candidate.Capture())).ToArray();

    private static GatewayDebugActionDescriptor Describe(GatewayDebugActionCandidateSnapshot snapshot) =>
        new(CreateHandle(snapshot), snapshot);

    private static bool Matches(GatewayDebugActionDescriptor item, GatewayDebugActionQuery query)
    {
        if (!string.IsNullOrEmpty(query.Search) &&
            item.Path.IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) < 0 &&
            item.Label.IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (query.Categories.Count > 0 && !query.Categories.Contains(item.Category, StringComparer.Ordinal))
        {
            return false;
        }

        if (query.Modes.Count > 0 && !query.Modes.Contains(item.Mode))
        {
            return false;
        }

        return query.AllowedGameStates.Count == 0 ||
            query.AllowedGameStates.Contains(item.AllowedGameStates, StringComparer.Ordinal);
    }

    private static string CreateHandle(GatewayDebugActionCandidateSnapshot snapshot)
    {
        var fingerprint = string.Join("\n", new[]
        {
            snapshot.Identity,
            snapshot.Path,
            snapshot.Category,
            snapshot.AllowedGameStates,
            snapshot.RuntimeType,
            snapshot.SourceType,
            snapshot.Mode.ToString()
        });
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
        return "debug-" + Convert.ToBase64String(hash, 0, 18)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class CandidateWithDescriptor
    {
        public CandidateWithDescriptor(
            IGatewayDebugActionCandidate candidate,
            GatewayDebugActionDescriptor descriptor)
        {
            Candidate = candidate;
            Descriptor = descriptor;
        }

        public IGatewayDebugActionCandidate Candidate { get; }
        public GatewayDebugActionDescriptor Descriptor { get; }
    }
}

public sealed class VerseGatewayDebugActionSource : IGatewayDebugActionSource
{
    private static readonly FieldInfo RootNodeField = typeof(Dialog_Debug).GetField(
        "rootNode",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
        throw new MissingFieldException(typeof(Dialog_Debug).FullName, "rootNode");
    private static readonly MethodInfo SetupMethod = typeof(Dialog_Debug).GetMethod(
        "TrySetupNodeGraph",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
        throw new MissingMethodException(typeof(Dialog_Debug).FullName, "TrySetupNodeGraph");

    public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates)
    {
        if (maximumCandidates < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCandidates));
        }

        try
        {
            SetupMethod.Invoke(null, null);
            var root = RootNodeField.GetValue(null) as DebugActionNode ??
                throw new InvalidOperationException(
                    "RimWorld did not create its debug-action root node.");
            return DiscoverBreadthFirst(root, maximumCandidates);
        }
        catch (GatewayDebugActionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: not null }
                ? exception.InnerException
                : exception;
            throw new GatewayDebugActionException(
                "debug_action_discovery_failed",
                "RimWorld could not initialize its native debug-action tree: " +
                cause!.GetType().Name + ": " + cause.Message,
                cause);
        }
    }

    internal static IReadOnlyList<IGatewayDebugActionCandidate> DiscoverBreadthFirst(
        DebugActionNode root,
        int maximumCandidates)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (maximumCandidates < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCandidates));
        }

        var leaves = new List<IGatewayDebugActionCandidate>();
        var seen = new HashSet<DebugActionNode> { root };
        var pending = new List<DebugActionNode> { root };
        var next = 0;
        while (next < pending.Count && leaves.Count < maximumCandidates)
        {
            var node = pending[next++];
            var children = node.children;
            if (children is null || children.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(node.Path))
                {
                    leaves.Add(new VerseDebugActionCandidate(node, leaves.Count));
                }

                continue;
            }

            foreach (var child in children)
            {
                if (seen.Add(child))
                {
                    pending.Add(child);
                }
            }
        }

        return new ReadOnlyCollection<IGatewayDebugActionCandidate>(leaves);
    }

    private sealed class VerseDebugActionCandidate : IGatewayDebugActionCandidate
    {
        private readonly DebugActionNode node;
        private readonly string identity;
        private readonly bool lazySubmenu;

        public VerseDebugActionCandidate(DebugActionNode node, int ordinal)
        {
            this.node = node;
            lazySubmenu = node.childGetter is not null &&
                (node.children is null || node.children.Count == 0);
            identity = string.Join("|", new[]
            {
                ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                node.Path,
                node.actionType.ToString(),
                DelegateIdentity(node.action),
                DelegateIdentity(node.pawnAction),
                DelegateIdentity(node.childGetter)
            });
        }

        public GatewayDebugActionCandidateSnapshot Capture()
        {
            var source = node.sourceAttribute;
            var errors = new List<string>();
            var label = CaptureValue(
                "label",
                () => node.LabelNow,
                node.label ?? node.Path,
                errors);
            var visible = CaptureValue("visibility", () => node.VisibleNow, false, errors);
            var active = CaptureValue("availability", () => node.ActiveNow, false, errors);
            var on = CaptureValue("toggle state", () => node.On, false, errors);
            return new GatewayDebugActionCandidateSnapshot(
                identity,
                node.Path,
                label,
                node.category ?? string.Empty,
                source?.allowedGameStates.ToString() ?? string.Empty,
                node.GetType().FullName ?? node.GetType().Name,
                ToMode(node, lazySubmenu),
                visible,
                active,
                on,
                ActionSourceType(node),
                errors.Count == 0 ? null : BoundError(errors));
        }

        public void Invoke()
        {
            if (node.actionType == DebugActionType.Action)
            {
                var action = node.action ?? throw new GatewayDebugActionException(
                    "debug_action_unsupported",
                    $"Debug action '{node.Path}' has no immediate delegate.");
                action();
                return;
            }

            node.Enter(null!);
        }

        private static GatewayDebugActionMode ToMode(
            DebugActionNode value,
            bool isLazySubmenu)
        {
            if (isLazySubmenu)
            {
                return GatewayDebugActionMode.Unsupported;
            }

            return value.actionType switch
            {
                DebugActionType.Action when value.action is not null =>
                    GatewayDebugActionMode.Immediate,
                DebugActionType.ToolMap when value.action is not null =>
                    GatewayDebugActionMode.MapPointer,
                DebugActionType.ToolMapForPawns when value.pawnAction is not null =>
                    GatewayDebugActionMode.PawnPointer,
                DebugActionType.ToolWorld when value.action is not null =>
                    GatewayDebugActionMode.WorldPointer,
                _ => GatewayDebugActionMode.Unsupported
            };
        }

        private static string DelegateIdentity(Delegate? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            var method = value.Method;
            var moduleIdentity = method.Module.ModuleVersionId.ToString("D");
            var targetIdentity = value.Target is null
                ? "static"
                : RuntimeHelpers.GetHashCode(value.Target).ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            return moduleIdentity + ":" + method.MetadataToken + ":" + targetIdentity;
        }

        private static string ActionSourceType(DebugActionNode value)
        {
            Delegate? source = value.action is not null
                ? value.action
                : value.pawnAction is not null
                    ? value.pawnAction
                    : value.childGetter;
            var type = source?.Method.DeclaringType ?? source?.Target?.GetType();
            return type?.FullName ?? type?.Name ?? string.Empty;
        }

        private static T CaptureValue<T>(
            string field,
            Func<T> capture,
            T fallback,
            ICollection<string> errors)
        {
            try
            {
                return capture();
            }
            catch (Exception exception)
            {
                errors.Add(field + ": " + exception.GetType().Name + ": " + exception.Message);
                return fallback;
            }
        }

        private static string BoundError(IEnumerable<string> errors)
        {
            var value = string.Join("; ", errors);
            const int maximumLength = 1024;
            return value.Length <= maximumLength
                ? value
                : value.Substring(0, maximumLength - 15) + "... [truncated]";
        }
    }
}
