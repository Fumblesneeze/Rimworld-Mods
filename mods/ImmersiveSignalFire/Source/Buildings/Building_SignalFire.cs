using ImmersiveSignalFire.Interactions;
using Verse;

namespace ImmersiveSignalFire.Buildings;

public sealed class Building_SignalFire : Building
{
    public CompSignalFire SignalComp => GetComp<CompSignalFire>();

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        NoPawnSignalFireMenuPatch.EnsureInstalled();
    }
}
