using System.Globalization;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsRowObservationTests
{
    [Test]
    public void RegisteredRowKeepsGlobalSaveAndEffectiveDisplaysDistinctAndHasNoSaveValueAtMainMenu()
    {
        var definition = new SettingDefinition<bool>("owned.enabled", false, SettingScope.GlobalAndSave,
            "interface.navigation", 0, SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
        var store = new SettingsStore(new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
            { [definition] = "original-active" }));
        store.Register(definition);
        store.ActivateSave(new("save-A", new Dictionary<string, SaveSettingValue>
            { [definition.Id] = new(false, "true") }));
        var revision = store.Revision;

        SettingsRowObservation Observe() => SettingsRowObservation.Capture(store, definition,
            "Owned option", "Readable help", "Navigation", value => value ? "On" : "Off");

        var active = Observe();
        Assert.Multiple(() =>
        {
            Assert.That(active.GlobalRequested.Text, Is.EqualTo("Off"));
            Assert.That(active.CurrentSaveRequested?.Text, Is.EqualTo("On"));
            Assert.That(active.Effective.Text, Is.EqualTo("Off"));
            Assert.That(active.UsesGlobal, Is.False);
            Assert.That(active.EffectiveOffReason, Is.EqualTo("original-active"));
            Assert.That(active.Revision, Is.EqualTo(revision));
            Assert.That(store.Revision, Is.EqualTo(revision));
        });

        store.DeactivateSave();
        var mainMenu = Observe();
        Assert.Multiple(() =>
        {
            Assert.That(mainMenu.CurrentSaveRequested, Is.Null);
            Assert.That(mainMenu.UsesGlobal, Is.Null);
            Assert.That(mainMenu.GlobalRequested.Text, Is.EqualTo("Off"));
            Assert.That(mainMenu.Effective.Text, Is.EqualTo("Off"));
        });
    }

    [Test]
    public void AFormatterChangingTheStoreRejectsTheCaptureAndTheNextCaptureUsesCurrentValues()
    {
        var definition = new SettingDefinition<int>("owned.count", 3, SettingScope.GlobalAndSave,
            "interface.navigation", 0, SettingApplicationTiming.NextDisplay, SettingCodecs.Int32);
        var store = new SettingsStore();
        store.Register(definition);
        bool changed = false;
        SettingsRowObservation? captured = null;

        Assert.Throws<InvalidOperationException>(() => captured = SettingsRowObservation.Capture(store, definition,
            "Count", "Readable help", "Navigation", value =>
            {
                if (!changed)
                {
                    changed = true;
                    Assert.That(store.SetGlobal(definition, 9).Succeeded, Is.True);
                }
                return value.ToString(CultureInfo.InvariantCulture);
            }));
        Assert.That(captured, Is.Null, "A mixed observation must never be published.");
        Assert.That(store.GetGlobal(definition), Is.EqualTo(9), "Rejecting the observation must not undo the caller's edit.");

        var current = SettingsRowObservation.Capture(store, definition, "Count", "Readable help", "Navigation",
            value => value.ToString(CultureInfo.InvariantCulture));
        Assert.That(current.GlobalRequested.Text, Is.EqualTo("9"));
        Assert.That(current.Effective.Text, Is.EqualTo("9"));
        Assert.That(current.Revision, Is.EqualTo(store.Revision));
    }

    [Test]
    public void RestartScopedGlobalRowReportsPolicyValuesAndTimingWithoutInventingSaveState()
    {
        var definition = new SettingDefinition<bool>("owned.restart", true, SettingScope.GlobalOnly,
            "interface.navigation", 0, SettingApplicationTiming.Restart, SettingCodecs.Boolean);
        var store = new SettingsStore();
        store.Register(definition);
        store.ActivateSave(new("save-A", new Dictionary<string, SaveSettingValue>
            { [definition.Id] = new(false, "false") }));
        SettingsRowObservation Observe() => SettingsRowObservation.Capture(store, definition,
            "Restart feature", "Readable help", "Navigation", value => value ? "On" : "Off");

        var before = Observe();
        Assert.Multiple(() =>
        {
            Assert.That(before.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.Restart));
            Assert.That(before.GlobalRequested.Text, Is.EqualTo("On"));
            Assert.That(before.Effective.Text, Is.EqualTo("On"));
            Assert.That(before.CurrentSaveRequested, Is.Null);
            Assert.That(before.UsesGlobal, Is.Null);
        });

        Assert.That(store.SetGlobal(definition, false).Succeeded, Is.True);
        var after = Observe();
        Assert.Multiple(() =>
        {
            Assert.That(after.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.Restart));
            Assert.That(after.GlobalRequested.Text, Is.EqualTo("Off"));
            Assert.That(after.Effective.Text, Is.EqualTo("Off"),
                "This is the current common policy projection; no native runtime application is claimed.");
            Assert.That(after.CurrentSaveRequested, Is.Null);
            Assert.That(after.UsesGlobal, Is.Null);
            Assert.That(before.Effective.Text, Is.EqualTo("On"), "The earlier detached observation must remain immutable.");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompositeDisplayRequiresAnExplicitFormatterAndNeverUsesPersistenceOrToString(bool formatted)
    {
        var codec = new CompositeCodec();
        var definition = new SettingDefinition<CompositeValue>("owned.composite", new(4, 9), SettingScope.GlobalAndSave,
            "interface.navigation", 0, SettingApplicationTiming.NextDisplay, codec);
        var store = new SettingsStore();
        store.Register(definition);
        Func<CompositeValue, string?>? display = formatted ? value => $"{value.Left} / {value.Right}" : null;

        var observed = SettingsRowObservation.Capture(store, definition, "Composite", "Readable help", "Navigation", display);

        Assert.That(observed.GlobalRequested.Text, Is.EqualTo(formatted ? "4 / 9" : null));
        Assert.That(observed.Effective.Text, Is.EqualTo(formatted ? "4 / 9" : null));
        Assert.That(observed.CurrentSaveRequested, Is.Null);
        Assert.That(codec.EncodingCalls, Is.Zero);
    }

    private sealed record CompositeValue(int Left, int Right)
    {
        public override string ToString() => throw new InvalidOperationException("Arbitrary ToString is not a display formatter.");
    }

    private sealed class CompositeCodec : ISettingCodec<CompositeValue>
    {
        public int EncodingCalls { get; private set; }
        public string Encode(CompositeValue value) { EncodingCalls++; return "persistence-only"; }
        public bool TryDecode(string encoded, out CompositeValue value) { value = new(4, 9); return encoded == "persistence-only"; }
    }
}
