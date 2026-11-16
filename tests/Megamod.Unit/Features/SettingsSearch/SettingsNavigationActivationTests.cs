using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsNavigationActivationTests
{
    [TestCase("Superniquito.ModOptionsSort", false, true)]
    [TestCase("ZIPender.Needle", true, false)]
    [TestCase("superniquito.modoptionssort.other", true, true)]
    [TestCase("unrelated.mod", true, true)]
    public void OriginalProvidersSuppressOnlyTheirFeatureAndPreserveSaveChoices(string package, bool sorting, bool searching)
    {
        var packages = new[] { package, package.ToUpperInvariant() };
        var store = new SettingsStore(SettingsEffectiveOff.Combine(
            new SettingsEffectiveOff(ModSettingsNavigationProviderPolicy.Reasons(packages)),
            new SettingsEffectiveOff(SettingsSearchProviderPolicy.Reasons(packages))));
        ModSettingsNavigationSettings.Register(store);
        SettingsSearchSettings.Register(store);
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, ModSettingsNavigationSettings.Enabled, false);
        store.SetSaveOverride(binding, ModSettingsNavigationSettings.Enabled, false);
        store.SetUseGlobal(binding, ModSettingsNavigationSettings.Enabled, true);
        var before = store.ExportSave(binding);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(ModSettingsNavigationSettings.Enabled), Is.EqualTo(sorting));
            Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.EqualTo(searching));
            Assert.That(store.GetGlobal(ModSettingsNavigationSettings.Enabled), Is.True);
            Assert.That(store.UsesGlobal(ModSettingsNavigationSettings.Enabled), Is.True);
            Assert.That(store.ExportSave(binding).Values, Is.EqualTo(before.Values));
            Assert.That(ModSettingsNavigationProviderPolicy.StartupWarnings(packages).Count(), Is.EqualTo(sorting ? 0 : 1));
        });
        store.DeactivateSave();
        Assert.That(store.SetSaveOverride(binding, ModSettingsNavigationSettings.Enabled, true).Succeeded, Is.False);
    }

    [Test]
    public void TurningSortingOffDoesNotTurnOptionSearchOff()
    {
        var store = new SettingsStore();
        ModSettingsNavigationSettings.Register(store);
        SettingsSearchSettings.Register(store);
        store.SetGlobal(ModSettingsNavigationSettings.Enabled, false);
        Assert.That(store.GetEffective(ModSettingsNavigationSettings.Enabled), Is.False);
        Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.True);
    }
}
