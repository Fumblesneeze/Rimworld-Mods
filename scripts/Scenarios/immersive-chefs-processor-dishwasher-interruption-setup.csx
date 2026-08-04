new System.Func<string>(() =>
{
    var stage = "resolve processor fixture";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The Processor interruption scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        var dishwasher = Find.Selector.SingleSelectedThing as ThingWithComps;
        if (dishwasher == null ||
            dishwasher.def.defName != "ImmersiveChefs_Dishwasher")
        {
            throw new System.InvalidOperationException(
                "The preceding Processor fixture did not leave its dishwasher selected.");
        }

        stage = "create native switch worker";
        var basicWorkType = DefDatabase<WorkTypeDef>.GetNamed("BasicWorker");
        Pawn worker = null;
        for (var attempt = 0; attempt < 32 && worker == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            if (!candidate.WorkTypeIsDisabled(basicWorkType) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                worker = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        if (worker == null)
        {
            throw new System.InvalidOperationException(
                "Could not generate a capable switch worker for the interruption fixture.");
        }

        worker.Name = new NameSingle("Dishwasher Switch Worker");
        worker.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(
            worker,
            new IntVec3(dishwasher.Position.x + 2, 0, dishwasher.Position.z - 2),
            map);
        worker.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            worker.workSettings.SetPriority(workType, 0);
        }
        worker.workSettings.SetPriority(basicWorkType, 1);

        stage = "frame passive interruption fixture";
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(dishwasher);
        Messages.Message(
            "Processor interruption ready: use native time and dishwasher gizmos to pause, resume, or eject.",
            MessageTypeDefOf.NeutralEvent,
            false);
        return dishwasher.ThingID + "|" + worker.ThingID;
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
