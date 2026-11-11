using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Unit.Patching;

[TestFixture]
public sealed class AdmissionCoordinatorTests
{
    [Test]
    public void LongInstallationFailureCannotHideIncompleteCleanup()
    {
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => { }, rollback: () => throw new InvalidOperationException("warning rollback failed")),
             Candidate("DDI-REPORT-ERROR", () => throw new InvalidOperationException("install failed " + new string('x', 512)),
                 rollback: () => throw new InvalidOperationException("error rollback failed"))]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.IsAdmitted(group.Id), Is.False);
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(warnings[0], Does.Contain("cleanupFailures=2").And.Contain("rollback failed").And.Contain("install failed"));
        Assert.That(warnings[1], Does.Contain("cleanupFailures=2"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FailedWarningSinksRetainBoundedInspectableFailuresWithoutRecursion(bool hasFallback)
    {
        var primaryCalls = 0;
        var fallbackCalls = 0;
        Action<string> fallback = _ => { fallbackCalls++; throw new InvalidOperationException("fallback unavailable"); };
        var coordinator = new AdmissionCoordinator(_ =>
        {
            primaryCalls++;
            throw new InvalidOperationException("primary unavailable\n" + new string('x', 1000));
        }, hasFallback ? fallback : null);
        for (int index = 0; index < 66; index++)
        {
            var group = new PatchGroup("rejected-" + index, AdmissionPhase.Early,
                [Candidate("DDI-REPORT-WARNING-" + index, () => { }, () => "unsupported")]);
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        }

        Assert.That(primaryCalls, Is.EqualTo(66));
        Assert.That(fallbackCalls, Is.EqualTo(hasFallback ? 66 : 0));
        Assert.That(coordinator.UndeliveredWarningCount, Is.EqualTo(66));
        Assert.That(coordinator.DeliveryFailures, Has.Count.EqualTo(64));
        Assert.That(coordinator.DeliveryFailures[0].PatchId, Is.EqualTo("DDI-REPORT-WARNING-0"));
        Assert.That(coordinator.DeliveryFailures[0].Warning, Does.Contain("unsupported"));
        Assert.That(coordinator.DeliveryFailures[0].PrimaryFailure.Length, Is.LessThanOrEqualTo(256));
        Assert.That(coordinator.DeliveryFailures[0].PrimaryFailure, Does.Not.Contain("\n"));
        Assert.That(coordinator.DeliveryFailures[0].FallbackFailure,
            Does.Contain(hasFallback ? "fallback unavailable" : "No fallback"));
        Assert.That(coordinator.Apply(new PatchGroup("independent", AdmissionPhase.Early,
            [Candidate("independent", () => { })]), AdmissionPhase.Early), Is.True);
    }

    [Test]
    public void PrimaryWarningFailureUsesIndependentFallbackAndDoesNotAbortOtherGroups()
    {
        var primaryCalls = 0;
        var fallback = new List<string>();
        var coordinator = new AdmissionCoordinator(_ =>
        {
            primaryCalls++;
            throw new InvalidOperationException("native logging unavailable");
        }, fallback.Add);
        var rejected = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => { }, () => "unsupported text flow")]);
        var independentInstalled = false;
        var independent = new PatchGroup("diagnostics.palette", AdmissionPhase.DefsReady,
            [Candidate("LOG-PRESENTATION-PALETTE", () => independentInstalled = true)]);

        Assert.That(coordinator.Apply(rejected, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.Apply(rejected, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.Apply(independent, AdmissionPhase.DefsReady), Is.True);
        Assert.That(primaryCalls, Is.EqualTo(1));
        Assert.That(fallback, Has.Count.EqualTo(1));
        Assert.That(fallback[0], Does.Contain("DDI-REPORT-WARNING").And.Contain("unsupported text flow"));
        Assert.That(independentInstalled, Is.True);
    }

    [Test]
    public void RejectionWarningsAreBoundedSingleLineWithActionableIdentity()
    {
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var candidate = Candidate("DDI-REPORT-WARNING", () => { },
            () => "changed text\r\n" + new string('x', 10000)) with { Target = "Verse.Log.Warning\n" + new string('y', 10000) };

        Assert.That(coordinator.Apply(new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [candidate]), AdmissionPhase.Early), Is.False);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].Length, Is.LessThanOrEqualTo(2000));
        Assert.That(warnings[0], Does.Not.Contain("\r").And.Not.Contain("\n"));
        Assert.That(warnings[0], Does.Contain("DDI-REPORT-WARNING").And.Contain("Verse.Log.Warning")
            .And.Contain("changed text").And.Contain("ordinary native diagnostics"));
    }

    [Test]
    public void ReentrantValidationCannotInstallOrPublishAPartialGroup()
    {
        var coordinator = new AdmissionCoordinator(_ => { });
        var probes = 0;
        var installs = 0;
        bool nestedResult = true;
        bool publishedDuringInstall = true;
        PatchGroup? group = null;
        group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () =>
            {
                installs++;
                publishedDuringInstall = coordinator.IsAdmitted(group!.Id);
            }, () =>
            {
                if (++probes == 1) nestedResult = coordinator.Apply(group!, AdmissionPhase.Early);
                return null;
            })]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(nestedResult, Is.False);
        Assert.That(publishedDuringInstall, Is.False);
        Assert.That(probes, Is.EqualTo(1));
        Assert.That(installs, Is.EqualTo(1));
        Assert.That(coordinator.IsAdmitted(group.Id), Is.True);
    }

    [Test]
    public void CleanupFailureDoesNotStopOtherOwnedCleanupOrPublishTheGroup()
    {
        var cleanup = new List<string>();
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => { }, rollback: () => cleanup.Add("warning")),
             Candidate("DDI-REPORT-ERROR", () => throw new InvalidOperationException("install failure"), rollback: () =>
                { cleanup.Add("error"); throw new InvalidOperationException("cleanup failure"); })]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.IsAdmitted(group.Id), Is.False);
        Assert.That(cleanup, Is.EqualTo(new[] { "error", "warning" }));
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(warnings[0], Does.Contain("install failure").And.Contain("cleanup failure"));
    }

    [TestCase("active provider mlie.logafterdeferror")]
    [TestCase("required definition reporting is unavailable")]
    public void UnavailableOwnedBehaviorSkipsProbesWithoutBlockingIndependentGroups(string reason)
    {
        var probes = 0;
        var installs = new List<string>();
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var rejected = new PatchGroup("diagnostics.definition-context", AdmissionPhase.Early,
            [Candidate("DDI-CONVERT-NEW", () => installs.Add("context"), () => { probes++; return null; })]);
        var independent = new PatchGroup("diagnostics.palette", AdmissionPhase.DefsReady,
            [Candidate("LOG-PRESENTATION-PALETTE", () => installs.Add("palette"))]);

        Assert.That(coordinator.Apply(rejected, AdmissionPhase.Early, unavailableReason: reason), Is.False);
        Assert.That(coordinator.Apply(independent, AdmissionPhase.DefsReady), Is.True);
        Assert.That(probes, Is.Zero);
        Assert.That(installs, Is.EqualTo(new[] { "palette" }));
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain(reason));
    }

    [Test]
    public void InstallerCannotReplaceALaterValidatedCandidate()
    {
        var installed = new List<string>();
        var coordinator = new AdmissionCoordinator(_ => { });
        var replacement = Candidate("unexpected", () => installed.Add("unvalidated replacement"),
            () => "unsupported replacement");
        var candidates = new PatchCandidate[2];
        candidates[0] = Candidate("DDI-REPORT-WARNING", () =>
        {
            installed.Add("warning");
            candidates[1] = replacement;
        });
        candidates[1] = Candidate("DDI-REPORT-ERROR", () => installed.Add("validated error"));
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early, candidates);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(installed, Is.EqualTo(new[] { "warning", "validated error" }));
    }

    [TestCase(AdmissionPhase.DefsReady, true)]
    [TestCase(AdmissionPhase.Early, false)]
    public void NonCandidateDoesNotProbeOrWarnAndCanActivateAtItsEnabledPhase(AdmissionPhase phase, bool enabled)
    {
        var warnings = new List<string>();
        var probes = 0;
        var installs = 0;
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => installs++, () => { probes++; return null; })]);

        Assert.That(coordinator.Apply(group, phase, enabled), Is.False);
        Assert.That(probes, Is.Zero);
        Assert.That(installs, Is.Zero);
        Assert.That(warnings, Is.Empty);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(probes, Is.EqualTo(1));
        Assert.That(installs, Is.EqualTo(1));
    }

    [Test]
    public void FailedCurrentInstallationRollsBackItsPartialWorkAndPriorOwnedWork()
    {
        var installed = new HashSet<string> { "foreign-existing", "owned-existing" };
        var cleanupOrder = new List<string>();
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => installed.Add("new-warning"), rollback: () =>
                { cleanupOrder.Add("warning"); installed.Remove("new-warning"); }),
             Candidate("DDI-REPORT-ERROR", () =>
                { installed.Add("partial-error"); throw new InvalidOperationException("install failed"); }, rollback: () =>
                { cleanupOrder.Add("error"); installed.Remove("partial-error"); })]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(installed, Is.EquivalentTo(new[] { "foreign-existing", "owned-existing" }));
        Assert.That(cleanupOrder, Is.EqualTo(new[] { "error", "warning" }));
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(warnings[0], Does.Contain("install failed"));
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(cleanupOrder, Has.Count.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReportingGroupRejectsBeforeAnyInstallationAndWarnsOncePerEnabledPatch(bool validatorThrows)
    {
        var warnings = new List<string>();
        var installed = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => installed.Add("warning")),
             Candidate("DDI-REPORT-ERROR", () => installed.Add("error"),
                 () => validatorThrows ? throw new InvalidOperationException("missing error text flow") : "missing error text flow")]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(installed, Is.Empty);
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(warnings[0], Does.Contain("fumblesneeze.megamod").And.Contain("DDI-REPORT-WARNING")
            .And.Contain("targetSpec=").And.Contain("Verse.Log").And.Contain("missing error text flow")
            .And.Contain("definition diagnostic context").And.Contain("ordinary native diagnostics"));
        Assert.That(warnings[1], Does.Contain("DDI-REPORT-ERROR"));
    }

    [Test]
    public void EarlyReportingGroupInstallsOnceDespiteRepeatedInitialization()
    {
        var warnings = new List<string>();
        var installed = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [Candidate("DDI-REPORT-WARNING", () => installed.Add("warning")),
             Candidate("DDI-REPORT-ERROR", () => installed.Add("error"))]);

        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(installed, Is.EqualTo(new[] { "warning", "error" }));
        Assert.That(warnings, Is.Empty);
    }

    private static PatchCandidate Candidate(string id, Action install,
        Func<string?>? validate = null, Action? rollback = null)
        => new(id, id, "Verse.Log", "definition diagnostic context", "ordinary native diagnostics",
            validate ?? (() => null), install, rollback ?? (() => { }));
}
