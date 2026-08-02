using System;
using System.Collections.Generic;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Client;

public sealed class GatewayClientException : Exception
{
    public GatewayClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class GatewayCliUsageException : Exception
{
    public GatewayCliUsageException(string message)
        : base(message)
    {
    }
}

public interface IGatewaySessionProvider
{
    GatewaySessionManifest Load(string? manifestPath, int? expectedProcessId);
}

public interface IGatewayProcessInspector
{
    DateTimeOffset? TryGetStartUtc(int processId);
}

public sealed class GatewayClientHttpRequest
{
    public GatewayClientHttpRequest(
        string method,
        Uri uri,
        IReadOnlyDictionary<string, string> headers,
        string? contentType,
        byte[] body)
    {
        Method = method;
        Uri = uri;
        Headers = headers;
        ContentType = contentType;
        Body = body;
    }

    public string Method { get; }

    public Uri Uri { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string? ContentType { get; }

    public byte[] Body { get; }
}

public sealed class GatewayClientHttpResponse
{
    public GatewayClientHttpResponse(int statusCode, string contentType, byte[] body)
    {
        StatusCode = statusCode;
        ContentType = contentType;
        Body = body;
    }

    public int StatusCode { get; }

    public string ContentType { get; }

    public byte[] Body { get; }
}

public interface IGatewayHttpTransport
{
    GatewayClientHttpResponse Send(GatewayClientHttpRequest request);
}

public sealed class GatewaySourceCompilationRequest
{
    public GatewaySourceCompilationRequest(
        string sourcePath,
        string managedAssembliesPath,
        string gatewayContractPath)
    {
        SourcePath = sourcePath;
        ManagedAssembliesPath = managedAssembliesPath;
        GatewayContractPath = gatewayContractPath;
    }

    public string SourcePath { get; }

    public string ManagedAssembliesPath { get; }

    public string GatewayContractPath { get; }
}

public interface IGatewaySourceCompiler
{
    byte[] Compile(GatewaySourceCompilationRequest request);
}

public interface IGatewayClientFileSystem
{
    string ReadAllText(string path);

    void WriteAllBytes(string path, byte[] contents);
}
