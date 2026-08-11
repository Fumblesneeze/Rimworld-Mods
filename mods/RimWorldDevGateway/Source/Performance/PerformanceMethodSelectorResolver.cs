using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal sealed class PerformanceSelectionRequest
{
    public PerformanceSelectionRequest(PerformanceMethodSelectorKind kind, string value, string category)
    {
        Kind = kind;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    public PerformanceMethodSelectorKind Kind { get; }
    public string Value { get; }
    public string Category { get; }
}

internal enum PerformanceHarmonyPatchKind
{
    Prefix,
    Postfix,
    Finalizer,
    Transpiler
}

internal sealed class PerformanceHarmonyPatch
{
    public PerformanceHarmonyPatch(
        string ownerId,
        PerformanceHarmonyPatchKind kind,
        MethodBase patchedTarget,
        MethodInfo patchMethod)
    {
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        Kind = kind;
        PatchedTarget = patchedTarget ?? throw new ArgumentNullException(nameof(patchedTarget));
        PatchMethod = patchMethod ?? throw new ArgumentNullException(nameof(patchMethod));
    }

    public string OwnerId { get; }
    public PerformanceHarmonyPatchKind Kind { get; }
    public MethodBase PatchedTarget { get; }
    public MethodInfo PatchMethod { get; }
}

internal interface IPerformanceHarmonyCatalog
{
    IReadOnlyList<PerformanceHarmonyPatch> ResolveOwner(string exactOwnerId);
}

internal sealed class PerformanceMethodSelectionException : Exception
{
    public PerformanceMethodSelectionException(string message) : base(message) { }
}

internal sealed class PerformanceMethodSelection
{
    public PerformanceMethodSelection(
        IReadOnlyList<PerformanceResolvedMethod> methods,
        IReadOnlyList<PerformanceUnsupportedMethod> unsupported,
        IReadOnlyList<string> circinusTargets)
    {
        Methods = methods;
        Unsupported = unsupported;
        CircinusTargets = circinusTargets;
    }

    public IReadOnlyList<PerformanceResolvedMethod> Methods { get; }
    public IReadOnlyList<PerformanceUnsupportedMethod> Unsupported { get; }
    public IReadOnlyList<string> CircinusTargets { get; }
}

internal sealed class PerformanceResolvedMethod
{
    internal PerformanceResolvedMethod(
        MethodBase method,
        IReadOnlyList<string> categories,
        IReadOnlyList<string> selectionReasons,
        IReadOnlyList<MethodBase> patchedTargets)
    {
        Method = method;
        Identity = PerformanceMethodIdentity.Of(method);
        DeclaringType = method.DeclaringType!.FullName!;
        Signature = PerformanceMethodIdentity.Signature(method);
        AssemblyName = method.Module.Assembly.GetName().Name ?? string.Empty;
        ModuleName = method.Module.Name;
        ModuleVersionId = method.Module.ModuleVersionId;
        Categories = categories;
        SelectionReasons = selectionReasons;
        PatchedTargets = patchedTargets;
    }

    public MethodBase Method { get; }
    public string Identity { get; }
    public string DeclaringType { get; }
    public string Signature { get; }
    public string AssemblyName { get; }
    public string ModuleName { get; }
    public Guid ModuleVersionId { get; }
    public IReadOnlyList<string> Categories { get; }
    public IReadOnlyList<string> SelectionReasons { get; }
    public IReadOnlyList<MethodBase> PatchedTargets { get; }
}

internal sealed class PerformanceUnsupportedMethod
{
    internal PerformanceUnsupportedMethod(
        MethodBase method,
        string category,
        string reason,
        IReadOnlyList<MethodBase> patchedTargets)
    {
        Method = method;
        Identity = PerformanceMethodIdentity.Of(method);
        Category = category;
        Reason = reason;
        PatchedTargets = patchedTargets;
    }

    public MethodBase Method { get; }
    public string Identity { get; }
    public string Category { get; }
    public string Reason { get; }
    public IReadOnlyList<MethodBase> PatchedTargets { get; }
}

internal static class PerformanceMethodIdentity
{
    public static string Of(MethodBase method) =>
        (method.Module.Assembly.GetName().Name ?? "?") + ":" +
        (method.DeclaringType?.FullName ?? "?") + "::" + Signature(method) + "@" +
        method.Module.ModuleVersionId.ToString("D") + ":" + MetadataToken(method);

    public static string Signature(MethodBase method) =>
        method.Name + "(" + string.Join(",", method.GetParameters().Select(parameter =>
            parameter.ParameterType.FullName ?? parameter.ParameterType.Name)) + ")";

    private static string MetadataToken(MethodBase method)
    {
        try { return method.MetadataToken.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        catch { return "dynamic"; }
    }
}

internal static class PerformanceMethodSelectorResolver
{
    public const int MaximumSelectedMethods =
        CircinusRuntimeRun.MaximumMethodRows + CircinusRuntimeRun.MaximumPatchRows;
    private const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.Static |
                                                       BindingFlags.DeclaredOnly;
    private static readonly HashSet<string> TickMethodNames = new(StringComparer.Ordinal)
    {
        "Tick",
        "TickRare",
        "TickLong",
        "CompTick",
        "MapComponentTick",
        "WorldComponentTick",
        "GameComponentTick"
    };

    public static PerformanceMethodSelection Resolve(
        IEnumerable<Assembly> loadedAssemblies,
        IPerformanceHarmonyCatalog harmonyCatalog,
        IEnumerable<PerformanceSelectionRequest> requests)
    {
        if (loadedAssemblies is null) throw new ArgumentNullException(nameof(loadedAssemblies));
        if (harmonyCatalog is null) throw new ArgumentNullException(nameof(harmonyCatalog));
        if (requests is null) throw new ArgumentNullException(nameof(requests));

        var assemblies = loadedAssemblies.Where(item => item is not null)
            .OrderBy(item => item.FullName, StringComparer.Ordinal)
            .ToArray();
        var selected = new Dictionary<MethodBase, MutableMethod>();
        var unsupported = new Dictionary<MethodBase, MutableUnsupported>();
        var targets = new SortedSet<string>(StringComparer.Ordinal);

        var declaredRequests = requests.Take(PerformanceTestContract.MaximumMethodSelectors + 1).ToArray();
        if (declaredRequests.Length > PerformanceTestContract.MaximumMethodSelectors)
            throw new PerformanceMethodSelectionException(
                $"Performance selection exceeds {PerformanceTestContract.MaximumMethodSelectors} selectors.");

        foreach (var request in declaredRequests.OrderBy(RequestKey, StringComparer.Ordinal))
        {
            ValidateRequest(request);
            switch (request.Kind)
            {
                case PerformanceMethodSelectorKind.HarmonyOwner:
                    ResolveHarmonyOwner(harmonyCatalog, request, selected, unsupported);
                    break;
                case PerformanceMethodSelectorKind.TickOverrides:
                    ResolveTickOverrides(assemblies, request, selected);
                    break;
                case PerformanceMethodSelectorKind.Type:
                    ResolveType(assemblies, request, selected);
                    break;
                case PerformanceMethodSelectorKind.Method:
                    ResolveMethod(assemblies, request, selected);
                    break;
                case PerformanceMethodSelectorKind.CircinusTarget:
                    targets.Add(request.Value);
                    break;
                case PerformanceMethodSelectorKind.WholeAssembly:
                    ResolveWholeAssembly(assemblies, request, selected);
                    break;
                default:
                    throw Invalid(request, "has an unsupported selector kind");
            }

            if (selected.Count > MaximumSelectedMethods)
                throw Invalid(request, $"exceeds the Circinus combined {MaximumSelectedMethods}-method selection ceiling");
        }

        var transpilerConflict = selected.Keys
            .Where(unsupported.ContainsKey)
            .OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal)
            .FirstOrDefault();
        if (transpilerConflict is not null)
            throw new PerformanceMethodSelectionException(
                "Harmony transpiler '" + PerformanceMethodIdentity.Of(transpilerConflict) +
                "' was also selected for the hand-armed runtime set; transpilers are never directly timed.");

        return new PerformanceMethodSelection(
            selected.Values
                .OrderBy(item => PerformanceMethodIdentity.Of(item.Method), StringComparer.Ordinal)
                .Select(item => item.Freeze())
                .ToArray(),
            unsupported.Values
                .OrderBy(item => PerformanceMethodIdentity.Of(item.Method), StringComparer.Ordinal)
                .Select(item => item.Freeze())
                .ToArray(),
            targets.ToArray());
    }

    private static void ResolveHarmonyOwner(
        IPerformanceHarmonyCatalog catalog,
        PerformanceSelectionRequest request,
        IDictionary<MethodBase, MutableMethod> selected,
        IDictionary<MethodBase, MutableUnsupported> unsupported)
    {
        var patches = catalog.ResolveOwner(request.Value) ??
                      throw Invalid(request, "returned no patch catalog");
        if (patches.Count == 0) throw Invalid(request, "does not resolve to an attached Harmony patch");

        foreach (var patch in patches.OrderBy(PatchKey, StringComparer.Ordinal))
        {
            if (!string.Equals(patch.OwnerId, request.Value, StringComparison.Ordinal))
                throw Invalid(request, "returned a patch for a different Harmony owner");
            if (patch.Kind == PerformanceHarmonyPatchKind.Transpiler)
            {
                if (!unsupported.TryGetValue(patch.PatchMethod, out var rejected))
                {
                    rejected = new MutableUnsupported(
                        patch.PatchMethod,
                        request.Category,
                        "Harmony transpiler invocation happens while patching and is unsupported for direct runtime timing.");
                    unsupported.Add(patch.PatchMethod, rejected);
                }
                rejected.AddTarget(patch.PatchedTarget);
                continue;
            }

            AddSelected(
                selected,
                patch.PatchMethod,
                request.Category,
                "attached Harmony " + patch.Kind.ToString().ToLowerInvariant() +
                " owned by " + request.Value,
                patch.PatchedTarget,
                request);
        }
    }

    private static void ResolveTickOverrides(
        IReadOnlyList<Assembly> assemblies,
        PerformanceSelectionRequest request,
        IDictionary<MethodBase, MutableMethod> selected)
    {
        var assembly = ExactAssembly(assemblies, request);
        var methods = SafeTypes(assembly, request)
            .SelectMany(type => type.GetMethods(DeclaredMethods))
            .Where(method => TickMethodNames.Contains(method.Name) && method.GetBaseDefinition() != method)
            .OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal)
            .ToArray();
        if (methods.Length == 0) throw Invalid(request, "does not resolve to a supported tick override");
        foreach (var method in methods)
            AddSelected(selected, method, request.Category, "product tick override", null, request);
    }

    private static void ResolveType(
        IReadOnlyList<Assembly> assemblies,
        PerformanceSelectionRequest request,
        IDictionary<MethodBase, MutableMethod> selected)
    {
        var type = ExactType(assemblies, request.Value, request);
        var methods = type.GetMethods(DeclaredMethods)
            .Where(method => UnsupportedReason(method) is null)
            .OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal)
            .ToArray();
        if (methods.Length == 0) throw Invalid(request, "type contains no supported declared methods");
        foreach (var method in methods)
            AddSelected(selected, method, request.Category, "explicit type selector " + request.Value, null, request);
    }

    private static void ResolveMethod(
        IReadOnlyList<Assembly> assemblies,
        PerformanceSelectionRequest request,
        IDictionary<MethodBase, MutableMethod> selected)
    {
        var separator = request.Value.IndexOf("::", StringComparison.Ordinal);
        if (separator <= 0 || separator == request.Value.Length - 2)
            throw Invalid(request, "must use 'Full.Type::Method' syntax");
        var typeName = request.Value.Substring(0, separator);
        var methodSelector = request.Value.Substring(separator + 2);
        var open = methodSelector.IndexOf('(');
        var methodName = open < 0 ? methodSelector : methodSelector.Substring(0, open);
        var type = ExactType(assemblies, typeName, request);
        var matches = type.GetMethods(DeclaredMethods)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .Where(method => open < 0 || string.Equals(
                PerformanceMethodIdentity.Signature(method),
                methodSelector,
                StringComparison.Ordinal))
            .OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal)
            .ToArray();
        if (matches.Length == 0) throw Invalid(request, "does not resolve to an exact declared method");
        if (matches.Length != 1) throw Invalid(request, "is ambiguous; include the exact parameter signature");
        AddSelected(selected, matches[0], request.Category, "explicit method selector " + request.Value, null, request);
    }

    private static void ResolveWholeAssembly(
        IReadOnlyList<Assembly> assemblies,
        PerformanceSelectionRequest request,
        IDictionary<MethodBase, MutableMethod> selected)
    {
        var assembly = ExactAssembly(assemblies, request);
        var methods = SafeTypes(assembly, request)
            .SelectMany(type => type.GetMethods(DeclaredMethods))
            .Where(method => UnsupportedReason(method) is null)
            .OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal)
            .ToArray();
        if (methods.Length == 0) throw Invalid(request, "assembly contains no supported methods");
        foreach (var method in methods)
            AddSelected(selected, method, request.Category, "explicit whole-assembly selector", null, request);
    }

    private static Assembly ExactAssembly(
        IEnumerable<Assembly> assemblies,
        PerformanceSelectionRequest request)
    {
        var matches = assemblies.Where(item => string.Equals(
            item.GetName().Name,
            request.Value,
            StringComparison.Ordinal)).ToArray();
        if (matches.Length == 0) throw Invalid(request, "does not resolve to an exact loaded assembly");
        if (matches.Length != 1) throw Invalid(request, "resolves to duplicate loaded assemblies");
        return matches[0];
    }

    private static Type ExactType(
        IEnumerable<Assembly> assemblies,
        string fullName,
        PerformanceSelectionRequest request)
    {
        var matches = assemblies.Select(item => item.GetType(fullName, false, false))
            .Where(item => item is not null)
            .Cast<Type>()
            .ToArray();
        if (matches.Length == 0) throw Invalid(request, "does not resolve to an exact loaded type");
        if (matches.Length != 1) throw Invalid(request, "resolves to duplicate loaded types");
        return matches[0];
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly, PerformanceSelectionRequest request)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException exception)
        {
            var detail = exception.LoaderExceptions.FirstOrDefault()?.Message ?? exception.Message;
            throw Invalid(request, "could not enumerate its assembly types: " + detail);
        }
    }

    private static void AddSelected(
        IDictionary<MethodBase, MutableMethod> selected,
        MethodBase method,
        string category,
        string reason,
        MethodBase? patchedTarget,
        PerformanceSelectionRequest request)
    {
        var unsupported = UnsupportedReason(method);
        if (unsupported is not null) throw Invalid(request, unsupported);
        if (!selected.TryGetValue(method, out var item))
        {
            item = new MutableMethod(method);
            selected.Add(method, item);
        }
        item.Categories.Add(category);
        item.Reasons.Add(reason);
        if (patchedTarget is not null) item.AddTarget(patchedTarget);
    }

    private static string? UnsupportedReason(MethodBase method)
    {
        if (method.DeclaringType is null) return "resolves to a dynamic or ownerless method";
        if (method.DeclaringType.ContainsGenericParameters || method.ContainsGenericParameters)
            return "resolves to an open generic method";
        if (method.IsAbstract) return "resolves to an abstract method";
        if (method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null ||
            method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            return "resolves to a compiler-generated method";
        return null;
    }

    private static void ValidateRequest(PerformanceSelectionRequest request)
    {
        if (request is null) throw new PerformanceMethodSelectionException("Performance selector is null.");
        if (string.IsNullOrWhiteSpace(request.Value)) throw Invalid(request, "has an empty value");
        if (string.IsNullOrWhiteSpace(request.Category)) throw Invalid(request, "has an empty category");
    }

    private static string RequestKey(PerformanceSelectionRequest request) =>
        ((int)request.Kind).ToString("D2") + ":" + request.Value + ":" + request.Category;

    private static string PatchKey(PerformanceHarmonyPatch patch) =>
        PerformanceMethodIdentity.Of(patch.PatchMethod) + ":" +
        PerformanceMethodIdentity.Of(patch.PatchedTarget) + ":" + patch.Kind;

    private static PerformanceMethodSelectionException Invalid(
        PerformanceSelectionRequest request,
        string detail) =>
        new($"Performance selector {request.Kind} '{request.Value}' {detail}.");

    private sealed class MutableMethod
    {
        private readonly SortedDictionary<string, MethodBase> patchedTargets = new(StringComparer.Ordinal);

        public MutableMethod(MethodBase method) => Method = method;

        public MethodBase Method { get; }
        public SortedSet<string> Categories { get; } = new(StringComparer.Ordinal);
        public SortedSet<string> Reasons { get; } = new(StringComparer.Ordinal);

        public void AddTarget(MethodBase target) =>
            patchedTargets[PerformanceMethodIdentity.Of(target)] = target;

        public PerformanceResolvedMethod Freeze() =>
            new(Method, Categories.ToArray(), Reasons.ToArray(), patchedTargets.Values.ToArray());
    }

    private sealed class MutableUnsupported
    {
        private readonly SortedDictionary<string, MethodBase> patchedTargets = new(StringComparer.Ordinal);

        public MutableUnsupported(MethodBase method, string category, string reason)
        {
            Method = method;
            Category = category;
            Reason = reason;
        }

        public MethodBase Method { get; }
        public string Category { get; }
        public string Reason { get; }

        public void AddTarget(MethodBase target) =>
            patchedTargets[PerformanceMethodIdentity.Of(target)] = target;

        public PerformanceUnsupportedMethod Freeze() =>
            new(Method, Category, Reason, patchedTargets.Values.ToArray());
    }
}
