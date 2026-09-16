using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class TablewareStackLedgerTests
{
    [Test]
    public void Partial_transfer_is_valued_from_the_transferred_units_not_the_whole_pile_average()
    {
        var pile = new TablewareStackLedger(new[]
        {
            new TablewareUnitSnapshot("Steel", 80), new TablewareUnitSnapshot("Steel", 80),
            new TablewareUnitSnapshot("Gold", 40), new TablewareUnitSnapshot("Gold", 40),
            new TablewareUnitSnapshot("Gold", 40), new TablewareUnitSnapshot("Gold", 40)
        });
        double Price(TablewareUnitSnapshot unit) => unit.MaterialDefName == "Steel" ? 8 : 28;
        Assert.That(pile.SumFirst(3, Price), Is.EqualTo(44));
        var moved = pile.Take(3);
        Assert.That(moved.SumFirst(3, Price) + pile.SumFirst(3, Price), Is.EqualTo(128));
        Assert.That(pile.SumFirst(3, Price), Is.EqualTo(84));
    }

    [Test]
    public void Damaging_a_pile_affects_hidden_units_and_destroys_broken_units()
    {
        var pile = new TablewareStackLedger(new[]
        {
            new TablewareUnitSnapshot("Steel", 80),
            new TablewareUnitSnapshot("Gold", 4),
            new TablewareUnitSnapshot("WoodLog", 33)
        });
        pile.ApplyDamage(6);
        var split = pile.Take(1);
        Assert.That(split.Units.Single().HitPoints, Is.EqualTo(74));
        Assert.That(pile.Units.Select(u => (u.MaterialDefName, u.HitPoints)),
            Is.EqualTo(new[] { ("WoodLog", 27) }));
    }

    [Test]
    public void Selecting_precious_plates_leaves_wooden_units_for_other_meals()
    {
        var pile = new TablewareStackLedger(new[]
        {
            new TablewareUnitSnapshot("WoodLog", 33),
            new TablewareUnitSnapshot("Gold", 46),
            new TablewareUnitSnapshot("WoodLog", 29),
            new TablewareUnitSnapshot("Silver", 55)
        });
        bool Eligible(TablewareUnitSnapshot unit) => unit.MaterialDefName is "Gold" or "Silver";

        Assert.That(pile.CountWhere(Eligible), Is.EqualTo(2));
        pile.Prioritize(Eligible, 2);
        var selected = pile.Take(2);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Units.Select(x => (x.MaterialDefName, x.HitPoints)),
                Is.EqualTo(new[] { ("Gold", 46), ("Silver", 55) }));
            Assert.That(pile.Units.Select(x => (x.MaterialDefName, x.HitPoints)),
                Is.EqualTo(new[] { ("WoodLog", 33), ("WoodLog", 29) }));
        });
    }

    [Test]
    public void Partial_merge_then_split_conserves_each_material_and_durability()
    {
        var steel = new TablewareStackLedger(new[]
        {
            new TablewareUnitSnapshot("Steel", 80),
            new TablewareUnitSnapshot("Steel", 72)
        });
        var gold = new TablewareStackLedger(new[]
        {
            new TablewareUnitSnapshot("Gold", 46),
            new TablewareUnitSnapshot("Gold", 39)
        });

        Assert.That(steel.Absorb(gold, stackLimit: 3), Is.EqualTo(1));
        var carried = steel.Take(2);

        Assert.Multiple(() =>
        {
            Assert.That(carried.Units.Select(x => (x.MaterialDefName, x.HitPoints)),
                Is.EqualTo(new[] { ("Steel", 80), ("Steel", 72) }));
            Assert.That(steel.Units.Select(x => (x.MaterialDefName, x.HitPoints)),
                Is.EqualTo(new[] { ("Gold", 46) }));
            Assert.That(gold.Units.Select(x => (x.MaterialDefName, x.HitPoints)),
                Is.EqualTo(new[] { ("Gold", 39) }));
        });
    }
}
