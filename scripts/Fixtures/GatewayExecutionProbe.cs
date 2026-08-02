using System;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway.LiveProbe;

public static class Entry
{
    public static string Execute(string requestJson)
    {
        var before = Prefs.DevMode;
        Prefs.DevMode = !before;
        var mutationObserved = Prefs.DevMode != before;
        Prefs.DevMode = before;

        return string.Format(
            CultureInfo.InvariantCulture,
            "{{\"frame\":{0},\"mutationObserved\":{1},\"programState\":\"{2}\",\"requestId\":\"{3}\",\"threadId\":{4}}}",
            Time.frameCount,
            mutationObserved ? "true" : "false",
            Current.ProgramState,
            Escape(requestJson),
            Thread.CurrentThread.ManagedThreadId);
    }

    private static string Escape(string value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
}
