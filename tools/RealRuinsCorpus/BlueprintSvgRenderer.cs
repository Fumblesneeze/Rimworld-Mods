using System.Globalization;
using System.Security;
using System.Text;

namespace RealRuinsCorpus;

public static class BlueprintSvgRenderer
{
    private static readonly IReadOnlyDictionary<string, (int Width, int Height)> KnownFootprints =
        new Dictionary<string, (int Width, int Height)>(StringComparer.Ordinal)
        {
            ["Table1x2c"] = (1, 2),
            ["Table2x2c"] = (2, 2),
            ["Table2x4c"] = (2, 4),
            ["Table3x3c"] = (3, 3),
            ["ElectricStove"] = (3, 1),
            ["FueledStove"] = (3, 1),
            ["ButcherTable"] = (3, 1),
            ["ElectricSmithy"] = (3, 1),
            ["FueledSmithy"] = (3, 1),
            ["MachiningTable"] = (3, 1),
            ["TailoringBench"] = (3, 1),
            ["HandTailoringBench"] = (3, 1),
            ["DrugLab"] = (3, 1),
            ["SimpleResearchBench"] = (3, 1),
            ["HiTechResearchBench"] = (3, 2),
            ["ChemfuelPoweredGenerator"] = (2, 2),
            ["WaterTowerS"] = (2, 2)
        };

    private static readonly HashSet<string> ContextFurniture = new(StringComparer.Ordinal)
    {
        "Bed", "Bedroll", "RoyalBed", "EndTable", "Dresser", "ChessTable", "PokerTable",
        "Cooler", "PassiveCooler", "Refrigerator", "Fridge", "Shelf", "ShelfSmall",
        "Door", "Autodoor", "PlantPot", "StandingLamp", "TubeTelevision", "Armchair"
    };

    public static string Render(BlueprintAnalysis analysis, int scale = 4)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        if (scale is < 2 or > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        var width = checked(analysis.Width * scale);
        var height = checked(analysis.Height * scale);
        var builder = new StringBuilder();
        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
        builder.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#181818\"/>");
        foreach (var item in analysis.PlacedItems)
        {
            var x = item.X * scale;
            var y = (analysis.Height - item.Z - 1) * scale;
            if (item.ActsAsWall)
            {
                builder.AppendLine(Rect(x, y, scale, scale, "#827568", null));
                continue;
            }

            if (item.DefName is "PowerConduit" or "PowerConduitInvisible")
            {
                builder.AppendLine(Rect(x + scale / 3, y + scale / 3, Math.Max(1, scale / 3), Math.Max(1, scale / 3), "#d4b94f", null));
                continue;
            }

            if (item.DefName is "DiningChair" or "Stool")
            {
                builder.AppendLine(Rect(x, y, scale, scale, "#4e9a64", "#172a1c"));
                continue;
            }

            if (!KnownFootprints.TryGetValue(item.DefName, out var footprint))
            {
                if (ContextFurniture.Contains(item.DefName))
                {
                    var contextColor = BlueprintAnalyzer.IsResidentialBed(item.DefName)
                        ? "#6388b7"
                        : BlueprintAnalyzer.IsFoodStorage(item.DefName)
                            ? "#62a8b0"
                            : item.DefName is "Door" or "Autodoor"
                                ? "#d7b87c"
                                : "#9277b8";
                    builder.AppendLine(Rect(x, y, scale, scale, contextColor, "#111111"));
                }
                continue;
            }

            var (occupiedX, occupiedY, occupiedWidth, occupiedHeight) = OccupiedRect(
                item.X,
                item.Z,
                item.Rotation,
                footprint.Width,
                footprint.Height,
                analysis.Height,
                scale);
            var color = item.DefName.StartsWith("Table", StringComparison.Ordinal)
                ? "#b6783e"
                : item.DefName is "ChemfuelPoweredGenerator" or "WaterTowerS"
                    ? "#3d87a8"
                    : "#b54d52";
            builder.AppendLine(Rect(occupiedX, occupiedY, occupiedWidth, occupiedHeight, color, "#111111"));
            builder.AppendLine($"<title>{SecurityElement.Escape(item.DefName)} at {item.X},{item.Z} rot {item.Rotation}</title>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string Rect(int x, int y, int width, int height, string fill, string? stroke) =>
        $"<rect x=\"{x}\" y=\"{y}\" width=\"{width}\" height=\"{height}\" fill=\"{fill}\"" +
        (stroke is null ? "/>" : $" stroke=\"{stroke}\" stroke-width=\"1\"/>");

    private static (int X, int Y, int Width, int Height) OccupiedRect(
        int rootX,
        int rootZ,
        int rotation,
        int width,
        int height,
        int mapHeight,
        int scale)
    {
        var centerX = rootX;
        var centerZ = rootZ;
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
        if (width % 2 == 0) centerX += xAdjust;
        if (height % 2 == 0) centerZ += zAdjust;
        var minX = centerX - (width - 1) / 2;
        var minZ = centerZ - (height - 1) / 2;
        return (minX * scale, (mapHeight - minZ - height) * scale, width * scale, height * scale);
    }
}
