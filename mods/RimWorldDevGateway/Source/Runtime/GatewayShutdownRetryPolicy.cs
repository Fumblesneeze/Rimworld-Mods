namespace RimWorldDevGateway;

internal static class GatewayShutdownRetryPolicy
{
    internal static readonly TimeSpan RetryWindow = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan PersistentRetryInterval = TimeSpan.FromSeconds(1);

    internal static bool ShouldRetry(
        Exception exception,
        DateTimeOffset nowUtc,
        DateTimeOffset deadlineUtc)
    {
        if (nowUtc >= deadlineUtc)
        {
            return false;
        }

        return ContainsTransientSessionLock(exception);
    }

    private static bool ContainsTransientSessionLock(Exception exception)
    {
        if (exception is GatewayTransientSessionCleanupException)
        {
            return true;
        }

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                if (ContainsTransientSessionLock(inner))
                {
                    return true;
                }
            }

            return false;
        }

        return exception.InnerException is not null &&
               ContainsTransientSessionLock(exception.InnerException);
    }
}

internal sealed class GatewayShutdownLifecycle
{
    private DateTimeOffset? fastRetryDeadlineUtc;
    private DateTimeOffset? nextAttemptUtc;
    private bool retainedFailureReported;
    private bool terminal;

    internal GatewayShutdownAttempt TryShutdown(
        Action cleanup,
        bool allowRetry,
        DateTimeOffset nowUtc)
    {
        if (cleanup is null)
        {
            throw new ArgumentNullException(nameof(cleanup));
        }

        if (terminal)
        {
            return GatewayShutdownAttempt.Terminal(attempted: false);
        }

        if (allowRetry && nextAttemptUtc.HasValue && nowUtc < nextAttemptUtc.Value)
        {
            return GatewayShutdownAttempt.Retained(attempted: false, reportableFailure: null);
        }

        try
        {
            cleanup();
            terminal = true;
            return GatewayShutdownAttempt.Terminal(attempted: true);
        }
        catch (Exception exception)
        {
            if (!allowRetry)
            {
                terminal = true;
                return GatewayShutdownAttempt.Terminal(attempted: true, exception);
            }

            fastRetryDeadlineUtc ??= nowUtc.Add(GatewayShutdownRetryPolicy.RetryWindow);
            var fastRetry = GatewayShutdownRetryPolicy.ShouldRetry(
                exception,
                nowUtc,
                fastRetryDeadlineUtc.Value);
            nextAttemptUtc = fastRetry
                ? nowUtc
                : nowUtc.Add(GatewayShutdownRetryPolicy.PersistentRetryInterval);

            Exception? reportableFailure = null;
            if (!fastRetry && !retainedFailureReported)
            {
                retainedFailureReported = true;
                reportableFailure = exception;
            }

            return GatewayShutdownAttempt.Retained(attempted: true, reportableFailure);
        }
    }
}

internal sealed class GatewayShutdownAttempt
{
    private GatewayShutdownAttempt(
        bool attempted,
        bool isTerminal,
        bool retainsOwnership,
        Exception? reportableFailure)
    {
        Attempted = attempted;
        IsTerminal = isTerminal;
        RetainsOwnership = retainsOwnership;
        ReportableFailure = reportableFailure;
    }

    internal bool Attempted { get; }

    internal bool IsTerminal { get; }

    internal bool RetainsOwnership { get; }

    internal Exception? ReportableFailure { get; }

    internal static GatewayShutdownAttempt Terminal(
        bool attempted,
        Exception? reportableFailure = null) =>
        new(attempted, isTerminal: true, retainsOwnership: false, reportableFailure);

    internal static GatewayShutdownAttempt Retained(
        bool attempted,
        Exception? reportableFailure) =>
        new(attempted, isTerminal: false, retainsOwnership: true, reportableFailure);
}
