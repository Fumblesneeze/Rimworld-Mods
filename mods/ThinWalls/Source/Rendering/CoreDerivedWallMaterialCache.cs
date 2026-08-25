using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

internal readonly struct HybridRegularRenderMaterial
{
    public HybridRegularRenderMaterial(
        Material nativeMaterial,
        IReadOnlyList<HybridRegularExteriorRenderMaterial> exteriorMaterials,
        HybridRegularShadowPlan shadow)
    {
        NativeMaterial = nativeMaterial;
        ExteriorMaterials = exteriorMaterials;
        Shadow = shadow;
    }

    public Material NativeMaterial { get; }

    public IReadOnlyList<HybridRegularExteriorRenderMaterial> ExteriorMaterials { get; }
    public HybridRegularShadowPlan Shadow { get; }
}

internal readonly struct HybridRegularExteriorRenderMaterial
{
    public HybridRegularExteriorRenderMaterial(
        HybridRegularExteriorRegion region,
        Material material)
    {
        Region = region;
        Material = material;
    }

    public HybridRegularExteriorRegion Region { get; }
    public Material Material { get; }
}

[StaticConstructorOnStartup]
internal static class CoreDerivedWallMaterialCache
{
    public const int HybridRegularCanvasSize = HybridRegularRasterCompositor.CanvasSize;
    public const int HybridRegularPadding = HybridRegularRasterCompositor.Padding;

    internal enum ThinMaterialPortion : byte
    {
        All,
        OutlineOnly,
        HorizontalSouth,
        HorizontalNorth,
        VerticalWest,
        VerticalEast,
    }

    private static readonly Dictionary<ThinMaterialKey, Material> ThinMaterials = new();
    private static readonly Dictionary<HybridMaterialKey, HybridRegularRenderMaterial> HybridMaterials = new();
    private static readonly Dictionary<string, Material> StateMaterials = new();
    private static readonly Dictionary<int, Material> RealtimeMaterials = new();
    private static readonly Dictionary<int, TexturePixels> ReadableTextures = new();
    private static readonly Dictionary<int, TexturePixels> ResolvedMaterialTextures = new();
    private static readonly IReadOnlyDictionary<string, string> AcceptedCoreAtlasHashes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Wall_Atlas_Bricks"] = "9103d223a5c8c5c38cda6b3b7c423e2d8c8dfaa5fcc719311ef0013d68b12044",
            ["Wall_Atlas_Planks"] = "080da89957a7fb03a84e6b22ebc0cd0b64b38ce1a2fca5b86f1195c734179aa3",
            ["Wall_Atlas_Smooth"] = "3264a8a3747279b010d7895507f31d30d3b5e2e2da949262a804595f3f305c95",
        };

    public static Material ThinMaterial(
        Material sourceAtlasMaterial,
        HybridWallRayMask rays,
        bool doubled,
        HybridWallQuadrant clearedQuadrants) =>
        ThinMaterial(
            sourceAtlasMaterial,
            rays,
            doubled ? rays : HybridWallRayMask.None,
            clearedQuadrants);

    public static Material ThinMaterial(
        Material sourceAtlasMaterial,
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallQuadrant clearedQuadrants)
        => DecoratedThinMaterial(
            sourceAtlasMaterial,
            rays,
            doubledRays,
            clearedQuadrants,
            ThinWallMaterialFamily.Stone,
            ThinWallDamageGrade.None,
            door: false,
            ThinMaterialPortion.All);

    public static Material DecoratedThinMaterial(
        Material sourceAtlasMaterial,
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallQuadrant clearedQuadrants,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door,
        ThinMaterialPortion portion = ThinMaterialPortion.All)
    {
        var key = new ThinMaterialKey(
            sourceAtlasMaterial.GetInstanceID(),
            rays,
            doubledRays,
            clearedQuadrants,
            family,
            damage,
            door,
            portion);
        if (ThinMaterials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(
            rays,
            key.DoubledRays,
            clearedQuadrants);
        Material material = BuildThinMaterial(sourceAtlasMaterial, plan, family, damage, door, portion, key.ToString());
        ThinMaterials[key] = material;
        return material;
    }

    public static Material RuntimeMaterial(
        Material sourceAtlasMaterial,
        HybridWallRuntimePlan plan,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door,
        HybridWallRayMask partitionRay = HybridWallRayMask.None,
        ThinWallSide? ownerSide = null,
        bool outlineOnly = false)
    {
        var key = new ThinMaterialKey(
            sourceAtlasMaterial.GetInstanceID(),
            plan.Raster.Rays,
            plan.Raster.DoubledRays,
            plan.Raster.ClearedQuadrants,
            family,
            damage,
            door,
            ThinMaterialPortion.All,
            plan.Raster.DoorRays,
            partitionRay,
            ownerSide.HasValue ? (int)ownerSide.Value : -1,
            outlineOnly);
        if (ThinMaterials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        Material material = BuildThinMaterial(
            sourceAtlasMaterial,
            plan.Raster,
            family,
            damage,
            door,
            ThinMaterialPortion.All,
            key.ToString(),
            plan,
            partitionRay,
            ownerSide,
            outlineOnly);
        ThinMaterials[key] = material;
        return material;
    }

    public static HybridRegularRenderMaterial HybridRegularMaterial(
        Material nativeLinkedMaterial,
        int linkIndex,
        IReadOnlyList<HybridWallCornerRaster> corners,
        IReadOnlyList<HybridWallContactVisual> visuals,
        IReadOnlyDictionary<int, Material> sourceMaterials)
    {
        var key = new HybridMaterialKey(
            nativeLinkedMaterial.GetInstanceID(),
            linkIndex,
            corners,
            visuals);
        if (HybridMaterials.TryGetValue(key, out HybridRegularRenderMaterial cached))
        {
            return cached;
        }

        HybridRegularRenderMaterial material = BuildHybridRegularMaterial(
            nativeLinkedMaterial,
            linkIndex,
            corners,
            visuals,
            sourceMaterials,
            key.ToString());
        HybridMaterials[key] = material;
        return material;
    }

    public static bool SupportsHybridAtlas(Texture texture)
    {
        if (!HybridWallAtlasSupport.IsSupported(texture.name, texture.width, texture.height))
        {
            return false;
        }
        try
        {
            _ = Readable(texture);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static Material StateMaterial(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        Color color)
    {
        Color32 baseColor = color;
        string key = $"{(int)rays}_{(int)doubledRays}_{baseColor.r}_{baseColor.g}_{baseColor.b}_{baseColor.a}";
        if (StateMaterials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        HybridWallRasterPlan plan = HybridWallRasterCompiler.Compile(rays, doubledRays);
        var pixels = new Color32[HybridWallRasterPlan.Size * HybridWallRasterPlan.Size];
        for (int index = 0; index < pixels.Length; index++)
        {
            HybridWallRasterSurface surface = plan.Pixels[index].Surface;
            pixels[index] = surface switch
            {
                HybridWallRasterSurface.Transparent => default,
                HybridWallRasterSurface.Outline => new Color32(9, 9, 9, baseColor.a),
                HybridWallRasterSurface.OutlineAntialias => new Color32(9, 9, 9, (byte)Math.Min(128, (int)baseColor.a)),
                HybridWallRasterSurface.Top => Scale(baseColor, 0.52f),
                HybridWallRasterSurface.WestSide => Scale(baseColor, 0.78f),
                HybridWallRasterSurface.EastSide => Scale(baseColor, 0.68f),
                _ => baseColor,
            };
        }

        Texture2D texture = BuildTexture(
            BaseContent.BadTex,
            pixels,
            HybridWallRasterPlan.Size,
            $"TW_State_{key}");
        var material = new Material(ShaderDatabase.Transparent)
        {
            mainTexture = texture,
            color = Color.white,
            hideFlags = HideFlags.HideAndDontSave,
            name = $"TW_State_{key}",
        };
        StateMaterials[key] = material;
        return material;
    }

    public static Material RealtimeMaterial(Material source)
    {
        int key = source.GetInstanceID();
        if (RealtimeMaterials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        var material = new Material(source)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = source.name + "_TW_Realtime",
        };
        material.renderQueue = ShaderDatabase.Transparent.renderQueue;
        RealtimeMaterials[key] = material;
        return material;
    }

    private static Material BuildThinMaterial(
        Material source,
        HybridWallRasterPlan plan,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door,
        ThinMaterialPortion portion,
        string nameSuffix,
        HybridWallRuntimePlan? runtimePlan = null,
        HybridWallRayMask partitionRay = HybridWallRayMask.None,
        ThinWallSide? ownerSide = null,
        bool outlineOnly = false)
    {
        TexturePixels diffuseSource = Readable(source.mainTexture);
        Texture? sourceMask = source.HasProperty("_MaskTex") ? source.GetTexture("_MaskTex") : null;
        TexturePixels? maskSource = sourceMask != null ? Readable(sourceMask) : null;
        var diffuse = new Color32[HybridWallRasterPlan.Size * HybridWallRasterPlan.Size];
        Color32[]? mask = maskSource.HasValue ? new Color32[diffuse.Length] : null;
        for (int index = 0; index < diffuse.Length; index++)
        {
            HybridWallRasterPixel sample = plan.Pixels[index];
            int x = index % HybridWallRasterPlan.Size;
            int y = index / HybridWallRasterPlan.Size;
            if (sample.Surface == HybridWallRasterSurface.Transparent ||
                !Included(sample, x, y, portion, runtimePlan, partitionRay, ownerSide, outlineOnly))
            {
                continue;
            }

            (int sourceX, int sourceY) = AtlasPixel(sample);
            diffuse[index] = StructuralDiffuse(diffuseSource, sample, family);
            if (sample.IsStructural)
            {
                diffuse[index] = HybridWallDoorAppearance.ApplyPanelTone(diffuse[index], door);
            }
            if (sample.Surface == HybridWallRasterSurface.OutlineAntialias)
            {
                diffuse[index].a = (byte)Math.Min((int)diffuse[index].a, 128);
            }

            if (mask != null && maskSource.HasValue)
            {
                mask[index] = maskSource.Value[sourceX, sourceY];
                if (sample.Surface == HybridWallRasterSurface.OutlineAntialias)
                {
                    mask[index].a = (byte)Math.Min((int)mask[index].a, 128);
                }
            }
        }


        IReadOnlyList<HybridWallTreatmentMark> treatment = HybridWallSurfaceTreatmentRecipe.Compile(
            plan,
            family,
            damage,
            door);
        for (int index = 0; index < diffuse.Length; index++)
        {
            diffuse[index] = HybridWallTreatmentColorizer.Apply(diffuse[index], treatment[index]);
            diffuse[index].a = HybridWallTreatmentCoverage.Alpha(diffuse[index].a, treatment[index]);
            if (mask != null)
            {
                mask[index].a = HybridWallTreatmentCoverage.Alpha(mask[index].a, treatment[index]);
            }
        }

        return DerivedMaterial(
            source,
            diffuse,
            mask,
            HybridWallRasterPlan.Size,
            nameSuffix,
            HybridWallTextureSampling.UseMipmaps(runtimePlan, partitionRay));
    }

    private static bool Included(
        HybridWallRasterPixel sample,
        int x,
        int y,
        ThinMaterialPortion portion,
        HybridWallRuntimePlan? runtimePlan,
        HybridWallRayMask partitionRay,
        ThinWallSide? ownerSide,
        bool outlineOnly)
    {
        bool outline = sample.Surface is HybridWallRasterSurface.Outline or HybridWallRasterSurface.OutlineAntialias;
        if (outlineOnly)
        {
            return outline;
        }
        if (runtimePlan != null && partitionRay != HybridWallRayMask.None)
        {
            HybridWallRayMask primaryOwner = runtimePlan.PrimaryOwnerAt(x, y);
            if (outline || !sample.IsStructural || !partitionRay.HasFlag(primaryOwner))
            {
                return false;
            }
            return !ownerSide.HasValue || IncludedOwnerHalf(sample, x, y, partitionRay, ownerSide.Value);
        }

        if (portion == ThinMaterialPortion.All)
        {
            return true;
        }

        if (portion == ThinMaterialPortion.OutlineOnly)
        {
            return outline;
        }
        if (outline || !sample.IsStructural)
        {
            return false;
        }

        return portion switch
        {
            ThinMaterialPortion.HorizontalSouth =>
                sample.Surface == HybridWallRasterSurface.Front ||
                sample.Surface == HybridWallRasterSurface.Top && y < 37 ||
                (sample.Surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide) && y < 37,
            ThinMaterialPortion.HorizontalNorth =>
                sample.Surface == HybridWallRasterSurface.Top && y >= 37 ||
                (sample.Surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide) && y >= 37,
            ThinMaterialPortion.VerticalWest =>
                sample.Surface == HybridWallRasterSurface.WestSide ||
                sample.Surface == HybridWallRasterSurface.Top && x < 30 ||
                sample.Surface == HybridWallRasterSurface.Front && x < 30,
            ThinMaterialPortion.VerticalEast =>
                sample.Surface == HybridWallRasterSurface.EastSide ||
                sample.Surface == HybridWallRasterSurface.Top && x >= 30 ||
                sample.Surface == HybridWallRasterSurface.Front && x >= 30,
            _ => false,
        };
    }

    private static bool IncludedOwnerHalf(
        HybridWallRasterPixel sample,
        int x,
        int y,
        HybridWallRayMask ray,
        ThinWallSide ownerSide)
    {
        bool horizontal = ray is HybridWallRayMask.East or HybridWallRayMask.West;
        if (horizontal)
        {
            return ownerSide == ThinWallSide.North
                ? sample.Surface == HybridWallRasterSurface.Front || y < 37
                : sample.Surface != HybridWallRasterSurface.Front && y >= 37;
        }

        return ownerSide == ThinWallSide.East
            ? sample.Surface == HybridWallRasterSurface.WestSide || x < 30
            : sample.Surface != HybridWallRasterSurface.WestSide && x >= 30;
    }

    private static HybridRegularRenderMaterial BuildHybridRegularMaterial(
        Material nativeLinked,
        int linkIndex,
        IReadOnlyList<HybridWallCornerRaster> corners,
        IReadOnlyList<HybridWallContactVisual> visuals,
        IReadOnlyDictionary<int, Material> sourceMaterials,
        string nameSuffix)
    {
        TexturePixels nativeSource = ResolvedMaterialAtlas(nativeLinked);
        var contactSources = new Dictionary<int, TexturePixels>();
        foreach (HybridWallContactVisual visual in visuals)
        {
            if (!sourceMaterials.TryGetValue(visual.MaterialId, out Material sourceMaterial))
            {
                throw new InvalidOperationException(
                    $"Hybrid contact {visual.Contact} has no source material {visual.MaterialId}.");
            }
            contactSources[visual.MaterialId] = ResolvedMaterialAtlas(sourceMaterial);
        }
        int transitionLinkIndex = HybridRegularRasterCompositor.TransitionLinkIndex(linkIndex, corners);
        CoreLinkedSemanticPlan transitionPlan = CoreLinkedSemanticCompiler.Compile(transitionLinkIndex);
        HybridWallRayMask addedRays = (HybridWallRayMask)(transitionLinkIndex & ~linkIndex);
        var nativeDiffuse = new Color32[HybridWallRasterPlan.Size * HybridWallRasterPlan.Size];
        var transitionDiffuse = new Color32[nativeDiffuse.Length];
        var nativeAlpha = new byte[nativeDiffuse.Length];
        var transitionAlpha = new byte[nativeDiffuse.Length];
        for (int y = 0; y < HybridWallRasterPlan.Size; y++)
        for (int x = 0; x < HybridWallRasterPlan.Size; x++)
        {
            (int sourceX, int sourceY) = AtlasPixel(linkIndex, x, y);
            (int transitionX, int transitionY) = AtlasPixel(transitionLinkIndex, x, y);
            HybridWallRasterPixel transitionSample = transitionPlan[x, y];
            HybridWallRayMask causalRays = transitionPlan.CausalAt(x, y) & addedRays;
            if (HybridRegularSquareDonor.TryMap(
                    transitionSample,
                    causalRays,
                    out int donorLinkIndex,
                    out int donorX,
                    out int donorY))
            {
                (transitionX, transitionY) = AtlasPixel(donorLinkIndex, donorX, donorY);
            }
            int index = y * HybridWallRasterPlan.Size + x;
            nativeDiffuse[index] = nativeSource[sourceX, sourceY];
            transitionDiffuse[index] = nativeSource[transitionX, transitionY];
            nativeAlpha[index] = nativeDiffuse[index].a;
            transitionAlpha[index] = transitionDiffuse[index].a;
        }

        HybridRegularCompositePlan plan = HybridRegularRasterCompositor.Compile(
            nativeAlpha,
            transitionAlpha,
            corners,
            linkIndex);
        Color32[] diffuse = HybridRegularRasterCompositor.ComposeOwned(
            nativeDiffuse,
            transitionDiffuse,
            plan,
            (index, sample) =>
            {
                HybridWallContactVisual visual = HybridWallContactVisualSelector.Select(plan, index, visuals);
                Color32 color = StructuralDiffuse(
                    contactSources[visual.MaterialId],
                    sample,
                    visual.Family);
                HybridWallTreatmentMark treatment = HybridWallSurfaceTreatmentRecipe.At(
                    sample,
                    visual.Contact.Ray,
                    visual.Family,
                    visual.Damage,
                    door: false,
                    sample.SourceX,
                    sample.SourceY);
                return HybridWallTreatmentColorizer.Apply(color, treatment);
            },
            (x, y, index) =>
            {
                (byte outlineX, byte outlineY) = OutlineSample(x, y);
                (int sourceX, int sourceY) = AtlasPixel(0, outlineX, outlineY);
                if (HybridRegularRasterCompositor.IsInsideNativeRegion(index))
                {
                    return nativeSource[sourceX, sourceY];
                }
                HybridWallContactVisual visual = HybridWallContactVisualSelector.Select(plan, index, visuals);
                return contactSources[visual.MaterialId][sourceX, sourceY];
            },
            sample =>
            {
                (int sourceX, int sourceY) = AtlasPixel(sample);
                return nativeSource[sourceX, sourceY];
            });
        HybridRegularSamplingLayers samplingLayers =
            HybridRegularSamplingLayerCompiler.Compile(diffuse);
        Material nativeMaterial = DerivedResolvedHybridMaterial(
            nativeLinked,
            samplingLayers.Native,
            HybridWallRasterPlan.Size,
            HybridWallRasterPlan.Size,
            nameSuffix + "_Native",
            Vector2.one,
            Vector2.zero);
        var exteriorMaterials = new List<HybridRegularExteriorRenderMaterial>();
        for (int index = 0; index < samplingLayers.ExteriorRegions.Count; index++)
        {
            HybridRegularExteriorSamplingLayer exterior = samplingLayers.ExteriorRegions[index];
            if (!exterior.HasPixels)
            {
                continue;
            }
            Material material = DerivedResolvedHybridMaterial(
                nativeLinked,
                exterior.Pixels,
                exterior.Width,
                exterior.Height,
                nameSuffix + $"_Exterior_{index}",
                Vector2.one,
                Vector2.zero);
            exteriorMaterials.Add(new HybridRegularExteriorRenderMaterial(exterior.Region, material));
        }
        return new HybridRegularRenderMaterial(
            nativeMaterial,
            exteriorMaterials,
            HybridRegularShadowCompiler.Compile(plan));
    }

    private static (byte X, byte Y) OutlineSample(int x, int y)
    {
        x = PositiveMod(x - HybridRegularPadding, HybridWallRasterPlan.Size);
        y = PositiveMod(y - HybridRegularPadding, HybridWallRasterPlan.Size);
        int distanceWest = x;
        int distanceEast = HybridWallRasterPlan.Size - 1 - x;
        int distanceSouth = y;
        int distanceNorth = HybridWallRasterPlan.Size - 1 - y;
        int minimum = Math.Min(Math.Min(distanceWest, distanceEast), Math.Min(distanceSouth, distanceNorth));
        if (minimum == distanceWest) return (1, (byte)y);
        if (minimum == distanceEast) return (58, (byte)y);
        if (minimum == distanceSouth) return ((byte)x, 1);
        return ((byte)x, 59);
    }

    private static int PositiveMod(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static Color32 Scale(Color32 color, float factor) => new(
        (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
        (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
        (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
        color.a);

    private static Material DerivedMaterial(
        Material source,
        Color32[] diffuse,
        Color32[]? mask,
        int size,
        string nameSuffix,
        bool useMipmaps = true)
    {
        Texture2D diffuseTexture = BuildTexture(
            source.mainTexture,
            diffuse,
            size,
            $"TW_Diffuse_{nameSuffix}",
            useMipmaps);
        var material = new Material(source)
        {
            mainTexture = diffuseTexture,
            mainTextureScale = Vector2.one,
            mainTextureOffset = Vector2.zero,
            hideFlags = HideFlags.HideAndDontSave,
            name = $"TW_CoreDerived_{nameSuffix}",
        };

        if (mask != null)
        {
            Texture? sourceMask = source.HasProperty("_MaskTex") ? source.GetTexture("_MaskTex") : null;
            Texture2D maskTexture = BuildTexture(
                sourceMask ?? source.mainTexture,
                mask,
                size,
                $"TW_Mask_{nameSuffix}",
                useMipmaps);
            material.SetTexture("_MaskTex", maskTexture);
        }

        return material;
    }

    private static Material DerivedResolvedHybridMaterial(
        Material nativeSource,
        Color32[] diffuse,
        int width,
        int height,
        string nameSuffix,
        Vector2 textureScale,
        Vector2 textureOffset)
    {
        Texture2D diffuseTexture = BuildTexture(
            nativeSource.mainTexture,
            diffuse,
            width,
            height,
            $"TW_HybridResolved_{nameSuffix}",
            useMipmaps: true);
        var material = new Material(ShaderDatabase.Cutout)
        {
            mainTexture = diffuseTexture,
            color = Color.white,
            mainTextureScale = textureScale,
            mainTextureOffset = textureOffset,
            hideFlags = HideFlags.HideAndDontSave,
            name = $"TW_CoreResolvedHybrid_{nameSuffix}",
            renderQueue = nativeSource.renderQueue,
        };
        return material;
    }

    private static Texture2D BuildTexture(
        Texture source,
        Color32[] pixels,
        int size,
        string name,
        bool useMipmaps = true) =>
        BuildTexture(source, pixels, size, size, name, useMipmaps);

    private static Texture2D BuildTexture(
        Texture source,
        Color32[] pixels,
        int width,
        int height,
        string name,
        bool useMipmaps = true)
    {
        var texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            mipChain: useMipmaps,
            linear: false)
        {
            name = name,
            filterMode = source.filterMode,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = source.anisoLevel,
            hideFlags = HideFlags.HideAndDontSave,
        };
        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: useMipmaps, makeNoLongerReadable: true);
        return texture;
    }

    private static TexturePixels Readable(Texture texture)
    {
        int id = texture.GetInstanceID();
        if (ReadableTextures.TryGetValue(id, out TexturePixels cached))
        {
            return cached;
        }

        Texture2D readable;
        bool destroyReadable = false;
        if (texture is Texture2D texture2D && texture2D.isReadable)
        {
            readable = texture2D;
        }
        else
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                readable.Apply(false, false);
                destroyReadable = true;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        var result = new TexturePixels(readable.width, readable.height, readable.GetPixels32());
        if (destroyReadable)
        {
            UnityEngine.Object.Destroy(readable);
        }

        ValidateCoreAtlas(texture, result);
        ReadableTextures[id] = result;
        return result;
    }

    private static TexturePixels ResolvedMaterialAtlas(Material source)
    {
        int id = source.GetInstanceID();
        if (ResolvedMaterialTextures.TryGetValue(id, out TexturePixels cached))
        {
            return cached;
        }

        _ = Readable(source.mainTexture);
        var normalized = new Material(source)
        {
            mainTextureScale = Vector2.one,
            mainTextureOffset = Vector2.zero,
            hideFlags = HideFlags.HideAndDontSave,
        };
        RenderTexture temporary = RenderTexture.GetTemporary(
            source.mainTexture.width,
            source.mainTexture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        RenderTexture previous = RenderTexture.active;
        Texture2D readable;
        try
        {
            RenderTexture.active = temporary;
            GL.Clear(clearDepth: true, clearColor: true, backgroundColor: Color.clear);
            Graphics.Blit(source.mainTexture, temporary, normalized);
            readable = new Texture2D(
                source.mainTexture.width,
                source.mainTexture.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: false);
            readable.ReadPixels(new Rect(0f, 0f, readable.width, readable.height), 0, 0);
            readable.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            UnityEngine.Object.Destroy(normalized);
        }

        var result = new TexturePixels(readable.width, readable.height, readable.GetPixels32());
        UnityEngine.Object.Destroy(readable);
        ResolvedMaterialTextures[id] = result;
        return result;
    }

    private static void ValidateCoreAtlas(Texture texture, TexturePixels pixels)
    {
        if (!AcceptedCoreAtlasHashes.TryGetValue(texture.name, out string expected))
        {
            throw new InvalidOperationException(
                $"Thin Walls rejected unknown linked atlas '{texture.name}'; only the pinned RimWorld 1.6 Core wall atlases may enter the hybrid compositor.");
        }
        if (pixels.Width != 320 || pixels.Height != 320)
        {
            throw new InvalidOperationException(
                $"Thin Walls rejected Core atlas '{texture.name}' at {pixels.Width}x{pixels.Height}; expected 320x320.");
        }

        string actual = pixels.Sha256();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Thin Walls rejected Core atlas '{texture.name}' with RGBA SHA-256 {actual}; expected {expected} for RimWorld 1.6.4871 rev591.");
        }
    }

    private static (int X, int Y) AtlasPixel(HybridWallRasterPixel sample)
        => AtlasPixel(sample.SourceLinkIndex, sample.SourceX, sample.SourceY);

    private static Color32 StructuralDiffuse(
        TexturePixels source,
        HybridWallRasterPixel sample,
        ThinWallMaterialFamily family)
    {
        (int sourceX, int sourceY) = AtlasPixel(sample);
        Color32 profile = source[sourceX, sourceY];
        if (family != ThinWallMaterialFamily.Stone ||
            !HybridWallStoneFacadeDonor.TryMap(sample, out HybridWallRasterPixel donor))
        {
            return profile;
        }

        (int donorX, int donorY) = AtlasPixel(donor);
        Color32 detail = source[donorX, donorY];
        return new Color32(detail.r, detail.g, detail.b, profile.a);
    }

    private static (int X, int Y) AtlasPixel(int linkIndex, int sourceX, int sourceY)
    {
        int column = linkIndex % 4;
        int row = linkIndex / 4;
        return (
            column * HybridWallProjectionProfile.SlotPixels + HybridWallProjectionProfile.GutterPixels + sourceX,
            row * HybridWallProjectionProfile.SlotPixels + HybridWallProjectionProfile.GutterPixels + sourceY);
    }

    private readonly struct TexturePixels
    {
        private readonly Color32[] pixels;

        public TexturePixels(int width, int height, Color32[] pixels)
        {
            Width = width;
            Height = height;
            this.pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public Color32 this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return default;
                }

                return pixels[y * Width + x];
            }
        }

        public string Sha256()
        {
            var bytes = new byte[pixels.Length * 4];
            for (int index = 0; index < pixels.Length; index++)
            {
                int offset = index * 4;
                bytes[offset] = pixels[index].r;
                bytes[offset + 1] = pixels[index].g;
                bytes[offset + 2] = pixels[index].b;
                bytes[offset + 3] = pixels[index].a;
            }
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    private readonly struct ThinMaterialKey : IEquatable<ThinMaterialKey>
    {
        public ThinMaterialKey(
            int materialId,
            HybridWallRayMask rays,
            HybridWallRayMask doubledRays,
            HybridWallQuadrant cleared,
            ThinWallMaterialFamily family,
            ThinWallDamageGrade damage,
            bool door,
            ThinMaterialPortion portion,
            HybridWallRayMask doorRays = HybridWallRayMask.None,
            HybridWallRayMask partitionRay = HybridWallRayMask.None,
            int ownerSide = -1,
            bool outlineOnly = false)
        {
            MaterialId = materialId;
            Rays = rays;
            DoubledRays = doubledRays;
            Cleared = cleared;
            Family = family;
            Damage = damage;
            Door = door;
            Portion = portion;
            DoorRays = doorRays;
            PartitionRay = partitionRay;
            OwnerSide = ownerSide;
            OutlineOnly = outlineOnly;
        }

        public int MaterialId { get; }
        public HybridWallRayMask Rays { get; }
        public HybridWallRayMask DoubledRays { get; }
        public HybridWallQuadrant Cleared { get; }
        public ThinWallMaterialFamily Family { get; }
        public ThinWallDamageGrade Damage { get; }
        public bool Door { get; }
        public ThinMaterialPortion Portion { get; }
        public HybridWallRayMask DoorRays { get; }
        public HybridWallRayMask PartitionRay { get; }
        public int OwnerSide { get; }
        public bool OutlineOnly { get; }

        public bool Equals(ThinMaterialKey other) =>
            MaterialId == other.MaterialId && Rays == other.Rays && DoubledRays == other.DoubledRays &&
            Cleared == other.Cleared && Family == other.Family && Damage == other.Damage &&
            Door == other.Door && Portion == other.Portion && DoorRays == other.DoorRays &&
            PartitionRay == other.PartitionRay && OwnerSide == other.OwnerSide && OutlineOnly == other.OutlineOnly;

        public override bool Equals(object? obj) => obj is ThinMaterialKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MaterialId;
                hash = (hash * 397) ^ (int)Rays;
                hash = (hash * 397) ^ (int)DoubledRays;
                hash = (hash * 397) ^ (int)Cleared;
                hash = (hash * 397) ^ (int)Family;
                hash = (hash * 397) ^ (int)Damage;
                hash = (hash * 397) ^ Door.GetHashCode();
                hash = (hash * 397) ^ (int)Portion;
                hash = (hash * 397) ^ (int)DoorRays;
                hash = (hash * 397) ^ (int)PartitionRay;
                hash = (hash * 397) ^ OwnerSide;
                return (hash * 397) ^ OutlineOnly.GetHashCode();
            }
        }

        public override string ToString() =>
            $"{MaterialId}_{(int)Rays}_{(int)DoubledRays}_{(int)Cleared}_{(int)Family}_{(int)Damage}_{(Door ? 1 : 0)}_{(int)Portion}_{(int)DoorRays}_{(int)PartitionRay}_{OwnerSide}_{(OutlineOnly ? 1 : 0)}";
    }

    private readonly struct HybridMaterialKey : IEquatable<HybridMaterialKey>
    {
        private readonly string cornerSignature;

        public HybridMaterialKey(
            int nativeMaterialId,
            int linkIndex,
            IReadOnlyList<HybridWallCornerRaster> corners,
            IReadOnlyList<HybridWallContactVisual> visuals)
        {
            NativeMaterialId = nativeMaterialId;
            LinkIndex = linkIndex;
            cornerSignature = $"projection={ThinWallRenderGeometry.ProjectionRecipeVersion}|{string.Join(";", corners)}|{string.Join(";", visuals)}";
        }

        public int NativeMaterialId { get; }
        public int LinkIndex { get; }

        public bool Equals(HybridMaterialKey other) =>
            NativeMaterialId == other.NativeMaterialId && LinkIndex == other.LinkIndex &&
            string.Equals(cornerSignature, other.cornerSignature, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is HybridMaterialKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NativeMaterialId;
                hash = (hash * 397) ^ LinkIndex;
                return (hash * 397) ^ cornerSignature.GetHashCode();
            }
        }

        public override string ToString() => $"{NativeMaterialId}_{LinkIndex}_{cornerSignature.GetHashCode()}";
    }
}

public readonly struct HybridWallCornerRaster
{
    public HybridWallCornerRaster(
        HybridWallQuadrant quadrant,
        HybridWallRayMask actualThinRays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays)
        : this(
            quadrant,
            actualThinRays,
            doubledRays,
            doorRays,
            actualThinRays,
            HybridWallRayMask.None)
    {
    }

    public HybridWallCornerRaster(
        HybridWallQuadrant quadrant,
        HybridWallRayMask actualThinRays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        HybridWallRayMask gutterRays,
        HybridWallRayMask sideTRays)
    {
        Quadrant = quadrant;
        ActualThinRays = actualThinRays;
        DoubledRays = doubledRays;
        DoorRays = doorRays;
        GutterRays = gutterRays & actualThinRays;
        SideTRays = sideTRays & actualThinRays;
    }

    public HybridWallQuadrant Quadrant { get; }
    public HybridWallRayMask ActualThinRays { get; }
    public HybridWallRayMask DoubledRays { get; }
    public HybridWallRayMask DoorRays { get; }
    public HybridWallRayMask GutterRays { get; }
    public HybridWallRayMask SideTRays { get; }

    public override string ToString() =>
        $"{(int)Quadrant}:{(int)ActualThinRays}:{(int)DoubledRays}:{(int)DoorRays}:{(int)GutterRays}:{(int)SideTRays}";
}
