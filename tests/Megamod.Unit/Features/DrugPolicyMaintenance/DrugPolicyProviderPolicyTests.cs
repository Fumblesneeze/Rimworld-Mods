using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.DrugPolicyMaintenance;

[TestFixture]
public sealed class DrugPolicyProviderPolicyTests
{
    [Test]
    public void ActiveOriginalUsesOrdinaryStartupOffWithoutRewritingPreferences()
    {
        var provider = DrugPolicyProviderPolicy.FromActivePackages(new[] { "MLIE.DRUGPOLICYFIX", "mlie.drugpolicyfix" });
        var store = new SettingsStore(provider.EffectiveOff);
        DrugPolicySettings.Register(store);
        store.SetGlobal(DrugPolicySettings.Enabled, true);
        Assert.Multiple(() =>
        {
            Assert.That(DrugPolicySettings.CaptureStartupEnabled(store), Is.False);
            Assert.That(store.GetGlobal(DrugPolicySettings.Enabled), Is.True);
            Assert.That(provider.StartupWarnings, Has.Count.EqualTo(1));
            Assert.That(provider.StartupWarnings[0], Does.Contain("best-effort").And.Contain("Mlie.DrugPolicyFix"));
        });
        var restored = new SettingsStore();
        DrugPolicySettings.Register(restored);
        restored.LoadGlobals(store.ExportGlobals());
        Assert.That(DrugPolicySettings.CaptureStartupEnabled(restored), Is.True);
    }

    [Test]
    public void OrdinaryOffUsesOneGlobalStartupChoiceAndCannotEditSaveOverrides()
    {
        var store = new SettingsStore();
        DrugPolicySettings.Register(store);
        Assert.That(DrugPolicySettings.CaptureStartupEnabled(store), Is.True);
        store.SetGlobal(DrugPolicySettings.Enabled, false);
        bool started = DrugPolicySettings.CaptureStartupEnabled(store);
        var save = store.ActivateSave(new SaveSettingsSnapshot("save"));
        Assert.That(store.SetUseGlobal(save, DrugPolicySettings.Enabled, false).Succeeded, Is.False);
        store.SetGlobal(DrugPolicySettings.Enabled, true);
        Assert.That(started, Is.False, "The captured startup choice cannot change mid-run.");
        Assert.That(DrugPolicySettings.CaptureStartupEnabled(store), Is.True);
    }

    [Test]
    public void SimilarNamesDoNotDisableTheFeature() => Assert.That(
        DrugPolicyProviderPolicy.FromActivePackages(new[] { "mlie.drugpolicyfix.extra", "unrelated" }).StartupWarnings, Is.Empty);
}
