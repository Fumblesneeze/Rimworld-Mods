using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class DrugGenerationProviderPolicyTests
{
    [Test]
    public void ActiveOriginalUsesOrdinaryEffectiveOffWithoutRewritingPreferences()
    {
        var active = new[] { "SEOHYEON.BESTDRUGPOLICY", "seohyeon.bestdrugpolicy" };
        var store = new SettingsStore(new SettingsEffectiveOff(DrugGenerationProviderPolicy.Reasons(active)));
        DrugGenerationSettings.Register(store);
        store.SetGlobal(DrugGenerationSettings.InitialMood, .4f);
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, DrugGenerationSettings.Enabled, false);
        store.SetSaveOverride(binding, DrugGenerationSettings.Enabled, true);
        var before = store.ExportSave(binding);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(DrugGenerationSettings.Enabled), Is.False);
            Assert.That(store.ReadGlobal(DrugGenerationSettings.Enabled).Value, Is.True);
            Assert.That(store.ReadCurrentRequested(DrugGenerationSettings.Enabled).Value, Is.True);
            Assert.That(DrugGenerationSettings.Capture(store).InitialMood, Is.EqualTo(.4f));
            Assert.That(store.ExportSave(binding).Values, Is.EquivalentTo(before.Values));
            Assert.That(DrugGenerationProviderPolicy.StartupWarnings(active), Has.Exactly(1).Items);
        });
    }

    [Test]
    public void UnrelatedActivePackagesLeaveGeneratorAvailableWithoutOverlapWarning()
    {
        var active = new[] { "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod" };
        var store = new SettingsStore(new SettingsEffectiveOff(DrugGenerationProviderPolicy.Reasons(active)));
        DrugGenerationSettings.Register(store);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(DrugGenerationSettings.Enabled), Is.True);
            Assert.That(DrugGenerationProviderPolicy.Reasons(active), Is.Empty);
            Assert.That(DrugGenerationProviderPolicy.StartupWarnings(active), Is.Empty);
        });
    }
}
