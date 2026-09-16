using System.Security.Cryptography;

namespace RimWorldModding.Mcp;

public sealed record GatewayScreenshotResult(
    string RunId,
    int GameProcessId,
    DateTimeOffset GameProcessStartUtc,
    string Path,
    long Bytes,
    string Sha256);

public sealed class GatewayScreenshot(string repositoryRoot)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<GatewayScreenshotResult> CaptureAsync(string runId, CancellationToken cancellationToken)
    {
        var status = new RunLeaseManager(_repositoryRoot).Status(runId);
        if (status.State != "ready" || status.GameProcessId is null ||
            status.GameProcessStartUtc is null || status.GatewayManifestPath is null)
            throw new ArgumentException("The selected run has no exact ready Gateway process and manifest.");

        var path = RepositoryRoot.ContainedPath(_repositoryRoot, System.IO.Path.Combine(
            status.RunRoot, "screenshots", $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.png"));
        var client = new GatewayWorkshopClient(_repositoryRoot, status.GatewayManifestPath, status.GameProcessId.Value);
        await client.CaptureScreenshotAsync(path, cancellationToken);
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        return new GatewayScreenshotResult(runId, status.GameProcessId.Value,
            status.GameProcessStartUtc.Value, path, stream.Length, hash);
    }
}
