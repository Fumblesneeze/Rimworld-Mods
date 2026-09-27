using UnityEngine;

namespace ThinWalls.Rendering;

/// <summary>Native linked UVs are normalized; texture identity and resolution are not admission rules.</summary>
public static class HybridWallAtlasSupport
{
    public static bool IsSupported(Texture? texture) =>
        texture != null && IsSupported(texture.width, texture.height);

    public static bool IsSupported(int width, int height) => width > 0 && height > 0;
}
