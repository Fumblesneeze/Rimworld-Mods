using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Tint a resolved native material without rebuilding its graphic or discarding mod bindings.</summary>
public static class NativeWallMaterial
{
    private static readonly Dictionary<(Material, Color, Color), Material> Tinted = new();
    private static readonly Dictionary<(Material, int), Material> Linked = new();

    public static Material ForLinks(Material source, int links)
    {
        Material native = MaterialAtlasPool.SubMaterialFromAtlas(source, (LinkDirections)links);
        if (source.mainTextureScale == Vector2.one && source.mainTextureOffset == Vector2.zero) return native;
        var key = (source, links);
        if (Linked.TryGetValue(key, out Material material)) return material;
        material = new Material(native)
        {
            mainTextureScale = Vector2.Scale(native.mainTextureScale, source.mainTextureScale),
            mainTextureOffset = source.mainTextureOffset + Vector2.Scale(native.mainTextureOffset, source.mainTextureScale),
        };
        Linked.Add(key, material);
        return material;
    }

    public static Material WithColors(Material source, Color primary, Color secondary)
    {
        var key = (source, primary, secondary);
        if (Tinted.TryGetValue(key, out Material material)) return material;
        material = new Material(source);
        if (material.HasProperty("_Color")) material.SetColor("_Color", primary);
        if (material.HasProperty("_ColorTwo")) material.SetColor("_ColorTwo", secondary);
        Tinted.Add(key, material);
        return material;
    }
}
