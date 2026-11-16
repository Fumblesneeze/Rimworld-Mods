using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

public sealed partial class SettingsSearchController
{
    private void DrawControls(DialogState state, Listing_Standard listing, bool navigation, bool search)
    {
        var font = Text.Font; var anchor = Text.Anchor; bool wrap = Text.WordWrap; var color = GUI.color;
        try
        {
            Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleLeft; Text.WordWrap = false;
            if (navigation)
            {
                var row = listing.GetRect(28f);
                var left = new Rect(row.x, row.y, row.width / 2f - 4f, row.height);
                RememberControl("ByAuthor", left);
                Widgets.CheckboxLabeled(left, T("ByAuthor"), ref byAuthor);
                if (byAuthor)
                {
                    var right = new Rect(row.x + row.width / 2f + 4f, row.y, row.width / 2f - 4f, row.height);
                    RememberControl("GroupSingles", right);
                    Widgets.CheckboxLabeled(right, T("GroupSingles"), ref groupSingles);
                }
                listing.Gap(4f);
            }
            if (!search) return;
            var status = listing.GetRect(26f);
            float buttonWidth = Math.Min(86f, status.width / 4f);
            var refresh = new Rect(status.xMax - buttonWidth * 2f - 4f, status.y, buttonWidth, status.height);
            var cancel = new Rect(status.xMax - buttonWidth, status.y, buttonWidth, status.height);
            int completed = Pages.Count(page => page.Status is SettingsDiscoveryStatus.Partial or SettingsDiscoveryStatus.Failed);
            string progress = discovery!.CatalogFailure is { } catalogFailure ? T("CatalogFailed") + " " + ReasonText(SettingsDiscoveryStop.None, catalogFailure) :
                Cancelled ? T("StatusCancelled") : Discovering ? T("Discovering", completed, Pages.Count) :
                Pages.Count == 0 ? T("Ready", 0) : T("Incomplete", Pages.Count);
            var progressRect = new Rect(status.x, status.y, Math.Max(0, refresh.x - status.x - 6f), status.height);
            Widgets.Label(progressRect, progress.Truncate(progressRect.width));
            RememberControl("Refresh", refresh); RememberControl("Cancel", cancel);
            if (Widgets.ButtonText(refresh, T("Refresh")))
            {
                CancelReveal(); discovery!.Refresh(state.Dialog); state.Refresh.MarkRequested();
                state.Generation = -1; observed.Clear(); observedCoverage.Clear(); observedRevision++;
            }
            if (Widgets.ButtonText(cancel, T("Cancel"))) { discovery!.Cancel(); CancelReveal(); }
            string counts = string.Join("\n", Pages.GroupBy(page => page.Status).OrderBy(group => group.Key)
                .Select(group => T("Status" + group.Key) + ": " + group.Count()));
            TooltipHandler.TipRegion(status, string.Join("\n", new[] { progress, counts, T("QueryHelp") }.Where(text => text.Length != 0)));
            listing.Gap(6f);
        }
        finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
    }

    private bool DrawRow(object context, object current)
    {
        var frame = (Frame)context;
        if (discovery == null || frame.Generation != discovery.Generation || frame.Index < 0 || frame.Index >= frame.Rows.Length) return false;
        var row = frame.Rows[frame.Index];
        if (!ReferenceEquals(row.Native.Native, current) || !discovery.TryResolve(row.Native.Page, out var native) || !ReferenceEquals(native, current)) return false;
        var font = Text.Font; var anchor = Text.Anchor; bool wrap = Text.WordWrap; var color = GUI.color;
        try
        {
            Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
            if (row.StartGroup)
            {
                frame.Column = 0;
                if (row.Heading != null)
                {
                    float height = Math.Max(24f, Text.CalcHeight(row.Heading, frame.Listing.ColumnWidth));
                    var heading = frame.Listing.GetRect(height);
                    Widgets.Label(heading, row.Heading);
                    frame.Listing.Gap(4f);
                }
            }
            Rect card;
            if (frame.FullWidth)
                card = frame.Listing.GetRect(40f).ContractedBy(2f);
            else
            {
                if (frame.Column == 0) frame.Pair = frame.Listing.GetRect(40f);
                card = (frame.Column == 0 ? frame.Pair.LeftHalf() : frame.Pair.RightHalf()).ContractedBy(2f);
                frame.Column = 1 - frame.Column;
            }
            bool selected = false;
            OptionRoute? selectedOption = null;
            if (Visible(frame, card))
            {
                Widgets.DrawOptionBackground(card, false);
                selected = Widgets.ButtonInvisible(card);
                DrawHeader(frame.State, row, card);
                RememberRow(row.Native.Page, null, card);
            }
            if (frame.FullWidth)
            {
                foreach (var option in row.Options)
                {
                    float width = Math.Max(1f, frame.Listing.ColumnWidth - 24f);
                    string label = string.IsNullOrEmpty(option.Entry.Label) ? option.Entry.Help ?? row.Row.Title : option.Entry.Label;
                    string value = option.OwnedId != null ? option.Entry.DisplayedValue ?? T("UnknownValue") :
                        T("LastObserved", option.Entry.DisplayedValue ?? T("UnknownValue"));
                    string detail = string.IsNullOrEmpty(option.Entry.Tab) ? value : option.Entry.Tab + " · " + value;
                    Text.Font = GameFont.Small;
                    float labelHeight = Math.Max(24f, Text.CalcHeight(label, width - 8f));
                    Text.Font = GameFont.Tiny;
                    float detailHeight = Math.Max(16f, Text.CalcHeight(detail, width - 8f));
                    var rect = frame.Listing.GetRect(labelHeight + detailHeight + 6f);
                    rect.xMin += 24f;
                    if (Visible(frame, rect))
                    {
                        Widgets.DrawOptionBackground(rect, false);
                        if (Widgets.ButtonInvisible(rect)) { selected = true; selectedOption = option; }
                        Text.Font = GameFont.Small;
                        Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, width - 8f, labelHeight), label);
                        Text.Font = GameFont.Tiny;
                        GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                        Widgets.Label(new Rect(rect.x + 4f, rect.y + labelHeight + 2f, width - 8f, detailHeight), detail);
                        GUI.color = color;
                        if (!string.IsNullOrEmpty(option.Entry.Help)) TooltipHandler.TipRegion(rect, option.Entry.Help);
                        RememberRow(row.Native.Page, option.Entry, rect);
                    }
                }
                frame.Listing.Gap(6f);
            }
            if (!selected) return false;
            if (frame.Generation != discovery.Generation || !discovery.TryResolve(row.Native.Page, out native) || !ReferenceEquals(native, current)) return false;
            // This is a real native ButtonInvisible result. The transpiler consumes it through
            // the original conditional and original constructor/provider-factory/Add block.
            CancelReveal();
            frame.Opening = new(row.Native, selectedOption, frame.Generation, settings.Store.Revision.Activation);
            return true;
        }
        catch (Exception error) { Warn("Settings row drawing failed: " + error.GetType().Name); return false; }
        finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
    }

    private void DrawHeader(DialogState state, PageRow row, Rect card)
    {
        if (!state.Icons.TryGetValue(row.Native.Page, out var icon))
        {
            try { icon = row.Native.Native.Content?.ModMetaData?.Icon; } catch { icon = null; }
            state.Icons[row.Native.Page] = icon;
        }
        if (!icon.NullOrBad()) GUI.DrawTexture(new Rect(card.x + 2f, card.y + 2f, 32f, 32f), icon);
        float x = card.x + 38f, width = Math.Max(1f, card.width - 42f);
        Text.WordWrap = false; Text.Anchor = TextAnchor.MiddleLeft; Text.Font = GameFont.Small;
        var page = Pages.FirstOrDefault(page => ReferenceEquals(page.Page, row.Native.Page));
        string coverage = state.Search ? CoverageText(page) : "";
        string subtitle = string.Join(" · ", new[] { row.Row.ShowAuthor ? row.Row.NormalizedAuthor : "", coverage }.Where(text => text.Length != 0));
        Widgets.Label(new Rect(x, card.y, width, subtitle.Length == 0 ? card.height : 22f), row.Row.Title.Truncate(width));
        if (subtitle.Length != 0)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, card.y + 21f, width, 15f), subtitle.Truncate(width));
        }
        Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
        string identity = string.Join("\n", new[] { row.Row.Title, row.Native.Page.ModName, row.Native.Page.RawAuthors,
            row.Native.Page.PackageIdentity, row.Native.Page.ProviderPageIdentity }.Where(text => !string.IsNullOrEmpty(text)).Distinct());
        // Source ordinal distinguishes otherwise-identical occurrences without inventing an owner.
        if (state.Rows.Count(other => other.Row.Title == row.Row.Title && other.Native.Page.PackageIdentity == row.Native.Page.PackageIdentity) > 1)
            identity += "\n#" + (row.Native.SourceOrdinal + 1);
        if (coverage.Length != 0) identity += "\n" + coverage;
        TooltipHandler.TipRegion(card, identity);
    }

    private string CoverageText(SettingsDiscoveryPage? page)
    {
        if (page == null) return T("StatusPending");
        string status = T("Status" + page.Status);
        string reason = ReasonText(page.StopReason, observedCoverage.TryGetValue(page.Page, out var observedReason)
            ? observedReason : discovery?.FailureReason(page.Page));
        return reason.Length == 0 ? status : status + " · " + reason;
    }
    private static string ReasonText(SettingsDiscoveryStop stop, string? failure)
    {
        string? key = SettingsChooserObservations.ReasonKey(stop, failure);
        return key == null ? "" : T(key);
    }

    private bool Visible(Frame frame, Rect rect)
    {
        var scroll = scrollField?.GetValue(frame.State.Dialog) is Vector2 current ? current : Vector2.zero;
        return rect.yMax >= scroll.y - 40f && rect.y <= scroll.y + frame.State.Dialog.windowRect.height + 40f;
    }
    private void RememberControl(string name, Rect rect)
    { if (Event.current?.type == EventType.Repaint) controlRects[name] = ScreenRect(rect); }
    private void RememberRow(SettingsPage page, SettingsSearchEntry? entry, Rect rect)
    { if (Event.current?.type == EventType.Repaint) drawnRows.Add(new(page, entry, ScreenRect(rect))); }
    private static Rect ScreenRect(Rect rect)
    {
        var first = GuiBackend.Current.GuiToScreenPoint(new Vector2(rect.xMin, rect.yMin));
        var last = GuiBackend.Current.GuiToScreenPoint(new Vector2(rect.xMax, rect.yMax));
        return Rect.MinMaxRect(first.x, first.y, last.x, last.y);
    }
}
