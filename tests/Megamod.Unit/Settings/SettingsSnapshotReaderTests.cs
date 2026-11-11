using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsSnapshotReaderTests
{
    [Test]
    public void DetachedEffectiveReadsApplyTheStartupChoiceWithoutChangingRequestedSnapshots()
    {
        var definition = ReadableNumbersSettings.Enabled;
        var globals = new GlobalSettingsReadSnapshot(new(new Dictionary<string, string> { [definition.Id] = "true" }), 12);
        var saved = new SaveSettingsSnapshot("detached", new Dictionary<string, SaveSettingValue>
            { [definition.Id] = new(false, "true") });
        var projection = new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
            { [definition] = "original-active" });

        var results = Task.Run(() => new[]
        {
            SettingsSnapshotReader.ReadEffective(definition, globals, projection, saved),
            SettingsSnapshotReader.ReadEffective(definition, globals, projection),
            SettingsSnapshotReader.ReadRequested(definition, globals, saved)
        }).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(results[0].Value, Is.False, "The detached save consumer must use the same effective-off choice.");
            Assert.That(results[1].Value, Is.False);
            Assert.That(results[2].Value, Is.True);
            Assert.That(globals.Values.Values[definition.Id], Is.EqualTo("true"));
            Assert.That(globals.GlobalRevision, Is.EqualTo(12));
            Assert.That(saved.Values[definition.Id], Is.EqualTo(new SaveSettingValue(false, "true")));
        });
    }

    [Test]
    public void DetachedWorkerReadsUseOnlyTheCapturedGlobalAndSaveValues()
    {
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        var globals = new GlobalSettingsReadSnapshot(new(new Dictionary<string, string> { [definition.Id] = "9" }), 12);
        var saved = new SaveSettingsSnapshot("detached", new Dictionary<string, SaveSettingValue>
            { [definition.Id] = new(false, "7") });
        var missing = new SaveSettingsSnapshot("missing", new Dictionary<string, SaveSettingValue>
            { [definition.Id] = new(false, null) });

        var results = Task.Run(() => new[]
        {
            SettingsSnapshotReader.ReadRequested(definition, globals, saved),
            SettingsSnapshotReader.ReadRequested(definition, globals),
            SettingsSnapshotReader.ReadRequested(definition, globals, missing)
        }).Result;

        Assert.Multiple(() =>
        {
            Assert.That(results[0].Value, Is.EqualTo(7));
            Assert.That(results[1].Value, Is.EqualTo(9));
            Assert.That(results[2].Value, Is.EqualTo(4));
            Assert.That(results[2].ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
            Assert.That(globals.GlobalRevision, Is.EqualTo(12));
            Assert.That(missing.Values[definition.Id].Override, Is.Null);
        });
    }
}
