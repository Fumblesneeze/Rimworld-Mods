using System;

namespace ImmersiveSignalFire.Effects;

internal sealed class SmokeRunnerLifecycle
{
    private int? startTick;
    private bool terminallyStopped;

    public void Advance(int elapsedTick, Action stop, Action start, Action tick)
    {
        if (terminallyStopped)
        {
            return;
        }

        if (startTick.HasValue && elapsedTick - startTick.Value >= MorseCadence.PuffDurationTicks)
        {
            stop();
            startTick = null;
        }

        if (!startTick.HasValue &&
            MorseCadence.TryGetActivePuffStart(elapsedTick, out int activePuffStartTick))
        {
            start();
            startTick = activePuffStartTick;
        }

        if (startTick.HasValue)
        {
            tick();
        }
    }

    public void Stop(Action stop)
    {
        terminallyStopped = true;
        if (!startTick.HasValue)
        {
            return;
        }

        stop();
        startTick = null;
    }
}
