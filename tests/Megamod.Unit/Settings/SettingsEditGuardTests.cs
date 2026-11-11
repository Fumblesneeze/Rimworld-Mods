using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsEditGuardTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void AvailableFeatureCannotMakeAnEarlierSaveBindingWritable(bool hasReplacementSave)
    {
        var definition = new SettingDefinition<bool>("choice", true, SettingScope.GlobalAndSave,
            "content", 0, SettingApplicationTiming.NewGameOrLoad, SettingCodecs.Boolean);
        var store = new SettingsStore();
        store.Register(definition);
        var earlier = store.ActivateSave(new("A"));
        store.SetUseGlobal(earlier, definition, false);
        if (hasReplacementSave) store.ActivateSave(new("B"));
        else store.DeactivateSave();
        var before = store.Revision;
        var guard = new SettingsEditGuard(() => null);

        Assert.That(guard.Commit(() => store.SetSaveOverride(earlier, definition, false)),
            Is.EqualTo(new SettingEditResult(false, "Megamod_Settings_NoActiveSave")));
        Assert.That(store.Revision, Is.EqualTo(before));
        Assert.That(store.GetEffective(definition), Is.True);
    }

    [TestCase("global")]
    [TestCase("save")]
    [TestCase("inheritance")]
    public void EligibilityIsRecheckedAtEveryCommitWithoutChangingUnavailablePreferences(string path)
    {
        var definition = new SettingDefinition<bool>("choice", true, SettingScope.GlobalAndSave,
            "content", 0, SettingApplicationTiming.NewGameOrLoad, SettingCodecs.Boolean);
        var store = new SettingsStore();
        store.Register(definition);
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, definition, false);
        store.SetSaveOverride(binding, definition, false);
        string? reason = null;
        var guard = new SettingsEditGuard(() => reason);
        Assert.That(guard.UnavailableReasonKey, Is.Null, "The row was enabled when drawn.");
        var revision = store.Revision;
        var globals = store.ExportGlobals();
        var save = store.ExportSave(binding);
        int changes = 0;
        store.Changed += () => changes++;
        Func<SettingEditResult> commit = path switch
        {
            "global" => () => store.SetGlobal(definition, false),
            "save" => () => store.SetSaveOverride(binding, definition, true),
            _ => () => store.SetUseGlobal(binding, definition, true)
        };

        reason = "FeatureUnavailable";
        var rejected = guard.Commit(commit);

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.EqualTo(new SettingEditResult(false, "FeatureUnavailable")));
            Assert.That(store.Revision, Is.EqualTo(revision));
            Assert.That(store.ExportGlobals().Values, Is.EqualTo(globals.Values));
            Assert.That(store.ExportSave(binding).Values, Is.EqualTo(save.Values));
            Assert.That(changes, Is.Zero);
        });
        reason = null;
        Assert.That(guard.Commit(commit).Succeeded, Is.True);
        Assert.That(changes, Is.EqualTo(1));
    }
}
