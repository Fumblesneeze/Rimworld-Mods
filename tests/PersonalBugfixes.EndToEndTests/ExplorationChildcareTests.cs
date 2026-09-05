using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using PersonalBugfixes.Exploration;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace PersonalBugfixes.EndToEndTests;

[RimWorldEndToEndTest("personal-bugfixes.exploration-childcare", PersonalBugfixesMod.PackageId,
    "brrainz.harmony", "ludeon.rimworld", "ludeon.rimworld.biotech",
    "oskarpotocki.vanillafactionsexpanded.core", "thelastbulletbender.rwexploration",
    "cyanobot.toddlers", PersonalBugfixesMod.PackageId,
    MaxFrames = 30000, MaxGameTicks = 18000, MaxWallClockSeconds = 600)]
public sealed class ExplorationChildcareTests : IRimWorldEndToEndTest
{
    private const string SaveName = "PersonalBugfixes-childcare";
    private Pawn adult = null!, baby = null!;
    private Building_Bed crib = null!;
    private Type componentType = null!;
    private FieldInfo learned = null!;
    private FieldInfo updateType = null!, revealAll = null!;
    private object oldUpdate = null!, oldReveal = null!;
    private bool[] oldFlags = null!, savedFlags = null!;
    private string adultId = "", babyId = "", cribId = "";
    private readonly List<string> wallIds = new();
    private readonly Dictionary<IntVec3, RoofDef> roofs = new();

    private object Component => Find.World.components.Single(c => c.GetType() == componentType);
    private List<bool> Flags => (List<bool>)learned.GetValue(Component);

    public void Arrange(IEndToEndContext context)
    {
        var result = PersonalBugfixesMod.StartupResults.Single();
        EndToEndAssert.Equal(FixState.Applied, result.State, result.ToString());
        var map = Find.CurrentMap;
        var assembly = LoadedModManager.RunningModsListForReading.Single(m =>
            m.PackageIdPlayerFacing.Equals(ExplorationFix.PackageId, StringComparison.OrdinalIgnoreCase))
            .assemblies.loadedAssemblies.Single(a => a.GetName().Name == "RimworldExplorationMode");
        componentType = assembly.GetType("RimworldExploration.WorldFeatureManager", true);
        learned = componentType.GetField("learnedFeatures");
        var visibility = assembly.GetType("RimworldExploration.VisibilityManager", true);
        updateType = visibility.GetField("_updateType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        revealAll = visibility.GetField("RevealAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        oldUpdate = updateType.GetValue(null); oldReveal = revealAll.GetValue(null);
        oldFlags = Flags.ToArray();
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        EndToEndAssert.True(!File.Exists(savePath), "Test save name must be unused in disposable save data.");
        context.DeferCleanup(() => { if (File.Exists(savePath)) File.Delete(savePath); });
        context.DeferCleanup(() =>
        {
            // Resolve by ID because native loading replaces every game object.
            var current = Find.CurrentMap;
            foreach (var id in new[] { babyId, adultId, cribId }.Concat(wallIds))
            {
                var thing = current.listerThings.AllThings.FirstOrDefault(t => t.ThingID == id);
                if (thing != null && !thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            }
            foreach (var pair in roofs) current.roofGrid.SetRoof(pair.Key, pair.Value);
            learned.SetValue(Component, oldFlags.ToList());
            updateType.SetValue(null, oldUpdate); revealAll.SetValue(null, oldReveal);
        });
        var cell = map.AllCells.Where(c => CellRect.CenteredOn(c, 5).Cells.All(p =>
                p.InBounds(map) && p.Standable(map) && !p.Fogged(map) && p.GetThingList(map).Count == 0))
            .OrderBy(c => c.DistanceToSquared(map.Center)).First();
        var roomRect = CellRect.CenteredOn(cell, 5);
        foreach (var edge in roomRect.EdgeCells)
        {
            var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
            wall.SetFaction(Faction.OfPlayer);
            wallIds.Add(wall.ThingID);
            GenSpawn.Spawn(wall, edge, map);
        }
        foreach (var inside in roomRect.ContractedBy(1).Cells)
        {
            roofs[inside] = map.roofGrid.RoofAt(inside);
            map.roofGrid.SetRoof(inside, RoofDefOf.RoofConstructed);
        }
        adult = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, fixedBiologicalAge: 28,
            fixedChronologicalAge: 28, forceNoGear: true,
            validatorPostGear: pawn => !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Childcare)));
        baby = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, fixedBiologicalAge: 0.1f,
            fixedChronologicalAge: 0.1f, developmentalStages: DevelopmentalStage.Baby, forceNoGear: true, allowDowned: true));
        adultId = adult.ThingID; babyId = baby.ThingID;
        crib = (Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Crib"), ThingDefOf.WoodLog);
        cribId = crib.ThingID;
        crib.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(crib, cell, map);
        GenSpawn.Spawn(adult, cell + new IntVec3(3, 0, 0), map);
        GenSpawn.Spawn(baby, cell + new IntVec3(4, 0, 0), map);
        baby.GetRoom().Temperature = 21f;
        EndToEndAssert.True(baby.DevelopmentalStage == DevelopmentalStage.Baby, "Fixture must be a real baby.");
        EndToEndAssert.True(!adult.WorkTypeIsDisabled(WorkTypeDefOf.Childcare), "Fixture adult must be capable of childcare.");
        baby.needs.rest.CurLevel = 0.1f;
        baby.needs.food.CurLevel = 1f;
        adult.needs.food.CurLevel = 1f;
        adult.needs.rest.CurLevel = 1f;
        adult.workSettings.SetPriority(WorkTypeDefOf.Childcare, 1);
        baby.ownership.ClaimBedIfNonMedical(crib);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(w => w.GetType().FullName == "LudeonTK.EditWindow_Log"))
            yield return new WindowCancelActionStep("close startup diagnostic log", "LudeonTK.EditWindow_Log");
        yield return new TimeControlActionStep("pause before childcare reproduction", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame adult, baby and crib", new[] { adultId, babyId, cribId }, 170);
        yield return new SelectionActionStep("select the adult", new[] { adultId }, false);
        yield return new AssertionStep("arrange one missing exploration discovery flag", _ =>
        {
            var count = Find.World.features.features.Count;
            EndToEndAssert.True(count >= 3, "Generated world needs at least three features.");
            ArrangePendingShortList();
        });
        yield return Snapshot("short-list before native baby action");
        yield return new ScreenshotStep("baby on ground before native command", Array.Empty<string>(), 0);
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(adultId, babyId);
        yield return new CheckpointStep("available native baby options", _ => new Dictionary<string, string>
            { ["options"] = string.Join("\n", options.Select(o => $"disabled={o.Disabled}: {o.Label}")) });
        var option = options.Single(o => !o.Disabled && o.Label.StartsWith("Put ", StringComparison.Ordinal) &&
            o.Label.EndsWith(" somewhere safe", StringComparison.Ordinal));
        yield return new FloatMenuActionStep("native put baby somewhere safe", adultId, babyId, option.StableId);
        yield return new ScreenshotStep("native childcare job ordered", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("let adult perform childcare", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("adult carries baby", _ => adult.carryTracker.CarriedThing == baby,
            new EndToEndDeadline(3600, 3600, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep("pause while baby is carried", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("adult carrying baby to crib", Array.Empty<string>(), 0);
        // Re-establish immediately before the failing TuckIntoBed/drop path. Setup never places the baby in bed.
        yield return new AssertionStep("retain missing flag and pending refresh before native drop", _ => ArrangePendingShortList());
        yield return Snapshot("short-list during native carry");
        yield return new TimeControlActionStep("finish native tuck into bed", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("baby rests in the crib", _ => baby.Spawned && baby.CurrentBed() == crib &&
            baby.CurJobDef == JobDefOf.LayDown && adult.carryTracker.CarriedThing == null,
            new EndToEndDeadline(6000, 6000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause completed childcare", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("inspect resting baby", new[] { babyId }, false);
        yield return new AssertionStep("discovery prefix survived native job", _ =>
        {
            EndToEndAssert.Equal(Find.World.features.features.Count, Flags.Count, "Missing discovery flag repaired.");
            EndToEndAssert.True(Flags.Take(Flags.Count - 1).All(f => f), "Existing discoveries preserved.");
            savedFlags = Flags.ToArray();
        });
        yield return Snapshot("baby safely resting after native drop");
        yield return new ScreenshotStep("baby resting in crib after native drop", Array.Empty<string>(), 0);
        yield return new SaveLoadActionStep("native save and load discovery flags", SaveName);
        yield return new AssertionStep("resolve baby and discoveries after native load", _ =>
        {
            baby = (Pawn)Find.CurrentMap.listerThings.AllThings.Single(t => t.ThingID == babyId);
            crib = (Building_Bed)Find.CurrentMap.listerThings.AllThings.Single(t => t.ThingID == cribId);
            EndToEndAssert.True(savedFlags.SequenceEqual(Flags), "Discovery flags persist exactly.");
            EndToEndAssert.True(baby.CurrentBed() == crib, "Baby remains in the crib after loading.");
        });
        yield return new SelectionActionStep("inspect loaded baby", new[] { babyId }, false);
        yield return new CameraActionStep("frame loaded crib", new[] { babyId, cribId }, 170);
        yield return new ScreenshotStep("baby still in crib after native load", Array.Empty<string>(), 0);
        yield return Snapshot("native load retained discoveries and crib");
    }

    private CheckpointStep Snapshot(string name) => new(name, _ => new Dictionary<string, string>
    {
        ["startup"] = PersonalBugfixesMod.StartupResults.Single().ToString(),
        ["featureCount"] = Find.World.features.features.Count.ToString(),
        ["flagCount"] = Flags.Count.ToString(),
        ["pendingRefresh"] = updateType.GetValue(null).ToString(),
        ["revealAll"] = revealAll.GetValue(null).ToString(),
        ["babyJob"] = baby.CurJobDef?.defName ?? "none",
        ["babySpawned"] = baby.Spawned.ToString(),
        ["bed"] = baby.CurrentBed()?.ThingID ?? "none"
    });

    private void ArrangePendingShortList()
    {
        learned.SetValue(Component, Enumerable.Repeat(true, Find.World.features.features.Count - 1).ToList());
        revealAll.SetValue(null, false);
        // Precondition only: the subsequent native registration owns invoking UpdateGraphics.
        updateType.SetValue(null, Enum.Parse(updateType.FieldType, "Full"));
    }
}

[RimWorldEndToEndTest("personal-bugfixes.absent-target", PersonalBugfixesMod.PackageId,
    "brrainz.harmony", "ludeon.rimworld", PersonalBugfixesMod.PackageId)]
public sealed class AbsentTargetTests : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context) { }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new AssertionStep("optional target absence is harmless", _ =>
            EndToEndAssert.Equal(FixState.Absent, PersonalBugfixesMod.StartupResults.Single().State, "Absent startup diagnostic."));
        yield return new CheckpointStep("retain absent startup diagnostic", _ => new Dictionary<string, string>
            { ["startup"] = PersonalBugfixesMod.StartupResults.Single().ToString() });
    }
}
