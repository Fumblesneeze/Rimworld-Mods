using UnityEngine;

namespace RimWorldDevGateway;

public sealed class UnityGatewayScreenshotBackend : IGatewayScreenshotBackend
{
    public object Capture()
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        if (texture is null)
        {
            throw new InvalidOperationException("Unity returned no screenshot texture.");
        }

        return texture;
    }

    public byte[] EncodePng(object resource)
    {
        if (resource is not Texture2D texture)
        {
            throw new ArgumentException("The screenshot resource is not a Unity Texture2D.", nameof(resource));
        }

        return ImageConversion.EncodeToPNG(texture);
    }

    public void Destroy(object resource)
    {
        if (resource is UnityEngine.Object unityObject)
        {
            UnityEngine.Object.Destroy(unityObject);
        }
    }
}
