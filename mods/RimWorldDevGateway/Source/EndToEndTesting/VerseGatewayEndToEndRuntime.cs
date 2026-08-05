using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public sealed class VerseGatewayEndToEndClock : IGatewayEndToEndClock
{
    public long FrameCount => Time.frameCount;

    public int GameTick => Current.Game?.tickManager?.TicksGame ?? 0;

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class VerseGatewayEndToEndExecutionReadiness : IGatewayEndToEndExecutionReadiness
{
    public bool IsPlayableMapReady() =>
        Current.ProgramState == ProgramState.Playing &&
        Current.Game?.CurrentMap is not null &&
        Find.CameraDriver is not null &&
        Find.Selector is not null &&
        Find.WindowStack is not null;
}
