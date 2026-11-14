using Megamod.Features.PracticalNeeds.Core;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.PracticalNeeds;

[TestFixture]
public sealed class PotentialMoodSettingsTests
{
    [Test]
    public void MissingSaveChoiceInheritsTheOnDefaultAtTheNextDecision()
    {
        var store = NewStore(); store.ActivateSave(new("fresh"));
        Assert.That(store.GetEffective(PotentialMoodSettings.Enabled), Is.True);
        Assert.That(store.UsesGlobal(PotentialMoodSettings.Enabled), Is.True);
        Assert.That(PotentialMoodSettings.Enabled.Id, Is.EqualTo("practicalNeeds.scheduledPotentialMood"));
        Assert.That(PotentialMoodSettings.Enabled.CategoryId, Is.EqualTo("recreation-care"));
        Assert.That(PotentialMoodSettings.Enabled.Scope, Is.EqualTo(SettingScope.GlobalAndSave));
        Assert.That(PotentialMoodSettings.Enabled.Order, Is.EqualTo(10));
        Assert.That(PotentialMoodSettings.Enabled.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.NextEligibleOccurrence));
    }

    [Test]
    public void CurrentGlobalsAndDormantSaveChoicesRemainIndependentOfTolerance()
    {
        var store = NewStore(); var first = store.ActivateSave(new("first"));
        store.SetUseGlobal(first, PotentialMoodSettings.Enabled, false);
        store.SetSaveOverride(first, PotentialMoodSettings.Enabled, false);
        store.SetUseGlobal(first, PotentialMoodSettings.Enabled, true);
        store.SetGlobal(PotentialMoodSettings.Enabled, false);
        Assert.That(store.GetEffective(PotentialMoodSettings.Enabled), Is.False);
        Assert.That(store.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        store.SetGlobal(PotentialMoodSettings.Enabled, true);
        Assert.That(store.GetEffective(PotentialMoodSettings.Enabled), Is.True);
        var saved = store.ExportSave(first);
        store.ActivateSave(new("second"));
        Assert.That(store.GetEffective(PotentialMoodSettings.Enabled), Is.True);
        Assert.That(store.SetSaveOverride(first, PotentialMoodSettings.Enabled, true).Succeeded, Is.False);
        var restored = store.ActivateSave(saved);
        store.SetUseGlobal(restored, PotentialMoodSettings.Enabled, false);
        Assert.That(store.GetEffective(PotentialMoodSettings.Enabled), Is.False);
        store.DeactivateSave();
        Assert.That(store.SetUseGlobal(restored, PotentialMoodSettings.Enabled, true).Succeeded, Is.False);
        Assert.That(store.SetSaveOverride(restored, PotentialMoodSettings.Enabled, true).Succeeded, Is.False);
        Assert.That(store.GetGlobal(PotentialMoodSettings.Enabled), Is.True);
    }

    [Test]
    public void ActiveOriginalOnlySuppressesMoodWithoutRewritingRetainedChoices()
    {
        var initial = NewStore(); var binding = initial.ActivateSave(new("colony"));
        initial.SetUseGlobal(binding, PotentialMoodSettings.Enabled, false);
        initial.SetSaveOverride(binding, PotentialMoodSettings.Enabled, true);
        var globals = initial.ExportGlobals(); var save = initial.ExportSave(binding);
        var packages = new[] { "AVILMASK.COMMONSENSE", "avilmask.commonsense" };
        var suppressed = new SettingsStore(new SettingsEffectiveOff(PotentialMoodProviderPolicy.Reasons(packages)));
        PotentialMoodSettings.Register(suppressed); DrugToleranceSettings.Register(suppressed);
        suppressed.LoadGlobals(globals); var loaded = suppressed.ActivateSave(save);
        Assert.That(suppressed.GetEffective(PotentialMoodSettings.Enabled), Is.False);
        Assert.That(suppressed.GetEffective(DrugToleranceSettings.Enabled), Is.True);
        Assert.That(suppressed.ReadCurrentRequested(PotentialMoodSettings.Enabled).Value, Is.True);
        Assert.That(suppressed.ExportGlobals().Values, Is.EqualTo(globals.Values));
        Assert.That(suppressed.ExportSave(loaded).Values, Is.EqualTo(save.Values));
        var warning = PotentialMoodProviderPolicy.StartupWarnings(packages).Single();
        Assert.That(warning, Does.Contain("avilmask.commonsense").And.Contain("best-effort").And.Contain("retained"));
        var later = NewStore(); later.LoadGlobals(globals); later.ActivateSave(save);
        Assert.That(later.GetEffective(PotentialMoodSettings.Enabled), Is.True);
    }

    [TestCase("unrelated.mod")]
    [TestCase("avilmask.commonsense.other")]
    [TestCase("seohyeon.onlyiftolerancebelow")]
    public void InactiveOrDifferentProvidersDoNotSuppressMood(string package)
    {
        Assert.That(PotentialMoodProviderPolicy.Reasons(new[] { package }), Is.Empty);
        Assert.That(PotentialMoodProviderPolicy.StartupWarnings(new[] { package }), Is.Empty);
    }

    private static SettingsStore NewStore()
    {
        var store = new SettingsStore(); PotentialMoodSettings.Register(store); DrugToleranceSettings.Register(store); return store;
    }
}
