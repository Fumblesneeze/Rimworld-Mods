using System.Globalization;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;

namespace Megamod.Features.DrugPolicyGeneration.Core;

// Declaration/capture only. Native Generate registration and current-preview invalidation are separate.
public static class DrugGenerationSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("drug-generation.enabled", true,
        SettingScope.GlobalAndSave, "drug-policy.generation", 0, SettingApplicationTiming.NextEligibleOccurrence, SettingCodecs.Boolean);
    public static readonly SettingDefinition<float> InitialMood = Threshold("drug-generation.initialMood", 1);
    public static readonly SettingDefinition<float> InitialJoy = Threshold("drug-generation.initialJoy", 2);
    public static readonly SettingDefinition<DrugToleranceChoice> InitialTolerance = new("drug-generation.initialTolerance", DrugToleranceChoice.Any,
        SettingScope.GlobalAndSave, "drug-policy.generation", 3, SettingApplicationTiming.NextEligibleOccurrence, new ToleranceCodec(),
        value => value != null && value.Kind != DrugToleranceChoiceKind.NeedsReview
            ? null : "Megamod_DrugGeneration_InvalidTolerance");

    public static void Register(SettingsStore store)
    {
        store.Register(Enabled);
        store.Register(InitialMood);
        store.Register(InitialJoy);
        store.Register(InitialTolerance);
    }

    public static DrugGenerationPreferences Capture(SettingsStore store) => new(
        store.GetEffective(InitialMood), store.GetEffective(InitialJoy), store.GetEffective(InitialTolerance));

    private static SettingDefinition<float> Threshold(string id, int order) => new(id, 1f,
        SettingScope.GlobalAndSave, "drug-policy.generation", order, SettingApplicationTiming.NextEligibleOccurrence, new ThresholdCodec(),
        value => float.IsFinite(value) && value >= 0 && value <= 1 ? null : "Megamod_DrugGeneration_InvalidThreshold");

    private sealed class ThresholdCodec : ISettingCodec<float>
    {
        public string Encode(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        public bool TryDecode(string encoded, out float value) => float.TryParse(encoded, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private sealed class ToleranceCodec : ISettingCodec<DrugToleranceChoice>
    {
        public string Encode(DrugToleranceChoice value) => value.Encoding ?? "1:any";
        public bool TryDecode(string encoded, out DrugToleranceChoice value)
        {
            value = DrugToleranceChoice.Decode(encoded);
            return value.Kind != DrugToleranceChoiceKind.NeedsReview;
        }
    }
}
