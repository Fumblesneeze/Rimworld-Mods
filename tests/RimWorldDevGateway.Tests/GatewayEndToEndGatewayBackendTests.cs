using NUnit.Framework;
using Mono.Cecil;
using RimWorldDevGateway.EndToEndTesting;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndGatewayBackendTests
{
    [Test]
    public void No_pawn_map_right_click_resolves_the_exact_RimWorld_selector_seam()
    {
        FieldInfo? field = typeof(VerseGatewayEndToEndFloatMenuActions).GetField(
            "SelectorLowPriorityInput",
            BindingFlags.Static | BindingFlags.NonPublic);
        var method = field?.GetValue(null) as MethodInfo;

        Assert.Multiple(() =>
        {
            Assert.That(field, Is.Not.Null);
            Assert.That(method, Is.Not.Null,
                "The production resolver must find the installed RimWorld 1.6 selector seam.");
            Assert.That(method?.DeclaringType, Is.EqualTo(typeof(RimWorld.Selector)));
            Assert.That(method?.Name, Is.EqualTo("SelectorOnGUI"));
            Assert.That(method?.IsPublic, Is.True,
                "The Gateway must bind RimWorld 1.6's exact public post-window selector seam.");
            Assert.That(method?.GetParameters(), Is.Empty);
        });
    }

    [Test]
    public void Map_menu_actions_require_native_outer_routing_preconditions()
    {
        MethodInfo? policy = typeof(VerseGatewayEndToEndFloatMenuActions).GetMethod(
            "HasPlayerControlledMap",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(policy, Is.Not.Null);
        bool fullyReady = (bool)policy!.Invoke(
            null,
            new object[] { true, true, true, true, false, true, false })!;
        bool noPlayerControl = (bool)policy.Invoke(
            null,
            new object[] { true, true, true, true, false, false, false })!;
        bool drawingWorld = (bool)policy.Invoke(
            null,
            new object[] { true, true, true, false, false, true, false })!;
        bool inputBlocked = (bool)policy.Invoke(
            null,
            new object[] { true, true, true, true, false, true, true })!;

        Assert.Multiple(() =>
        {
            Assert.That(fullyReady, Is.True);
            Assert.That(noPlayerControl, Is.False,
                "A playable map scene is not actionable until Current.Game.PlayerHasControl is true.");
            Assert.That(drawingWorld, Is.False,
                "The in-process map action must not bypass Core's DrawingMap outer route.");
            Assert.That(inputBlocked, Is.False,
                "The in-process map action must not bypass an input-absorbing native window.");
        });
    }

    [Test]
    public void Map_menu_actions_wire_the_native_outer_routing_policy()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(
            typeof(GatewayEndToEndGatewayBackend).Assembly.Location);
        TypeDefinition actions = assembly.MainModule.GetType(
            "RimWorldDevGateway.VerseGatewayEndToEndFloatMenuActions");
        MethodDefinition mapMenu = FindApply(
            actions,
            "RimWorldDevGateway.EndToEndTesting.MapFloatMenuOpenActionStep");
        MethodDefinition noPawn = FindApply(
            actions,
            "RimWorldDevGateway.EndToEndTesting.NoPawnMapRightClickActionStep");

        foreach (MethodDefinition action in new[] { mapMenu, noPawn })
        {
            Assert.Multiple(() =>
            {
                AssertCalls(action, "RimWorld.Planet.WorldRendererUtility", "get_DrawingMap");
                AssertCalls(action, "Verse.WindowStack", "get_AnyWindowAbsorbingAllInput");
                AssertCalls(action, actions.FullName, "HasPlayerControlledMap");
            });
        }

        Assert.Multiple(() =>
        {
            AssertDoesNotCall(mapMenu, "Verse.UI", "GUIToScreenPoint");
            AssertDoesNotCall(mapMenu, "Verse.WindowStack", "GetWindowAt");
            AssertCalls(noPawn, "Verse.UI", "GUIToScreenPoint");
            AssertCalls(noPawn, "Verse.WindowStack", "GetWindowAt");
        });
    }

    [Test]
    public void Map_menu_rejects_runtime_id_aliases_that_resolve_to_the_same_pawn()
    {
        MethodInfo? duplicatePolicy = typeof(VerseGatewayEndToEndFloatMenuActions).GetMethod(
            "IsDuplicateResolvedActor",
            BindingFlags.Static | BindingFlags.NonPublic);
        var first = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        var second = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));

        Assert.That(duplicatePolicy, Is.Not.Null);
        bool samePawn = (bool)duplicatePolicy!.Invoke(
            null,
            new object[] { new[] { first }, first })!;
        bool distinctPawn = (bool)duplicatePolicy.Invoke(
            null,
            new object[] { new[] { first }, second })!;

        Assert.Multiple(() =>
        {
            Assert.That(samePawn, Is.True);
            Assert.That(distinctPawn, Is.False);
        });
    }

    private static MethodDefinition FindApply(TypeDefinition type, string parameterType) =>
        type.Methods.Single(method =>
            method.Name == "Apply" &&
            method.Parameters.Count == 1 &&
            method.Parameters[0].ParameterType.FullName == parameterType);

    private static void AssertCalls(MethodDefinition method, string declaringType, string methodName) =>
        Assert.That(method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == declaringType &&
                reference.Name == methodName),
            Is.True,
            $"{method.FullName} must call {declaringType}.{methodName}.");

    private static void AssertDoesNotCall(MethodDefinition method, string declaringType, string methodName) =>
        Assert.That(method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == declaringType &&
                reference.Name == methodName),
            Is.False,
            $"{method.FullName} must not call {declaringType}.{methodName}.");

    [Test]
    public void Concrete_backend_maps_time_selection_camera_and_architect_queries_to_gateway_services()
    {
        var game = new RecordingGameOperations();
        var thingOperations = new RecordingThingOperations();
        var cameraOperations = new RecordingCameraOperations();
        var gizmoSource = new RecordingGizmoSource();
        var backend = CreateBackend(game, thingOperations, cameraOperations, gizmoSource);

        backend.SetTime(paused: true, EndToEndGameSpeed.Fast);
        backend.SetSelection(new[] { "thing_1" }, additive: false);
        backend.SetCamera("map-7", new GatewayMapCell(20, 30), 18f);
        var target = backend.ResolveTarget("thing_1");
        backend.QueryGizmos(Array.Empty<string>(), new[] { "Zone" });

        Assert.Multiple(() =>
        {
            Assert.That(game.Speed, Is.EqualTo(GatewayGameSpeed.Paused));
            Assert.That(thingOperations.SelectionOperation, Is.EqualTo(GatewaySelectionOperation.Replace));
            Assert.That(thingOperations.SelectionHandles, Is.EqualTo(new[] { "thing_1" }));
            Assert.That(cameraOperations.Center!.X, Is.EqualTo(20));
            Assert.That(cameraOperations.Center.Z, Is.EqualTo(30));
            Assert.That(cameraOperations.RootSize, Is.EqualTo(18f));
            Assert.That(target.MapHandle, Is.EqualTo("map-7"));
            Assert.That(target.OccupiedRect.MaxX, Is.EqualTo(12));
            Assert.That(gizmoSource.LastQuery!.OwnerHandles, Is.Empty);
            Assert.That(gizmoSource.LastQuery.ArchitectCategoryDefNames, Is.EqualTo(new[] { "Zone" }));
        });
    }

    [Test]
    public void Concrete_backend_rejects_artifactless_construction()
    {
        var error = Assert.Throws<ArgumentException>(() => CreateBackend(
            new RecordingGameOperations(),
            new RecordingThingOperations(),
            new RecordingCameraOperations(),
            new RecordingGizmoSource(),
            artifactDirectory: " "));

        Assert.That(error!.ParamName, Is.EqualTo("artifactDirectory"));
    }

    [Test]
    public void Screenshot_operation_persists_png_before_reporting_the_relative_artifact()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "gateway-e2e-backend-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        try
        {
            var backend = CreateBackend(
                new RecordingGameOperations(),
                new RecordingThingOperations(),
                new RecordingCameraOperations(),
                new RecordingGizmoSource(),
                directory,
                dispatcher,
                new FixedScreenshotBackend(png));

            var operation = backend.BeginScreenshot(
                new ScreenshotStep("evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            dispatcher.Drain(DispatchPhase.EndOfFrame);
            Assert.That(SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);
            var outcome = operation.GetOutcome();
            Assert.That(
                outcome.Passed,
                Is.True,
                $"{outcome.FailureCode}: {outcome.FailureMessage}");
            var artifact = outcome.Artifacts["screenshot"];

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Passed, Is.True);
                Assert.That(Path.IsPathRooted(artifact), Is.False);
                Assert.That(File.ReadAllBytes(Path.Combine(directory, artifact)), Is.EqualTo(png));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void Screenshot_operation_retries_one_transient_capture_on_a_fresh_end_of_frame()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "gateway-e2e-backend-retry-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var screenshots = new TransientScreenshotBackend(png);
        try
        {
            var backend = CreateBackend(
                new RecordingGameOperations(),
                new RecordingThingOperations(),
                new RecordingCameraOperations(),
                new RecordingGizmoSource(),
                directory,
                dispatcher,
                screenshots);

            var operation = backend.BeginScreenshot(
                new ScreenshotStep("retry evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            Assert.That(
                SpinWait.SpinUntil(
                    () =>
                    {
                        dispatcher.Drain(DispatchPhase.EndOfFrame);
                        return operation.IsCompleted;
                    },
                    TimeSpan.FromSeconds(5)),
                Is.True);
            var outcome = operation.GetOutcome();

            Assert.Multiple(() =>
            {
                Assert.That(
                    outcome.Passed,
                    Is.True,
                    $"{outcome.FailureCode}: {outcome.FailureMessage}");
                Assert.That(screenshots.CaptureCount, Is.EqualTo(2));
                Assert.That(
                    File.ReadAllBytes(Path.Combine(directory, outcome.Artifacts["screenshot"])),
                    Is.EqualTo(png));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void Float_menu_catalog_projects_native_options_to_shared_stable_metadata()
    {
        var consume = CreateHostSafeFloatMenuOption("Consume simple meal", () => { });
        var forbidden = CreateHostSafeFloatMenuOption("Cannot consume: forbidden", null);

        var projected = VerseGatewayEndToEndFloatMenuActions.ProjectOptions(new[]
        {
            consume,
            forbidden
        });

        Assert.Multiple(() =>
        {
            Assert.That(projected.Select(option => option.Label), Is.EqualTo(new[]
            {
                "Consume simple meal",
                "Cannot consume: forbidden"
            }));
            Assert.That(projected.Select(option => option.Disabled), Is.EqualTo(new[] { false, true }));
            Assert.That(projected[0].StableId, Is.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(consume)));
            Assert.That(projected[1].StableId, Is.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(forbidden)));
        });
    }

    [Test]
    public void Float_menu_stable_identity_distinguishes_callbacks_behind_the_same_visible_label()
    {
        var first = CreateHostSafeFloatMenuOption("Consume meal", FirstFloatMenuAction);
        var second = CreateHostSafeFloatMenuOption("Consume meal", SecondFloatMenuAction);

        Assert.That(
            VerseGatewayEndToEndFloatMenuActions.StableIdentity(first),
            Is.Not.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(second)));
    }

    [Test]
    public void Current_float_menu_requires_one_exact_enabled_visible_label()
    {
        var chosen = CreateHostSafeFloatMenuOption("For guests", () => { });
        var duplicate = CreateHostSafeFloatMenuOption("For guests", () => { });
        var other = CreateHostSafeFloatMenuOption("For colonists", () => { });

        GatewayEndToEndStepOutcome unique = VerseGatewayEndToEndFloatMenuActions.ResolveExactEnabledOption(
            new[] { other, chosen },
            "For guests",
            out FloatMenuOption? resolved);
        GatewayEndToEndStepOutcome ambiguous = VerseGatewayEndToEndFloatMenuActions.ResolveExactEnabledOption(
            new[] { chosen, duplicate },
            "For guests",
            out _);
        GatewayEndToEndStepOutcome missing = VerseGatewayEndToEndFloatMenuActions.ResolveExactEnabledOption(
            new[] { other },
            "For guests",
            out _);

        Assert.Multiple(() =>
        {
            Assert.That(unique.Passed, Is.True);
            Assert.That(resolved, Is.SameAs(chosen));
            Assert.That(ambiguous.Passed, Is.False);
            Assert.That(ambiguous.FailureCode, Is.EqualTo("current_float_menu_option_ambiguous"));
            Assert.That(missing.Passed, Is.False);
            Assert.That(missing.FailureCode, Is.EqualTo("current_float_menu_option_not_found"));
        });
    }

    [Test]
    public void Current_float_menu_uses_the_native_tutor_callback_notify_then_close_order()
    {
        var events = new List<string>();
        FloatMenuOption option = CreateHostSafeFloatMenuOption(
            "For guests",
            () => events.Add("chosen"));
        SetTutorTag(option, "ChooseGuestBed");
        var menu = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));

        bool accepted = VerseGatewayEndToEndFloatMenuActions.RunChosenLifecycle(
            option,
            menu,
            () => events.Add("closed"),
            tag =>
            {
                events.Add("allow:" + tag);
                return true;
            },
            tag => events.Add("notify:" + tag));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(events, Is.EqualTo(new[]
            {
                "allow:ChooseGuestBed",
                "chosen",
                "notify:ChooseGuestBed",
                "closed"
            }));
        });
    }

    [Test]
    public void Current_float_menu_tutor_rejection_does_not_choose_notify_or_close()
    {
        var events = new List<string>();
        FloatMenuOption option = CreateHostSafeFloatMenuOption(
            "For guests",
            () => events.Add("chosen"));
        SetTutorTag(option, "ChooseGuestBed");
        var menu = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));

        bool accepted = VerseGatewayEndToEndFloatMenuActions.RunChosenLifecycle(
            option,
            menu,
            () => events.Add("closed"),
            tag =>
            {
                events.Add("allow:" + tag);
                return false;
            },
            tag => events.Add("notify:" + tag));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            Assert.That(events, Is.EqualTo(new[] { "allow:ChooseGuestBed" }));
        });
    }

    [Test]
    public void Current_float_menu_requires_the_exact_captured_window()
    {
        var captured = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var replacement = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));

        GatewayEndToEndStepOutcome exact =
            VerseGatewayEndToEndFloatMenuActions.ResolveExactCapturedMenu(
                new[] { captured },
                captured,
                out FloatMenu? resolved);
        GatewayEndToEndStepOutcome stale =
            VerseGatewayEndToEndFloatMenuActions.ResolveExactCapturedMenu(
                new[] { replacement },
                captured,
                out _);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Passed, Is.True);
            Assert.That(resolved, Is.SameAs(captured));
            Assert.That(stale.Passed, Is.False);
            Assert.That(stale.FailureCode, Is.EqualTo("current_float_menu_stale"));
        });
    }

    [Test]
    public void Current_float_menu_can_explicitly_adopt_one_process_opened_window()
    {
        var physical = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var extra = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        FloatMenuAutomationLease.Clear();

        GatewayEndToEndStepOutcome exact =
            VerseGatewayEndToEndFloatMenuActions.CaptureSoleUnownedMenu(
                new[] { physical },
                out FloatMenu? captured);
        FloatMenuAutomationLease.Clear();
        GatewayEndToEndStepOutcome ambiguous =
            VerseGatewayEndToEndFloatMenuActions.CaptureSoleUnownedMenu(
                new[] { physical, extra },
                out _);
        FloatMenuAutomationLease.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(exact.Passed, Is.True);
            Assert.That(captured, Is.SameAs(physical));
            Assert.That(ambiguous.Passed, Is.False);
            Assert.That(ambiguous.FailureCode, Is.EqualTo("current_float_menu_capture_ambiguous"));
        });
    }

    [Test]
    public void Closed_float_menu_releases_its_automation_lease_before_the_next_native_menu()
    {
        var closed = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        FloatMenuAutomationLease.CaptureNewlyOpened(Array.Empty<FloatMenu>(), new[] { closed });

        FloatMenuAutomationLease.ReleaseIfNotOpen(Array.Empty<FloatMenu>());

        Assert.That(FloatMenuAutomationLease.CapturedForAutomation, Is.Null);
    }

    [Test]
    public void Sole_menu_adoption_releases_a_stale_closed_lease_before_capture()
    {
        var stale = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var newlyOpen = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        FloatMenuAutomationLease.Clear();
        FloatMenuAutomationLease.CaptureNewlyOpened(Array.Empty<FloatMenu>(), new[] { stale });

        GatewayEndToEndStepOutcome outcome =
            VerseGatewayEndToEndFloatMenuActions.CaptureSoleUnownedMenu(
                new[] { newlyOpen },
                out FloatMenu? captured);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True, outcome.FailureMessage);
            Assert.That(captured, Is.SameAs(newlyOpen));
            Assert.That(FloatMenuAutomationLease.CapturedForAutomation, Is.SameAs(newlyOpen));
        });
        FloatMenuAutomationLease.Clear();
    }

    [Test]
    public void Replacement_submenu_requires_one_new_menu_and_a_closed_source()
    {
        var source = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var replacement = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var extra = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));

        GatewayEndToEndStepOutcome exact =
            VerseGatewayEndToEndFloatMenuActions.ResolveReplacementMenu(
                source,
                sourceIsOpen: false,
                new[] { replacement },
                out FloatMenu? resolved);
        GatewayEndToEndStepOutcome absent =
            VerseGatewayEndToEndFloatMenuActions.ResolveReplacementMenu(
                source,
                sourceIsOpen: false,
                Array.Empty<FloatMenu>(),
                out _);
        GatewayEndToEndStepOutcome ambiguous =
            VerseGatewayEndToEndFloatMenuActions.ResolveReplacementMenu(
                source,
                sourceIsOpen: false,
                new[] { replacement, extra },
                out _);
        GatewayEndToEndStepOutcome sourceStillOpen =
            VerseGatewayEndToEndFloatMenuActions.ResolveReplacementMenu(
                source,
                sourceIsOpen: true,
                new[] { source, replacement },
                out _);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Passed, Is.True);
            Assert.That(resolved, Is.SameAs(replacement));
            Assert.That(absent.FailureCode, Is.EqualTo("replacement_float_menu_missing"));
            Assert.That(ambiguous.FailureCode, Is.EqualTo("replacement_float_menu_ambiguous"));
            Assert.That(sourceStillOpen.FailureCode, Is.EqualTo("replacement_float_menu_source_open"));
        });
    }

    [Test]
    public void Current_float_menu_preserves_both_native_colonist_ordering_modes()
    {
        var colonistOrder = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var ordinaryMenu = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo ordering = typeof(FloatMenu).GetField("givesColonistOrders", fields)!;
        ordering.SetValue(colonistOrder, true);
        ordering.SetValue(ordinaryMenu, false);

        Assert.Multiple(() =>
        {
            Assert.That(
                VerseGatewayEndToEndFloatMenuActions.GetColonistOrdering(colonistOrder),
                Is.True);
            Assert.That(
                VerseGatewayEndToEndFloatMenuActions.GetColonistOrdering(ordinaryMenu),
                Is.False);
        });
    }

    private static FloatMenuOption CreateHostSafeFloatMenuOption(string label, Action? action)
    {
        var option = (FloatMenuOption)FormatterServices.GetUninitializedObject(typeof(FloatMenuOption));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(FloatMenuOption).GetField("labelInt", fields)!
            .SetValue(option, label);
        typeof(FloatMenuOption).GetField("action", fields)!
            .SetValue(option, action);
        return option;
    }

    private static void SetTutorTag(FloatMenuOption option, string tutorTag)
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(FloatMenuOption).GetField("tutorTag", fields)!
            .SetValue(option, tutorTag);
    }

    private static void FirstFloatMenuAction()
    {
    }

    private static void SecondFloatMenuAction()
    {
    }

    private static GatewayEndToEndGatewayBackend CreateBackend(
        RecordingGameOperations game,
        RecordingThingOperations things,
        RecordingCameraOperations camera,
        RecordingGizmoSource gizmos,
        string artifactDirectory = "e2e-artifacts",
        GatewayDispatcher? dispatcher = null,
        IGatewayScreenshotBackend? screenshotBackend = null) =>
        new(
            new GatewayGameControlController(game),
            new GatewayThingController(things),
            new GatewayCameraController(camera),
            new GatewayGizmoRegistry(gizmos),
            new PassingFloatMenus(),
            new PassingSettlementTrade(),
            new PassingTradeDialogs(),
            new GatewayWindowsInput(),
            new GatewayScreenshotService(
                dispatcher ?? new GatewayDispatcher(),
                screenshotBackend ?? new UnusedScreenshotBackend(),
                1024),
            artifactDirectory);

    private sealed class RecordingGameOperations : IGatewayGameControlOperations
    {
        public bool DevMode { get; private set; } = true;
        public bool GodMode { get; private set; }
        public bool EffectiveGodMode => DevMode && GodMode;
        public bool DevModePermanentlyDisabled => false;
        public bool GameAvailable => true;
        public bool Paused => Speed == GatewayGameSpeed.Paused;
        public bool ForcePaused => false;
        public GatewayGameSpeed Speed { get; private set; } = GatewayGameSpeed.Normal;
        public void SetDevMode(bool enabled) => DevMode = enabled;
        public void SetGodMode(bool enabled) => GodMode = enabled;
        public void SetPaused(bool paused) => Speed = paused ? GatewayGameSpeed.Paused : GatewayGameSpeed.Normal;
        public void SetSpeed(GatewayGameSpeed speed) => Speed = speed;
    }

    private sealed class TransientScreenshotBackend : IGatewayScreenshotBackend
    {
        private readonly byte[] png;

        public TransientScreenshotBackend(byte[] png)
        {
            this.png = png;
        }

        public int CaptureCount { get; private set; }

        public object Capture()
        {
            CaptureCount++;
            if (CaptureCount == 1)
            {
                throw new InvalidOperationException("Transient Unity capture failure.");
            }

            return new object();
        }

        public byte[] EncodePng(object resource) => png;

        public void Destroy(object resource)
        {
        }
    }

    private sealed class RecordingThingOperations : IGatewayThingOperations
    {
        private IReadOnlyList<GatewayThingSummary> selection = Array.Empty<GatewayThingSummary>();

        public GatewaySelectionOperation? SelectionOperation { get; private set; }
        public IReadOnlyList<string>? SelectionHandles { get; private set; }

        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect) =>
            new("map-7", null, new[] { Summary("thing_1") });

        public GatewayThingInspection CaptureInspection(string handle) =>
            new(
                Summary(handle),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                null,
                null,
                null,
                Array.Empty<GatewayInspectionWarning>());

        public IReadOnlyList<GatewayThingSummary> CaptureSelection() => selection;

        public void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles)
        {
            SelectionOperation = operation;
            SelectionHandles = handles.ToArray();
            selection = handles.Select(Summary).ToArray();
        }

        private static GatewayThingSummary Summary(string handle) =>
            new(
                handle,
                "thing",
                "Thing",
                "Thing",
                "item",
                "map-7",
                new GatewayMapCell(10, 11),
                new GatewayMapRect(10, 11, 12, 13),
                "North",
                1,
                null,
                null,
                null,
                false,
                false,
                false,
                null);
    }

    private sealed class RecordingCameraOperations : IGatewayCameraOperations
    {
        public GatewayMapCell? Center { get; private set; }
        public float? RootSize { get; private set; }

        public GatewayCameraSnapshot Capture() => new(
            "map-7",
            Center ?? new GatewayMapCell(5, 5),
            RootSize ?? 12f,
            "Middle",
            new GatewayMapRect(0, 0, 50, 50),
            100,
            100,
            8f,
            60f);

        public void Set(GatewayMapCell? center, float? rootSize)
        {
            Center = center;
            RootSize = rootSize;
        }
    }

    private sealed class RecordingGizmoSource : IGatewayGizmoSource
    {
        public GatewayGizmoSourceQuery? LastQuery { get; private set; }

        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query)
        {
            LastQuery = query;
            return new GatewayGizmoDiscovery(
                "map-7",
                query.OwnerHandles,
                Array.Empty<IGatewayGizmoCandidate>(),
                false);
        }
    }

    private sealed class PassingFloatMenus : IGatewayEndToEndFloatMenuActions
    {
        public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class PassingTradeDialogs : IGatewayEndToEndTradeDialogActions
    {
        public GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class PassingSettlementTrade : IGatewayEndToEndSettlementTradeActions
    {
        public GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class UnusedScreenshotBackend : IGatewayScreenshotBackend
    {
        public object Capture() => new object();
        public byte[] EncodePng(object resource) => Array.Empty<byte>();
        public void Destroy(object resource) { }
    }

    private sealed class FixedScreenshotBackend : IGatewayScreenshotBackend
    {
        private readonly byte[] png;

        public FixedScreenshotBackend(byte[] png) => this.png = png;

        public object Capture() => new object();
        public byte[] EncodePng(object resource) => png;
        public void Destroy(object resource) { }
    }
}
