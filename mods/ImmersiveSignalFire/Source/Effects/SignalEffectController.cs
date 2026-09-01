using Blues;
using ImmersiveSignalFire.Buildings;

namespace ImmersiveSignalFire.Effects;

internal sealed class SignalEffectController
{
    private readonly Building_SignalFire fire;
    private readonly SmokeRunnerLifecycle smokeLifecycle = new();
    private EffectRunner? flameRunner;
    private EffectRunner? smokeRunner;

    public SignalEffectController(Building_SignalFire fire)
    {
        this.fire = fire;
    }

    public void Tick(int elapsedTicks)
    {
        if (flameRunner is null || flameRunner.IsDestroyed)
        {
            flameRunner = EffectRunner.CreateAttached(fire, SignalFireDefOf.ImmersiveSignalFire_Flame, 1f);
        }

        flameRunner.Tick();
        smokeLifecycle.Advance(elapsedTicks, StopSmokeRunner, StartSmokeRunner, TickSmokeRunner);
    }

    public void Stop()
    {
        if (flameRunner is not null && !flameRunner.IsDestroyed)
        {
            flameRunner.Kill();
        }

        smokeLifecycle.Stop(StopSmokeRunner);

        flameRunner = null;
    }

    private void StartSmokeRunner()
    {
        smokeRunner = EffectRunner.CreateAttached(
            fire,
            SignalFireDefOf.ImmersiveSignalFire_DarkSmokePulse,
            1f);
    }

    private void StopSmokeRunner()
    {
        if (smokeRunner is not null && !smokeRunner.IsDestroyed)
        {
            smokeRunner.Kill();
        }

        smokeRunner = null;
    }

    private void TickSmokeRunner()
    {
        if (smokeRunner is not null && !smokeRunner.IsDestroyed)
        {
            smokeRunner.Tick();
        }
    }
}
