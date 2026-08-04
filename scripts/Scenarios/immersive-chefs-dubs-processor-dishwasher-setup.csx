new System.Func<string>(() =>
{
    var stage = "resolve base dishwasher fixture";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The Dubs Processor dishwasher scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        var dishwasher = Find.Selector.SingleSelectedThing as ThingWithComps;
        if (dishwasher == null ||
            dishwasher.def.defName != "ImmersiveChefs_Dishwasher")
        {
            throw new System.InvalidOperationException(
                "The preceding Processor fixture did not leave its dishwasher selected.");
        }

        stage = "create real Dubs plumbing";
        var pipeDef = DefDatabase<ThingDef>.GetNamed("sewagePipeHidden");
        var towerDef = DefDatabase<ThingDef>.GetNamed("WaterTowerS");
        var towerCell = new IntVec3(
            dishwasher.Position.x - 5,
            0,
            dishwasher.Position.z - 1);
        var plumbingZ = dishwasher.Position.z;
        for (var x = towerCell.x + 2; x <= dishwasher.Position.x - 1; x++)
        {
            var pipe = ThingMaker.MakeThing(pipeDef, ThingDefOf.Steel);
            pipe.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(pipe, new IntVec3(x, 0, plumbingZ), map);
        }

        var tower = (ThingWithComps)ThingMaker.MakeThing(towerDef, ThingDefOf.Steel);
        tower.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(tower, towerCell, map, Rot4.North);

        stage = "initialize bounded water supply";
        var storage = tower.AllComps.FirstOrDefault(comp =>
            comp.GetType().FullName == "DubsBadHygiene.CompWaterStorage");
        if (storage == null)
        {
            throw new System.InvalidOperationException(
                "The real Dubs water tower has no CompWaterStorage.");
        }

        var waterStorageField = storage.GetType().GetField(
            "WaterStorage",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        if (waterStorageField == null)
        {
            throw new System.InvalidOperationException(
                "The installed Dubs CompWaterStorage shape changed.");
        }
        waterStorageField.SetValue(storage, 10f);

        stage = "validate one connected Dubs network";
        var dishwasherPipe = dishwasher.AllComps.FirstOrDefault(comp =>
            comp.GetType().FullName == "DubsBadHygiene.CompPipe");
        var towerPipe = tower.AllComps.FirstOrDefault(comp =>
            comp.GetType().FullName == "DubsBadHygiene.CompPipe");
        if (dishwasherPipe == null || towerPipe == null)
        {
            throw new System.InvalidOperationException(
                "The finalized dishwasher or water tower is missing its real Dubs pipe component.");
        }

        var pipeNetProperty = dishwasherPipe.GetType().GetProperty(
            "pipeNet",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        var towerNet = pipeNetProperty == null
            ? null
            : pipeNetProperty.GetValue(towerPipe, null);
        var dishwasherNet = pipeNetProperty == null
            ? null
            : pipeNetProperty.GetValue(dishwasherPipe, null);
        if (dishwasherNet == null || !object.ReferenceEquals(dishwasherNet, towerNet))
        {
            throw new System.InvalidOperationException(
                "Dubs did not connect the dishwasher and tower through the spawned plumbing.");
        }

        var totalWaterProperty = dishwasherNet.GetType().GetProperty(
            "WaterStorage",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        var connectedWater = totalWaterProperty == null
            ? -1f
            : (float)totalWaterProperty.GetValue(dishwasherNet, null);
        if (System.Math.Abs(connectedWater - 10f) > 0.001f)
        {
            throw new System.InvalidOperationException(
                "The connected Dubs network did not expose the exact 10.0-unit fixture supply.");
        }

        stage = "frame water baseline";
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(tower);
        Messages.Message(
            "Dubs dishwasher ready: the water tower starts at 10.0; unpause to load one 5.25-setting batch.",
            MessageTypeDefOf.NeutralEvent,
            false);
        return string.Join("|", new[]
        {
            dishwasher.ThingID,
            tower.ThingID,
            connectedWater.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
        });
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
