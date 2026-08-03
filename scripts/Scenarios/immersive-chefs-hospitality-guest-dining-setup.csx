new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The Hospitality guest scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "resolve Hospitality runtime";
        var compGuestType = HarmonyLib.AccessTools.TypeByName("Hospitality.CompGuest");
        var mapComponentType = HarmonyLib.AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
        if (compGuestType == null || mapComponentType == null)
        {
            throw new System.InvalidOperationException("The loaded Hospitality types are unavailable.");
        }

        object hospitalityMapComponent = null;
        foreach (var component in map.components)
        {
            if (component.GetType() == mapComponentType)
            {
                hospitalityMapComponent = component;
                break;
            }
        }

        if (hospitalityMapComponent == null)
        {
            throw new System.InvalidOperationException("The loaded Hospitality map component is unavailable.");
        }
        var onGuestJoinedLate = HarmonyLib.AccessTools.Method(mapComponentType, "OnGuestJoinedLate");
        var arrive = compGuestType.GetMethod("Arrive");
        if (onGuestJoinedLate == null || arrive == null)
        {
            throw new System.InvalidOperationException("The loaded Hospitality guest-registration shape is incompatible.");
        }

        stage = "find fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 40f, true))
        {
            var fixture = new CellRect(candidate.x - 10, candidate.z - 5, 21, 11);
            var valid = true;
            foreach (var cell in fixture.Cells)
            {
                if (!cell.InBounds(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                center = candidate;
                break;
            }
        }

        if (!center.IsValid)
        {
            throw new System.InvalidOperationException("Could not find a clear area for the Hospitality dining fixture.");
        }

        stage = "build sealed dining rooms";
        var fixtureRoom = new CellRect(center.x - 10, center.z - 5, 21, 11);
        foreach (var cell in fixtureRoom.Cells)
        {
            foreach (var thing in cell.GetThingList(map)
                         .Where(thing => thing.def.category == ThingCategory.Item ||
                                         thing.def.category == ThingCategory.Plant ||
                                         thing.def.category == ThingCategory.Filth)
                         .ToList())
            {
                thing.Destroy(DestroyMode.Vanish);
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            if (cell.x == fixtureRoom.minX || cell.x == fixtureRoom.maxX ||
                cell.z == fixtureRoom.minZ || cell.z == fixtureRoom.maxZ ||
                cell.x == center.x)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        foreach (var existing in map.listerThings.ThingsOfDef(
                     DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery")))
        {
            existing.SetForbidden(true, false);
        }

        stage = "create arrived guests and meals";
        var playerFaction = Faction.OfPlayer;
        Faction guestFaction = null;
        foreach (var candidateFaction in Find.FactionManager.AllFactionsListForReading)
        {
            if (candidateFaction != playerFaction &&
                !candidateFaction.HostileTo(playerFaction) &&
                !candidateFaction.def.hidden)
            {
                guestFaction = candidateFaction;
                break;
            }
        }

        if (guestFaction == null)
        {
            throw new System.InvalidOperationException("No non-hostile guest faction is available.");
        }
        var guestNames = new[] { "Colony Cutlery Guest", "Personal Cutlery Guest" };
        var guestCells = new[]
        {
            new IntVec3(center.x - 5, 0, center.z),
            new IntVec3(center.x + 5, 0, center.z)
        };
        var resultIds = new System.Collections.Generic.List<string>();

        for (var index = 0; index < guestNames.Length; index++)
        {
            var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                guestFaction,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            guest.Name = new NameSingle(guestNames[index]);
            if (guest.foodRestriction == null)
            {
                guest.foodRestriction = new Pawn_FoodRestrictionTracker(guest);
            }
            guest.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(guest, guestCells[index], map);

            ThingComp compGuest = null;
            foreach (var comp in guest.AllComps)
            {
                if (comp.GetType() == compGuestType)
                {
                    compGuest = comp;
                    break;
                }
            }

            if (compGuest == null)
            {
                throw new System.InvalidOperationException("Hospitality did not attach CompGuest to " + guestNames[index] + ".");
            }
            onGuestJoinedLate.Invoke(hospitalityMapComponent, new object[] { guest });
            arrive.Invoke(compGuest, new object[0]);

            var personalCutlery = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
                ThingDefOf.Steel);
            personalCutlery.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            if (!guest.inventory.innerContainer.TryAdd(personalCutlery, false))
            {
                throw new System.InvalidOperationException("Could not give personal cutlery to " + guestNames[index] + ".");
            }

            var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var plate = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
                ThingDefOf.Steel);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
            {
                new ImmersiveChefs.CulinaryServingRecord(
                    55,
                    35f,
                    ImmersiveChefs.ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
            {
                throw new System.InvalidOperationException("Could not embed " + guestNames[index] + "'s plate.");
            }

            var mealCell = new IntVec3(guestCells[index].x, 0, guestCells[index].z + 2);
            GenSpawn.Spawn(meal, mealCell, map);
            guest.needs.food.CurLevelPercentage = 0.20f;
            resultIds.Add(guest.ThingID);
            resultIds.Add(meal.ThingID);
            resultIds.Add(plate.ThingID);
            resultIds.Add(personalCutlery.ThingID);
        }

        stage = "create reachable colony cutlery";
        var colonyCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            DefDatabase<ThingDef>.GetNamed("Gold"));
        colonyCutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(colonyCutlery, new IntVec3(center.x - 3, 0, center.z), map);
        resultIds.Add(colonyCutlery.ThingID);

        stage = "frame fixture";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(25f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(map.mapPawns.AllPawnsSpawned.Single(
            pawn => pawn.LabelShort == "Colony Cutlery Guest"));
        resultIds.Add(center.ToString());
        return string.Join("|", resultIds.ToArray());
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
