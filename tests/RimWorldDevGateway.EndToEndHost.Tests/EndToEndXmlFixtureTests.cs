using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndXmlFixtureTests
{
    [Test]
    public void Changed_expectation_manifest_is_rejected_before_publishing_fixture()
    {
        using var sandbox = new FixtureSandbox();
        var plan = sandbox.Plan();
        File.AppendAllText(sandbox.Manifest, " ");
        Assert.Throws<EndToEndStageException>(() => new EndToEndXmlFixturePublisher().Publish(plan, _ => { }));
        Assert.That(Directory.Exists(plan.DestinationDirectory), Is.False);
    }

    [TestCase("[{\"message\":\"Error\",\"count\":0}]")]
    [TestCase("[{\"message\":\"Error\",\"count\":17}]")]
    [TestCase("[{\"message\":\"Error\\ncontext\",\"count\":1}]")]
    [TestCase("[{\"message\":\"Error\",\"count\":1,\"regex\":true}]")]
    [TestCase("[{\"message\":\"Error\",\"count\":1,\"count\":2}]")]
    [TestCase("[{\"message\":\"Error\",\"count\":1},{\"message\":\"Error\",\"count\":1}]")]
    public void Invalid_error_expectations_are_rejected(string entries)
    {
        using var sandbox = new FixtureSandbox();
        File.WriteAllText(sandbox.Manifest, File.ReadAllText(sandbox.Manifest).Replace("{", "{\"expectedErrors\":" + entries + ","));
        Assert.Throws<EndToEndStageException>(() => sandbox.Plan());
    }

    [Test]
    public void Selected_expectations_are_hashed_and_unselected_fixture_grants_none()
    {
        using var sandbox = new FixtureSandbox();
        var json = JsonNode.Parse(File.ReadAllText(sandbox.Manifest))!;
        json["expectedErrors"] = JsonSerializer.SerializeToNode(new[] { new { message = "Exact deliberate error", count = 1 } });
        File.WriteAllText(sandbox.Manifest, json.ToJsonString());
        var selected = EndToEndBundlePlanner.Create(new[] { sandbox.Candidate() }, new[] { "ludeon.rimworld", FixtureSandbox.Owner },
            sandbox.ModsRoot, "1.7", new[] { "diagnostics.xml" }, Array.Empty<string>(), sandbox.Root);
        var fixture = JsonSerializer.SerializeToNode(selected.XmlFixturesByGroup.Single().Value.Single())!;
        Assert.That(fixture["ExpectedErrors"]![0]!["Message"]!.GetValue<string>(), Is.EqualTo("Exact deliberate error"));
        Assert.That(fixture["ManifestSha256"]!.GetValue<string>(), Is.EqualTo(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sandbox.Manifest)))));
        var unselected = EndToEndBundlePlanner.Create(new[] { sandbox.Candidate() }, new[] { "ludeon.rimworld", FixtureSandbox.Owner },
            sandbox.ModsRoot, "1.7", new[] { "diagnostics.base" }, Array.Empty<string>(), sandbox.Root);
        Assert.That(unselected.XmlFixturesByGroup, Is.Empty);
    }

    [Test]
    public void Journal_limit_rejects_further_publication_and_retains_readable_rollback_lease()
    {
        using var sandbox = new FixtureSandbox();
        string journal = Path.Combine(sandbox.Root, "fixture.lease.json");
        var plans = Enumerable.Range(0, 180).Select(index => sandbox.AddPlan("fumblesneeze.fixture" + index)).ToArray();
        FileStream? locked = null;
        var observed = new ObservedPlans(plans, () => locked = new FileStream(
            Path.Combine(plans[0].DestinationDirectory, EndToEndXmlFixturePublisher.MarkerFileName), FileMode.Open, FileAccess.Read, FileShare.None));
        try
        {
            var failure = Assert.Throws<EndToEndStageException>(() => EndToEndXmlFixtureStaging.Stage(observed, journal));
            Assert.That(failure!.ToString(), Does.Contain("64 KiB"));
            Assert.That(new FileInfo(journal).Length, Is.LessThanOrEqualTo(64 * 1024));
            Assert.That(Directory.Exists(plans.Last().DestinationDirectory), Is.False);
        }
        finally { locked?.Dispose(); }
        Assert.That(EndToEndXmlFixtureStaging.Clean(journal), Is.Positive);
        Assert.That(File.Exists(journal), Is.False);
        Assert.That(Directory.Exists(plans[0].DestinationDirectory), Is.False);
    }

    private sealed class ObservedPlans(EndToEndXmlFixturePlan[] plans, Action afterFirst) : IReadOnlyList<EndToEndXmlFixturePlan>
    {
        public int Count => plans.Length;
        public EndToEndXmlFixturePlan this[int index] => plans[index];
        public IEnumerator<EndToEndXmlFixturePlan> GetEnumerator()
        {
            for (int index = 0; index < plans.Length; index++)
            {
                if (index == 1) afterFirst();
                yield return plans[index];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestCase("owner")]
    [TestCase("conditional")]
    [TestCase("wildcard")]
    [TestCase("missing-dependency")]
    public void Project_fixture_declarations_cannot_bypass_owner_or_dependency_checks(string mode)
    {
        using var sandbox = new FixtureSandbox();
        var candidate = sandbox.Candidate();
        string source = File.ReadAllText(candidate.ProjectPath);
        if (mode == "owner") source = source.Replace(FixtureSandbox.Owner, "fumblesneeze.other");
        if (mode == "conditional") source = source.Replace("<ItemGroup>", "<ItemGroup Condition=\"'true' == 'true'\">");
        if (mode == "wildcard") source = source.Replace("Diagnostics/fixture.json", "*/fixture.json");
        File.WriteAllText(candidate.ProjectPath, source);
        var installed = mode == "missing-dependency" ? new[] { FixtureSandbox.Owner } : new[] { "ludeon.rimworld", FixtureSandbox.Owner };
        Assert.That(() => EndToEndBundlePlanner.Create(new[] { candidate }, installed, sandbox.ModsRoot, "1.7",
            new[] { "diagnostics.xml" }, Array.Empty<string>(), sandbox.Root), Throws.Exception);
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [Test]
    public void Duplicate_journal_fields_do_not_select_an_ambiguous_cleanup_identity()
    {
        using var sandbox = new FixtureSandbox();
        string journal = Path.Combine(sandbox.Root, "fixture.lease.json");
        var lease = EndToEndXmlFixtureStaging.Stage(new[] { sandbox.Plan() }, journal).Single();
        string original = File.ReadAllText(journal);
        File.WriteAllText(journal, original.Replace("\"transactionId\":", "\"transactionId\":\"ambiguous\",\"transactionId\":"));
        Assert.That(() => EndToEndXmlFixtureStaging.Clean(journal), Throws.Exception);
        Assert.That(Directory.Exists(lease.DestinationDirectory), Is.True);
        Assert.That(File.Exists(journal), Is.True);
    }

    [Test]
    public void Internal_fixture_clean_command_removes_only_published_lease()
    {
        using var sandbox = new FixtureSandbox();
        string journal = Path.Combine(sandbox.Root, "fixture.lease.json");
        var lease = EndToEndXmlFixtureStaging.Stage(new[] { sandbox.Plan() }, journal).Single();
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.That(EndToEndHostCli.Invoke(new[] { "fixture-clean", "--lease-file", journal, "--output", "json" }, output, error), Is.Zero, error.ToString());
        Assert.That(File.Exists(journal), Is.False);
        Assert.That(Directory.Exists(lease.DestinationDirectory), Is.False);
        Assert.That(JsonDocument.Parse(output.ToString()).RootElement.GetProperty("cleanedCount").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public void Invalid_lease_anywhere_in_journal_prevents_partial_cleanup()
    {
        using var sandbox = new FixtureSandbox();
        string journal = Path.Combine(sandbox.Root, "fixture.lease.json");
        var lease = EndToEndXmlFixtureStaging.Stage(new[] { sandbox.Plan() }, journal).Single();
        var json = JsonNode.Parse(File.ReadAllText(journal))!;
        var invalid = json["leases"]![0]!.DeepClone();
        invalid["transactionId"] = "invalid";
        json["leases"]!.AsArray().Insert(0, invalid);
        File.WriteAllText(journal, json.ToJsonString());
        Assert.Throws<EndToEndStageException>(() => EndToEndXmlFixtureStaging.Clean(journal));
        Assert.That(Directory.Exists(lease.DestinationDirectory), Is.True);
        Assert.That(File.Exists(journal), Is.True);
    }

    [Test]
    public void Stage_bridge_retains_exact_lease_and_clean_removes_it_after_owned_package()
    {
        using var sandbox = new FixtureSandbox();
        string journal = Path.Combine(sandbox.Root, "evidence", "fixture.lease.json");
        var leases = EndToEndXmlFixtureStaging.Stage(new[] { sandbox.Plan() }, journal);
        Assert.That(File.Exists(journal), Is.True);
        Assert.That(File.Exists(Path.Combine(leases.Single().DestinationDirectory, "About", "About.xml")), Is.True);
        Assert.That(EndToEndXmlFixtureStaging.Clean(journal), Is.EqualTo(1));
        Assert.That(File.Exists(journal), Is.False);
        Assert.That(Directory.Exists(leases.Single().DestinationDirectory), Is.False);
    }

    [Test]
    public void Selected_project_fixture_satisfies_only_its_selected_group_and_keeps_full_metadata()
    {
        using var sandbox = new FixtureSandbox();
        var candidate = sandbox.Candidate();
        var installed = new[] { "ludeon.rimworld", FixtureSandbox.Owner };
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, installed, sandbox.ModsRoot, "1.7",
            new[] { "diagnostics.xml" }, Array.Empty<string>(), sandbox.Root);
        Assert.That(plan.Discovery.Groups, Has.Count.EqualTo(2));
        Assert.That(plan.OwnerStages.Single().Bundles.Single().Manifest.Tests, Has.Length.EqualTo(2));
        Assert.That(plan.XmlFixturesByGroup.Single().Value.Single().PackageId, Is.EqualTo(FixtureSandbox.Package));
        Assert.That(installed, Does.Not.Contain(FixtureSandbox.Package));
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Trailing_mods_separator_keeps_staging_outside_scanned_mods(bool alternateSeparator)
    {
        using var sandbox = new FixtureSandbox();
        char separator = alternateSeparator ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;
        var plan = EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root, new[] { sandbox.Manifest },
            new[] { "ludeon.rimworld", FixtureSandbox.Owner, FixtureSandbox.Package },
            Array.Empty<string>(), sandbox.ModsRoot + separator, "1.7").Single();
        EndToEndXmlFixtureLease? retained = null;
        bool stagedPayloadObserved = false;
        var publisher = new EndToEndXmlFixturePublisher(new Fault("after-stage-write", () =>
            stagedPayloadObserved = File.Exists(Path.Combine(retained!.TemporaryDirectory, "About", "About.xml"))));
        var lease = publisher.Publish(plan, prepared => retained = prepared);
        try
        {
            string expectedTemporary = Path.Combine(Path.GetDirectoryName(sandbox.ModsRoot)!,
                ".rimworld-modding-mcp", "xml-fixtures", lease.TransactionId);
            Assert.Multiple(() =>
            {
                Assert.That(stagedPayloadObserved, Is.True);
                Assert.That(plan.ModsRoot, Is.EqualTo(sandbox.ModsRoot));
                Assert.That(lease.TemporaryDirectory, Is.EqualTo(expectedTemporary));
                Assert.That(lease.TemporaryDirectory.StartsWith(sandbox.ModsRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase), Is.False);
            });
        }
        finally { Assert.That(publisher.TryCleanup(lease, out string? failure), Is.True, failure); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Recovery_refuses_noncanonical_mods_root_even_with_matching_marker(bool alternateSeparator)
    {
        using var sandbox = new FixtureSandbox();
        char separator = alternateSeparator ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;
        var lease = new EndToEndXmlFixtureLease(sandbox.ModsRoot + separator, FixtureSandbox.Owner,
            FixtureSandbox.Package, "1.7", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(lease.TemporaryDirectory);
        File.WriteAllText(Path.Combine(lease.TemporaryDirectory, EndToEndXmlFixturePublisher.MarkerFileName),
            JsonSerializer.Serialize(lease));
        string sentinel = Path.Combine(lease.TemporaryDirectory, "preserve.xml");
        File.WriteAllText(sentinel, "preserve ambiguous recovery data");
        var recovered = JsonSerializer.Deserialize<EndToEndXmlFixtureLease>(JsonSerializer.Serialize(lease))!;
        Assert.That(new EndToEndXmlFixturePublisher().TryCleanup(recovered, out string? failure), Is.False);
        Assert.That(failure, Does.Contain("lease"));
        Assert.That(File.ReadAllText(sentinel), Is.EqualTo("preserve ambiguous recovery data"));
    }

    [Test]
    public void Checked_in_diagnostics_fixture_has_a_valid_xml_only_plan()
    {
        string root = TestContext.CurrentContext.TestDirectory;
        while (!Directory.Exists(Path.Combine(root, "openspec")))
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        using var sandbox = new FixtureSandbox();
        var plan = EndToEndXmlFixturePlanner.PlanSelected(root,
            new[] { "tests/Fixtures/Megamod.Diagnostics/fixture.json" },
            new[] { "brrainz.harmony", "ludeon.rimworld", FixtureSandbox.Owner, FixtureSandbox.Package },
            Array.Empty<string>(), sandbox.ModsRoot, "1.7").Single();
        Assert.That(plan.Files.Select(file => file.RelativePath),
            Is.EquivalentTo(new[] { "About/About.xml", "Defs/DefinitionDiagnostics.xml" }));
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [TestCase("marker")]
    [TestCase("payload")]
    public void Failed_rollback_keeps_recoverable_lease_and_reports_incomplete_cleanup(string lockedFile)
    {
        using var sandbox = new FixtureSandbox();
        EndToEndXmlFixtureLease? retained = null;
        FileStream? lockedMarker = null;
        var publisher = new EndToEndXmlFixturePublisher(new Fault("after-stage-write", () =>
        {
            lockedMarker = new FileStream(Path.Combine(retained!.TemporaryDirectory,
                    lockedFile == "marker" ? EndToEndXmlFixturePublisher.MarkerFileName : "Defs/Diagnostic.xml"),
                FileMode.Open, FileAccess.Read, FileShare.None);
            throw new IOException("storage failed while marker is temporarily unavailable");
        }));
        try
        {
            var failure = Assert.Throws<EndToEndStageException>(() => publisher.Publish(sandbox.Plan(), lease => retained = lease));
            Assert.That(failure!.Message, Does.Contain("cleanup incomplete"));
            Assert.That(Directory.Exists(retained!.TemporaryDirectory), Is.True);
        }
        finally { lockedMarker?.Dispose(); }
        Assert.That(publisher.TryCleanup(retained!, out string? cleanupFailure), Is.True, cleanupFailure);
        Assert.That(Directory.Exists(retained!.TemporaryDirectory), Is.False);
    }

    [Test]
    public void Publishers_serialize_one_destination_before_preparing_the_second_lease()
    {
        using var sandbox = new FixtureSandbox();
        var plan = sandbox.Plan();
        using var firstEntered = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();
        Exception? firstFailure = null;
        Exception? secondFailure = null;
        var first = Task.Run(() =>
        {
            try { new EndToEndXmlFixturePublisher().Publish(plan, _ => { firstEntered.Set(); if (!releaseFirst.Wait(5000)) throw new TimeoutException(); }); }
            catch (Exception failure) { firstFailure = failure; }
        });
        Task? second = null;
        bool overlapped = false;
        try
        {
            Assert.That(firstEntered.Wait(5000), Is.True);
            second = Task.Run(() =>
            {
                secondStarted.Set();
                try { new EndToEndXmlFixturePublisher().Publish(plan, _ => secondEntered.Set()); }
                catch (Exception failure) { secondFailure = failure; }
            });
            Assert.That(secondStarted.Wait(5000), Is.True);
            overlapped = secondEntered.Wait(300);
        }
        finally
        {
            releaseFirst.Set();
            Assert.That(Task.WaitAll(second is null ? new[] { first } : new[] { first, second }, 5000), Is.True);
        }
        Assert.That(overlapped, Is.False, "Only one publisher may prepare a lease for the canonical destination at a time.");
        Assert.That(firstFailure, Is.Null);
        Assert.That(secondFailure, Is.TypeOf<EndToEndStageException>());
    }

    [Test]
    public void Publication_rechecks_destination_after_persistence_before_writing()
    {
        using var sandbox = new FixtureSandbox();
        string protectedDirectory = Path.Combine(sandbox.Root, "protected");
        Directory.CreateDirectory(protectedDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(sandbox.ModsRoot)!);
        try
        {
            Assert.Throws<EndToEndStageException>(() => new EndToEndXmlFixturePublisher().Publish(sandbox.Plan(),
                _ => FixtureSandbox.CreateDirectoryLink(sandbox.ModsRoot, protectedDirectory)));
            Assert.That(Directory.GetFileSystemEntries(protectedDirectory), Is.Empty);
        }
        finally { if (Directory.Exists(sandbox.ModsRoot)) Directory.Delete(sandbox.ModsRoot); }
    }

    [TestCase("oversized-manifest")]
    [TestCase("oversized-payload")]
    [TestCase("wrong-json-type")]
    [TestCase("malformed-json")]
    [TestCase("about-dtd")]
    public void Malformed_or_unbounded_input_has_a_fixture_validation_failure(string input)
    {
        using var sandbox = new FixtureSandbox();
        if (input == "oversized-manifest") File.AppendAllText(sandbox.Manifest, new string(' ', 64 * 1024));
        if (input == "oversized-payload") File.AppendAllText(sandbox.DefsFile, new string(' ', 8 * 1024 * 1024));
        if (input == "wrong-json-type") File.WriteAllText(sandbox.Manifest, "[]");
        if (input == "malformed-json") File.WriteAllText(sandbox.Manifest, "{invalid");
        if (input == "about-dtd") File.WriteAllText(sandbox.AboutFile, "<!DOCTYPE ModMetaData SYSTEM 'untrusted.dtd'>" + File.ReadAllText(sandbox.AboutFile));
        Assert.Throws<EndToEndStageException>(() => sandbox.Plan());
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [TestCase("after-lease-persist")]
    [TestCase("after-stage-write")]
    [TestCase("after-commit")]
    public void Publication_failure_cleans_only_its_retained_transaction(string point)
    {
        using var sandbox = new FixtureSandbox();
        var publisher = new EndToEndXmlFixturePublisher(new Fault(point, () => throw new IOException("injected storage failure")));
        EndToEndXmlFixtureLease? retained = null;
        var failure = Assert.Throws<EndToEndStageException>(() => publisher.Publish(sandbox.Plan(), lease => retained = lease));
        Assert.That(failure!.Message, Does.Contain("cleaned"));
        Assert.That(retained, Is.Not.Null);
        Assert.That(Directory.Exists(retained!.TemporaryDirectory), Is.False);
        Assert.That(Directory.Exists(retained.DestinationDirectory), Is.False);
        Assert.That(File.Exists(sandbox.DefsFile), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Planner_rejects_duplicate_fixture_identity(bool duplicateManifest)
    {
        using var sandbox = new FixtureSandbox();
        Assert.Throws<EndToEndStageException>(() => EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root,
            duplicateManifest ? new[] { sandbox.Manifest, sandbox.Manifest } : new[] { sandbox.Manifest },
            new[] { "ludeon.rimworld", FixtureSandbox.Owner, FixtureSandbox.Package },
            duplicateManifest ? Array.Empty<string>() : new[] { FixtureSandbox.Package.ToUpperInvariant() }, sandbox.ModsRoot, "1.7"));
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [Test]
    public void Publisher_preserves_existing_canonical_package_and_does_not_prepare_a_lease()
    {
        using var sandbox = new FixtureSandbox();
        var plan = sandbox.Plan();
        Directory.CreateDirectory(plan.DestinationDirectory);
        string sentinel = Path.Combine(plan.DestinationDirectory, "unowned.xml");
        File.WriteAllText(sentinel, "preserve existing package");
        int persisted = 0;
        Assert.Throws<EndToEndStageException>(() => new EndToEndXmlFixturePublisher().Publish(plan, _ => persisted++));
        Assert.That(persisted, Is.Zero);
        Assert.That(File.ReadAllText(sentinel), Is.EqualTo("preserve existing package"));
    }

    [TestCase("changed-marker")]
    [TestCase("descendant-reparse")]
    [TestCase("forged-path")]
    public void Cleanup_preserves_paths_when_ownership_or_tree_is_unsafe(string change)
    {
        using var sandbox = new FixtureSandbox();
        var publisher = new EndToEndXmlFixturePublisher();
        var lease = publisher.Publish(sandbox.Plan(), _ => { });
        string link = Path.Combine(lease.DestinationDirectory, "linked");
        string protectedDirectory = Path.Combine(sandbox.Root, "protected");
        Directory.CreateDirectory(protectedDirectory);
        string sentinel = Path.Combine(protectedDirectory, "sentinel");
        File.WriteAllText(sentinel, "preserve");
        if (change == "changed-marker")
            File.WriteAllText(Path.Combine(lease.DestinationDirectory, EndToEndXmlFixturePublisher.MarkerFileName),
                JsonSerializer.Serialize(lease with { TransactionId = Guid.NewGuid().ToString("N") }));
        if (change == "descendant-reparse") FixtureSandbox.CreateDirectoryLink(link, protectedDirectory);
        try
        {
            var cleanupLease = change == "forged-path" ? lease with { PackageId = "../protected" } : lease;
            Assert.That(publisher.TryCleanup(cleanupLease, out string? failure), Is.False);
            Assert.That(failure, Is.Not.Empty);
            Assert.That(Directory.Exists(lease.DestinationDirectory), Is.True);
            Assert.That(File.Exists(Path.Combine(lease.DestinationDirectory, "About", "About.xml")), Is.True,
                "Refused cleanup must preserve the complete package, not only its remaining directory.");
            if (change == "descendant-reparse") Assert.That(failure, Does.Contain("reparse"));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("preserve"));
        }
        finally { if (Directory.Exists(link)) Directory.Delete(link); }
    }

    [Test]
    public void Changed_source_is_rejected_and_owned_temporary_data_is_removed()
    {
        using var sandbox = new FixtureSandbox();
        var plan = sandbox.Plan();
        File.AppendAllText(sandbox.DefsFile, "<!-- changed after planning -->");
        var publisher = new EndToEndXmlFixturePublisher();
        EndToEndXmlFixtureLease? retained = null;
        Assert.Throws<EndToEndStageException>(() => publisher.Publish(plan, lease => retained = lease));
        Assert.That(retained, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(retained!.TemporaryDirectory), Is.False);
            Assert.That(Directory.Exists(retained.DestinationDirectory), Is.False);
        });
    }

    [Test]
    public void Publisher_persists_lease_before_mutation_and_cleanup_removes_only_published_fixture()
    {
        using var sandbox = new FixtureSandbox();
        var plan = sandbox.Plan();
        var publisher = new EndToEndXmlFixturePublisher();
        string journal = Path.Combine(sandbox.Root, "evidence", "fixture.lease.json");
        var lease = publisher.Publish(plan, prepared =>
        {
            Assert.That(Directory.Exists(prepared.TemporaryDirectory), Is.False);
            Assert.That(Directory.Exists(prepared.DestinationDirectory), Is.False);
            EndToEndLeaseJournal.WriteAtomically(journal, JsonSerializer.Serialize(prepared));
        });

        Assert.That(File.Exists(journal), Is.True, "The prepared lease must be durable before package writes.");
        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(Path.Combine(lease.DestinationDirectory, "Defs", "Diagnostic.xml")),
                Is.EqualTo(File.ReadAllBytes(sandbox.DefsFile)));
            Assert.That(File.Exists(Path.Combine(lease.DestinationDirectory, "About", "About.xml")), Is.True);
            Assert.That(File.Exists(Path.Combine(lease.DestinationDirectory, "unlisted.dll")), Is.False);
            Assert.That(Directory.Exists(lease.TemporaryDirectory), Is.False);
        });
        var recovered = JsonSerializer.Deserialize<EndToEndXmlFixtureLease>(File.ReadAllText(journal))!;
        Assert.That(publisher.TryCleanup(recovered, out string? failure), Is.True, failure);
        Assert.That(Directory.Exists(lease.DestinationDirectory), Is.False);
        Assert.That(File.Exists(sandbox.DefsFile), Is.True);
        Assert.That(publisher.TryCleanup(recovered, out failure), Is.True, failure);
    }

    [Test]
    public void Reparse_payload_directory_is_rejected_without_following_it()
    {
        using var sandbox = new FixtureSandbox();
        string link = Path.Combine(sandbox.Source, "Defs");
        string target = Path.Combine(sandbox.Root, "actual-defs");
        Directory.Move(link, target);
        FixtureSandbox.CreateDirectoryLink(link, target);
        try
        {
            Assert.Throws<EndToEndStageException>(() => sandbox.Plan());
            Assert.That(File.Exists(Path.Combine(target, "Diagnostic.xml")), Is.True);
        }
        finally { Directory.Delete(link); }
    }

    [TestCase("escape")]
    [TestCase("rooted")]
    [TestCase("normalized-traversal")]
    [TestCase("assembly")]
    [TestCase("missing-about")]
    [TestCase("duplicate")]
    [TestCase("outside-fixtures")]
    public void Unsafe_fixture_inventory_is_rejected_before_staging(string input)
    {
        using var sandbox = new FixtureSandbox();
        string manifest = sandbox.Manifest;
        var includes = new[] { "About/About.xml", "Defs/Diagnostic.xml" };
        if (input == "escape")
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(sandbox.Source)!, "outside.xml"), "<Defs/>");
            includes[1] = "../outside.xml";
        }
        if (input == "rooted") includes[1] = sandbox.DefsFile;
        if (input == "normalized-traversal") includes[1] = "Defs/../Defs/Diagnostic.xml";
        if (input == "assembly") includes[1] = "unlisted.dll";
        if (input == "missing-about") includes = new[] { "Defs/Diagnostic.xml" };
        if (input == "duplicate") includes = new[] { "About/About.xml", "Defs/Diagnostic.xml", "Defs/Diagnostic.xml" };
        sandbox.SetIncludes(includes);
        if (input == "outside-fixtures")
        {
            string outside = Path.Combine(sandbox.Root, "outside");
            Directory.Move(sandbox.Source, outside);
            manifest = Path.Combine(outside, "fixture.json");
        }
        Assert.Throws<EndToEndStageException>(() => EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root,
            new[] { manifest }, new[] { "ludeon.rimworld", FixtureSandbox.Owner, FixtureSandbox.Package },
            Array.Empty<string>(), sandbox.ModsRoot, "1.7"));
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [TestCase("schema")]
    [TestCase("extra-property")]
    [TestCase("duplicate-property")]
    [TestCase("version")]
    [TestCase("about-identity")]
    [TestCase("about-owner")]
    [TestCase("owner-order")]
    public void Invalid_selected_fixture_metadata_is_rejected(string mismatch)
    {
        using var sandbox = new FixtureSandbox();
        string metadata = File.ReadAllText(sandbox.Manifest);
        var active = new[] { "ludeon.rimworld", FixtureSandbox.Owner, FixtureSandbox.Package };
        switch (mismatch)
        {
            case "schema": metadata = metadata.Replace("XmlFixture/v1", "XmlFixture/v99"); break;
            case "extra-property": metadata = metadata.Replace("{", "{\"unknown\":true,"); break;
            case "duplicate-property": metadata = metadata.Replace("{", "{\"packageId\":\"other.mod\","); break;
            case "version": metadata = metadata.Replace("1.7", "1.6"); break;
            case "about-identity":
                File.WriteAllText(sandbox.AboutFile, File.ReadAllText(sandbox.AboutFile).Replace(FixtureSandbox.Package, "different.mod"));
                break;
            case "about-owner":
                File.WriteAllText(sandbox.AboutFile, File.ReadAllText(sandbox.AboutFile).Replace(FixtureSandbox.Owner + "<", "different.mod<"));
                break;
            case "owner-order": active = active.AsEnumerable().Reverse().ToArray(); break;
        }
        File.WriteAllText(sandbox.Manifest, metadata);
        Assert.Throws<EndToEndStageException>(() => EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root,
            new[] { sandbox.Manifest }, active, Array.Empty<string>(), sandbox.ModsRoot, "1.7"));
        Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
    }

    [Test]
    public void Unselected_fixture_does_not_require_payload_files()
    {
        using var sandbox = new FixtureSandbox();
        File.Delete(sandbox.DefsFile);
        var plans = EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root, new[] { sandbox.Manifest },
            new[] { "ludeon.rimworld", FixtureSandbox.Owner }, Array.Empty<string>(), sandbox.ModsRoot, "1.7");
        Assert.That(plans, Is.Empty);
    }

    [Test]
    public void Selected_fixture_has_exact_hashed_payload_without_mutating_installed_inventory_or_mods()
    {
        using var sandbox = new FixtureSandbox();
        var installed = new[] { "brrainz.harmony", "ludeon.rimworld", FixtureSandbox.Owner };
        var active = installed.Concat(new[] { FixtureSandbox.Package }).ToArray();

        var plans = EndToEndXmlFixturePlanner.PlanSelected(sandbox.Root, new[] { sandbox.Manifest },
            active, installed, sandbox.ModsRoot, "1.7");

        Assert.That(plans, Has.Count.EqualTo(1));
        var plan = plans.Single();
        Assert.Multiple(() =>
        {
            Assert.That(plan.PackageId, Is.EqualTo(FixtureSandbox.Package));
            Assert.That(plan.OwnerPackageId, Is.EqualTo(FixtureSandbox.Owner));
            Assert.That(plan.ActivePackageIds, Is.EqualTo(active));
            Assert.That(plan.DestinationDirectory, Is.EqualTo(Path.Combine(sandbox.ModsRoot, FixtureSandbox.Package)));
            Assert.That(plan.Files.Select(file => file.RelativePath),
                Is.EqualTo(new[] { "About/About.xml", "Defs/Diagnostic.xml" }));
            Assert.That(plan.Files.Single(file => file.RelativePath == "Defs/Diagnostic.xml").Sha256,
                Is.EqualTo(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sandbox.DefsFile)))));
            Assert.That(plan.Files.Single(file => file.RelativePath == "Defs/Diagnostic.xml").Length,
                Is.EqualTo(new FileInfo(sandbox.DefsFile).Length));
            Assert.That(installed, Does.Not.Contain(FixtureSandbox.Package));
            Assert.That(Directory.Exists(sandbox.ModsRoot), Is.False);
        });
    }

    private sealed class Fault(string point, Action action) : IEndToEndStageFaultInjector
    {
        public void OnFaultPoint(string candidate) { if (candidate == point) action(); }
    }

    private sealed class FixtureSandbox : IDisposable
    {
        public const string Owner = "fumblesneeze.megamod";
        public const string Package = "fumblesneeze.megamoddiagnosticsfixture";
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "XmlFixtureTests-" + Guid.NewGuid().ToString("N"));
        public string Source => Path.Combine(Root, "tests", "Fixtures", "Diagnostics");
        public string Manifest => Path.Combine(Source, "fixture.json");
        public string DefsFile => Path.Combine(Source, "Defs", "Diagnostic.xml");
        public string AboutFile => Path.Combine(Source, "About", "About.xml");
        public string ModsRoot => Path.Combine(Root, "game", "Mods");

        public FixtureSandbox()
        {
            Directory.CreateDirectory(Path.Combine(Source, "About"));
            Directory.CreateDirectory(Path.Combine(Source, "Defs"));
            File.WriteAllText(Path.Combine(Source, "About", "About.xml"),
                $"<ModMetaData><packageId>{Package}</packageId><supportedVersions><li>1.7</li></supportedVersions>" +
                $"<modDependencies><li><packageId>{Owner}</packageId></li></modDependencies></ModMetaData>");
            File.WriteAllText(DefsFile, "<Defs><DeliberatelyMissingDefType><defName>Diagnostic</defName></DeliberatelyMissingDefType></Defs>");
            File.WriteAllText(Path.Combine(Source, "unlisted.dll"), "not a fixture payload");
            File.WriteAllText(Manifest, JsonSerializer.Serialize(new
            {
                schema = "RimWorldDevGateway/XmlFixture/v1", ownerPackageId = Owner,
                packageId = Package, rimWorldVersion = "1.7",
                packageInclude = new[] { "Defs/Diagnostic.xml", "About/About.xml" }
            }));
        }

        public void SetIncludes(string[] includes)
        {
            var document = JsonNode.Parse(File.ReadAllText(Manifest))!;
            document["packageInclude"] = JsonSerializer.SerializeToNode(includes);
            File.WriteAllText(Manifest, document.ToJsonString());
        }

        public EndToEndXmlFixturePlan Plan() => EndToEndXmlFixturePlanner.PlanSelected(Root,
            new[] { Manifest }, new[] { "ludeon.rimworld", Owner, Package }, Array.Empty<string>(), ModsRoot, "1.7").Single();

        public EndToEndXmlFixturePlan AddPlan(string package)
        {
            string source = Path.Combine(Root, "tests", "Fixtures", package);
            Directory.CreateDirectory(Path.Combine(source, "About"));
            Directory.CreateDirectory(Path.Combine(source, "Defs"));
            File.WriteAllText(Path.Combine(source, "About", "About.xml"), File.ReadAllText(AboutFile).Replace(Package, package));
            File.WriteAllText(Path.Combine(source, "Defs", "Diagnostic.xml"), File.ReadAllText(DefsFile));
            string manifest = Path.Combine(source, "fixture.json");
            File.WriteAllText(manifest, File.ReadAllText(Manifest).Replace(Package, package));
            return EndToEndXmlFixturePlanner.PlanSelected(Root, new[] { manifest },
                new[] { "ludeon.rimworld", Owner, package }, Array.Empty<string>(), ModsRoot, "1.7").Single();
        }

        public EndToEndAssemblyCandidate Candidate()
        {
            string project = Path.Combine(Root, "tests", "Owner.EndToEnd", "Owner.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(project)!);
            File.WriteAllText(project, "<Project><PropertyGroup><RimWorldEndToEndTest>true</RimWorldEndToEndTest>" +
                $"<RimWorldEndToEndTestOwnerPackageId>{Owner}</RimWorldEndToEndTestOwnerPackageId></PropertyGroup>" +
                "<ItemGroup><RimWorldEndToEndXmlFixture Include=\"../Fixtures/Diagnostics/fixture.json\" /></ItemGroup></Project>");
            var declarations = new[]
            {
                new EndToEndMetadataDeclaration("Base", "diagnostics.base", Owner,
                    new[] { "ludeon.rimworld", Owner }, 600, 600, 60, true, true, true),
                new EndToEndMetadataDeclaration("Xml", "diagnostics.xml", Owner,
                    new[] { "ludeon.rimworld", Owner, Package }, 600, 600, 60, true, true, true)
            };
            var metadata = new EndToEndAssemblyMetadata(Path.Combine(Root, "Owner.dll"), "Owner", "1.0.0.0",
                "neutral", "null", Guid.NewGuid(), 1, new string('A', 64), Array.Empty<EndToEndAssemblyReference>(), declarations);
            return new EndToEndAssemblyCandidate(project, Owner, "Owner", metadata);
        }

        public static void CreateDirectoryLink(string link, string target)
        {
            if (!OperatingSystem.IsWindows()) { Directory.CreateSymbolicLink(link, target); return; }
            var start = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (string argument in new[] { "/d", "/c", "mklink", "/J", link, target }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            Assert.That(process.WaitForExit(10000), Is.True, "Bounded test-junction creation did not finish.");
            Assert.That(process.ExitCode, Is.Zero, process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd());
        }

        public void Dispose()
        {
            var resolved = Path.GetFullPath(Root);
            if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(resolved).StartsWith("XmlFixtureTests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unsafe test cleanup path.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }
    }
}
