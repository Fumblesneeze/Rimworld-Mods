using System.Collections;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Performance;

internal sealed class GatewayPerformanceEndToEndAdapter : IRimWorldEndToEndTest
{
    private readonly PerformanceTestDescriptor descriptor;
    private readonly IRimWorldPerformanceTest benchmark;

    public GatewayPerformanceEndToEndAdapter(PerformanceTestDescriptor descriptor)
    {
        this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        benchmark = (IRimWorldPerformanceTest?)Activator.CreateInstance(descriptor.TestType) ??
                    throw new InvalidOperationException("The admitted performance fixture constructor returned null.");
    }

    public void Arrange(IEndToEndContext context)
    {
        var service = context.GetRequiredService<GatewayPerformanceRunService>();
        if (context is GatewayEndToEndTestContext gatewayContext &&
            service is CoordinatedGatewayPerformanceRunService coordinated)
            gatewayContext.DeferCleanup(coordinated.TryCleanup);
        else
            context.DeferCleanup(service.Cleanup);
        service.Prepare(descriptor, WithThroughputCounter(context));
        benchmark.Arrange(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        GetSteps(context).GetEnumerator();

    private IEnumerable<EndToEndStep> GetSteps(IEndToEndContext context)
    {
        var service = context.GetRequiredService<GatewayPerformanceRunService>();
        var performanceContext = WithThroughputCounter(context);
        foreach (var step in service.BeginWarmUp(descriptor, performanceContext)) yield return step;
        if (benchmark is IPerformanceSamplePreparation preparation)
        {
            using var preparationSteps = preparation.PrepareSample(context) ??
                                         throw new InvalidOperationException(
                                             "The performance fixture returned a null sample-preparation iterator.");
            while (preparationSteps.MoveNext())
                yield return preparationSteps.Current ??
                             throw new InvalidOperationException(
                                 "The performance fixture yielded a null sample-preparation step.");
        }
        foreach (var step in service.BeginSample(descriptor, performanceContext)) yield return step;
        using var workload = benchmark.Execute(context) ??
                             throw new InvalidOperationException("The performance fixture returned a null step iterator.");
        while (workload.MoveNext())
            yield return workload.Current ??
                         throw new InvalidOperationException("The performance fixture yielded a null step.");
        foreach (var step in service.CompleteSample(descriptor, performanceContext)) yield return step;
        if (benchmark is IPerformanceSampleValidation validation)
            validation.ValidateSample(context);
        if (benchmark is IPerformancePostSampleEvidence evidence)
        {
            using var evidenceSteps = evidence.CapturePostSampleEvidence(context) ??
                                      throw new InvalidOperationException(
                                          "The performance fixture returned a null post-sample evidence iterator.");
            while (evidenceSteps.MoveNext())
                yield return evidenceSteps.Current ??
                             throw new InvalidOperationException(
                                 "The performance fixture yielded a null post-sample evidence step.");
        }
    }

    private IEndToEndContext WithThroughputCounter(IEndToEndContext context) =>
        benchmark is IPerformanceThroughputCounter counter
            ? new ThroughputContext(context, counter)
            : context;

    private sealed class ThroughputContext : IEndToEndContext
    {
        private readonly IEndToEndContext inner;
        private readonly IPerformanceThroughputCounter counter;

        public ThroughputContext(IEndToEndContext inner, IPerformanceThroughputCounter counter)
        {
            this.inner = inner;
            this.counter = counter;
        }

        public long FrameCount => inner.FrameCount;
        public int GameTick => inner.GameTick;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IPerformanceThroughputCounter)
                ? counter
                : inner.GetService(serviceType);
        public void DeferCleanup(Action cleanupAction) => inner.DeferCleanup(cleanupAction);
    }
}
