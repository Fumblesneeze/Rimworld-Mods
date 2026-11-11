using Megamod.Features.Diagnostics;
using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsEffectiveOffTests
{
    [Test]
    public void EffectiveOffRetainsInvalidPreferenceDiagnosticsAndPayload()
    {
        var definition = DiagnosticSettings.DefinitionContext;
        var projection = new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
        { [definition] = "original-active" });
        var globals = new GlobalSettingsSnapshot(new Dictionary<string, string> { [definition.Id] = "invalid" });
        var store = new SettingsStore(projection);
        store.Register(definition);
        store.LoadGlobals(globals);
        var requested = store.ReadCurrentRequested(definition);
        var effective = store.ReadEffective(definition);
        var detached = SettingsSnapshotReader.ReadEffective(definition, new(globals, store.Revision.Global), projection);

        Assert.Multiple(() =>
        {
            Assert.That(requested.Value, Is.True);
            Assert.That(requested.ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
            Assert.That(effective.Value, Is.False);
            Assert.That(effective.ErrorKey, Is.EqualTo(requested.ErrorKey));
            Assert.That(detached, Is.EqualTo(effective));
            Assert.That(store.ExportGlobals().Values[definition.Id], Is.EqualTo("invalid"));
            Assert.That(store.EffectiveOffReason(definition), Is.EqualTo("original-active"));
        });
    }

    [Test]
    public void ProjectionRetainsItsStartupCopyAndLeavesUnrelatedOptionsAlone()
    {
        var definition = DiagnosticSettings.DefinitionContext;
        var reasons = new Dictionary<SettingDefinition<bool>, string> { [definition] = "original-active" };
        var projection = new SettingsEffectiveOff(reasons);
        reasons.Clear();
        reasons[WarningCaptureSettings.MuteWarnings] = "later-mutation";
        var store = new SettingsStore(projection);
        DiagnosticSettings.Register(store);
        WarningCaptureSettings.Register(store);
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        store.SetGlobal(ReadableNumbersSettings.MinimumIntegerDigits, 7);
        var punctuation = new PunctuationPreference(PunctuationPreset.Custom, "#", "@");
        store.SetGlobal(ReadableNumbersSettings.Punctuation, punctuation);

        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(definition), Is.False);
            Assert.That(store.EffectiveOffReason(definition), Is.EqualTo("original-active"));
            Assert.That(store.GetEffective(WarningCaptureSettings.MuteWarnings), Is.True);
            Assert.That(store.EffectiveOffReason(WarningCaptureSettings.MuteWarnings), Is.Null);
            Assert.That(store.GetEffective(ReadableNumbersSettings.MinimumIntegerDigits), Is.EqualTo(7));
            Assert.That(store.GetEffective(ReadableNumbersSettings.Punctuation), Is.EqualTo(punctuation));
        });
    }

    [Test]
    public void SameSettingIdDoesNotSubstituteForTheMappedDefinitionIdentity()
    {
        var original = DiagnosticSettings.DefinitionContext;
        var lookalike = new SettingDefinition<bool>(original.Id, true, original.Scope, original.CategoryId,
            original.Order, original.ApplicationTiming, SettingCodecs.Boolean);
        var store = new SettingsStore(new(new Dictionary<SettingDefinition<bool>, string>
        { [original] = "original-active" }));
        store.Register(lookalike);

        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(lookalike), Is.True);
            Assert.That(store.EffectiveOffReason(lookalike), Is.Null);
            Assert.That(() => store.EffectiveOffReason(original), Throws.InvalidOperationException
                .With.Message.EqualTo("Use the registered definition for this setting."));
        });
    }

    [Test]
    public void EnabledPreferenceIsEffectivelyOffWithoutAStoredRewrite()
    {
        var definition = DiagnosticSettings.DefinitionContext;
        var projection = new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
        { [definition] = "original-active" });
        var store = new SettingsStore(projection);
        store.Register(definition);
        store.LoadGlobals(new(new Dictionary<string, string> { [definition.Id] = "true" }));
        var revision = store.Revision;

        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(definition), Is.False, "The transient off choice must govern the effective value.");
            Assert.That(store.GetGlobal(definition), Is.True);
            Assert.That(store.ExportGlobals().Values[definition.Id], Is.EqualTo("true"));
            Assert.That(store.Revision, Is.EqualTo(revision));
        });
    }
}
