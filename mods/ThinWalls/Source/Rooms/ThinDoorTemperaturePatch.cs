using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rooms;

[HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.EqualizeTemperaturesThroughBuilding))]
public static class ThinDoorTemperaturePatch
{
    [HarmonyPrefix]
    public static bool Prefix(Building b, float rate)
    {
        if (b is not Building_ThinDoor door)
        {
            return true;
        }

        EqualizeAcrossOwnedEdge(door, rate);
        return false;
    }

    private static void EqualizeAcrossOwnedEdge(Building_ThinDoor door, float rate)
    {
        OwnedEdge owned = door.OwnedEdge;
        IntVec3 first = owned.Cell;
        IntVec3 second = owned.OppositeCell;
        if (!first.InBounds(door.Map) || !second.InBounds(door.Map))
        {
            return;
        }

        Room firstRoom = first.GetRoom(door.Map);
        Room secondRoom = second.GetRoom(door.Map);
        if (firstRoom == null || secondRoom == null || ReferenceEquals(firstRoom, secondRoom))
        {
            return;
        }

        var rooms = new List<Room> { firstRoom, secondRoom };
        float mean = (firstRoom.Temperature + secondRoom.Temperature) * 0.5f;
        float limiter = 1f;
        foreach (Room room in rooms)
        {
            if (room.UsesOutdoorTemperature)
            {
                continue;
            }

            float delta = (mean - room.Temperature) * rate;
            if (Mathf.Approximately(delta, 0f))
            {
                continue;
            }

            float proposed = room.Temperature + delta / room.CellCount;
            if (delta > 0f && proposed > mean)
            {
                proposed = mean;
            }
            else if (delta < 0f && proposed < mean)
            {
                proposed = mean;
            }

            limiter = Mathf.Min(
                limiter,
                Mathf.Abs((proposed - room.Temperature) * room.CellCount / delta));
        }

        foreach (Room room in rooms)
        {
            if (room.UsesOutdoorTemperature)
            {
                continue;
            }

            float delta = mean - room.Temperature;
            float vacuumFactor = door.Map.Biome.inVacuum && delta < 0f ? 0.1f : 1f;
            room.Temperature += delta * rate * limiter * vacuumFactor / room.CellCount;
        }
    }
}
