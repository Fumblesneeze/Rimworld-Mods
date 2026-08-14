using System.Text.RegularExpressions;

namespace RealRuinsCorpus;

public sealed record ShowcaseLayoutAuditResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    int ChairCount,
    string DiningTableDef,
    int WorkstationCount,
    int ConduitRunCount,
    int PrivateBedroomCount,
    int RecreationNookCount,
    int DishReturnCount);

public static partial class ShowcaseLayoutAudit
{
    public static ShowcaseLayoutAuditResult AuditDishwasherScene(
        string layoutSource,
        string furnishSource,
        string placementPreflightSource)
    {
        ArgumentNullException.ThrowIfNull(layoutSource);
        ArgumentNullException.ThrowIfNull(furnishSource);
        ArgumentNullException.ThrowIfNull(placementPreflightSource);
        var errors = new List<string>();

        var tableDef = Regex.Match(furnishSource, "GetNamed\\(\\\"(?<def>Table[123]x[234]c)\\\"\\)")
            .Groups["def"].Value;
        var chairBlock = Regex.Match(
            furnishSource,
            "var chairCells = new\\[\\]\\s*\\{(?<cells>.*?)\\};",
            RegexOptions.Singleline).Groups["cells"].Value;
        var chairCount = Regex.Matches(chairBlock, "new IntVec3\\(").Count;
        var expectedSeats = tableDef switch
        {
            "Table1x2c" => 2,
            "Table2x2c" => 4,
            "Table2x4c" => 8,
            "Table3x3c" => 12,
            _ => 0
        };
        if (expectedSeats == 0 || chairCount > expectedSeats)
        {
            errors.Add($"Dining table '{tableDef}' cannot support {chairCount} declared perimeter chairs.");
        }

        if (!furnishSource.Contains("if (generatorDef.rotatable)", StringComparison.Ordinal) ||
            !Regex.IsMatch(furnishSource, "GenSpawn\\.Spawn\\(generator,.*Rot4\\.North\\)", RegexOptions.Singleline))
        {
            errors.Add("Chemfuel generator must assert non-rotatability and use its native north rotation.");
        }

        if (layoutSource.Contains("var utility =", StringComparison.Ordinal) ||
            !layoutSource.Contains("utilityYard", StringComparison.Ordinal) ||
            !layoutSource.Contains("map.roofGrid.SetRoof(cell, null)", StringComparison.Ordinal))
        {
            errors.Add("Water tower and fueled power require a distinct unroofed exterior utility yard.");
        }

        var workstationMatches = Regex.Matches(
            furnishSource,
            "GenSpawn\\.Spawn\\((dishwasher|stove|prep), (?<cell>\\w+), map, Rot4\\.(North|South|East)\\)");
        if (workstationMatches.Count != 3 ||
            !placementPreflightSource.Contains("north-workbench", StringComparison.Ordinal) ||
            !placementPreflightSource.Contains("east-workbench", StringComparison.Ordinal))
        {
            errors.Add("Dishwasher, stove and prep station must use the finalized-Def wall-run preflight.");
        }

        var conduitRunCount = Regex.Matches(furnishSource, "new CellRect\\(.*conduit", RegexOptions.IgnoreCase).Count;
        if (!furnishSource.Contains("center.z + 5", StringComparison.Ordinal) ||
            !furnishSource.Contains("center.x + 8", StringComparison.Ordinal))
        {
            errors.Add("Conduits must be routed along the north/east wall cells and service-yard branch.");
        }

        if (!layoutSource.Contains("yardFenceCells", StringComparison.Ordinal))
        {
            errors.Add("Exterior utilities require an enclosed service yard with deliberate access gaps.");
        }

        if (!furnishSource.Contains("KitchenSink", StringComparison.Ordinal) &&
            !furnishSource.Contains("BasinStuff", StringComparison.Ordinal))
        {
            errors.Add("Wet service wall must contain an exact installed Dubs sink or basin.");
        }

        if (!layoutSource.Contains("bedroomWing", StringComparison.Ordinal) ||
            !furnishSource.Contains("GetNamed(\"Cooler\")", StringComparison.Ordinal) ||
            !layoutSource.Contains("coolerCell", StringComparison.Ordinal))
        {
            errors.Add("Showcase crop requires connected residential context and a functional freezer buffer.");
        }

        var bedroomDoorBlock = Regex.Match(
            layoutSource,
            "var privateBedroomDoors = new\\[\\]\\s*\\{(?<cells>.*?)\\};",
            RegexOptions.Singleline).Groups["cells"].Value;
        var bedroomBedBlock = Regex.Match(
            layoutSource,
            "var privateBedroomBeds = new\\[\\]\\s*\\{(?<cells>.*?)\\};",
            RegexOptions.Singleline).Groups["cells"].Value;
        var privateBedroomDoors = Regex.Matches(bedroomDoorBlock, "new IntVec3\\(").Count;
        var privateBedroomBeds = Regex.Matches(bedroomBedBlock, "new IntVec3\\(").Count;
        var partitionBlock = Regex.Match(
            layoutSource,
            "foreach \\(var partitionX in new\\[\\] \\{(?<xs>.*?)\\}\\)",
            RegexOptions.Singleline).Groups["xs"].Value;
        var partitionCount = Regex.Matches(partitionBlock, "center\\.x").Count;
        if (privateBedroomDoors != 4 || privateBedroomBeds != 4 || partitionCount < 3)
        {
            errors.Add("Residential wing must contain four separated private rooms with one real bed and exterior door each.");
        }

        var recreationNookCount = Regex.Matches(
            furnishSource,
            "var recreationNook = .*GetNamed\\(\\\"ChessTable\\\"\\)",
            RegexOptions.Singleline).Count;
        if (recreationNookCount != 1)
        {
            errors.Add("Dining/common room must contain one deliberately seated recreation nook.");
        }

        var dishReturnCount = Regex.Matches(
            furnishSource,
            "var dishReturn = .*MakeThing\\(smallShelfDef",
            RegexOptions.Singleline).Count;
        if (dishReturnCount != 1 ||
            !furnishSource.Contains("dishReturn.GetStoreSettings().filter.SetAllow(DefDatabase<SpecialThingFilterDef>.GetNamed(\"ImmersiveChefs_AllowDirtyKitchenware\"), true", StringComparison.Ordinal))
        {
            errors.Add("Dining exit must contain one dirty-only dish-return shelf.");
        }

        if (!placementPreflightSource.Contains("GenAdj.OccupiedRect", StringComparison.Ordinal) ||
            !placementPreflightSource.Contains("ThingUtility.InteractionCellWhenAt", StringComparison.Ordinal) ||
            !placementPreflightSource.Contains("validate the wall-mounted cooler", StringComparison.Ordinal) ||
            !furnishSource.Contains("GenSpawn.Spawn(cooler, coolerCell, map, Rot4.East)", StringComparison.Ordinal))
        {
            errors.Add("The scenario must preflight real occupied/interaction cells and use the layout's exact cooler wall opening.");
        }

        foreach (var label in new[]
        {
            "dining plant pot",
            "secondary dining lamp",
            "east dining sculpture",
            "dishwasher",
            "kitchen sink",
            "ingredient prep station",
            "electric stove",
            "water tower",
            "chemfuel generator"
        })
        {
            if (!placementPreflightSource.Contains($"\"{label}\"", StringComparison.Ordinal))
            {
                errors.Add($"The finalized-Def placement preflight is missing '{label}'.");
            }
        }

        return new ShowcaseLayoutAuditResult(
            errors.Count == 0,
            errors,
            chairCount,
            tableDef,
            workstationMatches.Count,
            conduitRunCount,
            Math.Min(privateBedroomDoors, privateBedroomBeds),
            recreationNookCount,
            dishReturnCount);
    }
}
