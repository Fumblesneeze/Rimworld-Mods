using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public sealed record EndToEndSelectedDiagnosticPlan(string Schema, string TestId, string OwnerPackageId,
    IReadOnlyList<string> ActivePackageIds, string AssemblyFileName, string AssemblyIdentity, string ModuleVersionId,
    string AssemblySha256, string ManifestFileName, string ManifestSha256, IReadOnlyList<EndToEndDiagnosticExpectation> ExpectedDiagnostics);

public static class EndToEndSelectedTestExpectations
{
    public const string Schema = "RimWorldDevGateway/SelectedTestExpectations/v1";
    private const int MaximumReceiptBytes = 64 * 1024;

    public static EndToEndSelectedDiagnosticPlan? Plan(EndToEndBundlePlan plan)
    {
        var tests = plan.Discovery.SelectedTests.Where(test => test.ExpectedDiagnostics.Count > 0).ToArray();
        if (tests.Length == 0) return null;
        if (plan.Discovery.SelectedTests.Count != 1)
            throw new EndToEndDiscoveryException("Runtime expected diagnostics require exactly one selected test in a fresh process.");
        var test = tests.Single();
        var bundle = plan.OwnerStages.Single(owner => owner.OwnerPackageId == test.OwnerPackageId).Bundles
            .Single(bundle => bundle.SourceAssemblyPath == test.AssemblyPath);
        var selected = new EndToEndSelectedDiagnosticPlan(Schema, test.Id, test.OwnerPackageId, test.ActivePackageIds.ToArray(), bundle.AssemblyFileName,
            test.AssemblyIdentity, test.ModuleVersionId.ToString("D"), test.AssemblySha256.ToUpperInvariant(),
            bundle.ManifestFileName, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bundle.ManifestJson))),
            EndToEndDiagnosticExpectation.ValidateAndCopy(test.ExpectedDiagnostics));
        _ = SerializeBounded(selected);
        return selected;
    }

    private static byte[] SerializeBounded(EndToEndSelectedDiagnosticPlan selected)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(selected, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (bytes.Length > MaximumReceiptBytes)
            throw new EndToEndDiscoveryException("Selected-test expectations exceed the aggregate serialized receipt limit of 64 KiB.");
        return bytes;
    }

    public static EndToEndXmlFixtureExpectationReceipt? Write(EndToEndBundlePlan plan, string leaseFile)
    {
        var selected = Plan(plan);
        if (selected is null) return null;
        string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(leaseFile))!, "selected-test-expectations.json");
        EndToEndXmlFixturePlanner.RejectReparseAncestors(path);
        byte[] bytes = SerializeBounded(selected);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        return new(path, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}
