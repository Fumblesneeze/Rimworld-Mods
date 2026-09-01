using System;
using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;

namespace ImmersiveSignalFire.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-signal-fire.smoke-visual",
    "fumblesneeze.immersivesignalfire",
    "brrainz.harmony",
    "ludeon.rimworld",
    "blues.forge",
    "meathax.showmeyourtools",
    "fumblesneeze.immersivesignalfire",
    MaxFrames = 8_000,
    MaxGameTicks = 4_000,
    MaxWallClockSeconds = 300)]
public sealed class SignalFireSmokeVisualWorkflowTests : IRimWorldEndToEndTest
{
    private const string TerminalVisualStep =
        "unnoticed single-use fire is gone with cleanable soot left behind";
    private const string NextUnrelatedFixtureStep =
        "arrange a fresh prepared fire and the misunderstood quality band";

    private readonly SignalFirePlayerWorkflowTests workflow = new();

    public void Arrange(IEndToEndContext context) => workflow.Arrange(context);

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        IEnumerator<EndToEndStep> steps = workflow.Execute(context);
        try
        {
            while (steps.MoveNext())
            {
                EndToEndStep step = steps.Current;
                if (string.Equals(step.Name, NextUnrelatedFixtureStep, StringComparison.Ordinal))
                {
                    throw new EndToEndAssertionException(
                        $"The smoke-visual cutoff '{TerminalVisualStep}' was not reached before the next fixture.");
                }

                yield return step;
                if (string.Equals(step.Name, TerminalVisualStep, StringComparison.Ordinal))
                {
                    yield break;
                }
            }

            throw new EndToEndAssertionException(
                $"The delegated player workflow ended without the smoke-visual cutoff '{TerminalVisualStep}'.");
        }
        finally
        {
            steps.Dispose();
        }
    }
}
