using System.Collections.Generic;
using System.Linq;
using ImmersiveSignalFire.Buildings;
using ImmersiveSignalFire.Interactions;
using RimWorld;
using Verse;

namespace ImmersiveSignalFire.Signals;

public sealed class MapComponent_SignalResponses : MapComponent
{
    private List<ScheduledSignalResponse> scheduled = new();
    private Building_SignalFire? pendingNoPawnMenuFire;

    public MapComponent_SignalResponses(Map map) : base(map)
    {
    }

    public void Schedule(Faction? faction, int dueTick)
    {
        if (faction is not null)
        {
            scheduled.Add(new ScheduledSignalResponse(faction, dueTick));
        }
    }

    public void QueueNoPawnMenu(Building_SignalFire fire)
    {
        pendingNoPawnMenuFire = fire;
    }

    public void FlushNoPawnMenu()
    {
        Building_SignalFire? fire = pendingNoPawnMenuFire;
        if (fire is null)
        {
            return;
        }

        pendingNoPawnMenuFire = null;
        if (fire.Spawned && fire.Map == map && !fire.SignalComp.IsBusy)
        {
            SignalFireMenu.OpenNoPawnTopLevel(fire);
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (scheduled.Count == 0)
        {
            return;
        }

        int now = Find.TickManager.TicksGame;
        foreach (ScheduledSignalResponse response in scheduled.Where(response => response.DueTick <= now).ToList())
        {
            NativeMilitaryAid.TryCall(map, response.Faction);
            scheduled.Remove(response);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref scheduled, "immersiveSignalFireResponses", LookMode.Deep);
        scheduled ??= new List<ScheduledSignalResponse>();
    }
}

public sealed class ScheduledSignalResponse : IExposable
{
    private Faction? faction;
    private int dueTick;

    public ScheduledSignalResponse()
    {
    }

    public ScheduledSignalResponse(Faction faction, int dueTick)
    {
        this.faction = faction;
        this.dueTick = dueTick;
    }

    public Faction? Faction => faction;
    public int DueTick => dueTick;

    public void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref dueTick, "dueTick");
    }
}
