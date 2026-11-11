using Megamod.Features.MessagePlacement;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.MessagePlacement;

[TestFixture]
public sealed class MessagePlacementPreviewTests
{
    private static readonly MessageBounds[] Samples = { new(140, 16, 260, 26), new(140, 42, 300, 26) };

    [Test]
    public void IndependentCoordinatesAndViewportChangesRecalculateTheDetachedPreview()
    {
        var store = NewStore();
        store.SetGlobal(MessagePlacementSettings.PositionOffset, new(10, -10));
        var first = Capture(store);
        Assert.That(first.Messages[0], Is.EqualTo(new MessageBounds(240, 0, 260, 26)));
        store.SetGlobal(MessagePlacementSettings.PositionOffset, new(-10, 10));
        var changed = Capture(store);
        Assert.That(changed.Messages[0], Is.EqualTo(new MessageBounds(40, 96, 260, 26)));
        var resized = MessagePlacementPreview.Capture(store, Samples, 800, 600, true);
        Assert.That(resized.Messages[0], Is.EqualTo(new MessageBounds(60, 76, 260, 26)));
        Assert.That(first.Messages[0], Is.EqualTo(new MessageBounds(240, 0, 260, 26)));
        Assert.That(resized.StatusKey, Is.EqualTo("Megamod_Messages_BarIntegrationPending"));
        Assert.That(Samples[0], Is.EqualTo(new MessageBounds(140, 16, 260, 26)));
    }

    private static MessagePreviewSnapshot Capture(SettingsStore store) =>
        MessagePlacementPreview.Capture(store, Samples, 1000, 800, true);

    [Test]
    public void EffectiveScopeFollowsSaveOverridesInheritanceAndMenuWithoutWritingSettings()
    {
        var store = NewStore();
        store.SetGlobal(MessagePlacementSettings.PositionOffset, new(5, 10));
        var save = store.ActivateSave(new("save-a"));
        store.SetUseGlobal(save, MessagePlacementSettings.PositionOffset, false);
        store.SetSaveOverride(save, MessagePlacementSettings.PositionOffset, new(20, 30));
        var before = store.Revision;
        var overridden = Capture(store);
        Assert.That(overridden.ScopeKey, Is.EqualTo("Megamod_Messages_PreviewSave"));
        Assert.That(overridden.Messages[0], Is.EqualTo(new MessageBounds(340, 256, 260, 26)));
        Assert.That(store.Revision, Is.EqualTo(before));
        store.SetGlobal(MessagePlacementSettings.PositionOffset, new(10, 20));
        Assert.That(Capture(store).Messages, Is.EqualTo(overridden.Messages));
        store.SetUseGlobal(save, MessagePlacementSettings.PositionOffset, true);
        Assert.That(Capture(store).Messages[0], Is.EqualTo(new MessageBounds(240, 176, 260, 26)));
        store.DeactivateSave();
        var menu = Capture(store);
        Assert.That(menu.ScopeKey, Is.EqualTo("Megamod_Messages_PreviewGlobal"));
        Assert.That(menu.Messages[0], Is.EqualTo(new MessageBounds(240, 176, 260, 26)));
        store.ActivateSave(new("save-b"));
        Assert.That(Capture(store).ScopeKey, Is.EqualTo("Megamod_Messages_PreviewSave"));
    }

    private static SettingsStore NewStore()
    {
        var store = new SettingsStore();
        MessagePlacementSettings.Register(store);
        return store;
    }

    [Test]
    public void DisabledOrUnadmittedPlacementCannotPretendThatConfiguredMovementIsActive()
    {
        var store = NewStore();
        store.SetGlobal(MessagePlacementSettings.PositionOffset, new(10, 20));
        store.SetGlobal(MessagePlacementSettings.Enabled, false);
        var disabled = Capture(store);
        Assert.That(disabled.Messages, Is.Empty);
        Assert.That(disabled.StatusKey, Is.EqualTo("Megamod_Messages_PreviewDisabled"));
        store.SetGlobal(MessagePlacementSettings.Enabled, true);
        var unavailable = MessagePlacementPreview.Capture(store, Samples, 1000, 800, false);
        Assert.That(unavailable.Messages, Is.Empty);
        Assert.That(unavailable.StatusKey, Is.EqualTo("Megamod_Messages_PreviewUnavailable"));
        var suppressed = new SettingsStore(new SettingsEffectiveOff(MessagePlacementProviderPolicy.Reasons(new[] { "Mlie.BetterMessagePlacement" })));
        MessagePlacementSettings.Register(suppressed);
        suppressed.LoadGlobals(store.ExportGlobals());
        Assert.That(Capture(suppressed).StatusKey, Is.EqualTo(disabled.StatusKey));
        Assert.That(suppressed.ReadCurrentRequested(MessagePlacementSettings.Enabled).Value, Is.True);
        Assert.That(suppressed.GetEffective(MessagePlacementSettings.PositionOffset), Is.EqualTo(new MessageOffsets(10, 20)));
    }
}
