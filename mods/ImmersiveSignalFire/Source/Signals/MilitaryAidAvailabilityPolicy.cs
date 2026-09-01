using System;

namespace ImmersiveSignalFire.Signals;

public static class MilitaryAidAvailabilityPolicy
{
    public const int CooldownTicks = 60_000;

    public static int RemainingCooldownTicks(int lastRequestTick, int currentTick)
    {
        long remaining = (long)lastRequestTick + CooldownTicks - currentTick;
        return remaining <= 0 ? 0 : (int)Math.Min(int.MaxValue, remaining);
    }
}
