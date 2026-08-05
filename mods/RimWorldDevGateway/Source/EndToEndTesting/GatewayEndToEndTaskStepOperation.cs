using System.Threading.Tasks;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndTaskStepOperation : IGatewayEndToEndStepOperation
{
    private readonly Task<GatewayEndToEndStepOutcome> task;
    private readonly string failureCode;
    private readonly string failureMessage;

    public GatewayEndToEndTaskStepOperation(
        Task<GatewayEndToEndStepOutcome> task,
        string failureCode,
        string failureMessage)
    {
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        this.failureCode = string.IsNullOrWhiteSpace(failureCode)
            ? throw new ArgumentException("An asynchronous failure code is required.", nameof(failureCode))
            : failureCode;
        this.failureMessage = failureMessage ?? throw new ArgumentNullException(nameof(failureMessage));
    }

    public bool IsCompleted => task.IsCompleted;

    public GatewayEndToEndStepOutcome GetOutcome()
    {
        if (!task.IsCompleted)
        {
            throw new InvalidOperationException("The asynchronous E2E operation has not completed.");
        }

        if (task.IsCanceled || task.IsFaulted)
        {
            return GatewayEndToEndStepOutcome.Fail(failureCode, failureMessage);
        }

        return task.GetAwaiter().GetResult() ??
               GatewayEndToEndStepOutcome.Fail(failureCode, failureMessage);
    }
}
