using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record GatewayRawMutationResult(
    string RunId,
    string SourceSha256,
    string EntryType,
    JsonElement Response,
    bool Mutation,
    bool GameplayAcceptanceEvidence);

public sealed class GatewayRawMutation(string repositoryRoot)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<GatewayRawMutationResult> ExecuteAsync(
        string runId,
        string sourceCode,
        string entryType,
        string requestJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceCode) || Encoding.UTF8.GetByteCount(sourceCode) > 256 * 1024)
            throw new ArgumentException("sourceCode must be nonblank and at most 256 KiB UTF-8.");
        var status = new RunLeaseManager(_repositoryRoot).Status(runId);
        if (status.State != "ready" || status.GameProcessId is null || status.GatewayManifestPath is null)
            throw new ArgumentException("The selected run has no exact ready Gateway process and manifest.");
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceCode)));
        var root = Path.Combine(status.RunRoot, "raw-mutations");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{sourceHash[..12]}.cs");
        try
        {
            File.WriteAllText(path, sourceCode, new UTF8Encoding(false));
            var client = new GatewayWorkshopClient(
                _repositoryRoot, status.GatewayManifestPath, status.GameProcessId.Value);
            var response = await client.ExecuteSourceAsync(path, entryType, requestJson, cancellationToken);
            return new GatewayRawMutationResult(runId, sourceHash, entryType, response, true, false);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
