using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceSettingsTests
{
    [Test]
    public void CurrentGlobalInheritanceAndDormantOverridesUseTheCommonStore()
    {
        var store = NewStore();
        var first = store.ActivateSave(new("first"));
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        store.SetUseGlobal(first, DrugToleranceSettings.Enabled, false);
        store.SetSaveOverride(first, DrugToleranceSettings.Enabled, false);
        store.SetUseGlobal(first, DrugToleranceSettings.Enabled, true);
        store.SetGlobal(DrugToleranceSettings.Enabled, false);
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.False);
        store.SetGlobal(DrugToleranceSettings.Enabled, true);
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        store.SetUseGlobal(first, DrugToleranceSettings.Enabled, false);
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.False);
        store.ActivateSave(new("second"));
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        Assert.That(store.SetSaveOverride(first, DrugToleranceSettings.Enabled, true).Succeeded, Is.False);
        store.DeactivateSave();
        Assert.That(store.SetUseGlobal(first, DrugToleranceSettings.Enabled, true).Succeeded, Is.False);
    }
    [Test]
    public void ActiveOriginalSuppressesOnlyTheEffectiveValueAndWarnsOnce()
    {
        var store = NewStore(); var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, DrugToleranceSettings.Enabled, false);
        store.SetSaveOverride(binding, DrugToleranceSettings.Enabled, true);
        var globals = store.ExportGlobals(); var save = store.ExportSave(binding);
        var active = new[] { "SEOHYEON.ONLYIFTOLERANCEBELOW", "seohyeon.onlyiftolerancebelow" };
        var suppressed = new SettingsStore(new SettingsEffectiveOff(DrugToleranceProviderPolicy.Reasons(active)));
        DrugToleranceSettings.Register(suppressed); suppressed.LoadGlobals(globals); var restored = suppressed.ActivateSave(save);
        Assert.That(suppressed.GetEffective(DrugToleranceSettings.Enabled), Is.False);
        Assert.That(suppressed.ReadCurrentRequested(DrugToleranceSettings.Enabled).Value, Is.True);
        Assert.That(suppressed.ExportGlobals().Values, Is.EqualTo(globals.Values));
        Assert.That(suppressed.ExportSave(restored).Values, Is.EqualTo(save.Values));
        Assert.That(DrugToleranceProviderPolicy.StartupWarnings(active).Count(), Is.EqualTo(1));
        var later = NewStore(); later.LoadGlobals(globals); later.ActivateSave(save);
        Assert.That(later.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        Assert.That(DrugToleranceProviderPolicy.Reasons(new[] { "unrelated.mod" }), Is.Empty);
    }
    private static SettingsStore NewStore() { var store = new SettingsStore(); DrugToleranceSettings.Register(store); return store; }
}
