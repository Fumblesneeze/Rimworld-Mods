using RimWorld;
using UnityEngine;
using Verse;

namespace ImmersiveChefs;

internal enum CountertopSurfaceKind
{
    None,
    Eat,
    Item
}

internal readonly struct MicrowaveSupportFacts
{
    internal MicrowaveSupportFacts(
        bool spawned,
        bool completedBuilding,
        CountertopSurfaceKind surface,
        bool storage)
    {
        Spawned = spawned;
        CompletedBuilding = completedBuilding;
        Surface = surface;
        Storage = storage;
    }

    internal bool Spawned { get; }
    internal bool CompletedBuilding { get; }
    internal CountertopSurfaceKind Surface { get; }
    internal bool Storage { get; }
}

internal readonly struct CountertopCell : IEquatable<CountertopCell>
{
    internal CountertopCell(int x, int z)
    {
        X = x;
        Z = z;
    }

    internal int X { get; }
    internal int Z { get; }

    public bool Equals(CountertopCell other) => X == other.X && Z == other.Z;

    public override bool Equals(object? obj) => obj is CountertopCell other && Equals(other);

    public override int GetHashCode() => (X * 397) ^ Z;
}

internal static class MicrowaveSupportPolicy
{
    internal static bool Allows(MicrowaveSupportFacts facts) =>
        facts.Spawned &&
        facts.CompletedBuilding &&
        !facts.Storage &&
        facts.Surface is CountertopSurfaceKind.Eat or CountertopSurfaceKind.Item;

    internal static bool InteractionCellIsUsable(
        IEnumerable<CountertopCell> supportCells,
        CountertopCell? supportInteractionCell,
        CountertopCell interactionCell,
        bool inBounds,
        bool standable) =>
        inBounds &&
        standable &&
        !supportCells.Contains(interactionCell) &&
        (!supportInteractionCell.HasValue || !supportInteractionCell.Value.Equals(interactionCell));
}

internal static class MicrowaveOperationalPolicy
{
    internal static bool Allows(
        bool spawned,
        bool forbidden,
        bool powered,
        bool brokenDown,
        bool supported) =>
        spawned &&
        !forbidden &&
        powered &&
        !brokenDown &&
        supported;
}

internal static class MicrowaveRecoveryPolicy
{
    internal static IEnumerable<CountertopCell> OrderedCells(
        CountertopCell origin,
        int width,
        int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return Enumerable.Range(0, width)
            .SelectMany(x => Enumerable.Range(0, height).Select(z => new CountertopCell(x, z)))
            .OrderBy(cell => DistanceSquared(origin, cell))
            .ThenBy(cell => cell.Z)
            .ThenBy(cell => cell.X);
    }

    private static int DistanceSquared(CountertopCell origin, CountertopCell candidate)
    {
        var x = candidate.X - origin.X;
        var z = candidate.Z - origin.Z;
        return (x * x) + (z * z);
    }
}

internal static class MicrowaveSupportRuntime
{
    internal static Building? FindAt(IntVec3 cell, Map map, Thing? thingToIgnore = null)
    {
        return cell.GetThingList(map)
            .Where(thing => !ReferenceEquals(thing, thingToIgnore))
            .OfType<Building>()
            .FirstOrDefault(IsValid);
    }

    internal static bool IsValid(Thing thing)
    {
        var surface = thing.def.surfaceType switch
        {
            SurfaceType.Eat => CountertopSurfaceKind.Eat,
            SurfaceType.Item => CountertopSurfaceKind.Item,
            _ => CountertopSurfaceKind.None
        };
        var storage = thing.def.thingClass is { } thingClass &&
            typeof(ISlotGroupParent).IsAssignableFrom(thingClass);
        return MicrowaveSupportPolicy.Allows(new MicrowaveSupportFacts(
            thing.Spawned,
            thing is Building,
            surface,
            storage));
    }
}

public sealed class PlaceWorker_MicrowaveCountertop : PlaceWorker
{
    private const string MissingSupport =
        "Requires a completed table or workbench with an eating or item surface.";
    private const string BlockedInteraction =
        "Rotate or move the microwave so its interaction cell is beside the supporting surface.";

    public override AcceptanceReport AllowsPlacing(
        BuildableDef checkingDef,
        IntVec3 loc,
        Rot4 rot,
        Map map,
        Thing? thingToIgnore = null,
        Thing? thing = null)
    {
        var support = MicrowaveSupportRuntime.FindAt(loc, map, thingToIgnore);
        if (support is null)
        {
            return MissingSupport;
        }

        if (checkingDef is not ThingDef thingDef || !thingDef.hasInteractionCell)
        {
            return AcceptanceReport.WasAccepted;
        }

        var interactionCell = loc + thingDef.interactionCellOffset.RotatedBy(rot);
        var supportCells = support.OccupiedRect().Cells
            .Select(cell => new CountertopCell(cell.x, cell.z))
            .ToArray();
        CountertopCell? supportInteractionCell = support.def.hasInteractionCell
            ? new CountertopCell(support.InteractionCell.x, support.InteractionCell.z)
            : null;
        if (!MicrowaveSupportPolicy.InteractionCellIsUsable(
                supportCells,
                supportInteractionCell,
                new CountertopCell(interactionCell.x, interactionCell.z),
                interactionCell.InBounds(map),
                interactionCell.InBounds(map) && interactionCell.Standable(map)))
        {
            return BlockedInteraction;
        }

        return AcceptanceReport.WasAccepted;
    }
}

public sealed class Building_Microwave : Building
{
    public override void TickRare()
    {
        base.TickRare();
        if (!Spawned || MicrowaveSupportRuntime.FindAt(Position, Map, this) is not null)
        {
            return;
        }

        RecoverFromMissingSupport();
    }

    private void RecoverFromMissingSupport()
    {
        var map = Map;
        var position = Position;
        var wasSelected = Find.Selector.IsSelected(this);
        var minified = this.MakeMinified();
        if (minified is null)
        {
            Log.Error("[ImmersiveChefs] Countertop microwave lost its support but could not be minified.");
            return;
        }

        if (!GenPlace.TryPlaceThing(minified, position, map, ThingPlaceMode.Near, squareRadius: 4) &&
            !TryPlaceAtNearestStandableCell(minified, position, map))
        {
            Log.Error("[ImmersiveChefs] Countertop microwave was minified after support loss but no standable map cell accepted it.");
            return;
        }

        if (wasSelected)
        {
            Find.Selector.Select(minified, playSound: false, forceDesignatorDeselect: false);
        }
    }

    private static bool TryPlaceAtNearestStandableCell(
        MinifiedThing minified,
        IntVec3 origin,
        Map map)
    {
        var orderedCells = MicrowaveRecoveryPolicy.OrderedCells(
            new CountertopCell(origin.x, origin.z),
            map.Size.x,
            map.Size.z);
        foreach (var candidate in orderedCells)
        {
            var cell = new IntVec3(candidate.X, 0, candidate.Z);
            if (cell.Standable(map) &&
                GenPlace.TryPlaceThing(minified, cell, map, ThingPlaceMode.Direct))
            {
                return true;
            }
        }

        return false;
    }
}
