using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_TablewareStack : CompProperties
{
    public CompProperties_TablewareStack() => compClass = typeof(CompTablewareStack);
}

// A view is neither spawned nor held and never owns physical units. It supplies
// the native Stuff/quality/sanitation contract to graphics and stat workers.
internal sealed class TablewareUnitView : ThingWithComps { }

public sealed class CompTablewareStack : ThingComp
{
    private TablewareStackLedger? ledger;
    private List<string>? savedMaterials;
    private List<int>? savedHitPoints;
    private bool applyingDamage;
    private readonly Dictionary<string, TablewareUnitView> views = new(StringComparer.Ordinal);

    internal static CompTablewareStack? For(Thing? thing) => thing is TablewareUnitView ? null :
        (thing as ThingWithComps)?.GetComp<CompTablewareStack>();

    public IReadOnlyList<TablewareUnitSnapshot> Units => Current.Units;

    private TablewareStackLedger Current
    {
        get
        {
            if (applyingDamage) return ledger!;
            if (ledger is not null && ledger.Units.Count == parent.stackCount &&
                (parent.stackCount == 0 || ledger.Units[0].HitPoints == parent.HitPoints)) return ledger;
            var units = ledger?.Units.ToList() ?? new List<TablewareUnitSnapshot>();
            // Generation and old saves may assign the final count after PostMake.
            // Native transfers restore exact snapshots in their postfix instead.
            if (units.Count > parent.stackCount) units.RemoveRange(parent.stackCount, units.Count - parent.stackCount);
            while (units.Count < parent.stackCount)
                units.Add(new TablewareUnitSnapshot(parent.Stuff?.defName, parent.HitPoints));
            if (units.Count > 0 && units[0].HitPoints != parent.HitPoints)
                units[0] = new TablewareUnitSnapshot(units[0].MaterialDefName, parent.HitPoints);
            ledger = new TablewareStackLedger(units);
            return ledger;
        }
    }

    internal TablewareStackLedger Snapshot() => new(Current.Units);

    internal void Restore(TablewareStackLedger snapshot)
    {
        ledger = snapshot;
        if (!parent.Destroyed && snapshot.Units.Count > 0)
        {
            var first = snapshot.Units[0];
            var stuff = ResolveMaterial(first);
            if (parent.Stuff != stuff)
            {
                parent.SetStuffDirect(stuff);
                foreach (var stat in DefDatabase<StatDef>.AllDefsListForReading)
                    stat.Worker.ClearCacheForThing(parent);
            }
            parent.HitPoints = first.HitPoints;
            StatDefOf.Mass.Worker.ClearCacheForThing(parent);
            StatDefOf.MarketValue.Worker.ClearCacheForThing(parent);
            StatDefOf.Flammability.Worker.ClearCacheForThing(parent);
            StatDefOf.DeteriorationRate.Worker.ClearCacheForThing(parent);
            parent.Notify_ColorChanged();
        }
    }

    internal ThingDef? ResolveMaterial(TablewareUnitSnapshot unit) => unit.MaterialDefName is null ? null :
        DefDatabase<ThingDef>.GetNamedSilentFail(unit.MaterialDefName) ?? parent.Stuff;

    internal Thing UnitView(int index)
    {
        var unit = Units[index];
        var key = unit.MaterialDefName ?? "";
        if (!views.TryGetValue(key, out var view))
        {
            view = new TablewareUnitView { def = parent.def, stackCount = 1 };
            view.SetStuffDirect(ResolveMaterial(unit));
            view.InitializeComps();
            views.Add(key, view);
        }
        view.HitPoints = unit.HitPoints;
        if (parent.TryGetQuality(out var quality))
            view.GetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Colony);
        parent.GetComp<CompSanitation>()?.PostSplitOff(view);
        return view;
    }

    internal int CountWhere(Func<TablewareUnitSnapshot, bool> predicate) => Current.CountWhere(predicate);

    internal float SumStat(StatDef stat, int count, bool applyPostProcess = true)
    {
        var index = 0;
        return (float)Current.SumFirst(Math.Min(count, parent.stackCount), _ =>
            stat.Worker.GetValue(StatRequest.For(UnitView(index++)), applyPostProcess));
    }

    internal bool Prioritize(Func<TablewareUnitSnapshot, bool> predicate, int count)
    {
        var current = Current;
        if (count > current.CountWhere(predicate)) return false;
        current.Prioritize(predicate, count);
        Restore(current);
        return true;
    }

    public override string TransformLabel(string label)
    {
        if (parent is TablewareUnitView || Units.Select(u => u.MaterialDefName).Distinct().Take(2).Count() < 2)
            return label;
        return parent.def.LabelCap + (parent.TryGetQuality(out var quality) ? " (" + quality.GetLabel() + ")" : "");
    }

    public override string CompInspectStringExtra()
    {
        if (parent is TablewareUnitView) return "";
        var groups = Units.GroupBy(u => u.MaterialDefName).ToArray();
        return groups.Length < 2 ? "" : string.Join(", ", groups.Select(g =>
            (ResolveMaterial(g.First())?.LabelCap.ToString() ?? parent.def.LabelCap.ToString()) + " ×" + g.Count()));
    }

    public override void PostExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            var units = Units;
            savedMaterials = units.Select(u => u.MaterialDefName ?? "").ToList();
            savedHitPoints = units.Select(u => u.HitPoints).ToList();
        }
        Scribe_Collections.Look(ref savedMaterials, "tablewareUnitMaterials", LookMode.Value);
        Scribe_Collections.Look(ref savedHitPoints, "tablewareUnitHitPoints", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (savedMaterials is not null && savedHitPoints is not null && savedMaterials.Count == savedHitPoints.Count)
                ledger = new TablewareStackLedger(savedMaterials.Select((m, i) =>
                    new TablewareUnitSnapshot(m.Length == 0 ? null : m, savedHitPoints[i])));
            Restore(Current);
            savedMaterials = null;
            savedHitPoints = null;
        }
    }

    internal int BeginNativeDamage()
    {
        var units = Current.Units;
        var maximum = units.Max(unit => unit.HitPoints);
        // Core destroys a whole Thing when its exposed HP reaches zero. Let the
        // strongest remaining unit set that threshold during the worker call;
        // the ledger stays authoritative and is restored before returning.
        applyingDamage = true;
        parent.HitPoints = maximum;
        return maximum;
    }

    internal void FinishNativeDamage(int maximum)
    {
        applyingDamage = false;
        if (parent.Destroyed) return;
        var damage = Math.Max(0, maximum - parent.HitPoints);
        ledger!.ApplyDamage(damage);
        parent.stackCount = ledger.Units.Count;
        Restore(ledger);
        if (parent.Spawned) parent.Map.listerMergeables.Notify_ThingStackChanged(parent);
    }
}
