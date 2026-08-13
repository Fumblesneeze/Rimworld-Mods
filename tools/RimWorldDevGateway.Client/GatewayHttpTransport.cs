using System;
using System.IO;
using System.Linq;
using System.Net;

namespace RimWorldDevGateway.Client;

public sealed class GatewayHttpTransport : IGatewayHttpTransport
{
    private readonly int timeoutMilliseconds;
    private readonly int maxResponseBytes;

    public GatewayHttpTransport(int timeoutMilliseconds = 20_000, int maxResponseBytes = 32 * 1024 * 1024)
    {
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        }

        if (maxResponseBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResponseBytes));
        }

        this.timeoutMilliseconds = timeoutMilliseconds;
        this.maxResponseBytes = maxResponseBytes;
    }

    public GatewayClientHttpResponse Send(GatewayClientHttpRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var webRequest = (HttpWebRequest)WebRequest.Create(request.Uri);
        webRequest.Method = request.Method;
        webRequest.Proxy = null;
        webRequest.AllowAutoRedirect = false;
        webRequest.KeepAlive = false;
        webRequest.Timeout = timeoutMilliseconds;
        webRequest.ReadWriteTimeout = timeoutMilliseconds;
        webRequest.ServicePoint.Expect100Continue = false;
        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            webRequest.ContentType = request.ContentType;
        }

        foreach (var header in request.Headers)
        {
            webRequest.Headers[header.Key] = header.Value;
        }

        try
        {
            if (request.Body.Length > 0)
            {
                webRequest.ContentLength = request.Body.Length;
                using var requestStream = webRequest.GetRequestStream();
                requestStream.Write(request.Body, 0, request.Body.Length);
            }
            else
            {
                webRequest.ContentLength = 0;
            }

            using var response = (HttpWebResponse)webRequest.GetResponse();
            return Read(response);
        }
        catch (WebException exception) when (exception.Response is HttpWebResponse response)
        {
            using (response)
            {
                return Read(response);
            }
        }
        catch (WebException exception)
        {
            throw new GatewayClientException(
                $"Gateway HTTP request failed: {Bound(exception.Message, 1024)}",
                exception);
        }
    }

    private GatewayClientHttpResponse Read(HttpWebResponse response)
    {
        if (response.ContentLength > maxResponseBytes)
        {
            throw new GatewayClientException(
                $"Gateway HTTP response exceeded the {maxResponseBytes}-byte limit.");
        }

        using var stream = response.GetResponseStream();
        using var body = new MemoryStream();
        if (stream != null)
        {
            var buffer = new byte[8192];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (body.Length + count > maxResponseBytes)
                {
                    throw new GatewayClientException(
                        $"Gateway HTTP response exceeded the {maxResponseBytes}-byte limit.");
                }

                body.Write(buffer, 0, count);
            }
        }

        return new GatewayClientHttpResponse(
            (int)response.StatusCode,
            response.ContentType ?? string.Empty,
            body.ToArray(),
            response.Headers.AllKeys
                .Where(name => name != null)
                .ToDictionary(
                    name => name!,
                    name => response.Headers[name!] ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase));
    }

    private static string Bound(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters ? value : value.Substring(0, maximumCharacters) + "...";
}
