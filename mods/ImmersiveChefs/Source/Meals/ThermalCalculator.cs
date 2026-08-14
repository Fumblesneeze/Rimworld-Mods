namespace ImmersiveChefs;

public enum ThermalBand
{
    SteamingHot,
    Warm,
    RoomTemperature,
    Cold,
    Frozen
}

public static class ThermalCalculator
{
    public const int TicksPerHour = 2500;

    public static float TemperatureAfter(
        float currentTemperature,
        float ambientTemperature,
        int elapsedTicks,
        float baseHalfLifeHours)
    {
        if (elapsedTicks <= 0)
        {
            return currentTemperature;
        }

        if (currentTemperature <= 0f && ambientTemperature > 0f)
        {
            var thawHalfLifeTicks = Math.Max(1f, baseHalfLifeHours * TicksPerHour * 2f);
            var factorAtZero = ambientTemperature / (ambientTemperature - currentTemperature);
            var ticksToZero = factorAtZero >= 1f
                ? 0d
                : -thawHalfLifeTicks * (Math.Log(factorAtZero) / Math.Log(2d));
            if (elapsedTicks <= ticksToZero)
            {
                return Approach(
                    currentTemperature,
                    ambientTemperature,
                    elapsedTicks,
                    thawHalfLifeTicks);
            }

            var remainingTicks = Math.Max(0d, elapsedTicks - ticksToZero);
            var ambientRate = ambientTemperature <= 10f ? 2f : 1f;
            var postThawHalfLifeTicks = Math.Max(
                1f,
                baseHalfLifeHours * TicksPerHour / ambientRate);
            return Approach(0f, ambientTemperature, remainingTicks, postThawHalfLifeTicks);
        }

        var rate = ambientTemperature <= 0f ? 4f : ambientTemperature <= 10f ? 2f : 1f;
        var effectiveHalfLifeTicks = Math.Max(1f, baseHalfLifeHours * TicksPerHour / rate);
        return Approach(currentTemperature, ambientTemperature, elapsedTicks, effectiveHalfLifeTicks);
    }

    public static ThermalBand BandFor(float temperature)
    {
        if (temperature >= 55f) return ThermalBand.SteamingHot;
        if (temperature >= 35f) return ThermalBand.Warm;
        if (temperature >= 15f) return ThermalBand.RoomTemperature;
        if (temperature > 0f) return ThermalBand.Cold;
        return ThermalBand.Frozen;
    }

    public static int MoodOffsetFor(float temperature)
    {
        return MoodOffsetFor(BandFor(temperature));
    }

    public static int MoodOffsetFor(ThermalBand band)
    {
        return band switch
        {
            ThermalBand.SteamingHot => 2,
            ThermalBand.Warm => 1,
            ThermalBand.Cold => -3,
            ThermalBand.Frozen => -10,
            _ => 0
        };
    }

    public static float EatingDurationMultiplier(ThermalBand band) =>
        band == ThermalBand.Frozen ? 1.5f : 1f;

    public static int MicrowaveQualityLoss(int baseQualityLoss, float sourceTemperatureCelsius)
    {
        var boundedBase = Math.Max(0, baseQualityLoss);
        if (float.IsNaN(sourceTemperatureCelsius) || sourceTemperatureCelsius >= 15f)
        {
            return boundedBase;
        }

        var depthPenalty = float.IsNegativeInfinity(sourceTemperatureCelsius)
            ? 10
            : (int)Math.Ceiling((15f - sourceTemperatureCelsius) / 5f);
        return boundedBase + Math.Max(0, Math.Min(10, depthPenalty));
    }

    private static float Approach(
        float currentTemperature,
        float ambientTemperature,
        double elapsedTicks,
        float effectiveHalfLifeTicks)
    {
        var factor = Math.Pow(2d, -elapsedTicks / effectiveHalfLifeTicks);
        return ambientTemperature + ((currentTemperature - ambientTemperature) * (float)factor);
    }
}
