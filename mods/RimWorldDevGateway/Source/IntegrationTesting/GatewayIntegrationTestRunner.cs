using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway;

public sealed class GatewayIntegrationTestAssemblySource
{
    public GatewayIntegrationTestAssemblySource(string owningPackageId, string identity)
    {
        OwningPackageId = string.IsNullOrWhiteSpace(owningPackageId)
            ? throw new ArgumentException("An owning package ID is required.", nameof(owningPackageId))
            : owningPackageId;
        Identity = string.IsNullOrWhiteSpace(identity)
            ? throw new ArgumentException("An assembly source identity is required.", nameof(identity))
            : identity;
    }

    public string OwningPackageId { get; }

    public string Identity { get; }
}

public interface IGatewayIntegrationTestAssemblyCatalog
{
    IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery();

    Assembly Load(GatewayIntegrationTestAssemblySource source);
}

public interface IGatewayIntegrationTestAssemblyDiscoveryCursor : IDisposable
{
    GatewayIntegrationTestAssemblyDiscoveryStep Advance();
}

public sealed class GatewayIntegrationTestAssemblyDiscoveryStep
{
    private GatewayIntegrationTestAssemblyDiscoveryStep(
        bool isComplete,
        GatewayIntegrationTestAssemblySource? source)
    {
        IsComplete = isComplete;
        Source = source;
    }

    public bool IsComplete { get; }

    public GatewayIntegrationTestAssemblySource? Source { get; }

    public static GatewayIntegrationTestAssemblyDiscoveryStep Progress() => new(false, null);

    public static GatewayIntegrationTestAssemblyDiscoveryStep Found(
        GatewayIntegrationTestAssemblySource source) =>
        new(false, source ?? throw new ArgumentNullException(nameof(source)));

    public static GatewayIntegrationTestAssemblyDiscoveryStep Complete() => new(true, null);
}

public sealed class GatewayIntegrationTestAssemblyDiscoveryCursor :
    IGatewayIntegrationTestAssemblyDiscoveryCursor
{
    private const int AbsoluteMaximumSources = 1024;
    private readonly IReadOnlyList<GatewayIntegrationTestAssemblySource> sources;
    private int index;
    private bool disposed;

    public GatewayIntegrationTestAssemblyDiscoveryCursor(
        IEnumerable<GatewayIntegrationTestAssemblySource> sources)
    {
        var boundedSources = (sources ?? throw new ArgumentNullException(nameof(sources)))
            .Where(source => source is not null)
            .OrderBy(source => source.OwningPackageId, StringComparer.Ordinal)
            .ThenBy(source => source.Identity, StringComparer.Ordinal)
            .Take(AbsoluteMaximumSources + 1)
            .ToArray();
        if (boundedSources.Length > AbsoluteMaximumSources)
        {
            throw new ArgumentException(
                "The integration-test assembly discovery cursor exceeds its absolute source limit.",
                nameof(sources));
        }

        this.sources = boundedSources;
    }

    public GatewayIntegrationTestAssemblyDiscoveryStep Advance()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayIntegrationTestAssemblyDiscoveryCursor));
        }

        return index < sources.Count
            ? GatewayIntegrationTestAssemblyDiscoveryStep.Found(sources[index++])
            : GatewayIntegrationTestAssemblyDiscoveryStep.Complete();
    }

    public void Dispose()
    {
        disposed = true;
    }
}

public interface IGatewayIntegrationTestArtifactStore
{
    bool IsAttached { get; }

    GatewayIntegrationTestSnapshot? CommittedSnapshot { get; }

    IGatewayIntegrationTestPersistenceOperation BeginPersist(GatewayIntegrationTestSnapshot snapshot);
}

public interface IGatewayIntegrationTestPersistenceOperation
{
    bool IsCompleted { get; }

    GatewayIntegrationTestPersistenceOutcome GetOutcome();
}

public sealed class GatewayIntegrationTestPersistenceOutcome
{
    private GatewayIntegrationTestPersistenceOutcome(
        bool succeeded,
        GatewayIntegrationTestSnapshot snapshot,
        Exception? failure)
    {
        Succeeded = succeeded;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Failure = failure;
    }

    public bool Succeeded { get; }

    public GatewayIntegrationTestSnapshot Snapshot { get; }

    public Exception? Failure { get; }

    public static GatewayIntegrationTestPersistenceOutcome Success(
        GatewayIntegrationTestSnapshot snapshot) => new(true, snapshot, null);

    public static GatewayIntegrationTestPersistenceOutcome Failed(
        GatewayIntegrationTestSnapshot snapshot,
        Exception failure) =>
        new(false, snapshot, failure ?? throw new ArgumentNullException(nameof(failure)));
}

internal sealed class GatewayIntegrationTestCompletedPersistenceOperation :
    IGatewayIntegrationTestPersistenceOperation
{
    private readonly GatewayIntegrationTestPersistenceOutcome outcome;

    public GatewayIntegrationTestCompletedPersistenceOperation(
        GatewayIntegrationTestPersistenceOutcome outcome)
    {
        this.outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
    }

    public bool IsCompleted => true;

    public GatewayIntegrationTestPersistenceOutcome GetOutcome() => outcome;
}

public interface IGatewayIntegrationTestBackgroundScheduler
{
    Task<T> Run<T>(Func<T> work);
}

public sealed class GatewayIntegrationTestBackgroundScheduler : IGatewayIntegrationTestBackgroundScheduler
{
    public Task<T> Run<T>(Func<T> work) => Task.Run(work ?? throw new ArgumentNullException(nameof(work)));
}

public interface IGatewayIntegrationTestReadiness
{
    bool IsMainThread { get; }

    bool IsReady(RunAt lifecyclePoint);
}

public sealed class GatewayIntegrationTestSnapshot
{
    public const int MaximumSerializedUtf8Bytes = 4 * 1024 * 1024;

    internal GatewayIntegrationTestSnapshot(
        bool enabled,
        string discoveryState,
        int discoveredAssemblyCount = 0,
        int discoveredTestCount = 0,
        IEnumerable<GatewayIntegrationTestAssemblySnapshot>? discoveredAssemblies = null,
        IEnumerable<GatewayIntegrationTestDescriptor>? discoveredTests = null,
        IEnumerable<GatewayIntegrationTestFailure>? failures = null,
        IEnumerable<GatewayIntegrationTestResult>? results = null,
        IEnumerable<GatewayIntegrationTestLifecycleSnapshot>? lifecyclePoints = null,
        int omittedFailureCount = 0,
        int omittedResultCount = 0)
    {
        Enabled = enabled;
        DiscoveryState = discoveryState;
        DiscoveredAssemblyCount = discoveredAssemblyCount;
        DiscoveredTestCount = discoveredTestCount;
        DiscoveredAssemblies = new ReadOnlyCollection<GatewayIntegrationTestAssemblySnapshot>(
            (discoveredAssemblies ?? Array.Empty<GatewayIntegrationTestAssemblySnapshot>()).ToArray());
        DiscoveredTests = new ReadOnlyCollection<GatewayIntegrationTestDescriptor>(
            (discoveredTests ?? Array.Empty<GatewayIntegrationTestDescriptor>()).ToArray());
        Failures = new ReadOnlyCollection<GatewayIntegrationTestFailure>(
            (failures ?? Array.Empty<GatewayIntegrationTestFailure>()).ToArray());
        Results = new ReadOnlyCollection<GatewayIntegrationTestResult>(
            (results ?? Array.Empty<GatewayIntegrationTestResult>()).ToArray());
        LifecyclePoints = new ReadOnlyCollection<GatewayIntegrationTestLifecycleSnapshot>(
            (lifecyclePoints ?? Array.Empty<GatewayIntegrationTestLifecycleSnapshot>()).ToArray());
        OmittedFailureCount = omittedFailureCount;
        OmittedResultCount = omittedResultCount;
    }

    public bool Enabled { get; }

    public string DiscoveryState { get; }

    public int DiscoveredAssemblyCount { get; }

    public int DiscoveredTestCount { get; }

    public IReadOnlyList<GatewayIntegrationTestAssemblySnapshot> DiscoveredAssemblies { get; }

    public IReadOnlyList<GatewayIntegrationTestDescriptor> DiscoveredTests { get; }

    public IReadOnlyList<GatewayIntegrationTestFailure> Failures { get; }

    public IReadOnlyList<GatewayIntegrationTestResult> Results { get; }

    public IReadOnlyList<GatewayIntegrationTestLifecycleSnapshot> LifecyclePoints { get; }

    public int OmittedFailureCount { get; }

    public int OmittedResultCount { get; }
}

public sealed class GatewayIntegrationTestAssemblySnapshot
{
    internal GatewayIntegrationTestAssemblySnapshot(
        string owningPackageId,
        string sourceIdentity,
        string assemblyIdentity)
    {
        OwningPackageId = owningPackageId;
        SourceIdentity = sourceIdentity;
        AssemblyIdentity = assemblyIdentity;
    }

    public string OwningPackageId { get; }
    public string SourceIdentity { get; }
    public string AssemblyIdentity { get; }
}

public sealed class GatewayIntegrationTestDescriptor
{
    internal GatewayIntegrationTestDescriptor(
        string owningPackageId,
        string assemblyIdentity,
        string testName,
        RunAt runAt)
    {
        OwningPackageId = owningPackageId;
        AssemblyIdentity = assemblyIdentity;
        TestName = testName;
        RunAt = runAt;
    }

    public string OwningPackageId { get; }
    public string AssemblyIdentity { get; }
    public string TestName { get; }
    public RunAt RunAt { get; }
}

public sealed class GatewayIntegrationTestLifecycleSnapshot
{
    internal GatewayIntegrationTestLifecycleSnapshot(
        RunAt runAt,
        string state,
        DateTimeOffset? startedUtc,
        DateTimeOffset? completedUtc,
        int executedCount,
        int passedCount,
        int failedCount)
    {
        RunAt = runAt;
        State = state;
        StartedUtc = startedUtc;
        CompletedUtc = completedUtc;
        ExecutedCount = executedCount;
        PassedCount = passedCount;
        FailedCount = failedCount;
    }

    public RunAt RunAt { get; }
    public string State { get; }
    public DateTimeOffset? StartedUtc { get; }
    public DateTimeOffset? CompletedUtc { get; }
    public int ExecutedCount { get; }
    public int PassedCount { get; }
    public int FailedCount { get; }
}

public sealed class GatewayIntegrationTestFailure
{
    internal GatewayIntegrationTestFailure(
        string code,
        string message,
        string? owningPackageId = null,
        string? assemblyIdentity = null,
        string? testName = null,
        string? exceptionType = null,
        string? stackTrace = null)
    {
        Code = code;
        Message = message;
        OwningPackageId = owningPackageId;
        AssemblyIdentity = assemblyIdentity;
        TestName = testName;
        ExceptionType = exceptionType;
        StackTrace = stackTrace;
    }

    public string Code { get; }

    public string Message { get; }

    public string? OwningPackageId { get; }

    public string? AssemblyIdentity { get; }

    public string? TestName { get; }

    public string? ExceptionType { get; }

    public string? StackTrace { get; }
}

public sealed class GatewayIntegrationTestResult
{
    internal GatewayIntegrationTestResult(
        string owningPackageId,
        string assemblyIdentity,
        string testName,
        RunAt runAt,
        string state,
        DateTimeOffset startedUtc,
        DateTimeOffset? completedUtc = null,
        long? durationMilliseconds = null,
        string? exceptionType = null,
        string? message = null,
        string? stackTrace = null)
    {
        OwningPackageId = owningPackageId;
        AssemblyIdentity = assemblyIdentity;
        TestName = testName;
        RunAt = runAt;
        State = state;
        StartedUtc = startedUtc;
        CompletedUtc = completedUtc;
        DurationMilliseconds = durationMilliseconds;
        ExceptionType = exceptionType;
        Message = message;
        StackTrace = stackTrace;
    }

    public string OwningPackageId { get; }

    public string AssemblyIdentity { get; }

    public string TestName { get; }

    public RunAt RunAt { get; }

    public string State { get; }

    public DateTimeOffset StartedUtc { get; }

    public DateTimeOffset? CompletedUtc { get; }

    public long? DurationMilliseconds { get; }

    public string? ExceptionType { get; }

    public string? Message { get; }

    public string? StackTrace { get; }
}

public sealed class GatewayIntegrationTestRunner
{
    private const int AbsoluteMaximumAssemblies = 64;
    private const int AbsoluteMaximumTests = 128;
    private const int AbsoluteMaximumFailures = 64;
    private const int AbsoluteMaximumTypesPerAssembly = 256;
    private const int AbsoluteMaximumMethodsPerType = 128;
    private const int AbsoluteMaximumDiscoveryWork = 8 * 1024;
    private const int MaximumMessageCharacters = 256;
    private const int MaximumStackCharacters = 1024;
    private const int MaximumIdentityCharacters = 256;
    private const int MaximumPackageIdCharacters = 128;
    private readonly bool enabled;
    private readonly IGatewayIntegrationTestAssemblyCatalog catalog;
    private readonly IGatewayIntegrationTestArtifactStore artifactStore;
    private readonly IGatewayIntegrationTestReadiness readiness;
    private readonly int maximumAssemblies;
    private readonly int maximumTests;
    private readonly int maximumFailures;
    private readonly int maximumTypesPerAssembly;
    private readonly int maximumMethodsPerType;
    private readonly int maximumDiscoveryWork;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly IGatewayIntegrationTestBackgroundScheduler backgroundScheduler;
    private readonly Action<string, Exception?>? diagnostics;
    private readonly List<GatewayIntegrationTestFailure> failures = new();
    private readonly List<GatewayIntegrationTestResult> results = new();
    private readonly List<DiscoveredTest> discoveredTests = new();
    private readonly List<GatewayIntegrationTestAssemblySnapshot> discoveredAssemblies = new();
    private readonly HashSet<RunAt> completedStages = new();
    private readonly Dictionary<RunAt, LifecycleState> lifecycleStates = new();
    private readonly HashSet<string> reportedPersistenceFaults = new(StringComparer.Ordinal);
    private IGatewayIntegrationTestAssemblyDiscoveryCursor? discoveryCursor;
    private GatewayIntegrationTestAssemblySource? pendingAssemblySource;
    private int discoveryWorkCount;
    private int attemptedAssemblyCount;
    private bool discoveryAttempted;
    private string discoveryState;
    private int discoveredAssemblyCount;
    private int omittedFailureCount;
    private int omittedResultCount;
    private string? sessionCredential;
    private volatile GatewayIntegrationTestSnapshot snapshot;
    private volatile GatewayIntegrationTestSnapshot? publishedSnapshot;
    private Task<SourceDiscoveryResult>? pendingDiscoveryWork;
    private GatewayIntegrationTestSnapshot? pendingPersistenceCandidate;
    private IGatewayIntegrationTestPersistenceOperation? pendingPersistence;
    private DiscoveredTest? pendingInvocation;
    private RunAt pendingInvocationLifecycle;

    public GatewayIntegrationTestRunner(
        bool enabled,
        IGatewayIntegrationTestAssemblyCatalog catalog,
        IGatewayIntegrationTestArtifactStore artifactStore,
        IGatewayIntegrationTestReadiness readiness,
        int maximumAssemblies = AbsoluteMaximumAssemblies,
        int maximumTests = AbsoluteMaximumTests,
        int maximumFailures = AbsoluteMaximumFailures,
        int maximumTypesPerAssembly = AbsoluteMaximumTypesPerAssembly,
        int maximumMethodsPerType = AbsoluteMaximumMethodsPerType,
        int maximumDiscoveryWork = AbsoluteMaximumDiscoveryWork,
        Func<DateTimeOffset>? utcNow = null,
        IGatewayIntegrationTestBackgroundScheduler? backgroundScheduler = null,
        Action<string, Exception?>? diagnostics = null)
    {
        if (maximumAssemblies <= 0 || maximumAssemblies > AbsoluteMaximumAssemblies)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAssemblies));
        }

        if (maximumTests <= 0 || maximumTests > AbsoluteMaximumTests)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumTests));
        }

        if (maximumFailures <= 0 || maximumFailures > AbsoluteMaximumFailures)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFailures));
        }

        if (maximumTypesPerAssembly <= 0 || maximumTypesPerAssembly > AbsoluteMaximumTypesPerAssembly)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumTypesPerAssembly));
        }

        if (maximumMethodsPerType <= 0 || maximumMethodsPerType > AbsoluteMaximumMethodsPerType)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMethodsPerType));
        }

        if (maximumDiscoveryWork <= 0 || maximumDiscoveryWork > AbsoluteMaximumDiscoveryWork)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDiscoveryWork));
        }

        this.enabled = enabled;
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        this.readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        this.maximumAssemblies = maximumAssemblies;
        this.maximumTests = maximumTests;
        this.maximumFailures = maximumFailures;
        this.maximumTypesPerAssembly = maximumTypesPerAssembly;
        this.maximumMethodsPerType = maximumMethodsPerType;
        this.maximumDiscoveryWork = maximumDiscoveryWork;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.backgroundScheduler = backgroundScheduler ?? new GatewayIntegrationTestBackgroundScheduler();
        this.diagnostics = diagnostics;
        discoveryState = enabled ? "not-started" : "disabled";
        foreach (RunAt runAt in Enum.GetValues(typeof(RunAt)))
        {
            lifecycleStates.Add(runAt, new LifecycleState(runAt, enabled ? "pending" : "disabled"));
        }

        snapshot = CreateSnapshot();
    }

    public GatewayIntegrationTestSnapshot Snapshot => snapshot;

    public GatewayIntegrationTestSnapshot? PublishedSnapshot => publishedSnapshot;

    internal bool IsPersistenceIdle =>
        pendingPersistenceCandidate is null && pendingPersistence is null;

    public void AttachSessionCredential(string? credential)
    {
        sessionCredential = string.IsNullOrEmpty(credential) ? null : credential;
        RefreshSnapshot();
    }

    internal void ConfirmInitialPublishedSnapshot(GatewayIntegrationTestSnapshot attachedSnapshot)
    {
        if (!ReferenceEquals(attachedSnapshot, snapshot))
        {
            throw new InvalidOperationException(
                "The attached integration-test snapshot is not the runner's exact initial snapshot.");
        }

        publishedSnapshot = attachedSnapshot;
    }

    private SourceDiscoveryResult DiscoverSource(
        GatewayIntegrationTestAssemblySource source,
        int workBudget)
    {
        var result = new SourceDiscoveryResult(source);
        Assembly assembly;
        result.WorkCount++;
        try
        {
            assembly = catalog.Load(source) ??
                throw new InvalidOperationException("The integration-test assembly catalog returned a null assembly.");
            result.AssemblyIdentity = assembly.FullName ?? source.Identity;
        }
        catch (Exception exception)
        {
            result.Failures.Add(new WorkerFailure(
                "assembly_load_failed",
                $"Could not load integration-test assembly '{source.Identity}'.",
                null,
                exception));
            return result;
        }

        if (result.WorkCount >= workBudget)
        {
            result.WorkLimitReached = true;
            return result;
        }

        Type[] types;
        result.WorkCount++;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
            foreach (var loaderException in exception.LoaderExceptions
                         .Where(item => item is not null)
                         .Take(maximumTypesPerAssembly))
            {
                result.Failures.Add(new WorkerFailure(
                    "type_load_failed",
                    $"One type from integration-test assembly '{source.Identity}' could not load.",
                    null,
                    loaderException));
            }
        }
        catch (Exception exception)
        {
            result.Failures.Add(new WorkerFailure(
                "type_discovery_failed",
                $"Types from integration-test assembly '{source.Identity}' could not be discovered.",
                null,
                exception));
            return result;
        }

        var orderedTypes = types
            .Where(type => type is not null)
            .Select(type => new WorkerType(type, ReflectionTypeName(type)))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Take(maximumTypesPerAssembly + 1)
            .ToArray();
        if (orderedTypes.Length > maximumTypesPerAssembly)
        {
            result.Failures.Add(new WorkerFailure(
                "type_limit_reached",
                $"Assembly '{source.Identity}' exceeded the {maximumTypesPerAssembly} type discovery limit."));
            orderedTypes = orderedTypes.Take(maximumTypesPerAssembly).ToArray();
        }

        foreach (var type in orderedTypes)
        {
            if (result.WorkCount >= workBudget)
            {
                result.WorkLimitReached = true;
                break;
            }

            result.WorkCount++;
            MethodInfo[] methods;
            try
            {
                methods = type.Type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
            }
            catch (Exception exception)
            {
                result.Failures.Add(new WorkerFailure(
                    "method_discovery_failed",
                    $"Methods on integration-test type '{type.Name}' could not be discovered.",
                    null,
                    exception));
                continue;
            }

            var orderedMethods = methods
                .Select(method => new WorkerMethod(
                    method,
                    method.Name,
                    method.MetadataToken))
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ThenBy(method => method.MetadataToken)
                .Take(maximumMethodsPerType + 1)
                .ToArray();
            if (orderedMethods.Length > maximumMethodsPerType)
            {
                result.Failures.Add(new WorkerFailure(
                    "method_limit_reached",
                    $"Type '{type.Name}' exceeded the {maximumMethodsPerType} method discovery limit."));
                orderedMethods = orderedMethods.Take(maximumMethodsPerType).ToArray();
            }

            foreach (var method in orderedMethods)
            {
                if (result.WorkCount >= workBudget)
                {
                    result.WorkLimitReached = true;
                    break;
                }

                result.WorkCount++;
                InspectMethod(result, type, method);
            }

            if (result.WorkLimitReached)
            {
                break;
            }
        }

        return result;
    }

    private static void InspectMethod(
        SourceDiscoveryResult result,
        WorkerType type,
        WorkerMethod method)
    {
        var testName = type.Name + "." + method.Name;
        try
        {
            var attributes = CustomAttributeData.GetCustomAttributes(method.Method);
            var attribute = attributes
                .Where(candidate => candidate.AttributeType == typeof(IntegrationTestAttribute))
                .SingleOrDefault();
            if (attribute is null)
            {
                return;
            }

            if (attribute.ConstructorArguments.Count != 1)
            {
                throw new InvalidOperationException(
                    "The integration-test marker has an unexpected constructor shape.");
            }

            var runAt = (RunAt)Convert.ToInt32(attribute.ConstructorArguments[0].Value);
            if (!Enum.IsDefined(typeof(RunAt), runAt))
            {
                result.Failures.Add(new WorkerFailure(
                    "invalid_test_lifecycle",
                    $"Integration test '{testName}' declares an unknown lifecycle point.",
                    testName));
            }
            else if (attributes.Any(candidate =>
                         candidate.AttributeType == typeof(AsyncStateMachineAttribute)) &&
                     method.Method.ReturnType == typeof(void))
            {
                result.Failures.Add(new WorkerFailure(
                    "invalid_async_void_test",
                    $"Integration test '{testName}' is async void and cannot be observed to completion.",
                    testName));
            }
            else if (!method.Method.IsPublic ||
                     !method.Method.IsStatic ||
                     method.Method.ReturnType != typeof(void) ||
                     method.Method.GetParameters().Length != 0 ||
                     method.Method.ContainsGenericParameters)
            {
                result.Failures.Add(new WorkerFailure(
                    "invalid_test_signature",
                    $"Integration test '{testName}' must be public static, parameterless, non-generic, and return void.",
                    testName));
            }
            else
            {
                result.Tests.Add(new WorkerTest(testName, runAt, method.Method));
            }
        }
        catch (Exception exception)
        {
            result.Failures.Add(new WorkerFailure(
                "test_attribute_failed",
                $"The integration-test attribute on '{testName}' could not be read.",
                testName,
                exception));
        }
    }

    private void ApplySourceDiscovery(SourceDiscoveryResult result)
    {
        discoveryWorkCount += result.WorkCount;
        if (result.AssemblyIdentity is not null)
        {
            discoveredAssemblyCount++;
            discoveredAssemblies.Add(new GatewayIntegrationTestAssemblySnapshot(
                Bound(result.Source.OwningPackageId, MaximumPackageIdCharacters),
                Bound(result.Source.Identity, MaximumIdentityCharacters),
                Bound(result.AssemblyIdentity, MaximumIdentityCharacters)));
        }

        foreach (var failure in result.Failures)
        {
            AddFailure(
                failure.Code,
                failure.Message,
                result.Source,
                failure.TestName,
                failure.Exception);
        }

        foreach (var test in result.Tests)
        {
            if (discoveredTests.Count == maximumTests)
            {
                omittedResultCount++;
                continue;
            }

            discoveredTests.Add(new DiscoveredTest(
                Bound(result.Source.OwningPackageId, MaximumPackageIdCharacters),
                Bound(result.AssemblyIdentity ?? result.Source.Identity, MaximumIdentityCharacters),
                Bound(test.TestName, MaximumIdentityCharacters),
                test.RunAt,
                test.Method));
        }

        if (result.WorkLimitReached || discoveryWorkCount >= maximumDiscoveryWork)
        {
            AddFailure(
                "discovery_work_limit_reached",
                $"Integration-test discovery stopped after {maximumDiscoveryWork} bounded work steps.");
            FinishDiscovery();
            return;
        }

        RefreshSnapshot();
    }

    private static string ReflectionTypeName(Type type) => type.FullName ?? type.Name;

    public void Observe(RunAt lifecyclePoint)
    {
        TryObserve(lifecyclePoint);
    }

    public bool TryObserve(RunAt lifecyclePoint)
    {
        if (!enabled)
        {
            return false;
        }

        if (!Enum.IsDefined(typeof(RunAt), lifecyclePoint))
        {
            throw new ArgumentOutOfRangeException(nameof(lifecyclePoint));
        }

        if (!readiness.IsMainThread)
        {
            throw new InvalidOperationException("In-game integration tests must be observed on the game main thread.");
        }

        if (AdvancePersistence())
        {
            return true;
        }

        if (!artifactStore.IsAttached)
        {
            return false;
        }

        if (!readiness.IsReady(lifecyclePoint))
        {
            return false;
        }

        if (pendingInvocation is not null)
        {
            if (pendingInvocationLifecycle != lifecyclePoint)
            {
                return false;
            }

            InvokePendingTest();
            return true;
        }

        if (!discoveryAttempted)
        {
            BeginDiscovery();
            return true;
        }

        if (discoveryState == "running")
        {
            AdvanceDiscovery();
            return true;
        }

        if (discoveryState == "infrastructure-failed")
        {
            return false;
        }

        if (completedStages.Contains(lifecyclePoint) || discoveredTests.Count == 0)
        {
            return false;
        }

        ExecuteNext(lifecyclePoint);
        return true;
    }

    private void BeginDiscovery()
    {
        discoveryAttempted = true;
        discoveryState = "running";
        try
        {
            discoveryCursor = catalog.BeginDiscovery() ??
                throw new InvalidOperationException("The integration-test assembly catalog returned a null discovery cursor.");
        }
        catch (Exception exception)
        {
            AddFailure("assembly_catalog_failed", "Active-mod integration-test discovery failed.", exception: exception);
            FinishDiscovery();
            return;
        }

        discoveryWorkCount++;
        RefreshSnapshot();
    }

    private void AdvanceDiscovery()
    {
        if (pendingDiscoveryWork is not null)
        {
            if (!pendingDiscoveryWork.IsCompleted)
            {
                return;
            }

            SourceDiscoveryResult result;
            try
            {
                result = pendingDiscoveryWork.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                result = SourceDiscoveryResult.Catastrophic(
                    pendingAssemblySource!,
                    exception);
            }

            pendingDiscoveryWork = null;
            pendingAssemblySource = null;
            ApplySourceDiscovery(result);
            return;
        }

        if (discoveryWorkCount >= maximumDiscoveryWork)
        {
            AddFailure(
                "discovery_work_limit_reached",
                $"Integration-test discovery stopped after {maximumDiscoveryWork} bounded work steps.");
            FinishDiscovery();
            return;
        }

        discoveryWorkCount++;
        GatewayIntegrationTestAssemblyDiscoveryStep step;
        try
        {
            step = discoveryCursor?.Advance() ??
                throw new InvalidOperationException("The integration-test assembly discovery cursor returned null.");
        }
        catch (Exception exception)
        {
            AddFailure("assembly_catalog_failed", "Active-mod integration-test discovery failed.", exception: exception);
            FinishDiscovery();
            return;
        }

        if (step.IsComplete)
        {
            FinishDiscovery();
            return;
        }

        if (step.Source is not null)
        {
            if (attemptedAssemblyCount >= maximumAssemblies)
            {
                AddFailure(
                    "assembly_limit_reached",
                    $"Only the first {maximumAssemblies} deterministic active-mod test assemblies were considered.");
                FinishDiscovery();
                return;
            }

            pendingAssemblySource = step.Source;
            attemptedAssemblyCount++;
            var remainingWork = Math.Max(1, maximumDiscoveryWork - discoveryWorkCount);
            try
            {
                pendingDiscoveryWork = backgroundScheduler.Run(
                    () => DiscoverSource(step.Source, remainingWork)) ??
                    throw new InvalidOperationException(
                        "The integration-test background scheduler returned a null task.");
            }
            catch (Exception exception)
            {
                var failed = SourceDiscoveryResult.Catastrophic(step.Source, exception);
                pendingAssemblySource = null;
                ApplySourceDiscovery(failed);
            }
        }

        RefreshSnapshot();
    }

    private void FinishDiscovery()
    {
        try
        {
            discoveryCursor?.Dispose();
        }
        catch (Exception exception)
        {
            AddFailure(
                "assembly_catalog_dispose_failed",
                "The integration-test discovery cursor could not be released.",
                exception: exception);
        }

        discoveryCursor = null;
        pendingAssemblySource = null;
        discoveredTests.Sort(DiscoveredTestComparer.Instance);
        if (discoveredTests.Count == 0)
        {
            AddFailure(
                "no_integration_tests_discovered",
                "Integration testing was enabled, but no valid staged tests were discovered for active mods.");
            discoveryState = "failed";
        }
        else
        {
            discoveryState = "completed";
        }

        RefreshSnapshot();
        QueueCurrentSnapshotForPersistence();
    }

    private void ExecuteNext(RunAt lifecyclePoint)
    {
        var lifecycleState = lifecycleStates[lifecyclePoint];
        if (lifecycleState.State == "pending")
        {
            lifecycleState.State = "running";
            lifecycleState.StartedUtc = utcNow().ToUniversalTime();
        }

        var stageTests = discoveredTests.Where(test => test.RunAt == lifecyclePoint).ToArray();
        if (lifecycleState.NextTestIndex >= stageTests.Length)
        {
            CompleteStage(lifecyclePoint, lifecycleState);
            return;
        }

        var test = stageTests[lifecycleState.NextTestIndex];
        var startedUtc = utcNow().ToUniversalTime();
        var running = new GatewayIntegrationTestResult(
            test.OwningPackageId,
            test.AssemblyIdentity,
            test.TestName,
            lifecyclePoint,
            "running",
            startedUtc);
        results.Add(running);
        RefreshSnapshot();
        pendingInvocation = test;
        pendingInvocationLifecycle = lifecyclePoint;
        QueueCurrentSnapshotForPersistence();
    }

    private void InvokePendingTest()
    {
        var test = pendingInvocation!;
        var lifecyclePoint = pendingInvocationLifecycle;
        var lifecycleState = lifecycleStates[lifecyclePoint];
        pendingInvocation = null;
        var running = results[results.Count - 1];
        var startedUtc = running.StartedUtc;

        var stopwatch = Stopwatch.StartNew();
        GatewayIntegrationTestResult terminal;
        try
        {
            test.Method.Invoke(null, null);
            stopwatch.Stop();
            terminal = new GatewayIntegrationTestResult(
                test.OwningPackageId,
                test.AssemblyIdentity,
                test.TestName,
                lifecyclePoint,
                "passed",
                startedUtc,
                utcNow().ToUniversalTime(),
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var failure = UnwrapInvocationException(exception);
            var details = GatewayIntegrationTestExceptionFormatter.Format(failure, sessionCredential);
            terminal = new GatewayIntegrationTestResult(
                test.OwningPackageId,
                test.AssemblyIdentity,
                test.TestName,
                lifecyclePoint,
                "failed",
                startedUtc,
                utcNow().ToUniversalTime(),
                stopwatch.ElapsedMilliseconds,
                Bound(details.Type, MaximumIdentityCharacters),
                Bound(details.Message, MaximumMessageCharacters),
                Bound(details.StackTrace, MaximumStackCharacters));
        }

        results[results.Count - 1] = terminal;
        lifecycleState.NextTestIndex++;
        lifecycleState.ExecutedCount++;
        if (terminal.State == "passed")
        {
            lifecycleState.PassedCount++;
        }
        else
        {
            lifecycleState.FailedCount++;
        }

        RefreshSnapshot();

        var stageTests = discoveredTests.Where(candidate => candidate.RunAt == lifecyclePoint).ToArray();
        if (lifecycleState.NextTestIndex == stageTests.Length)
        {
            CompleteStage(lifecyclePoint, lifecycleState);
            return;
        }

        QueueCurrentSnapshotForPersistence();
    }

    private void CompleteStage(RunAt lifecyclePoint, LifecycleState lifecycleState)
    {
        completedStages.Add(lifecyclePoint);
        lifecycleState.State = lifecycleState.FailedCount == 0 ? "completed" : "failed";
        lifecycleState.CompletedUtc = utcNow().ToUniversalTime();
        RefreshSnapshot();
        QueueCurrentSnapshotForPersistence();
    }

    private void AddFailure(
        string code,
        string message,
        GatewayIntegrationTestAssemblySource? source = null,
        string? testName = null,
        Exception? exception = null)
    {
        if (failures.Count == maximumFailures)
        {
            omittedFailureCount++;
            return;
        }

        var details = exception is null
            ? default(GatewayIntegrationTestExceptionDetails?)
            : GatewayIntegrationTestExceptionFormatter.Format(exception, sessionCredential);
        failures.Add(new GatewayIntegrationTestFailure(
            code,
            Bound(message, MaximumMessageCharacters),
            source is null ? null : Bound(source.OwningPackageId, MaximumPackageIdCharacters),
            source is null ? null : Bound(source.Identity, MaximumIdentityCharacters),
            testName is null ? null : Bound(testName, MaximumIdentityCharacters),
            details.HasValue ? Bound(details.Value.Type, MaximumIdentityCharacters) : null,
            details.HasValue ? Bound(details.Value.StackTrace, MaximumStackCharacters) : null));
    }

    private void RefreshSnapshot()
    {
        snapshot = CreateSnapshot();
    }

    private GatewayIntegrationTestSnapshot CreateSnapshot() =>
        new(
            enabled,
            discoveryState,
            discoveredAssemblyCount,
            discoveredTests.Count,
            discoveredAssemblies,
            discoveredTests.Select(test => test.Snapshot()),
            failures,
            results,
            lifecycleStates.Values
                .OrderBy(value => value.RunAt)
                .Select(value => value.Snapshot()),
            omittedFailureCount,
            omittedResultCount);

    private bool AdvancePersistence()
    {
        if (pendingPersistence is not null)
        {
            if (!pendingPersistence.IsCompleted)
            {
                return true;
            }

            GatewayIntegrationTestPersistenceOutcome outcome;
            try
            {
                outcome = pendingPersistence.GetOutcome();
            }
            catch (Exception exception)
            {
                ReportPersistenceFailure(exception);
                pendingPersistence = null;
                return true;
            }

            pendingPersistence = null;
            if (!outcome.Succeeded ||
                !ReferenceEquals(outcome.Snapshot, pendingPersistenceCandidate))
            {
                ReportPersistenceFailure(
                    outcome.Failure ?? new InvalidOperationException(
                        "The persistence lane did not commit the exact requested snapshot."));
                return true;
            }

            pendingPersistenceCandidate = null;
            publishedSnapshot = outcome.Snapshot;
            return true;
        }

        if (pendingPersistenceCandidate is null)
        {
            return false;
        }

        if (!artifactStore.IsAttached)
        {
            return true;
        }

        try
        {
            pendingPersistence = artifactStore.BeginPersist(pendingPersistenceCandidate) ??
                throw new InvalidOperationException(
                    "The integration-test artifact store returned a null persistence operation.");
        }
        catch (Exception exception)
        {
            ReportPersistenceFailure(exception);
        }

        return true;
    }

    private void QueueCurrentSnapshotForPersistence()
    {
        if (pendingPersistenceCandidate is not null || pendingPersistence is not null)
        {
            throw new InvalidOperationException(
                "A prior integration-test snapshot must commit before another transition.");
        }

        pendingPersistenceCandidate = snapshot;
    }

    private void ReportPersistenceFailure(Exception exception)
    {
        var details = GatewayIntegrationTestExceptionFormatter.Format(exception, sessionCredential);
        var key = details.Type + ":" + details.Message;
        if (reportedPersistenceFaults.Count >= 32 || !reportedPersistenceFaults.Add(key))
        {
            return;
        }

        try
        {
            diagnostics?.Invoke(
                "persist-integration-test-snapshot",
                new InvalidOperationException(details.Type + ": " + details.Message));
        }
        catch
        {
            // Diagnostics are advisory and must not destabilize the update loop.
        }
    }

    private static Exception UnwrapInvocationException(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null } target
            ? target.InnerException
            : exception;

    private string Bound(string? value, int maximumCharacters)
    {
        var safeValue = GatewayIntegrationTestExceptionFormatter.Redact(value, sessionCredential);
        return safeValue.Length <= maximumCharacters
            ? safeValue
            : safeValue.Substring(0, maximumCharacters - 3) + "...";
    }

    private sealed class SourceDiscoveryResult
    {
        public SourceDiscoveryResult(GatewayIntegrationTestAssemblySource source)
        {
            Source = source;
        }

        public GatewayIntegrationTestAssemblySource Source { get; }
        public string? AssemblyIdentity { get; set; }
        public int WorkCount { get; set; }
        public bool WorkLimitReached { get; set; }
        public List<WorkerFailure> Failures { get; } = new();
        public List<WorkerTest> Tests { get; } = new();

        public static SourceDiscoveryResult Catastrophic(
            GatewayIntegrationTestAssemblySource source,
            Exception exception)
        {
            var result = new SourceDiscoveryResult(source);
            result.Failures.Add(new WorkerFailure(
                "assembly_discovery_worker_failed",
                $"The background discovery worker for '{source.Identity}' failed.",
                null,
                exception));
            return result;
        }
    }

    private sealed class WorkerFailure
    {
        public WorkerFailure(
            string code,
            string message,
            string? testName = null,
            Exception? exception = null)
        {
            Code = code;
            Message = message;
            TestName = testName;
            Exception = exception;
        }

        public string Code { get; }
        public string Message { get; }
        public string? TestName { get; }
        public Exception? Exception { get; }
    }

    private sealed class WorkerType
    {
        public WorkerType(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public Type Type { get; }
        public string Name { get; }
    }

    private sealed class WorkerMethod
    {
        public WorkerMethod(MethodInfo method, string name, int metadataToken)
        {
            Method = method;
            Name = name;
            MetadataToken = metadataToken;
        }

        public MethodInfo Method { get; }
        public string Name { get; }
        public int MetadataToken { get; }
    }

    private sealed class WorkerTest
    {
        public WorkerTest(string testName, RunAt runAt, MethodInfo method)
        {
            TestName = testName;
            RunAt = runAt;
            Method = method;
        }

        public string TestName { get; }
        public RunAt RunAt { get; }
        public MethodInfo Method { get; }
    }

    private sealed class DiscoveredTest
    {
        public DiscoveredTest(
            string owningPackageId,
            string assemblyIdentity,
            string testName,
            RunAt runAt,
            MethodInfo method)
        {
            OwningPackageId = owningPackageId;
            AssemblyIdentity = assemblyIdentity;
            TestName = testName;
            RunAt = runAt;
            Method = method;
        }

        public string OwningPackageId { get; }
        public string AssemblyIdentity { get; }
        public string TestName { get; }
        public RunAt RunAt { get; }
        public MethodInfo Method { get; }

        public GatewayIntegrationTestDescriptor Snapshot() =>
            new(OwningPackageId, AssemblyIdentity, TestName, RunAt);
    }

    private sealed class DiscoveredTestComparer : IComparer<DiscoveredTest>
    {
        public static DiscoveredTestComparer Instance { get; } = new();

        public int Compare(DiscoveredTest? left, DiscoveredTest? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            var package = StringComparer.Ordinal.Compare(left.OwningPackageId, right.OwningPackageId);
            if (package != 0) return package;
            var assembly = StringComparer.Ordinal.Compare(left.AssemblyIdentity, right.AssemblyIdentity);
            return assembly != 0
                ? assembly
                : StringComparer.Ordinal.Compare(left.TestName, right.TestName);
        }
    }

    private sealed class LifecycleState
    {
        public LifecycleState(RunAt runAt, string state)
        {
            RunAt = runAt;
            State = state;
        }

        public RunAt RunAt { get; }
        public string State { get; set; }
        public DateTimeOffset? StartedUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public int ExecutedCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int NextTestIndex { get; set; }

        public GatewayIntegrationTestLifecycleSnapshot Snapshot() =>
            new(RunAt, State, StartedUtc, CompletedUtc, ExecutedCount, PassedCount, FailedCount);
    }
}
