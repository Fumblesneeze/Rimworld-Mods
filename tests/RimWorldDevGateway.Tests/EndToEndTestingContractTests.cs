using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class EndToEndTestingContractTests
{
    [Test]
    public void Semantic_inspection_steps_require_exact_targets_without_screen_coordinates()
    {
        var tab = new PawnInspectTabActionStep("open gear", "Thing_Human123", EndToEndPawnInspectTab.Gear);
        var open = ThingInfoCardActionStep.Open("open plate", "Thing_Plate456");
        var close = ThingInfoCardActionStep.Close("close plate", "Thing_Plate456");
        var closeInspect = new InspectPaneCloseActionStep(
            "close contents",
            "Thing_Fridge789",
            "AdaptiveStorage.ContentsITab");
        var cancelWindow = new WindowCancelActionStep(
            "cancel dialog",
            "Verse.Dialog_MessageBox");
        var modSettings = new ModSettingsActionStep(
            "open settings",
            "fumblesneeze.immersivechefs");

        Assert.Multiple(() =>
        {
            Assert.That(tab.PawnRuntimeId, Is.EqualTo("Thing_Human123"));
            Assert.That(tab.Tab, Is.EqualTo(EndToEndPawnInspectTab.Gear));
            Assert.That(open.ThingRuntimeId, Is.EqualTo("Thing_Plate456"));
            Assert.That(open.IsOpenAction, Is.True);
            Assert.That(close.IsOpenAction, Is.False);
            Assert.That(closeInspect.SelectedThingRuntimeId, Is.EqualTo("Thing_Fridge789"));
            Assert.That(closeInspect.ExpectedTabRuntimeType, Is.EqualTo("AdaptiveStorage.ContentsITab"));
            Assert.That(cancelWindow.ExpectedWindowRuntimeType, Is.EqualTo("Verse.Dialog_MessageBox"));
            Assert.That(modSettings.PackageId, Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(tab, Is.Not.InstanceOf<ProcessInputActionStep>());
            Assert.That(open, Is.Not.InstanceOf<ProcessInputActionStep>());
            Assert.That(closeInspect, Is.Not.InstanceOf<ProcessInputActionStep>());
            Assert.That(cancelWindow, Is.Not.InstanceOf<ProcessInputActionStep>());
            Assert.That(modSettings, Is.Not.InstanceOf<ProcessInputActionStep>());
            Assert.That(
                () => new ModSettingsActionStep("open settings", " "),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Describe_accepts_a_concrete_product_test_with_an_exact_ordered_package_set()
    {
        var descriptor = EndToEndTestContract.Describe(typeof(ValidProductTest));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo("immersive-chefs.adverse-meal"));
            Assert.That(descriptor.OwnerPackageId, Is.EqualTo("fumblesneeze.immersivechefs"));
            Assert.That(descriptor.ActivePackageIds, Is.EqualTo(new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                "fumblesneeze.immersivechefs"
            }));
            Assert.That(descriptor.Deadline.MaxFrames, Is.EqualTo(900));
            Assert.That(descriptor.Deadline.MaxGameTicks, Is.EqualTo(2_500));
            Assert.That(descriptor.Deadline.MaxWallClock, Is.EqualTo(TimeSpan.FromSeconds(45)));
        });
    }

    [Test]
    public void Describe_accepts_the_implicit_gateway_owner_but_never_an_explicit_gateway_package()
    {
        var descriptor = EndToEndTestContract.Describe(typeof(ValidGatewaySelfTest));

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.OwnerPackageId, Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(descriptor.ActivePackageIds, Is.EqualTo(new[] { "ludeon.rimworld" }));
            Assert.That(descriptor.LaunchedPackageIds, Is.EqualTo(new[]
            {
                "ludeon.rimworld",
                EndToEndTestContract.GatewayPackageId
            }));
        });

        var error = Assert.Throws<EndToEndContractException>(
            () => EndToEndTestContract.Describe(typeof(ExplicitGatewayTest)));
        Assert.That(error!.Message, Does.Contain("must not list").And.Contain(EndToEndTestContract.GatewayPackageId));
    }

    [TestCase(typeof(MissingAttributeTest), "attribute")]
    [TestCase(typeof(AbstractAttributedTest), "concrete")]
    [TestCase(typeof(AttributedNonTest), "IRimWorldEndToEndTest")]
    [TestCase(typeof(MissingOwnerTest), "owner")]
    [TestCase(typeof(DuplicatePackageTest), "duplicate")]
    [TestCase(typeof(CoreBeforeHarmonyTest), "brrainz.harmony")]
    [TestCase(typeof(InvalidDeadlineTest), "deadline")]
    public void Describe_rejects_invalid_declarations(Type testType, string messageFragment)
    {
        var error = Assert.Throws<EndToEndContractException>(() => EndToEndTestContract.Describe(testType));

        Assert.That(error!.Message, Does.Contain(messageFragment).IgnoreCase);
    }

    [Test]
    public void Step_contract_separates_typed_actions_waits_and_observations()
    {
        var waitDeadline = new EndToEndDeadline(120, 600, TimeSpan.FromSeconds(10));
        EndToEndStep[] steps =
        {
            new GizmoActionStep("undraft", new[] { "pawn:42" }, "Command_Toggle", EndToEndGizmoInteraction.Invoke),
            new FloatMenuActionStep("eat-meal", "pawn:42", "thing:meal:7", "Consume"),
            new SettlementTradeActionStep("open-settlement-trade", settlementWorldObjectId: 41, caravanWorldObjectId: 42),
            new TimeControlActionStep("run", paused: false, speed: EndToEndGameSpeed.Superfast),
            new SelectionActionStep("select-meal", new[] { "thing:meal:7" }, additive: false),
            new CameraActionStep("frame-meal", new[] { "thing:meal:7" }, paddingPixels: 24),
            TradeDialogActionStep.AdjustTransfer(
                "buy-one-meal",
                "MealFine42",
                countDelta: -1),
            TradeDialogActionStep.Accept("accept-trade"),
            ProcessInputActionStep.Click("click-gizmo", new EndToEndScreenPoint(100, 200), EndToEndMouseButton.Left),
            ProcessInputActionStep.Drag(
                "drag-zone",
                new EndToEndScreenPoint(10, 20),
                new EndToEndScreenPoint(80, 90),
                EndToEndMouseButton.Left),
            new WaitUntilStep("meal-consumed", _ => true, waitDeadline),
            new AssertionStep("ware-returned", _ => EndToEndAssert.Equal(1, 1)),
            new ScreenshotStep("after", new[] { "pawn:42", "thing:plate:8" }, paddingPixels: 32),
            new CheckpointStep("meal-state", _ => new Dictionary<string, string> { ["meal"] = "consumed" })
        };

        Assert.That(Array.ConvertAll(steps, step => step.Kind), Is.EqualTo(new[]
        {
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Act,
            EndToEndStepKind.Wait,
            EndToEndStepKind.Observe,
            EndToEndStepKind.Observe,
            EndToEndStepKind.Observe
        }));
        Assert.That(((WaitUntilStep)steps[10]).Deadline, Is.SameAs(waitDeadline));
        Assert.Multiple(() =>
        {
            var settlementTrade = (SettlementTradeActionStep)steps[2];
            Assert.That(settlementTrade.SettlementWorldObjectId, Is.EqualTo(41));
            Assert.That(settlementTrade.CaravanWorldObjectId, Is.EqualTo(42));
            var adjust = (TradeDialogActionStep)steps[6];
            Assert.That(adjust.Action, Is.EqualTo(EndToEndTradeDialogAction.AdjustTransfer));
            Assert.That(adjust.ThingRuntimeId, Is.EqualTo("MealFine42"));
            Assert.That(adjust.CountDelta, Is.EqualTo(-1));
            var accept = (TradeDialogActionStep)steps[7];
            Assert.That(accept.Action, Is.EqualTo(EndToEndTradeDialogAction.Accept));
            Assert.That(accept.ThingRuntimeId, Is.Null);
        });
    }

    [Test]
    public void Checkpoint_artifacts_enforce_exact_count_field_and_aggregate_utf8_boundaries()
    {
        var exactCount = Enumerable.Range(0, CheckpointStep.MaximumArtifactFields)
            .ToDictionary(index => "k" + index, _ => "v");
        Assert.That(new CheckpointStep("exact-count", _ => exactCount).Capture(null!),
            Has.Count.EqualTo(CheckpointStep.MaximumArtifactFields));
        var overCount = Enumerable.Range(0, CheckpointStep.MaximumArtifactFields + 1)
            .ToDictionary(index => "k" + index, _ => "v");
        Assert.That(() => new CheckpointStep("over-count", _ => overCount).Capture(null!),
            Throws.InvalidOperationException.With.Message.Contains("at most"));

        var exactKey = new string('k', CheckpointStep.MaximumArtifactKeyUtf8Bytes);
        Assert.That(new CheckpointStep("exact-key", _ => new Dictionary<string, string>
            { [exactKey] = "v" }).Capture(null!), Has.Count.EqualTo(1));
        var overKey = exactKey + "k";
        Assert.That(() => new CheckpointStep("over-key", _ => new Dictionary<string, string>
            { [overKey] = "v" }).Capture(null!),
            Throws.InvalidOperationException.With.Message.Contains("key exceeds"));

        var exactValue = new string('v', CheckpointStep.MaximumArtifactValueUtf8Bytes);
        Assert.That(new CheckpointStep("exact-value", _ => new Dictionary<string, string>
            { ["k"] = exactValue }).Capture(null!), Has.Count.EqualTo(1));
        Assert.That(() => new CheckpointStep("over-value", _ => new Dictionary<string, string>
            { ["k"] = exactValue + "v" }).Capture(null!),
            Throws.InvalidOperationException.With.Message.Contains("value exceeds"));

        var aggregate = Enumerable.Range(0, 17).ToDictionary(
            index => "aggregate-" + index,
            _ => exactValue);
        Assert.That(() => new CheckpointStep("over-aggregate", _ => aggregate).Capture(null!),
            Throws.InvalidOperationException.With.Message.Contains("aggregate"));
    }

    [TestCase(0, 2)]
    [TestCase(1, 0)]
    [TestCase(-1, 2)]
    public void Settlement_trade_rejects_invalid_world_object_ids(int settlementId, int caravanId)
    {
        Assert.That(
            () => new SettlementTradeActionStep("trade", settlementId, caravanId),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Incident_action_requires_an_exact_def_and_preserves_any_exact_faction_load_id()
    {
        var step = new IncidentActionStep(
            "spawn trader caravan",
            "TraderCaravanArrival",
            factionLoadId: -17);

        Assert.Multiple(() =>
        {
            Assert.That(step.Kind, Is.EqualTo(EndToEndStepKind.Act));
            Assert.That(step.IncidentDefName, Is.EqualTo("TraderCaravanArrival"));
            Assert.That(step.FactionLoadId, Is.EqualTo(-17));
            Assert.That(
                () => new IncidentActionStep("incident", " "),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Save_load_action_requires_one_safe_leaf_save_name()
    {
        var step = new SaveLoadActionStep("reload", "ImmersiveChefs_FtvPersistence");

        Assert.Multiple(() =>
        {
            Assert.That(step.Kind, Is.EqualTo(EndToEndStepKind.Act));
            Assert.That(step.SaveName, Is.EqualTo("ImmersiveChefs_FtvPersistence"));
            Assert.That(
                () => new SaveLoadActionStep("reload", "folder/save"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => new SaveLoadActionStep("reload", ".."),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [TestCase("CON")]
    [TestCase("nul.txt")]
    [TestCase("COM9")]
    [TestCase("COM¹.rws")]
    [TestCase("COM²")]
    [TestCase("COM³.backup")]
    [TestCase("LPT1.backup")]
    [TestCase("LPT¹")]
    [TestCase("LPT².rws")]
    [TestCase("LPT³.backup")]
    [TestCase("save.")]
    [TestCase("save ")]
    [TestCase(" save")]
    public void Save_load_action_rejects_windows_aliases_and_noncanonical_names(string saveName)
    {
        Assert.That(
            () => new SaveLoadActionStep("reload", saveName),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Save_load_action_preserves_the_bounded_windows_leaf_contract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                new SaveLoadActionStep(
                    "reload",
                    new string('a', SaveLoadActionStep.MaxSaveNameLength)).SaveName,
                Has.Length.EqualTo(SaveLoadActionStep.MaxSaveNameLength));
            Assert.That(
                () => new SaveLoadActionStep(
                    "reload",
                    new string('a', SaveLoadActionStep.MaxSaveNameLength + 1)),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Dialog_confirmation_declares_only_the_exact_supported_window_type()
    {
        var step = new DialogConfirmationActionStep(
            "confirm survival batch",
            "Replimat.Dialog_BatchMakeSurvivalMeals");

        Assert.Multiple(() =>
        {
            Assert.That(step.Kind, Is.EqualTo(EndToEndStepKind.Act));
            Assert.That(step.ExpectedWindowTypeName, Is.EqualTo("Replimat.Dialog_BatchMakeSurvivalMeals"));
            Assert.That(
                () => new DialogConfirmationActionStep("confirm", " "),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Architect_category_action_declares_only_an_exact_category_and_open_state()
    {
        var open = new ArchitectCategoryActionStep("open production", "Production", open: true);
        var close = new ArchitectCategoryActionStep("close production", "Production", open: false);

        Assert.Multiple(() =>
        {
            Assert.That(open.Kind, Is.EqualTo(EndToEndStepKind.Act));
            Assert.That(open.CategoryDefName, Is.EqualTo("Production"));
            Assert.That(open.Open, Is.True);
            Assert.That(close.Open, Is.False);
            Assert.That(
                () => new ArchitectCategoryActionStep("open", " ", open: true),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Shared_steps_do_not_publish_an_unbounded_synchronous_delegate_action()
    {
        var unsafeProperties = typeof(EndToEndStep).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(EndToEndStep).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties())
            .Where(property => typeof(Delegate).IsAssignableFrom(property.PropertyType))
            .Where(property => property.DeclaringType?.Name.IndexOf("Action", StringComparison.Ordinal) >= 0)
            .Select(property => property.DeclaringType!.FullName + "." + property.Name)
            .ToArray();

        Assert.That(unsafeProperties, Is.Empty,
            "Typed action steps must not expose arbitrary synchronous delegates on Unity's main thread.");
    }

    [Test]
    public void Settlement_trade_can_require_one_exact_native_failure()
    {
        var step = new SettlementTradeActionStep(
            "reject the wrong settlement",
            settlementWorldObjectId: 41,
            caravanWorldObjectId: 42,
            expectedFailureCode: "settlement_trade_target_mismatch");

        Assert.That(step.ExpectedFailureCode, Is.EqualTo("settlement_trade_target_mismatch"));
    }

    [Test]
    public void Settlement_trade_rejects_a_blank_expected_failure_code()
    {
        Assert.That(
            () => new SettlementTradeActionStep("trade", 41, 42, expectedFailureCode: " "),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Gizmo_step_accepts_an_exact_architect_category_without_a_thing_owner()
    {
        var step = new GizmoActionStep(
            "draw-growing-zone",
            Array.Empty<string>(),
            "Designator_ZoneAdd_Growing",
            EndToEndGizmoInteraction.Drag,
            stableGizmoId: "Zone_Growing",
            startCell: new EndToEndMapCell(1, 2),
            endCell: new EndToEndMapCell(3, 4),
            architectCategoryDefNames: new[] { "Zone" });

        Assert.Multiple(() =>
        {
            Assert.That(step.TargetRuntimeIds, Is.Empty);
            Assert.That(step.ArchitectCategoryDefNames, Is.EqualTo(new[] { "Zone" }));
        });
    }

    [TestCase(0)]
    [TestCase(-10_001)]
    [TestCase(10_001)]
    public void Trade_adjustment_rejects_zero_or_unbounded_deltas(int countDelta)
    {
        Assert.That(
            () => TradeDialogActionStep.AdjustTransfer("trade", "MealFine42", countDelta),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Gizmo_step_can_declare_that_native_preflight_must_reject_the_target()
    {
        var step = new GizmoActionStep(
            "reject-floor-microwave",
            Array.Empty<string>(),
            "RimWorld.Designator_Build",
            EndToEndGizmoInteraction.Place,
            startCell: new EndToEndMapCell(10, 20),
            architectCategoryDefNames: new[] { "Production" },
            expectRejected: true);

        Assert.That(step.ExpectRejected, Is.True);
    }

    [Test]
    public void Gizmo_step_accepts_cardinal_rotation_only_for_native_place()
    {
        var step = new GizmoActionStep(
            "place-east",
            Array.Empty<string>(),
            "RimWorld.Designator_Build",
            EndToEndGizmoInteraction.Place,
            startCell: new EndToEndMapCell(10, 20),
            architectCategoryDefNames: new[] { "Production" },
            rotation: EndToEndCardinalRotation.East);

        Assert.Multiple(() =>
        {
            Assert.That(step.Rotation, Is.EqualTo(EndToEndCardinalRotation.East));
            Assert.That(
                () => new GizmoActionStep(
                    "drag-east",
                    Array.Empty<string>(),
                    "RimWorld.Designator_Zone",
                    EndToEndGizmoInteraction.Drag,
                    startCell: new EndToEndMapCell(10, 20),
                    endCell: new EndToEndMapCell(12, 22),
                    architectCategoryDefNames: new[] { "Zone" },
                    rotation: EndToEndCardinalRotation.East),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void Gizmo_step_preserves_the_original_nine_parameter_constructor_for_staged_bundles()
    {
        var constructor = typeof(GizmoActionStep).GetConstructor(new[]
        {
            typeof(string),
            typeof(IEnumerable<string>),
            typeof(string),
            typeof(EndToEndGizmoInteraction),
            typeof(string),
            typeof(EndToEndMapCell?),
            typeof(EndToEndMapCell?),
            typeof(IEnumerable<string>),
            typeof(bool)
        });

        Assert.That(constructor, Is.Not.Null,
            "Already-built dynamic E2E bundles must retain their exact GizmoActionStep constructor token.");
    }

    [Test]
    public void Assertion_and_context_helpers_produce_owned_failures_and_cleanup()
    {
        var context = new FakeContext();
        var cleanupCalled = false;
        context.DeferCleanup(() => cleanupCalled = true);

        var error = Assert.Throws<EndToEndAssertionException>(
            () => EndToEndAssert.Equal("expected", "actual", "meal state"));
        context.RunCleanup();

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("meal state").And.Contain("expected").And.Contain("actual"));
            Assert.That(cleanupCalled, Is.True);
            Assert.That(context.GetRequiredService<string>(), Is.EqualTo("service"));
        });
    }

    [Test]
    public void Float_menu_catalog_metadata_is_host_safe_and_preserves_the_visible_label()
    {
        var option = new EndToEndFloatMenuOption(
            "  float-0123456789abcdef  ",
            "Consume simple meal",
            disabled: false);

        Assert.Multiple(() =>
        {
            Assert.That(option.StableId, Is.EqualTo("float-0123456789abcdef"));
            Assert.That(option.Label, Is.EqualTo("Consume simple meal"));
            Assert.That(option.Disabled, Is.False);
            Assert.That(typeof(IEndToEndFloatMenuCatalog).Assembly, Is.EqualTo(typeof(IRimWorldEndToEndTest).Assembly));
        });
    }

    [TestCase("", "Consume simple meal", "stableId")]
    [TestCase("float-0123456789abcdef", "", "label")]
    public void Float_menu_catalog_metadata_rejects_missing_identity_or_label(
        string stableId,
        string label,
        string expectedParameter)
    {
        var error = Assert.Throws<ArgumentException>(
            () => new EndToEndFloatMenuOption(stableId, label, disabled: false));

        Assert.That(error!.ParamName, Is.EqualTo(expectedParameter));
    }

    [TestCase(EndToEndTestStatus.Pending, false)]
    [TestCase(EndToEndTestStatus.Arranging, false)]
    [TestCase(EndToEndTestStatus.Running, false)]
    [TestCase(EndToEndTestStatus.Cleaning, false)]
    [TestCase(EndToEndTestStatus.Passed, true)]
    [TestCase(EndToEndTestStatus.Failed, true)]
    [TestCase(EndToEndTestStatus.InfrastructureFailed, true)]
    [TestCase(EndToEndTestStatus.Skipped, true)]
    [TestCase(EndToEndTestStatus.Aborted, true)]
    public void Result_states_identify_terminal_outcomes(EndToEndTestStatus status, bool expectedTerminal)
    {
        Assert.That(EndToEndResultStates.IsTerminal(status), Is.EqualTo(expectedTerminal));
    }

    [RimWorldEndToEndTest(
        "immersive-chefs.adverse-meal",
        "fumblesneeze.immersivechefs",
        "brrainz.harmony",
        "ludeon.rimworld",
        "fumblesneeze.immersivechefs",
        MaxFrames = 900,
        MaxGameTicks = 2_500,
        MaxWallClockSeconds = 45)]
    public sealed class ValidProductTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest(
        "gateway.self-test",
        EndToEndTestContract.GatewayPackageId,
        "ludeon.rimworld")]
    public sealed class ValidGatewaySelfTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest(
        "gateway.explicit",
        EndToEndTestContract.GatewayPackageId,
        "ludeon.rimworld",
        EndToEndTestContract.GatewayPackageId)]
    public sealed class ExplicitGatewayTest : NoOpTest
    {
    }

    public sealed class MissingAttributeTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest("invalid.abstract", "fumblesneeze.immersivechefs", "ludeon.rimworld", "fumblesneeze.immersivechefs")]
    public abstract class AbstractAttributedTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest("invalid.interface", "fumblesneeze.immersivechefs", "ludeon.rimworld", "fumblesneeze.immersivechefs")]
    public sealed class AttributedNonTest
    {
    }

    [RimWorldEndToEndTest("invalid.owner", "fumblesneeze.immersivechefs", "ludeon.rimworld", "another.mod")]
    public sealed class MissingOwnerTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest("invalid.duplicate", "fumblesneeze.immersivechefs", "ludeon.rimworld", "fumblesneeze.immersivechefs", "FUMBLESNEEZE.IMMERSIVECHEFS")]
    public sealed class DuplicatePackageTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest("invalid.harmony-order", "fumblesneeze.immersivechefs", "ludeon.rimworld", "brrainz.harmony", "fumblesneeze.immersivechefs")]
    public sealed class CoreBeforeHarmonyTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest(
        "invalid.deadline",
        "fumblesneeze.immersivechefs",
        "ludeon.rimworld",
        "fumblesneeze.immersivechefs",
        MaxFrames = 0,
        MaxGameTicks = 0,
        MaxWallClockSeconds = 0)]
    public sealed class InvalidDeadlineTest : NoOpTest
    {
    }

    public abstract class NoOpTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }
    }

    private sealed class FakeContext : IEndToEndContext
    {
        private readonly List<Action> cleanup = new();

        public long FrameCount => 12;

        public int GameTick => 34;

        public object? GetService(Type serviceType) => serviceType == typeof(string) ? "service" : null;

        public void DeferCleanup(Action cleanupAction) => cleanup.Add(cleanupAction);

        public void RunCleanup()
        {
            for (var index = cleanup.Count - 1; index >= 0; index--)
            {
                cleanup[index]();
            }
        }
    }
}
