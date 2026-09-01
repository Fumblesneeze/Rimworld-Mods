using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmersiveSignalFire.Effects;

public readonly struct MorseWindow
{
    public MorseWindow(int startTick, int duration)
    {
        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (duration <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        StartTick = startTick;
        Duration = duration;
    }

    public int StartTick { get; }

    public int Duration { get; }

    public int EndTickExclusive => checked(StartTick + Duration);

    public bool Contains(int elapsedTick) =>
        elapsedTick >= StartTick && elapsedTick < EndTickExclusive;
}

public static class MorseCadence
{
    public const int TotalDurationTicks = 600;
    public const int PuffDurationTicks = 21;
    public const float MinimumSmokeSpeed = 0.25f;
    public const float MaximumSmokeSpeed = 0.75f;

    private static readonly MorseWindow[] Windows =
    {
        new(0, PuffDurationTicks),
        new(60, PuffDurationTicks),
        new(120, PuffDurationTicks),
        new(360, PuffDurationTicks),
        new(420, PuffDurationTicks),
        new(480, PuffDurationTicks),
    };

    public static IReadOnlyList<MorseWindow> OnWindows => Windows;

    public static bool IsSmokeOn(int elapsedTick) =>
        elapsedTick >= 0 &&
        elapsedTick < TotalDurationTicks &&
        Windows.Any(window => window.Contains(elapsedTick));

    public static bool IsPuffStart(int elapsedTick) =>
        elapsedTick >= 0 &&
        elapsedTick < TotalDurationTicks &&
        Windows.Any(window => window.StartTick == elapsedTick);

    internal static bool TryGetActivePuffStart(int elapsedTick, out int puffStartTick)
    {
        MorseWindow active = Windows.FirstOrDefault(window => window.Contains(elapsedTick));
        if (active.Duration > 0)
        {
            puffStartTick = active.StartTick;
            return true;
        }

        puffStartTick = -1;
        return false;
    }
}
