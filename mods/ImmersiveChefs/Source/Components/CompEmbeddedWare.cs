using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_EmbeddedWare : CompProperties
{
    public CompProperties_EmbeddedWare()
    {
        compClass = typeof(CompEmbeddedWare);
    }
}

public sealed class CompEmbeddedWare : ThingComp, IThingHolder
{
    private ThingOwner<Thing>? embeddedPlates;
    private int lastFireDamageTick = -1;
    private bool platingOpportunityFailed;
    private readonly IngestionLifecycleState ingestionLifecycle = new();

    private ThingOwner<Thing> EmbeddedPlates =>
        embeddedPlates ??= new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);

    public int EmbeddedPlateCount => EmbeddedPlates.InnerListForReading.Sum(plate => plate.stackCount);

    public bool PlatingOpportunityFailed => platingOpportunityFailed;

    public IReadOnlyList<PlateBinding> Bindings => EmbeddedPlates.InnerListForReading
        .SelectMany(plate => Enumerable.Repeat(Snapshot(plate), plate.stackCount))
        .ToList()
        .AsReadOnly();

    public bool TryEmbedPlate(Thing plate)
    {
        if (plate.Destroyed || plate.stackCount <= 0)
        {
            return false;
        }

        bool embedded;
        if (plate.holdingOwner is { } sourceOwner)
        {
            embedded = sourceOwner.TryTransferToContainer(
                plate,
                EmbeddedPlates,
                1,
                canMergeWithExistingStacks: false) == 1;
            if (embedded && EmbeddedPlateCount >= parent.stackCount)
            {
                platingOpportunityFailed = false;
            }

            return embedded;
        }

        var single = plate.stackCount > 1 ? plate.SplitOff(1) : plate;
        if (single.Spawned)
        {
            single.DeSpawn(DestroyMode.Vanish);
        }

        embedded = EmbeddedPlates.TryAdd(single, canMergeWithExistingStacks: false);
        if (embedded && EmbeddedPlateCount >= parent.stackCount)
        {
            platingOpportunityFailed = false;
        }

        return embedded;
    }

    public void RecordFailedPlatingOpportunity()
    {
        platingOpportunityFailed = true;
    }

    public Thing? ReleasePlateThing()
    {
        var plate = EmbeddedPlates.InnerListForReading.LastOrDefault();
        if (plate is null)
        {
            return null;
        }

        if (plate.stackCount > 1)
        {
            return plate.SplitOff(1);
        }

        EmbeddedPlates.Remove(plate);
        return plate;
    }

    public Thing? PeekPlateThing() => EmbeddedPlates.InnerListForReading.LastOrDefault();

    public PlateBinding? PeekOne() => PeekPlateThing() is { } plate ? Snapshot(plate) : null;

    public override void PrePostIngested(Pawn ingester)
    {
        ingestionLifecycle.Begin();
        DiningSessionRegistry.BeginIngestion(ingester);
    }

    public override void PostIngested(Pawn ingester)
    {
        try
        {
            var plate = ReleasePlateThing();
            if (plate is not null)
            {
                if (!ingester.RaceProps.Humanlike)
                {
                    ReturnUnusedPlate(plate, ingester);
                    return;
                }

                var captured = DiningSessionRegistry.CapturePlate(ingester, plate);
                (plate as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
                if (!captured)
                {
                    PlacePlate(plate, ingester.PositionHeld, ingester.MapHeld);
                }
            }
        }
        finally
        {
            ingestionLifecycle.End();
        }
    }

    internal void AbortIngestion()
    {
        ingestionLifecycle.Abort();
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (dinfo.Def?.defName.Equals("Flame", StringComparison.OrdinalIgnoreCase) == true)
        {
            lastFireDamageTick = Find.TickManager?.TicksGame ?? 0;
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        if (!ingestionLifecycle.ShouldRecoverEmbeddedWareOnDestroy ||
            !EmbeddedPlates.Any || previousMap is null)
        {
            return;
        }

        var currentTick = Find.TickManager?.TicksGame ?? 0;
        var causedByFire = lastFireDamageTick == currentTick;
        var position = parent.Position;
        foreach (var plate in EmbeddedPlates.InnerListForReading.ToList())
        {
            EmbeddedPlates.Remove(plate);
            if (causedByFire && plate.FlammableNow)
            {
                plate.Destroy(DestroyMode.KillFinalize);
                continue;
            }

            (plate as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            PlacePlate(plate, position, previousMap);
        }
    }

    public override void PostSplitOff(Thing piece)
    {
        if (ReferenceEquals(piece, parent))
        {
            return;
        }

        var target = (piece as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        if (target is null)
        {
            return;
        }

        target.platingOpportunityFailed = platingOpportunityFailed;
        TransferTo(target, Math.Min(piece.stackCount, EmbeddedPlateCount));
    }

    public override void PreAbsorbStack(Thing otherStack, int count)
    {
        var source = (otherStack as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        platingOpportunityFailed |= source?.platingOpportunityFailed == true;
        source?.TransferTo(this, Math.Min(count, source.EmbeddedPlateCount));
    }

    public override bool AllowStackWith(Thing other)
    {
        var otherComp = (other as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        return otherComp is not null &&
               platingOpportunityFailed == otherComp.platingOpportunityFailed;
    }

    public override string CompInspectStringExtra() =>
        EmbeddedPlateCount == 0 ? "Service ware: unplated" : $"Bound plates: {EmbeddedPlateCount}";

    public override void PostExposeData()
    {
        Scribe_Deep.Look(ref embeddedPlates, "embeddedPlates", this);
        Scribe_Values.Look(ref lastFireDamageTick, "lastFireDamageTick", -1);
        Scribe_Values.Look(ref platingOpportunityFailed, "platingOpportunityFailed", false);
        embeddedPlates ??= new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);
    }

    public ThingOwner GetDirectlyHeldThings() => EmbeddedPlates;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, EmbeddedPlates);
    }

    IThingHolder? IThingHolder.ParentHolder => parent.ParentHolder;

    private void TransferTo(CompEmbeddedWare target, int count)
    {
        var remaining = count;
        foreach (var plate in EmbeddedPlates.InnerListForReading.ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            var transferred = EmbeddedPlates.TryTransferToContainer(
                plate,
                target.EmbeddedPlates,
                Math.Min(remaining, plate.stackCount),
                canMergeWithExistingStacks: false);
            remaining -= transferred;
        }
    }

    private static PlateBinding Snapshot(Thing plate)
    {
        var quality = QualityUtility.TryGetQuality(plate, out var foundQuality)
            ? foundQuality
            : QualityCategory.Normal;
        return new PlateBinding(
            plate.def.defName,
            plate.Stuff?.defName,
            (int)quality,
            plate.HitPoints,
            (plate as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
            (plate as ThingWithComps)?.GetComp<CompSanitation>()?.WashProvenance ?? WashProvenance.None);
    }

    private static void PlacePlate(Thing plate, IntVec3 position, Map? map)
    {
        if (map is null)
        {
            plate.Destroy(DestroyMode.Vanish);
            return;
        }

        GenPlace.TryPlaceThing(plate, position, map, ThingPlaceMode.Near);
    }

    private static void ReturnUnusedPlate(Thing plate, Pawn ingester)
    {
        if (CaravanUtility.GetCaravan(ingester) is { } caravan)
        {
            var destination = ingester.inventory?.innerContainer ??
                              caravan.PawnsListForReading
                                  .Select(pawn => pawn.inventory?.innerContainer)
                                  .FirstOrDefault(container => container is not null);
            if (destination?.TryAdd(plate, canMergeWithExistingStacks: false) == true)
            {
                caravan.RecacheInventory();
                return;
            }

            Log.Error("[ImmersiveChefs] Could not return an animal-excluded meal's plate to caravan inventory.");
            return;
        }

        PlacePlate(plate, ingester.PositionHeld, ingester.MapHeld);
    }
}
