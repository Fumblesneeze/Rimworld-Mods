using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Actual XML loading supplies the evidence. Only native window actions select rows and copy output.
[RimWorldEndToEndTest("megamod.diagnostics.definition-initial-default", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddiagnosticsfixture",
    MaxFrames = 2400, MaxGameTicks = 2400, MaxWallClockSeconds = 120)]
public sealed class NativeDefinitionDiagnosticsLogTests : IRimWorldEndToEndTest
{
    private readonly NativeDefinitionDiagnosticsLogScenario scenario = new(expectedLegacy: false);

    public void Arrange(IEndToEndContext context) => scenario.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => scenario.Execute(context);
}

// One stateful native workflow; the public declarations select its immutable expected startup mode.
internal sealed class NativeDefinitionDiagnosticsLogScenario
{
    private readonly bool expectedLegacy;
    private bool observedLegacy;
    private const string ContextMarker = "\n\n[Def Error] Current definition context\n";
    private const int ClipboardCharacterLimit = 256 * 1024;
    private static readonly string[] Names =
    {
        "MegamodDiagnosticsFixtureFirst", "MegamodDiagnosticsFixtureSecond", "MegamodDiagnosticsFixtureMissingType"
    };
    private LogMessage[] initialRecords = Array.Empty<LogMessage>();
    private LogMessage[] targets = Array.Empty<LogMessage>();
    private string[] targetTexts = Array.Empty<string>();
    private string[] targetTraces = Array.Empty<string>();
    private EndToEndNativeWindow window = null!;
    private EditWindow_Log? initialLog;
    private bool initialAutoOpenPending;
    private EditWindow_Log? ownedLog;
    private string pointerScope = null!;
    private NativeDiagnosticClipboard.Snapshot? priorClipboard;
    private string? expectedClipboard;
    private string? copiedClipboard;
    private bool clipboardAttempted;
    private bool clipboardRestoreAttempted;
    private string clipboardCleanup = "not attempted";
    private NativeDiagnosticLogView view = null!;

    internal NativeDefinitionDiagnosticsLogScenario(bool expectedLegacy) => this.expectedLegacy = expectedLegacy;

    public void Arrange(IEndToEndContext context)
    {
        observedLegacy = GenCommandLine.CommandLineArgPassed("legacy-xml-deserializer");
        EndToEndAssert.Equal(expectedLegacy, observedLegacy,
            "Native XML loading mode differs from this scenario's declaration.");
        // No synthetic diagnostics, Def creation, history clearing, settings edits or once-key resets.
        view = new NativeDiagnosticLogView(() => pointerScope);
        initialRecords = Log.Messages.ToArray();
        initialLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
        initialAutoOpenPending = EditWindow_Log.wantsToOpen;
        var speed = Find.TickManager.CurTimeSpeed;
        var priorView = new[] { "selectedMessage", "messagesScrollPosition", "detailsScrollPosition", "detailsPaneHeight" }
            .Select(name => typeof(EditWindow_Log).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Native log observation field unavailable: " + name))
            .ToDictionary(field => field, field => field.GetValue(null));
        context.DeferCleanup(() =>
        {
            try { RestoreClipboardIfOwned(); }
            finally
            {
                try
                {
                    if (ownedLog != null)
                    {
                        var current = Find.WindowStack.WindowOfType<EditWindow_Log>();
                        if (current != null && !ReferenceEquals(current, ownedLog))
                            throw new InvalidOperationException("Native log was replaced; cleanup cannot claim the replacement window.");
                        if (initialLog == null) current?.Close();
                        else if (current == null) Find.WindowStack.Add(initialLog);
                        // Cleanup only: restore the disposable view's prior state, never set an asserted result.
                        foreach (var pair in priorView) pair.Key.SetValue(null, pair.Value);
                        EditWindow_Log.wantsToOpen = initialAutoOpenPending;
                    }
                }
                finally { Find.TickManager.CurTimeSpeed = speed; }
            }
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("actual loader records before native log interaction", _ => BaselineEvidence());
        yield return Shot("native view before XML diagnostic log interaction");
        yield return new AssertionStep("three real loader occurrences are available for native inspection", _ =>
        {
            EndToEndAssert.True(Prefs.DevMode, "The isolated native log workflow requires developer mode.");
            targets = Names.Select(name => initialRecords.Single(message =>
                message.text.Contains("\nDefinition: " + name + "\n", StringComparison.Ordinal))).ToArray();
            targetTexts = targets.Select(message => message.text).ToArray();
            targetTraces = targets.Select(message => message.StackTrace).ToArray();
            for (int i = 0; i < targets.Length; i++)
            {
                EndToEndAssert.Equal(i < 2 ? LogMessageType.Warning : LogMessageType.Error, targets[i].type);
                EndToEndAssert.Equal(1, targets[i].repeats, "Initial loader records must remain separate occurrences.");
                EndToEndAssert.True(targetTexts[i].Contains("Package: fumblesneeze.megamoddiagnosticsfixture", StringComparison.Ordinal));
                EndToEndAssert.True(targetTexts[i].Contains("XML file (relative to source mod):", StringComparison.Ordinal)
                    && targetTexts[i].Contains("DefinitionDiagnostics.xml", StringComparison.Ordinal));
                int marker = targetTexts[i].IndexOf(ContextMarker, StringComparison.Ordinal);
                EndToEndAssert.True(marker > 0 && marker == targetTexts[i].LastIndexOf(ContextMarker, StringComparison.Ordinal),
                    "Each accepted occurrence must retain one context section and original text.");
                if (i < 2) EndToEndAssert.Equal("Parsed 37.5 as int.", targetTexts[i].Substring(0, marker));
                else EndToEndAssert.True(targetTexts[i].StartsWith("Type MegamodDiagnosticsFixtureMissingDefType is not a Def type or could not be found, in file ", StringComparison.Ordinal));
            }
            EndToEndAssert.False(ReferenceEquals(targets[0], targets[1]), "The same-text warnings must not collapse into one row.");
        });
        yield return new NativeDeveloperLogActionStep("open the ordinary developer log or retain its native automatic opening", true);
        yield return Capture("capture the native log before any coordinate action");
        yield return Shot("native log before selecting a fixture occurrence");
        yield return new AssertionStep("native log matches the inspected titled content layout", _ =>
        {
            EndToEndAssert.Equal("Debug log", ownedLog!.optionalTitle);
            EndToEndAssert.True(ContentWidth > 0 && ContentHeight > 180, "Native log viewport is too small.");
        });
        yield return new NativePointerScopeBeginStep("keep the pointer owned during native log inspection", handle => pointerScope = handle);

        for (int i = 0; i < Names.Length; i++)
        {
            int index = i;
            yield return Capture("capture the log before restoring the compact trace pane for " + Names[i]);
            yield return ToolbarClick("use native Trace small before locating " + Names[i], "Trace small");
            Rect row = default;
            bool visible = false;
            for (int attempt = 0; ; attempt++)
            {
                yield return Capture("capture native row viewport " + attempt + " for " + Names[i]);
                yield return new CheckpointStep("observe row geometry " + attempt + " for " + Names[i], _ =>
                {
                    row = MeasureRow(targets[index], out visible);
                    return GeometryEvidence(row, visible);
                });
                yield return Shot("native list observation " + attempt + " for " + Names[i]);
                if (visible) break;
                Vector2 trackPoint = default;
                Vector2 beforeScroll = default;
                yield return new AssertionStep("a bounded native track click can expose " + Names[i], _ =>
                {
                    EndToEndAssert.True(attempt < 3, "Three native track attempts did not expose the row; inspect the retained viewport observations.");
                    AssertTargetsUnchanged();
                    beforeScroll = Read<Vector2>("messagesScrollPosition");
                    trackPoint = PlanNativeTrackClick(row);
                });
                yield return new CheckpointStep("observed native scrollbar target for " + Names[i], _ => new Dictionary<string, string>
                {
                    ["trackPointInContent"] = trackPoint.ToString(), ["scrollBefore"] = beforeScroll.ToString(),
                    ["provider"] = "Inspected RimWorld 1.7 UnityStubs.GUI.HandleScrollbars; outside-thumb track click"
                });
                yield return Click("click the native scrollbar track toward " + Names[i], trackPoint);
                yield return Shot("native viewport after scrollbar action for " + Names[i]);
                yield return new AssertionStep("native scrollbar action changed vertical scroll", _ =>
                {
                    Vector2 after = Read<Vector2>("messagesScrollPosition");
                    EndToEndAssert.True(Math.Abs(after.y - beforeScroll.y) > 0.5f && Math.Abs(after.x - beforeScroll.x) < 0.01f,
                        "The native track action did not produce the observed vertical-only movement; inspect the preceding screenshot.");
                });
            }
            yield return new AssertionStep("the actual fixture row is wholly visible: " + Names[i], _ =>
            {
                AssertTargetsUnchanged();
                EndToEndAssert.True(visible, "The retained row is hidden or outside this measured native viewport. Inspect the preceding screenshot/geometry; a native scrolling workflow is required, not a guessed click.");
            });
            yield return Click("select the actual native row for " + Names[i], row.center);
            yield return new AssertionStep("native selection identifies " + Names[i], _ => AssertSelected(index));
            yield return Capture("capture the selected log before expanding its native details");
            yield return ToolbarClick("expand native details for " + Names[i], "Trace big");
            yield return Shot("selected native details for " + Names[i]);
            yield return new AssertionStep("expanded native details still belong to " + Names[i], _ =>
            {
                AssertSelected(index);
                EndToEndAssert.True(Read<float>("detailsPaneHeight") > 100f, "The native Trace big action must expand the detail pane.");
                var font = Text.Font;
                try
                {
                    Text.Font = GameFont.Tiny;
                    EndToEndAssert.True(Text.CalcHeight(targetTexts[index], ContentWidth - 16) <= Read<float>("detailsPaneHeight") - 7,
                        "The complete diagnostic context does not fit this expanded native detail pane. Retain the screenshot and add a native detail-scroll workflow.");
                }
                finally { Text.Font = font; }
            });
            yield return new CheckpointStep("selected full text and available trace for " + Names[i], _ => new Dictionary<string, string>
            {
                ["definition"] = Names[index], ["severity"] = targets[index].type.ToString(),
                ["fullText"] = targets[index].text, ["availableTrace"] = targets[index].StackTrace,
                ["selectedReferenceVerified"] = "true"
            });
        }

        yield return Capture("capture the native Copy button before clipboard action");
        yield return Shot("selected diagnostic before native Copy all");
        yield return new AssertionStep("prepare bounded clipboard ownership without altering it", _ =>
        {
            AssertTargetsUnchanged();
            expectedClipboard = ExpectedNativeCopy(Log.Messages.ToArray());
            priorClipboard = NativeDiagnosticClipboard.Observe(ClipboardCharacterLimit);
            EndToEndAssert.True(priorClipboard.Supported,
                "Native Copy requires an empty or plain-text-only clipboard. Image, file, rich-text and unknown formats are preserved by refusing this action.");
            EndToEndAssert.False(string.Equals(priorClipboard.Text, expectedClipboard, StringComparison.Ordinal),
                "The clipboard already equals this log. A changed result cannot be demonstrated without overwriting a precondition.");
        });
        yield return NativeCopyClick();
        yield return new AssertionStep("native Copy retained each selected diagnostic and its trace", _ =>
        {
            copiedClipboard = ReadBoundedClipboard();
            // Do not include either clipboard value in assertion errors (the prior value is private).
            EndToEndAssert.True(string.Equals(expectedClipboard, copiedClipboard, StringComparison.Ordinal),
                "The native clipboard result differs from the bounded retained-log snapshot; inspect action evidence without overwriting an unrelated clipboard value.");
            for (int i = 0; i < targets.Length; i++)
                EndToEndAssert.True(copiedClipboard.Contains(targetTexts[i], StringComparison.Ordinal)
                    && copiedClipboard.Contains(targetTraces[i], StringComparison.Ordinal),
                    "The native copy omitted full context or available trace for " + Names[i]);
            AssertTargetsUnchanged();
        });
        yield return new CheckpointStep("bounded text produced by native Copy all", _ => new Dictionary<string, string>
        {
            ["copiedRetainedOutput"] = copiedClipboard!,
            ["characters"] = copiedClipboard!.Length.ToString(CultureInfo.InvariantCulture),
            ["scope"] = "Native Copy copies all retained records, not only the selected record. Prior clipboard content is never retained."
        });
        yield return Shot("native log after successful Copy all");
        yield return new AssertionStep("conditionally restore the clipboard owned by this native action", _ => RestoreClipboardIfOwned());
        yield return new CheckpointStep("clipboard cleanup outcome", _ => new Dictionary<string, string> { ["outcome"] = clipboardCleanup });
        yield return new NativePointerScopeEndStep("restore the pointer after native log inspection", pointerScope);
        yield return new NativeDeveloperLogActionStep("restore the native developer log's prior open state", initialLog != null);
    }

    private IReadOnlyDictionary<string, string> BaselineEvidence()
    {
        var evidence = new Dictionary<string, string>
        {
            ["requestedXmlDeserializer"] = expectedLegacy ? "legacy" : "default",
            ["observedXmlDeserializer"] = observedLegacy ? "legacy" : "default",
            ["retainedCount"] = initialRecords.Length.ToString(CultureInfo.InvariantCulture),
            ["initialLogOpen"] = (initialLog != null).ToString(),
            ["initialAutoOpenPending"] = initialAutoOpenPending.ToString()
        };
        var matching = initialRecords.Where(record => record.text.Contains("MegamodDiagnosticsFixture", StringComparison.Ordinal)
            || record.text.StartsWith("Parsed 37.5 as int.", StringComparison.Ordinal)).Take(32).ToArray();
        for (int i = 0; i < matching.Length; i++)
        {
            EndToEndAssert.True(matching[i].text.Length <= ClipboardCharacterLimit, "Fixture diagnostic exceeds the evidence bound.");
            evidence["record-" + i] = matching[i].type + " x" + matching[i].repeats + ": " + matching[i].text;
        }
        return evidence;
    }

    private NativeWindowCaptureStep Capture(string name) => new(name, windows =>
    {
        window = windows.Single(item => item.RuntimeType == typeof(EditWindow_Log).FullName);
        var actual = Find.WindowStack.WindowOfType<EditWindow_Log>();
        EndToEndAssert.NotNull(actual);
        EndToEndAssert.True(initialLog == null || ReferenceEquals(initialLog, actual), "The previously open native log was replaced.");
        EndToEndAssert.True(ownedLog == null || ReferenceEquals(ownedLog, actual), "The captured native log instance changed.");
        ownedLog = actual;
        view.Bind(window, actual!);
    });

    private NativeWindowClickActionStep ToolbarClick(string name, string label) => view.ToolbarClick(name, label);
    private Rect MeasureRow(LogMessage target, out bool visible) => view.MeasureRow(target, out visible);
    private Vector2 PlanNativeTrackClick(Rect row) => view.PlanNativeTrackClick(row);
    private IReadOnlyDictionary<string, string> GeometryEvidence(Rect row, bool visible) => view.GeometryEvidence(row, visible);
    private NativeWindowClickActionStep Click(string name, Vector2 point) => view.Click(name, point);
    private float ContentWidth => view.ContentWidth;
    private float ContentHeight => view.ContentHeight;
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static LogMessage? Selected => NativeDiagnosticLogView.Selected;
    private static T Read<T>(string name) => NativeDiagnosticLogView.Read<T>(name);

    private void AssertSelected(int index)
    {
        EndToEndAssert.True(ReferenceEquals(Selected, targets[index]), "Native click selected a different retained message.");
        AssertTargetsUnchanged();
    }

    private void AssertTargetsUnchanged()
    {
        var retained = Log.Messages.ToArray();
        for (int i = 0; i < targets.Length; i++)
        {
            EndToEndAssert.True(retained.Any(message => ReferenceEquals(message, targets[i])), "A fixture record left retained history.");
            EndToEndAssert.Equal(targetTexts[i], targets[i].text);
            EndToEndAssert.Equal(targetTraces[i], targets[i].StackTrace);
            EndToEndAssert.Equal(i < 2 ? LogMessageType.Warning : LogMessageType.Error, targets[i].type);
            EndToEndAssert.Equal(1, targets[i].repeats);
        }
    }

    private static string ExpectedNativeCopy(IEnumerable<LogMessage> records)
    {
        // Native framing is used only to establish cleanup ownership. Actual copied fixture content is checked separately.
        var result = new StringBuilder();
        foreach (var message in records)
        {
            EndToEndAssert.True((long)result.Length + message.text.Length + message.StackTrace.Length + 8 <= ClipboardCharacterLimit,
                "Retained output exceeds the bounded native Copy scenario.");
            if (result.Length != 0) result.AppendLine();
            result.AppendLine(message.text).Append(message.StackTrace);
            if (result[result.Length - 1] != '\n') result.AppendLine();
        }
        return result.ToString();
    }

    private static string ReadBoundedClipboard()
    {
        var current = NativeDiagnosticClipboard.Observe(ClipboardCharacterLimit);
        EndToEndAssert.True(current.Supported, "Clipboard now contains unsupported formats; its private content is not retained.");
        return current.Text!;
    }

    private NativeWindowClickActionStep NativeCopyClick()
    {
        var action = ToolbarClick("copy all retained messages with the native Copy to clipboard button", "Copy to clipboard");
        // Recheck directly before yielding the native action, after geometry preparation.
        NativeDiagnosticClipboard.RequireUnchanged(priorClipboard!, ClipboardCharacterLimit);
        clipboardAttempted = true;
        return action;
    }

    private void RestoreClipboardIfOwned()
    {
        if (!clipboardAttempted) return;
        var current = NativeDiagnosticClipboard.Observe(ClipboardCharacterLimit);
        bool IsPrior(NativeDiagnosticClipboard.Snapshot value) => value.Supported &&
            value.Empty == priorClipboard!.Empty && string.Equals(value.Text, priorClipboard.Text, StringComparison.Ordinal);
        // A previous setter/readback failure keeps ownership pending until restoration is actually verified.
        if (clipboardRestoreAttempted && IsPrior(current))
        {
            clipboardCleanup = "verified prior clipboard restoration on deferred retry";
            clipboardAttempted = false;
            return;
        }
        if (!current.Supported || !string.Equals(current.Text, expectedClipboard, StringComparison.Ordinal))
        {
            if (clipboardRestoreAttempted)
                throw new InvalidOperationException("Clipboard restoration remains unverified after a changed value/format; preserve it and report incomplete cleanup.");
            clipboardCleanup = "preserved a subsequent clipboard edit or unsupported format";
            clipboardAttempted = false;
            return;
        }
        clipboardRestoreAttempted = true;
        if (priorClipboard!.Empty) NativeDiagnosticClipboard.RestoreEmptyIfUnchanged(current);
        else
        {
            NativeDiagnosticClipboard.RequireUnchanged(current, ClipboardCharacterLimit);
            GUIUtility.systemCopyBuffer = priorClipboard.Text!;
        }
        var restored = NativeDiagnosticClipboard.Observe(ClipboardCharacterLimit);
        EndToEndAssert.True(IsPrior(restored),
            "Clipboard restoration readback failed; cleanup remains incomplete and deferred restoration remains pending.");
        clipboardCleanup = "privately verified prior text/empty state after exact owned-value match";
        clipboardAttempted = false;
    }
}
