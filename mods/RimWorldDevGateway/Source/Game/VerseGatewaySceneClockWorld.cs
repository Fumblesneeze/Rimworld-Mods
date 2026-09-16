using Verse;

namespace RimWorldDevGateway;

/// <summary>Disposable scene setup; callers must use the Unity-thread dispatcher.</summary>
public sealed class VerseGatewaySceneClockWorld : IGatewaySceneClockWorld
{
    public GatewaySceneClockSnapshot Capture()
    {
        var map = RequireMap();
        var ticks = Find.TickManager;
        return new GatewaySceneClockSnapshot("map-" + map.uniqueID, ticks.gameStartAbsTick,
            ticks.TicksGame, GenLocalDate.DayTick(map));
    }

    public void SetGameStartAbsTick(int value)
    {
        var map = RequireMap();
        Find.TickManager.gameStartAbsTick = value;
        map.weatherManager.ResetSkyTargetLerpCache();
    }

    private static Map RequireMap()
    {
        if (Current.ProgramState != ProgramState.Playing || Current.Game?.CurrentMap is not { } map ||
            !Current.Game.PlayerHasControl || LongEventHandler.AnyEventNowOrWaiting)
            throw new GatewayAutomationException("map_unavailable", "Scene time requires a settled, playable current map.");
        return map;
    }
}
