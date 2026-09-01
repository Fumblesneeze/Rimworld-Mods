using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmersiveSignalFire.Contacts;

public sealed class ContactCandidate
{
    public ContactCandidate(
        string id,
        string name,
        int techLevelOrdinal,
        bool allied,
        bool defeated,
        IEnumerable<int> settlementDistances)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        TechLevelOrdinal = techLevelOrdinal;
        Allied = allied;
        Defeated = defeated;
        SettlementDistances = (settlementDistances ?? throw new ArgumentNullException(nameof(settlementDistances)))
            .ToArray();
    }

    public string Id { get; }

    public string Name { get; }

    public int TechLevelOrdinal { get; }

    public bool Allied { get; }

    public bool Defeated { get; }

    public IReadOnlyList<int> SettlementDistances { get; }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A non-empty value is required.", parameterName)
            : value.Trim();
}

public sealed class EligibleContact
{
    internal EligibleContact(string id, string name, int nearestDistance)
    {
        Id = id;
        Name = name;
        NearestDistance = nearestDistance;
    }

    public string Id { get; }

    public string Name { get; }

    public int NearestDistance { get; }
}

public static class ContactCatalog
{
    public static IReadOnlyList<EligibleContact> Select(
        IEnumerable<ContactCandidate> candidates,
        int industrialTechOrdinal,
        int maximumDistance)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (maximumDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDistance));
        }

        return candidates
            .Where(candidate => candidate.Allied &&
                                !candidate.Defeated &&
                                candidate.TechLevelOrdinal < industrialTechOrdinal)
            .Select(candidate => new
            {
                Candidate = candidate,
                Nearest = candidate.SettlementDistances
                    .Where(distance => distance >= 0 && distance <= maximumDistance)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min(),
            })
            .Where(projected => projected.Nearest != int.MaxValue)
            .GroupBy(projected => projected.Candidate.Id, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(projected => projected.Nearest)
                .ThenBy(projected => projected.Candidate.Name, StringComparer.Ordinal)
                .First())
            .Select(projected => new EligibleContact(
                projected.Candidate.Id,
                projected.Candidate.Name,
                projected.Nearest))
            .OrderBy(contact => contact.NearestDistance)
            .ThenBy(contact => contact.Name, StringComparer.Ordinal)
            .ThenBy(contact => contact.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
