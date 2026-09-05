using NUnit.Framework;
using PersonalBugfixes.Exploration;

namespace PersonalBugfixes.Tests;

[TestFixture]
public sealed class DiscoveryFlagsTests
{
    [Test]
    public void Reading_new_feature_preserves_existing_flags_and_initializes_missing_flags()
    {
        var flags = new List<bool> { true, false, true };
        Assert.That(DiscoveryFlags.Read(flags, 4), Is.False);
        Assert.That(flags, Is.EqualTo(new[] { true, false, true, false, false }));
        Assert.That(DiscoveryFlags.Read(flags, 0), Is.True);
        Assert.That(flags.Count, Is.EqualTo(5));
    }

    [Test]
    public void Invalid_index_is_not_hidden_and_existing_state_is_unchanged()
    {
        var flags = new List<bool> { true };
        Assert.Throws<ArgumentOutOfRangeException>(() => DiscoveryFlags.Read(flags, -1));
        Assert.That(flags, Is.EqualTo(new[] { true }));
    }
}
