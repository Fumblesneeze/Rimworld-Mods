using System.Text.Json;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class SelectedTestDiagnosticTests
{
    [TestCase("messages")]
    [TestCase("causes")]
    [TestCase("escaped-unicode")]
    public void Oversized_serialized_receipt_is_rejected_during_planning_before_any_write(string mode)
    {
        var rules = Enumerable.Range(0, mode == "causes" ? 4 : 16).Select(index => new EndToEndDiagnosticExpectation
        {
            Id = "rule-" + index, Severity = "ERROR", Count = 1,
            Message = index + new string(mode == "escaped-unicode" ? '\u00e9' : 'x', mode == "escaped-unicode" ? 1000 : 4090),
            InnerCauses = mode == "causes" ? Enumerable.Range(0, 4).Select(cause => " ---> Owned.Exception: " + cause + new string('x', 4000)).ToArray() : Array.Empty<string>()
        }).ToArray();
        var candidate = Candidate(rules);
        string root = Path.Combine(Path.GetTempPath(), "ExpectedDiagnosticLimit", Guid.NewGuid().ToString("N"));
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, candidate.Metadata.Declarations.Single().ActivePackageIds,
            Path.Combine(root, "Mods"), "1.7", new[] { "alpha.optional" }, Array.Empty<string>());
        var failure = Assert.Throws<EndToEndDiscoveryException>(() => EndToEndSelectedTestExpectations.Plan(plan));
        Assert.That(failure!.Message, Does.Contain("64 KiB"));
        Assert.Throws<EndToEndDiscoveryException>(() => EndToEndSelectedTestExpectations.Write(plan, Path.Combine(root, "stage.lease.json")));
        Assert.That(Directory.Exists(root), Is.False, "Invalid declarations must fail before creating a receipt or staging directory.");
    }

    [TestCase("unselected", false)]
    [TestCase("exact", true)]
    [TestCase("mixed", false)]
    public void Selection_never_borrows_an_unselected_siblings_allowance(string selection, bool hasAllowance)
    {
        var selected = Candidate(new[] { Rule() });
        var raw = EndToEndAssemblyMetadataReader.Read(selected.Metadata.AssemblyPath);
        var candidate = new EndToEndAssemblyCandidate(selected.ProjectPath, selected.ExpectedOwnerPackageId, selected.ExpectedAssemblyName, raw);
        var ids = selection == "exact" ? new[] { "alpha.optional" } : selection == "mixed" ? new[] { "alpha.optional", "alpha.base-a" } : new[] { "alpha.base-a" };
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod" },
            Path.Combine(Path.GetTempPath(), "ExpectedDiagnosticPlan", Guid.NewGuid().ToString("N"), "Mods"), "1.7", ids, Array.Empty<string>());
        if (selection == "mixed") {
            Assert.Throws<EndToEndDiscoveryException>(() => EndToEndSelectedTestExpectations.Plan(plan));
        } else Assert.That(EndToEndSelectedTestExpectations.Plan(plan) is not null, Is.EqualTo(hasAllowance));
    }

    [Test]
    public void Selected_plan_binds_expectations_to_exact_test_group_and_compiled_artifacts()
    {
        var candidate = Candidate(new[] { Rule() });
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, candidate.Metadata.Declarations.Single().ActivePackageIds,
            Path.Combine(Path.GetTempPath(), "ExpectedDiagnosticPlan", Guid.NewGuid().ToString("N"), "Mods"), "1.7",
            new[] { "alpha.optional" }, Array.Empty<string>());
        var selected = EndToEndSelectedTestExpectations.Plan(plan);
        Assert.That(selected, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(selected!.TestId, Is.EqualTo("alpha.optional"));
            Assert.That(selected.OwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(selected.ActivePackageIds, Is.EqualTo(candidate.Metadata.Declarations.Single().ActivePackageIds));
            Assert.That(selected.AssemblySha256, Is.EqualTo(candidate.Metadata.Sha256.ToUpperInvariant()));
            Assert.That(selected.ModuleVersionId, Is.EqualTo(candidate.Metadata.ModuleVersionId.ToString("D")));
            Assert.That(selected.ExpectedDiagnostics.Single().InnerCauses, Is.EqualTo(Rule().InnerCauses));
        });
    }

    [Test]
    public void Bundle_manifest_preserves_selected_compiled_diagnostic_contract()
    {
        var candidate = Candidate(new[] { Rule() });
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, candidate.Metadata.Declarations.Single().ActivePackageIds,
            Path.Combine(Path.GetTempPath(), "ExpectedDiagnosticPlan", Guid.NewGuid().ToString("N"), "Mods"), "1.7",
            new[] { "alpha.optional" }, Array.Empty<string>());
        using var json = JsonDocument.Parse(plan.OwnerStages.Single().Bundles.Single().ManifestJson);
        Assert.That(json.RootElement.GetProperty("schema").GetString(), Is.EqualTo("RimWorldDevGateway.EndToEndTests/v3"));
        var rule = json.RootElement.GetProperty("tests")[0].GetProperty("expectedDiagnostics")[0];
        Assert.Multiple(() =>
        {
            Assert.That(rule.GetProperty("id").GetString(), Is.EqualTo("owned-save"));
            Assert.That(rule.GetProperty("severity").GetString(), Is.EqualTo("WARN"));
            Assert.That(rule.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(rule.GetProperty("message").GetString(), Is.EqualTo(Rule().Message));
            Assert.That(rule.GetProperty("innerCauses")[0].GetString(), Is.EqualTo(Rule().InnerCauses[0]));
        });
    }

    [TestCase("severity")]
    [TestCase("fatal")]
    [TestCase("empty-id")]
    [TestCase("control")]
    [TestCase("zero-count")]
    [TestCase("excess-count")]
    [TestCase("duplicate")]
    [TestCase("ambiguous")]
    [TestCase("excess-declarations")]
    [TestCase("other-token")]
    [TestCase("repeated-token")]
    [TestCase("traversal")]
    [TestCase("outside-saves")]
    [TestCase("wildcard")]
    [TestCase("invalid-cause")]
    [TestCase("repeated-cause")]
    public void Discovery_rejects_malformed_diagnostic_declarations(string mode)
    {
        var rule = Rule();
        switch (mode)
        {
            case "severity": rule.Severity = "warn"; break;
            case "fatal": rule.Severity = "FATAL"; break;
            case "empty-id": rule.Id = ""; break;
            case "control": rule.Message += "\nOther"; break;
            case "zero-count": rule.Count = 0; break;
            case "excess-count": rule.Count = 17; break;
            case "other-token": rule.Message = "Fault at {other}"; break;
            case "repeated-token": rule.Message += "{savedata}"; break;
            case "traversal": rule.Message = "Fault at \"{savedata}\\Saves\\..\\outside.rws\""; break;
            case "outside-saves": rule.Message = "Fault at \"{savedata}\\Elsewhere\\owned.rws\""; break;
            case "wildcard": rule.Message = "Fault at \"{savedata}\\Saves\\*.rws\""; break;
            case "invalid-cause": rule.InnerCauses = new[] { "System.InvalidOperationException: unrelated" }; break;
            case "repeated-cause": rule.InnerCauses = new[] { " ---> System.Exception: owned", " ---> System.Exception: owned" }; break;
        }
        var rules = mode switch
        {
            "duplicate" => new[] { rule, rule },
            "ambiguous" => new[] { rule, new EndToEndDiagnosticExpectation { Id = "different-id", Severity = rule.Severity, Message = rule.Message, Count = 1 } },
            "excess-declarations" => Enumerable.Range(0, 17).Select(i => new EndToEndDiagnosticExpectation { Id = "entry-" + i, Severity = "ERROR", Message = "Fault " + i, Count = 1 }).ToArray(),
            _ => new[] { rule }
        };
        var candidate = Candidate(rules);
        var error = Assert.Throws<EndToEndDiscoveryException>(() => EndToEndDiscoveryValidator.DiscoverAndSelect(
            new[] { candidate }, new[] { "alpha.optional" }, Array.Empty<string>()));
        Assert.That(error!.Message, Does.Contain("expected diagnostic"));
    }

    internal static EndToEndDiagnosticExpectation Rule() => new()
    {
        Id = "owned-save", Severity = "WARN", Count = 2,
        Message = "An exception was thrown during saving to \"{savedata}\\Saves\\Owned-Fault.rws.new\": System.InvalidOperationException: owned fault",
        InnerCauses = new[] { " ---> System.InvalidOperationException: exact inner cause" }
    };

    internal static EndToEndAssemblyCandidate Candidate(IEnumerable<EndToEndDiagnosticExpectation> rules, bool gatewayOwner = false)
    {
        string root = FindRoot();
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string project = Path.Combine(root, "tests", "Fixtures", "EndToEndHost.ValidFixtures");
        var source = EndToEndAssemblyMetadataReader.Read(Path.Combine(project, "bin", configuration, "net480", "EndToEndHost.ValidFixtures.dll"));
        var original = source.Declarations.Single(test => test.Id == "alpha.optional");
        string owner = gatewayOwner ? "fumblesneeze.rimworlddevgateway" : original.OwnerPackageId;
        var declaration = new EndToEndMetadataDeclaration(original.TypeName, original.Id, owner,
            gatewayOwner ? new[] { "brrainz.harmony", "ludeon.rimworld" } : original.ActivePackageIds, original.MaxFrames, original.MaxGameTicks, original.MaxWallClockSeconds,
            original.IsConcrete, original.ImplementsContract, original.HasPublicParameterlessConstructor,
            original.LegacyXmlDeserializer, rules);
        var metadata = new EndToEndAssemblyMetadata(source.AssemblyPath, source.AssemblyName, source.AssemblyVersion,
            source.AssemblyCulture, source.AssemblyPublicKey, source.ModuleVersionId, source.Length, source.Sha256,
            source.AssemblyReferences, new[] { declaration });
        return new(Path.Combine(project, "EndToEndHost.ValidFixtures.csproj"), owner, source.AssemblyName, metadata);
    }

    internal static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) return directory.FullName;
        throw new InvalidOperationException("Repository root was not found.");
    }
}
