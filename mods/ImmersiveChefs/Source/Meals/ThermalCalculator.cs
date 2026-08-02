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

        var rate = ambientTemperature <= 0f ? 4f : ambientTemperature <= 10f ? 2f : 1f;
        var effectiveHalfLifeTicks = Math.Max(1f, baseHalfLifeHours * TicksPerHour / rate);
        var factor = Math.Pow(2d, -elapsedTicks / effectiveHalfLifeTicks);
        return ambientTemperature + ((currentTemperature - ambientTemperature) * (float)factor);
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
        return BandFor(temperature) switch
        {
            ThermalBand.SteamingHot => 2,
            ThermalBand.Warm => 1,
            ThermalBand.Cold => -3,
            ThermalBand.Frozen => -6,
            _ => 0
        };
    }
}
