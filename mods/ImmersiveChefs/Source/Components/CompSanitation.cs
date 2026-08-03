using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_Sanitation : CompProperties
{
    public CompProperties_Sanitation()
    {
        compClass = typeof(CompSanitation);
    }

    public bool selfCleaning;
}

public sealed class CompSanitation : ThingComp
{
    private bool dirty;
    private WashProvenance washProvenance;
    private int schemaVersion = SanitationStateModel.CurrentSchemaVersion;

    public bool IsDirty => dirty;

    public WashProvenance WashProvenance => washProvenance;

    public bool WashedInWildWater => washProvenance == WashProvenance.WildWater;

    public bool SelfCleaning => ((CompProperties_Sanitation)props).selfCleaning ||
                                parent.def.GetModExtension<KitchenwareExtension>()?.selfCleaning == true;

    public void MarkDirty()
    {
        if (SelfCleaning)
        {
            dirty = false;
            washProvenance = WashProvenance.Safe;
        }
        else
        {
            dirty = true;
        }
        NotifyStorageStateChanged();
    }

    public void MarkClean(WashProvenance provenance = WashProvenance.Safe)
    {
        dirty = false;
        washProvenance = provenance;
        NotifyStorageStateChanged();
    }

    public override bool AllowStackWith(Thing other)
    {
        var otherComp = (other as ThingWithComps)?.GetComp<CompSanitation>();
        return otherComp is not null &&
               dirty == otherComp.dirty &&
               washProvenance == otherComp.washProvenance;
    }

    public override void PostSplitOff(Thing piece)
    {
        base.PostSplitOff(piece);
        if ((piece as ThingWithComps)?.GetComp<CompSanitation>() is not { } splitSanitation)
        {
            return;
        }

        splitSanitation.dirty = dirty;
        splitSanitation.washProvenance = washProvenance;
        splitSanitation.schemaVersion = schemaVersion;
    }

    public override string CompInspectStringExtra()
    {
        if (SelfCleaning)
        {
            return "Cleanliness: self-cleaning";
        }

        var provenance = washProvenance == WashProvenance.WildWater ? " (wild-water washed)" : string.Empty;
        return $"Cleanliness: {(dirty ? "dirty" : "clean")}{provenance}";
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref schemaVersion, "schemaVersion", SanitationStateModel.CurrentSchemaVersion);
        Scribe_Values.Look(ref dirty, "dirty", false);
        Scribe_Values.Look(ref washProvenance, "washProvenance", WashProvenance.None);
        if (SelfCleaning)
        {
            dirty = false;
            washProvenance = WashProvenance.Safe;
        }
    }

    private void NotifyStorageStateChanged()
    {
        if (parent.Spawned && parent.Map is { } map)
        {
            map.listerHaulables.Notify_AddedThing(parent);
        }
    }
}
