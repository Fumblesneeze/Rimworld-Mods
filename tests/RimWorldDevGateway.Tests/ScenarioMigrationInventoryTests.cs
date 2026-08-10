using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class ScenarioMigrationInventoryTests
{
    [Test]
    public void Every_legacy_descriptor_has_exactly_one_valid_inventory_entry()
    {
        var root = FindRepositoryRoot();
        var descriptorNames = Directory
            .GetFiles(Path.Combine(root, "scripts", "Scenarios"), "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var inventoryPath = Path.Combine(root, "docs", "ScenarioMigrationInventory.md");
        var inventory = File.ReadAllText(inventoryPath);
        var rows = Regex.Matches(
                inventory,
                @"^\| \[`(?<file>[^`]+\.json)`\]\((?<link>[^\r\n]+)\) \| (?<status>[^|]+) \| (?<group>[^|]+) \| (?<workflow>[^|]+) \|$",
                RegexOptions.Multiline)
            .Cast<Match>()
            .ToArray();
        var tableBody = InventoryTableBody(inventory);
        var documentedNames = rows
            .Select(match => match.Groups["file"].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(documentedNames, Is.EqualTo(descriptorNames),
                "The migration inventory must track every current JSON descriptor exactly once.");
            Assert.That(rows.Length, Is.EqualTo(tableBody.Length),
                "Every inventory table body line must parse as one complete descriptor row.");
            Assert.That(documentedNames.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(documentedNames.Length),
                "A scenario descriptor must not have duplicate inventory rows.");
            Assert.That(rows, Has.All.Matches<Match>(match =>
                    ValidStatus(match.Groups["status"].Value)),
                "Every descriptor must use one of the four contract statuses.");
            Assert.That(rows, Has.All.Matches<Match>(match =>
                    match.Groups["group"].Value.IndexOf(
                        "ludeon.rimworld",
                        StringComparison.Ordinal) >= 0 &&
                    match.Groups["group"].Value.Trim().EndsWith(
                        "`fumblesneeze.immersivechefs`",
                        StringComparison.Ordinal)),
                "Every inventory row must spell out a complete non-Gateway group from Core through the owning product.");
            Assert.That(rows, Has.All.Matches<Match>(match =>
                    !string.IsNullOrWhiteSpace(match.Groups["workflow"].Value)),
                "Every inventory row must name an observable player workflow.");
            Assert.That(rows, Has.All.Matches<Match>(match =>
                    string.Equals(
                        match.Groups["link"].Value,
                        "../scripts/Scenarios/" + match.Groups["file"].Value,
                        StringComparison.Ordinal)),
                "Every inventory row must link to its exact descriptor.");
            Assert.That(rows, Has.All.Matches<Match>(match =>
                    InventoryGroupMatchesDescriptor(root, match)),
                "Every inventory row must name the normalized complete package order declared by its descriptor.");
            Assert.That(rows.Where(match =>
                    match.Groups["status"].Value.Trim().StartsWith(
                        "converted",
                        StringComparison.Ordinal)),
                Has.All.Matches<Match>(match => ConvertedReplacementResolves(root, match)),
                "Every converted row must resolve one attributed E2E replacement with the same exact group.");
            Assert.That(
                InventoryTotals(inventory),
                Is.EqualTo(StatusTotals(rows)),
                "The stated category totals must equal the parsed inventory rows.");
        });
    }

    private static bool InventoryGroupMatchesDescriptor(string root, Match row)
    {
        var descriptorPath = Path.Combine(
            root,
            "scripts",
            "Scenarios",
            row.Groups["file"].Value);
        ScenarioDescriptor descriptor;
        try
        {
            using var stream = File.OpenRead(descriptorPath);
            descriptor = (ScenarioDescriptor)new DataContractJsonSerializer(
                typeof(ScenarioDescriptor)).ReadObject(stream)!;
        }
        catch (SerializationException)
        {
            return false;
        }

        var expectedName = Path.GetFileNameWithoutExtension(descriptorPath);
        if (descriptor.SchemaVersion != 1 ||
            !string.Equals(descriptor.Name, expectedName, StringComparison.Ordinal) ||
            descriptor.RequiredPackageIds is null ||
            descriptor.RequiredPackageIds.Length == 0 ||
            descriptor.RequiredPackageIds.Count(packageId => string.Equals(
                packageId,
                "brrainz.harmony",
                StringComparison.OrdinalIgnoreCase)) != 1 ||
            descriptor.RequiredPackageIds.Count(packageId => string.Equals(
                packageId,
                "fumblesneeze.immersivechefs",
                StringComparison.OrdinalIgnoreCase)) != 1 ||
            descriptor.RequiredPackageIds.Any(string.IsNullOrWhiteSpace) ||
            descriptor.RequiredPackageIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                descriptor.RequiredPackageIds.Length ||
            descriptor.Steps is null ||
            descriptor.Steps.Length == 0 ||
            descriptor.Steps.Any(step => !step.IsValid()) ||
            descriptor.Steps.Select(step => step.Id).Distinct(StringComparer.Ordinal).Count() !=
                descriptor.Steps.Length ||
            descriptor.Steps.Any(step => !step.HasExistingSource(descriptorPath)))
        {
            return false;
        }

        var expected = new[] { "brrainz.harmony", "ludeon.rimworld" }
            .Concat(descriptor.RequiredPackageIds.Where(packageId =>
                !string.Equals(packageId, "brrainz.harmony", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(packageId, "ludeon.rimworld", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    packageId,
                    "fumblesneeze.immersivechefs",
                    StringComparison.OrdinalIgnoreCase)))
            .Concat(new[] { "fumblesneeze.immersivechefs" })
            .ToArray();
        var documented = DocumentedGroup(row);
        return documented.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ConvertedReplacementResolves(string root, Match row)
    {
        var replacement = Regex.Match(
            row.Groups["status"].Value.Trim(),
            @"^converted → `(?<id>[^`]+)` \(delete duplicate in 5\.4\)$");
        if (!replacement.Success)
        {
            return false;
        }

        var replacementId = replacement.Groups["id"].Value;
        var matches = Directory
            .GetFiles(Path.Combine(root, "tests", "ImmersiveChefs.EndToEndTests"), "*.cs")
            .SelectMany(path => Regex.Matches(
                    File.ReadAllText(path),
                    @"\[RimWorldEndToEndTest\((?<args>.*?)\)\]",
                    RegexOptions.Singleline)
                .Cast<Match>())
            .Select(match => match.Groups["args"].Value.Replace(
                "EndToEndTestContract.CorePackageId",
                "\"ludeon.rimworld\""))
            .Select(arguments => Regex.Matches(arguments, @"""(?<value>[^""]+)""")
                .Cast<Match>()
                .Select(match => match.Groups["value"].Value)
                .ToArray())
            .Where(values => values.Length >= 3 &&
                             string.Equals(values[0], replacementId, StringComparison.Ordinal))
            .ToArray();
        return matches.Length == 1 &&
               matches[0].Skip(2).SequenceEqual(
                   DocumentedGroup(row),
                   StringComparer.OrdinalIgnoreCase);
    }

    private static string[] DocumentedGroup(Match row) => Regex
        .Matches(row.Groups["group"].Value, @"`(?<id>[^`]+)`")
        .Cast<Match>()
        .Select(match => match.Groups["id"].Value)
        .ToArray();

    private static string[] InventoryTableBody(string inventory)
    {
        var lines = inventory.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var header = Array.FindIndex(lines, line => line.StartsWith(
            "| Descriptor | Status / replacement |",
            StringComparison.Ordinal));
        if (header < 0 || header + 2 >= lines.Length)
        {
            return Array.Empty<string>();
        }

        return lines.Skip(header + 2)
            .TakeWhile(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static int[] InventoryTotals(string inventory)
    {
        var match = Regex.Match(
            inventory,
            @"Inventory total: (?<all>\d+) descriptors — (?<pending>\d+) pending E2E migration, (?<converted>\d+) converted, (?<interactive>\d+) interactive-only, and (?<retired>\d+) retired\.");
        return match.Success
            ? new[]
            {
                int.Parse(match.Groups["all"].Value),
                int.Parse(match.Groups["pending"].Value),
                int.Parse(match.Groups["converted"].Value),
                int.Parse(match.Groups["interactive"].Value),
                int.Parse(match.Groups["retired"].Value)
            }
            : Array.Empty<int>();
    }

    private static int[] StatusTotals(Match[] rows)
    {
        var statuses = rows.Select(row => row.Groups["status"].Value.Trim()).ToArray();
        return new[]
        {
            rows.Length,
            statuses.Count(status => string.Equals(status, "pending E2E migration", StringComparison.Ordinal)),
            statuses.Count(status => status.StartsWith("converted", StringComparison.Ordinal)),
            statuses.Count(status => string.Equals(status, "interactive-only", StringComparison.Ordinal)),
            statuses.Count(status => string.Equals(status, "retired", StringComparison.Ordinal))
        };
    }

    private static bool ValidStatus(string value)
    {
        var normalized = value.Trim();
        return string.Equals(normalized, "pending E2E migration", StringComparison.Ordinal) ||
               Regex.IsMatch(normalized,
                   @"^converted → `[^`]+` \(delete duplicate in 5\.4\)$") ||
               string.Equals(normalized, "interactive-only", StringComparison.Ordinal) ||
               string.Equals(normalized, "retired", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImmersiveChefs.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    [DataContract]
    private sealed class ScenarioDescriptor
    {
        [DataMember(Name = "schemaVersion", IsRequired = true)]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "name", IsRequired = true)]
        public string? Name { get; set; }

        [DataMember(Name = "requiredPackageIds", IsRequired = true)]
        public string[]? RequiredPackageIds { get; set; }

        [DataMember(Name = "steps", IsRequired = true)]
        public ScenarioStep[]? Steps { get; set; }
    }

    [DataContract]
    private sealed class ScenarioStep
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string? Id { get; set; }

        [DataMember(Name = "kind", IsRequired = true)]
        public string? Kind { get; set; }

        [DataMember(Name = "sourceFile")]
        public string? SourceFile { get; set; }

        [DataMember(Name = "fileName")]
        public string? FileName { get; set; }

        internal bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Kind))
            {
                return false;
            }

            return string.Equals(Kind, "csharp", StringComparison.Ordinal)
                ? !string.IsNullOrWhiteSpace(SourceFile)
                : string.Equals(Kind, "screenshot", StringComparison.Ordinal) &&
                  !string.IsNullOrWhiteSpace(FileName);
        }

        internal bool HasExistingSource(string descriptorPath)
        {
            if (!string.Equals(Kind, "csharp", StringComparison.Ordinal))
            {
                return true;
            }

            return File.Exists(Path.Combine(
                Path.GetDirectoryName(descriptorPath)!,
                SourceFile!));
        }
    }
}
