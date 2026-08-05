using System.Threading.Tasks;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndTaskInspectionOperationFactory : IGatewayEndToEndInspectionOperationFactory
{
    private readonly IGatewayEndToEndBundleInspector inspector;

    public GatewayEndToEndTaskInspectionOperationFactory(IGatewayEndToEndBundleInspector inspector)
    {
        this.inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
    }

    public IGatewayEndToEndInspectionOperation Begin(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        var packages = activePackageIds?.ToArray() ??
            throw new ArgumentNullException(nameof(activePackageIds));
        return new TaskOperation(Task.Run(() =>
        {
            try
            {
                return inspector.Inspect(candidate, packages);
            }
            catch
            {
                return GatewayEndToEndBundleInspectionResult.Failed(
                    "bundle_inspection_failed",
                    "The staged E2E bundle failed during guarded inspection; arbitrary exception text was suppressed.");
            }
        }));
    }

    private sealed class TaskOperation : IGatewayEndToEndInspectionOperation
    {
        private readonly Task<GatewayEndToEndBundleInspectionResult> task;

        public TaskOperation(Task<GatewayEndToEndBundleInspectionResult> task) =>
            this.task = task ?? throw new ArgumentNullException(nameof(task));

        public bool IsCompleted => task.IsCompleted;

        public GatewayEndToEndBundleInspectionResult GetOutcome()
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The E2E inspection operation has not completed.");
            }

            return task.GetAwaiter().GetResult();
        }
    }
}
