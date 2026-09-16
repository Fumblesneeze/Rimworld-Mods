namespace ImmersiveChefs;

public readonly struct TablewareUnitSnapshot
{
    public TablewareUnitSnapshot(string? materialDefName, int hitPoints)
    {
        MaterialDefName = materialDefName;
        HitPoints = hitPoints;
    }

    public string? MaterialDefName { get; }
    public int HitPoints { get; }
}

// Quality and sanitation remain native homogeneous stack boundaries. Only the
// properties which may differ within a tableware stack live in this ledger.
public sealed class TablewareStackLedger
{
    private readonly List<TablewareUnitSnapshot> units;

    public TablewareStackLedger(IEnumerable<TablewareUnitSnapshot> units)
    {
        this.units = new List<TablewareUnitSnapshot>(units);
    }

    public IReadOnlyList<TablewareUnitSnapshot> Units => units;

    public int CountWhere(Func<TablewareUnitSnapshot, bool> predicate) => units.Count(predicate);

    public double SumFirst(int count, Func<TablewareUnitSnapshot, double> value)
    {
        if (count < 0 || count > units.Count) throw new ArgumentOutOfRangeException(nameof(count));
        return units.Take(count).Sum(value);
    }

    public void ApplyDamage(int damage)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        for (var i = units.Count - 1; i >= 0; i--)
        {
            var unit = units[i];
            if (unit.HitPoints <= damage) units.RemoveAt(i);
            else units[i] = new TablewareUnitSnapshot(unit.MaterialDefName, unit.HitPoints - damage);
        }
    }

    public void Prioritize(Func<TablewareUnitSnapshot, bool> predicate, int count)
    {
        if (count < 0 || count > CountWhere(predicate)) throw new ArgumentOutOfRangeException(nameof(count));
        var selected = new List<TablewareUnitSnapshot>();
        var remainder = new List<TablewareUnitSnapshot>();
        foreach (var unit in units)
        {
            if (selected.Count < count && predicate(unit)) selected.Add(unit);
            else remainder.Add(unit);
        }
        units.Clear();
        units.AddRange(selected);
        units.AddRange(remainder);
    }

    public int Absorb(TablewareStackLedger other, int stackLimit)
    {
        if (ReferenceEquals(this, other)) return 0;
        var count = Math.Min(other.units.Count, Math.Max(0, stackLimit - units.Count));
        units.AddRange(other.units.Take(count));
        other.units.RemoveRange(0, count);
        return count;
    }

    public TablewareStackLedger Take(int count)
    {
        if (count < 0 || count > units.Count) throw new ArgumentOutOfRangeException(nameof(count));
        var result = new TablewareStackLedger(units.Take(count));
        units.RemoveRange(0, count);
        return result;
    }
}
