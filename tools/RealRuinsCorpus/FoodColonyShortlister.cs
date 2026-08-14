namespace RealRuinsCorpus;

public sealed record FoodColonyPlacement(string DefName, int X, int Z, int Rotation, bool Roofed);

public sealed record FoodColonyCandidate(
    string SourceId,
    double Score,
    int Beds,
    int DiningTables,
    int DiningSeats,
    int CookingWorkstations,
    int FoodStorageBuildings,
    int WallCells,
    int SerializedCells,
    int FoodDistrictWidth,
    int FoodDistrictHeight,
    IReadOnlyList<string> RelevantDefs,
    IReadOnlyList<FoodColonyPlacement> RelevantPlacements,
    string BlueprintPath);

public sealed record FoodColonyShortlistResult(
    int BlueprintsInspected,
    int RejectedBlueprints,
    int EligibleBlueprints,
    IReadOnlyList<FoodColonyCandidate> Candidates);

public static class FoodColonyShortlister
{
    private const int MaximumBlueprints = 5_000;

    public static async Task<FoodColonyShortlistResult> ShortlistAsync(
        string blueprintDirectory,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blueprintDirectory);
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        var root = Path.GetFullPath(blueprintDirectory);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);

        var paths = Directory.EnumerateFiles(root, "*.bp", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumBlueprints + 1)
            .ToArray();
        if (paths.Length > MaximumBlueprints)
            throw new InvalidDataException($"Blueprint directory exceeds {MaximumBlueprints} files.");

        var candidates = new List<FoodColonyCandidate>();
        var rejected = 0;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
                var analysis = BlueprintAnalyzer.Analyze(stream, Path.GetFileNameWithoutExtension(path));
                var candidate = CreateCandidate(analysis, path);
                if (candidate is not null) candidates.Add(candidate);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                rejected++;
            }
        }

        var ranked = candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.FoodDistrictWidth * candidate.FoodDistrictHeight)
            .ThenBy(candidate => candidate.SourceId, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
        return new FoodColonyShortlistResult(paths.Length, rejected, candidates.Count, ranked);
    }

    private static FoodColonyCandidate? CreateCandidate(BlueprintAnalysis analysis, string path)
    {
        var beds = analysis.PlacedItems.Count(item => BlueprintAnalyzer.IsResidentialBed(item.DefName));
        var cooking = analysis.PlacedItems.Where(item => BlueprintAnalyzer.IsCookingWorkstation(item.DefName)).ToArray();
        var foodStorage = analysis.PlacedItems.Where(item => BlueprintAnalyzer.IsFoodStorage(item.DefName)).ToArray();
        var diningSeats = analysis.DiningTables.Sum(item => item.AdjacentChairCount);
        if (beds < 4 || analysis.DiningTables.Count == 0 || diningSeats < 4 || cooking.Length < 2 ||
            foodStorage.Length == 0 || analysis.WallCells < 24)
        {
            return null;
        }

        var district = analysis.PlacedItems.Where(item =>
                BlueprintAnalyzer.IsCookingWorkstation(item.DefName) ||
                BlueprintAnalyzer.IsFoodStorage(item.DefName) ||
                item.DefName.StartsWith("Table", StringComparison.Ordinal) ||
                item.DefName is "DiningChair" or "Stool")
            .ToArray();
        var minX = district.Min(item => item.X);
        var maxX = district.Max(item => item.X);
        var minZ = district.Min(item => item.Z);
        var maxZ = district.Max(item => item.Z);
        var districtWidth = maxX - minX + 1;
        var districtHeight = maxZ - minZ + 1;
        var compactness = 120.0 / Math.Max(24, districtWidth * districtHeight);
        var wallTouchRate = analysis.Workstations.Count == 0
            ? 0
            : analysis.Workstations.Count(item => item.TouchesWall) / (double)analysis.Workstations.Count;
        var conduitWallRate = analysis.PowerConduitsAtOrAdjacentToWalls.Total == 0
            ? 0
            : analysis.PowerConduitsAtOrAdjacentToWalls.Matching /
              (double)analysis.PowerConduitsAtOrAdjacentToWalls.Total;
        static double ShowcaseScaleScore(int count, int ideal, double weight)
        {
            var distance = Math.Abs(count - ideal);
            return weight * Math.Max(-2.0, 1.0 - distance / (double)ideal);
        }

        var score = ShowcaseScaleScore(beds, 8, 16.0) +
                    ShowcaseScaleScore(diningSeats, 8, 12.0) +
                    ShowcaseScaleScore(cooking.Length, 3, 10.0) +
                    ShowcaseScaleScore(foodStorage.Length, 2, 5.0) +
                    compactness + wallTouchRate * 6.0 + conduitWallRate * 3.0;
        var relevantDefs = district.Select(item => item.DefName)
            .Concat(analysis.PlacedItems.Where(item => BlueprintAnalyzer.IsResidentialBed(item.DefName))
                .Select(item => item.DefName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var relevantPlacements = analysis.PlacedItems.Where(item =>
                BlueprintAnalyzer.IsResidentialBed(item.DefName) ||
                BlueprintAnalyzer.IsCookingWorkstation(item.DefName) ||
                BlueprintAnalyzer.IsFoodStorage(item.DefName) ||
                item.DefName.StartsWith("Table", StringComparison.Ordinal) ||
                item.DefName is "DiningChair" or "Stool" or "Door" or "Autodoor" or
                    "EndTable" or "Dresser" or "ChessTable" or "PokerTable" or
                    "Shelf" or "ShelfSmall")
            .Select(item => new FoodColonyPlacement(item.DefName, item.X, item.Z, item.Rotation, item.Roofed))
            .OrderBy(item => item.Z)
            .ThenBy(item => item.X)
            .ThenBy(item => item.DefName, StringComparer.Ordinal)
            .ToArray();

        return new FoodColonyCandidate(
            analysis.SourceId,
            Math.Round(score, 3),
            beds,
            analysis.DiningTables.Count,
            diningSeats,
            cooking.Length,
            foodStorage.Length,
            analysis.WallCells,
            analysis.SerializedCells,
            districtWidth,
            districtHeight,
            relevantDefs,
            relevantPlacements,
            Path.GetFullPath(path));
    }
}
