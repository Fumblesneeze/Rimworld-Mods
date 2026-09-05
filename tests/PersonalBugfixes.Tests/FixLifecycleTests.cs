using NUnit.Framework;

namespace PersonalBugfixes.Tests;

[TestFixture]
public sealed class FixLifecycleTests
{
    private sealed class Fix : IPersonalFix
    {
        public string Id { get; set; } = "fixture";
        public string TargetDescription => "fixture target";
        public bool TargetPresent { get; set; } = true;
        public string? ShapeError;
        public ProbeOutcome Before = ProbeOutcome.BugPresent;
        public ProbeOutcome After = ProbeOutcome.Healthy;
        public bool Installed;
        public bool Verified = true;
        public int Applications, Removals, Probes;
        public bool ThrowAfterApply, ThrowOnRemove;
        public string? Inspect() => ShapeError;
        public ProbeOutcome Probe() { Probes++; return Installed ? After : Before; }
        public void Apply() { Applications++; Installed = true; if (ThrowAfterApply) throw new InvalidOperationException("apply failed"); }
        public bool VerifyInstalled() => Verified;
        public void Remove() { Removals++; if (ThrowOnRemove) throw new InvalidOperationException("cleanup failed"); Installed = false; }
    }

    [Test]
    public void Supported_bug_is_applied_only_after_failed_original_probe_and_verified_again()
    {
        var fix = new Fix();
        var lifecycle = new FixLifecycle();
        var result = lifecycle.Evaluate(fix);
        Assert.That(result.State, Is.EqualTo(FixState.Applied));
        Assert.That(fix.Probes, Is.EqualTo(2));
        Assert.That(lifecycle.Evaluate(fix), Is.SameAs(result));
        Assert.That(fix.Applications, Is.EqualTo(1));
    }

    [TestCase(false, null, ProbeOutcome.BugPresent, FixState.Absent, 0)]
    [TestCase(true, "ambiguous IL", ProbeOutcome.BugPresent, FixState.Incompatible, 0)]
    [TestCase(true, null, ProbeOutcome.Healthy, FixState.NotRequired, 1)]
    [TestCase(true, null, ProbeOutcome.Inconclusive, FixState.Incompatible, 1)]
    public void Does_not_install_without_both_shape_and_bug_evidence(bool present, string? shape,
        ProbeOutcome before, FixState expected, int probes)
    {
        var fix = new Fix { TargetPresent = present, ShapeError = shape, Before = before };
        Assert.That(new FixLifecycle().Evaluate(fix).State, Is.EqualTo(expected));
        Assert.That(fix.Applications, Is.Zero);
        Assert.That(fix.Probes, Is.EqualTo(probes));
    }

    [TestCase(false, ProbeOutcome.Healthy)]
    [TestCase(true, ProbeOutcome.BugPresent)]
    [TestCase(true, ProbeOutcome.Inconclusive)]
    public void Failed_postcondition_removes_only_the_applied_fix(bool verified, ProbeOutcome after)
    {
        var fix = new Fix { Verified = verified, After = after };
        var result = new FixLifecycle().Evaluate(fix);
        Assert.That(result.State, Is.EqualTo(FixState.Failed));
        Assert.That(result.Warning, Is.True);
        Assert.That(fix.Removals, Is.EqualTo(1));
        Assert.That(fix.Installed, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Failed_partial_application_logs_cleanup_and_does_not_block_another_fix(bool cleanupFails)
    {
        var lifecycle = new FixLifecycle();
        var bad = new Fix { ThrowAfterApply = true, ThrowOnRemove = cleanupFails };
        var result = lifecycle.Evaluate(bad);
        Assert.That(result.State, Is.EqualTo(FixState.Failed));
        Assert.That(result.Detail, Does.Contain(cleanupFails ? "ROLLBACK FAILED: cleanup failed" : "own patch removed"));
        Assert.That(bad.Removals, Is.EqualTo(1));
        Assert.That(lifecycle.Evaluate(new Fix { Id = "next" }).State, Is.EqualTo(FixState.Applied));
    }
}
