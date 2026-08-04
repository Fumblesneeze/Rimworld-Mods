using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace RimWorldDevGateway;

public sealed class GatewayHttpRequest
{
    public GatewayHttpRequest(
        string method,
        string target,
        string path,
        string query,
        IReadOnlyDictionary<string, string> headers,
        byte[] body,
        CancellationToken cancellationToken = default)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Query = query ?? throw new ArgumentNullException(nameof(query));
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        var headerCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            headerCopy.Add(header.Key, header.Value);
        }

        Headers = new ReadOnlyDictionary<string, string>(headerCopy);
        Body = body ?? throw new ArgumentNullException(nameof(body));
        CancellationToken = cancellationToken;
    }

    public string Method { get; }

    public string Target { get; }

    public string Path { get; }

    public string Query { get; }

    public string HttpVersion => "HTTP/1.1";

    public IReadOnlyDictionary<string, string> Headers { get; }

    public byte[] Body { get; }

    public CancellationToken CancellationToken { get; }
}

public sealed class GatewayHttpResponse
{
    private Action? transportCompleted;

    public GatewayHttpResponse(int statusCode, string reasonPhrase, string contentType, byte[] body)
        : this(statusCode, reasonPhrase, contentType, body, transportCompleted: null)
    {
    }

    private GatewayHttpResponse(
        int statusCode,
        string reasonPhrase,
        string contentType,
        byte[] body,
        Action? transportCompleted)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase ?? throw new ArgumentNullException(nameof(reasonPhrase));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        this.transportCompleted = transportCompleted;
    }

    public int StatusCode { get; }

    public string ReasonPhrase { get; }

    public string ContentType { get; }

    public byte[] Body { get; }

    internal GatewayHttpResponse WithTransportCompletion(Action completion)
    {
        if (completion is null)
        {
            throw new ArgumentNullException(nameof(completion));
        }

        return new GatewayHttpResponse(StatusCode, ReasonPhrase, ContentType, Body, completion);
    }

    internal GatewayHttpResponse TransferTransportCompletionTo(GatewayHttpResponse replacement)
    {
        if (replacement is null)
        {
            throw new ArgumentNullException(nameof(replacement));
        }

        replacement.transportCompleted = Interlocked.Exchange(ref transportCompleted, null);
        return replacement;
    }

    internal void NotifyTransportCompleted()
    {
        Interlocked.Exchange(ref transportCompleted, null)?.Invoke();
    }

    public static GatewayHttpResponse Empty(int statusCode, string reasonPhrase) =>
        new(statusCode, reasonPhrase, "application/json; charset=utf-8", Array.Empty<byte>());
}
