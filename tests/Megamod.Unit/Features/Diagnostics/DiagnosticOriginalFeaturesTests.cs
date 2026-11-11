using Megamod.Features.Diagnostics;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class DiagnosticOriginalFeaturesTests
{
    [Test]
    public void LaterStartupWithoutAnActiveOriginalRestoresTheRetainedRequest()
    {
        var first = DiagnosticOriginalFeatures.FromActivePackages(new[] { "mlie.logafterdeferror" });
        var firstStore = new SettingsStore(first.EffectiveOff);
        DiagnosticSettings.Register(firstStore);
        firstStore.SetGlobal(DiagnosticSettings.DefinitionContext, true);
        var retained = firstStore.ExportGlobals();
        var later = DiagnosticOriginalFeatures.FromActivePackages(new[] { "example.mlie.logafterdeferror", "bs.betterlog.extra" });
        var laterStore = new SettingsStore(later.EffectiveOff);
        DiagnosticSettings.Register(laterStore);
        laterStore.LoadGlobals(retained);

        Assert.That(later.StartupWarnings, Is.Empty);
        Assert.That(laterStore.GetEffective(DiagnosticSettings.DefinitionContext), Is.True);
        Assert.That(laterStore.EffectiveOffReason(DiagnosticSettings.DefinitionContext), Is.Null);
    }

    [Test]
    public void MultipleOriginalsHaveFiniteDistinctWarningsAndDoNotReadSavedPreferences()
    {
        var input = new List<string> { "mlie.logafterdeferror", "ORDPUS.LogAfterDefError", "bs.betterlog", "BS.BetterLog" };
        var originals = DiagnosticOriginalFeatures.FromActivePackages(input);
        input.Clear();
        var store = new SettingsStore(originals.EffectiveOff);
        DiagnosticSettings.Register(store);
        WarningCaptureSettings.Register(store);
        store.SetGlobal(DiagnosticSettings.DefinitionContext, false);
        Assert.That(originals.StartupWarnings, Has.Count.EqualTo(3));
        Assert.That(originals.StartupWarnings.Distinct().Count(), Is.EqualTo(3));
        Assert.That(originals.StartupWarnings.All(text => text.Length < 512 && !text.Contains('\n')), Is.True);
        Assert.That(store.GetEffective(DiagnosticSettings.DefinitionContext), Is.False);
        Assert.That(store.GetEffective(WarningCaptureSettings.MuteWarnings), Is.False);
        Assert.That(store.ExportGlobals().Values[DiagnosticSettings.DefinitionContext.Id], Is.EqualTo("false"));
        Assert.That(store.ExportGlobals().Values.ContainsKey(WarningCaptureSettings.MuteWarnings.Id), Is.False);
    }

    [Test]
    public void OriginalDefinitionProviderUsesTheSameOffPathAsItsContinuation()
    {
        var originals = DiagnosticOriginalFeatures.FromActivePackages(new[] { "ORDPUS.LogAfterDefError" });
        var store = new SettingsStore(originals.EffectiveOff);
        DiagnosticSettings.Register(store);
        Assert.That(store.GetEffective(DiagnosticSettings.DefinitionContext), Is.False);
        Assert.That(store.GetGlobal(DiagnosticSettings.DefinitionContext), Is.True);
        Assert.That(originals.StartupWarnings, Has.Count.EqualTo(1));
        Assert.That(originals.StartupWarnings[0], Does.Contain("ordpus.logafterdeferror").And.Contain("S094"));
    }

    [Test]
    public void ActiveBetterLogWarnsOnceEvenWhenMuteWasAlreadyOff()
    {
        var originals = DiagnosticOriginalFeatures.FromActivePackages(new[] { "BS.BetterLog", "bs.betterlog" });
        var store = new SettingsStore(originals.EffectiveOff);
        DiagnosticSettings.Register(store);
        WarningCaptureSettings.Register(store);

        Assert.That(originals.StartupWarnings, Has.Count.EqualTo(1));
        Assert.That(originals.StartupWarnings[0], Does.Contain("bs.betterlog").And.Contain("S035"));
        Assert.That(store.GetEffective(WarningCaptureSettings.MuteWarnings), Is.False);
        Assert.That(store.GetEffective(DiagnosticSettings.DefinitionContext), Is.True);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        Assert.That(store.GetEffective(WarningCaptureSettings.MuteWarnings), Is.False);
        Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
    }

    [Test]
    public void ActiveOriginalUsesOrdinaryOffWhileItsEnabledPreferenceAndOtherFeatureSurvive()
    {
        var originals = DiagnosticOriginalFeatures.FromActivePackages(new[] { "Mlie.LogAfterDefError" });
        var store = new SettingsStore(originals.EffectiveOff);
        DiagnosticSettings.Register(store);
        WarningCaptureSettings.Register(store);
        store.SetGlobal(DiagnosticSettings.DefinitionContext, true);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);

        Assert.That(store.GetEffective(DiagnosticSettings.DefinitionContext), Is.False);
        Assert.That(store.ReadCurrentRequested(DiagnosticSettings.DefinitionContext).Value, Is.True);
        Assert.That(store.GetEffective(WarningCaptureSettings.MuteWarnings), Is.True);
        Assert.That(originals.StartupWarnings, Has.Count.EqualTo(1));
        Assert.That(originals.StartupWarnings[0], Does.Contain("mlie.logafterdeferror").And.Contain("S094")
            .And.Contain("best-effort").And.Contain("stored preferences"));
    }
}
