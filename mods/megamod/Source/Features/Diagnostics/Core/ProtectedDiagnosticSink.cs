using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Megamod.Features.Diagnostics;

public sealed record ProtectedDeliveryFailure(string Warning, string NativeFailure, string FallbackFailure, string LastResortFailure);

public sealed class ProtectedDiagnosticSink
{
    private readonly ThreadLocal<bool> writing = new();
    private long reentrantCount;
    public long ReentrantCount => Interlocked.Read(ref reentrantCount);
    private long undeliveredCount;
    private readonly ConcurrentQueue<ProtectedDeliveryFailure> failures = new();
    public long UndeliveredCount => Interlocked.Read(ref undeliveredCount);
    public IReadOnlyList<ProtectedDeliveryFailure> DeliveryFailures => failures.ToArray();
    private readonly Func<string, bool> native;
    private readonly Action<string> fallback;
    private readonly Action<string> lastResort;
    public ProtectedDiagnosticSink(Func<string, bool> native, Action<string> fallback, Action<string> lastResort)
    {
        this.native = native;
        this.fallback = fallback;
        this.lastResort = lastResort;
    }
    public void Write(string warning)
    {
        if (writing.Value) { Interlocked.Increment(ref reentrantCount); return; }
        writing.Value = true;
        try { Deliver(Bound(warning, 4096)); }
        finally { writing.Value = false; }
    }
    private void Deliver(string warning)
    {
        string nativeFailure;
        try { if (native(warning)) return; nativeFailure = "Native warning route unavailable."; }
        catch (Exception exception) { nativeFailure = Bound(exception.GetBaseException().Message, 256); }
        string fallbackFailure;
        try { fallback(warning); return; }
        catch (Exception exception) { fallbackFailure = Bound(exception.GetBaseException().Message, 256); }
        try { lastResort(warning); }
        catch (Exception exception)
        {
            if (Interlocked.Increment(ref undeliveredCount) <= 64)
                failures.Enqueue(new ProtectedDeliveryFailure(warning, nativeFailure, fallbackFailure, Bound(exception.GetBaseException().Message, 256)));
        }
    }
    private static string Bound(string value, int limit)
    {
        var chars = (value.Length <= limit ? value : value.Substring(0, limit - 3) + "...").ToCharArray();
        for (int i = 0; i < chars.Length; i++) if (char.IsControl(chars[i]) || chars[i] is '\u2028' or '\u2029') chars[i] = ' ';
        return new string(chars);
    }
}
