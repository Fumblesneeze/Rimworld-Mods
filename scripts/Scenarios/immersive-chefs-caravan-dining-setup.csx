new System.Func<string>(() =>
{
    const string scenarioName = "Immersive Chefs caravan dining scenario";
    var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    pawn.inventory.innerContainer.ClearAndDestroyContents();
    var caravan = RimWorld.Planet.CaravanMaker.MakeCaravan(
        new[] { pawn },
        Faction.OfPlayer,
        Find.CurrentMap.Tile,
        true);
    caravan.Name = scenarioName;

    var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
    var plate = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
        ThingDefOf.Steel);
    var silverware = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
        ThingDefOf.Steel);
    plate.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
    silverware.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
    if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
    {
        throw new System.InvalidOperationException("Could not embed the scenario plate in its meal.");
    }

    if (!pawn.inventory.innerContainer.TryAdd(meal, false) ||
        !pawn.inventory.innerContainer.TryAdd(silverware, false))
    {
        throw new System.InvalidOperationException("Could not populate the caravan dining scenario inventory.");
    }

    pawn.needs.food.CurLevelPercentage = 0.50f;
    caravan.RecacheInventory();
    CameraJumper.TryJumpAndSelect(
        new RimWorld.Planet.GlobalTargetInfo(caravan),
        CameraJumper.MovementMode.Cut);
    Find.World.UI.inspectPane.OpenTabType = typeof(RimWorld.Planet.WITab_Caravan_Items);
    return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0}|{1}|{2}|{3}",
        pawn.ThingID,
        meal.ThingID,
        plate.ThingID,
        silverware.ThingID);
})()
