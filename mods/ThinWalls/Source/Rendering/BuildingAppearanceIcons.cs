using System;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>UI-only compositions of native textures; no raster assets or texture allocations.</summary>
public static class BuildingAppearanceIcons
{
    public static Rect SymbolBounds(float buttonSize, float labelHeight, float badgeHeight)
    {
        float scale = buttonSize / 75f;
        float top = Math.Max(22f * scale, 5f * scale + badgeHeight);
        float height = Math.Max(0f, Math.Min(46f * scale, buttonSize + 8f * scale - labelHeight - top));
        float width = height * (54f / 46f);
        return new Rect((buttonSize - width) / 2f, top, width, height);
    }

    public static Rect OffsetObject => new(1f, 11f, 19f, 18f);

    public static void OffsetArrow(out Vector2 tail, out Vector2 tip)
    {
        tail = new Vector2(22f, 20f);
        tip = new Vector2(40f, 20f);
    }

    public static void ShrinkArrow(int index, out Vector2 tail, out Vector2 tip)
    {
        tail = index == 0 ? new Vector2(4f, 4f) : new Vector2(36f, 36f);
        tip = index == 0 ? new Vector2(18f, 18f) : new Vector2(22f, 22f);
    }

    // GUIUtility rotates in screen space. Right-compose local rotations so translated/scaled
    // gizmos retain their own pivot, including when RimWorld applies a non-default UI scale.
    public static Matrix4x4 RotationAround(Vector2 pivot, float degrees)
    {
        float c = (float)Math.Cos(degrees * Math.PI / 180), s = (float)Math.Sin(degrees * Math.PI / 180);
        Matrix4x4 result = Matrix4x4.identity;
        result.m00 = result.m11 = c;
        result.m01 = -s;
        result.m10 = s;
        result.m03 = pivot.x - c * pivot.x + s * pivot.y;
        result.m13 = pivot.y - s * pivot.x - c * pivot.y;
        return result;
    }

    public static void Draw(Rect button, bool shrink, bool disabled, Material material, GizmoRenderParms parms,
        float labelHeight, float badgeHeight)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color original = GUI.color;
        Matrix4x4 matrix = GUI.matrix;
        Rect bounds = parms.shrunk ? button.ContractedBy(button.width * 0.13f) : SymbolBounds(button.width, labelHeight, badgeHeight);
        if (!parms.shrunk) bounds.position += button.position;
        Color accent = shrink ? new Color(1f, 0.82f, 0.36f) : new Color(0.42f, 0.84f, 1f);
        if (disabled || parms.lowLight) accent = accent.SaturationChanged(0f);
        float alpha = parms.lowLight ? 0.6f : 1f;
        try
        {
            // Work in a 40px symbol square independent of native UI scaling.
            GUI.matrix *= Matrix4x4.TRS(new Vector3(bounds.x, bounds.y, 0f), Quaternion.identity,
                new Vector3(bounds.width / 40f, bounds.height / 40f, 1f));
            if (shrink)
            {
                for (int i = 0; i < 2; i++)
                {
                    ShrinkArrow(i, out Vector2 tail, out Vector2 tip);
                    Arrow(tail, tip, accent, alpha, material);
                }
            }
            else
            {
                Square(OffsetObject, new Color(0.82f, 0.88f, 0.92f), alpha, material);
                OffsetArrow(out Vector2 tail, out Vector2 tip);
                Arrow(tail, tip, accent, alpha, material);
            }
        }
        finally { GUI.matrix = matrix; GUI.color = original; }
    }

    // The complete Core overlay arrow retains its native contour at small UI sizes. Resolve
    // lazily on the first repaint, not during host-side geometry tests or static initialization.
    private static Texture2D arrow;

    private static void Arrow(Vector2 tail, Vector2 tip, Color color, float alpha, Material material)
    {
        Vector2 delta = tip - tail;
        float angle = (float)(Math.Atan2(delta.y, delta.x) * 180 / Math.PI);
        float length = delta.magnitude;
        Matrix4x4 original = GUI.matrix;
        GUI.matrix *= RotationAround(tail, angle + 90f);
        GUI.color = color.ToTransparent(alpha);
        arrow ??= ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");
        GenUI.DrawTextureWithMaterial(new Rect(tail.x - 7f, tail.y - length, 14f, length), arrow, material);
        GUI.matrix = original;
    }

    private static void Square(Rect rect, Color color, float alpha, Material material)
    {
        Fill(rect, new Color(0.09f, 0.1f, 0.11f), alpha, material);
        Fill(rect.ContractedBy(1f), color * new Color(0.72f, 0.72f, 0.72f, 1f), alpha, material);
        Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 2f), color, alpha, material);
    }

    private static void Fill(Rect rect, Color color, float alpha, Material material)
    {
        GUI.color = color.ToTransparent(alpha);
        GenUI.DrawTextureWithMaterial(rect, BaseContent.WhiteTex, material);
    }
}
