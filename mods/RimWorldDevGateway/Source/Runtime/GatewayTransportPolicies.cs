using System.Collections.Generic;
using System.Threading;

namespace RimWorldDevGateway;

internal sealed class GatewayRequestGate
{
    private readonly int capacity;
    private int active;

    public GatewayRequestGate(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.capacity = capacity;
    }

    public int ActiveCount => Volatile.Read(ref active);

    public IDisposable? TryEnter()
    {
        while (true)
        {
            var observed = Volatile.Read(ref active);
            if (observed >= capacity)
            {
                return null;
            }

            if (Interlocked.CompareExchange(ref active, observed + 1, observed) == observed)
            {
                return new Lease(this);
            }
        }
    }

    private void Release()
    {
        if (Interlocked.Decrement(ref active) < 0)
        {
            Interlocked.Exchange(ref active, 0);
            throw new InvalidOperationException("The gateway request gate was released without a matching admission.");
        }
    }

    private sealed class Lease : IDisposable
    {
        private GatewayRequestGate? owner;

        public Lease(GatewayRequestGate owner)
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref owner, null)?.Release();
        }
    }
}

internal static class GatewayTransportResponsePolicy
{
    private const int MaximumErrorMessageCharacters = 2_048;

    public static GatewayHttpResponse Error(
        int status,
        string reason,
        string requestId,
        string code,
        string message,
        long durationMilliseconds,
        bool retryable = false)
    {
        var error = new SortedDictionary<string, object?>
        {
            ["code"] = code,
            ["message"] = Bound(message ?? string.Empty, MaximumErrorMessageCharacters),
            ["retryable"] = retryable
        };
        var envelope = new SortedDictionary<string, object?>
        {
            ["apiVersion"] = "1",
            ["durationMs"] = Math.Max(0, durationMilliseconds),
            ["error"] = error,
            ["ok"] = false,
            ["requestId"] = requestId
        };
        return new GatewayHttpResponse(
            status,
            reason,
            "application/json; charset=utf-8",
            GatewayJsonWriter.Write(envelope));
    }

    public static GatewayHttpResponse EnforceLimit(
        GatewayHttpResponse response,
        int maximumResponseBytes,
        string requestId,
        long durationMilliseconds)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (maximumResponseBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        }

        return response.Body.Length <= maximumResponseBytes
            ? response
            : Error(
                500,
                "Internal Server Error",
                requestId,
                "response_too_large",
                $"The response exceeded the {maximumResponseBytes}-byte transport limit.",
                durationMilliseconds);
    }

    private static string Bound(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters
            ? value
            : value.Substring(0, maximumCharacters) + "...";
}
