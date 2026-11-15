using Megamod.Features.SettingsSearch;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsSearchSettingsTests
{
    [Test]
    public void FreshSaveInheritsEnabledSearchAndAStoredSaveChoiceDoesNotChangeGlobalBrowsingPreference()
    {
        var store = new SettingsStore();
        SettingsSearchSettings.Register(store);
        var binding = store.ActivateSave(new("colony"));
        Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.True);
        Assert.That(store.UsesGlobal(SettingsSearchSettings.Enabled), Is.True);
        Assert.That(store.SetUseGlobal(binding, SettingsSearchSettings.Enabled, false).Succeeded, Is.True);
        Assert.That(store.SetSaveOverride(binding, SettingsSearchSettings.Enabled, false).Succeeded, Is.True);
        Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.False);
        Assert.That(store.GetGlobal(SettingsSearchSettings.Enabled), Is.True);
        store.DeactivateSave();
        Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.True);
        Assert.That(store.SetSaveOverride(binding, SettingsSearchSettings.Enabled, true).Succeeded, Is.False);
        Assert.That(SettingsSearchSettings.Enabled.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.NextDisplay));
    }

    [Test]
    public void ActiveNeedleSuppressesOnlySearchAndRetainsRequestedAndDormantValuesWithOneWarning()
    {
        var packages = new[] { "ZIPender.Needle", "zipender.needle" };
        var store = new SettingsStore(new SettingsEffectiveOff(SettingsSearchProviderPolicy.Reasons(packages)));
        SettingsSearchSettings.Register(store);
        var other = new SettingDefinition<bool>("other.navigation", true, SettingScope.GlobalAndSave,
            "interface.navigation", 11, SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
        store.Register(other);
        store.SetGlobal(SettingsSearchSettings.Enabled, true);
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, SettingsSearchSettings.Enabled, false);
        store.SetSaveOverride(binding, SettingsSearchSettings.Enabled, false);
        store.SetUseGlobal(binding, SettingsSearchSettings.Enabled, true);
        var globals = store.ExportGlobals();
        var saved = store.ExportSave(binding);

        Assert.That(store.GetEffective(SettingsSearchSettings.Enabled), Is.False);
        Assert.That(store.ReadCurrentRequested(SettingsSearchSettings.Enabled).Value, Is.True);
        Assert.That(store.GetEffective(other), Is.True);
        Assert.That(store.ExportGlobals().Values, Is.EqualTo(globals.Values));
        Assert.That(store.ExportSave(binding).Values, Is.EqualTo(saved.Values));
        var warnings = SettingsSearchProviderPolicy.StartupWarnings(packages).ToArray();
        Assert.That(warnings, Has.Length.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("zipender.needle").And.Contain("retained").And.Contain("best-effort"));
    }

    [TestCase("unrelated.mod")]
    [TestCase("zipender.needle.other")]
    [TestCase("other.zipender.needle")]
    [TestCase("Superniquito.ModOptionsSort")]
    public void OtherActiveIdentitiesDoNotSuppressSearch(string package)
    {
        Assert.That(SettingsSearchProviderPolicy.Reasons(new[] { package }), Is.Empty);
        Assert.That(SettingsSearchProviderPolicy.StartupWarnings(new[] { package }), Is.Empty);
    }

    [Test]
    public void EmptyActiveListHasNoOriginalProviderPolicy()
    {
        Assert.That(SettingsSearchProviderPolicy.Reasons(Array.Empty<string>()), Is.Empty);
        Assert.That(SettingsSearchProviderPolicy.StartupWarnings(Array.Empty<string>()), Is.Empty);
    }
}
