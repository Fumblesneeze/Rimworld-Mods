using Megamod.Features.Diagnostics;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class WarningCaptureOptionsTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void CommonEffectiveOffKeepsWarningAndDefinitionOptionsIndependent(bool suppressWarnings)
    {
        var target = suppressWarnings ? WarningCaptureSettings.MuteWarnings : DiagnosticSettings.DefinitionContext;
        var store = new SettingsStore(new(new Dictionary<SettingDefinition<bool>, string>
        { [target] = "original-active" }));
        WarningCaptureSettings.Register(store);
        DiagnosticSettings.Register(store);
        store.LoadGlobals(new(new Dictionary<string, string>
        {
            [WarningCaptureSettings.MuteWarnings.Id] = "true",
            [DiagnosticSettings.DefinitionContext.Id] = "true"
        }));
        var warning = new WarningCaptureOptionsSource(store);
        var context = new DiagnosticOptionsSource(store);
        var initial = warning.Current;
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, false);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        var observed = Task.Run(() => warning.Current).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(initial.MuteWarnings, Is.EqualTo(!suppressWarnings), "The first warning snapshot must use common effective resolution.");
            Assert.That(observed.MuteWarnings, Is.EqualTo(!suppressWarnings));
            Assert.That(context.Current.DefinitionContextEnabled, Is.EqualTo(suppressWarnings));
            Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
            Assert.That(store.GetGlobal(DiagnosticSettings.DefinitionContext), Is.True);
        });
    }

    [TestCase("BS.BetterLog", false)]
    [TestCase("mlie.logafterdeferror", true)]
    public void SourceOverlapDoesNotEraseRequestedPreferenceOrDisableUnrelatedFeature(string package, bool available)
    {
        var store = new SettingsStore(DiagnosticOriginalFeatures.FromActivePackages(new[] { package }).EffectiveOff);
        WarningCaptureSettings.Register(store);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        var options = new WarningCaptureOptionsSource(store);
        Assert.That(options.Current.MuteWarnings, Is.EqualTo(available));
        Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
    }
    [Test]
    public void LoadedGlobalWarningMutePublishesToWorkersAndEditsDoNotRewriteOldSnapshots()
    {
        var store = new SettingsStore();
        WarningCaptureSettings.Register(store);
        Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.False);
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string> { [WarningCaptureSettings.MuteWarnings.Id] = "true" }));
        var options = new WarningCaptureOptionsSource(store);
        var before = options.Current;
        Assert.That(Task.Run(() => options.Current.MuteWarnings).Result, Is.True);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, false);
        Assert.That(Task.Run(() => options.Current.MuteWarnings).Result, Is.False);
        Assert.That(before.MuteWarnings, Is.True);
        Assert.That(WarningCaptureSettings.MuteWarnings.Scope, Is.EqualTo(SettingScope.GlobalOnly));
    }
}
