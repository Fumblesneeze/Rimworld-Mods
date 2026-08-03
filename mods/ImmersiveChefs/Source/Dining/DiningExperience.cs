using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class Thought_MemoryCulinaryQuality : Thought_Memory
{
    public override float MoodOffset()
    {
        return base.MoodOffset() * ImmersiveChefsMod.Settings.QualityMoodScale;
    }
}

internal static class DiningExperience
{
    internal static void Apply(Pawn pawn, ThingDef mealDef, CulinaryServingRecord serving, DiningSession? dining)
    {
        var memories = pawn.needs?.mood?.thoughts?.memories;
        if (memories is null)
        {
            return;
        }

        var settings = ImmersiveChefsMod.Settings;
        if (settings.CulinaryQualityEnabled)
        {
            ReplaceMemory(memories, "ImmersiveChefs_CulinaryQuality", QualityStage(serving.QualityScore));
        }

        if (settings.MealTemperatureEnabled)
        {
            ReplaceMemory(
                memories,
                "ImmersiveChefs_MealTemperature",
                (int)ThermalCalculator.BandFor(serving.TemperatureCelsius));
        }

        if (AssistedDiningPolicy.ShouldRecordDiningMemory(
                dining?.IsAssisted == true,
                pawn.health.capacities.CanBeAwake,
                settings.WareRequirementMode,
                dining?.CarriedCutlery is not null))
        {
            var diningStage = DiningStandardsRuntime.DiningThoughtStage(pawn, mealDef, serving, dining);
            ReplaceMemory(memories, "ImmersiveChefs_DiningExperience", diningStage);
        }
    }

    private static int QualityStage(int qualityScore)
    {
        return Math.Max(0, Math.Min(100, qualityScore)) switch
        {
            < 20 => 0,
            < 35 => 1,
            < 50 => 2,
            < 65 => 3,
            < 80 => 4,
            < 90 => 5,
            _ => 6
        };
    }

    private static void ReplaceMemory(MemoryThoughtHandler memories, string defName, int stage)
    {
        var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
        if (thoughtDef is null || stage < 0 || stage >= thoughtDef.stages.Count)
        {
            return;
        }

        memories.RemoveMemoriesOfDef(thoughtDef);
        memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, stage));
    }
}
