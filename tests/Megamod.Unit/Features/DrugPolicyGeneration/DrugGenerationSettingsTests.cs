using System.Collections.Generic;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyGeneration;

[TestFixture]
public sealed class DrugGenerationSettingsTests
{
    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("-Infinity")]
    [TestCase("1.1")]
    [TestCase("-0.1")]
    [TestCase("0,5")]
    [TestCase("future:value")]
    public void InvalidStoredThresholdRetainsPayloadAndReportsFallback(string encoded)
    {
        var store = NewStore();
        store.LoadGlobals(new(new Dictionary<string, string> { [DrugGenerationSettings.InitialMood.Id] = encoded }));
        var binding = store.ActivateSave(new("colony", new Dictionary<string, SaveSettingValue>
            { [DrugGenerationSettings.InitialJoy.Id] = new(false, encoded) }));
        Assert.Multiple(() =>
        {
            Assert.That(store.ReadGlobal(DrugGenerationSettings.InitialMood).ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(store.ReadEffective(DrugGenerationSettings.InitialJoy).ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(DrugGenerationSettings.Capture(store).InitialMood, Is.EqualTo(1));
            Assert.That(DrugGenerationSettings.Capture(store).InitialJoy, Is.EqualTo(1));
            Assert.That(store.ExportGlobals().Values[DrugGenerationSettings.InitialMood.Id], Is.EqualTo(encoded));
            Assert.That(store.ExportSave(binding).Values[DrugGenerationSettings.InitialJoy.Id].Override, Is.EqualTo(encoded));
        });
    }

    [TestCase("1:limit:NaN")]
    [TestCase("1:limit:1.01")]
    [TestCase("future:unknown")]
    public void UnknownStoredToleranceFallsBackToAnyWithoutErasingItsEncoding(string encoded)
    {
        var store = NewStore();
        store.LoadGlobals(new(new Dictionary<string, string> { [DrugGenerationSettings.InitialTolerance.Id] = encoded }));
        Assert.Multiple(() =>
        {
            Assert.That(store.ReadGlobal(DrugGenerationSettings.InitialTolerance).ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(DrugGenerationSettings.Capture(store).InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(store.ExportGlobals().Values[DrugGenerationSettings.InitialTolerance.Id], Is.EqualTo(encoded));
        });
    }

    [Test]
    [SetCulture("fr-FR")]
    public void ExportAndReloadPreservePercentageBoundariesAndAnyVersusNumericOne()
    {
        var store = NewStore();
        store.SetGlobal(DrugGenerationSettings.InitialMood, 0f);
        store.SetGlobal(DrugGenerationSettings.InitialJoy, .12345679f);
        store.SetGlobal(DrugGenerationSettings.InitialTolerance, DrugToleranceChoice.Any);
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, DrugGenerationSettings.InitialTolerance, false);
        store.SetSaveOverride(binding, DrugGenerationSettings.InitialTolerance, DrugToleranceChoice.Numeric(1f));
        var restored = NewStore();
        restored.LoadGlobals(store.ExportGlobals());
        var restoredBinding = restored.ActivateSave(store.ExportSave(binding));
        var numeric = DrugGenerationSettings.Capture(restored);
        restored.SetUseGlobal(restoredBinding, DrugGenerationSettings.InitialTolerance, true);
        var any = DrugGenerationSettings.Capture(restored);
        restored.SetUseGlobal(restoredBinding, DrugGenerationSettings.InitialTolerance, false);
        Assert.Multiple(() =>
        {
            Assert.That(numeric.InitialMood, Is.Zero);
            Assert.That(numeric.InitialJoy, Is.EqualTo(.12345679f));
            Assert.That(numeric.InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(numeric.InitialTolerance.Limit, Is.EqualTo(1f));
            Assert.That(any.InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(DrugGenerationSettings.Capture(restored).InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
        });
    }

    [TestCase(-.01f)]
    [TestCase(1.01f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void InvalidThresholdEditsPreservePreferencesAndRevision(float invalid)
    {
        var store = NewStore();
        store.SetGlobal(DrugGenerationSettings.InitialMood, .3f);
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, DrugGenerationSettings.InitialJoy, false);
        store.SetSaveOverride(binding, DrugGenerationSettings.InitialJoy, .6f);
        var revision = store.Revision;
        var global = store.SetGlobal(DrugGenerationSettings.InitialMood, invalid);
        var saved = store.SetSaveOverride(binding, DrugGenerationSettings.InitialJoy, invalid);
        Assert.Multiple(() =>
        {
            Assert.That(global.Succeeded, Is.False);
            Assert.That(saved.Succeeded, Is.False);
            Assert.That(global.ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(saved.ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(DrugGenerationSettings.Capture(store).InitialMood, Is.EqualTo(.3f));
            Assert.That(DrugGenerationSettings.Capture(store).InitialJoy, Is.EqualTo(.6f));
            Assert.That(store.Revision, Is.EqualTo(revision));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidToleranceEditPreservesExistingNumericPreference(bool nullChoice)
    {
        var store = NewStore();
        store.SetGlobal(DrugGenerationSettings.InitialTolerance, DrugToleranceChoice.Numeric(1));
        var revision = store.Revision;
        var result = store.SetGlobal(DrugGenerationSettings.InitialTolerance,
            nullChoice ? null! : DrugToleranceChoice.Decode("future:unknown"));
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorKey, Is.Not.Null.And.Not.Empty);
            Assert.That(DrugGenerationSettings.Capture(store).InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(DrugGenerationSettings.Capture(store).InitialTolerance.Limit, Is.EqualTo(1));
            Assert.That(store.Revision, Is.EqualTo(revision));
        });
    }

    [Test]
    public void CapturedPreviewPreferencesFollowCurrentScopeWithoutChangingEarlierCapture()
    {
        var store = NewStore();
        var original = DrugGenerationSettings.Capture(store);
        store.SetGlobal(DrugGenerationSettings.InitialMood, .35f);
        store.SetGlobal(DrugGenerationSettings.InitialJoy, .6f);
        store.SetGlobal(DrugGenerationSettings.InitialTolerance, DrugToleranceChoice.Numeric(1));
        var binding = store.ActivateSave(new("colony"));
        store.SetUseGlobal(binding, DrugGenerationSettings.InitialMood, false);
        store.SetSaveOverride(binding, DrugGenerationSettings.InitialMood, .15f);
        var current = DrugGenerationSettings.Capture(store);
        store.SetGlobal(DrugGenerationSettings.InitialMood, .9f);
        store.SetGlobal(DrugGenerationSettings.InitialJoy, .8f);
        var refreshed = DrugGenerationSettings.Capture(store);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetEffective(DrugGenerationSettings.Enabled), Is.True);
            Assert.That(original.InitialMood, Is.EqualTo(1));
            Assert.That(original.InitialJoy, Is.EqualTo(1));
            Assert.That(original.InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(current.InitialMood, Is.EqualTo(.15f));
            Assert.That(current.InitialJoy, Is.EqualTo(.6f));
            Assert.That(current.InitialTolerance.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(current.InitialTolerance.Limit, Is.EqualTo(1));
            Assert.That(refreshed.InitialMood, Is.EqualTo(.15f));
            Assert.That(refreshed.InitialJoy, Is.EqualTo(.8f));
        });
    }

    private static SettingsStore NewStore()
    {
        var store = new SettingsStore();
        DrugGenerationSettings.Register(store);
        return store;
    }
}
