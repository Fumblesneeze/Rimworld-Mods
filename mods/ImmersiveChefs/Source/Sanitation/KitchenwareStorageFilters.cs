using Verse;

namespace ImmersiveChefs;

public enum KitchenwareStorageFilter
{
    Clean,
    Dirty
}

public static class KitchenwareStoragePolicy
{
    public static bool Allows(KitchenwareStorageFilter filter, bool isDirty) =>
        filter == KitchenwareStorageFilter.Dirty ? isDirty : !isDirty;

    internal static bool IsReusableKitchenware(ThingDef def) =>
        def.GetModExtension<KitchenwareExtension>()?.product is
            KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;
}

public sealed class SpecialThingFilterWorker_CleanKitchenware : SpecialThingFilterWorker
{
    public override bool Matches(Thing thing) =>
        KitchenwareStoragePolicy.IsReusableKitchenware(thing.def) &&
        KitchenwareStoragePolicy.Allows(
            KitchenwareStorageFilter.Clean,
            (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true);

    public override bool CanEverMatch(ThingDef def) => KitchenwareStoragePolicy.IsReusableKitchenware(def);
}

public sealed class SpecialThingFilterWorker_DirtyKitchenware : SpecialThingFilterWorker
{
    public override bool Matches(Thing thing) =>
        KitchenwareStoragePolicy.IsReusableKitchenware(thing.def) &&
        KitchenwareStoragePolicy.Allows(
            KitchenwareStorageFilter.Dirty,
            (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true);

    public override bool CanEverMatch(ThingDef def) => KitchenwareStoragePolicy.IsReusableKitchenware(def);
}
