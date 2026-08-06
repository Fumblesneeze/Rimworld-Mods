using UnityEngine;
using Verse;

namespace ImmersiveChefs;

internal static class PortableTextureVariationPaths
{
    internal static string Resolve(string basePath, PortableTextureFamily family)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("A base texture path is required.", nameof(basePath));
        }

        return family switch
        {
            PortableTextureFamily.Base => basePath,
            PortableTextureFamily.BaseDirty => basePath + "_Dirty",
            PortableTextureFamily.Wood => basePath + "_Wood",
            PortableTextureFamily.WoodDirty => basePath + "_WoodDirty",
            PortableTextureFamily.Stone => basePath + "_Stone",
            PortableTextureFamily.StoneDirty => basePath + "_StoneDirty",
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
        };
    }
}

public sealed class Graphic_PortableKitchenwareVariation : Graphic_Single
{
    private static KitchenMaterialClassifier? classifier;
    private static readonly Dictionary<(ThingDef Stuff, KitchenwareProduct Product), KitchenMaterialKind>
        MaterialKinds = new();
    private readonly Dictionary<PortableTextureFamily, Graphic_Single> familyGraphics = new();

    public override Material MatAt(Rot4 rot, Thing thing = null!)
    {
        var selected = SelectGraphic(thing);
        return ReferenceEquals(selected, this)
            ? base.MatAt(rot, thing)
            : selected.MatAt(rot, thing);
    }

    public override Material MatSingleFor(Thing thing)
    {
        var selected = SelectGraphic(thing);
        return ReferenceEquals(selected, this)
            ? base.MatSingleFor(thing)
            : selected.MatSingleFor(thing);
    }

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return GraphicDatabase.Get<Graphic_PortableKitchenwareVariation>(
            path,
            newShader,
            drawSize,
            newColor,
            newColorTwo,
            data,
            maskPath);
    }

    private Graphic SelectGraphic(Thing? thing)
    {
        var extension = thing?.def.GetModExtension<KitchenwareExtension>();
        if (thing is null || extension is null)
        {
            return this;
        }

        var material = MaterialKindFor(thing, extension);
        var dirty = (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
        var family = PortableTextureVariationPolicy.Select(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.TextureVariations),
            ImmersiveChefsMod.Settings.ShowDirtyWareTextures,
            extension.product,
            material,
            dirty);
        return family == PortableTextureFamily.Base ? this : GraphicFor(family);
    }

    private Graphic_Single GraphicFor(PortableTextureFamily family)
    {
        if (familyGraphics.TryGetValue(family, out var cached))
        {
            return cached;
        }

        var variantPath = PortableTextureVariationPaths.Resolve(path, family);
        var graphic = (Graphic_Single)GraphicDatabase.Get<Graphic_Single>(
            variantPath,
            Shader,
            drawSize,
            color,
            colorTwo,
            data,
            variantPath + MaskSuffix);
        familyGraphics[family] = graphic;
        return graphic;
    }

    private static KitchenMaterialKind MaterialKindFor(
        Thing thing,
        KitchenwareExtension extension)
    {
        if (thing.Stuff is not { } stuff)
        {
            return extension.fixedMaterialKind ?? KitchenMaterialKind.OtherMetal;
        }

        var cacheKey = (stuff, extension.product);
        if (MaterialKinds.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        classifier ??= OptionalMaterialAdapter.CreateClassifier();
        var categories = stuff.stuffProps?.categories;
        var classification = classifier.Classify(
            new KitchenMaterialDescriptor(
                stuff.defName,
                categories?.Any(category => category.defName.Equals(
                    "Metallic",
                    StringComparison.OrdinalIgnoreCase)) == true,
                categories?.Any(category => category.defName.Equals(
                    "Woody",
                    StringComparison.OrdinalIgnoreCase)) == true,
                categories?.Any(category => category.defName.Equals(
                    "Stony",
                    StringComparison.OrdinalIgnoreCase)) == true),
            extension.product);
        var material = classification?.Kind ??
                       extension.fixedMaterialKind ??
                       KitchenMaterialKind.OtherMetal;
        MaterialKinds[cacheKey] = material;
        return material;
    }
}
