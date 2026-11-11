using Megamod.Features.Diagnostics;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class DiagnosticOptionsTests
{
    [Test]
    public void EffectiveOffAppliesBeforeTheFirstDiagnosticSnapshotAndAfterPreferenceEdits()
    {
        var definition = DiagnosticSettings.DefinitionContext;
        var store = new SettingsStore(new(new Dictionary<SettingDefinition<bool>, string>
        { [definition] = "original-active" }));
        DiagnosticSettings.Register(store);
        store.LoadGlobals(new(new Dictionary<string, string> { [definition.Id] = "true" }));
        var options = new DiagnosticOptionsSource(store);
        var initial = options.Current;
        store.SetGlobal(definition, false);
        var manuallyOff = options.Current;
        store.SetGlobal(definition, true);
        var observed = Task.Run(() => options.Current).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(initial.DefinitionContextEnabled, Is.False, "The first loader snapshot must use the effective choice.");
            Assert.That(observed.DefinitionContextEnabled, Is.False);
            Assert.That(manuallyOff.DefinitionContextEnabled, Is.False);
            Assert.That(store.GetGlobal(definition), Is.True);
            Assert.That(store.ExportGlobals().Values[definition.Id], Is.EqualTo("true"));
        });
    }

    [Test]
    public void GlobalEditsPublishNewOptionsWithoutChangingPreviouslyReadSnapshots()
    {
        var store = new SettingsStore();
        DiagnosticSettings.Register(store);
        var options = new DiagnosticOptionsSource(store);
        DiagnosticOptions previous = options.Current;

        Assert.That(store.SetGlobal(DiagnosticSettings.DefinitionContext, false).Succeeded, Is.True);
        DiagnosticOptions observed = Task.Run(() => options.Current).GetAwaiter().GetResult();

        Assert.That(observed.DefinitionContextEnabled, Is.False);
        Assert.That(previous.DefinitionContextEnabled, Is.True);
        Assert.That(store.SetGlobal(DiagnosticSettings.DefinitionContext, true).Succeeded, Is.True);
        Assert.That(options.Current.DefinitionContextEnabled, Is.True);
        Assert.That(observed.DefinitionContextEnabled, Is.False);
    }

    [Test]
    public void InitialDiagnosticSnapshotUsesTheLoadedGlobalChoice()
    {
        var store = new SettingsStore();
        DiagnosticSettings.Register(store);
        Assert.That(store.GetGlobal(DiagnosticSettings.DefinitionContext), Is.True);
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string>
        {
            [DiagnosticSettings.DefinitionContext.Id] = "false"
        }));

        var options = new DiagnosticOptionsSource(store);

        Assert.That(options.Current.DefinitionContextEnabled, Is.False);
    }
}
