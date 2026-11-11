using Megamod.Features.MessagePlacement;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.MessagePlacement;

[TestFixture]
public sealed class MessagePlacementSettingsTests
{
    [Test]
    public void OrdinaryDisableAndOriginalEffectiveOffPreserveOffsetsAndRequestedEnabledPreference()
    {
        var store = NewStore();
        store.SetGlobal(Position, new(10, 20));
        store.SetGlobal(MessagePlacementSettings.Enabled, false);
        Assert.That(store.GetEffective(MessagePlacementSettings.Enabled), Is.False);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(10, 20)));
        store.SetGlobal(MessagePlacementSettings.Enabled, true);
        var before = store.ExportGlobals();
        var suppressed = new SettingsStore(new SettingsEffectiveOff(MessagePlacementProviderPolicy.Reasons(new[] { "MLIE.BETTERMESSAGEPLACEMENT" })));
        MessagePlacementSettings.Register(suppressed);
        suppressed.LoadGlobals(before);
        Assert.That(suppressed.GetEffective(MessagePlacementSettings.Enabled), Is.False);
        Assert.That(suppressed.ReadCurrentRequested(MessagePlacementSettings.Enabled).Value, Is.True);
        Assert.That(suppressed.GetEffective(Position), Is.EqualTo(new MessageOffsets(10, 20)));
        Assert.That(suppressed.ExportGlobals().Values, Is.EqualTo(before.Values));
        Assert.That(MessagePlacementProviderPolicy.StartupWarnings(new[] { "MLIE.BETTERMESSAGEPLACEMENT" }), Has.Count.EqualTo(1));
        Assert.That(MessagePlacementProviderPolicy.Reasons(new[] { "unrelated.mod" }), Is.Empty);
    }
    [Test]
    public void InheritanceSaveSwitchingAndDormantPairsUseTheExistingCommonStore()
    {
        var store = NewStore();
        store.SetGlobal(Position, new(10, 20));
        var first = store.ActivateSave(new("save-a"));
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(10, 20)));
        Assert.That(store.SetSaveOverride(first, Position, new(30, 40)).Succeeded, Is.False);
        store.SetUseGlobal(first, Position, false);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(10, 20)));
        store.SetSaveOverride(first, Position, new(30, 40));
        store.SetUseGlobal(first, Position, true);
        store.SetGlobal(Position, new(-100, 100));
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(-100, 100)));
        var snapshot = store.ExportSave(first);
        store.ActivateSave(new("save-b"));
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(-100, 100)));
        Assert.That(store.SetSaveOverride(first, Position, default).Succeeded, Is.False);
        var restored = store.ActivateSave(snapshot);
        store.SetUseGlobal(restored, Position, false);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(30, 40)));
        Assert.That(store.GetGlobal(Position), Is.EqualTo(new MessageOffsets(-100, 100)));
        store.DeactivateSave();
        Assert.That(store.SetUseGlobal(restored, Position, true).Succeeded, Is.False);
        Assert.That(store.SetSaveOverride(restored, Position, default).Succeeded, Is.False);
    }

    [Test]
    public void ScopeResetAndRejectedSaveEditsPreserveOtherScopeAndInheritance()
    {
        var store = NewStore();
        store.SetGlobal(Position, new(10, 20));
        var binding = store.ActivateSave(new("save-a"));
        store.SetUseGlobal(binding, Position, false);
        store.SetSaveOverride(binding, Position, new(30, 40));
        Assert.That(store.SetSaveOverride(binding, Position, new(99, double.NaN)).Succeeded, Is.False);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(30, 40)));
        store.SetGlobal(Position, default);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(30, 40)));
        store.SetGlobal(Position, new(50, 60));
        store.SetSaveOverride(binding, Position, default);
        Assert.That(store.UsesGlobal(Position), Is.False);
        Assert.That(store.GetEffective(Position), Is.EqualTo(default(MessageOffsets)));
        Assert.That(store.GetGlobal(Position), Is.EqualTo(new MessageOffsets(50, 60)));
    }

    [Test]
    public void MalformedGlobalPairDoesNotReplaceAValidSaveOverride()
    {
        var store = NewStore();
        const string malformed = "{\"HorizontalPercent\":5}";
        store.LoadGlobals(new(new Dictionary<string, string> { [Position.Id] = malformed }));
        Assert.That(store.ReadGlobal(Position).Value, Is.EqualTo(default(MessageOffsets)));
        Assert.That(store.ReadGlobal(Position).ErrorKey, Is.Not.Null);
        var binding = store.ActivateSave(new("save-a"));
        store.SetUseGlobal(binding, Position, false);
        store.SetSaveOverride(binding, Position, new(-20, 40));
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(-20, 40)));
        Assert.That(store.ExportGlobals().Values[Position.Id], Is.EqualTo(malformed));
    }
    [TestCase("{\"HorizontalPercent\":10}")]
    [TestCase("{\"VerticalPercent\":20}")]
    [TestCase("{\"HorizontalPercent\":10,\"HorizontalPercent\":30,\"VerticalPercent\":20}")]
    [TestCase("{\"HorizontalPercent\":10,\"VerticalPercent\":20,\"Other\":1}")]
    [TestCase("{\"HorizontalPercent\":101,\"VerticalPercent\":20}")]
    [TestCase("{\"HorizontalPercent\":10,\"VerticalPercent\":\"20\"}")]
    [TestCase("{\"HorizontalPercent\":1e400,\"VerticalPercent\":20}")]
    [TestCase("null")]
    [TestCase("bad json")]
    public void MalformedSavePairFallsBackTogetherWithoutRewritingEitherScope(string encoded)
    {
        var store = NewStore();
        store.SetGlobal(Position, new(15, 25));
        var binding = store.ActivateSave(new("save-a", new Dictionary<string, SaveSettingValue>
            { [Position.Id] = new(false, encoded) }));
        var value = store.ReadEffective(Position);
        Assert.That(value.Value, Is.EqualTo(default(MessageOffsets)));
        Assert.That(value.ErrorKey, Is.Not.Null.And.Not.Empty);
        Assert.That(store.GetGlobal(Position), Is.EqualTo(new MessageOffsets(15, 25)));
        Assert.That(store.ExportSave(binding).Values[Position.Id], Is.EqualTo(new SaveSettingValue(false, encoded)));
    }
    [TestCase(101, 20)]
    [TestCase(20, -101)]
    [TestCase(double.NaN, 0)]
    [TestCase(0, double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity, 0)]
    public void InvalidEditPreservesTheEntirePreviousPairAndRevision(double x, double y)
    {
        var store = NewStore();
        store.SetGlobal(Position, new(12, 34));
        var before = store.Revision;
        var rejected = store.SetGlobal(Position, new(x, y));
        Assert.That(rejected.Succeeded, Is.False);
        Assert.That(rejected.ErrorKey, Is.Not.Null.And.Not.Empty);
        Assert.That(store.GetEffective(Position), Is.EqualTo(new MessageOffsets(12, 34)));
        Assert.That(store.Revision, Is.EqualTo(before));
    }
    private static SettingDefinition<MessageOffsets> Position => MessagePlacementSettings.PositionOffset;
    private static SettingsStore NewStore()
    {
        var store = new SettingsStore();
        MessagePlacementSettings.Register(store);
        return store;
    }

    [Test]
    public void EqualAndOppositeCoordinatesRoundTripAsOneCommonPreference()
    {
        var store = NewStore();
        Assert.That(store.GetEffective(Position), Is.EqualTo(default(MessageOffsets)));
        Assert.That(store.SetGlobal(Position, new(12.5, -12.5)).Succeeded, Is.True);
        var restored = NewStore();
        restored.LoadGlobals(store.ExportGlobals());
        Assert.That(restored.GetEffective(Position), Is.EqualTo(new MessageOffsets(12.5, -12.5)));
    }
}
