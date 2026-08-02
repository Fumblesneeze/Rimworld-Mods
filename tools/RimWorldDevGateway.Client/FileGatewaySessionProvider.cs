using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Client;

public sealed class FileGatewaySessionProvider : IGatewaySessionProvider
{
    private const string ManifestEnvironmentVariable = "RIMWORLD_DEV_GATEWAY_MANIFEST";

    private readonly IGatewayProcessInspector processes;
    private readonly string defaultManifestPath;

    public FileGatewaySessionProvider(IGatewayProcessInspector processes, string? defaultManifestPath = null)
    {
        this.processes = processes ?? throw new ArgumentNullException(nameof(processes));
        this.defaultManifestPath = string.IsNullOrWhiteSpace(defaultManifestPath)
            ? ResolveDefaultManifestPath()
            : defaultManifestPath!;
    }

    public GatewaySessionManifest Load(string? manifestPath, int? expectedProcessId)
    {
        var path = string.IsNullOrWhiteSpace(manifestPath) ? defaultManifestPath : manifestPath!;
        GatewaySessionManifest manifest;
        try
        {
            manifest = GatewayContractJson.ReadFile<GatewaySessionManifest>(path);
        }
        catch (Exception exception)
        {
            throw new GatewayClientException($"Could not read gateway manifest '{path}': {exception.Message}", exception);
        }

        if (!string.Equals(manifest.ApiVersion, "1", StringComparison.Ordinal))
        {
            throw new GatewayClientException($"Gateway manifest '{path}' has unsupported API version '{manifest.ApiVersion}'.");
        }

        if (!string.Equals(manifest.State, "active", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(manifest.Token) ||
            manifest.ProcessId <= 0)
        {
            throw new GatewayClientException($"Gateway manifest '{path}' does not describe an active credentialed session.");
        }

        if (expectedProcessId.HasValue && manifest.ProcessId != expectedProcessId.Value)
        {
            throw new GatewayClientException(
                $"Gateway manifest PID {manifest.ProcessId} does not match expected PID {expectedProcessId.Value}.");
        }

        if (!DateTimeOffset.TryParseExact(
                manifest.ProcessStartUtc,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expectedStart))
        {
            throw new GatewayClientException($"Gateway manifest '{path}' has an invalid process start identity.");
        }

        var actualStart = processes.TryGetStartUtc(manifest.ProcessId);
        if (!actualStart.HasValue || actualStart.Value.ToUniversalTime() != expectedStart.ToUniversalTime())
        {
            throw new GatewayClientException(
                $"Gateway manifest PID {manifest.ProcessId} is not live with the advertised process start identity.");
        }

        ValidateBaseUrl(manifest.BaseUrl);
        return manifest;
    }

    private static string ResolveDefaultManifestPath()
    {
        var configured = Environment.GetEnvironmentVariable(ManifestEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            profile,
            "AppData",
            "LocalLow",
            "Ludeon Studios",
            "RimWorld by Ludeon Studios",
            "DevGateway",
            "current.json");
    }

    private static void ValidateBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttp ||
            !IPAddress.TryParse(uri.Host, out var address) ||
            !IPAddress.Loopback.Equals(address) ||
            !string.Equals(uri.AbsolutePath.TrimEnd('/'), "/api/v1", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new GatewayClientException("Gateway manifest baseUrl must be an IPv4 loopback HTTP /api/v1 URL.");
        }
    }
}

public sealed class GatewayProcessInspector : IGatewayProcessInspector
{
    public DateTimeOffset? TryGetStartUtc(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return null;
            }

            return new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
