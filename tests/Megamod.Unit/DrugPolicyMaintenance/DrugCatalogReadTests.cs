using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.DrugPolicyMaintenance;

[TestFixture]
public sealed class DrugCatalogReadTests
{
    [Test]
    public void PresetAvailabilityAddsItsOwnConditionWithoutRevokingSharedCatalog()
    {
        var drug = new object();
        bool definitionsCurrent = true;
        bool presetCurrent = false;
        var read = new DrugCatalogRead(new[] { new DrugCatalogEntry(drug, false, true) }, () => definitionsCurrent);
        var preset = read.ForPreset(() => presetCurrent);
        Assert.That(preset.IsCurrent, Is.False);
        Assert.That(read.IsCurrent, Is.True);
        presetCurrent = true;
        Assert.That(preset.IsCurrent, Is.True);
        definitionsCurrent = false;
        Assert.Multiple(() =>
        {
            Assert.That(read.IsCurrent, Is.False);
            Assert.That(preset.IsCurrent, Is.False);
            Assert.That(preset.Contains(drug), Is.True, "Expiry revokes authority, not detached identity data.");
            Assert.That(preset.Contains(new object()), Is.False);
        });
    }

    [Test]
    public void PublicationRetainsItsOwnOrderedIdentitiesAndFlagsWhenInputChanges()
    {
        var first = new object();
        var second = new object();
        var source = new List<DrugCatalogEntry> { new(first, true, false), new(second, false, true) };
        var read = new DrugCatalogRead(source, () => true);

        source[0] = new(second, true, true);
        source.Reverse();
        source.Clear();

        Assert.That(read.Entries, Is.EqualTo(new[]
        {
            new DrugCatalogEntry(first, true, false), new DrugCatalogEntry(second, false, true)
        }));
        Assert.That(read.IsCurrent, Is.True);
        Assert.Throws<NotSupportedException>(() => ((IList<DrugCatalogEntry>)read.Entries).Clear());
    }
}
