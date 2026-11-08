#if RIMWORLD_1_7
using System.Reflection;
using UnityStubs.Rendering;
using Verse;

namespace RimWorldDevGateway;

// Inspected 1.7 native contract: ScreenCaptureFeature exposes _readable only during
// rendered-frame completion; TryReadScreen settles and requires Recorded before readback.
// Never use ScreenCapture.CaptureScreenshotAsTexture here: it manufactures an empty
// texture when that contract is unavailable (including Update-driven EOF callbacks).
public sealed class WaylithGatewayScreenshotReadback : IGatewayScreenshotReadbackSource
{
    public const string ProviderName = "RimWorldWaylith.Rendering.ScreenCaptureFeature";
    public string Provider => ProviderName;
    public int CurrentFrame
    {
        get { RequireMainThread(); return UnityEngine.Time.frameCount; }
    }

    public bool TryRead(out byte[] rgbaTopDown, out int width, out int height)
    {
        RequireMainThread();
        var provider = ScreenCaptureBackend.Current;
        if (provider == null || !SupportsProvider(provider.GetType()))
            throw new GatewayScreenshotException("screenshot_readback_unsupported",
                "The inspected native recorded-frame readback provider is unavailable.");
        return provider.TryReadScreen(out rgbaTopDown, out width, out height);
    }

    public static bool SupportsProvider(Type type)
    {
        if (type.FullName != ProviderName || type.Assembly.GetName().Name != "RimWorld" ||
            !typeof(IScreenCaptureBackend).IsAssignableFrom(type)) return false;
        var method = type.GetMethod(nameof(IScreenCaptureBackend.TryReadScreen),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null,
            new[] { typeof(byte[]).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType() }, null);
        return method != null && method.ReturnType == typeof(bool) && !method.IsGenericMethod &&
            method.GetParameters().All(parameter => parameter.IsOut);
    }

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread)
            throw new GatewayScreenshotException("screenshot_wrong_thread", "Native readback requires the Unity thread.");
    }
}
#endif
