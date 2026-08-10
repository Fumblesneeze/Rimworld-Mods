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
    private string? personalDiningOwnerThingId;
    private bool returnToMapAfterInterruptedSession;

    public bool IsDirty => dirty;

    public WashProvenance WashProvenance => washProvenance;

    public bool WashedInWildWater => washProvenance == WashProvenance.WildWater;

    public bool ReturnToMapAfterInterruptedSession => returnToMapAfterInterruptedSession;

    public bool SelfCleaning => ((CompProperties_Sanitation)props).selfCleaning ||
                                parent.def.GetModExtension<KitchenwareExtension>()?.selfCleaning == true;

    public bool IsPersonalDiningWareFor(Pawn pawn) =>
        pawn is not null && IsPersonalDiningWareFor(pawn.ThingID);

    public bool IsPersonalDiningWareFor(string? pawnThingId) =>
        !string.IsNullOrEmpty(personalDiningOwnerThingId) &&
        StringComparer.Ordinal.Equals(personalDiningOwnerThingId, pawnThingId);

    public void MarkPersonalDiningOwner(Pawn pawn)
    {
        MarkPersonalDiningOwner(pawn?.ThingID);
    }

    public void MarkPersonalDiningOwner(string? pawnThingId)
    {
        personalDiningOwnerThingId = pawnThingId;
        returnToMapAfterInterruptedSession = false;
    }

    public void ClearPersonalDiningOwner()
    {
        personalDiningOwnerThingId = null;
    }

    public void MarkSessionTransferredWare()
    {
        personalDiningOwnerThingId = null;
        returnToMapAfterInterruptedSession = true;
    }

    public void ClearSessionTransfer()
    {
        returnToMapAfterInterruptedSession = false;
    }

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
               washProvenance == otherComp.washProvenance &&
               returnToMapAfterInterruptedSession == otherComp.returnToMapAfterInterruptedSession &&
               StringComparer.Ordinal.Equals(
                   personalDiningOwnerThingId,
                   otherComp.personalDiningOwnerThingId);
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
        splitSanitation.personalDiningOwnerThingId = personalDiningOwnerThingId;
        splitSanitation.returnToMapAfterInterruptedSession = returnToMapAfterInterruptedSession;
    }

    public override string CompInspectStringExtra()
    {
        return SanitationInspectionText.For(dirty, SelfCleaning);
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref schemaVersion, "schemaVersion", SanitationStateModel.CurrentSchemaVersion);
        Scribe_Values.Look(ref dirty, "dirty", false);
        Scribe_Values.Look(ref washProvenance, "washProvenance", WashProvenance.None);
        Scribe_Values.Look(ref personalDiningOwnerThingId, "personalDiningOwnerThingId");
        Scribe_Values.Look(
            ref returnToMapAfterInterruptedSession,
            "returnToMapAfterInterruptedSession",
            false);
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

public static class SanitationInspectionText
{
    public static string For(bool isDirty, bool selfCleaning)
    {
        return TranslationKeyFor(isDirty, selfCleaning).Translate();
    }

    public static string TranslationKeyFor(bool isDirty, bool selfCleaning) => selfCleaning
        ? "ImmersiveChefs_Cleanliness_SelfCleaning"
        : isDirty
            ? "ImmersiveChefs_Cleanliness_Dirty"
            : "ImmersiveChefs_Cleanliness_Clean";
}
