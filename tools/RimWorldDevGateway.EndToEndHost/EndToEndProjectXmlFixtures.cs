using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndProjectXmlFixtures
{
    public static IReadOnlyDictionary<string, IReadOnlyList<EndToEndXmlFixturePlan>> PlanSelected(
        string repositoryRoot, EndToEndDiscoveryResult discovery, IEnumerable<string> installedPackageIds,
        string modsRoot, string version)
    {
        var result = new Dictionary<string, IReadOnlyList<EndToEndXmlFixturePlan>>(StringComparer.Ordinal);
        foreach (var group in discovery.SelectedTests.GroupBy(EndToEndDiscoveryValidator.GroupId))
        {
            var plans = new List<EndToEndXmlFixturePlan>();
            foreach (var project in group.GroupBy(test => test.ProjectPath, StringComparer.OrdinalIgnoreCase))
            {
                string owner = project.First().OwnerPackageId;
                var manifests = ReadDeclarations(repositoryRoot, project.Key, owner);
                var selected = EndToEndXmlFixturePlanner.PlanSelected(repositoryRoot, manifests,
                    group.First().ActivePackageIds, installedPackageIds, modsRoot, version);
                if (selected.Any(plan => plan.OwnerPackageId != owner))
                    throw new EndToEndStageException("XML fixture owner does not match its declaring E2E project.");
                plans.AddRange(selected);
            }
            if (plans.GroupBy(plan => plan.PackageId, StringComparer.OrdinalIgnoreCase).Any(items => items.Count() > 1))
                throw new EndToEndStageException("Selected group declares an XML fixture package more than once.");
            if (plans.Count > 0) result.Add(group.Key, new ReadOnlyCollection<EndToEndXmlFixturePlan>(plans));
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<EndToEndXmlFixturePlan>>(result);
    }

    private static string[] ReadDeclarations(string repositoryRoot, string projectPath, string owner)
    {
        string full = Path.GetFullPath(projectPath);
        string prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot)) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new EndToEndStageException("XML fixture declaring project escapes the repository.");
        using var stream = new MemoryStream(EndToEndXmlFixturePlanner.ReadBounded(full, 1024 * 1024));
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        var items = document.Descendants().Where(element => element.Name.LocalName == "RimWorldEndToEndXmlFixture").ToArray();
        if (items.Length == 0) return Array.Empty<string>();
        var marker = document.Descendants().Where(element => element.Name.LocalName == "RimWorldEndToEndTest").ToArray();
        var owners = document.Descendants().Where(element => element.Name.LocalName == "RimWorldEndToEndTestOwnerPackageId").ToArray();
        if (marker.Length != 1 || marker[0].Value != "true" || owners.Length != 1 || owners[0].Value != owner)
            throw new EndToEndStageException("XML fixtures require an explicitly marked matching-owner E2E project.");
        return items.Select(item =>
        {
            string? include = item.Attribute("Include")?.Value;
            if (item.Attributes().Count() != 1 || item.HasElements || string.IsNullOrWhiteSpace(include) ||
                include.IndexOfAny(new[] { '$', '*', '?', ';' }) >= 0 ||
                item.Ancestors().Any(parent => parent.Attribute("Condition") is not null))
                throw new EndToEndStageException("XML fixture project item must contain one unconditional literal Include.");
            return Path.GetFullPath(include, Path.GetDirectoryName(full)!);
        }).ToArray();
    }
}
