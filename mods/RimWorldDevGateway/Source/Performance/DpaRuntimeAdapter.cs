using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway.Performance;

internal interface IDpaTypeSource
{
    Type? Resolve(string fullName);
}

internal sealed class AssemblyDpaTypeSource : IDpaTypeSource
{
    private readonly Assembly assembly;

    public AssemblyDpaTypeSource(Assembly assembly) =>
        this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

    public Type? Resolve(string fullName) => assembly.GetType(fullName, false, false);
}

[DataContract]
internal sealed class DpaAssemblyIdentity
{
    public DpaAssemblyIdentity(string identity, Guid mvid, long length, string sha256, string productVersion)
    {
        Identity = identity;
        ModuleVersionId = mvid;
        Length = length;
        Sha256 = sha256;
        ProductVersion = productVersion;
    }

    [DataMember(Name = "identity", Order = 1)] public string Identity { get; private set; }
    [DataMember(Name = "moduleVersionId", Order = 2)] public Guid ModuleVersionId { get; private set; }
    [DataMember(Name = "length", Order = 3)] public long Length { get; private set; }
    [DataMember(Name = "sha256", Order = 4)] public string Sha256 { get; private set; }
    [DataMember(Name = "productVersion", Order = 5)] public string ProductVersion { get; private set; }
}

internal enum DpaDiagnosticCategory
{
    Tick = 1,
    Update = 2
}

internal static class DpaRuntimeAdapter
{
    public const string PackageId = "dubwise.dubsperformanceanalyzer.steam";
    public const string AssemblyName = "PerformanceAnalyzer";
    public const int MaximumLoadedAssemblies = 1024;

    public static bool TryBind(
        bool packageActive,
        IEnumerable<Assembly> loadedAssemblies,
        out DpaRuntimeBinding? binding,
        out string reason)
    {
        binding = null;
        if (!packageActive)
        {
            reason = $"Dubs Performance Analyzer package '{PackageId}' is inactive.";
            return false;
        }
        if (loadedAssemblies is null)
        {
            reason = "The loaded-assembly catalog is unavailable.";
            return false;
        }

        var assemblies = loadedAssemblies.Take(MaximumLoadedAssemblies + 1).ToArray();
        if (assemblies.Length > MaximumLoadedAssemblies)
        {
            reason = $"The loaded-assembly catalog exceeds {MaximumLoadedAssemblies} entries.";
            return false;
        }
        if (assemblies.Any(candidate => candidate is null))
        {
            reason = "The loaded-assembly catalog contains null.";
            return false;
        }
        var matches = assemblies.Where(candidate =>
            string.Equals(candidate.GetName().Name, AssemblyName, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1)
        {
            reason = matches.Length == 0
                ? $"DPA assembly '{AssemblyName}' is not loaded."
                : $"Expected exactly one loaded DPA assembly '{AssemblyName}', observed {matches.Length}.";
            return false;
        }

        try
        {
            var location = matches[0].Location;
            if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
                throw new InvalidOperationException("DPA assembly has no readable physical location.");
            string hash;
            using (var stream = File.OpenRead(location))
            using (var algorithm = SHA256.Create())
                hash = string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("X2")));
            var info = new FileInfo(location);
            var identity = new DpaAssemblyIdentity(
                matches[0].FullName ?? AssemblyName,
                matches[0].ManifestModule.ModuleVersionId,
                info.Length,
                hash,
                FileVersionInfo.GetVersionInfo(location).ProductVersion ?? string.Empty);
            if (!DpaShapeBinder.TryBind(new AssemblyDpaTypeSource(matches[0]), out var shape, out reason))
                return false;
            if (!ReflectionPerformanceHarmonyCatalog.TryBind(assemblies, out var harmony, out reason))
                return false;
            binding = new DpaRuntimeBinding(identity, shape!, harmony!);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Could not bind DPA: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }
}

internal static class DpaShapeBinder
{
    public static bool TryBind(IDpaTypeSource source, out DpaShape? shape, out string reason)
    {
        shape = null;
        if (source is null)
        {
            reason = "DPA type source is unavailable.";
            return false;
        }
        try
        {
            var analyzer = Type(source, "Analyzer.Profiling.Analyzer");
            var controller = Type(source, "Analyzer.Profiling.ProfileController");
            var profiler = Type(source, "Analyzer.Profiling.Profiler");
            var profileLog = Type(source, "Analyzer.Profiling.ProfileLog");
            var utility = Type(source, "Analyzer.Profiling.Utility");
            var internalUtility = Type(source, "Analyzer.Profiling.InternalMethodUtility");
            var transpilerUtility = Type(source, "Analyzer.Profiling.TranspilerMethodUtility");
            var guiController = Type(source, "Analyzer.Profiling.GUIController");
            var category = Type(source, "Analyzer.Profiling.Category");
            var settings = Type(source, "Analyzer.Settings");
            var modbase = Type(source, "Analyzer.Modbase");
            var rootUpdate = Type(source, "Analyzer.Profiling.H_RootUpdate");
            var tickUpdate = Type(source, "Analyzer.Profiling.H_DoSingleTickUpdate");
            var rootPlay = source.Resolve("Verse.Root_Play") ?? typeof(Verse.Root_Play);
            var tickManager = source.Resolve("Verse.TickManager") ?? typeof(Verse.TickManager);
            if (!category.IsEnum || Enum.GetUnderlyingType(category) != typeof(int) ||
                !string.Equals(Enum.GetName(category, 1), "Tick", StringComparison.Ordinal) ||
                !string.Equals(Enum.GetName(category, 2), "Update", StringComparison.Ordinal))
                throw new MissingMemberException(category.FullName, "Tick=1/Update=2");

            var recordCount = profiler.GetField("RECORDS_HELD", BindingFlags.Public | BindingFlags.Static);
            if (recordCount is null || !recordCount.IsLiteral || recordCount.FieldType != typeof(int) ||
                !Equals(recordCount.GetRawConstantValue(), 2000))
                throw new MissingFieldException(profiler.FullName, "public const Int32 RECORDS_HELD=2000");

            var internalProfiler = ExactField(
                internalUtility, "InternalProfiler", null, BindingFlags.Public | BindingFlags.Static);
            if (!string.Equals(internalProfiler.FieldType.FullName, "HarmonyLib.HarmonyMethod", StringComparison.Ordinal))
                throw new MissingFieldException(internalUtility.FullName, "HarmonyLib.HarmonyMethod InternalProfiler");
            var harmonyMethodTarget = ExactField(
                internalProfiler.FieldType, "method", typeof(MethodInfo), BindingFlags.Public | BindingFlags.Instance);
            var harmonyType = internalProfiler.FieldType.Assembly.GetType("HarmonyLib.Harmony", false, false) ??
                              throw new TypeLoadException("Missing HarmonyLib.Harmony type.");
            var harmonyMethodConstructor = internalProfiler.FieldType.GetConstructor(new[] { typeof(MethodInfo) }) ??
                                           throw new MissingMethodException(
                                               internalProfiler.FieldType.FullName, ".ctor(MethodInfo)");

            shape = new DpaShape(
                ExactMethod(analyzer, "BeginProfiling", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactMethod(analyzer, "EndProfiling", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactMethod(analyzer, "Cleanup", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactProperty(analyzer, "CurrentlyProfiling", typeof(bool), requireSetter: false),
                ExactProperty(analyzer, "CurrentlyCleaningUp", typeof(bool), requireSetter: true),
                ExactProperty(analyzer, "Logs", typeof(List<>).MakeGenericType(profileLog), requireSetter: false),
                ExactProperty(controller, "Profiles",
                    typeof(ConcurrentDictionary<,>).MakeGenericType(typeof(string), profiler), requireSetter: false),
                ExactProperty(controller, "Handles", typeof(ConcurrentBag<GCHandle>), requireSetter: false),
                ExactMethod(utility, "PatchInternalMethod", BindingFlags.Public | BindingFlags.Static, typeof(void),
                    typeof(MethodInfo), category),
                ExactField(internalUtility, "PatchedInternals", typeof(HashSet<MethodInfo>), BindingFlags.Public | BindingFlags.Static),
                internalProfiler,
                harmonyMethodTarget,
                ExactField(utility, "patchedAssemblies", typeof(List<string>), BindingFlags.Public | BindingFlags.Static),
                ExactField(utility, "patchedTypes", typeof(List<string>), BindingFlags.Public | BindingFlags.Static),
                ExactField(utility, "patchedMethods", typeof(List<string>), BindingFlags.Public | BindingFlags.Static),
                ExactField(transpilerUtility, "PatchedMeths", typeof(List<MethodBase>), BindingFlags.Public | BindingFlags.Static),
                ExactField(modbase, "isPatched", typeof(bool), BindingFlags.Public | BindingFlags.Static),
                ExactField(guiController, "types", typeof(Dictionary<string, Type>), BindingFlags.Public | BindingFlags.Static),
                ExactField(settings, "disableThreadedPatching", typeof(bool), BindingFlags.Public | BindingFlags.Static),
                ExactProperty(modbase, "Harmony", harmonyType, requireSetter: false),
                ExactMethod(harmonyType, "Patch", BindingFlags.Public | BindingFlags.Instance, typeof(MethodInfo),
                    typeof(MethodBase), internalProfiler.FieldType, internalProfiler.FieldType,
                    internalProfiler.FieldType, internalProfiler.FieldType),
                harmonyMethodConstructor,
                ExactMethod(rootPlay, "Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, typeof(void)),
                ExactMethod(tickManager, "DoSingleTick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, typeof(void)),
                ExactMethod(rootUpdate, "Prefix", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactMethod(rootUpdate, "Postfix", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactMethod(tickUpdate, "Prefix", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                ExactMethod(tickUpdate, "Postfix", BindingFlags.Public | BindingFlags.Static, typeof(void)),
                category,
                profiler,
                ExactField(profiler, "key", typeof(string), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "label", typeof(string), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "type", typeof(Type), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "meth", typeof(MethodBase), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "times", typeof(double[]), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "hits", typeof(int[]), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "currentIndex", typeof(uint), BindingFlags.Public | BindingFlags.Instance),
                ExactField(profiler, "Empty", typeof(bool), BindingFlags.Public | BindingFlags.Instance));
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "DPA exact runtime shape changed: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private static Type Type(IDpaTypeSource source, string name) =>
        source.Resolve(name) ?? throw new TypeLoadException("Missing DPA type " + name + ".");

    private static MethodInfo ExactMethod(
        Type type, string name, BindingFlags flags, Type returnType, params Type[] parameters)
    {
        var method = type.GetMethod(name, flags, null, parameters, null);
        if (method is null || method.ReturnType != returnType || method.DeclaringType != type)
            throw new MissingMethodException(type.FullName, name + "(" + string.Join(",", parameters.Select(item => item.Name)) + "):" + returnType.Name);
        return method;
    }

    private static PropertyInfo ExactProperty(Type type, string name, Type? propertyType, bool requireSetter)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (property is null || (propertyType is not null && property.PropertyType != propertyType) ||
            property.GetMethod is null || !property.GetMethod.IsStatic ||
            property.DeclaringType != type ||
            (requireSetter && (property.SetMethod is null || !property.SetMethod.IsStatic)))
            throw new MissingMemberException(type.FullName, name);
        return property;
    }

    private static FieldInfo ExactField(Type type, string name, Type? fieldType, BindingFlags flags)
    {
        var field = type.GetField(name, flags);
        if (field is null || (fieldType is not null && field.FieldType != fieldType) || field.DeclaringType != type)
            throw new MissingFieldException(type.FullName, name);
        return field;
    }
}

internal sealed class DpaShape
{
    public DpaShape(
        MethodInfo begin, MethodInfo end, MethodInfo cleanup, PropertyInfo profiling,
        PropertyInfo cleaning, PropertyInfo logs, PropertyInfo profiles, PropertyInfo handles,
        MethodInfo patchInternal, FieldInfo patchedInternals, FieldInfo internalProfiler,
        FieldInfo harmonyMethodTarget, FieldInfo patchedAssemblies, FieldInfo patchedTypes,
        FieldInfo patchedMethods, FieldInfo transpilerPatchedMethods, FieldInfo analyzerPatched,
        FieldInfo generatedTypes, FieldInfo disableThreading, PropertyInfo harmonyInstance,
        MethodInfo harmonyPatch, ConstructorInfo harmonyMethodConstructor,
        MethodInfo rootUpdateTarget, MethodInfo tickUpdateTarget,
        MethodInfo rootUpdatePrefix, MethodInfo rootUpdatePostfix,
        MethodInfo tickUpdatePrefix, MethodInfo tickUpdatePostfix,
        Type category, Type profiler,
        FieldInfo key, FieldInfo label, FieldInfo type, FieldInfo method, FieldInfo times,
        FieldInfo hits, FieldInfo currentIndex, FieldInfo empty)
    {
        Begin = begin; End = end; Cleanup = cleanup; Profiling = profiling; Cleaning = cleaning;
        Logs = logs; Profiles = profiles; Handles = handles; PatchInternal = patchInternal;
        PatchedInternals = patchedInternals; InternalProfiler = internalProfiler;
        HarmonyMethodTarget = harmonyMethodTarget; PatchedAssemblies = patchedAssemblies;
        PatchedTypes = patchedTypes; PatchedMethods = patchedMethods;
        TranspilerPatchedMethods = transpilerPatchedMethods; AnalyzerPatched = analyzerPatched;
        GeneratedTypes = generatedTypes;
        DisableThreading = disableThreading; HarmonyInstance = harmonyInstance;
        HarmonyPatch = harmonyPatch; HarmonyMethodConstructor = harmonyMethodConstructor;
        RootUpdateTarget = rootUpdateTarget; TickUpdateTarget = tickUpdateTarget;
        RootUpdatePrefix = rootUpdatePrefix; RootUpdatePostfix = rootUpdatePostfix;
        TickUpdatePrefix = tickUpdatePrefix; TickUpdatePostfix = tickUpdatePostfix;
        Category = category; Profiler = profiler;
        Key = key; Label = label; Type = type; Method = method; Times = times; Hits = hits;
        CurrentIndex = currentIndex; Empty = empty;
    }

    public MethodInfo Begin { get; }
    public MethodInfo End { get; }
    public MethodInfo Cleanup { get; }
    public PropertyInfo Profiling { get; }
    public PropertyInfo Cleaning { get; }
    public PropertyInfo Logs { get; }
    public PropertyInfo Profiles { get; }
    public PropertyInfo Handles { get; }
    public MethodInfo PatchInternal { get; }
    public FieldInfo PatchedInternals { get; }
    public FieldInfo InternalProfiler { get; }
    public FieldInfo HarmonyMethodTarget { get; }
    public FieldInfo PatchedAssemblies { get; }
    public FieldInfo PatchedTypes { get; }
    public FieldInfo PatchedMethods { get; }
    public FieldInfo TranspilerPatchedMethods { get; }
    public FieldInfo AnalyzerPatched { get; }
    public FieldInfo GeneratedTypes { get; }
    public FieldInfo DisableThreading { get; }
    public PropertyInfo HarmonyInstance { get; }
    public MethodInfo HarmonyPatch { get; }
    public ConstructorInfo HarmonyMethodConstructor { get; }
    public MethodInfo RootUpdateTarget { get; }
    public MethodInfo TickUpdateTarget { get; }
    public MethodInfo RootUpdatePrefix { get; }
    public MethodInfo RootUpdatePostfix { get; }
    public MethodInfo TickUpdatePrefix { get; }
    public MethodInfo TickUpdatePostfix { get; }
    public Type Category { get; }
    public Type Profiler { get; }
    public FieldInfo Key { get; }
    public FieldInfo Label { get; }
    public FieldInfo Type { get; }
    public FieldInfo Method { get; }
    public FieldInfo Times { get; }
    public FieldInfo Hits { get; }
    public FieldInfo CurrentIndex { get; }
    public FieldInfo Empty { get; }
}

internal sealed class DpaRuntimeBinding
{
    public const int MaximumSelectors = 1;
    private readonly DpaShape shape;
    private readonly ReflectionPerformanceHarmonyCatalog harmony;

    public DpaRuntimeBinding(
        DpaAssemblyIdentity identity,
        DpaShape shape,
        ReflectionPerformanceHarmonyCatalog harmony)
    {
        Identity = identity;
        this.shape = shape;
        this.harmony = harmony;
    }

    public DpaAssemblyIdentity Identity { get; }

    public bool TryBeginInternalCallDiagnostic(
        IEnumerable<MethodInfo> selectors,
        DpaDiagnosticCategory category,
        out DpaRuntimeRun? run,
        out string reason)
    {
        run = null;
        try
        {
            if ((bool)shape.Profiling.GetValue(null, null)!)
                throw new InvalidOperationException("DPA is already profiling.");
            if ((bool)shape.Cleaning.GetValue(null, null)!)
                throw new InvalidOperationException("DPA is already cleaning up.");
            var dirty = DpaNativeState.Problems(shape, harmony);
            if (dirty.Count != 0)
                throw new InvalidOperationException(
                    "DPA contains pre-existing profiling state; the diagnostic will not claim or clean it: " +
                    string.Join(", ", dirty) + ".");
            var methods = MaterializeSelectors(selectors);
            var previous = (bool)shape.DisableThreading.GetValue(null)!;
            shape.DisableThreading.SetValue(null, true);
            var prepared = new DpaRuntimeRun(Identity, shape, harmony, methods, category, previous);
            try
            {
                prepared.Register();
                run = prepared;
                reason = string.Empty;
                return true;
            }
            catch (Exception primary)
            {
                try
                {
                    prepared.RequestCleanup();
                }
                catch (Exception cleanup)
                {
                    run = prepared;
                    reason = "Could not prepare DPA diagnostic and cleanup did not complete: " +
                             CircinusRuntimeAdapter.Describe(new AggregateException(primary, cleanup));
                    return false;
                }
                bool cleanupComplete;
                string cleanupReason;
                try
                {
                    cleanupComplete = prepared.TryConfirmCleanup(out cleanupReason);
                }
                catch (Exception inspection)
                {
                    run = prepared;
                    reason = "Could not prepare DPA diagnostic and cleanup could not be confirmed; " +
                             "retryable cleanup owner retained: " +
                             CircinusRuntimeAdapter.Describe(new AggregateException(primary, inspection));
                    return false;
                }
                if (cleanupComplete)
                {
                    reason = "Could not prepare DPA diagnostic: " +
                             CircinusRuntimeAdapter.Describe(primary);
                    return false;
                }
                run = prepared;
                reason = "Could not prepare DPA diagnostic: " +
                         CircinusRuntimeAdapter.Describe(primary) +
                         "; retryable cleanup owner retained: " + cleanupReason;
                return false;
            }
        }
        catch (Exception exception)
        {
            reason = "Could not prepare DPA diagnostic: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private static MethodInfo[] MaterializeSelectors(IEnumerable<MethodInfo> selectors)
    {
        if (selectors is null) throw new ArgumentNullException(nameof(selectors));
        var result = selectors.Take(MaximumSelectors + 1).ToArray();
        if (result.Length == 0) throw new InvalidOperationException("At least one DPA selector is required.");
        if (result.Length > MaximumSelectors)
            throw new InvalidOperationException($"DPA selector count exceeds {MaximumSelectors}.");
        if (result.Any(method => method is null)) throw new InvalidOperationException("DPA selectors contain null.");
        if (result.Distinct().Count() != result.Length) throw new InvalidOperationException("DPA selectors contain duplicates.");
        foreach (var method in result)
        {
            if (method.ContainsGenericParameters || method.IsGenericMethodDefinition ||
                method.DeclaringType?.ContainsGenericParameters == true)
                throw new InvalidOperationException("DPA internal-call selectors must not be generic.");
            var body = method.GetMethodBody()?.GetILAsByteArray();
            if (body is null || body.Length <= 1)
                throw new InvalidOperationException("DPA internal-call selectors require a concrete nonempty method body.");
        }
        return result.OrderBy(PerformanceMethodIdentity.Of, StringComparer.Ordinal).ToArray();
    }

    internal static string OwnerKey(MethodInfo method) => method.DeclaringType + ":" + method.Name + "-int";

}

internal static class DpaNativeState
{
    public static IReadOnlyList<string> Problems(
        DpaShape shape,
        ReflectionPerformanceHarmonyCatalog harmony)
    {
        var result = new List<string>();
        if ((bool)shape.Profiling.GetValue(null, null)!) result.Add("profiling");
        if ((bool)shape.Cleaning.GetValue(null, null)!) result.Add("cleaning");
        if ((bool)shape.AnalyzerPatched.GetValue(null)!) result.Add("analyzer-patched");
        AddCount(result, "profiles", shape.Profiles.GetValue(null, null));
        AddCount(result, "handles", shape.Handles.GetValue(null, null));
        AddCount(result, "logs", shape.Logs.GetValue(null, null));
        AddCount(result, "internal-patches", shape.PatchedInternals.GetValue(null));
        AddCount(result, "assemblies", shape.PatchedAssemblies.GetValue(null));
        AddCount(result, "types", shape.PatchedTypes.GetValue(null));
        AddCount(result, "methods", shape.PatchedMethods.GetValue(null));
        AddCount(result, "transpilers", shape.TranspilerPatchedMethods.GetValue(null));
        var patches = harmony.ResolveOwner("Dubwise.DubsProfiler");
        if (patches.Count != 0) result.Add("harmony-patches=" + patches.Count);
        return result;
    }

    private static void AddCount(ICollection<string> result, string name, object? collection)
    {
        if (collection is null) throw new InvalidOperationException("DPA returned a null " + name + " collection.");
        var count = (int)(collection.GetType().GetProperty("Count")?.GetValue(collection, null) ??
                          throw new MissingMemberException(collection.GetType().FullName, "Count"));
        if (count != 0) result.Add(name + "=" + count);
    }
}

internal sealed class DpaRuntimeRun : IDisposable
{
    public const int MaximumProfilerEntries = 4096;
    public const int MaximumSampleRows = 1_000_000;
    private readonly DpaAssemblyIdentity identity;
    private readonly DpaShape shape;
    private readonly ReflectionPerformanceHarmonyCatalog harmony;
    private readonly MethodInfo[] selectors;
    private readonly DpaDiagnosticCategory category;
    private readonly bool previousDisableThreading;
    private bool registered;
    private bool started;
    private bool stopped;
    private bool disposed;
    private bool cleanupRequested;

    public DpaRuntimeRun(
        DpaAssemblyIdentity identity, DpaShape shape, ReflectionPerformanceHarmonyCatalog harmony,
        MethodInfo[] selectors,
        DpaDiagnosticCategory category, bool previousDisableThreading)
    {
        this.identity = identity;
        this.shape = shape;
        this.harmony = harmony;
        this.selectors = selectors;
        this.category = category;
        this.previousDisableThreading = previousDisableThreading;
    }

    internal void Register()
    {
        foreach (var method in selectors)
            shape.PatchInternal.Invoke(null, new[] { method, Enum.ToObject(shape.Category, (int)category) });
        var retained = shape.PatchedInternals.GetValue(null) as HashSet<MethodInfo> ??
                       throw new InvalidOperationException("DPA patched-internal registry is unavailable.");
        var harmonyMethod = shape.InternalProfiler.GetValue(null) ??
                            throw new InvalidOperationException("DPA internal profiler is unavailable.");
        var expectedPatch = shape.HarmonyMethodTarget.GetValue(harmonyMethod) as MethodInfo ??
                            throw new InvalidOperationException("DPA internal profiler has no exact patch method.");
        var generatedTypes = shape.GeneratedTypes.GetValue(null) as Dictionary<string, Type> ??
                             throw new InvalidOperationException("DPA generated-type registry is unavailable.");
        InstallMeasurementCycle();
        var ownedPatches = harmony.ResolveOwner("Dubwise.DubsProfiler");
        foreach (var method in selectors)
        {
            if (!retained.Contains(method))
                throw new InvalidOperationException("DPA did not retain exact internal-call selector " +
                                                    PerformanceMethodIdentity.Of(method) + ".");
            var exactPatches = ownedPatches.Where(patch =>
                patch.Kind == PerformanceHarmonyPatchKind.Transpiler &&
                Equals(patch.PatchedTarget, method) &&
                Equals(patch.PatchMethod, expectedPatch)).ToArray();
            if (exactPatches.Length != 1)
                throw new InvalidOperationException(
                    "DPA did not apply exactly one native internal-call transpiler to " +
                    PerformanceMethodIdentity.Of(method) + ".");
            var key = DpaRuntimeBinding.OwnerKey(method);
            if (!generatedTypes.TryGetValue(key, out var ownerType) || ownerType is null)
                throw new InvalidOperationException("DPA did not retain generated owner type " + key + ".");
        }
        var cycleTarget = category == DpaDiagnosticCategory.Tick
            ? shape.TickUpdateTarget
            : shape.RootUpdateTarget;
        var cyclePrefix = category == DpaDiagnosticCategory.Tick
            ? shape.TickUpdatePrefix
            : shape.RootUpdatePrefix;
        var cyclePostfix = category == DpaDiagnosticCategory.Tick
            ? shape.TickUpdatePostfix
            : shape.RootUpdatePostfix;
        RequireExactCyclePatch(ownedPatches, cycleTarget, cyclePrefix, PerformanceHarmonyPatchKind.Prefix);
        RequireExactCyclePatch(ownedPatches, cycleTarget, cyclePostfix, PerformanceHarmonyPatchKind.Postfix);
        registered = true;
    }

    private void InstallMeasurementCycle()
    {
        var target = category == DpaDiagnosticCategory.Tick ? shape.TickUpdateTarget : shape.RootUpdateTarget;
        var prefix = category == DpaDiagnosticCategory.Tick ? shape.TickUpdatePrefix : shape.RootUpdatePrefix;
        var postfix = category == DpaDiagnosticCategory.Tick ? shape.TickUpdatePostfix : shape.RootUpdatePostfix;
        var harmonyInstance = shape.HarmonyInstance.GetValue(null, null) ??
                              throw new InvalidOperationException("DPA profiling Harmony instance is unavailable.");
        var prefixDescriptor = shape.HarmonyMethodConstructor.Invoke(new object[] { prefix });
        var postfixDescriptor = shape.HarmonyMethodConstructor.Invoke(new object[] { postfix });
        shape.HarmonyPatch.Invoke(harmonyInstance,
            new[] { target, prefixDescriptor, postfixDescriptor, null, null });
        shape.AnalyzerPatched.SetValue(null, true);
    }

    private static void RequireExactCyclePatch(
        IReadOnlyList<PerformanceHarmonyPatch> ownedPatches,
        MethodInfo target,
        MethodInfo patchMethod,
        PerformanceHarmonyPatchKind kind)
    {
        var count = ownedPatches.Count(patch =>
            patch.Kind == kind && Equals(patch.PatchedTarget, target) && Equals(patch.PatchMethod, patchMethod));
        if (count != 1)
            throw new InvalidOperationException(
                "DPA did not apply exactly one native " + kind.ToString().ToLowerInvariant() +
                " measurement-cycle patch to " + PerformanceMethodIdentity.Of(target) + ".");
    }

    public bool TryStart(out string reason)
    {
        try
        {
            RequireUsable();
            if (!registered) throw new InvalidOperationException("DPA selectors are not registered.");
            if (started) throw new InvalidOperationException("DPA diagnostic already started.");
            shape.Begin.Invoke(null, null);
            if (!(bool)shape.Profiling.GetValue(null, null)!)
                throw new InvalidOperationException("DPA did not enter profiling state.");
            started = true;
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Could not start DPA diagnostic: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public bool TryStopAndCapture(
        string workloadVersion,
        IEnumerable<string> checkpoints,
        out DpaDiagnosticCapture? capture,
        out string reason)
    {
        capture = null;
        try
        {
            RequireUsable();
            if (!started || stopped) throw new InvalidOperationException("DPA diagnostic is not active.");
            ValidateText(workloadVersion, 256, "workload version");
            var retainedCheckpoints = (checkpoints ?? throw new ArgumentNullException(nameof(checkpoints)))
                .Take(129).ToArray();
            if (retainedCheckpoints.Length > 128)
                throw new InvalidOperationException("DPA checkpoint count exceeds 128.");
            foreach (var checkpoint in retainedCheckpoints) ValidateText(checkpoint, 256, "checkpoint");

            shape.End.Invoke(null, null);
            stopped = true;
            if ((bool)shape.Profiling.GetValue(null, null)!)
                throw new InvalidOperationException("DPA did not leave profiling state.");
            var entries = SnapshotEntries();
            if (!entries.Any(entry => !entry.Empty && entry.Samples.Length != 0))
                throw new InvalidOperationException(
                    "DPA produced no invoked raw internal-callee entry for outer selector " +
                    PerformanceMethodIdentity.Of(selectors[0]) + ".");
            capture = new DpaDiagnosticCapture(
                identity,
                workloadVersion,
                category.ToString().ToLowerInvariant(),
                selectors.Select(PerformanceMethodIdentity.Of).ToArray(),
                retainedCheckpoints,
                entries);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Could not stop and snapshot DPA diagnostic: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private IReadOnlyList<DpaDiagnosticEntry> SnapshotEntries()
    {
        var profiles = shape.Profiles.GetValue(null, null) as IEnumerable ??
                       throw new InvalidOperationException("DPA profiler registry is unavailable.");
        var result = new List<DpaDiagnosticEntry>();
        var samples = 0;
        foreach (var pair in profiles)
        {
            if (result.Count == MaximumProfilerEntries)
                throw new InvalidOperationException($"DPA profiler entries exceed {MaximumProfilerEntries}.");
            var pairType = pair.GetType();
            var value = pairType.GetProperty("Value")?.GetValue(pair, null) ??
                        throw new MissingMemberException(pairType.FullName, "Value");
            if (!shape.Profiler.IsInstanceOfType(value))
                throw new InvalidOperationException("DPA profiler registry returned a changed value type.");
            var times = (double[])shape.Times.GetValue(value)!;
            var hits = (int[])shape.Hits.GetValue(value)!;
            if (times.Length != 2000 || hits.Length != 2000)
                throw new InvalidOperationException("DPA profiler ring no longer contains exactly 2000 slots.");
            var retained = new List<DpaDiagnosticSample>();
            for (var index = 0; index < times.Length; index++)
            {
                if (hits[index] == 0 && times[index] == 0d) continue;
                if (++samples > MaximumSampleRows)
                    throw new InvalidOperationException($"DPA raw sample rows exceed {MaximumSampleRows}.");
                if (hits[index] < 0 || double.IsNaN(times[index]) || double.IsInfinity(times[index]) || times[index] < 0d)
                    throw new InvalidOperationException("DPA raw sample contains an invalid value.");
                retained.Add(new DpaDiagnosticSample(index, hits[index], times[index]));
            }
            var key = (string?)shape.Key.GetValue(value) ?? string.Empty;
            var label = (string?)shape.Label.GetValue(value) ?? string.Empty;
            ValidateText(key, 4096, "profile key");
            ValidateText(label, 4096, "profile label");
            var method = shape.Method.GetValue(value) as MethodBase;
            var type = shape.Type.GetValue(value) as Type;
            var currentIndex = (uint)shape.CurrentIndex.GetValue(value)!;
            if (currentIndex >= 2000)
                throw new InvalidOperationException("DPA profiler ring index is outside its native 2000 slots.");
            result.Add(new DpaDiagnosticEntry(
                key,
                label,
                type?.AssemblyQualifiedName ?? string.Empty,
                method is null ? string.Empty : PerformanceMethodIdentity.Of(method),
                currentIndex,
                (bool)shape.Empty.GetValue(value)!,
                retained));
        }
        return result.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
    }

    public void RequestCleanup()
    {
        if (disposed) return;
        if (cleanupRequested)
        {
            if ((bool)shape.Cleaning.GetValue(null, null)!) return;
            var remaining = DpaNativeState.Problems(shape, harmony);
            if (remaining.Count == 0 &&
                (bool)shape.DisableThreading.GetValue(null)! == previousDisableThreading)
                return;
            cleanupRequested = false;
        }
        if ((bool)shape.Profiling.GetValue(null, null)!) shape.End.Invoke(null, null);
        shape.DisableThreading.SetValue(null, previousDisableThreading);
        // DPA queues CleanupBackground and only sets CurrentlyCleaningUp inside that worker.
        // Claim the public state before queueing so a pre-start false value can never be
        // mistaken for completion; the native worker clears it only after its final action.
        shape.Cleaning.SetValue(null, true, null);
        cleanupRequested = true;
        try
        {
            shape.Cleanup.Invoke(null, null);
        }
        catch
        {
            cleanupRequested = false;
            shape.Cleaning.SetValue(null, false, null);
            throw;
        }
    }

    public bool TryConfirmCleanup(out string reason)
    {
        if (disposed)
        {
            reason = string.Empty;
            return true;
        }
        if (!cleanupRequested)
        {
            reason = "DPA cleanup has not been requested.";
            return false;
        }
        var problems = DpaNativeState.Problems(shape, harmony);
        if (problems.Count != 0)
        {
            reason = "DPA native cleanup is pending or incomplete: " + string.Join(", ", problems) + ".";
            return false;
        }
        if ((bool)shape.DisableThreading.GetValue(null)! != previousDisableThreading)
        {
            reason = "DPA threaded-patching setting was not restored.";
            return false;
        }
        disposed = true;
        reason = string.Empty;
        return true;
    }

    public void Dispose()
    {
        if (disposed) return;
        if (!cleanupRequested) RequestCleanup();
        if (!TryConfirmCleanup(out var reason)) throw new InvalidOperationException(reason);
    }

    private void RequireUsable()
    {
        if (disposed) throw new ObjectDisposedException(nameof(DpaRuntimeRun));
    }

    private static void ValidateText(string value, int maximumUtf8Bytes, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("DPA " + field + " is empty.");
        if (Encoding.UTF8.GetByteCount(value) > maximumUtf8Bytes)
            throw new InvalidOperationException($"DPA {field} exceeds {maximumUtf8Bytes} UTF-8 bytes.");
    }
}

[DataContract]
internal sealed class DpaDiagnosticCapture
{
    public const string SchemaValue = "RimWorldDevGateway/DpaDiagnostic/v1";

    public DpaDiagnosticCapture(
        DpaAssemblyIdentity assembly, string workloadVersion, string category,
        IReadOnlyList<string> selectors, IReadOnlyList<string> checkpoints,
        IReadOnlyList<DpaDiagnosticEntry> entries)
    {
        Assembly = assembly;
        WorkloadVersion = workloadVersion;
        Category = category;
        Selectors = selectors.ToArray();
        Checkpoints = checkpoints.ToArray();
        Entries = entries.ToArray();
    }

    [DataMember(Name = "schema", Order = 1)] public string Schema { get; private set; } = SchemaValue;
    [DataMember(Name = "kind", Order = 2)] public string Kind { get; private set; } = "diagnostic";
    [DataMember(Name = "profiler", Order = 3)] public string Profiler { get; private set; } = "dpa";
    [DataMember(Name = "baselineEligible", Order = 4)] public bool BaselineEligible { get; private set; } = false;
    [DataMember(Name = "assembly", Order = 5)] public DpaAssemblyIdentity Assembly { get; private set; }
    [DataMember(Name = "workloadVersion", Order = 6)] public string WorkloadVersion { get; private set; }
    [DataMember(Name = "category", Order = 7)] public string Category { get; private set; }
    [DataMember(Name = "selectors", Order = 8)] public string[] Selectors { get; private set; }
    [DataMember(Name = "checkpoints", Order = 9)] public string[] Checkpoints { get; private set; }
    [DataMember(Name = "entries", Order = 10)] public DpaDiagnosticEntry[] Entries { get; private set; }
}

[DataContract]
internal sealed class DpaDiagnosticEntry
{
    public DpaDiagnosticEntry(
        string key, string label, string type, string method, uint currentIndex,
        bool empty, IReadOnlyList<DpaDiagnosticSample> samples)
    {
        Key = key; Label = label; Type = type; Method = method; CurrentIndex = currentIndex;
        Empty = empty; Samples = samples.ToArray();
    }

    [DataMember(Name = "key", Order = 1)] public string Key { get; private set; }
    [DataMember(Name = "label", Order = 2)] public string Label { get; private set; }
    [DataMember(Name = "type", Order = 3)] public string Type { get; private set; }
    [DataMember(Name = "method", Order = 4)] public string Method { get; private set; }
    [DataMember(Name = "currentIndex", Order = 5)] public uint CurrentIndex { get; private set; }
    [DataMember(Name = "empty", Order = 6)] public bool Empty { get; private set; }
    [DataMember(Name = "samples", Order = 7)] public DpaDiagnosticSample[] Samples { get; private set; }
}

[DataContract]
internal sealed class DpaDiagnosticSample
{
    public DpaDiagnosticSample(int index, int calls, double milliseconds)
    {
        Index = index; Calls = calls; Milliseconds = milliseconds;
    }

    [DataMember(Name = "index", Order = 1)] public int Index { get; private set; }
    [DataMember(Name = "calls", Order = 2)] public int Calls { get; private set; }
    [DataMember(Name = "milliseconds", Order = 3)] public double Milliseconds { get; private set; }
}
