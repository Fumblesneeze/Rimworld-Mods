using System;
using System.Collections.Generic;

namespace Megamod.Patching;

public enum AdmissionPhase { Early, DefsReady, GameReady }

public sealed record PatchCandidate(
    string Id, string TargetSpecId, string Target, string UnavailableBehavior, string Fallback,
    Func<string?> Validate, Action Install, Action Rollback);

public sealed record PatchGroup(string Id, AdmissionPhase Phase, IReadOnlyList<PatchCandidate> Patches);

public sealed record WarningDeliveryFailure(string PatchId, string Warning, string PrimaryFailure, string FallbackFailure);

public sealed class AdmissionCoordinator
{
    private readonly Dictionary<string, bool> results = new(StringComparer.Ordinal);
    private readonly Action<string> warningRecorder;
    private readonly Action<string>? fallbackWarningRecorder;
    private readonly List<WarningDeliveryFailure> deliveryFailures = new();

    public AdmissionCoordinator(Action<string> warningRecorder, Action<string>? fallbackWarningRecorder = null)
    {
        this.warningRecorder = warningRecorder;
        this.fallbackWarningRecorder = fallbackWarningRecorder;
    }

    public bool IsAdmitted(string groupId) => results.TryGetValue(groupId, out bool result) && result;

    public IReadOnlyList<WarningDeliveryFailure> DeliveryFailures => deliveryFailures.AsReadOnly();
    public long UndeliveredWarningCount { get; private set; }

    public bool Apply(PatchGroup group, AdmissionPhase phase, bool enabled = true, string? unavailableReason = null)
    {
        if (!enabled || group.Phase != phase) return false;
        if (results.TryGetValue(group.Id, out bool result)) return result;
        var candidates = new List<PatchCandidate>(group.Patches);
        results[group.Id] = false;
        if (unavailableReason != null) return Reject(group.Id, candidates, unavailableReason);
        string? failure = null;
        foreach (var patch in candidates)
        {
            try { failure = patch.Validate(); }
            catch (Exception exception) { failure = exception.GetBaseException().Message; }
            if (failure != null) break;
        }
        if (failure != null) return Reject(group.Id, candidates, failure);
        var attempted = new List<PatchCandidate>();
        try
        {
            foreach (var patch in candidates)
            {
                attempted.Add(patch);
                patch.Install();
            }
        }
        catch (Exception exception)
        {
            failure = exception.GetBaseException().Message;
            int cleanupFailures = 0;
            string? firstCleanupFailure = null;
            for (int index = attempted.Count - 1; index >= 0; index--)
            {
                try { attempted[index].Rollback(); }
                catch (Exception cleanupException)
                {
                    cleanupFailures++;
                    firstCleanupFailure ??= $"{attempted[index].Id}: {cleanupException.GetBaseException().Message}";
                }
            }
            if (cleanupFailures > 0)
                failure = $"cleanupFailures={cleanupFailures}; install={Bound(failure, 96)}; firstCleanup={Bound(firstCleanupFailure!, 96)}";
            return Reject(group.Id, candidates, failure);
        }
        results[group.Id] = true;
        return true;
    }

    private bool Reject(string groupId, IReadOnlyList<PatchCandidate> candidates, string reason)
    {
        results[groupId] = false;
        foreach (var patch in candidates)
        {
            string warning = $"[fumblesneeze.megamod] Patch {Bound(patch.Id)} not applied; target={Bound(patch.Target)}; " +
                $"targetSpec={Bound(patch.TargetSpecId)}; reason={Bound(reason)}; unavailable={Bound(patch.UnavailableBehavior)}; fallback={Bound(patch.Fallback)}";
            try { warningRecorder(warning); }
            catch (Exception primaryException)
            {
                string fallbackFailure = "No fallback warning recorder was supplied.";
                if (fallbackWarningRecorder != null)
                {
                    try { fallbackWarningRecorder(warning); continue; }
                    catch (Exception fallbackException) { fallbackFailure = fallbackException.GetBaseException().Message; }
                }
                UndeliveredWarningCount++;
                if (deliveryFailures.Count < 64)
                    deliveryFailures.Add(new WarningDeliveryFailure(Bound(patch.Id), warning,
                        Bound(primaryException.GetBaseException().Message), Bound(fallbackFailure)));
            }
        }
        return false;
    }

    private static string Bound(string value, int maximumFieldCharacters = 256)
    {
        var characters = value.Length <= maximumFieldCharacters ? value.ToCharArray() : value.Substring(0, maximumFieldCharacters - 3).ToCharArray();
        for (int index = 0; index < characters.Length; index++)
            if (char.IsControl(characters[index])) characters[index] = ' ';
        return new string(characters) + (value.Length > maximumFieldCharacters ? "..." : string.Empty);
    }
}
