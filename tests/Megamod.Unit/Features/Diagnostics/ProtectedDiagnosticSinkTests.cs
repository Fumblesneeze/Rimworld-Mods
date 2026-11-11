using Megamod.Features.Diagnostics;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class ProtectedDiagnosticSinkTests
{
    [Test]
    public void EveryConcurrentRequiredWarningIsAttemptedWhileFailureReceiptsStayBounded()
    {
        int attempts = 0;
        string failure = "failure\n" + new string('x', 10000);
        var sink = new ProtectedDiagnosticSink(_ => { Interlocked.Increment(ref attempts); throw new Exception(failure); },
            _ => throw new Exception(failure), _ => throw new Exception(failure));
        Parallel.For(0, 80, index => sink.Write("Patch P" + index + "\r\n" + new string('w', 10000)));
        Assert.That(attempts, Is.EqualTo(80));
        Assert.That(sink.UndeliveredCount, Is.EqualTo(80));
        Assert.That(sink.DeliveryFailures, Has.Count.EqualTo(64));
        Assert.That(sink.ReentrantCount, Is.Zero);
        Assert.That(sink.DeliveryFailures.All(item => item.Warning.Length <= 4096 && !item.Warning.Contains('\n') &&
            item.NativeFailure.Length <= 256 && item.FallbackFailure.Length <= 256 && item.LastResortFailure.Length <= 256), Is.True);
    }
    [Test]
    public void RecursiveFailureNotificationCannotCreateAnotherWarningOrPoisonTheNextWrite()
    {
        ProtectedDiagnosticSink sink = null!;
        int calls = 0;
        var written = new List<string>();
        sink = new ProtectedDiagnosticSink(_ =>
        {
            if (++calls == 1) sink.Write("recursive failure notification");
            return false;
        }, written.Add, _ => { });
        sink.Write("first");
        sink.Write("second");
        Assert.That(written, Is.EqualTo(new[] { "first", "second" }));
        Assert.That(sink.ReentrantCount, Is.EqualTo(1));
    }
    [Test]
    public void AllSinkFailuresStayInspectableWithoutAbortingAdmissionOrRecursing()
    {
        var attempts = new List<string>();
        var sink = new ProtectedDiagnosticSink(_ => { attempts.Add("native"); throw new InvalidOperationException("native failed"); },
            _ => { attempts.Add("fallback"); throw new InvalidOperationException("file failed"); },
            _ => { attempts.Add("stderr"); throw new InvalidOperationException("stderr failed"); });
        Assert.DoesNotThrow(() => sink.Write("Patch P could not be admitted"));
        Assert.That(attempts, Is.EqualTo(new[] { "native", "fallback", "stderr" }));
        Assert.That(sink.UndeliveredCount, Is.EqualTo(1));
        Assert.That(sink.DeliveryFailures.Single(), Is.EqualTo(new ProtectedDeliveryFailure("Patch P could not be admitted", "native failed", "file failed", "stderr failed")));
    }
    [Test]
    public void UnavailableNativeWarningIsWrittenOnceThroughTheIndependentFallback()
    {
        var fallback = new List<string>();
        int lastResort = 0;
        var sink = new ProtectedDiagnosticSink(_ => false, fallback.Add, _ => lastResort++);
        sink.Write("[fumblesneeze.megamod] Patch P skipped; targetSpec=P; native fallback remains.");
        Assert.That(fallback, Has.Count.EqualTo(1));
        Assert.That(fallback[0], Does.Contain("targetSpec=P"));
        Assert.That(lastResort, Is.Zero);
    }
}
