using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ImmersiveSignalFire.Contacts;

internal sealed class WorldContact
{
    public WorldContact(Faction faction, int nearestDistance)
    {
        Faction = faction;
        NearestDistance = nearestDistance;
    }

    public Faction Faction { get; }
    public int NearestDistance { get; }
}

internal static class WorldContactCatalog
{
    internal const int MaximumDistance = 10;

    public static IReadOnlyList<WorldContact> For(Map map)
    {
        if (map is null || Find.WorldObjects is null || Find.WorldGrid is null)
        {
            return Array.Empty<WorldContact>();
        }

        List<Settlement> settlements = Find.WorldObjects.Settlements;
        Faction[] factions = Find.FactionManager.AllFactionsListForReading
            .Where(faction => faction != Faction.OfPlayer)
            .ToArray();
        Dictionary<string, Faction> byId = factions.ToDictionary(
            faction => faction.loadID.ToString(CultureInfo.InvariantCulture),
            StringComparer.Ordinal);
        ContactCandidate[] candidates = factions
            .Select(faction => new ContactCandidate(
                faction.loadID.ToString(CultureInfo.InvariantCulture),
                faction.Name,
                (int)faction.def.techLevel,
                faction.PlayerRelationKind == FactionRelationKind.Ally,
                faction.defeated,
                settlements
                    .Where(settlement => settlement.Faction == faction && !settlement.Destroyed)
                    .Select(settlement => (int)Math.Ceiling(
                        Find.WorldGrid.ApproxDistanceInTiles(map.Tile, settlement.Tile)))))
            .ToArray();

        return ContactCatalog.Select(candidates, (int)TechLevel.Industrial, MaximumDistance)
            .Select(contact => new WorldContact(byId[contact.Id], contact.NearestDistance))
            .ToArray();
    }

    public static bool IsEligible(Map map, Faction faction) =>
        faction is not null && For(map).Any(contact => contact.Faction == faction);
}
