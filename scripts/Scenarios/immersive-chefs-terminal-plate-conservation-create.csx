new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.TerminalPlateScenario";
    var context = AppDomain.CurrentDomain.GetData(contextKey) as
        System.Collections.Generic.Dictionary<string, object>;
    if (context == null)
    {
        throw new System.InvalidOperationException("The terminal plate fixture context is missing.");
    }

    var map = (Map)context["map"];
    var mealCells = (System.Collections.Generic.List<IntVec3>)context["mealCells"];
    var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
    ThingDef flammableStuff = null;
    ThingDef nonflammableStuff = null;
    foreach (var stuff in GenStuff.AllowedStuffsFor(plateDef))
    {
        var flammability = plateDef.GetStatValueAbstract(StatDefOf.Flammability, stuff);
        if (flammability > 0f && flammableStuff == null)
        {
            flammableStuff = stuff;
        }
        else if (flammability <= 0f && nonflammableStuff == null)
        {
            nonflammableStuff = stuff;
        }
    }

    if (flammableStuff == null || nonflammableStuff == null)
    {
        throw new System.InvalidOperationException(
            "The finalized plate Def must allow both a flammable and nonflammable Stuff.");
    }

    var meals = new System.Collections.Generic.List<ThingWithComps>();
    var plates = new System.Collections.Generic.List<Thing>();
    var stuffs = new[] { flammableStuff, flammableStuff, nonflammableStuff };
    for (var index = 0; index < mealCells.Count; index++)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, stuffs[index]);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
        {
            throw new System.InvalidOperationException("A terminal fixture meal rejected its exact plate.");
        }

        GenSpawn.Spawn(meal, mealCells[index], map);
        meals.Add(meal);
        plates.Add(plate);
    }

    meals[1].HitPoints = 1;
    meals[2].HitPoints = 1;
    var rottable = meals[0].GetComp<CompRottable>();
    if (rottable == null)
    {
        throw new System.InvalidOperationException("The expiring meal lacks its finalized rot component.");
    }

    rottable.RotProgress = rottable.PropsRot.TicksToRotStart + 1f;
    context["meals"] = meals;
    context["plates"] = plates;
    context["flammableStuff"] = flammableStuff;
    context["nonflammableStuff"] = nonflammableStuff;
    return meals[0].ThingID + "|" + plates[0].ThingID + "|" + mealCells[0] + "|" +
           meals[1].ThingID + "|" + plates[1].ThingID + "|" + mealCells[1] + "|" +
           meals[2].ThingID + "|" + plates[2].ThingID + "|" + mealCells[2] + "|" +
           flammableStuff.defName + "|" + nonflammableStuff.defName;
})()
