using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeScenarioSelectionTests
{
    [Test]
    public void Quicktest_is_quiet_until_a_named_scenario_is_explicitly_selected()
    {
        var result = InvokeScenarioResolver();

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo(
                "none|False|;gateway-regression|True|;caravan-dining|False|caravan-dining.json"));
        });
    }

    [Test]
    public void Scenario_package_requirements_are_checked_against_the_supplied_package_state()
    {
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("Test-GatewayPackageId") +
            LoadFunction("Assert-GatewayScenarioRequiredPackages") +
            "$plan = [pscustomobject]@{ Name = 'fixture'; Descriptor = [pscustomobject]@{ requiredPackageIds = @('core', 'product') } }\n" +
            "Assert-GatewayScenarioRequiredPackages -ScenarioPlan $plan -PackageIds @('core', 'product') -PackageState 'configured'\n" +
            "try { Assert-GatewayScenarioRequiredPackages -ScenarioPlan $plan -PackageIds @('core') -PackageState 'loaded'; throw 'Expected rejection.' } catch { Write-Output $_.Exception.Message }\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("requires loaded mod 'product'"));
        });
    }

    [Test]
    public void Scenario_screenshot_names_are_unique_and_scenario_scoped()
    {
        var duplicate = InvokeScenarioResolver(
            "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[" +
            "{\"id\":\"before\",\"kind\":\"screenshot\",\"fileName\":\"scenario-view.png\"}," +
            "{\"id\":\"after\",\"kind\":\"screenshot\",\"fileName\":\"scenario-view.png\"}]}");
        var reserved = InvokeScenarioResolver(
            "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[" +
            "{\"id\":\"overwrite\",\"kind\":\"screenshot\",\"fileName\":\"gateway-screenshot.png\"}]}");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.ExitCode, Is.Not.Zero);
            Assert.That(duplicate.StandardError, Does.Contain("duplicate screenshot file name"));
            Assert.That(reserved.ExitCode, Is.Not.Zero);
            Assert.That(reserved.StandardError, Does.Contain("must begin with 'scenario-'"));
        });
    }

    [Test]
    public void Missing_final_player_log_is_rejected()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Player.log");
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("Assert-GatewayFinalPlayerLog") +
            $"try {{ Assert-GatewayFinalPlayerLog -Path {PowerShellLiteral(missingPath)} -BearerToken ''; throw 'Expected rejection.' }} catch {{ Write-Output $_.Exception.Message }}\n";

        var result = RunPowerShellScript(script);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("Final Player.log is missing"));
        });
    }

    [Test]
    public void Quicktest_waits_for_playable_state_before_main_thread_def_export()
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var source = File.ReadAllText(smokePath);
        var waitIndex = source.IndexOf("gateway-smoke-wait-playing-before-def-export", StringComparison.Ordinal);
        var exportIndex = source.IndexOf("gateway-smoke-def-export", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(waitIndex, Is.GreaterThanOrEqualTo(0), "The quicktest pre-export wait marker is missing.");
            Assert.That(exportIndex, Is.GreaterThan(waitIndex), "Def export must run after quicktest reaches a playable map.");
        });
    }

    [Test]
    public void Caravan_scenario_source_shape_clears_random_inventory_and_embeds_the_plate()
    {
        var sourcePath = Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Scenarios",
            "immersive-chefs-caravan-dining-setup.csx");
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("innerContainer.ClearAndDestroyContents()"));
            Assert.That(source, Does.Contain("TryEmbedPlate(plate)"));
            Assert.That(source, Does.Not.Contain("innerContainer.TryAdd(plate"));
        });
    }

    [Test]
    public void Animal_caravan_scenario_source_shape_uses_a_dog_without_competing_forage_and_keeps_ware_embedded_or_loose()
    {
        var sourcePath = Path.Combine(
            FindSourceRepositoryRoot(),
            "scripts",
            "Scenarios",
            "immersive-chefs-animal-caravan-dining-setup.csx");
        var source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("DefDatabase<PawnKindDef>.GetNamed(\"LabradorRetriever\")"));
            Assert.That(source, Does.Contain("PawnKindDefOf.Colonist"));
            Assert.That(source, Does.Contain("new[] { escort, animal }"));
            Assert.That(source, Does.Contain("escort.needs.food.CurLevelPercentage = 1f"));
            Assert.That(source, Does.Contain("escort.skills.GetSkill(SkillDefOf.Plants).Level = 0"));
            Assert.That(source, Does.Contain("innerContainer.ClearAndDestroyContents()"));
            Assert.That(source, Does.Contain("TryEmbedPlate(plate)"));
            Assert.That(source, Does.Contain("innerContainer.TryAdd(cutlery"));
            Assert.That(source, Does.Not.Contain("innerContainer.TryAdd(plate"));
        });
    }

    [Test]
    public void Animal_map_scenario_source_shape_spawns_only_the_meal_and_loose_cutlery_before_native_ingestion()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var setup = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-animal-map-dining-setup.csx"));
        var arm = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-animal-map-dining-arm.csx"));

        Assert.Multiple(() =>
        {
            Assert.That(setup, Does.Contain("DefDatabase<PawnKindDef>.GetNamed(\"Raccoon\")"));
            Assert.That(setup, Does.Contain("ThingDefOf.Wall"));
            Assert.That(setup, Does.Contain("candidate.Standable(map)"));
            Assert.That(setup, Does.Not.Contain("Faction.OfPlayer"));
            Assert.That(setup, Does.Contain("GenSpawn.Spawn(animal"));
            Assert.That(setup, Does.Contain("GenSpawn.Spawn(meal"));
            Assert.That(setup, Does.Contain("GenSpawn.Spawn(cutlery"));
            Assert.That(setup, Does.Contain("TryEmbedPlate(plate)"));
            Assert.That(setup, Does.Not.Contain("GenSpawn.Spawn(plate"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(meal"));
            Assert.That(setup, Does.Not.Contain("?? throw"));
            Assert.That(setup, Does.Not.Contain(" is null"));
            Assert.That(setup, Does.Not.Contain(".First(cell =>"));
            Assert.That(
                arm,
                Does.Contain("animal.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false)"));
            Assert.That(arm, Does.Contain("animal.needs.food.CurLevel = 0.01f"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
        });
    }

    [Test]
    public void Hospitality_guest_scenario_is_explicit_and_arms_two_native_ingest_jobs()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptor = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hospitality-guest-dining.json"));
        var setup = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hospitality-guest-dining-setup.csx"));
        var arm = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hospitality-guest-dining-arm.csx"));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("\"orion.hospitality\""));
            Assert.That(descriptor, Does.Contain("\"kind\": \"csharp\""));
            Assert.That(setup, Does.Contain("OnGuestJoinedLate"));
            Assert.That(setup, Does.Contain("GetMethod(\"Arrive\")"));
            Assert.That(
                setup,
                Does.Contain("guest.foodRestriction = new Pawn_FoodRestrictionTracker(guest)"),
                "Hospitality's guest thoughts require the ordinary RimWorld food-policy tracker.");
            Assert.That(setup, Does.Contain("innerContainer.TryAdd(personalCutlery"));
            Assert.That(setup, Does.Contain("GenSpawn.Spawn(colonyCutlery"));
            Assert.That(setup, Does.Contain("ThingDefOf.Wall"));
            Assert.That(arm, Does.Contain("JobMaker.MakeJob(JobDefOf.Ingest"));
            Assert.That(arm, Does.Contain("StartJob"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
            Assert.That(arm, Does.Not.Contain("Find.TickManager.TogglePaused()"));
        });
    }

    [Test]
    public void Independent_child_dining_scenario_is_explicit_and_arms_a_native_ingest_job()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptor = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-independent-child-dining.json"));
        var setup = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-independent-child-dining-setup.csx"));
        var arm = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-independent-child-dining-arm.csx"));
        var integrationSource = File.ReadAllText(Path.Combine(
            FindSourceRepositoryRoot(),
            "tests",
            "ImmersiveChefs.InGame.IntegrationTests",
            "FinalizedImmersiveChefsIntegrationTests.cs"));
        var childTestStart = integrationSource.IndexOf(
            "public static void ActiveBiotechIndependentChildCompletesOrdinaryDiningWorkflow()",
            StringComparison.Ordinal);
        var childTestEnd = integrationSource.IndexOf(
            "public static void CompletedMapDiningWithoutCutleryCreatesOneDirtEvent()",
            childTestStart,
            StringComparison.Ordinal);
        Assert.That(childTestStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(childTestEnd, Is.GreaterThan(childTestStart));
        var childTest = integrationSource.Substring(childTestStart, childTestEnd - childTestStart);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("\"ludeon.rimworld.biotech\""));
            Assert.That(descriptor, Does.Contain("\"kind\": \"csharp\""));
            Assert.That(setup, Does.Contain("DevelopmentalStage.Child"));
            Assert.That(setup, Does.Contain("GetNamed(\"MealLavish\")"));
            Assert.That(setup, Does.Contain("TryEmbedPlate(plate)"));
            Assert.That(setup, Does.Contain("GenSpawn.Spawn(cutlery"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(child)"));
            Assert.That(arm, Does.Contain("child.Position.x + 4"));
            Assert.That(arm, Does.Contain("expectedMealCell.GetThingList(map)"));
            Assert.That(arm, Does.Not.Contain("ThingsOfDef"));
            Assert.That(arm, Does.Contain("JobMaker.MakeJob(JobDefOf.Ingest"));
            Assert.That(arm, Does.Contain("child.jobs.StartJob"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
            Assert.That(arm, Does.Not.Contain("Find.TickManager.TogglePaused()"));
            Assert.That(childTest, Does.Contain("TryGetMainTreeThinkNode<JobGiver_GetFood>"));
            Assert.That(childTest, Does.Contain("DevelopmentalStage.Baby"));
            Assert.That(childTest, Does.Contain("toddler.thinker.MainThinkNodeRoot.TryIssueJobPackage"));
            Assert.That(childTest, Does.Not.Contain("DoSingleTick"));
        });
    }

    [Test]
    public void Kitchenware_fabrication_scenario_uses_real_bills_jobs_and_power()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptor = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-fabrication.json"));
        var setup = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-fabrication-setup.csx"));
        var arm = File.ReadAllText(Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-fabrication-arm.csx"));
        var primitiveWorkerBlock = setup.Substring(
            setup.IndexOf("Pawn primitiveCrafter", StringComparison.Ordinal),
            setup.IndexOf("Pawn modernCrafter", StringComparison.Ordinal) -
            setup.IndexOf("Pawn primitiveCrafter", StringComparison.Ordinal));
        var modernWorkerBlock = setup.Substring(
            setup.IndexOf("Pawn modernCrafter", StringComparison.Ordinal),
            setup.IndexOf("if (primitiveCrafter == null", StringComparison.Ordinal) -
            setup.IndexOf("Pawn modernCrafter", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("\"immersive-chefs-kitchenware-fabrication\""));
            Assert.That(descriptor, Does.Contain("\"kind\": \"csharp\""));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakePrimitiveCookware"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeModernCookware"));
            Assert.That(setup, Does.Contain("BlocksGranite"));
            Assert.That(setup, Does.Contain("ThingDefOf.Steel"));
            Assert.That(setup, Does.Contain("new Bill_Production"));
            Assert.That(setup, Does.Contain("new[] { 4, 2, 4, 6, 4, 2 }"));
            Assert.That(setup, Does.Contain("handles.stackCount = 1"));
            Assert.That(setup, Does.Contain("BillStoreModeDefOf.DropOnFloor"));
            Assert.That(setup, Does.Contain("CompPowerBattery"));
            Assert.That(setup, Does.Contain("UpdatePowerNetsAndConnections_First"));
            Assert.That(setup, Does.Contain("WorkTypeDefOf.Smithing"));
            Assert.That(
                primitiveWorkerBlock,
                Does.Contain("WorkTypeIsDisabled(WorkTypeDefOf.Crafting)"));
            Assert.That(
                modernWorkerBlock,
                Does.Contain("WorkTypeIsDisabled(WorkTypeDefOf.Smithing)"));
            Assert.That(arm, Does.Contain("WorkGiver_DoBill"));
            Assert.That(arm, Does.Contain("JobOnThing"));
            Assert.That(arm, Does.Contain("StartJob"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
            Assert.That(arm, Does.Not.Contain("Find.TickManager.TogglePaused()"));
        });
    }

    [Test]
    public void Kitchenware_route_matrix_scenario_uses_loaded_adobe_and_native_fabrication_jobs()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-route-matrix.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-route-matrix-setup.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-route-matrix-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var arm = File.ReadAllText(armPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("argon.expandedmaterials.masonry"));
            Assert.That(descriptor, Does.Contain("oskarpotocki.vanillafactionsexpanded.core"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeSoftPlates"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeSoftCutlery"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeAdobePlates"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeMedievalCookware"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_SmithPlates"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_SmithCutlery"));
            Assert.That(setup, Does.Contain("EM_AdobeBricks"));
            Assert.That(setup, Does.Contain("FueledSmithy"));
            Assert.That(setup, Does.Contain("CompRefuelable"));
            Assert.That(setup, Does.Contain("new Bill_Production"));
            Assert.That(arm, Does.Contain("WorkGiver_DoBill"));
            Assert.That(arm, Does.Contain("JobOnThing"));
            Assert.That(arm, Does.Contain("StartJob"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
            Assert.That(arm, Does.Not.Contain("GenRecipe.MakeRecipeProducts"));
        });
    }

    [Test]
    public void Chefs_knife_scenario_fabricates_without_synthetically_equipping_the_product()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-chefs-knife.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-chefs-knife-setup.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-chefs-knife-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var arm = File.ReadAllText(armPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-chefs-knife"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_MakeChefsKnife"));
            Assert.That(setup, Does.Contain("TableMachining"));
            Assert.That(setup, Does.Contain("CompPowerBattery"));
            Assert.That(setup, Does.Contain("new Bill_Production"));
            Assert.That(setup, Does.Contain("MeleeWeapon_Knife"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_ChefsKnife"));
            Assert.That(setup, Does.Not.Contain("Wear("));
            Assert.That(arm, Does.Contain("WorkGiver_DoBill"));
            Assert.That(arm, Does.Contain("JobOnThing"));
            Assert.That(arm, Does.Contain("StartJob"));
            Assert.That(arm, Does.Contain("Find.TickManager.Pause()"));
            Assert.That(arm, Does.Not.Contain("GenRecipe.MakeRecipeProducts"));
        });
    }

    [Test]
    public void Glitterworld_trade_scenario_seeds_trader_stock_without_granting_colony_ownership()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-glitterworld-trade.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-glitterworld-trade-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-glitterworld-trade-stock.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-glitterworld-trade-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var arm = File.ReadAllText(armPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-glitterworld-trade"));
            Assert.That(setup, Does.Contain("OrbitalTradeBeacon"));
            Assert.That(stock, Does.Contain("new TradeShip"));
            Assert.That(stock, Does.Contain("Orbital_Exotic"));
            Assert.That(stock, Does.Contain("ImmersiveChefs_GlitterworldCookware"));
            Assert.That(stock, Does.Contain("GetDirectlyHeldThings"));
            Assert.That(stock, Does.Contain("passingShipManager.AddShip"));
            Assert.That(stock, Does.Not.Contain("GenSpawn.Spawn(glitterworld"));
            Assert.That(arm, Does.Contain("TryOpenComms"));
            Assert.That(arm, Does.Not.Contain("TradeAction.PlayerBuys"));
        });
    }

    [Test]
    public void Cooking_ware_selection_scenario_uses_native_bills_and_vanilla_urgent_food_boundary()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooking-ware-selection.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooking-ware-selection-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooking-ware-selection-stock.csx");
        var urgencyPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooking-ware-selection-urgency.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooking-ware-selection-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(urgencyPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var urgency = File.ReadAllText(urgencyPath);
        var arm = File.ReadAllText(armPath);
        var scenarioSource = setup + stock + urgency + arm;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-cooking-ware-selection"));
            Assert.That(setup, Does.Contain("CookMealSimple"));
            Assert.That(stock, Does.Contain("MarkDirty"));
            Assert.That(urgency, Does.Contain("JobGiver_GetFood"));
            Assert.That(urgency, Does.Not.Contain("UrgentProductionRequestRegistry"));
            Assert.That(arm, Does.Contain("WorkGiver_DoBill"));
            Assert.That(arm, Does.Contain("JobOnThing"));
            Assert.That(scenarioSource, Does.Not.Contain("MakeRecipeProducts"));
            Assert.That(scenarioSource, Does.Not.Contain("ThingMaker.MakeThing(ThingDefOf.MealSimple"));
            Assert.That(scenarioSource, Does.Not.Contain("ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(\"MealSimple\")"));
        });
    }

    [Test]
    public void Recipe_complexity_scenario_uses_exact_vanilla_recipes_and_native_bill_jobs()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-recipe-complexity.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-recipe-complexity-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-recipe-complexity-stock.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-recipe-complexity-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var arm = File.ReadAllText(armPath);
        var scenarioSource = setup + stock + arm;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-recipe-complexity"));
            Assert.That(setup, Does.Contain("CookMealSimple"));
            Assert.That(setup, Does.Contain("CookMealFine"));
            Assert.That(setup, Does.Contain("CookMealLavish"));
            Assert.That(stock, Does.Contain("CompSanitation"));
            Assert.That(arm, Does.Contain("WorkGiver_DoBill"));
            Assert.That(arm, Does.Contain("JobOnThing"));
            Assert.That(scenarioSource, Does.Not.Contain("MakeRecipeProducts"));
            Assert.That(scenarioSource, Does.Not.Contain("ThingMaker.MakeThing(ThingDefOf.Meal"));
        });
    }

    [Test]
    public void Cooperative_cooking_scenario_leaves_lead_and_assistant_jobs_to_the_native_scheduler()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooperative-cooking.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooperative-cooking-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooperative-cooking-stock.csx");
        var linkPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooperative-cooking-link.csx");
        var activatePath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-cooperative-cooking-activate.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(linkPath), Is.True);
            Assert.That(File.Exists(activatePath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var link = File.ReadAllText(linkPath);
        var activate = File.ReadAllText(activatePath);
        var scenarioSource = setup + stock + link + activate;
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-cooperative-cooking"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Assisted Lead"));
            Assert.That(setup, Does.Contain("Assisted Specialist"));
            Assert.That(setup, Does.Contain("Control Lead"));
            Assert.That(link, Does.Contain("ImmersiveChefs_SauceStation"));
            Assert.That(activate, Does.Contain("LinkedFacilitiesListForReading.Contains"));
            Assert.That(activate, Does.Contain("foreach (var candidateStove"));
            Assert.That(
                activate,
                Does.Not.Contain("FirstOrDefault(thing => thing.TryGetComp<CompAffectedByFacilities>()?"));
            Assert.That(stock, Does.Contain("CookMealSimple"));
            Assert.That(stock, Does.Contain("SetPawnRestriction"));
            Assert.That(setup, Does.Contain("SetPriority(cookingWorkType, 1)"));
            Assert.That(scenarioSource, Does.Not.Contain("StartJob"));
            Assert.That(scenarioSource, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(scenarioSource, Does.Not.Contain("EndCurrentJob"));
            Assert.That(scenarioSource, Does.Not.Contain("Log.Message("));
            Assert.That(scenarioSource, Does.Not.Contain("NotifyWorkTick"));
            Assert.That(scenarioSource, Does.Not.Contain("MakeRecipeProducts"));
        });
    }

    [Test]
    public void Prepared_food_workflow_scenario_leaves_preparation_and_cooking_to_native_work()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-workflow.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-workflow-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-workflow-stock.csx");
        var activatePath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-workflow-activate.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(activatePath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var activate = File.ReadAllText(activatePath);
        var scenarioSource = setup + stock + activate;
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-prepared-food-workflow"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Prep Chef"));
            Assert.That(setup, Does.Contain("Prepared Cook"));
            Assert.That(setup, Does.Contain("Raw Cook"));
            Assert.That(stock, Does.Contain("ImmersiveChefs_PrepareIngredients"));
            Assert.That(stock, Does.Contain("CookMealSimple"));
            Assert.That(stock, Does.Contain("SetPawnRestriction"));
            Assert.That(stock, Does.Contain("RepeatCount"));
            Assert.That(activate, Does.Contain("prepChef.drafter.Drafted = false"));
            Assert.That(scenarioSource, Does.Not.Contain("StartJob"));
            Assert.That(scenarioSource, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(scenarioSource, Does.Not.Contain("EndCurrentJob"));
            Assert.That(scenarioSource, Does.Not.Contain("MakeRecipeProducts"));
            Assert.That(scenarioSource, Does.Not.Contain("ApplyProducts"));
            Assert.That(scenarioSource, Does.Not.Contain("NotifyWorkTick"));
            Assert.That(scenarioSource, Does.Not.Contain("RotProgress"));
            Assert.That(scenarioSource, Does.Not.Contain("DoSingleTick"));
        });
    }

    [Test]
    public void Prepared_food_save_load_scenario_only_arranges_the_persistent_fixture()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-save-load.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-food-save-load-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-prepared-food-save-load"));
            Assert.That(descriptor, Does.Contain("\"id\": \"pre\""));
            Assert.That(setup, Does.Contain("Prepared Save Reload"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_PreparedFood"));
            Assert.That(setup, Does.Contain("Preparation quality"));
            Assert.That(setup, Does.Contain("RotProgress"));
            Assert.That(setup, Does.Not.Contain("GameDataSaveLoader"));
            Assert.That(setup, Does.Not.Contain("SaveGame"));
            Assert.That(setup, Does.Not.Contain("LoadGame"));
            Assert.That(setup, Does.Not.Contain("Scribe"));
        });
    }

    [Test]
    public void Microwave_reheating_scenario_leaves_food_choice_heating_and_ingestion_to_player_actions()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-microwave-reheating.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-microwave-reheating-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-microwave-reheating"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Microwave Diner"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Microwave"));
            Assert.That(setup, Does.Contain("CompCulinaryState"));
            Assert.That(setup, Does.Contain("80,"));
            Assert.That(setup, Does.Contain("-5f"));
            Assert.That(setup, Does.Contain("TryEmbedPlate"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(setup, Does.Contain("PowerNet"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(meal)"));
            Assert.That(setup, Does.Contain("Find.CameraDriver.SetRootPosAndSize"));
            Assert.That(setup, Does.Contain("diner.drafter.Drafted = true"));
            Assert.That(setup, Does.Not.Contain("JobDefOf.Ingest"));
            Assert.That(setup, Does.Not.Contain("JobGiver_GetFood"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(setup, Does.Not.Contain("TryReheat"));
            Assert.That(setup, Does.Not.Contain("ReheatCurrentServing"));
        });
    }

    [Test]
    public void Meal_cooling_holders_scenario_only_arranges_real_ambient_refrigerated_and_frozen_contexts()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-meal-cooling-holders.json");
        var setupFileNames = new[]
        {
            "immersive-chefs-meal-cooling-holders-prepare.csx",
            "immersive-chefs-meal-cooling-holders-meals.csx",
            "immersive-chefs-meal-cooling-holders-build.csx",
            "immersive-chefs-meal-cooling-holders-frame.csx"
        };
        var setupPaths = setupFileNames
            .Select(fileName => Path.Combine(scenarioDirectory, fileName))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(setupPaths.All(File.Exists), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = string.Join("\n", setupPaths.Select(File.ReadAllText));
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-meal-cooling-holders"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            foreach (var setupFileName in setupFileNames)
            {
                Assert.That(descriptor, Does.Contain(setupFileName));
            }
            Assert.That(setup, Does.Contain("Ambient Cooling Control"));
            Assert.That(setup, Does.Contain("Refrigerated Cooling Control"));
            Assert.That(setup, Does.Contain("Frozen Cooling Control"));
            Assert.That(setup, Does.Contain("ThingDefOf.Cooler"));
            Assert.That(setup, Does.Contain("ThingDefOf.Heater"));
            Assert.That(setup, Does.Contain("CompTempControl"));
            Assert.That(setup, Does.Contain("PowerNet"));
            Assert.That(setup, Does.Contain("RoofDefOf.RoofConstructed"));
            Assert.That(setup, Does.Contain("PsychologicallyOutdoors"));
            Assert.That(setup, Does.Contain("new ImmersiveChefs.CulinaryServingRecord(70, 70f"));
            Assert.That(setup, Does.Contain("Find.CameraDriver.SetRootPosAndSize"));
            Assert.That(setup, Does.Not.Contain("AdvanceTemperature"));
            Assert.That(setup, Does.Not.Contain("PeekCurrentServing"));
            Assert.That(setup, Does.Not.Contain("DoSingleTick"));
            Assert.That(setup, Does.Not.Contain("Find.TickManager.CurTimeSpeed"));
            Assert.That(setup, Does.Not.Contain("room.Temperature ="));
            Assert.That(setup, Does.Contain("AppDomain.CurrentDomain.SetData"));
            Assert.That(setup, Does.Contain("AppDomain.CurrentDomain.GetData"));
        });
    }

    [Test]
    public void Meal_serving_stack_scenario_leaves_the_exact_native_split_and_ingestion_to_player_action()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-meal-serving-stack.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-meal-serving-stack-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-meal-serving-stack"));
            Assert.That(descriptor, Does.Contain("immersive-chefs-meal-serving-stack-setup.csx"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("meal.stackCount = 3"));
            Assert.That(setup, Does.Contain("new ImmersiveChefs.CulinaryServingRecord(20, 70f"));
            Assert.That(setup, Does.Contain("new ImmersiveChefs.CulinaryServingRecord(50, 70f"));
            Assert.That(setup, Does.Contain("new ImmersiveChefs.CulinaryServingRecord(80, 70f"));
            Assert.That(setup, Does.Contain("plate.stackCount = 3"));
            Assert.That(setup, Does.Contain("cutlery.stackCount = 3"));
            Assert.That(setup, Does.Contain("diner.drafter.Drafted = true"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(meal)"));
            Assert.That(setup, Does.Not.Contain("SplitOff"));
            Assert.That(setup, Does.Not.Contain("ConsumeOne"));
            Assert.That(setup, Does.Not.Contain("PostIngested"));
            Assert.That(setup, Does.Not.Contain("JobDefOf.Ingest"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(setup, Does.Not.Contain("DoSingleTick"));
        });
    }

    [Test]
    public void Hidden_provenance_scenario_uses_native_bill_and_food_selection_boundaries()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hidden-provenance.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hidden-provenance-setup.csx");
        var stockPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hidden-provenance-stock.csx");
        var activatePath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-hidden-provenance-activate.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(stockPath), Is.True);
            Assert.That(File.Exists(activatePath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var stock = File.ReadAllText(stockPath);
        var activate = File.ReadAllText(activatePath);
        var scenarioSource = setup + stock + activate;
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-hidden-provenance"));
            Assert.That(setup, Does.Contain("Hidden Bill Cook"));
            Assert.That(setup, Does.Contain("Hidden Policy Diner"));
            Assert.That(stock, Does.Contain("CookMealSimple"));
            Assert.That(stock, Does.Contain("Meat_Human"));
            Assert.That(stock, Does.Contain("RawRice"));
            Assert.That(stock, Does.Contain("SetPawnRestriction"));
            Assert.That(stock, Does.Contain("CurrentFoodPolicy"));
            Assert.That(stock, Does.Contain("exactSourcesHidden: true"));
            Assert.That(activate, Does.Contain("Hidden Bill Cook"));
            Assert.That(activate, Does.Contain("Hidden Policy Diner"));
            Assert.That(scenarioSource, Does.Not.Contain("StartJob"));
            Assert.That(scenarioSource, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(scenarioSource, Does.Not.Contain("JobGiver_GetFood"));
            Assert.That(scenarioSource, Does.Not.Contain("MakeRecipeProducts"));
            Assert.That(scenarioSource, Does.Not.Contain("FoodUtility.TryFindBestFoodSourceFor"));
        });
    }

    [Test]
    public void Handheld_food_scenario_uses_vanilla_food_choice_and_ingest_jobs()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-handheld-food-exclusions.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-handheld-food-exclusions-setup.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-handheld-food-exclusions-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var arm = File.ReadAllText(armPath);
        var scenarioSource = setup + arm;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-handheld-food-exclusions"));
            Assert.That(setup, Does.Contain("ThingDefOf.Pemmican"));
            Assert.That(setup, Does.Contain("ThingDefOf.MealSurvivalPack"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Plate"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(arm, Does.Contain("JobGiver_GetFood"));
            Assert.That(arm, Does.Contain("TryGiveJob"));
            Assert.That(arm, Does.Contain("JobDefOf.Ingest"));
            Assert.That(arm, Does.Contain("ReferenceEquals(job.targetA.Thing, expectedFood)"));
            Assert.That(scenarioSource, Does.Not.Contain(".Ingested("));
            Assert.That(scenarioSource, Does.Not.Contain("TryEmbedPlate"));
        });
    }

    [Test]
    public void Nutrient_paste_dining_scenario_uses_the_vanilla_dispenser_ingest_path()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-nutrient-paste-dining.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-nutrient-paste-dining-setup.csx");
        var armPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-nutrient-paste-dining-arm.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(armPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var arm = File.ReadAllText(armPath);
        var scenarioSource = setup + arm;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-nutrient-paste-dining"));
            Assert.That(setup, Does.Contain("NutrientPasteDispenser"));
            Assert.That(setup, Does.Contain("ThingDefOf.Hopper"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Plate"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(arm, Does.Contain("JobGiver_GetFood"));
            Assert.That(arm, Does.Contain("TryGiveJob"));
            Assert.That(arm, Does.Contain("JobDefOf.Ingest"));
            Assert.That(arm, Does.Contain("ReferenceEquals(job.targetA.Thing, dispenser)"));
            Assert.That(scenarioSource, Does.Not.Contain("TryDispenseFood"));
            Assert.That(scenarioSource, Does.Not.Contain(".Ingested("));
            Assert.That(scenarioSource, Does.Not.Contain("BindPastePlate"));
        });
    }

    [Test]
    public void Prepared_paste_scenario_leaves_the_dispense_order_to_native_player_input()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-paste-dispensing.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-prepared-paste-dispensing-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-prepared-paste-dispensing"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("NutrientPasteDispenser"));
            Assert.That(setup, Does.Contain("ThingDefOf.Hopper"));
            Assert.That(setup, Does.Contain("map.fogGrid.Unfog(cell)"));
            Assert.That(setup, Does.Contain("Find.CameraDriver.SetRootPosAndSize"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(worker)"));
            Assert.That(setup, Does.Not.Contain("TryDispenseFood"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_PreparedFood"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_DispensePreparedPaste"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
        });
    }

    [Test]
    public void Vnpe_prepared_paste_scenario_uses_the_real_pipe_tap_and_leaves_dispensing_to_player_input()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-vnpe-prepared-paste.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-vnpe-prepared-paste-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-vnpe-prepared-paste"));
            Assert.That(descriptor, Does.Contain("oskarpotocki.vanillafactionsexpanded.core"));
            Assert.That(descriptor, Does.Contain("vanillaexpanded.vnutriente"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("VNPE_NutrientPasteTap"));
            Assert.That(setup, Does.Contain("VNPE_NutrientPasteVat"));
            Assert.That(setup, Does.Contain("VNPE_NutrientPastePipe"));
            Assert.That(setup, Does.Contain("PipeSystem.CompResourceStorage"));
            Assert.That(setup, Does.Contain("AmountStored"));
            Assert.That(setup, Does.Contain("Find.CameraDriver.SetRootPosAndSize"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(worker)"));
            Assert.That(setup, Does.Not.Contain("TryDispenseFood"));
            Assert.That(setup, Does.Not.Contain("TryDispenseFoodOverride"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_PreparedFood"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_DispensePreparedPaste"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
        });
    }

    [Test]
    public void Common_sense_cleanup_scenario_builds_both_dining_preconditions_and_leaves_outcomes_to_player_input()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-common-sense-cleanup.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-common-sense-cleanup-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-common-sense-cleanup"));
            Assert.That(descriptor, Does.Contain("avilmask.commonsense"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Common Sense Diner"));
            Assert.That(setup, Does.Contain("Common Sense Nurse"));
            Assert.That(setup, Does.Contain("Common Sense Patient"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Dishwasher"));
            Assert.That(setup, Does.Contain("TryEmbedPlate"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(setup, Does.Contain("Find.CameraDriver.SetRootPosAndSize"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(diner)"));
            Assert.That(setup, Does.Contain("worker.workSettings.SetPriority(workType, 0)"));
            Assert.That(setup, Does.Contain("nurse.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1)"));
            Assert.That(setup, Does.Contain("nurse.drafter.Drafted = true"));
            Assert.That(setup, Does.Not.Contain("JobDefOf.Ingest"));
            Assert.That(setup, Does.Not.Contain("JobDefOf.FeedPatient"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
        });
    }

    [Test]
    public void Common_sense_no_route_scenario_requires_player_to_restore_a_route_before_ordinary_cleaning()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-common-sense-no-route.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-common-sense-no-route-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-common-sense-no-route"));
            Assert.That(descriptor, Does.Contain("avilmask.commonsense"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Common Sense No Route Diner"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Dishwasher"));
            Assert.That(setup, Does.Contain("SwitchIsOn = false"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(setup, Does.Contain("TryEmbedPlate"));
            Assert.That(setup, Does.Contain("WorkTypeDefOf.Cleaning"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(diner)"));
            Assert.That(setup, Does.Not.Contain("JobDefOf.Ingest"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_DoDishes"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
        });
    }

    [Test]
    public void Imported_meal_plating_scenario_leaves_the_native_cooking_job_to_player_time_control()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-imported-meal-plating.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-imported-meal-plating-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-imported-meal-plating"));
            Assert.That(descriptor, Does.Not.Contain("-arm.csx"));
            Assert.That(setup, Does.Contain("Imported Meal Plating Cook"));
            Assert.That(setup, Does.Contain("ThingDefOf.MealSimple"));
            Assert.That(setup, Does.Contain("ImmersiveChefs_Plate"));
            Assert.That(setup, Does.Contain("FueledStove"));
            Assert.That(setup, Does.Contain("SetPriority(cookingWorkType, 1)"));
            Assert.That(setup, Does.Contain("Find.Selector.Select(meal)"));
            Assert.That(setup, Does.Not.Contain("WorkGiver_PlateMeals"));
            Assert.That(setup, Does.Not.Contain("ImmersiveChefs_PlateMeals"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(setup, Does.Not.Contain("TryEmbedPlate"));
        });
    }

    [Test]
    public void Scenario_request_ids_are_deterministically_bounded_for_descriptive_names()
    {
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            LoadFunction("New-GatewayScenarioRequestId") +
            "$first = New-GatewayScenarioRequestId -ScenarioName 'immersive-chefs-dubs-processor-dishwasher' -StepId 'processor-fixture'\n" +
            "$repeat = New-GatewayScenarioRequestId -ScenarioName 'immersive-chefs-dubs-processor-dishwasher' -StepId 'processor-fixture'\n" +
            "$other = New-GatewayScenarioRequestId -ScenarioName 'immersive-chefs-dubs-processor-dishwasher' -StepId 'dubs-plumbing'\n" +
            "Write-Output ($first + '|' + $repeat + '|' + $other)\n";

        var result = RunPowerShellScript(script);
        var values = result.StandardOutput.Trim().Split('|');

        Assert.That(result.ExitCode, Is.Zero, result.StandardError);
        Assert.That(values, Has.Length.EqualTo(3));
        if (values.Length != 3)
        {
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Has.Length.InRange(1, 64));
            Assert.That(values[0], Does.Match("^[A-Za-z0-9._:-]+$"));
            Assert.That(values[0], Is.EqualTo(values[1]));
            Assert.That(values[0], Is.Not.EqualTo(values[2]));
        });
    }

    [Test]
    public void Dubs_processor_dishwasher_scenario_uses_real_plumbing_and_leaves_the_cycle_to_player_time()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-dubs-processor-dishwasher.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-dubs-processor-dishwasher-setup.csx");
        var baseSetupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-processor-dishwasher-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(baseSetupPath), Is.True);
        });

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);
        var combinedSetup = setup + File.ReadAllText(baseSetupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("syrchalis.processor.framework"));
            Assert.That(descriptor, Does.Contain("Dubwise.DubsBadHygiene"));
            Assert.That(descriptor, Does.Contain("fumblesneeze.immersivechefs"));
            Assert.That(setup, Does.Contain("WaterTowerS"));
            Assert.That(setup, Does.Contain("sewagePipeHidden"));
            Assert.That(setup, Does.Contain("towerCell.x + 2"));
            Assert.That(setup, Does.Contain("dishwasher.Position.x - 1"));
            Assert.That(setup, Does.Contain("WaterStorage"));
            Assert.That(setup, Does.Contain("10f"));
            Assert.That(combinedSetup, Does.Contain("ImmersiveChefs_Cookware"));
            Assert.That(combinedSetup, Does.Contain("ImmersiveChefs_Plate"));
            Assert.That(combinedSetup, Does.Contain("ImmersiveChefs_Cutlery"));
            Assert.That(combinedSetup, Does.Contain("WorkTypeDefOf.Hauling"));
            Assert.That(
                combinedSetup,
                Does.Contain("ThingDefOf.Wall"),
                "The passive fixture must isolate its native haulers from unrelated colony hauling and hand-washing work.");
            Assert.That(combinedSetup, Does.Not.Contain("StartJob"));
            Assert.That(combinedSetup, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(combinedSetup, Does.Not.Contain("TryConsumeCycleWater"));
        });
    }

    [Test]
    public void Processor_interruption_scenario_is_passive_and_provides_a_basic_worker()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-processor-dishwasher-interruption.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-processor-dishwasher-interruption-setup.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
        });

        if (!File.Exists(descriptorPath) || !File.Exists(setupPath))
        {
            return;
        }

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("immersive-chefs-processor-dishwasher-setup.csx"));
            Assert.That(descriptor, Does.Contain("syrchalis.processor.framework"));
            Assert.That(descriptor, Does.Contain("fumblesneeze.immersivechefs"));
            Assert.That(setup, Does.Contain("GetNamed(\"BasicWorker\")"));
            Assert.That(setup, Does.Contain("Dishwasher Switch Worker"));
            Assert.That(setup, Does.Not.Contain("DoFlick"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
            Assert.That(setup, Does.Not.Contain("TryTakeOrderedJob"));
            Assert.That(setup, Does.Not.Contain("EjectAllDirty"));
            Assert.That(setup, Does.Not.Contain("Find.TickManager.DoSingleTick"));
        });
    }

    [Test]
    public void Terminal_plate_scenario_is_passive_and_exposes_rot_and_fire_targets()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-terminal-plate-conservation.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-terminal-plate-conservation-setup.csx");
        var createPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-terminal-plate-conservation-create.csx");
        var framePath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-terminal-plate-conservation-frame.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(createPath), Is.True);
            Assert.That(File.Exists(framePath), Is.True);
        });

        if (!File.Exists(descriptorPath) || !File.Exists(setupPath) ||
            !File.Exists(createPath) || !File.Exists(framePath))
        {
            return;
        }

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath) +
                    File.ReadAllText(createPath) +
                    File.ReadAllText(framePath);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("fumblesneeze.immersivechefs"));
            Assert.That(setup, Does.Contain("RotProgress"));
            Assert.That(setup, Does.Contain("GenStuff.AllowedStuffsFor"));
            Assert.That(setup, Does.Contain("GetStatValueAbstract(StatDefOf.Flammability"));
            Assert.That(setup, Does.Contain("TryEmbedPlate"));
            Assert.That(setup, Does.Not.Contain("expiringMeal.Destroy("));
            Assert.That(setup, Does.Not.Contain("flammableFireMeal.Destroy("));
            Assert.That(setup, Does.Not.Contain("nonflammableFireMeal.Destroy("));
            Assert.That(setup, Does.Not.Contain("TakeDamage"));
            Assert.That(setup, Does.Not.Contain("TryStartFireIn"));
            Assert.That(setup, Does.Not.Contain("DoSingleTick"));
        });
    }

    [Test]
    public void Kitchenware_alert_scenario_arranges_an_active_stove_and_silent_controls_without_driving_them()
    {
        var scenarioDirectory = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Scenarios");
        var descriptorPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-alerts.json");
        var setupPath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-alerts-setup.csx");
        var framePath = Path.Combine(
            scenarioDirectory,
            "immersive-chefs-kitchenware-alerts-frame.csx");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(descriptorPath), Is.True);
            Assert.That(File.Exists(setupPath), Is.True);
            Assert.That(File.Exists(framePath), Is.True);
        });

        if (!File.Exists(descriptorPath) || !File.Exists(setupPath) || !File.Exists(framePath))
        {
            return;
        }

        var descriptor = File.ReadAllText(descriptorPath);
        var setup = File.ReadAllText(setupPath) + File.ReadAllText(framePath);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Does.Contain("fumblesneeze.immersivechefs"));
            Assert.That(setup, Does.Contain("FueledStove"));
            Assert.That(setup, Does.Contain("Campfire"));
            Assert.That(setup, Does.Contain("RawBerries"));
            Assert.That(setup, Does.Contain("CookMealSimple"));
            Assert.That(setup, Does.Contain("BillStack.AddBill"));
            Assert.That(setup, Does.Contain("MarkDirty"));
            Assert.That(setup, Does.Contain("SetForbidden(true"));
            Assert.That(setup, Does.Contain("Find.TickManager.Pause"));
            Assert.That(setup, Does.Not.Contain("SetForbidden(false"));
            Assert.That(setup, Does.Not.Contain("WaterShallow"));
            Assert.That(setup, Does.Not.Contain("DoSingleTick"));
            Assert.That(setup, Does.Not.Contain("GetReport"));
            Assert.That(setup, Does.Not.Contain("TakeOrderedJob"));
            Assert.That(setup, Does.Not.Contain("StartJob"));
        });
    }

    private static InvocationResult InvokeScenarioResolver(string? descriptorJson = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeScenarioSelectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "caravan-dining.json"),
                descriptorJson ?? "{\"schemaVersion\":1,\"name\":\"caravan-dining\",\"requiredPackageIds\":[],\"steps\":[]}",
                new UTF8Encoding(false));
            var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
            var invocationPath = Path.Combine(root, "invoke.ps1");
            var invocation =
                "$ErrorActionPreference = 'Stop'\n" +
                "$tokens = $null; $parseErrors = $null\n" +
                $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
                "$functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Resolve-GatewaySmokeScenario' }, $true)\n" +
                "if ($null -eq $functionAst) { throw 'Function was not found: Resolve-GatewaySmokeScenario' }\n" +
                "Invoke-Expression $functionAst.Extent.Text\n" +
                $"$none = Resolve-GatewaySmokeScenario -ScenarioName '' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                $"$regression = Resolve-GatewaySmokeScenario -ScenarioName 'gateway-regression' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                $"$custom = Resolve-GatewaySmokeScenario -ScenarioName 'caravan-dining' -ScenarioDirectory {PowerShellLiteral(root)} -Quicktest $true\n" +
                "$parts = @($none, $regression, $custom) | ForEach-Object { [string]$_.Name + '|' + [string]$_.RunsGatewayRegression + '|' + [System.IO.Path]::GetFileName([string]$_.DescriptorPath) }\n" +
                "Write-Output ($parts -join ';')\n";
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string LoadFunction(string functionName)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        return
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            $"$functionAst = $ast.Find({{ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq '{functionName}' }}, $true)\n" +
            $"if ($null -eq $functionAst) {{ throw 'Function was not found: {functionName}' }}\n" +
            "Invoke-Expression $functionAst.Extent.Text\n";
    }

    private static InvocationResult RunPowerShellScript(string script)
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeScenarioSelectionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invocationPath = Path.Combine(root, "invoke.ps1");
            File.WriteAllText(invocationPath, script, new UTF8Encoding(false));
            return RunPowerShell(invocationPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult RunPowerShell(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Gateway scenario resolver probe timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

    private static string FindSourceRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the source repository root.");
    }

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

    private sealed class InvocationResult
    {
        internal InvocationResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
    }
}
