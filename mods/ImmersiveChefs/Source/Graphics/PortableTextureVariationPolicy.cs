namespace ImmersiveChefs;

internal enum PortableTextureFamily
{
    Base,
    BaseDirty,
    Wood,
    WoodDirty,
    Stone,
    StoneDirty
}

internal static class PortableTextureVariationPolicy
{
    internal static PortableTextureFamily Select(
        bool integrationEnabled,
        bool showDirtyTextures,
        KitchenwareProduct product,
        KitchenMaterialKind material,
        bool dirty)
    {
        if (product == KitchenwareProduct.ChefsKnife)
        {
            return PortableTextureFamily.Base;
        }

        var cleanFamily = !integrationEnabled
            ? PortableTextureFamily.Base
            : material switch
        {
            KitchenMaterialKind.Wood when product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery =>
                PortableTextureFamily.Wood,
            KitchenMaterialKind.PrimitiveStone when product is
                KitchenwareProduct.Cookware or KitchenwareProduct.Plate =>
                PortableTextureFamily.Stone,
                _ => PortableTextureFamily.Base
            };
        if (!dirty || !showDirtyTextures)
        {
            return cleanFamily;
        }

        return cleanFamily switch
        {
            PortableTextureFamily.Wood => PortableTextureFamily.WoodDirty,
            PortableTextureFamily.Stone => PortableTextureFamily.StoneDirty,
            _ => PortableTextureFamily.BaseDirty
        };
    }
}
