namespace ImmersiveChefs;

[Flags]
public enum ContaminationSources
{
    None = 0,
    DirtyCookware = 1,
    DirtyPlate = 2,
    DirtyCutlery = 4,
    EmergencyUnplated = 8,
    WildWaterCookware = 16,
    WildWaterPlate = 32,
    WildWaterCutlery = 64
}

public readonly struct CulinaryServingSnapshot
{
    public CulinaryServingSnapshot(
        int schemaVersion,
        int qualityScore,
        float temperatureCelsius,
        ContaminationSources contamination,
        int microwaveReheatCount,
        int lastThermalTick)
    {
        SchemaVersion = schemaVersion;
        QualityScore = qualityScore;
        TemperatureCelsius = temperatureCelsius;
        Contamination = contamination;
        MicrowaveReheatCount = microwaveReheatCount;
        LastThermalTick = lastThermalTick;
    }

    public int SchemaVersion { get; }
    public int QualityScore { get; }
    public float TemperatureCelsius { get; }
    public ContaminationSources Contamination { get; }
    public int MicrowaveReheatCount { get; }
    public int LastThermalTick { get; }
}

public sealed class CulinaryServingRecord
{
    public const int CurrentSchemaVersion = 1;

    public CulinaryServingRecord(
        int qualityScore,
        float temperatureCelsius,
        ContaminationSources contamination,
        int microwaveReheatCount,
        int lastThermalTick)
    {
        QualityScore = Math.Max(0, Math.Min(100, qualityScore));
        TemperatureCelsius = temperatureCelsius;
        Contamination = contamination;
        MicrowaveReheatCount = Math.Max(0, microwaveReheatCount);
        LastThermalTick = Math.Max(0, lastThermalTick);
    }

    public int QualityScore { get; private set; }
    public float TemperatureCelsius { get; private set; }
    public ContaminationSources Contamination { get; private set; }
    public int MicrowaveReheatCount { get; private set; }
    public int LastThermalTick { get; private set; }

    public CulinaryServingSnapshot Capture()
    {
        return new CulinaryServingSnapshot(
            CurrentSchemaVersion,
            QualityScore,
            TemperatureCelsius,
            Contamination,
            MicrowaveReheatCount,
            LastThermalTick);
    }

    public static CulinaryServingRecord Restore(CulinaryServingSnapshot snapshot)
    {
        return new CulinaryServingRecord(
            snapshot.QualityScore,
            snapshot.TemperatureCelsius,
            snapshot.Contamination,
            snapshot.MicrowaveReheatCount,
            snapshot.LastThermalTick);
    }

    public void Reheat(float targetTemperature, int qualityLoss, int currentTick)
    {
        TemperatureCelsius = targetTemperature;
        QualityScore = Math.Max(0, QualityScore - Math.Max(0, qualityLoss));
        MicrowaveReheatCount++;
        LastThermalTick = Math.Max(0, currentTick);
    }

    public void AddContamination(ContaminationSources sources)
    {
        Contamination |= sources;
    }

    public void AdvanceTemperature(float ambientTemperature, int currentTick, float halfLifeHours)
    {
        var boundedTick = Math.Max(0, currentTick);
        var elapsed = Math.Max(0, boundedTick - LastThermalTick);
        if (elapsed > 0)
        {
            TemperatureCelsius = ThermalCalculator.TemperatureAfter(
                TemperatureCelsius,
                ambientTemperature,
                elapsed,
                halfLifeHours);
        }

        LastThermalTick = boundedTick;
    }
}
