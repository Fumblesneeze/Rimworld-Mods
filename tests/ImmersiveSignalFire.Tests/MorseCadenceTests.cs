using System.Linq;
using System.Collections.Generic;
using ImmersiveSignalFire.Effects;
using NUnit.Framework;

namespace ImmersiveSignalFire.Tests;

[TestFixture]
public sealed class MorseCadenceTests
{
    [Test]
    public void MorseLikeCadenceUsesTwoGroupsOfThreePeriodicPuffsWithAReadableGroupBreak()
    {
        MorseWindow[] windows = MorseCadence.OnWindows.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(windows, Has.Length.EqualTo(6));
            Assert.That(windows.Select(window => window.Duration),
                Is.All.EqualTo(MorseCadence.PuffDurationTicks));
            Assert.That(windows.Select(window => window.StartTick), Is.EqualTo(new[]
            {
                0, 60, 120, 360, 420, 480,
            }));
            Assert.That(windows.First().StartTick, Is.EqualTo(0));
            Assert.That(windows.Last().EndTickExclusive, Is.EqualTo(501));
            Assert.That(MorseCadence.TotalDurationTicks, Is.EqualTo(600));
        });
    }

    [TestCase(0, true)]
    [TestCase(19, true)]
    [TestCase(20, true)]
    [TestCase(21, false)]
    [TestCase(40, false)]
    [TestCase(59, false)]
    [TestCase(60, true)]
    [TestCase(80, true)]
    [TestCase(81, false)]
    [TestCase(120, true)]
    [TestCase(140, true)]
    [TestCase(141, false)]
    [TestCase(359, false)]
    [TestCase(360, true)]
    [TestCase(479, false)]
    [TestCase(480, true)]
    [TestCase(500, true)]
    [TestCase(501, false)]
    [TestCase(599, false)]
    public void BlanketAndEmitterShareTheBoundedPuffPhase(int elapsedTick, bool expected)
    {
        Assert.That(MorseCadence.IsSmokeOn(elapsedTick), Is.EqualTo(expected));
    }

    [TestCase(360, true)]
    [TestCase(380, true)]
    [TestCase(381, false)]
    [TestCase(419, false)]
    [TestCase(420, true)]
    public void BlanketsLowerExactlyWhenEachPlumeEmitterStops(
        int elapsedTick,
        bool expected)
    {
        Assert.That(MorseCadence.IsSmokeOn(elapsedTick), Is.EqualTo(expected));
    }

    [Test]
    public void WindSpeedReferenceIsPinnedToInspectedSimpleFxClamp()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MorseCadence.MinimumSmokeSpeed, Is.EqualTo(0.25f));
            Assert.That(MorseCadence.MaximumSmokeSpeed, Is.EqualTo(0.75f));
        });
    }

    [Test]
    public void MorseLikeSignalProducesTwoGroupsOfThreePeriodicPlumeStarts()
    {
        int[] puffStarts = Enumerable.Range(0, MorseCadence.TotalDurationTicks)
            .Where(MorseCadence.IsPuffStart)
            .ToArray();

        Assert.That(puffStarts, Is.EqualTo(new[]
        {
            0, 60, 120, 360, 420, 480,
        }));
    }

    [Test]
    public void EveryPlumeRestoresTheEarlierTwentyOneTickEmission()
    {
        Assert.That(MorseCadence.PuffDurationTicks, Is.EqualTo(21));
    }

    [Test]
    public void SmokeRunnerLifecycleCreatesSixNonOverlappingPlumesAndStopsBeforeTickTwentyOne()
    {
        var lifecycle = new SmokeRunnerLifecycle();
        var starts = new List<int>();
        var stops = new List<int>();
        var ticks = new List<int>();
        int liveRunners = 0;
        int maximumLiveRunners = 0;
        int elapsedTick = -1;

        for (elapsedTick = 0; elapsedTick < MorseCadence.TotalDurationTicks; elapsedTick++)
        {
            lifecycle.Advance(
                elapsedTick,
                stop: () =>
                {
                    Assert.That(liveRunners, Is.EqualTo(1), "Only a live smoke runner may be stopped.");
                    stops.Add(elapsedTick);
                    liveRunners--;
                },
                start: () =>
                {
                    Assert.That(liveRunners, Is.Zero, "A new plume must not overlap an emitter.");
                    starts.Add(elapsedTick);
                    liveRunners++;
                    maximumLiveRunners = System.Math.Max(maximumLiveRunners, liveRunners);
                },
                tick: () =>
                {
                    Assert.That(liveRunners, Is.EqualTo(1), "Only the current smoke runner may tick.");
                    ticks.Add(elapsedTick);
                });
        }

        lifecycle.Stop(() => liveRunners--);

        Assert.Multiple(() =>
        {
            Assert.That(starts, Is.EqualTo(new[] { 0, 60, 120, 360, 420, 480 }));
            Assert.That(stops, Is.EqualTo(new[] { 21, 81, 141, 381, 441, 501 }));
            Assert.That(ticks, Has.Count.EqualTo(6 * MorseCadence.PuffDurationTicks));
            Assert.That(ticks, Does.Contain(20));
            Assert.That(ticks, Does.Not.Contain(21), "The first runner must stop before a tick at the exclusive boundary.");
            Assert.That(maximumLiveRunners, Is.EqualTo(1));
            Assert.That(liveRunners, Is.Zero);
        });
    }

    [Test]
    public void TerminalStopKillsAnActivePlumeOnceAndPreventsFurtherTicks()
    {
        var lifecycle = new SmokeRunnerLifecycle();
        int liveRunners = 0;
        int stops = 0;
        int ticks = 0;

        lifecycle.Advance(
            360,
            stop: () =>
            {
                stops++;
                liveRunners--;
            },
            start: () => liveRunners++,
            tick: () => ticks++);

        lifecycle.Stop(() =>
        {
            stops++;
            liveRunners--;
        });
        lifecycle.Stop(() => stops++);
        lifecycle.Advance(241, () => stops++, () => liveRunners++, () => ticks++);

        Assert.Multiple(() =>
        {
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(liveRunners, Is.Zero);
            Assert.That(ticks, Is.EqualTo(1), "Terminal cleanup must prevent the active runner from ticking again.");
        });
    }

    [Test]
    public void ReloadMidPuffReconstructsEmitterOnlyUntilTheOriginalBoundary()
    {
        var reloadedLifecycle = new SmokeRunnerLifecycle();
        var starts = new List<int>();
        var stops = new List<int>();
        var ticks = new List<int>();
        int elapsedTick = 370;

        reloadedLifecycle.Advance(
            elapsedTick,
            () => stops.Add(elapsedTick),
            () => starts.Add(elapsedTick),
            () => ticks.Add(elapsedTick));
        elapsedTick = 380;
        reloadedLifecycle.Advance(
            elapsedTick,
            () => stops.Add(elapsedTick),
            () => starts.Add(elapsedTick),
            () => ticks.Add(elapsedTick));
        elapsedTick = 381;
        reloadedLifecycle.Advance(
            elapsedTick,
            () => stops.Add(elapsedTick),
            () => starts.Add(elapsedTick),
            () => ticks.Add(elapsedTick));

        Assert.Multiple(() =>
        {
            Assert.That(starts, Is.EqualTo(new[] { 370 }),
                "A fresh post-load lifecycle must reconstruct the already-active scheduled puff.");
            Assert.That(ticks, Is.EqualTo(new[] { 370, 380 }));
            Assert.That(stops, Is.EqualTo(new[] { 381 }),
                "Reconstruction must preserve the scheduled start tick instead of extending the puff after load.");
        });
    }
}
