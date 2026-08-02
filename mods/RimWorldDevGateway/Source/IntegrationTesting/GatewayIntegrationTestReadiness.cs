using System;
using System.Collections.Generic;
using System.Threading;
using RimWorldDevGateway.IntegrationTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayIntegrationTestLifecycleProbe
{
    bool IsMainThread { get; }
    int FrameCount { get; }
    bool PlayDataLoaded { get; }
    bool NoLongEvent { get; }
    bool EntryProgramState { get; }
    bool EntryRootPresent { get; }
    bool UiRootPresent { get; }
    bool WindowStackPresent { get; }
    bool PlayingProgramState { get; }
    bool CurrentMapPresent { get; }
}

public sealed class GatewayIntegrationTestStableReadiness : IGatewayIntegrationTestReadiness
{
    private readonly IGatewayIntegrationTestLifecycleProbe probe;
    private readonly Dictionary<RunAt, int> firstReadyFrames = new();

    public GatewayIntegrationTestStableReadiness(IGatewayIntegrationTestLifecycleProbe probe)
    {
        this.probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    public bool IsMainThread => probe.IsMainThread;

    public bool IsReady(RunAt lifecyclePoint)
    {
        if (!Enum.IsDefined(typeof(RunAt), lifecyclePoint))
        {
            throw new ArgumentOutOfRangeException(nameof(lifecyclePoint));
        }

        var condition = lifecyclePoint switch
        {
            RunAt.MainMenuLoaded =>
                probe.PlayDataLoaded &&
                probe.NoLongEvent &&
                probe.EntryProgramState &&
                probe.EntryRootPresent &&
                probe.UiRootPresent &&
                probe.WindowStackPresent,
            RunAt.PlayableMapLoaded =>
                probe.PlayDataLoaded &&
                probe.NoLongEvent &&
                probe.PlayingProgramState &&
                probe.CurrentMapPresent,
            _ => false
        };
        if (!condition)
        {
            firstReadyFrames.Remove(lifecyclePoint);
            return false;
        }

        var frame = probe.FrameCount;
        if (!firstReadyFrames.TryGetValue(lifecyclePoint, out var firstFrame) || frame < firstFrame)
        {
            firstReadyFrames[lifecyclePoint] = frame;
            return false;
        }

        return frame > firstFrame;
    }
}

public sealed class VerseGatewayIntegrationTestLifecycleProbe : IGatewayIntegrationTestLifecycleProbe
{
    private readonly int mainThreadId;

    public VerseGatewayIntegrationTestLifecycleProbe()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

    public int FrameCount => Time.frameCount;

    public bool PlayDataLoaded => PlayDataLoader.Loaded;

    public bool NoLongEvent => !LongEventHandler.AnyEventNowOrWaiting;

    public bool EntryProgramState => Current.ProgramState == ProgramState.Entry;

    public bool EntryRootPresent => Current.Root_Entry is not null;

    public bool UiRootPresent => Find.UIRoot is not null;

    public bool WindowStackPresent => Find.WindowStack is not null;

    public bool PlayingProgramState => Current.ProgramState == ProgramState.Playing;

    public bool CurrentMapPresent => Find.CurrentMap is not null;
}
