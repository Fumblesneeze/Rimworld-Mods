using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class JobDriver_DispensePreparedPaste : JobDriver
{
    private Thing Dispenser => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Dispenser, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOn(() => !PasteDispenserAdapter.CanDispense(Dispenser));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return new Toil
        {
            initAction = Dispense,
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    private void Dispense()
    {
        var pasteMeal = PasteDispenserAdapter.TryDispense(Dispenser);
        if (pasteMeal is null || Dispenser.Map is not { } map)
        {
            return;
        }

        var sourceDefs = (pasteMeal as ThingWithComps)?.GetComp<CompIngredients>()?.ingredients ??
                         new List<ThingDef>();
        var prepared = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood"));
        var nutrition = Math.Max(0.05f, pasteMeal.GetStatValue(StatDefOf.Nutrition));
        prepared.stackCount = Math.Max(1, GenMath.RoundRandom(nutrition / 0.05f));
        (prepared as ThingWithComps)?.GetComp<CompPreparedFood>()?.Initialize(new PreparedFoodState(
            PreparedFoodDietaryPolicy.CreatePasteContributions(
                sourceDefs.Select(def => def.defName),
                totalNutrition: 0.05f),
            ImmersiveChefsMod.Settings.PastePreparationQuality,
            preparerThingId: null,
            DietaryClassification.ForDefs(sourceDefs),
            exactSourcesHidden: true,
            ingredientPoisonChance: 0f));
        pasteMeal.Destroy(DestroyMode.Vanish);
        GenPlace.TryPlaceThing(prepared, Dispenser.InteractionCell, map, ThingPlaceMode.Near);
    }
}

[HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
internal static class PreparedPasteFloatMenuPatch
{
    private static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref List<FloatMenuOption> __result)
    {
        if (selectedPawns.Count != 1 || selectedPawns[0] is not { } pawn || pawn.Map is null)
        {
            return;
        }

        var cell = IntVec3.FromVector3(clickPos);
        foreach (var dispenser in cell.GetThingList(pawn.Map).Where(PasteDispenserAdapter.CanDispense))
        {
            if (!pawn.CanReserveAndReach(dispenser, PathEndMode.InteractionCell, Danger.Some))
            {
                continue;
            }

            __result.Add(new FloatMenuOption("ImmersiveChefs_DispensePreparedPaste".Translate(), () =>
            {
                var job = JobMaker.MakeJob(ImmersiveChefsDefOf.ImmersiveChefs_DispensePreparedPaste, dispenser);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }));
        }
    }
}
