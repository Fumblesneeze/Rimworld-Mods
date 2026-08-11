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
        context.DeferCleanup(service.Cleanup);
        service.Prepare(descriptor, context);
        benchmark.Arrange(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        GetSteps(context).GetEnumerator();

    private IEnumerable<EndToEndStep> GetSteps(IEndToEndContext context)
    {
        var service = context.GetRequiredService<GatewayPerformanceRunService>();
        foreach (var step in service.BeginWarmUp(descriptor, context)) yield return step;
        foreach (var step in service.BeginSample(descriptor, context)) yield return step;
        using var workload = benchmark.Execute(context) ??
                             throw new InvalidOperationException("The performance fixture returned a null step iterator.");
        while (workload.MoveNext())
            yield return workload.Current ??
                         throw new InvalidOperationException("The performance fixture yielded a null step.");
        foreach (var step in service.CompleteSample(descriptor, context)) yield return step;
    }
}

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
