using System;
using System.Linq;
using Megamod.Patching;
using Verse;

namespace Megamod.Features.Diagnostics;

// Construct before admission. Neither this route nor its fallback invokes Verse.Log.Warning.
public sealed class ProtectedDiagnosticsRuntime : IDisposable
{
    private readonly StartupWarningFile? fallback;
    public ProtectedDiagnosticsRuntime(string activeSaveDataRoot)
    {
        ProtectedNativeWarning? native = null;
        try
        {
            var fileType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name == "Waylith")
                .Select(assembly => assembly.GetType("Waylith.Logging.FileLog", false))
                .SingleOrDefault(type => type != null);
            if (fileType != null) native = new ProtectedNativeWarning(typeof(Log), fileType);
            NativeUnavailableReason = fileType == null ? "The active native file backend could not be resolved." : native!.UnavailableReason;
        }
        catch (Exception exception) { NativeUnavailableReason = exception.GetBaseException().GetType().Name; }
        Exception? fileFailure = null;
        try { fallback = new StartupWarningFile(activeSaveDataRoot); }
        catch (Exception exception) { fileFailure = exception; }
        Sink = new ProtectedDiagnosticSink(message => native?.TryWrite(message) == true,
            message =>
            {
                if (fallback == null) throw new InvalidOperationException("The owned fallback path is unavailable.", fileFailure);
                fallback.Append(message);
            }, message => Console.Error.WriteLine(message));
    }

    public ProtectedDiagnosticSink Sink { get; }
    public string? NativeUnavailableReason { get; }
    public string? FallbackFilePath => fallback?.FilePath;
    public void Write(string warning) => Sink.Write(warning);
    public void Dispose() => fallback?.Dispose();
}
