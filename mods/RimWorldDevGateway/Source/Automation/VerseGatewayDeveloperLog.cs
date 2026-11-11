using System.Reflection;
using LudeonTK;
using Verse;

namespace RimWorldDevGateway;

internal static class VerseGatewayDeveloperLog
{
    private static readonly MethodInfo? Toggle = typeof(DebugWindowsOpener).GetMethod("ToggleLogWindow",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

    public static void Register(GatewayAutomationRegistry registry) => GatewayDeveloperLogAutomation.Register(
        registry, Read, ToggleNative, Availability);

    private static GatewayAutomationAvailability Availability()
    {
#if RIMWORLD_1_7
        return new GatewayAutomationAvailability(Toggle is { ReturnType: var result, IsGenericMethod: false,
            ContainsGenericParameters: false } && result == typeof(void) && Toggle.GetMethodBody() != null,
            "native_log_command_unavailable");
#else
        return new GatewayAutomationAvailability(false, "native_log_runtime_unverified");
#endif
    }

    private static GatewayDeveloperLogState Read()
    {
        RequireMainThread();
        var stack = Find.WindowStack;
        bool ready = PlayDataLoader.Loaded && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.UIRoot?.debugWindowOpener != null && stack != null;
        return GatewayDeveloperLogState.FromOpenWindowTypes(ready, Prefs.DevMode, typeof(EditWindow_Log),
            stack?.Windows.Where(window => window.IsOpen).Select(window => window.GetType()) ?? Enumerable.Empty<Type>());
    }

    private static void ToggleNative()
    {
        RequireMainThread();
        if (!Availability().Available || Find.UIRoot?.debugWindowOpener is not { } opener)
            throw new GatewayAutomationException("native_log_command_unavailable", "The native log toolbar command is unavailable.");
        Toggle!.Invoke(opener, null);
    }

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread)
            throw new GatewayAutomationException("native_log_wrong_thread", "Native log commands require the game thread.");
    }
}
