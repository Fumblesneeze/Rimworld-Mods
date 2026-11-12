using Megamod.Features.DrugPolicyMaintenance;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Megamod.Defs.Tests.DrugPolicyMaintenance;

[TestFixture, NonParallelizable]
public sealed class NativeDrugPresetBaselineTests
{
    [Test]
    public void BaselineContainsCurrentDrugIdentitiesRatherThanOldNamesOrNonDrugs()
    {
        var current = Drug("Megamod_CurrentDrug");
        var obsolete = Drug(current.defName);
        var noLongerDrug = Drug("Megamod_NoLongerDrug");
        noLongerDrug.ingestible.drugCategory = DrugCategory.None;
        using var definitions = new ThingDefScope(current, noLongerDrug);

        var read = NativeDrugCatalog.CaptureInitial(() => true).ForPreset(() => true);

        Assert.Multiple(() =>
        {
            Assert.That(read.Contains(current), Is.True);
            Assert.That(read.Contains(obsolete), Is.False);
            Assert.That(read.Contains(noLongerDrug), Is.False);
            Assert.That(read.Contains(null), Is.False);
            Assert.That(read.IsCurrent, Is.True);
        });
    }

    [Test]
    public void MalformedAvailableDrugRejectsTheCompleteBaseline()
    {
        var supported = Drug("Megamod_SupportedDrug");
        var malformed = Drug("Megamod_MalformedDrug");
        malformed.comps.Clear();
        using var definitions = new ThingDefScope(supported, malformed);

        var failure = Assert.Throws<InvalidOperationException>(() =>
            NativeDrugCatalog.CaptureInitial(() => true));

        Assert.That(failure!.Message, Does.Contain("ThingDef 'Megamod_MalformedDrug'"));
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.EqualTo(new[] { supported, malformed }));
    }

    [Test]
    public void CapturedMembershipStaysDetachedWhileTheOwnersReceiptCanExpire()
    {
        var initial = Drug("Megamod_ReplacedDrug");
        bool current = true;
        PresetEntriesRead read;
        using (var definitions = new ThingDefScope(initial))
            read = NativeDrugCatalog.CaptureInitial(() => current).ForPreset(() => true);
        current = false;
        var replacement = Drug(initial.defName);
        using var nextDefinitions = new ThingDefScope(replacement);

        Assert.Multiple(() =>
        {
            Assert.That(read.Contains(initial), Is.True);
            Assert.That(read.Contains(replacement), Is.False);
            Assert.That(read.IsCurrent, Is.False);
        });
    }

    private static ThingDef Drug(string name) => new()
    {
        defName = name,
        category = ThingCategory.Item,
        ingestible = new IngestibleProperties { drugCategory = DrugCategory.Social, joy = 0.2f },
        comps = new List<CompProperties> { new CompProperties_Drug { addictiveness = 0.4f, listOrder = 1f } }
    };

    private sealed class ThingDefScope : IDisposable
    {
        public ThingDefScope(params ThingDef[] definitions)
        {
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty,
                "This fixture must exclusively own an initially empty ThingDef database.");
            try { DefDatabase<ThingDef>.Add(definitions); }
            catch { DefDatabase<ThingDef>.Clear(); throw; }
        }

        public void Dispose() => DefDatabase<ThingDef>.Clear();
    }
}
