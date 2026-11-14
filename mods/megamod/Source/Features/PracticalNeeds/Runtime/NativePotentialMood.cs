using System;
using System.Runtime.CompilerServices;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Settings;
using RimWorld;
using Verse;

namespace Megamod.Features.PracticalNeeds.Runtime;

public sealed class NativePotentialMood
{
    private readonly SettingsRuntime settings;
    private readonly Func<bool> scheduledBoundaryAvailable;
    private readonly Action<string> report;
    private readonly ConditionalWeakTable<Game, object> readingNotices = new();

    public NativePotentialMood(SettingsRuntime settings, Func<bool> scheduledBoundaryAvailable, Action<string> report)
    { this.settings = settings; this.scheduledBoundaryAvailable = scheduledBoundaryAvailable; this.report = report; }

    public bool Available => ScheduledContext.Ready(settings) && scheduledBoundaryAvailable() &&
        settings.Store.GetEffective(PotentialMoodSettings.Enabled);
    public string? StatusKey => !UnityData.IsInMainThread ? "Megamod.PotentialMood.ContextUnavailable" :
        !settings.Store.GetEffective(PotentialMoodSettings.Enabled) ? null :
        !ScheduledContext.Ready(settings) ? "Megamod.PotentialMood.ContextUnavailable" :
        !scheduledBoundaryAvailable() ? "Megamod.PotentialMood.Unavailable" : null;

    public ScheduledDrugReadResult EvaluateScheduled(Pawn_DrugPolicyTracker tracker, ThingDef drug)
    {
        if (!UnityData.IsInMainThread) return ScheduledDrugReadResult.Unavailable;
        if (!settings.Store.GetEffective(PotentialMoodSettings.Enabled)) return ScheduledDrugReadResult.Allowed;
        if (!Available) return ScheduledDrugReadResult.Unavailable;
        var game = Current.Game;
        return ScheduledDrugReadBoundary.Evaluate(() =>
        {
            if (!ScheduledContext.TryCapture(settings, tracker, drug, out var context)) return Unavailable(game);
            var mood = context!.Pawn.needs?.mood;
            float threshold = context.Entry.onlyIfMoodBelow;
            if (mood == null || threshold == 1f) return ScheduledDrugReadResult.Allowed;
            if (!float.IsFinite(threshold) || threshold < 0f || threshold > 1f) return Unavailable(game);
            float current = mood.CurLevelPercentage;
            // This is the actual native thought/difficulty calculation and its native tick cache.
            float potential = mood.CurInstantLevelPercentage;
            if (!context.IsCurrent(settings) || !ReferenceEquals(context.Pawn.needs?.mood, mood) ||
                context.Entry.onlyIfMoodBelow != threshold) return Unavailable(game);
            var result = PotentialMoodRule.Evaluate(true, true, threshold, current, potential);
            return result == ScheduledDrugReadResult.Unavailable ? Unavailable(game) : result;
        }, _ => Unavailable(game));
    }

    private ScheduledDrugReadResult Unavailable(Game? game)
    {
        if (game == null || !ReferenceEquals(Current.Game, game) || readingNotices.TryGetValue(game, out _))
            return ScheduledDrugReadResult.Unavailable;
        readingNotices.Add(game, new object());
        try { report("[fumblesneeze.megamod] S024 targetSpec=DRUG-SCHEDULED-COMPOSITION; current scheduled mood reading unavailable; only the extra mood restriction is omitted; native and independent tolerance decisions remain."); }
        catch (Exception) { }
        try { Messages.Message("Megamod.PotentialMood.ReadingUnavailable".Translate(), MessageTypeDefOf.RejectInput, false); }
        catch (Exception) { }
        return ScheduledDrugReadResult.Unavailable;
    }
}
