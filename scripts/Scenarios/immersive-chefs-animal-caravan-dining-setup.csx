new System.Func<string>(() =>
{
    const string scenarioName = "Immersive Chefs animal caravan dining scenario";
    var escort = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    var animal = PawnGenerator.GeneratePawn(
        DefDatabase<PawnKindDef>.GetNamed("LabradorRetriever"),
        Faction.OfPlayer);
    escort.inventory.innerContainer.ClearAndDestroyContents();
    animal.inventory.innerContainer.ClearAndDestroyContents();
    escort.skills.GetSkill(SkillDefOf.Plants).Level = 0;
    var caravan = RimWorld.Planet.CaravanMaker.MakeCaravan(
        new[] { escort, animal },
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
    meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
    {
        new ImmersiveChefs.CulinaryServingRecord(
            0,
            -20f,
            ImmersiveChefs.ContaminationSources.DirtyCookware |
            ImmersiveChefs.ContaminationSources.DirtyPlate |
            ImmersiveChefs.ContaminationSources.DirtySilverware,
            20,
            Find.TickManager.TicksGame)
    });
    if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
    {
        throw new System.InvalidOperationException("Could not embed the animal scenario plate in its meal.");
    }

    if (!animal.inventory.innerContainer.TryAdd(meal, false) ||
        !animal.inventory.innerContainer.TryAdd(silverware, false))
    {
        throw new System.InvalidOperationException("Could not populate the animal caravan dining inventory.");
    }

    escort.needs.food.CurLevelPercentage = 1f;
    animal.needs.food.CurLevelPercentage = 0.50f;
    caravan.RecacheInventory();
    CameraJumper.TryJumpAndSelect(
        new RimWorld.Planet.GlobalTargetInfo(caravan),
        CameraJumper.MovementMode.Cut);
    Find.World.UI.inspectPane.OpenTabType = typeof(RimWorld.Planet.WITab_Caravan_Items);
    return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0}|{1}|{2}|{3}|{4}",
        escort.ThingID,
        animal.ThingID,
        meal.ThingID,
        plate.ThingID,
        silverware.ThingID);
})()
