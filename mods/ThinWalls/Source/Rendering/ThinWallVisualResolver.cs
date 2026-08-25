using System.Collections.Generic;
using System.Linq;

namespace ThinWalls.Rendering;

public enum ThinWallMaterialFamily
{
    Stone,
    Wood,
    Metal,
}

public enum ThinWallDamageGrade
{
    None,
    Moderate,
    Heavy,
    Severe,
}

public static class ThinWallVisualResolver
{
    public static ThinWallMaterialFamily ResolveMaterialFamily(IEnumerable<string>? categoryDefNames)
    {
        string[] categories = categoryDefNames?.ToArray() ?? System.Array.Empty<string>();
        if (categories.Contains("Woody"))
        {
            return ThinWallMaterialFamily.Wood;
        }
        if (categories.Contains("Metallic"))
        {
            return ThinWallMaterialFamily.Metal;
        }
        return ThinWallMaterialFamily.Stone;
    }

    public static ThinWallDamageGrade DamageGrade(int hitPoints, int maxHitPoints)
    {
        if (maxHitPoints <= 0)
        {
            return ThinWallDamageGrade.None;
        }

        float ratio = hitPoints / (float)maxHitPoints;
        if (ratio <= 0.25f)
        {
            return ThinWallDamageGrade.Severe;
        }
        if (ratio <= 0.50f)
        {
            return ThinWallDamageGrade.Heavy;
        }
        return ratio <= 0.75f
            ? ThinWallDamageGrade.Moderate
            : ThinWallDamageGrade.None;
    }
}
