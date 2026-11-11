using System.Security.Cryptography;
using System.Text.Json;

namespace RimWorldDevGateway.EndToEndHost;

public sealed record EndToEndXmlFixtureExpectationReceipt(string Path, string Sha256);

public static class EndToEndXmlFixtureExpectations
{
    // Internal handoff from the fixture publisher to its exact child, never a caller-provided ignore list.
    public static EndToEndXmlFixtureExpectationReceipt Write(IReadOnlyList<EndToEndXmlFixturePlan> plans, string leaseFile)
    {
        if (plans.Count == 0) throw new EndToEndStageException("Expected diagnostics require selected fixture plans.");
        var active = plans[0].ActivePackageIds;
        if (plans.Any(plan => !plan.ActivePackageIds.SequenceEqual(active)))
            throw new EndToEndStageException("Expected diagnostics require one exact selected active group.");
        var messages = plans.SelectMany(plan => plan.ExpectedErrors).Select(error => error.Message).ToArray();
        if (messages.Distinct(StringComparer.Ordinal).Count() != messages.Length)
            throw new EndToEndStageException("Expected diagnostic messages must have one fixture owner in a selected group.");
        string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(leaseFile))!, "fixture-expectations.json");
        EndToEndXmlFixturePlanner.RejectReparseAncestors(path);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schema = "RimWorldDevGateway/XmlFixtureExpectations/v1", activePackageIds = active,
            fixtures = plans.Select(plan => new
            {
                plan.OwnerPackageId, plan.PackageId, plan.ManifestSha256, plan.Files, plan.ExpectedErrors
            })
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (bytes.Length > 64 * 1024) throw new EndToEndStageException("XML fixture expectations exceed 64 KiB.");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        return new EndToEndXmlFixtureExpectationReceipt(path, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}
