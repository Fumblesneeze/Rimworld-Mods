using System;

namespace RimWorldDevGateway.Contracts;

public static class WorkshopPublicationState
{
    public static string FailureStage(string stage, bool ioFailure, string result)
    {
        if (string.IsNullOrWhiteSpace(stage)) throw new ArgumentException("A failure stage is required.", nameof(stage));
        var family = stage.StartsWith("create", StringComparison.Ordinal)
            ? "create"
            : stage.StartsWith("preview-", StringComparison.Ordinal)
                ? "preview-submit"
                : stage.StartsWith("app-dependency-", StringComparison.Ordinal) || stage.StartsWith("dependency-", StringComparison.Ordinal)
                    ? "dependency"
                    : "submit";
        // Steam can finish the upload before reporting a lost login or timeout to the submitting client.
        return family + (ioFailure || result is "k_EResultNotLoggedOn" or "k_EResultTimeout" ? "-indeterminate" : "-failed-definite");
    }
}
