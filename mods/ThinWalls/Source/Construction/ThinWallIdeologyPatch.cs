using HarmonyLib;
using RimWorld;
using Verse;

namespace ThinWalls.Construction;

/// <summary>
/// Thin Walls use a special directional designator, so their Def cannot ask RimWorld to
/// generate its ordinary build designator. Ideology treats that same Def flag as meaning
/// "precept-owned building" unless told otherwise. Thin Walls are ordinary architecture,
/// not an ideology-locked buildable, and must remain constructible by every colonist.
/// </summary>
[HarmonyPatch(typeof(Ideo), nameof(Ideo.MembersCanBuild))]
public static class ThinWallIdeologyPatch
{
    [HarmonyPostfix]
    public static void Postfix(Thing thing, ref bool __result)
    {
        if (!__result && ThinWallUtility.IsThinEdgeDef(thing.def))
        {
            __result = true;
        }
    }
}
