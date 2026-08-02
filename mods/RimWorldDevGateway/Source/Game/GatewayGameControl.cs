using RimWorld;
using RimWorldDevGateway.Contracts;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewayGameControlException : Exception
{
    public GatewayGameControlException(string code, string message)
        : base(message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; }
}

public interface IGatewayGameControlOperations
{
    bool DevMode { get; }

    bool GodMode { get; }

    bool EffectiveGodMode { get; }

    bool DevModePermanentlyDisabled { get; }

    bool GameAvailable { get; }

    bool Paused { get; }

    bool ForcePaused { get; }

    GatewayGameSpeed Speed { get; }

    void SetDevMode(bool enabled);

    void SetGodMode(bool enabled);

    void SetPaused(bool paused);

    void SetSpeed(GatewayGameSpeed speed);
}

public sealed class GatewayGameStateSnapshot
{
    public GatewayGameStateSnapshot(
        bool devMode,
        bool godMode,
        bool effectiveGodMode,
        bool devModePermanentlyDisabled,
        bool? paused,
        bool? forcePaused,
        GatewayGameSpeed? speed)
    {
        DevMode = devMode;
        GodMode = godMode;
        EffectiveGodMode = effectiveGodMode;
        DevModePermanentlyDisabled = devModePermanentlyDisabled;
        Paused = paused;
        ForcePaused = forcePaused;
        Speed = speed;
    }

    public bool DevMode { get; }

    public bool GodMode { get; }

    public bool EffectiveGodMode { get; }

    public bool DevModePermanentlyDisabled { get; }

    public bool? Paused { get; }

    public bool? ForcePaused { get; }

    public GatewayGameSpeed? Speed { get; }
}

public sealed class GatewayGameStateMutationResult
{
    public GatewayGameStateMutationResult(
        GatewayGameStateSnapshot before,
        GatewayGameStateSnapshot after)
    {
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));
    }

    public GatewayGameStateSnapshot Before { get; }

    public GatewayGameStateSnapshot After { get; }
}

/// <summary>
/// Coordinates validated semantic state transitions. Callers must invoke this controller through
/// the gateway's main-thread dispatcher.
/// </summary>
public sealed class GatewayGameControlController
{
    private readonly IGatewayGameControlOperations operations;

    public GatewayGameControlController(IGatewayGameControlOperations operations)
    {
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    public GatewayGameStateSnapshot Capture()
    {
        var gameAvailable = operations.GameAvailable;
        return new GatewayGameStateSnapshot(
            operations.DevMode,
            operations.GodMode,
            operations.EffectiveGodMode,
            operations.DevModePermanentlyDisabled,
            gameAvailable ? operations.Paused : null,
            gameAvailable ? operations.ForcePaused : null,
            gameAvailable ? operations.Speed : null);
    }

    public GatewayGameStateMutationResult Mutate(GatewayGameStateMutationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!request.DevMode.HasValue &&
            !request.GodMode.HasValue &&
            !request.Paused.HasValue &&
            string.IsNullOrWhiteSpace(request.Speed))
        {
            throw new GatewayGameControlException(
                "empty_game_state_mutation",
                "At least one game-state field is required.");
        }

        var requestedSpeed = ParseSpeed(request.Speed);
        var finalDevMode = request.DevMode ?? operations.DevMode;
        var finalGodMode = request.GodMode ?? (finalDevMode ? operations.GodMode : false);
        if (finalGodMode && !finalDevMode)
        {
            throw new GatewayGameControlException(
                "invalid_game_state",
                "God mode cannot be enabled while developer mode is disabled.");
        }

        if (request.DevMode == true && operations.DevModePermanentlyDisabled)
        {
            throw new GatewayGameControlException(
                "dev_mode_permanently_disabled",
                "This RimWorld configuration has permanently disabled developer mode.");
        }

        if ((request.Paused.HasValue || requestedSpeed.HasValue) && !operations.GameAvailable)
        {
            throw new GatewayGameControlException(
                "game_unavailable",
                "Pause and speed controls require an active game.");
        }

        if (request.Paused.HasValue && requestedSpeed.HasValue &&
            request.Paused.Value != (requestedSpeed.Value == GatewayGameSpeed.Paused))
        {
            throw new GatewayGameControlException(
                "invalid_game_state",
                "The requested pause state conflicts with the requested speed.");
        }

        if (operations.GameAvailable && operations.ForcePaused &&
            (request.Paused == false ||
             (requestedSpeed.HasValue && requestedSpeed.Value != GatewayGameSpeed.Paused)))
        {
            throw new GatewayGameControlException(
                "game_force_paused",
                "RimWorld is force-paused by an active window or game state and cannot be unpaused semantically.");
        }

        var before = Capture();
        try
        {
            Apply(request, requestedSpeed, finalDevMode, finalGodMode);
            var after = Capture();
            VerifyRequestedState(request, requestedSpeed, finalDevMode, finalGodMode, after);
            return new GatewayGameStateMutationResult(before, after);
        }
        catch
        {
            Restore(before);
            throw;
        }
    }

    private void Apply(
        GatewayGameStateMutationRequest request,
        GatewayGameSpeed? requestedSpeed,
        bool finalDevMode,
        bool finalGodMode)
    {
        if (!finalDevMode && operations.GodMode != finalGodMode)
        {
            operations.SetGodMode(finalGodMode);
        }

        if (operations.DevMode != finalDevMode)
        {
            operations.SetDevMode(finalDevMode);
        }

        if (finalDevMode && operations.GodMode != finalGodMode)
        {
            operations.SetGodMode(finalGodMode);
        }

        if (requestedSpeed.HasValue && operations.Speed != requestedSpeed.Value)
        {
            operations.SetSpeed(requestedSpeed.Value);
        }
        else if (request.Paused.HasValue && operations.Paused != request.Paused.Value)
        {
            operations.SetPaused(request.Paused.Value);
        }
    }

    private static void VerifyRequestedState(
        GatewayGameStateMutationRequest request,
        GatewayGameSpeed? requestedSpeed,
        bool finalDevMode,
        bool finalGodMode,
        GatewayGameStateSnapshot after)
    {
        if (after.DevMode != finalDevMode ||
            after.GodMode != finalGodMode ||
            (request.Paused.HasValue && after.Paused != request.Paused.Value) ||
            (requestedSpeed.HasValue && after.Speed != requestedSpeed.Value))
        {
            throw new GatewayGameControlException(
                "game_state_rejected",
                "RimWorld rejected one or more requested game-state changes.");
        }
    }

    private void Restore(GatewayGameStateSnapshot snapshot)
    {
        try
        {
            if (snapshot.DevMode && !operations.DevMode)
            {
                operations.SetDevMode(true);
            }

            if (operations.GodMode != snapshot.GodMode)
            {
                operations.SetGodMode(snapshot.GodMode);
            }

            if (snapshot.Speed.HasValue && operations.GameAvailable &&
                operations.Speed != snapshot.Speed.Value)
            {
                operations.SetSpeed(snapshot.Speed.Value);
            }

            if (snapshot.Paused.HasValue && operations.GameAvailable &&
                !operations.ForcePaused && operations.Paused != snapshot.Paused.Value)
            {
                operations.SetPaused(snapshot.Paused.Value);
            }

            if (!snapshot.DevMode && operations.DevMode)
            {
                operations.SetDevMode(false);
            }
        }
        catch
        {
            // Preserve the original transition failure; rollback is best-effort in a mutable game host.
        }
    }

    private static GatewayGameSpeed? ParseSpeed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.GetNames(typeof(GatewayGameSpeed)).Any(name =>
                string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) &&
            Enum.TryParse(value, ignoreCase: true, out GatewayGameSpeed speed) &&
            Enum.IsDefined(typeof(GatewayGameSpeed), speed))
        {
            return speed;
        }

        throw new GatewayGameControlException(
            "invalid_game_speed",
            "Speed must be Paused, Normal, Fast, Superfast, or Ultrafast.");
    }
}

public sealed class VerseGatewayGameControlOperations : IGatewayGameControlOperations
{
    public bool DevMode => Prefs.DevMode;

    public bool GodMode => DebugSettings.godMode;

    public bool EffectiveGodMode => DebugSettings.ShowDevGizmos;

    public bool DevModePermanentlyDisabled => DevModePermanentlyDisabledUtility.Disabled;

    public bool GameAvailable => Current.Game?.tickManager is not null;

    public bool Paused => RequireTickManager().Paused;

    public bool ForcePaused => RequireTickManager().ForcePaused;

    public GatewayGameSpeed Speed => FromVerseSpeed(RequireTickManager().CurTimeSpeed);

    public void SetDevMode(bool enabled)
    {
        Prefs.DevMode = enabled;
    }

    public void SetGodMode(bool enabled)
    {
        DebugSettings.godMode = enabled;
    }

    public void SetPaused(bool paused)
    {
        var tickManager = RequireTickManager();
        if (paused)
        {
            tickManager.Pause();
            return;
        }

        if (tickManager.ForcePaused)
        {
            throw new GatewayGameControlException(
                "game_force_paused",
                "RimWorld is force-paused and cannot currently be unpaused.");
        }

        if (tickManager.Paused)
        {
            tickManager.TogglePaused();
        }
    }

    public void SetSpeed(GatewayGameSpeed speed)
    {
        var tickManager = RequireTickManager();
        if (tickManager.ForcePaused && speed != GatewayGameSpeed.Paused)
        {
            throw new GatewayGameControlException(
                "game_force_paused",
                "RimWorld is force-paused and cannot currently be unpaused.");
        }

        tickManager.CurTimeSpeed = ToVerseSpeed(speed);
    }

    private static TickManager RequireTickManager() =>
        Current.Game?.tickManager ?? throw new GatewayGameControlException(
            "game_unavailable",
            "No active RimWorld game is available.");

    private static GatewayGameSpeed FromVerseSpeed(TimeSpeed speed) => speed switch
    {
        TimeSpeed.Paused => GatewayGameSpeed.Paused,
        TimeSpeed.Normal => GatewayGameSpeed.Normal,
        TimeSpeed.Fast => GatewayGameSpeed.Fast,
        TimeSpeed.Superfast => GatewayGameSpeed.Superfast,
        TimeSpeed.Ultrafast => GatewayGameSpeed.Ultrafast,
        _ => throw new ArgumentOutOfRangeException(nameof(speed))
    };

    private static TimeSpeed ToVerseSpeed(GatewayGameSpeed speed) => speed switch
    {
        GatewayGameSpeed.Paused => TimeSpeed.Paused,
        GatewayGameSpeed.Normal => TimeSpeed.Normal,
        GatewayGameSpeed.Fast => TimeSpeed.Fast,
        GatewayGameSpeed.Superfast => TimeSpeed.Superfast,
        GatewayGameSpeed.Ultrafast => TimeSpeed.Ultrafast,
        _ => throw new ArgumentOutOfRangeException(nameof(speed))
    };
}
