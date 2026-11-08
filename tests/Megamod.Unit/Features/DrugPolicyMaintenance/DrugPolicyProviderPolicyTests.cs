using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Features.DrugPolicyMaintenance;

[TestFixture]
public sealed class DrugPolicyProviderPolicyTests
{
    [Test]
    public void ActiveOriginalWithholdsOwnedMaintenanceWithoutMatchingSimilarNames()
    {
        Assert.That(DrugPolicyProviderPolicy.UnavailableReason(new[] { "ludeon.rimworld", "MLIE.DRUGPOLICYFIX" }),
            Does.Contain("Mlie.DrugPolicyFix").And.Contain("native/provider"));
        Assert.That(DrugPolicyProviderPolicy.UnavailableReason(new[] { "mlie.drugpolicyfix.extra", "unrelated" }), Is.Null);
        Assert.That(DrugPolicyProviderPolicy.UnavailableReason(Array.Empty<string>()), Is.Null);
    }
}
