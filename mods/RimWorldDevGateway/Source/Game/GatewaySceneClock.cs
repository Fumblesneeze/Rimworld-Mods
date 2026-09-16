namespace RimWorldDevGateway;

public sealed class GatewaySceneClockSnapshot
{
    public GatewaySceneClockSnapshot(string mapHandle, int gameStartAbsTick, int ticksGame, int localDayTick)
    {
        MapHandle = mapHandle;
        GameStartAbsTick = gameStartAbsTick;
        TicksGame = ticksGame;
        LocalDayTick = localDayTick;
    }
    public string MapHandle { get; }
    public int GameStartAbsTick { get; }
    public int TicksGame { get; }
    public int LocalDayTick { get; }
}

public interface IGatewaySceneClockWorld
{
    GatewaySceneClockSnapshot Capture();
    void SetGameStartAbsTick(int value);
}

public sealed class GatewaySceneClockResult
{
    public GatewaySceneClockResult(GatewaySceneClockSnapshot before, GatewaySceneClockSnapshot after)
    {
        Before = before;
        After = after;
    }
    public GatewaySceneClockSnapshot Before { get; }
    public GatewaySceneClockSnapshot After { get; }
}

public sealed class GatewaySceneClockController
{
    private readonly IGatewaySceneClockWorld world;
    public GatewaySceneClockController(IGatewaySceneClockWorld world) =>
        this.world = world ?? throw new ArgumentNullException(nameof(world));

    public GatewaySceneClockSnapshot Capture() => world.Capture();

    public GatewaySceneClockResult Apply(string mapHandle, int? minuteOfDay, int? gameStartAbsTick)
    {
        if (string.IsNullOrWhiteSpace(mapHandle) || mapHandle.Length > 256 ||
            minuteOfDay.HasValue == gameStartAbsTick.HasValue || minuteOfDay < 0 || minuteOfDay > 1439)
            throw new GatewayAutomationException("invalid_scene_time",
                "Specify a current map handle and exactly one of minuteOfDay (0–1439) or gameStartAbsTick.");
        var before = world.Capture();
        if (!string.Equals(mapHandle, before.MapHandle, StringComparison.Ordinal))
            throw new GatewayAutomationException("stale_map_handle", "The requested map is no longer current.");
        // Integer rounding to the nearest of RimWorld's 60,000 ticks per day.
        var target = gameStartAbsTick.HasValue ? (long)gameStartAbsTick.Value :
            (long)before.GameStartAbsTick + (minuteOfDay!.Value * 60000L + 720) / 1440 - before.LocalDayTick;
        if (target == 0 || target < int.MinValue || target > int.MaxValue ||
            target + before.TicksGame < 0 || target + before.TicksGame > int.MaxValue)
            throw new GatewayAutomationException("invalid_scene_time", "The requested absolute game clock is out of range.");
        try
        {
            world.SetGameStartAbsTick((int)target);
            return new GatewaySceneClockResult(before, world.Capture());
        }
        catch
        {
            world.SetGameStartAbsTick(before.GameStartAbsTick);
            throw;
        }
    }
}
