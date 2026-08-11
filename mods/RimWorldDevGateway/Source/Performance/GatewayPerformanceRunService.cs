using System.Diagnostics;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal abstract class GatewayPerformanceRunService
{
    public abstract void Prepare(PerformanceTestDescriptor descriptor, IEndToEndContext context);
    public abstract IEnumerable<EndToEndStep> BeginWarmUp(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context);
    public abstract IEnumerable<EndToEndStep> BeginSample(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context);
    public abstract IEnumerable<EndToEndStep> CompleteSample(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context);
    public abstract void Cleanup();
}

internal interface IGatewayPerformanceRuntimeBackend
{
    void Prepare(PerformanceTestDescriptor descriptor);
    void Start(PerformanceTestDescriptor descriptor);
    void ArmTickBoundary(int gameTick);
    void ConfirmTickBoundary(int gameTick);
    void DisarmTickBoundary();
    IReadOnlyDictionary<string, string> Complete(
        PerformanceTestDescriptor descriptor,
        GatewayPerformanceControlWindow controlWindow,
        IReadOnlyDictionary<string, long> throughputCounts);
    void Cleanup();
}

internal sealed class GatewayPerformanceControlWindow
{
    public GatewayPerformanceControlWindow(
        int startGameTick,
        int endGameTick,
        long startTimestamp,
        long endTimestamp,
        long managedMemoryStartBytes,
        long managedMemoryEndBytes,
        IReadOnlyList<int> garbageCollectionsStart,
        IReadOnlyList<int> garbageCollectionsEnd)
    {
        StartGameTick = startGameTick;
        EndGameTick = endGameTick;
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
        ManagedMemoryStartBytes = managedMemoryStartBytes;
        ManagedMemoryEndBytes = managedMemoryEndBytes;
        GarbageCollectionsStart = garbageCollectionsStart;
        GarbageCollectionsEnd = garbageCollectionsEnd;
    }

    public int StartGameTick { get; }
    public int EndGameTick { get; }
    public long StartTimestamp { get; }
    public long EndTimestamp { get; }
    public long ManagedMemoryStartBytes { get; }
    public long ManagedMemoryEndBytes { get; }
    public IReadOnlyList<int> GarbageCollectionsStart { get; }
    public IReadOnlyList<int> GarbageCollectionsEnd { get; }
}

internal interface IGatewayPerformanceThroughputCounter
{
    long Read(string id);
}

internal sealed class CoordinatedGatewayPerformanceRunService : GatewayPerformanceRunService
{
    private readonly IGatewayPerformanceRuntimeBackend backend;
    private readonly Func<long> timestamp;
    private readonly Func<long> managedMemory;
    private readonly Func<int, int> garbageCollections;
    private bool prepared;
    private bool started;
    private int sampleStartTick;
    private long sampleStartTimestamp;
    private long sampleStartMemory;
    private int[]? sampleStartCollections;
    private IReadOnlyDictionary<string, long>? sampleStartThroughput;
    private IReadOnlyDictionary<string, string> artifacts = new Dictionary<string, string>();

    public CoordinatedGatewayPerformanceRunService(
        IGatewayPerformanceRuntimeBackend backend,
        Func<long>? timestamp = null,
        Func<long>? managedMemory = null,
        Func<int, int>? garbageCollections = null)
    {
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        this.timestamp = timestamp ?? Stopwatch.GetTimestamp;
        this.managedMemory = managedMemory ?? (() => GC.GetTotalMemory(forceFullCollection: false));
        this.garbageCollections = garbageCollections ?? GC.CollectionCount;
    }

    public override void Prepare(PerformanceTestDescriptor descriptor, IEndToEndContext context)
    {
        if (prepared) throw new InvalidOperationException("The performance service is already prepared.");
        backend.Prepare(descriptor ?? throw new ArgumentNullException(nameof(descriptor)));
        prepared = true;
    }

    public override IEnumerable<EndToEndStep> BeginWarmUp(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context)
    {
        RequirePrepared(descriptor, context);
        if (descriptor.WarmUpTicks == 0) yield break;
        var startTick = context.GameTick;
        var boundary = ExactBoundary(startTick, descriptor.WarmUpTicks);
        backend.ArmTickBoundary(boundary);
        yield return new TimeControlActionStep(
            "performance-warmup-unpause",
            paused: false,
            Speed(descriptor.GameSpeed));
        yield return new WaitUntilStep(
            "performance-warmup-window",
            current => ElapsedTicks(startTick, current.GameTick) >= descriptor.WarmUpTicks,
            Deadline(descriptor));
        yield return new TimeControlActionStep(
            "performance-warmup-pause",
            paused: true,
            Speed(descriptor.GameSpeed));
        try
        {
            backend.ConfirmTickBoundary(context.GameTick);
        }
        finally
        {
            backend.DisarmTickBoundary();
        }
    }

    public override IEnumerable<EndToEndStep> BeginSample(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context)
    {
        RequirePrepared(descriptor, context);
        if (started) throw new InvalidOperationException("The performance sample is already active.");
        yield return new ScreenshotStep(
            "performance sample before native game progression",
            Array.Empty<string>(),
            paddingPixels: 0);
        sampleStartThroughput = ReadThroughputSnapshot(descriptor, context);
        backend.Start(descriptor);
        started = true;
        sampleStartTick = context.GameTick;
        backend.ArmTickBoundary(ExactBoundary(sampleStartTick, descriptor.SampleTicks));
        sampleStartTimestamp = timestamp();
        sampleStartMemory = managedMemory();
        sampleStartCollections = Collections();
        yield return new TimeControlActionStep(
            "performance-sample-unpause",
            paused: false,
            Speed(descriptor.GameSpeed));
    }

    public override IEnumerable<EndToEndStep> CompleteSample(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context)
    {
        RequirePrepared(descriptor, context);
        if (!started || sampleStartCollections is null)
            throw new InvalidOperationException("The performance sample has not started.");
        yield return new WaitUntilStep(
            "performance-sample-window",
            current => ElapsedTicks(sampleStartTick, current.GameTick) >= descriptor.SampleTicks,
            Deadline(descriptor));
        yield return new TimeControlActionStep(
            "performance-sample-pause",
            paused: true,
            Speed(descriptor.GameSpeed));
        try
        {
            backend.ConfirmTickBoundary(context.GameTick);
        }
        finally
        {
            backend.DisarmTickBoundary();
        }

        var endTimestamp = timestamp();
        var counts = ReadThroughputDeltas(descriptor, context, sampleStartThroughput!);
        artifacts = backend.Complete(
            descriptor,
            new GatewayPerformanceControlWindow(
                sampleStartTick,
                context.GameTick,
                sampleStartTimestamp,
                endTimestamp,
                sampleStartMemory,
                managedMemory(),
                sampleStartCollections,
                Collections()),
            counts);
        started = false;
        yield return new CheckpointStep(
            "performance-artifacts",
            _ => artifacts);
        yield return new ScreenshotStep(
            "performance sample after exact native game progression",
            Array.Empty<string>(),
            paddingPixels: 0);
    }

    public override void Cleanup()
    {
        try
        {
            backend.Cleanup();
        }
        finally
        {
            prepared = false;
            started = false;
            sampleStartCollections = null;
            sampleStartThroughput = null;
            artifacts = new Dictionary<string, string>();
        }
    }

    private static EndToEndDeadline Deadline(PerformanceTestDescriptor descriptor)
    {
        var deadline = PerformanceBundleDeadline.Calculate(descriptor.WarmUpTicks, descriptor.SampleTicks);
        return new EndToEndDeadline(
            deadline.MaxFrames,
            deadline.MaxGameTicks,
            TimeSpan.FromSeconds(deadline.MaxWallClockSeconds));
    }

    private static EndToEndGameSpeed Speed(PerformanceGameSpeed speed) =>
        (EndToEndGameSpeed)(int)speed;

    private static long ElapsedTicks(int start, int current) => (long)current - start;

    private static int ExactBoundary(int start, int count)
    {
        var boundary = (long)start + count;
        if (start < 0 || count <= 0 || boundary > int.MaxValue)
            throw new InvalidOperationException(
                $"Performance tick boundary is outside the supported game-tick range: {start} + {count}.");
        return (int)boundary;
    }

    private void RequirePrepared(PerformanceTestDescriptor descriptor, IEndToEndContext context)
    {
        if (!prepared) throw new InvalidOperationException("The performance service is not prepared.");
        _ = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _ = context ?? throw new ArgumentNullException(nameof(context));
    }

    private int[] Collections() => new[]
    {
        garbageCollections(0),
        garbageCollections(1),
        garbageCollections(2)
    };

    private static IReadOnlyDictionary<string, long> ReadThroughputSnapshot(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context)
    {
        if (descriptor.ThroughputCheckpoints.Count == 0)
            return new Dictionary<string, long>(StringComparer.Ordinal);
        var counter = context.GetService(typeof(IGatewayPerformanceThroughputCounter)) as
                      IGatewayPerformanceThroughputCounter ??
                      throw new InvalidOperationException(
                          "The performance fixture declares throughput checkpoints but no counter is registered.");
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var checkpoint in descriptor.ThroughputCheckpoints)
        {
            var count = counter.Read(checkpoint.Id);
            if (count < 0)
                throw new InvalidOperationException(
                    $"Performance throughput '{checkpoint.Id}' returned a negative counter.");
            result.Add(checkpoint.Id, count);
        }
        return result;
    }

    private static IReadOnlyDictionary<string, long> ReadThroughputDeltas(
        PerformanceTestDescriptor descriptor,
        IEndToEndContext context,
        IReadOnlyDictionary<string, long> start)
    {
        var end = ReadThroughputSnapshot(descriptor, context);
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var checkpoint in descriptor.ThroughputCheckpoints)
        {
            var initial = start[checkpoint.Id];
            var current = end[checkpoint.Id];
            if (current < initial)
                throw new InvalidOperationException(
                    $"Performance throughput '{checkpoint.Id}' reset during the sample window.");
            var delta = current - initial;
            if (delta < checkpoint.MinimumCount)
                throw new InvalidOperationException(
                    $"Performance throughput '{checkpoint.Id}' observed {delta}, expected at least " +
                    checkpoint.MinimumCount + " during the sample window.");
            result.Add(checkpoint.Id, delta);
        }
        return result;
    }
}
