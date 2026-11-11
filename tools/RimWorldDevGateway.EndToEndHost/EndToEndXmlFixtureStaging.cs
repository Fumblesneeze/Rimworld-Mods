using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndXmlFixtureStaging
{
    private const string Schema = "RimWorldDevGateway/XmlFixtureLeases/v1";
    private const int MaximumJournalBytes = 64 * 1024;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IReadOnlyList<EndToEndXmlFixtureLease> Stage(IReadOnlyList<EndToEndXmlFixturePlan> plans, string leaseFile)
    {
        ArgumentNullException.ThrowIfNull(plans);
        if (plans.Count == 0) throw new EndToEndStageException("XML fixture staging requires at least one selected fixture.");
        string path = ValidatePath(leaseFile);
        if (Path.Exists(path)) throw new EndToEndStageException("An XML fixture lease already exists; recover it before staging.");
        var leases = new List<EndToEndXmlFixtureLease>();
        var publisher = new EndToEndXmlFixturePublisher();
        try
        {
            foreach (var plan in plans)
                publisher.Publish(plan, lease =>
                {
                    string content = JsonSerializer.Serialize(new Journal(Schema, leases.Append(lease).ToArray()), Json);
                    if (Encoding.UTF8.GetByteCount(content) > MaximumJournalBytes)
                        throw new EndToEndStageException("XML fixture journal would exceed 64 KiB; further publication refused.");
                    leases.Add(lease);
                    EndToEndLeaseJournal.WriteAtomically(path, content);
                });
            return leases.AsReadOnly();
        }
        catch (Exception exception)
        {
            var failures = Cleanup(leases);
            if (failures.Count == 0) EndToEndLeaseJournal.Clear(path);
            throw new EndToEndStageException(failures.Count == 0
                ? "XML fixture staging failed; all prepared leases were cleaned."
                : "XML fixture staging failed; cleanup incomplete: " + string.Join("; ", failures), exception);
        }
    }

    public static int Clean(string leaseFile)
    {
        string path = ValidatePath(leaseFile);
        using var document = JsonDocument.Parse(EndToEndXmlFixturePlanner.ReadBounded(path, MaximumJournalBytes));
        RejectDuplicateFields(document.RootElement);
        var journal = document.RootElement.Deserialize<Journal>(Json);
        if (journal is null || journal.Schema != Schema || journal.Leases is not { Length: > 0 })
            throw new EndToEndStageException("The XML fixture lease journal is invalid.");
        foreach (var lease in journal.Leases) EndToEndXmlFixturePublisher.ValidateLease(lease);
        if (journal.Leases.Select(lease => lease.DestinationDirectory).Distinct(StringComparer.OrdinalIgnoreCase).Count() != journal.Leases.Length)
            throw new EndToEndStageException("The XML fixture lease journal repeats a destination.");
        var failures = Cleanup(journal.Leases);
        if (failures.Count != 0) throw new EndToEndStageException("XML fixture cleanup incomplete: " + string.Join("; ", failures));
        EndToEndLeaseJournal.Clear(path);
        return journal.Leases.Length;
    }

    private static List<string> Cleanup(IEnumerable<EndToEndXmlFixtureLease> leases)
    {
        var failures = new List<string>();
        var publisher = new EndToEndXmlFixturePublisher();
        foreach (var lease in leases.Reverse())
            if (!publisher.TryCleanup(lease, out string? failure)) failures.Add(failure ?? "Unknown cleanup failure.");
        return failures;
    }

    private static void RejectDuplicateFields(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in element.EnumerateObject())
            {
                if (!names.Add(field.Name)) throw new EndToEndStageException("Duplicate XML fixture journal field.");
                RejectDuplicateFields(field.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectDuplicateFields(child);
    }

    private static string ValidatePath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        EndToEndXmlFixturePlanner.RejectReparseAncestors(fullPath);
        return fullPath;
    }

    private sealed record Journal(string Schema, EndToEndXmlFixtureLease[] Leases);
}
