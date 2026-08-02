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
    private int schemaVersion = SanitationStateModel.CurrentSchemaVersion;

    public bool IsDirty => dirty;

    public bool SelfCleaning => ((CompProperties_Sanitation)props).selfCleaning ||
                                parent.def.GetModExtension<KitchenwareExtension>()?.selfCleaning == true;

    public void MarkDirty()
    {
        dirty = !SelfCleaning;
    }

    public void MarkClean()
    {
        dirty = false;
    }

    public override bool AllowStackWith(Thing other)
    {
        var otherComp = (other as ThingWithComps)?.GetComp<CompSanitation>();
        return otherComp is not null && dirty == otherComp.dirty;
    }

    public override string CompInspectStringExtra()
    {
        return SelfCleaning ? "Cleanliness: self-cleaning" : $"Cleanliness: {(dirty ? "dirty" : "clean")}";
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref schemaVersion, "schemaVersion", SanitationStateModel.CurrentSchemaVersion);
        Scribe_Values.Look(ref dirty, "dirty", false);
        if (SelfCleaning)
        {
            dirty = false;
        }
    }
}
