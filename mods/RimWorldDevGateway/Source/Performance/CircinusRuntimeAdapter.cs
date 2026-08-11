using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway.Performance;

internal interface ICircinusTypeSource
{
    Type? Resolve(string fullName);
}

internal sealed class AssemblyCircinusTypeSource : ICircinusTypeSource
{
    private readonly Assembly assembly;

    public AssemblyCircinusTypeSource(Assembly assembly)
    {
        this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public Type? Resolve(string fullName) => assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
}

internal sealed class CircinusAssemblyIdentity
{
    public CircinusAssemblyIdentity(
        string assemblyName,
        string assemblyIdentity,
        Guid moduleVersionId,
        long length,
        string sha256)
    {
        AssemblyName = assemblyName;
        AssemblyIdentity = assemblyIdentity;
        ModuleVersionId = moduleVersionId;
        Length = length;
        Sha256 = sha256;
    }

    public string AssemblyName { get; }
    public string AssemblyIdentity { get; }
    public Guid ModuleVersionId { get; }
    public long Length { get; }
    public string Sha256 { get; }
}

internal static class CircinusRuntimeAdapter
{
    public const string PackageId = "astryl.circinus";
    public const string AssemblyName = "Circinus";

    public static bool TryBind(
        bool packageActive,
        IEnumerable<Assembly> loadedAssemblies,
        out CircinusRuntimeBinding? binding,
        out string reason)
    {
        binding = null;
        if (!packageActive)
        {
            reason = $"Circinus package '{PackageId}' is inactive.";
            return false;
        }

        if (loadedAssemblies is null)
        {
            reason = "The loaded-assembly catalog is unavailable.";
            return false;
        }

        var matches = loadedAssemblies.Where(assembly =>
            assembly is not null && string.Equals(
                assembly.GetName().Name,
                AssemblyName,
                StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1)
        {
            reason = matches.Length == 0
                ? $"Circinus assembly '{AssemblyName}' is not loaded."
                : $"Expected exactly one loaded Circinus assembly '{AssemblyName}', observed {matches.Length}.";
            return false;
        }

        if (!TryIdentity(matches[0], out var identity, out reason)) return false;
        if (!CircinusShapeBinder.TryBind(
                new AssemblyCircinusTypeSource(matches[0]),
                out var shape,
                out reason)) return false;

        binding = new CircinusRuntimeBinding(identity!, shape!);
        reason = string.Empty;
        return true;
    }

    private static bool TryIdentity(
        Assembly assembly,
        out CircinusAssemblyIdentity? identity,
        out string reason)
    {
        identity = null;
        try
        {
            var location = assembly.Location;
            if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
            {
                reason = "Circinus assembly has no readable physical location for identity hashing.";
                return false;
            }

            var info = new FileInfo(location);
            string hash;
            using (var stream = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var algorithm = SHA256.Create())
            {
                hash = ToHex(algorithm.ComputeHash(stream));
            }

            var name = assembly.GetName();
            identity = new CircinusAssemblyIdentity(
                name.Name ?? AssemblyName,
                name.FullName ?? name.Name ?? AssemblyName,
                assembly.ManifestModule.ModuleVersionId,
                info.Length,
                hash);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Could not retain Circinus assembly identity: " + Describe(exception);
            return false;
        }
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes) builder.Append(value.ToString("X2"));
        return builder.ToString();
    }

    internal static string Describe(Exception exception)
    {
        var actual = exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
        var text = actual.GetType().FullName + ": " + actual.Message;
        return text.Length <= 512 ? text : text.Substring(0, 512);
    }
}

internal static class CircinusShapeBinder
{
    public static bool TryBind(
        ICircinusTypeSource source,
        out CircinusShape? shape,
        out string reason)
    {
        shape = null;
        if (source is null)
        {
            reason = "Circinus type source is unavailable.";
            return false;
        }

        try
        {
            var runDocument = Type(source, "Circinus.Contract.RunDocument");
            var runRecorder = Type(source, "Circinus.Session.RunRecorder");
            var instrumenter = Type(source, "Circinus.Profiling.Instrumenter");
            var profilerRegistry = Type(source, "Circinus.Profiling.ProfilerRegistry");
            var devProfiler = Type(source, "Circinus.Profiling.DevProfiler");
            var profileTarget = Type(source, "Circinus.Profiling.ProfileTarget");
            var targetCatalogue = Type(source, "Circinus.Profiling.TargetCatalogue");
            var methodRef = Type(source, "Circinus.Contract.MethodRef");
            var patchRef = Type(source, "Circinus.Contract.PatchRef");
            var harmonyIndex = Type(source, "Circinus.Identity.HarmonyIndex");
            var settings = Type(source, "Circinus.Bootstrap.CircinusSettings");
            var mod = Type(source, "Circinus.Bootstrap.CircinusMod");
            var consent = Type(source, "Circinus.UI.Window_Consent");
            var autoProfile = Type(source, "Circinus.UI.Window_AutoProfile");

            var documentToJson = Method(runDocument, "ToJson", isStatic: false, typeof(string));
            var documentId = Field(runDocument, "Id", isStatic: false, typeof(string));
            var documentSchemaMajor = Field(runDocument, "SchemaMajor", isStatic: false, typeof(int));
            var documentSchemaMinor = Field(runDocument, "SchemaMinor", isStatic: false, typeof(int));

            var recorderCurrent = Property(runRecorder, "Current", isStatic: true, runRecorder);
            var recorderStart = Method(runRecorder, "Start", false, typeof(bool), typeof(string));
            var recorderStop = Method(runRecorder, "Stop", false, runDocument);
            var recorderAddMarker = Method(runRecorder, "AddMarker", false, typeof(void), typeof(string), typeof(string));
            var recorderRecording = Property(runRecorder, "Recording", false, typeof(bool));
            var recorderActiveId = Property(runRecorder, "ActiveId", false, typeof(string));
            var recorderDocument = Property(runRecorder, "Document", false, runDocument);

            var armMethod = Method(instrumenter, "ArmMethod", true, typeof(bool), typeof(MethodBase), typeof(string));
            var armTarget = Method(instrumenter, "Arm", true, typeof(int), profileTarget, typeof(bool));
            var disarmMethod = Method(instrumenter, "DisarmMethod", true, typeof(bool), typeof(MethodBase));
            var disarmTarget = Method(instrumenter, "Disarm", true, typeof(int), profileTarget);
            var isPatched = Method(instrumenter, "IsPatched", true, typeof(bool), typeof(MethodBase));
            var disarmAll = Method(instrumenter, "DisarmAll", true, typeof(void));

            var registryEnabled = VolatileField(profilerRegistry, "Enabled", typeof(bool));
            var registryRecording = VolatileField(profilerRegistry, "Recording", typeof(bool));
            var registryFind = Method(profilerRegistry, "Find", true, devProfiler, typeof(MethodBase));
            var registryAll = Method(
                profilerRegistry,
                "All",
                true,
                typeof(List<>).MakeGenericType(devProfiler));
            var registryResetAll = Method(profilerRegistry, "ResetAll", true, typeof(void));
            var registryCycleCount = Property(profilerRegistry, "CycleCount", true, typeof(int));
            var registryRecordedFrames = Property(profilerRegistry, "RecordedFrames", true, typeof(int));
            var registrySkippedFrames = Property(profilerRegistry, "SkippedFrames", true, typeof(int));
            var registryDutyPct = Property(profilerRegistry, "DutyPct", true, typeof(double));

            var profilerMethod = ReadonlyField(devProfiler, "Method", typeof(MethodBase));
            var profilerHandArmed = Field(devProfiler, "HandArmed", false, typeof(bool));
            var profilerSampleShift = Property(devProfiler, "SampleShift", false, typeof(int));
            var profilerTotalCalls = Property(devProfiler, "TotalCalls", false, typeof(long));
            var profilerTimedCalls = Property(devProfiler, "TotalTimedCalls", false, typeof(long));
            var profilerEmpty = Property(devProfiler, "Empty", false, typeof(bool));
            var profilerCyclesSeen = Property(devProfiler, "CyclesSeen", false, typeof(int));

            var targetGet = Method(targetCatalogue, "Get", true, profileTarget, typeof(string));
            var targetKey = Field(profileTarget, "Key", false, typeof(string));
            var targetCategory = Field(profileTarget, "Category", false, typeof(string));
            var targetMethodCount = GetterOnlyProperty(profileTarget, "MethodCount", typeof(int));
            var methodRefKey = Field(methodRef, "Key", false, typeof(string));
            var patchRefKey = Field(patchRef, "Key", false, typeof(string));
            var patchRefsType = typeof(List<>).MakeGenericType(patchRef);
            var harmonyForPatchMethod = Method(
                harmonyIndex,
                "ForPatchMethod",
                true,
                patchRefsType,
                typeof(MethodBase));
            var harmonyRefOf = Method(harmonyIndex, "RefOf", true, methodRef, typeof(MethodBase));

            var settingsFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal)
            {
                ["autoStartProfiler"] = Field(settings, "autoStartProfiler", false, typeof(bool)),
                ["autoArmProfiler"] = Field(settings, "autoArmProfiler", false, typeof(bool)),
                ["armedTargetKeys"] = Field(settings, "armedTargetKeys", false, typeof(List<string>)),
                ["autoProfile"] = Field(settings, "autoProfile", false, typeof(bool)),
                ["autoProfileAsked"] = Field(settings, "autoProfileAsked", false, typeof(int)),
                ["showWarmupWindow"] = Field(settings, "showWarmupWindow", false, typeof(bool)),
                ["warmupSeconds"] = Field(settings, "warmupSeconds", false, typeof(int)),
                ["ingestEnabled"] = Field(settings, "ingestEnabled", false, typeof(bool)),
                ["consentVersion"] = Field(settings, "consentVersion", false, typeof(int)),
                ["autoRecord"] = Field(settings, "autoRecord", false, typeof(bool))
            };
            var liveSettings = Field(mod, "Settings", true, settings);
            Constant(consent, "DisclosureVersion", typeof(int), 2);
            Constant(autoProfile, "AskedVersion", typeof(int), 1);

            shape = new CircinusShape(
                runDocument,
                documentToJson,
                documentId,
                documentSchemaMajor,
                documentSchemaMinor,
                recorderCurrent,
                recorderStart,
                recorderStop,
                recorderAddMarker,
                recorderRecording,
                recorderActiveId,
                recorderDocument,
                armMethod,
                armTarget,
                disarmMethod,
                disarmTarget,
                isPatched,
                disarmAll,
                registryEnabled,
                registryRecording,
                registryFind,
                registryAll,
                registryResetAll,
                registryCycleCount,
                registryRecordedFrames,
                registrySkippedFrames,
                registryDutyPct,
                profilerMethod,
                profilerHandArmed,
                profilerSampleShift,
                profilerTotalCalls,
                profilerTimedCalls,
                profilerEmpty,
                profilerCyclesSeen,
                targetGet,
                targetKey,
                targetCategory,
                targetMethodCount,
                methodRefKey,
                patchRefKey,
                harmonyForPatchMethod,
                harmonyRefOf,
                liveSettings,
                settingsFields);
            reason = string.Empty;
            return true;
        }
        catch (CircinusShapeException exception)
        {
            reason = exception.Message;
            return false;
        }
        catch (Exception exception)
        {
            reason = "Circinus shape inspection failed: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private static Type Type(ICircinusTypeSource source, string name) =>
        source.Resolve(name) ?? throw Missing(name);

    private static MethodInfo Method(
        Type type,
        string name,
        bool isStatic,
        Type returnType,
        params Type[] parameters)
    {
        var matches = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(method => method.DeclaringType == type && method.Name == name && method.IsStatic == isStatic &&
                             method.ReturnType == returnType &&
                             method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters))
            .ToArray();
        if (matches.Length != 1)
        {
            throw Missing($"{type.FullName}.{name} exact public {(isStatic ? "static" : "instance")} " +
                          $"signature returning {returnType.FullName}");
        }

        return matches[0];
    }

    private static PropertyInfo Property(Type type, string name, bool isStatic, Type propertyType)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                              BindingFlags.DeclaredOnly);
        var getter = property?.GetGetMethod(nonPublic: false);
        if (property is null || property.PropertyType != propertyType || getter is null || getter.IsStatic != isStatic)
        {
            throw Missing($"{type.FullName}.{name} exact public {(isStatic ? "static" : "instance")} " +
                          $"property of {propertyType.FullName}");
        }

        return property;
    }

    private static PropertyInfo GetterOnlyProperty(Type type, string name, Type propertyType)
    {
        var property = Property(type, name, isStatic: false, propertyType);
        if (property.GetSetMethod(nonPublic: true) is not null)
        {
            throw Missing($"{type.FullName}.{name} public getter-only instance property");
        }

        return property;
    }

    private static FieldInfo Field(Type type, string name, bool isStatic, Type fieldType)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                       BindingFlags.DeclaredOnly);
        if (field is null || field.FieldType != fieldType || field.IsStatic != isStatic)
        {
            throw Missing($"{type.FullName}.{name} exact public {(isStatic ? "static" : "instance")} " +
                          $"field of {fieldType.FullName}");
        }

        return field;
    }

    private static FieldInfo ReadonlyField(Type type, string name, Type fieldType)
    {
        var field = Field(type, name, isStatic: false, fieldType);
        if (!field.IsInitOnly) throw Missing($"{type.FullName}.{name} public readonly instance field");
        return field;
    }

    private static FieldInfo VolatileField(Type type, string name, Type fieldType)
    {
        var field = Field(type, name, isStatic: true, fieldType);
        if (!field.GetRequiredCustomModifiers().Contains(typeof(IsVolatile)))
        {
            throw Missing($"{type.FullName}.{name} public static volatile field");
        }

        return field;
    }

    private static void Constant(Type type, string name, Type fieldType, object value)
    {
        var field = Field(type, name, isStatic: true, fieldType);
        if (!field.IsLiteral || !Equals(field.GetRawConstantValue(), value))
        {
            throw Missing($"{type.FullName}.{name} public constant value {value}");
        }
    }

    private static CircinusShapeException Missing(string member) =>
        new($"Unsupported Circinus shape: missing or changed {member}.");
}

internal sealed class CircinusShapeException : Exception
{
    public CircinusShapeException(string message) : base(message) { }
}

internal sealed class CircinusShape
{
    public CircinusShape(
        Type runDocumentType,
        MethodInfo documentToJson,
        FieldInfo documentId,
        FieldInfo documentSchemaMajor,
        FieldInfo documentSchemaMinor,
        PropertyInfo recorderCurrent,
        MethodInfo recorderStart,
        MethodInfo recorderStop,
        MethodInfo recorderAddMarker,
        PropertyInfo recorderRecording,
        PropertyInfo recorderActiveId,
        PropertyInfo recorderDocument,
        MethodInfo armMethod,
        MethodInfo armTarget,
        MethodInfo disarmMethod,
        MethodInfo disarmTarget,
        MethodInfo isPatched,
        MethodInfo disarmAll,
        FieldInfo registryEnabled,
        FieldInfo registryRecording,
        MethodInfo registryFind,
        MethodInfo registryAll,
        MethodInfo registryResetAll,
        PropertyInfo registryCycleCount,
        PropertyInfo registryRecordedFrames,
        PropertyInfo registrySkippedFrames,
        PropertyInfo registryDutyPct,
        FieldInfo profilerMethod,
        FieldInfo profilerHandArmed,
        PropertyInfo profilerSampleShift,
        PropertyInfo profilerTotalCalls,
        PropertyInfo profilerTimedCalls,
        PropertyInfo profilerEmpty,
        PropertyInfo profilerCyclesSeen,
        MethodInfo targetGet,
        FieldInfo targetKey,
        FieldInfo targetCategory,
        PropertyInfo targetMethodCount,
        FieldInfo methodRefKey,
        FieldInfo patchRefKey,
        MethodInfo harmonyForPatchMethod,
        MethodInfo harmonyRefOf,
        FieldInfo liveSettings,
        IReadOnlyDictionary<string, FieldInfo> settingsFields)
    {
        RunDocumentType = runDocumentType;
        DocumentToJson = documentToJson;
        DocumentId = documentId;
        DocumentSchemaMajor = documentSchemaMajor;
        DocumentSchemaMinor = documentSchemaMinor;
        RecorderCurrent = recorderCurrent;
        RecorderStart = recorderStart;
        RecorderStop = recorderStop;
        RecorderAddMarker = recorderAddMarker;
        RecorderRecording = recorderRecording;
        RecorderActiveId = recorderActiveId;
        RecorderDocument = recorderDocument;
        ArmMethod = armMethod;
        ArmTarget = armTarget;
        DisarmMethod = disarmMethod;
        DisarmTarget = disarmTarget;
        IsPatched = isPatched;
        DisarmAll = disarmAll;
        RegistryEnabled = registryEnabled;
        RegistryRecording = registryRecording;
        RegistryFind = registryFind;
        RegistryAll = registryAll;
        RegistryResetAll = registryResetAll;
        RegistryCycleCount = registryCycleCount;
        RegistryRecordedFrames = registryRecordedFrames;
        RegistrySkippedFrames = registrySkippedFrames;
        RegistryDutyPct = registryDutyPct;
        ProfilerMethod = profilerMethod;
        ProfilerHandArmed = profilerHandArmed;
        ProfilerSampleShift = profilerSampleShift;
        ProfilerTotalCalls = profilerTotalCalls;
        ProfilerTimedCalls = profilerTimedCalls;
        ProfilerEmpty = profilerEmpty;
        ProfilerCyclesSeen = profilerCyclesSeen;
        TargetGet = targetGet;
        TargetKey = targetKey;
        TargetCategory = targetCategory;
        TargetMethodCount = targetMethodCount;
        MethodRefKey = methodRefKey;
        PatchRefKey = patchRefKey;
        HarmonyForPatchMethod = harmonyForPatchMethod;
        HarmonyRefOf = harmonyRefOf;
        LiveSettings = liveSettings;
        SettingsFields = settingsFields;
    }

    public Type RunDocumentType { get; }
    public MethodInfo DocumentToJson { get; }
    public FieldInfo DocumentId { get; }
    public FieldInfo DocumentSchemaMajor { get; }
    public FieldInfo DocumentSchemaMinor { get; }
    public PropertyInfo RecorderCurrent { get; }
    public MethodInfo RecorderStart { get; }
    public MethodInfo RecorderStop { get; }
    public MethodInfo RecorderAddMarker { get; }
    public PropertyInfo RecorderRecording { get; }
    public PropertyInfo RecorderActiveId { get; }
    public PropertyInfo RecorderDocument { get; }
    public MethodInfo ArmMethod { get; }
    public MethodInfo ArmTarget { get; }
    public MethodInfo DisarmMethod { get; }
    public MethodInfo DisarmTarget { get; }
    public MethodInfo IsPatched { get; }
    public MethodInfo DisarmAll { get; }
    public FieldInfo RegistryEnabled { get; }
    public FieldInfo RegistryRecording { get; }
    public MethodInfo RegistryFind { get; }
    public MethodInfo RegistryAll { get; }
    public MethodInfo RegistryResetAll { get; }
    public PropertyInfo RegistryCycleCount { get; }
    public PropertyInfo RegistryRecordedFrames { get; }
    public PropertyInfo RegistrySkippedFrames { get; }
    public PropertyInfo RegistryDutyPct { get; }
    public FieldInfo ProfilerMethod { get; }
    public FieldInfo ProfilerHandArmed { get; }
    public PropertyInfo ProfilerSampleShift { get; }
    public PropertyInfo ProfilerTotalCalls { get; }
    public PropertyInfo ProfilerTimedCalls { get; }
    public PropertyInfo ProfilerEmpty { get; }
    public PropertyInfo ProfilerCyclesSeen { get; }
    public MethodInfo TargetGet { get; }
    public FieldInfo TargetKey { get; }
    public FieldInfo TargetCategory { get; }
    public PropertyInfo TargetMethodCount { get; }
    public FieldInfo MethodRefKey { get; }
    public FieldInfo PatchRefKey { get; }
    public MethodInfo HarmonyForPatchMethod { get; }
    public MethodInfo HarmonyRefOf { get; }
    public FieldInfo LiveSettings { get; }
    public IReadOnlyDictionary<string, FieldInfo> SettingsFields { get; }
}

internal sealed class CircinusRuntimeBinding
{
    internal const int SupportedSchemaMajor = 1;
    internal const int InspectedSchemaMinor = 15;
    private readonly CircinusShape shape;

    public CircinusRuntimeBinding(CircinusAssemblyIdentity identity, CircinusShape shape)
    {
        Identity = identity;
        this.shape = shape;
    }

    public CircinusAssemblyIdentity Identity { get; }
    public int SchemaMajor => SupportedSchemaMajor;
    public int SchemaMinor => InspectedSchemaMinor;

    public bool ValidateLocalOnlySettings(out string reason)
    {
        try
        {
            var settings = shape.LiveSettings.GetValue(null);
            if (settings is null)
            {
                reason = "CircinusMod.Settings is null.";
                return false;
            }

            return False(settings, "autoStartProfiler", out reason) &&
                   False(settings, "autoArmProfiler", out reason) &&
                   Empty(settings, "armedTargetKeys", out reason) &&
                   False(settings, "autoProfile", out reason) &&
                   Equal(settings, "autoProfileAsked", 1, out reason) &&
                   False(settings, "showWarmupWindow", out reason) &&
                   Equal(settings, "warmupSeconds", 0, out reason) &&
                   False(settings, "ingestEnabled", out reason) &&
                   Equal(settings, "consentVersion", 2, out reason) &&
                   False(settings, "autoRecord", out reason);
        }
        catch (Exception exception)
        {
            reason = "Could not validate Circinus local-only settings: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public bool TryBeginRun(string label, out CircinusRuntimeRun? run, out string reason)
    {
        run = null;
        if (string.IsNullOrWhiteSpace(label) || label.Length > 256)
        {
            reason = "Circinus run label must contain 1-256 characters.";
            return false;
        }

        if (!ValidateLocalOnlySettings(out reason)) return false;
        object? recorder = null;
        var started = false;
        try
        {
            recorder = shape.RecorderCurrent.GetValue(null);
            if (recorder is null)
            {
                reason = "Circinus RunRecorder.Current is null.";
                return false;
            }

            if ((bool)shape.RecorderRecording.GetValue(recorder))
            {
                reason = "Circinus already owns an active recording; refusing to replace it.";
                return false;
            }

            shape.RegistryEnabled.SetValue(null, false);
            shape.RegistryRecording.SetValue(null, false);
            shape.DisarmAll.Invoke(null, Array.Empty<object>());
            shape.RegistryResetAll.Invoke(null, Array.Empty<object>());
            started = (bool)shape.RecorderStart.Invoke(recorder, new object[] { label });
            if (!started)
            {
                reason = "Circinus RunRecorder refused the requested run.";
                return false;
            }

            var runId = (string?)shape.RecorderActiveId.GetValue(recorder);
            if (string.IsNullOrWhiteSpace(runId) || runId!.Length > 256)
            {
                shape.RecorderStop.Invoke(recorder, Array.Empty<object>());
                reason = "Circinus started without one bounded native run identity.";
                return false;
            }

            var document = shape.RecorderDocument.GetValue(recorder);
            if (document is null || !shape.RunDocumentType.IsInstanceOfType(document))
            {
                TryStopRecorder(recorder);
                reason = "Circinus started without one exact RunDocument.";
                return false;
            }
            var documentId = (string?)shape.DocumentId.GetValue(document);
            var schemaMajor = (int)shape.DocumentSchemaMajor.GetValue(document);
            _ = (int)shape.DocumentSchemaMinor.GetValue(document);
            if (!string.Equals(documentId, runId, StringComparison.Ordinal))
            {
                TryStopRecorder(recorder);
                reason = $"Circinus active run identity '{runId}' does not match its live document '{documentId}'.";
                return false;
            }
            if (schemaMajor != SupportedSchemaMajor)
            {
                TryStopRecorder(recorder);
                reason = $"Circinus started with unsupported schema major {schemaMajor}; expected {SupportedSchemaMajor}.";
                return false;
            }

            run = new CircinusRuntimeRun(this, shape, recorder, runId);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            if (started && recorder is not null) TryStopRecorder(recorder);
            reason = "Could not start Circinus run: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private void TryStopRecorder(object recorder)
    {
        try
        {
            shape.RegistryRecording.SetValue(null, false);
            shape.RegistryEnabled.SetValue(null, false);
            shape.RecorderStop.Invoke(recorder, Array.Empty<object>());
        }
        catch
        {
            // Preserve the primary acquisition failure; later isolated-process cleanup remains available.
        }
    }

    private bool False(object settings, string name, out string reason) =>
        Equal(settings, name, false, out reason);

    private bool Equal(object settings, string name, object expected, out string reason)
    {
        var actual = shape.SettingsFields[name].GetValue(settings);
        if (Equals(actual, expected))
        {
            reason = string.Empty;
            return true;
        }

        reason = $"Circinus setting {name} must be {expected}, observed {actual}.";
        return false;
    }

    private bool Empty(object settings, string name, out string reason)
    {
        var value = shape.SettingsFields[name].GetValue(settings) as ICollection;
        if (value is { Count: 0 })
        {
            reason = string.Empty;
            return true;
        }

        reason = $"Circinus setting {name} must be an empty list.";
        return false;
    }
}

internal sealed class CircinusCapture
{
    public CircinusCapture(
        string runId,
        int schemaMajor,
        int schemaMinor,
        string inMemoryJson,
        string persistedJson,
        IReadOnlyList<CircinusProfilerSidecar> sidecars)
    {
        RunId = runId;
        SchemaMajor = schemaMajor;
        SchemaMinor = schemaMinor;
        InMemoryJson = inMemoryJson;
        PersistedJson = persistedJson;
        Sidecars = sidecars;
    }

    public string RunId { get; }
    public int SchemaMajor { get; }
    public int SchemaMinor { get; }
    public string InMemoryJson { get; }
    public string PersistedJson { get; }
    public IReadOnlyList<CircinusProfilerSidecar> Sidecars { get; }
}

internal enum CircinusRowKind
{
    Method,
    Patch
}

[DataContract]
internal sealed class CircinusProfilerSidecar
{
    public CircinusProfilerSidecar(
        string methodIdentity,
        CircinusRowKind rowKind,
        string rowKey,
        int ambiguousTargetCount,
        bool handArmed,
        int sampleShift,
        long totalCalls,
        long totalTimedCalls,
        bool empty,
        int cyclesSeen,
        string? noRowReason)
    {
        MethodIdentity = methodIdentity;
        RowKind = rowKind;
        RowKey = rowKey;
        AmbiguousTargetCount = ambiguousTargetCount;
        HandArmed = handArmed;
        SampleShift = sampleShift;
        TotalCalls = totalCalls;
        TotalTimedCalls = totalTimedCalls;
        Empty = empty;
        CyclesSeen = cyclesSeen;
        NoRowReason = noRowReason;
    }

    [DataMember(Name = "methodIdentity", Order = 1)] public string MethodIdentity { get; private set; }
    [DataMember(Name = "rowKind", Order = 2)] public CircinusRowKind RowKind { get; private set; }
    [DataMember(Name = "rowKey", Order = 3)] public string RowKey { get; private set; }
    [DataMember(Name = "ambiguousTargetCount", Order = 4)] public int AmbiguousTargetCount { get; private set; }
    [DataMember(Name = "handArmed", Order = 5)] public bool HandArmed { get; private set; }
    [DataMember(Name = "sampleShift", Order = 6)] public int SampleShift { get; private set; }
    [DataMember(Name = "totalCalls", Order = 7)] public long TotalCalls { get; private set; }
    [DataMember(Name = "totalTimedCalls", Order = 8)] public long TotalTimedCalls { get; private set; }
    [DataMember(Name = "empty", Order = 9)] public bool Empty { get; private set; }
    [DataMember(Name = "cyclesSeen", Order = 10)] public int CyclesSeen { get; private set; }
    [DataMember(Name = "noRowReason", EmitDefaultValue = false, Order = 11)] public string? NoRowReason { get; private set; }
}

internal sealed class CircinusRuntimeRun : IDisposable
{
    public const int MaximumMethodRows = 1000;
    public const int MaximumPatchRows = 3000;
    private readonly CircinusShape shape;
    private readonly CircinusRuntimeBinding binding;
    private readonly object recorder;
    private readonly string runId;
    private readonly Dictionary<MethodBase, CircinusOwnedMethod> methods = new();
    private readonly HashSet<MethodBase> individuallyArmedMethods = new();
    private readonly Dictionary<string, object> targets = new(StringComparer.Ordinal);
    private bool stopped;
    private bool disposed;

    public CircinusRuntimeRun(
        CircinusRuntimeBinding binding,
        CircinusShape shape,
        object recorder,
        string runId)
    {
        this.binding = binding;
        this.shape = shape;
        this.recorder = recorder;
        this.runId = runId;
    }

    public string RunId => runId;

    public bool TryArmSelection(PerformanceMethodSelection selection, out string reason)
    {
        if (!CanMutate(out reason)) return false;
        if (selection is null)
        {
            reason = "Circinus performance method selection is null.";
            return false;
        }

        foreach (var method in selection.Methods)
        {
            var label = method.Categories.FirstOrDefault() ?? "performance";
            if (TryArmMethod(method.Method, label, out reason)) continue;
            Dispose();
            reason = "Could not arm the complete performance method selection: " + reason;
            return false;
        }

        foreach (var target in selection.CircinusTargets)
        {
            if (TryArmTarget(target, out reason)) continue;
            Dispose();
            reason = "Could not arm the complete Circinus target selection: " + reason;
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryArmMethod(MethodBase method, string label, out string reason)
    {
        if (!CanMutate(out reason)) return false;
        if (method is null)
        {
            reason = "Circinus method is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(label) || label.Length > 256)
        {
            reason = "Circinus method label must contain 1-256 characters.";
            return false;
        }

        if (methods.ContainsKey(method))
        {
            reason = $"Circinus method '{method}' is already registered by this run.";
            return false;
        }
        if (!binding.ValidateLocalOnlySettings(out reason)) return false;

        var armAttempted = false;
        try
        {
            if ((bool)shape.IsPatched.Invoke(null, new object[] { method }))
            {
                reason = $"Circinus method '{method}' is already armed outside this run.";
                return false;
            }

            if (!TryResolveRowIdentity(method, out var row, out reason)) return false;
            if (!CanAddRows(new[] { row! }, out reason)) return false;
            armAttempted = true;
            if (!(bool)shape.ArmMethod.Invoke(null, new object[] { method, label }))
            {
                reason = $"Circinus refused to arm method '{method}'.";
                return false;
            }

            var profiler = shape.RegistryFind.Invoke(null, new object[] { method });
            if (profiler is null)
            {
                shape.DisarmMethod.Invoke(null, new object[] { method });
                reason = $"Circinus did not expose the profiler for '{method}'.";
                return false;
            }

            shape.ProfilerHandArmed.SetValue(profiler, true);
            methods.Add(method, new CircinusOwnedMethod(method, row!));
            individuallyArmedMethods.Add(method);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            if (armAttempted) TryInvoke(shape.DisarmMethod, method);
            reason = "Could not arm Circinus method: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public bool TryArmTarget(string key, out string reason)
    {
        if (!CanMutate(out reason)) return false;
        if (string.IsNullOrWhiteSpace(key) || key.Length > 256)
        {
            reason = "Circinus target key must contain 1-256 characters.";
            return false;
        }

        if (targets.ContainsKey(key))
        {
            reason = $"Circinus target '{key}' is already registered by this run.";
            return false;
        }
        if (!binding.ValidateLocalOnlySettings(out reason)) return false;

        object? target = null;
        var armAttempted = false;
        try
        {
            target = shape.TargetGet.Invoke(null, new object[] { key });
            if (target is null)
            {
                reason = $"Circinus target '{key}' did not resolve; the selector is stale or unsupported.";
                return false;
            }

            var before = RegistryMethods();
            armAttempted = true;
            var armed = (int)shape.ArmTarget.Invoke(null, new[] { target, (object)true });
            var methodCount = (int)shape.TargetMethodCount.GetValue(target);
            if (armed <= 0 || methodCount != armed)
            {
                TryInvoke(shape.DisarmTarget, target);
                armAttempted = false;
                reason = $"Circinus target '{key}' armed {armed} methods but reports {methodCount} owned methods.";
                return false;
            }

            var addedMethods = RegistryMethods()
                .Where(method => !before.Contains(method) && !methods.ContainsKey(method))
                .OrderBy(MethodIdentity, StringComparer.Ordinal)
                .ToArray();
            if (addedMethods.Length != armed)
            {
                TryInvoke(shape.DisarmTarget, target);
                armAttempted = false;
                reason = $"Circinus target '{key}' armed {armed} methods but exposed {addedMethods.Length} new profilers.";
                return false;
            }

            var owned = new List<CircinusOwnedMethod>(addedMethods.Length);
            foreach (var method in addedMethods)
            {
                var profiler = shape.RegistryFind.Invoke(null, new object[] { method });
                if (profiler is null || !(bool)shape.ProfilerHandArmed.GetValue(profiler))
                {
                    TryInvoke(shape.DisarmTarget, target);
                    armAttempted = false;
                    reason = $"Circinus target '{key}' exposed a profiler that is not HandArmed.";
                    return false;
                }
                if (!TryResolveRowIdentity(method, out var row, out reason))
                {
                    TryInvoke(shape.DisarmTarget, target);
                    armAttempted = false;
                    return false;
                }
                owned.Add(new CircinusOwnedMethod(method, row!));
            }

            if (!CanAddRows(owned.Select(item => item.Row), out reason))
            {
                TryInvoke(shape.DisarmTarget, target);
                armAttempted = false;
                return false;
            }

            foreach (var item in owned) methods.Add(item.Method, item);
            targets.Add(key, target);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            if (armAttempted && target is not null) TryInvoke(shape.DisarmTarget, target);
            reason = "Could not arm Circinus target: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public void AddMarker(string name, string value)
    {
        ThrowIfStopped();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256 || value is null || value.Length > 1024)
            throw new ArgumentException("Circinus marker name/value exceeds its bounded contract.");
        shape.RecorderAddMarker.Invoke(recorder, new object[] { name, value });
    }

    public void SetSampling(bool enabled)
    {
        ThrowIfStopped();
        if (enabled && !binding.ValidateLocalOnlySettings(out var reason))
            throw new InvalidOperationException(reason);
        shape.RegistryEnabled.SetValue(null, enabled);
        shape.RegistryRecording.SetValue(null, enabled);
    }

    public bool TryStopAndCapture(
        Func<string, string?> persistedJsonReader,
        out CircinusCapture? capture,
        out string reason)
    {
        capture = null;
        if (!CanMutate(out reason)) return false;
        if (persistedJsonReader is null)
        {
            reason = "Circinus persisted JSON reader is missing.";
            return false;
        }

        try
        {
            SetSampling(false);
            if (!TrySnapshotSidecars(out var sidecars, out reason)) return false;
            var document = shape.RecorderStop.Invoke(recorder, Array.Empty<object>());
            stopped = true;
            if (document is null || !shape.RunDocumentType.IsInstanceOfType(document))
            {
                reason = "Circinus Stop returned no exact RunDocument.";
                return false;
            }

            var id = (string?)shape.DocumentId.GetValue(document) ?? string.Empty;
            var schemaMajor = (int)shape.DocumentSchemaMajor.GetValue(document);
            var schemaMinor = (int)shape.DocumentSchemaMinor.GetValue(document);
            var raw = (string?)shape.DocumentToJson.Invoke(document, Array.Empty<object>()) ?? string.Empty;
            if (!string.Equals(id, runId, StringComparison.Ordinal))
            {
                reason = $"Circinus stopped run identity '{id}' does not match requested '{runId}'.";
                return false;
            }

            if (schemaMajor != CircinusRuntimeBinding.SupportedSchemaMajor)
            {
                reason = $"Circinus returned unsupported schema major {schemaMajor}; expected " +
                         $"{CircinusRuntimeBinding.SupportedSchemaMajor}.";
                return false;
            }

            if (!TryReadIdentity(raw, out var inMemory, out reason)) return false;
            if (!string.Equals(inMemory!.Id, id, StringComparison.Ordinal) ||
                inMemory.SchemaMajor != schemaMajor || inMemory.SchemaMinor != schemaMinor)
            {
                reason = "Circinus in-memory JSON identity/schema does not match the stopped RunDocument.";
                return false;
            }

            var persistedJson = persistedJsonReader(id);
            if (string.IsNullOrWhiteSpace(persistedJson))
            {
                reason = "Circinus persisted JSON is missing after the native stop operation.";
                return false;
            }

            var persistedText = persistedJson!;
            if (!TryReadIdentity(persistedText, out var persisted, out reason)) return false;
            if (!string.Equals(persisted!.Id, id, StringComparison.Ordinal) ||
                persisted.SchemaMajor != schemaMajor || persisted.SchemaMinor != schemaMinor)
            {
                reason = "Circinus persisted run identity/schema does not match the stopped in-memory document.";
                return false;
            }

            if (!TryCorrelateRows(raw, sidecars!, out reason)) return false;
            capture = new CircinusCapture(id, schemaMajor, schemaMinor, raw, persistedText, sidecars!);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            stopped = !(bool)shape.RecorderRecording.GetValue(recorder);
            reason = "Could not stop/capture Circinus run: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        var failures = new List<Exception>();
        try
        {
            shape.RegistryRecording.SetValue(null, false);
            shape.RegistryEnabled.SetValue(null, false);
        }
        catch (Exception exception)
        {
            failures.Add(new InvalidOperationException(
                "Circinus registry sampling could not be disabled.", exception));
        }

        if (!stopped)
        {
            try
            {
                if ((bool)shape.RecorderRecording.GetValue(recorder))
                    shape.RecorderStop.Invoke(recorder, Array.Empty<object>());
                if ((bool)shape.RecorderRecording.GetValue(recorder))
                    throw new InvalidOperationException("Circinus recorder remained active after Stop().");
                stopped = true;
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException(
                    "Circinus recorder could not be stopped.", exception));
            }
        }

        foreach (var method in individuallyArmedMethods)
        {
            try
            {
                shape.DisarmMethod.Invoke(null, new object[] { method });
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException(
                    $"Circinus method '{MethodIdentity(method)}' could not be disarmed.", exception));
            }
        }
        foreach (var pair in targets)
        {
            try
            {
                shape.DisarmTarget.Invoke(null, new[] { pair.Value });
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException(
                    $"Circinus target '{pair.Key}' could not be disarmed.", exception));
            }
            try
            {
                if ((int)shape.TargetMethodCount.GetValue(pair.Value) != 0)
                    throw new InvalidOperationException(
                        $"Circinus target '{pair.Key}' retained armed methods after cleanup.");
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        try
        {
            if ((bool)shape.RegistryRecording.GetValue(null) ||
                (bool)shape.RegistryEnabled.GetValue(null))
                throw new InvalidOperationException("Circinus registry remained enabled after cleanup.");
            var registered = RegistryMethods();
            foreach (var method in methods.Keys)
            {
                var patched = (bool)shape.IsPatched.Invoke(null, new object[] { method });
                if (patched || registered.Contains(method))
                    failures.Add(new InvalidOperationException(
                        $"Circinus retained owned profiler '{MethodIdentity(method)}' after cleanup."));
            }
        }
        catch (Exception exception)
        {
            failures.Add(new InvalidOperationException(
                "Circinus owned-profiler cleanup could not be verified.", exception));
        }

        if (failures.Count > 0)
            throw new AggregateException(
                "Circinus run cleanup retained retryable recorder or profiler ownership.",
                failures);

        individuallyArmedMethods.Clear();
        methods.Clear();
        targets.Clear();
        disposed = true;
    }

    private bool CanMutate(out string reason)
    {
        if (disposed)
        {
            reason = "Circinus run is disposed.";
            return false;
        }

        if (stopped)
        {
            reason = "Circinus run is already stopped.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private HashSet<MethodBase> RegistryMethods()
    {
        var profilers = shape.RegistryAll.Invoke(null, Array.Empty<object>()) as IList ??
                        throw new InvalidOperationException("Circinus ProfilerRegistry.All returned no list.");
        var result = new HashSet<MethodBase>();
        foreach (var profiler in profilers)
        {
            if (profiler is null) continue;
            if (shape.ProfilerMethod.GetValue(profiler) is MethodBase method) result.Add(method);
        }
        return result;
    }

    private bool TryResolveRowIdentity(
        MethodBase method,
        out CircinusRowIdentity? identity,
        out string reason)
    {
        try
        {
            var patchRefs = shape.HarmonyForPatchMethod.Invoke(null, new object[] { method }) as IList;
            CircinusRowKind kind;
            string? key;
            var ambiguousTargetCount = 0;
            if (patchRefs is { Count: > 0 })
            {
                kind = CircinusRowKind.Patch;
                ambiguousTargetCount = patchRefs.Count;
                var firstPatchRef = patchRefs[0];
                key = firstPatchRef is null ? null : shape.PatchRefKey.GetValue(firstPatchRef) as string;
                if (string.IsNullOrWhiteSpace(key) || key!.Length > 256)
                {
                    identity = null;
                    reason = $"Circinus patch row identity is missing or unbounded for '{MethodIdentity(method)}'.";
                    return false;
                }

                // Circinus 1.15 emits one PatchStat under ForPatchMethod(method)[0] and records
                // additional native attachments through AmbiguousTargets. Mirror that exact
                // identity while retaining the complete native attachment count in the sidecar.
                identity = new CircinusRowIdentity(kind, key, ambiguousTargetCount);
                reason = string.Empty;
                return true;
            }
            else
            {
                kind = CircinusRowKind.Method;
                var methodRef = shape.HarmonyRefOf.Invoke(null, new object[] { method });
                key = methodRef is null ? null : shape.MethodRefKey.GetValue(methodRef) as string;
            }

            if (string.IsNullOrWhiteSpace(key) || key!.Length > 256)
            {
                identity = null;
                reason = $"Circinus row identity is missing or unbounded for '{MethodIdentity(method)}'.";
                return false;
            }

            identity = new CircinusRowIdentity(kind, key, ambiguousTargetCount);
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            identity = null;
            reason = $"Could not resolve Circinus row identity for '{MethodIdentity(method)}': " +
                     CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    private bool CanAddRows(IEnumerable<CircinusRowIdentity> rows, out string reason)
    {
        var additions = rows.ToArray();
        var existingMethods = methods.Values.Count(item => item.Row.Kind == CircinusRowKind.Method);
        var existingPatches = methods.Values.Count(item => item.Row.Kind == CircinusRowKind.Patch);
        var methodRows = existingMethods + additions.Count(item => item.Kind == CircinusRowKind.Method);
        var patchRows = existingPatches + additions.Count(item => item.Kind == CircinusRowKind.Patch);
        if (methodRows > MaximumMethodRows)
        {
            reason = $"Circinus run would exceed the native {MaximumMethodRows}-method-row output cap.";
            return false;
        }
        if (patchRows > MaximumPatchRows)
        {
            reason = $"Circinus run would exceed the native {MaximumPatchRows}-patch-row output cap.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool TrySnapshotSidecars(
        out IReadOnlyList<CircinusProfilerSidecar>? sidecars,
        out string reason)
    {
        var captured = new List<CircinusProfilerSidecar>(methods.Count);
        foreach (var owned in methods.Values.OrderBy(item => MethodIdentity(item.Method), StringComparer.Ordinal))
        {
            var method = owned.Method;
            try
            {
                var profiler = shape.RegistryFind.Invoke(null, new object[] { method });
                if (profiler is null)
                {
                    sidecars = null;
                    reason = $"Circinus profiler sidecar is missing for '{MethodIdentity(method)}'.";
                    return false;
                }

                var empty = (bool)shape.ProfilerEmpty.GetValue(profiler);
                captured.Add(new CircinusProfilerSidecar(
                    MethodIdentity(method),
                    owned.Row.Kind,
                    owned.Row.Key,
                    owned.Row.AmbiguousTargetCount,
                    (bool)shape.ProfilerHandArmed.GetValue(profiler),
                    (int)shape.ProfilerSampleShift.GetValue(profiler),
                    (long)shape.ProfilerTotalCalls.GetValue(profiler),
                    (long)shape.ProfilerTimedCalls.GetValue(profiler),
                    empty,
                    (int)shape.ProfilerCyclesSeen.GetValue(profiler),
                    empty ? "empty-or-uninvoked" : null));
            }
            catch (Exception exception)
            {
                sidecars = null;
                reason = $"Could not snapshot Circinus profiler '{MethodIdentity(method)}': " +
                         CircinusRuntimeAdapter.Describe(exception);
                return false;
            }
        }

        sidecars = captured;
        reason = string.Empty;
        return true;
    }

    private static bool TryCorrelateRows(
        string rawJson,
        IReadOnlyList<CircinusProfilerSidecar> sidecars,
        out string reason)
    {
        ParsedRunRows? rows;
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawJson), writable: false);
            rows = (ParsedRunRows?)new DataContractJsonSerializer(typeof(ParsedRunRows))
                .ReadObject(stream);
        }
        catch (Exception exception)
        {
            reason = "Circinus row correlation could not parse native JSON: " +
                     CircinusRuntimeAdapter.Describe(exception);
            return false;
        }

        if (rows is null)
        {
            reason = "Circinus row correlation received no native run document.";
            return false;
        }

        var methodCounts = CountRows(rows.Methods, row => row.Method?.Key);
        var patchCounts = CountRows(rows.Patches, row => row.Patch?.Key);
        foreach (var sidecar in sidecars)
        {
            var counts = sidecar.RowKind == CircinusRowKind.Patch ? patchCounts : methodCounts;
            counts.TryGetValue(sidecar.RowKey, out var matches);
            if (sidecar.Empty)
            {
                if (matches == 0) continue;
                reason = $"Circinus row correlation for '{sidecar.RowKey}' found {matches} row(s) for an empty profiler.";
                return false;
            }

            if (matches != 1)
            {
                var dropped = sidecar.RowKind == CircinusRowKind.Patch ? rows.PatchesDropped : 0;
                reason = $"Circinus row correlation for non-empty '{sidecar.RowKey}' found {matches} row(s)" +
                         (dropped > 0 ? $" while {dropped} patch row(s) were dropped." : ".");
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static Dictionary<string, int> CountRows<TRow>(
        IEnumerable<TRow>? rows,
        Func<TRow, string?> key)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows ?? Array.Empty<TRow>())
        {
            var value = key(row);
            if (string.IsNullOrWhiteSpace(value)) continue;
            result.TryGetValue(value!, out var count);
            result[value!] = count + 1;
        }

        return result;
    }

    private static string MethodIdentity(MethodBase method)
    {
        var type = method.DeclaringType?.FullName ?? "?";
        var parameters = string.Join(",", method.GetParameters().Select(parameter =>
            parameter.ParameterType.FullName ?? parameter.ParameterType.Name));
        return (method.DeclaringType?.Assembly.GetName().Name ?? "?") + ":" + type + "." +
               method.Name + "(" + parameters + ")";
    }

    private sealed class CircinusOwnedMethod
    {
        public CircinusOwnedMethod(MethodBase method, CircinusRowIdentity row)
        {
            Method = method;
            Row = row;
        }

        public MethodBase Method { get; }
        public CircinusRowIdentity Row { get; }
    }

    private sealed class CircinusRowIdentity
    {
        public CircinusRowIdentity(
            CircinusRowKind kind,
            string key,
            int ambiguousTargetCount)
        {
            Kind = kind;
            Key = key;
            AmbiguousTargetCount = ambiguousTargetCount;
        }

        public CircinusRowKind Kind { get; }
        public string Key { get; }
        public int AmbiguousTargetCount { get; }
    }

    private void ThrowIfStopped()
    {
        if (!CanMutate(out var reason)) throw new InvalidOperationException(reason);
    }

    private static void TryInvoke(MethodInfo method, object argument)
    {
        try { method.Invoke(null, new[] { argument }); }
        catch { }
    }

    private static bool TryReadIdentity(
        string json,
        out PersistedRunIdentity? identity,
        out string reason)
    {
        identity = null;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(bytes, writable: false);
            identity = (PersistedRunIdentity?)new DataContractJsonSerializer(typeof(PersistedRunIdentity))
                .ReadObject(stream);
            if (identity is null || string.IsNullOrWhiteSpace(identity.Id))
            {
                reason = "Circinus persisted JSON has no run identity.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = "Circinus persisted JSON is invalid: " + CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    [DataContract]
    private sealed class PersistedRunIdentity
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "schemaMajor", IsRequired = true)]
        public int SchemaMajor { get; set; }

        [DataMember(Name = "schemaMinor", IsRequired = true)]
        public int SchemaMinor { get; set; }
    }

    [DataContract]
    private sealed class ParsedRunRows
    {
        [DataMember(Name = "methods", EmitDefaultValue = false)]
        public List<ParsedMethodRow>? Methods { get; set; }

        [DataMember(Name = "patches", EmitDefaultValue = false)]
        public List<ParsedPatchRow>? Patches { get; set; }

        [DataMember(Name = "patchesDropped", EmitDefaultValue = false)]
        public int PatchesDropped { get; set; }
    }

    [DataContract]
    private sealed class ParsedMethodRow
    {
        [DataMember(Name = "method", EmitDefaultValue = false)]
        public ParsedRowReference? Method { get; set; }
    }

    [DataContract]
    private sealed class ParsedPatchRow
    {
        [DataMember(Name = "patch", EmitDefaultValue = false)]
        public ParsedRowReference? Patch { get; set; }
    }

    [DataContract]
    private sealed class ParsedRowReference
    {
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string? Key { get; set; }
    }
}
