using System.Reflection;
using ImmersiveSignalFire.Signals;
using Mono.Cecil;
using NUnit.Framework;

namespace ImmersiveSignalFire.Harmony.Tests;

[TestFixture]
public sealed class RuntimeShapeTests
{
    [Test]
    public void ForgeRunnerAndInclusiveRangeShapesMatchTheBoundedPlumeContract()
    {
        using AssemblyDefinition forge = AssemblyDefinition.ReadAssembly(Metadata("DynamicEffectsForgeAssemblyPath"));
        TypeDefinition runner = forge.MainModule.GetType("Blues.EffectRunner");
        MethodDefinition createAt = runner.Methods.Single(method =>
            method.Name == "CreateAt" && method.IsStatic && method.Parameters.Count == 4);
        MethodDefinition beat = runner.Methods.Single(method => method.Name == "Beat");
        MethodDefinition canFleck = forge.MainModule.GetType("Blues.FleckInstruction").Methods
            .Single(method => method.Name == "CanFleck" && method.Parameters.Count == 1);
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(SignalQuality).Assembly.Location);
        MethodDefinition tick = product.MainModule
            .GetType("ImmersiveSignalFire.Effects.SignalEffectController")
            .Methods.Single(method => method.Name == "Tick");
        MethodDefinition stopSmokeRunner = product.MainModule
            .GetType("ImmersiveSignalFire.Effects.SignalEffectController")
            .Methods.Single(method => method.Name == "StopSmokeRunner");

        Assert.Multiple(() =>
        {
            Assert.That(createAt.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Blues.ActionManager" &&
                reference.Name == "AddEffect"), Is.True,
                "Forge CreateAt must hand unattached runners to its map action manager for automatic ticking.");
            Assert.That(beat.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Blues.EffectRunner" &&
                reference.Name == "Kill"), Is.True,
                "An unattached Forge runner must retain its duration-completion kill path.");
            Assert.That(canFleck.Body.Instructions.Any(instruction => instruction.OpCode.Name.StartsWith("blt")), Is.True);
            Assert.That(canFleck.Body.Instructions.Any(instruction => instruction.OpCode.Name == "cgt"), Is.True,
                "Forge must keep both ActiveTicks endpoints inclusive, making restored 0~20 exactly 21 emission ticks.");
            Assert.That(tick.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Effects.SmokeRunnerLifecycle" &&
                reference.Name == "Advance"), Is.True,
                "The live controller must use the deterministically tested ordered smoke lifecycle.");
            Assert.That(stopSmokeRunner.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Blues.EffectRunner" &&
                reference.Name == "Kill"), Is.True,
                "The ordered stop callback must kill the attached Forge runner before it can loop.");
        });
    }

    [Test]
    public void RimWorld16ExposesTheNarrowInteractionAndAidSeams()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));

        Assert.Multiple(() =>
        {
            AssertMethod(game, "RimWorld.FloatMenuMakerMap", "GetOptions", 3);
            AssertMethod(game, "RimWorld.Selector", "HandleMapClicks", 0);
            AssertMethod(game, "RimWorld.Selector", "SelectorOnGUI", 0);
            AssertMethod(game, "RimWorld.UIRoot_Play", "UIRootOnGUI", 0);
            AssertMethod(game, "RimWorld.FactionDialogMaker", "CallForAid", 2);
            AssertMethod(game, "RimWorld.FilthMaker", "TryMakeFilth", 6);
            Assert.That(game.MainModule.GetType("RimWorld.Dialog_BeginLordJob"), Is.Not.Null);
            Assert.That(game.MainModule.GetType("RimWorld.IPawnRoleSelectionWidget"), Is.Not.Null);
        });
    }

    [Test]
    public void ProductOwnsOneNoPawnPatchAndSharedDialogSubclass()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(SignalQuality).Assembly.Location);
        TypeDefinition? mod = product.MainModule.GetType("ImmersiveSignalFire.SignalFireMod");
        MethodDefinition? modConstructor = mod?.Methods.SingleOrDefault(method =>
            method.IsConstructor &&
            method.IsPublic &&
            method.Parameters.Count == 1 &&
            method.Parameters[0].ParameterType.FullName == "Verse.ModContentPack");
        MethodDefinition? deferredInitializer = mod?.Methods.SingleOrDefault(method =>
            method.Name == "InitializeAfterContentLoad" && method.IsStatic && method.IsPrivate);
        TypeDefinition? building = product.MainModule.GetType("ImmersiveSignalFire.Buildings.Building_SignalFire");
        MethodDefinition? spawnSetup = building?.Methods.SingleOrDefault(method => method.Name == "SpawnSetup");
        TypeDefinition? patch = product.MainModule.GetType("ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch");
        MethodDefinition? patchInstaller = patch?.Methods.SingleOrDefault(method => method.Name == "Install");
        MethodDefinition? ensurePatchInstalled = patch?.Methods.SingleOrDefault(method => method.Name == "EnsureInstalled");
        MethodDefinition? prefix = patch?.Methods.SingleOrDefault(method => method.Name == "Prefix");
        MethodDefinition? uiRootPostfix = patch?.Methods.SingleOrDefault(method => method.Name == "UIRootPostfix");
        TypeDefinition? menu = product.MainModule.GetType("ImmersiveSignalFire.Interactions.SignalFireMenu");
        MethodDefinition? openNoPawnMenu = menu?.Methods.SingleOrDefault(method =>
            method.Name == "OpenNoPawnTopLevel");
        TypeDefinition? responses = product.MainModule.GetType("ImmersiveSignalFire.Signals.MapComponent_SignalResponses");
        MethodDefinition? queueMenu = responses?.Methods.SingleOrDefault(method => method.Name == "QueueNoPawnMenu");
        MethodDefinition? flushMenu = responses?.Methods.SingleOrDefault(method => method.Name == "FlushNoPawnMenu");
        TypeDefinition? dialog = product.MainModule.GetType("ImmersiveSignalFire.UI.Dialog_BeginSignalFire");
        CustomAttribute? patchTarget = patch?.CustomAttributes.SingleOrDefault(attribute =>
            attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch" &&
            attribute.ConstructorArguments.Count == 2);

        Assert.Multiple(() =>
        {
            Assert.That(mod, Is.Not.Null);
            Assert.That(mod!.IsPublic, Is.True,
                "RimWorld must discover the bootstrap through its normal public Mod entry point.");
            Assert.That(mod.BaseType.FullName, Is.EqualTo("Verse.Mod"));
            Assert.That(modConstructor, Is.Not.Null,
                "The discoverable Mod entry point must expose RimWorld's exact public ModContentPack constructor.");
            Assert.That(modConstructor!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "System.Threading.Interlocked" &&
                reference.Name == "Exchange"), Is.True,
                "The bootstrap must remain idempotent when RimWorld discovers it more than once.");
            Assert.That(modConstructor.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.LongEventHandler" &&
                reference.Name == "ExecuteWhenFinished"), Is.True,
                "Unity material initialization must be deferred to RimWorld's finished-loading main-thread phase.");
            Assert.That(modConstructor.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch" &&
                reference.Name == "EnsureInstalled"), Is.True,
                "The exact patch must install before Core can JIT/in-line the private input seam; SpawnSetup remains the fallback.");
            Assert.That(modConstructor.Body.Instructions.Any(instruction =>
                instruction.OpCode.Name == "ldftn" &&
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.SignalFireMod" &&
                reference.Name == "InitializeAfterContentLoad"), Is.True,
                "The scheduled finished-loading callback must be the reviewed product initializer.");
            Assert.That(modConstructor.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Effects.OptionalToolsAdapter" &&
                reference.Name == "Initialize"), Is.False,
                "Unity material setup must never run in the off-thread Mod constructor.");
            Assert.That(deferredInitializer, Is.Not.Null);
            Assert.That(deferredInitializer!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch" &&
                reference.Name == "Install"), Is.False,
                "Loading-phase patch installation is lost in the live process and must not be the gameplay anchor.");
            Assert.That(deferredInitializer.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Effects.OptionalToolsAdapter" &&
                reference.Name == "Initialize"), Is.True,
                "Optional Unity material setup must run only from the deferred main-thread initializer.");
            Assert.That(patch, Is.Not.Null);
            Assert.That(spawnSetup, Is.Not.Null,
                "A spawned signal fire must own the final game-thread installation anchor.");
            Assert.That(spawnSetup!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch" &&
                reference.Name == "EnsureInstalled"), Is.True,
                "Newly built and loaded fires must ensure the no-pawn route exists before player input.");
            Assert.That(ensurePatchInstalled, Is.Not.Null);
            Assert.That(ensurePatchInstalled!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch" &&
                reference.Name == "Install"), Is.True,
                "The spawn-time anchor must call the reviewed explicit installer.");
            Assert.That(ensurePatchInstalled.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch" &&
                reference.Name == "get_IsInstalled"), Is.True,
                "The install guard must re-check both live Harmony patches instead of trusting only stored state.");
            Assert.That(patchInstaller, Is.Not.Null);
            Assert.That(patchInstaller!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "HarmonyLib.Harmony" &&
                reference.Name == "Patch"), Is.True,
                "The no-pawn route must use an explicit owner-scoped Harmony patch instead of relying on failed PatchAll discovery.");
            Assert.That(patchInstaller.Body.Instructions.Count(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "HarmonyLib.Harmony" &&
                reference.Name == "Patch"), Is.GreaterThanOrEqualTo(2),
                "The installer must own both click capture and post-window-stack menu delivery.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "HarmonyLib.Harmony" &&
                reference.Name == "GetPatchInfo"), Is.True,
                "The explicit installer must verify that Harmony retained the exact live patch before reporting success.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is TypeReference reference &&
                reference.FullName == "RimWorld.Selector"), Is.True,
                "The explicit installer must resolve RimWorld.Selector itself.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is string value && value == "SelectorOnGUI"), Is.True,
                "The explicit installer must resolve the post-window low-priority selector method.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is string value && value == "SelectorOnGUI_BeforeMainTabs"), Is.False,
                "The no-pawn patch must not intercept map input before RimWorld windows consume it.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is TypeReference reference &&
                reference.FullName == "RimWorld.UIRoot_Play"), Is.True,
                "The explicit installer must resolve RimWorld.UIRoot_Play itself.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is string value && value == "UIRootOnGUI"), Is.True,
                "The explicit installer must resolve the exact post-input delivery seam.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is TypeReference reference &&
                reference.FullName == "ImmersiveSignalFire.Interactions.NoPawnSignalFireMenuPatch"), Is.True,
                "The explicit installer must resolve this patch class itself.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is string value && value == "Prefix"), Is.True,
                "The explicit installer must resolve the exact prefix method.");
            Assert.That(patchInstaller.Body.Instructions.Any(instruction =>
                instruction.Operand is string value && value == "UIRootPostfix"), Is.True,
                "The explicit installer must resolve the exact play-root postfix method.");
            Assert.That(patchTarget, Is.Not.Null);
            Assert.That(((TypeReference)patchTarget!.ConstructorArguments[0].Value).FullName,
                Is.EqualTo("RimWorld.Selector"));
            Assert.That(patchTarget.ConstructorArguments[1].Value, Is.EqualTo("SelectorOnGUI"));
            Assert.That(patch!.Methods.Any(method => method.Name == "Prefix"), Is.True,
                "The no-pawn route must inspect a still-unconsumed right-click at Core's low-priority selector seam.");
            Assert.That(prefix!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Signals.MapComponent_SignalResponses" &&
                reference.Name == "QueueNoPawnMenu"), Is.True,
                "The post-window selector seam must defer the new window until after the creating input event.");
            Assert.That(prefix.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "UnityEngine.Event" &&
                reference.Name == "get_mousePosition"), Is.True,
                "The exact map target must come from the current native GUI event so minimized in-process input remains faithful.");
            Assert.That(prefix.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.UI" &&
                reference.Name == "UIToMapPosition"), Is.True,
                "The current GUI event position must be converted through RimWorld's native map projection.");
            Assert.That(prefix.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.UI" &&
                reference.Name == "MouseMapPosition"), Is.False,
                "The no-pawn route must not depend on the foreground desktop pointer.");
            Assert.That(prefix.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "RimWorld.Planet.WorldRendererUtility" &&
                reference.Name == "get_DrawingMap"), Is.True,
                "The no-pawn route must remain unavailable while the world view owns low-priority input.");
            Assert.That(prefix.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.WindowStack" &&
                reference.Name == "get_AnyWindowAbsorbingAllInput"), Is.True,
                "The no-pawn route must fail closed behind an input-absorbing native window.");
            Assert.That(queueMenu, Is.Not.Null);
            Assert.That(flushMenu, Is.Not.Null,
                "The map component must expose the one-shot queued-menu delivery operation.");
            Assert.That(responses!.Methods.Any(method => method.Name == "MapComponentUpdate"), Is.False,
                "Menu delivery must not depend on simulation/map updates while the game is paused.");
            Assert.That(uiRootPostfix, Is.Not.Null);
            Assert.That(uiRootPostfix!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "ImmersiveSignalFire.Signals.MapComponent_SignalResponses" &&
                reference.Name == "FlushNoPawnMenu"), Is.True,
                "The second hook must deliver the queued menu only after every Core map-input handler processed the creating event.");
            Assert.That(openNoPawnMenu, Is.Not.Null);
            Assert.That(openNoPawnMenu!.Body.Instructions.Any(instruction =>
                instruction.OpCode.Name == "newobj" &&
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.FloatMenu"), Is.True,
                "A zero-selection route must use the native base FloatMenu that remains open without a selected pawn.");
            Assert.That(openNoPawnMenu.Body.Instructions.Any(instruction =>
                instruction.OpCode.Name == "newobj" &&
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "Verse.FloatMenuMap"), Is.False,
                "Core's FloatMenuMap closes itself whenever no pawn is selected.");
            Assert.That(dialog, Is.Not.Null);
            Assert.That(dialog!.BaseType.FullName, Is.EqualTo("Verse.Window"));
            Assert.That(dialog.Methods.Any(method => method.Name == "OnAcceptKeyPressed"), Is.True);
            Assert.That(dialog.Fields.Any(field =>
                field.FieldType.FullName == "System.Reflection.FieldInfo"), Is.False,
                "The Core-safe dialog must not repair a DLC-guarded base constructor by reflection.");
            Assert.That(product.MainModule.GetType("ImmersiveSignalFire.Buildings.CompSignalFire"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ImmersiveSignalFire.Signals.MapComponent_SignalResponses"), Is.Not.Null);
        });
    }

    [Test]
    public void ProductDoesNotLinkOptionalToolsOrDeveloperGatewayAssemblies()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(SignalQuality).Assembly.Location);
        string[] references = product.MainModule.AssemblyReferences.Select(reference => reference.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(references, Does.Contain("BluesForge"));
            Assert.That(references, Does.Not.Contain("JobEffects"));
            Assert.That(references.Any(reference => reference.StartsWith("RimWorldDevGateway", StringComparison.Ordinal)), Is.False);
        });
    }

    private static void AssertMethod(AssemblyDefinition assembly, string typeName, string methodName, int parameters)
    {
        TypeDefinition? type = assembly.MainModule.GetType(typeName);
        Assert.That(type, Is.Not.Null, $"Missing type {typeName}.");
        Assert.That(type!.Methods.Any(method => method.Name == methodName && method.Parameters.Count == parameters),
            Is.True, $"Missing {typeName}.{methodName}/{parameters}.");
    }

    private static string Metadata(string key) =>
        typeof(RuntimeShapeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key)
            .Value;
}
