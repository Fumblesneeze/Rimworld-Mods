using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class EndToEndTestingContractTests
{
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
                "ludeon.rimworld",
                "brrainz.harmony",
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
    [TestCase(typeof(CoreNotFirstTest), "ludeon.rimworld")]
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
            new TimeControlActionStep("run", paused: false, speed: EndToEndGameSpeed.Superfast),
            new SelectionActionStep("select-meal", new[] { "thing:meal:7" }, additive: false),
            new CameraActionStep("frame-meal", new[] { "thing:meal:7" }, paddingPixels: 24),
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
            EndToEndStepKind.Wait,
            EndToEndStepKind.Observe,
            EndToEndStepKind.Observe,
            EndToEndStepKind.Observe
        }));
        Assert.That(((WaitUntilStep)steps[7]).Deadline, Is.SameAs(waitDeadline));
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
        "ludeon.rimworld",
        "brrainz.harmony",
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

    [RimWorldEndToEndTest("invalid.duplicate", "fumblesneeze.immersivechefs", "ludeon.rimworld", "fumblesneeze.immersivechefs", "fumblesneeze.immersivechefs")]
    public sealed class DuplicatePackageTest : NoOpTest
    {
    }

    [RimWorldEndToEndTest("invalid.core-order", "fumblesneeze.immersivechefs", "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.immersivechefs")]
    public sealed class CoreNotFirstTest : NoOpTest
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
