using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsLifecycleEntryTests
{
    [Test]
    public void ChangedCanReadTheBindingWithoutReactivatingOrExposingTheUnpublishedCandidate()
    {
        var entry = new SettingsLifecycleEntry();
        var store = new SettingsStore();
        var snapshot = new SaveSettingsSnapshot("loaded");
        SaveSettingsBinding? binding = null;
        SaveSettingsBinding? callbackBinding = null;
        int notifications = 0;

        SaveSettingsBinding? ReadBinding()
        {
            entry.Run(() =>
            {
                if (binding == null) binding = store.ActivateSave(snapshot);
            });
            return binding;
        }
        store.Changed += () =>
        {
            // Bound the failing case; production must prevent this second activation entirely.
            if (++notifications > 1) throw new InvalidOperationException("Activation reentered from Changed.");
            callbackBinding = ReadBinding();
        };

        SaveSettingsBinding? result = null;
        Assert.DoesNotThrow(() => result = ReadBinding());
        Assert.Multiple(() =>
        {
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(callbackBinding, Is.Null);
            Assert.That(result, Is.Not.Null);
            Assert.That(ReadBinding(), Is.SameAs(result));
            Assert.That(store.Revision.Activation, Is.EqualTo(1));
            Assert.That(store.ExportSave(result!).SaveIdentity, Is.EqualTo("loaded"));
        });
    }
}
