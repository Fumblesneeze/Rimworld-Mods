using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsEffectiveOffCompositionTests
{
    [Test]
    public void CombinedStartupReasonsRetainEachMasterAndStoredPreference()
    {
        var first = new SettingDefinition<bool>("first", true, SettingScope.GlobalOnly, "test", 0,
            SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
        var second = new SettingDefinition<bool>("second", true, SettingScope.GlobalOnly, "test", 1,
            SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
        var reasons = new Dictionary<SettingDefinition<bool>, string> { [first] = "first-reason" };
        var a = new SettingsEffectiveOff(reasons);
        var b = new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string> { [second] = "second-reason", [first] = "duplicate" });
        var store = new SettingsStore(SettingsEffectiveOff.Combine(a, b));
        reasons.Clear();
        store.Register(first); store.Register(second);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(first), Is.False);
            Assert.That(store.GetEffective(second), Is.False);
            Assert.That(store.GetGlobal(first), Is.True);
            Assert.That(store.GetGlobal(second), Is.True);
            Assert.That(store.EffectiveOffReason(first), Is.EqualTo("first-reason"));
            Assert.That(store.EffectiveOffReason(second), Is.EqualTo("second-reason"));
        });
    }
}
