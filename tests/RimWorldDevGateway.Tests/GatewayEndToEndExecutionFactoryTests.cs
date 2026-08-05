using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndExecutionFactoryTests
{
    [Test]
    public void Factory_builds_the_state_machine_with_the_exact_artifact_directory()
    {
        var clock = new FixedClock();
        var context = new GatewayEndToEndTestContext(() => 1, () => 2, _ => null);
        var driver = new PassingDriver();
        var isolation = new PassingIsolation();
        string? capturedDirectory = null;
        var factory = new GatewayEndToEndExecutionFactory(
            clock,
            () => context,
            directory =>
            {
                capturedDirectory = directory;
                return driver;
            },
            isolation);

        var machine = factory.Create(
            new[] { Descriptor() },
            "session-token",
            "exact-artifact-directory");

        Assert.Multiple(() =>
        {
            Assert.That(machine, Is.TypeOf<GatewayEndToEndExecutionStateMachine>());
            Assert.That(capturedDirectory, Is.EqualTo("exact-artifact-directory"));
            Assert.That(machine.Snapshot.GroupState, Is.EqualTo("pending"));
        });
    }

    [Test]
    public void Factory_rejects_a_null_driver_from_the_artifact_scoped_factory()
    {
        var factory = new GatewayEndToEndExecutionFactory(
            new FixedClock(),
            () => new GatewayEndToEndTestContext(() => 0, () => 0, _ => null),
            _ => null!,
            new PassingIsolation());

        Assert.Throws<InvalidOperationException>(() =>
            factory.Create(new[] { Descriptor() }, null, "artifacts"));
    }

    private static GatewayEndToEndRuntimeTestDescriptor Descriptor() => new(
        "factory.test",
        "fumblesneeze.rimworlddevgateway",
        new[] { "ludeon.rimworld" },
        typeof(EmptyTest).FullName!,
        100,
        100,
        10,
        typeof(EmptyTest));

    private sealed class EmptyTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context) { }
        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }
    }

    private sealed class FixedClock : IGatewayEndToEndClock
    {
        public long FrameCount => 1;
        public int GameTick => 2;
        public DateTimeOffset UtcNow => new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class PassingDriver : IGatewayEndToEndStepDriver
    {
        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context) =>
            GatewayEndToEndCompletedStepOperation.Passed();
    }

    private sealed class PassingIsolation : IGatewayEndToEndTestIsolation
    {
        public bool IsReady => true;

        public void Prepare(IEndToEndContext context) { }
        public bool Cleanup(IEndToEndContext context) => true;
    }
}
