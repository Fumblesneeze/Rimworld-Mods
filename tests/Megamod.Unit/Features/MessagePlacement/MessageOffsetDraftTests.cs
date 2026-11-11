using System.Globalization;
using Megamod.Features.MessagePlacement;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageOffsetDraftTests
{
    [Test]
    public void DisabledResetHasNoCommitOrDraftSideEffect()
    {
        var draft = new MessageOffsetDraft();
        draft.Edit("8", "16", CultureInfo.InvariantCulture);
        int commits = 0;
        Assert.That(draft.ResetValue(false, CultureInfo.InvariantCulture, _ => { commits++; return new(true); }).Succeeded, Is.False);
        Assert.That(commits, Is.Zero);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("8", "16")));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CommonStoreRejectsStaleEnabledCallbacksWithoutLosingTheDraft(bool reset)
    {
        var store = new SettingsStore(); MessagePlacementSettings.Register(store);
        var definition = MessagePlacementSettings.PositionOffset;
        var binding = store.ActivateSave(new("first"));
        store.SetUseGlobal(binding, definition, false);
        var draft = new MessageOffsetDraft(); var culture = CultureInfo.InvariantCulture;
        draft.Synchronize(default, culture); draft.Edit("8", "16", culture);
        store.ActivateSave(new("second"));
        SettingEditResult Commit(MessageOffsets value) => store.SetSaveOverride(binding, definition, value);
        var result = reset ? draft.ResetValue(true, culture, Commit) : draft.Apply(true, culture, Commit);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(draft.ErrorKey, Is.EqualTo("Megamod_Settings_NoActiveSave"));
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("8", "16")));
        Assert.That(store.GetEffective(definition), Is.EqualTo(default(MessageOffsets)));
    }

    [Test]
    public void CultureChangeReformatsTheStoredPairWithoutPublishingADraft()
    {
        var draft = new MessageOffsetDraft();
        draft.Synchronize(new(12.5, -12.5), CultureInfo.GetCultureInfo("en-US"));
        draft.Edit("99", "98", CultureInfo.GetCultureInfo("en-US"));
        draft.Synchronize(new(12.5, -12.5), CultureInfo.GetCultureInfo("de-DE"));
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("12,5", "-12,5")));
        Assert.That(draft.CanApply(CultureInfo.GetCultureInfo("de-DE")), Is.True);
    }
    [Test]
    public void StoredValueAndLifecycleChangesReplaceDraftsButOrdinaryRedrawDoesNot()
    {
        var draft = new MessageOffsetDraft(); var culture = CultureInfo.InvariantCulture;
        draft.Synchronize(new(4, 8), culture);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("4", "8")));
        draft.Edit("9", "bad", culture);
        draft.Synchronize(new(4, 8), culture);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("9", "bad")));
        Assert.That(draft.ErrorKey, Is.Not.Null);
        draft.Synchronize(new(5, 8), culture);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("5", "8")));
        Assert.That(draft.ErrorKey, Is.Null);
        draft.Edit("9", "bad", culture);
        draft.Reset(); draft.Synchronize(new(5, 8), culture);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("5", "8")));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResetUsesOnlyTheProvidedScopeAndLeavesInheritanceUnchanged(bool global)
    {
        var store = new SettingsStore(); MessagePlacementSettings.Register(store);
        var definition = MessagePlacementSettings.PositionOffset;
        store.SetGlobal(definition, new(4, 8));
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, definition, false);
        store.SetSaveOverride(binding, definition, new(12, 16));
        var draft = new MessageOffsetDraft();
        draft.Synchronize(global ? store.GetGlobal(definition) : store.GetEffective(definition), CultureInfo.InvariantCulture);
        var result = draft.ResetValue(true, CultureInfo.InvariantCulture, value =>
            global ? store.SetGlobal(definition, value) : store.SetSaveOverride(binding, definition, value));
        Assert.That(result.Succeeded, Is.True);
        Assert.That(store.GetGlobal(definition), Is.EqualTo(global ? default : new MessageOffsets(4, 8)));
        Assert.That(store.GetEffective(definition), Is.EqualTo(global ? new MessageOffsets(12, 16) : default));
        Assert.That(store.UsesGlobal(definition), Is.False);
        Assert.That((draft.Horizontal, draft.Vertical), Is.EqualTo(("0", "0")));
    }
    [TestCase(false, "12", "20", null)]
    [TestCase(true, "12", "", "Megamod_Messages_InvalidOffsetPair")]
    [TestCase(true, "12", "101", "Megamod_Messages_InvalidOffsetPair")]
    public void DisabledOrInvalidDraftCannotCallTheCommitDelegate(bool enabled, string x, string y, string? error)
    {
        var draft = new MessageOffsetDraft();
        draft.Edit(x, y, CultureInfo.InvariantCulture);
        int commits = 0;
        var result = draft.Apply(enabled, CultureInfo.InvariantCulture, _ => { commits++; return new(true); });
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorKey, Is.EqualTo(error));
        Assert.That(commits, Is.Zero);
        Assert.That(draft.Horizontal, Is.EqualTo(x));
        Assert.That(draft.Vertical, Is.EqualTo(y));
    }
    [Test]
    public void ValidCompletePairCommitsOnceThroughTheCommonStore()
    {
        var store = new SettingsStore(); MessagePlacementSettings.Register(store);
        var draft = new MessageOffsetDraft();
        var culture = CultureInfo.GetCultureInfo("de-DE");
        draft.Synchronize(default, culture);
        draft.Edit("12,5", "-12,5", culture);
        Assert.That(store.GetGlobal(MessagePlacementSettings.PositionOffset), Is.EqualTo(default(MessageOffsets)));
        int commits = 0;
        var result = draft.Apply(true, culture, value => { commits++; return store.SetGlobal(MessagePlacementSettings.PositionOffset, value); });
        Assert.That(result.Succeeded, Is.True);
        Assert.That(commits, Is.EqualTo(1));
        Assert.That(store.GetGlobal(MessagePlacementSettings.PositionOffset), Is.EqualTo(new MessageOffsets(12.5, -12.5)));
    }
}
