new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.MealCoolingHolderScenario";
    var stage = "resolve prepared context";
    try
    {
        var context = AppDomain.CurrentDomain.GetData(contextKey) as
            System.Collections.Generic.Dictionary<string, object>;
        if (context == null)
        {
            throw new System.InvalidOperationException("The prepared holder context is missing.");
        }

        var map = (Map)context["map"];
        var mealCells = (System.Collections.Generic.List<IntVec3>)context["mealCells"];
        var controlNames = (string[])context["controlNames"];
        stage = "create identical plated hot meals";
        var meals = new System.Collections.Generic.List<ThingWithComps>();
        var plates = new System.Collections.Generic.List<ThingWithComps>();
        for (var index = 0; index < mealCells.Count; index++)
        {
            var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var plate = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
                ThingDefOf.Steel);
            plate.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
            {
                new ImmersiveChefs.CulinaryServingRecord(70, 70f,
                    ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame)
            });
            if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
            {
                throw new System.InvalidOperationException(
                    "Could not embed the exact plate for " + controlNames[index] + ".");
            }

            GenSpawn.Spawn(meal, mealCells[index], map);
            meals.Add(meal);
            plates.Add(plate);
        }

        context["meals"] = meals;
        context["plates"] = plates;
        return string.Join(",", meals.Select(meal => meal.ThingID).ToArray());
    }
    catch (System.Exception error)
    {
        AppDomain.CurrentDomain.SetData(contextKey, null);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
