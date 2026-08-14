using System.IO.Compression;
using System.Xml;

namespace RealRuinsCorpus;

public sealed record PlacementMetric(int Total, int Matching);

public sealed record DiningTableObservation(string DefName, int AdjacentChairCount);

public sealed record WorkstationObservation(string DefName, bool TouchesWall, bool Roofed);

public sealed record OutdoorUtilityObservation(string DefName, bool Roofed);

public sealed record PlacedItemObservation(
    string DefName,
    int X,
    int Z,
    int Rotation,
    bool ActsAsWall,
    bool Roofed);

public sealed record BlueprintAnalysis(
    string SourceId,
    int Width,
    int Height,
    int SerializedCells,
    int RoofedCells,
    int WallCells,
    PlacementMetric DiningTablesWithEnoughAdjacentChairs,
    PlacementMetric PowerConduitsUnderWalls,
    PlacementMetric PowerConduitsAtOrAdjacentToWalls,
    PlacementMetric WorkstationsTouchingWalls,
    PlacementMetric FixedOutdoorUtilitiesRoofed,
    IReadOnlyList<DiningTableObservation> DiningTables,
    IReadOnlyList<WorkstationObservation> Workstations,
    IReadOnlyList<OutdoorUtilityObservation> OutdoorUtilities,
    IReadOnlyList<PlacedItemObservation> PlacedItems,
    IReadOnlyDictionary<string, int> ItemDefCounts);

public static class BlueprintAnalyzer
{
    private const long MaximumExpandedBytes = 64L * 1024 * 1024;
    private const int MaximumCells = 250_000;
    private const int MaximumItems = 1_000_000;

    private static readonly IReadOnlyDictionary<string, Footprint> DiningTables =
        new Dictionary<string, Footprint>(StringComparer.Ordinal)
        {
            ["Table1x2c"] = new(1, 2, MinimumSeats: 2),
            ["Table2x2c"] = new(2, 2, MinimumSeats: 4),
            ["Table2x4c"] = new(2, 4, MinimumSeats: 6),
            ["Table3x3c"] = new(3, 3, MinimumSeats: 8)
        };

    private static readonly HashSet<string> DiningChairs = new(StringComparer.Ordinal)
    {
        "DiningChair",
        "Stool"
    };

    private static readonly HashSet<string> ResidentialBeds = new(StringComparer.Ordinal)
    {
        "Bed",
        "Bedroll",
        "RoyalBed"
    };

    private static readonly HashSet<string> FoodStorage = new(StringComparer.Ordinal)
    {
        "Cooler",
        "PassiveCooler",
        "Refrigerator",
        "Fridge"
    };

    private static readonly HashSet<string> PowerConduits = new(StringComparer.Ordinal)
    {
        "PowerConduit",
        "PowerConduitInvisible"
    };

    private static readonly HashSet<string> FixedOutdoorUtilities = new(StringComparer.Ordinal)
    {
        "ChemfuelPoweredGenerator",
        "WaterTowerS"
    };

    private static readonly IReadOnlyDictionary<string, Footprint> WorkstationFootprints =
        new Dictionary<string, Footprint>(StringComparer.Ordinal)
        {
            ["ElectricStove"] = new(3, 1, 0),
            ["FueledStove"] = new(3, 1, 0),
            ["ButcherTable"] = new(3, 1, 0),
            ["ElectricSmithy"] = new(3, 1, 0),
            ["FueledSmithy"] = new(3, 1, 0),
            ["MachiningTable"] = new(3, 1, 0),
            ["TailoringBench"] = new(3, 1, 0),
            ["HandTailoringBench"] = new(3, 1, 0),
            ["DrugLab"] = new(3, 1, 0),
            ["SimpleResearchBench"] = new(3, 1, 0),
            ["HiTechResearchBench"] = new(3, 2, 0)
        };

    public static BlueprintAnalysis Analyze(Stream gzipBlueprint, string sourceId)
    {
        ArgumentNullException.ThrowIfNull(gzipBlueprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        using var gzip = new GZipStream(gzipBlueprint, CompressionMode.Decompress, leaveOpen: true);
        using var bounded = new BoundedReadStream(gzip, MaximumExpandedBytes);
        using var reader = XmlReader.Create(bounded, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumExpandedBytes,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        });

        var width = 0;
        var height = 0;
        var cells = new Dictionary<Cell, CellState>();
        Cell? currentCell = null;
        var cellDepth = -1;
        var itemCount = 0;
        while (reader.Read())
        {
            if (reader.Depth > 32)
            {
                throw new InvalidDataException("Blueprint XML exceeds the maximum depth of 32.");
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                RequireAttributeBudget(reader);
                if (reader.Depth == 0 && reader.LocalName == "snapshot")
                {
                    width = ReadBoundedInt(reader, "width", 1, 500);
                    height = ReadBoundedInt(reader, "height", 1, 500);
                }
                else if (reader.LocalName == "cell")
                {
                    if (cells.Count >= MaximumCells)
                    {
                        throw new InvalidDataException($"Blueprint exceeds the maximum of {MaximumCells} cells.");
                    }

                    currentCell = new Cell(
                        ReadBoundedInt(reader, "x", 0, 499),
                        ReadBoundedInt(reader, "z", 0, 499));
                    cellDepth = reader.Depth;
                    cells.Add(currentCell.Value, new CellState());
                    if (reader.IsEmptyElement)
                    {
                        currentCell = null;
                        cellDepth = -1;
                    }
                }
                else if (currentCell.HasValue && reader.Depth == cellDepth + 1 && reader.LocalName == "roof")
                {
                    cells[currentCell.Value].Roofed = true;
                }
                else if (currentCell.HasValue && reader.Depth == cellDepth + 1 && reader.LocalName == "item")
                {
                    itemCount++;
                    if (itemCount > MaximumItems)
                    {
                        throw new InvalidDataException($"Blueprint exceeds the maximum of {MaximumItems} items.");
                    }

                    var defName = ReadRequiredText(reader, "def", 256);
                    var rotation = ReadOptionalRotation(reader);
                    cells[currentCell.Value].Items.Add(new Item(
                        defName,
                        rotation,
                        reader.GetAttribute("actsAsWall") == "1"));
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement &&
                     currentCell.HasValue &&
                     reader.Depth == cellDepth &&
                     reader.LocalName == "cell")
            {
                currentCell = null;
                cellDepth = -1;
            }
        }

        if (width == 0 || height == 0)
        {
            throw new InvalidDataException("Blueprint does not contain one valid snapshot root.");
        }

        var tables = cells
            .SelectMany(pair => pair.Value.Items
                .Where(item => DiningTables.ContainsKey(item.DefName))
                .Select(item => (Root: pair.Key, Item: item)))
            .ToArray();
        var chairs = cells
            .Where(pair => pair.Value.Items.Any(item => DiningChairs.Contains(item.DefName)))
            .Select(pair => pair.Key)
            .ToHashSet();
        var tableObservations = tables.Select(table =>
            {
                var footprint = DiningTables[table.Item.DefName];
                var occupied = OccupiedCells(table.Root, table.Item.Rotation, footprint.Width, footprint.Height);
                var adjacent = AdjacentCardinal(occupied);
                return new DiningTableObservation(table.Item.DefName, chairs.Count(adjacent.Contains));
            })
            .ToArray();
        var tablesWithEnoughSeats = tableObservations.Count(table =>
            table.AdjacentChairCount >= DiningTables[table.DefName].MinimumSeats);

        var conduits = cells
            .Where(pair => pair.Value.Items.Any(item => PowerConduits.Contains(item.DefName)))
            .ToArray();
        var conduitsUnderWalls = conduits.Count(pair => pair.Value.Items.Any(item => item.ActsAsWall));
        var wallCells = cells
            .Where(pair => pair.Value.Items.Any(item => item.ActsAsWall))
            .Select(pair => pair.Key)
            .ToHashSet();
        var conduitsAtOrAdjacentWalls = conduits.Count(pair =>
            wallCells.Contains(pair.Key) || CardinalCells(pair.Key).Any(wallCells.Contains));

        var workstations = cells
            .SelectMany(pair => pair.Value.Items
                .Where(item => WorkstationFootprints.ContainsKey(item.DefName))
                .Select(item => (Root: pair.Key, Item: item)))
            .ToArray();
        var workstationObservations = workstations.Select(workstation =>
            {
                var footprint = WorkstationFootprints[workstation.Item.DefName];
                var occupied = OccupiedCells(
                    workstation.Root,
                    workstation.Item.Rotation,
                    footprint.Width,
                    footprint.Height);
                return new WorkstationObservation(
                    workstation.Item.DefName,
                    AdjacentCardinal(occupied).Any(wallCells.Contains),
                    occupied.Any(cell => cells.TryGetValue(cell, out var state) && state.Roofed));
            })
            .ToArray();

        var utilities = cells
            .SelectMany(pair => pair.Value.Items
                .Where(item => FixedOutdoorUtilities.Contains(item.DefName))
                .Select(item => (Root: pair.Key, Item: item)))
            .ToArray();
        var utilityObservations = utilities.Select(utility =>
            {
                var footprint = FixedUtilityFootprint(utility.Item.DefName);
                var occupied = OccupiedCells(utility.Root, utility.Item.Rotation, footprint.Width, footprint.Height);
                return new OutdoorUtilityObservation(
                    utility.Item.DefName,
                    occupied.Any(cell => cells.TryGetValue(cell, out var state) && state.Roofed));
            })
            .ToArray();
        var itemDefCounts = cells
            .SelectMany(pair => pair.Value.Items)
            .GroupBy(item => item.DefName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var placedItems = cells
            .SelectMany(pair => pair.Value.Items.Select(item => new PlacedItemObservation(
                item.DefName,
                pair.Key.X,
                pair.Key.Z,
                item.Rotation,
                item.ActsAsWall,
                pair.Value.Roofed)))
            .ToArray();

        return new BlueprintAnalysis(
            sourceId,
            width,
            height,
            cells.Count,
            cells.Count(pair => pair.Value.Roofed),
            wallCells.Count,
            new PlacementMetric(tables.Length, tablesWithEnoughSeats),
            new PlacementMetric(conduits.Length, conduitsUnderWalls),
            new PlacementMetric(conduits.Length, conduitsAtOrAdjacentWalls),
            new PlacementMetric(workstations.Length, workstationObservations.Count(item => item.TouchesWall)),
            new PlacementMetric(utilities.Length, utilityObservations.Count(item => item.Roofed)),
            tableObservations,
            workstationObservations,
            utilityObservations,
            placedItems,
            itemDefCounts);
    }

    internal static bool IsResidentialBed(string defName) => ResidentialBeds.Contains(defName);

    internal static bool IsCookingWorkstation(string defName) =>
        defName is "ElectricStove" or "FueledStove" or "ButcherTable";

    internal static bool IsFoodStorage(string defName) => FoodStorage.Contains(defName);

    private static void RequireAttributeBudget(XmlReader reader)
    {
        if (reader.AttributeCount > 64)
        {
            throw new InvalidDataException("Blueprint element exceeds the maximum of 64 attributes.");
        }
    }

    private static int ReadBoundedInt(XmlReader reader, string name, int minimum, int maximum)
    {
        var raw = ReadRequiredText(reader, name, 32);
        if (!int.TryParse(raw, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ||
            value < minimum || value > maximum)
        {
            throw new InvalidDataException($"Blueprint attribute '{name}' is outside {minimum}..{maximum}.");
        }

        return value;
    }

    private static string ReadRequiredText(XmlReader reader, string name, int maximumLength)
    {
        var value = reader.GetAttribute(name);
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            throw new InvalidDataException($"Blueprint attribute '{name}' is missing or too long.");
        }

        return value;
    }

    private static int ReadOptionalRotation(XmlReader reader)
    {
        var raw = reader.GetAttribute("rot") ?? "0";
        return int.TryParse(raw, out var value) && value is >= 0 and <= 3
            ? value
            : throw new InvalidDataException("Blueprint item rotation must be 0..3.");
    }

    private static HashSet<Cell> OccupiedCells(Cell root, int rotation, int width, int height)
    {
        var center = root;
        if (rotation is 1 or 3)
        {
            (width, height) = (height, width);
        }

        var (xAdjust, zAdjust) = rotation switch
        {
            1 => (0, -1),
            2 => (-1, -1),
            3 => (-1, 0),
            _ => (0, 0)
        };
        if (width % 2 == 0)
        {
            center = center with { X = center.X + xAdjust };
        }

        if (height % 2 == 0)
        {
            center = center with { Z = center.Z + zAdjust };
        }

        var minimumX = center.X - (width - 1) / 2;
        var minimumZ = center.Z - (height - 1) / 2;
        var result = new HashSet<Cell>();
        for (var x = minimumX; x < minimumX + width; x++)
        {
            for (var z = minimumZ; z < minimumZ + height; z++)
            {
                result.Add(new Cell(x, z));
            }
        }

        return result;
    }

    private static HashSet<Cell> AdjacentCardinal(HashSet<Cell> occupied)
    {
        var result = new HashSet<Cell>();
        foreach (var cell in occupied)
        {
            foreach (var adjacent in new[]
                     {
                         new Cell(cell.X - 1, cell.Z),
                         new Cell(cell.X + 1, cell.Z),
                         new Cell(cell.X, cell.Z - 1),
                         new Cell(cell.X, cell.Z + 1)
                     })
            {
                if (!occupied.Contains(adjacent))
                {
                    result.Add(adjacent);
                }
            }
        }

        return result;
    }

    private static IEnumerable<Cell> CardinalCells(Cell cell)
    {
        yield return new Cell(cell.X - 1, cell.Z);
        yield return new Cell(cell.X + 1, cell.Z);
        yield return new Cell(cell.X, cell.Z - 1);
        yield return new Cell(cell.X, cell.Z + 1);
    }

    private static Footprint FixedUtilityFootprint(string defName) => defName switch
    {
        "ChemfuelPoweredGenerator" => new Footprint(2, 2, 0),
        "WaterTowerS" => new Footprint(2, 2, 0),
        _ => throw new InvalidOperationException($"No exact footprint is registered for '{defName}'.")
    };

    private sealed class CellState
    {
        public bool Roofed { get; set; }

        public List<Item> Items { get; } = new();
    }

    private readonly record struct Cell(int X, int Z);

    private readonly record struct Item(string DefName, int Rotation, bool ActsAsWall);

    private readonly record struct Footprint(int Width, int Height, int MinimumSeats);

    private sealed class BoundedReadStream(Stream inner, long maximumBytes) : Stream
    {
        private long totalRead;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            Add(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = inner.Read(buffer);
            Add(read);
            return read;
        }

        private void Add(int read)
        {
            totalRead += read;
            if (totalRead > maximumBytes)
            {
                throw new InvalidDataException($"Blueprint expands beyond {maximumBytes} bytes.");
            }
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
