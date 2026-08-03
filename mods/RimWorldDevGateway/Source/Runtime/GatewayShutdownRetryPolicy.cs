namespace RimWorldDevGateway;

internal static class GatewayShutdownRetryPolicy
{
    internal static readonly TimeSpan RetryWindow = TimeSpan.FromSeconds(1);

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
