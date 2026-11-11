using Megamod.Features.ContentSelection.Core;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentKeyTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("1:a1:b0:")]
    [TestCase("1:a1: 1:c")]
    [TestCase("999999999999999999999:a1:b1:c")]
    [TestCase("-1:a1:b1:c")]
    [TestCase("1:a1:b2:c")]
    [TestCase("1:a1:b1:ctrailing")]
    public void MalformedOrIncompleteStoredIdentityIsRejectedWithoutThrowing(string? token)
    {
        Assert.That(ContentKey.TryParse(token, out var key), Is.False);
        Assert.That(key, Is.Null);
    }

    [Test]
    public void StorageTokenRoundTripsExactIdentityWithoutSeparatorCollisions()
    {
        var key = new ContentKey("author.provider", "Example.SharedDef", "item/with:separators_日本語");
        var token = key.ToStorageToken();

        Assert.That(ContentKey.TryParse(token, out var restored), Is.True);
        Assert.That(restored, Is.EqualTo(key));
        Assert.That(new ContentKey("a", "bc", "d").ToStorageToken(),
            Is.Not.EqualTo(new ContentKey("ab", "c", "d").ToStorageToken()));
    }

    [Test]
    public void SourceCaseNormalizesWhileDefAndFullTypeIdentitiesStayDistinct()
    {
        var key = new ContentKey("Author.Provider", "First.SharedDef", "SameName");
        Assert.Multiple(() =>
        {
            Assert.That(key, Is.EqualTo(new ContentKey("author.provider", "First.SharedDef", "SameName")));
            Assert.That(key, Is.Not.EqualTo(new ContentKey("other.provider", "First.SharedDef", "SameName")));
            Assert.That(key, Is.Not.EqualTo(new ContentKey("author.provider", "Second.SharedDef", "SameName")));
            Assert.That(key, Is.Not.EqualTo(new ContentKey("author.provider", "First.SharedDef", "samename")));
        });
    }
}
