using System.Linq;
using ImmersiveSignalFire.Contacts;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class ContactCatalogTests
{
    [Test]
    public void RequiresAlliedLivingPreIndustrialFactionAndInclusiveNearbySettlement()
    {
        ContactCandidate[] candidates =
        {
            Candidate("tribal", "Tribal", 2, allied: true, defeated: false, 10, 4),
            Candidate("medieval", "Medieval", 3, allied: true, defeated: false, 9),
            Candidate("industrial", "Industrial", 4, allied: true, defeated: false, 1),
            Candidate("neutral", "Neutral", 2, allied: false, defeated: false, 1),
            Candidate("defeated", "Defeated", 2, allied: true, defeated: true, 1),
            Candidate("far", "Far", 2, allied: true, defeated: false, 11),
            Candidate("none", "No settlement", 2, allied: true, defeated: false),
        };

        EligibleContact[] actual = ContactCatalog.Select(candidates, industrialTechOrdinal: 4, maximumDistance: 10)
            .ToArray();

        Assert.That(actual.Select(contact => contact.Id), Is.EqualTo(new[] { "tribal", "medieval" }));
        Assert.That(actual[0].NearestDistance, Is.EqualTo(4));
    }

    [Test]
    public void OrdersByNearestSettlementThenNameAndProjectsEachFactionOnce()
    {
        ContactCandidate[] candidates =
        {
            Candidate("z", "Zulu", 2, allied: true, defeated: false, 7, 2, 5),
            Candidate("b", "Beta", 3, allied: true, defeated: false, 2),
            Candidate("a", "Alpha", 2, allied: true, defeated: false, 2),
        };

        EligibleContact[] actual = ContactCatalog.Select(candidates, 4, 10).ToArray();

        Assert.That(actual.Select(contact => contact.Id), Is.EqualTo(new[] { "a", "b", "z" }));
        Assert.That(actual.Select(contact => contact.NearestDistance), Is.EqualTo(new[] { 2, 2, 2 }));
    }

    private static ContactCandidate Candidate(
        string id,
        string name,
        int tech,
        bool allied,
        bool defeated,
        params int[] settlementDistances) =>
        new(id, name, tech, allied, defeated, settlementDistances);
}
