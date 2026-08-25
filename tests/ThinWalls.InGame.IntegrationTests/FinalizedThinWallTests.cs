using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using ThinWalls.Designation;
using Verse;

namespace ThinWalls.InGame.IntegrationTests;

public static class FinalizedThinWallTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ThinWallDefAndSpecialDesignatorAreFinalizedExactlyOnce()
    {
        ThingDef wall = DefDatabase<ThingDef>.GetNamed("TW_ThinWall");
        ThingDef door = DefDatabase<ThingDef>.GetNamed("TW_ThinDoor");
        DesignationCategoryDef structure = DefDatabase<DesignationCategoryDef>.GetNamed("Structure");
        Designator_ThinWall[] designators = structure.AllResolvedDesignators.OfType<Designator_ThinWall>().ToArray();

        IntegrationAssert.Equal(3, wall.costStuffCount, "Thin walls must cost ceil-half of Core wall material");
        IntegrationAssert.Equal(150f, wall.GetStatValueAbstract(StatDefOf.MaxHitPoints),
            "Thin walls must have half the Core wall base durability");
        IntegrationAssert.Equal(Traversability.Standable, wall.passability,
            "Thin-wall owner cells must remain standable");
        IntegrationAssert.False(wall.holdsRoof, "Thin walls must never support roofs");
        IntegrationAssert.False(wall.building.isEdifice, "Thin walls must not occupy the cell as an edifice");
        IntegrationAssert.False(wall.building.allowAutoroof, "Thin walls must not seed automatic roofs");
        IntegrationAssert.False(wall.canGenerateDefaultDesignator,
            "Only the directional special designator may expose Thin Walls");
        IntegrationAssert.NotNull(wall.blueprintDef,
            "Thin Walls must receive RimWorld's implied native blueprint Def");
        IntegrationAssert.NotNull(wall.frameDef,
            "Thin Walls must receive RimWorld's implied native frame Def");
        IntegrationAssert.False(wall.blueprintDef.holdsRoof,
            "The implied Thin Wall blueprint must not support roofs");
        IntegrationAssert.False(wall.frameDef.holdsRoof,
            "The implied Thin Wall frame must not support roofs");
        IntegrationAssert.Equal(2, designators.Length,
            "Structure must contain exactly the Thin Wall and Thin Door edge designators");
        IntegrationAssert.Equal(1, designators.Count(designator => designator.GetType() == typeof(Designator_ThinWall)),
            "Structure must resolve exactly one Thin Wall designator");
        IntegrationAssert.Equal(1, designators.OfType<Designator_ThinDoor>().Count(),
            "Structure must resolve exactly one Thin Door designator");
        IntegrationAssert.NotNull(wall.graphic.MatSingle.mainTexture,
            "The selected original Thin Walls diffuse must resolve into a live material");
        IntegrationAssert.Equal(13, door.costStuffCount,
            "Thin doors must cost ceil-half of the Core simple door material");
        IntegrationAssert.Equal(80f, door.GetStatValueAbstract(StatDefOf.MaxHitPoints),
            "Thin doors must have half the Core simple door base durability");
        IntegrationAssert.Equal(Traversability.Standable, door.passability,
            "Thin-door owner cells must remain standable");
        IntegrationAssert.False(door.holdsRoof, "Thin doors must never support roofs");
        IntegrationAssert.False(door.building.isEdifice, "Thin doors must not occupy the owner cell as an edifice");
        IntegrationAssert.NotNull(door.blueprintDef, "Thin doors must receive a native implied blueprint");
        IntegrationAssert.NotNull(door.frameDef, "Thin doors must receive a native implied frame");
        IntegrationAssert.False(door.blueprintDef.holdsRoof, "Thin-door blueprints must not support roofs");
        IntegrationAssert.False(door.frameDef.holdsRoof, "Thin-door frames must not support roofs");
        IntegrationAssert.NotNull(door.graphic.MatSingle.mainTexture,
            "The Thin Door mover diffuse must resolve into a live material");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void HarmonyOwnerIsPresentAndProductHasNoGatewayReference()
    {
        int ownedPatchCount = Harmony.GetAllPatchedMethods()
            .SelectMany(method =>
            {
                Patches? patches = Harmony.GetPatchInfo(method);
                return (patches?.Prefixes ?? Enumerable.Empty<Patch>())
                    .Concat(patches?.Postfixes ?? Enumerable.Empty<Patch>())
                    .Concat(patches?.Transpilers ?? Enumerable.Empty<Patch>())
                    .Concat(patches?.Finalizers ?? Enumerable.Empty<Patch>());
            })
            .Count(patch => patch.owner == ThinWallsMod.PackageId);
        string[] references = typeof(ThinWallsMod).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        IntegrationAssert.Equal(20, ownedPatchCount,
            "Thin Walls must acquire each declared patch exactly once");
        IntegrationAssert.False(references.Any(reference => reference.StartsWith("RimWorldDevGateway")),
            "The product assembly must not reference the development gateway");
    }
}
