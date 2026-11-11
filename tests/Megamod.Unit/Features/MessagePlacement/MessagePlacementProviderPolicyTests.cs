using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessagePlacementProviderPolicyTests
{
    [TestCase("Mlie.BetterMessagePlacement.extra")]
    [TestCase("fumblesneeze.megamod")]
    [TestCase("other.bar.mod")]
    public void UnrelatedProvidersAreNotAWholeFeatureConflict(string id) =>
        Assert.That(MessagePlacementProviderPolicy.OriginalActive([id]), Is.False);

    [TestCase("Mlie.BetterMessagePlacement")]
    [TestCase(" mlie.bettermESSAGEplacement ")]
    public void TheActiveOriginalOwnsPlacementRegardlessOfPackageCasing(string id) =>
        Assert.That(MessagePlacementProviderPolicy.OriginalActive(["ludeon.rimworld", id]), Is.True);
}
