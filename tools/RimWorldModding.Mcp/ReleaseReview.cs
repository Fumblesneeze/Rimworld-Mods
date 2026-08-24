using System.Buffers.Binary;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record ReviewedSubscriberScreenshot(string Path, long Bytes, string Sha256);

public sealed record ReleaseReviewResult(
    string Status,
    string PackageId,
    string PublishedFileId,
    string PublicationPlanSha256,
    string ReceiptPath,
    string ReviewPath,
    string Observation,
    IReadOnlyList<ReviewedSubscriberScreenshot> Screenshots);

public static class ReleaseReview
{
    public static ReleaseReviewResult Accept(
        string repositoryRoot,
        string planPath,
        string planSha256,
        string confirmationNonce,
        string receiptPath,
        string observation)
    {
        if (string.IsNullOrWhiteSpace(observation) || observation.Trim().Length < 20)
            throw new InvalidOperationException("A concrete personal subscriber screenshot observation is required.");
        var status = ReleasePlanAdmission.Status(repositoryRoot, planPath, planSha256, confirmationNonce);
        if (!status.AwaitingPersonalSubscriberReview || status.DurablePublishedFileId is null)
            throw new InvalidOperationException("The exact publication is not awaiting personal subscriber evidence review.");

        var root = RepositoryRoot.Resolve(repositoryRoot);
        var receipt = RepositoryRoot.ContainedPath(root, receiptPath);
        var expectedRoot = Path.Combine(root, "artifacts", "Releases", status.PackageId, "publication") +
                           Path.DirectorySeparatorChar;
        if (!receipt.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(receipt))
            throw new InvalidOperationException("Publication receipt is not a retained receipt for this package.");
        var workerResultPath = Path.Combine(root, "artifacts", "Releases", status.PackageId,
            "workers", status.PlanSha256, "worker-result.json");
        if (!File.Exists(workerResultPath))
            throw new InvalidOperationException("The exact durable publisher worker result is missing.");
        var workerResult = JsonSerializer.Deserialize(
            File.ReadAllText(workerResultPath), McpJsonContext.Default.ReleasePublishResult) ??
                           throw new InvalidOperationException("The durable publisher worker result is empty.");
        if (!string.Equals(Path.GetFullPath(workerResult.ReceiptPath), receipt, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(workerResult.ReceiptSha256, ReleaseCandidateBuilder.Hash(receipt), StringComparison.Ordinal) ||
            workerResult.PublicationPlanSha256 != status.PlanSha256 ||
            workerResult.PackageId != status.PackageId ||
            workerResult.PublishedFileId != status.DurablePublishedFileId ||
            workerResult.CandidateDigest != status.CandidateDigest ||
            !workerResult.SteamVerified || !workerResult.LocalPackageRestored)
            throw new InvalidOperationException("Publication receipt is not bound to the exact successful durable worker result.");
        using var document = JsonDocument.Parse(File.ReadAllText(receipt));
        var response = document.RootElement;
        if (GatewayWorkshopClient.String(response, "schema") != "RimWorldModReleaseReceipt/v1" ||
            GatewayWorkshopClient.String(response, "packageId") != status.PackageId ||
            GatewayWorkshopClient.String(response, "title") != status.Title ||
            GatewayWorkshopClient.String(response, "candidateDigest") != status.CandidateDigest ||
            GatewayWorkshopClient.String(response, "publicationPlanSha256") != status.PlanSha256 ||
            GatewayWorkshopClient.String(response, "publishedFileId") != status.DurablePublishedFileId ||
            GatewayWorkshopClient.String(response, "subscriberEvidenceStatus") != "awaiting-personal-review")
            throw new InvalidOperationException("Publication receipt does not match the exact plan and Workshop item.");
        if (!GatewayWorkshopClient.TryProperty(response, "subscriber", out var subscriber) ||
            !GatewayWorkshopClient.TryProperty(subscriber, "screenshots", out var screenshotArray) ||
            screenshotArray.ValueKind != JsonValueKind.Array ||
            GatewayWorkshopClient.String(subscriber, "status") != "passed")
            throw new InvalidOperationException("Publication receipt does not contain subscriber screenshots.");
        var screenshots = screenshotArray.EnumerateArray().Select(item => Path.GetFullPath(item.GetString()!)).ToArray();
        if (screenshots.Length < 2 || screenshots.Distinct(StringComparer.OrdinalIgnoreCase).Count() != screenshots.Length)
            throw new InvalidOperationException("Subscriber review requires at least two distinct retained screenshots.");
        var evidenceRoot = Path.GetFullPath(GatewayWorkshopClient.String(subscriber, "evidenceRoot"))
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var expectedEvidenceRoot = Path.Combine(root, "artifacts", "Releases", status.PackageId, "subscriber") +
                                   Path.DirectorySeparatorChar;
        if (!evidenceRoot.StartsWith(expectedEvidenceRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(evidenceRoot.TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(workerResult.SubscriberEvidenceRoot).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber evidence root is not the exact retained package evidence root.");
        var reviewed = screenshots.Select(path =>
        {
            if (!path.StartsWith(evidenceRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Subscriber screenshot is missing or escapes its exact evidence root.");
            ValidatePng(path);
            return new ReviewedSubscriberScreenshot(path, new FileInfo(path).Length, ReleaseCandidateBuilder.Hash(path));
        }).ToArray();

        var reviewPath = Path.Combine(Path.GetDirectoryName(receipt)!, "subscriber-personal-review.json");
        var review = new Dictionary<string, object?>
        {
            ["schema"] = "RimWorldSubscriberPersonalReview/v1",
            ["reviewedUtc"] = DateTimeOffset.UtcNow,
            ["packageId"] = status.PackageId,
            ["publishedFileId"] = status.DurablePublishedFileId,
            ["publicationPlanSha256"] = status.PlanSha256,
            ["observation"] = observation.Trim(),
            ["screenshots"] = reviewed
        };
        DurableFile.WriteAllText(reviewPath,
            JsonSerializer.Serialize(review, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        var statePath = Path.Combine(root, "artifacts", "Releases", status.PackageId, "publication-state.txt");
        DurableFile.WriteAllText(statePath,
            $"complete-reviewed|{status.PlanSha256}|{status.DurablePublishedFileId}");
        return new ReleaseReviewResult("complete-personally-reviewed", status.PackageId,
            status.DurablePublishedFileId, status.PlanSha256, receipt, reviewPath, observation.Trim(), reviewed);
    }

    private static void ValidatePng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) is 0 or > 16384 ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) is 0 or > 16384 ||
            bytes.AsSpan().IndexOf("IEND"u8) < 0)
            throw new InvalidOperationException("Subscriber evidence contains an invalid PNG screenshot.");
    }
}
