using Megamod.Features.ContentSelection.Core;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentCatalogTests
{
    [Test]
    public void ConflictingExactKeysAreAmbiguousButRepeatedSameDefIsResolvable()
    {
        var key = new ContentKey("author.provider", "Verse.ThingDef", "Medicine");
        var identity = new object();
        var repeated = new ContentCatalog(new[]
        {
            new ContentCatalogEntry(key, identity), new ContentCatalogEntry(key, identity)
        });
        var conflicting = new ContentCatalog(new[]
        {
            new ContentCatalogEntry(key, identity), new ContentCatalogEntry(key, new object())
        });

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Resolve(key).Status, Is.EqualTo(ContentResolutionStatus.Present));
            Assert.That(repeated.Resolve(key).Identity, Is.SameAs(identity));
            Assert.That(conflicting.Resolve(key).Status, Is.EqualTo(ContentResolutionStatus.Ambiguous));
            Assert.That(conflicting.Resolve(key).Identity, Is.Null);
        });
    }

    [Test]
    public void CatalogResolvesExactNativeIdentityAndRetainsItsOwnSnapshot()
    {
        var key = new ContentKey("author.provider", "Verse.ThingDef", "Medicine");
        var identity = new object();
        var supplied = new List<ContentCatalogEntry> { new(key, identity) };
        var catalog = new ContentCatalog(supplied);
        supplied.Clear();

        var result = catalog.Resolve(key);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ContentResolutionStatus.Present));
            Assert.That(result.Identity, Is.SameAs(identity));
            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Resolve(new ContentKey("other.provider", "Verse.ThingDef", "Medicine")).Status,
                Is.EqualTo(ContentResolutionStatus.Missing));
        });
    }
}
