using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.ContentSelection.Runtime;

// This initial native page uses ordinary FloatMenus; the full S020 searchable windows remain open.
public sealed class IncidentContentSettingsUi
{
    private readonly IncidentContentRuntime runtime;
    private readonly SettingsRuntime settings;
    private readonly ContentChoiceRow[] rows;
    private readonly ContentBrowseSession browse;

    private IncidentContentSettingsUi(IncidentContentRuntime runtime, SettingsRuntime settings, ContentCatalog catalog)
    {
        if (!ReferenceEquals(runtime.Catalog, catalog)) throw new ArgumentException("The UI must use the runtime's exact finalized catalog.");
        this.runtime = runtime;
        this.settings = settings;
        rows = catalog.Entries.Where(entry => entry.Identity is IncidentDef).GroupBy(entry => entry.Key).Select(group =>
        {
            var definition = (IncidentDef)group.First().Identity;
            bool ambiguous = catalog.Resolve(group.Key).Status != ContentResolutionStatus.Present;
            return new ContentChoiceRow(runtime.Definitions, group.Key, definition,
                ambiguous ? group.Key.DefName : definition.LabelCap.ToString(),
                "Megamod_Content_IncidentKind".Translate(), definition.modContentPack?.Name ?? group.Key.SourcePackageId);
        }).ToArray();
        browse = new(settings.Store, rows);
    }

    public static IncidentContentSettingsUi Register(IncidentContentRuntime runtime, SettingsRuntime settings,
        SettingsRenderer renderer, ContentCatalog catalog)
    {
        var page = new IncidentContentSettingsUi(runtime, settings, catalog);
        renderer.AddSection("content.browser", "content", int.MinValue, "Megamod_Content_Category", 36,
            rect =>
            {
                if (Widgets.ButtonText(new Rect(rect.x, rect.y, rect.width, 30), "Megamod_Content_OpenBrowser".Translate()) &&
                    !Find.WindowStack.IsOpen<ContentSelectionWindow>())
                {
                    page.browse.Reset();
                    Find.WindowStack.Add(new ContentSelectionWindow(page.CreateBrowserRenderer()));
                }
            }, page.browse.Reset);
        renderer.AddBoolean(ContentSettings.Enabled, "Megamod_Content_Enabled", "Megamod_Content_Category",
            "Megamod_Content_EnabledHelp");
        return page;
    }

    private SettingsRenderer CreateBrowserRenderer()
    {
        var renderer = new SettingsRenderer(settings);
        renderer.AddSection("content.navigation", "content", int.MinValue, "Megamod_Content_Category", 164,
            DrawNavigation, browse.Reset);
        foreach (var row in rows)
        {
            renderer.AddDynamicBoolean(row.Definition,
                () => "Megamod_Content_AllowEntry".Translate(row.Label, row.Key.DefName), "Megamod_Content_Category",
                () => row.Label + "\n" + row.SourceLabel + " [" + row.Key.SourcePackageId + "]\n" +
                    row.Key.FullTypeName + " / " + row.Key.DefName + "\n" + "Megamod_Content_ChoiceHelp".Translate(),
                () => Status(row), () => browse.VisibleRanks().ContainsKey(row.Key),
                () => Unavailable(row), () => browse.VisibleRanks().TryGetValue(row.Key, out int rank) ? rank : int.MaxValue);
        }
        return renderer;
    }

    private string? Unavailable(ContentChoiceRow row)
    {
        if (runtime.DefinitionLifecycle.InvalidatedBy != null) return "Megamod_Content_DefinitionsInvalidated";
        if (!runtime.Bridge.Available || !runtime.DefinitionLifecycle.Available) return "Megamod_Content_PatchesUnavailable";
        if (runtime.Catalog.Resolve(row.Key).Status != ContentResolutionStatus.Present) return "Megamod_Content_Ambiguous";
        return IsCurrent(row) ? null : "Megamod_Content_Missing";
    }

    private static bool IsCurrent(ContentChoiceRow row) =>
        ReferenceEquals(DefDatabase<IncidentDef>.GetNamedSilentFail(row.Key.DefName), row.Identity);

    private string? Status(ContentChoiceRow row)
    {
        string? reason = Unavailable(row);
        // The shared renderer already draws the feature reason and stored-value error separately.
        if (reason != null) return null;
        if (runtime.CurrentAppliedPolicy is { Enabled: false })
            return "Megamod_Content_DisabledRetained".Translate();
        // A missing settings binding can mean unavailable lifecycle support in a loaded game.
        // Only an actually inactive game may display the main-menu next-load status.
        bool hasLoadedGame = Current.ProgramState == ProgramState.Playing && Current.Game != null;
        var state = row.ReadState(settings.Store, runtime.CurrentAppliedPolicy, hasLoadedGame);
        if (state.UnavailableReasonKey != null) return state.UnavailableReasonKey.Translate();
        if (state.ValueErrorKey != null) return null;
        return state.Status switch
        {
            ContentChoiceStatus.AppliedAllowed => "Megamod_Content_AppliedAllowed".Translate(),
            ContentChoiceStatus.AppliedExcluded => "Megamod_Content_AppliedExcluded".Translate(),
            ContentChoiceStatus.PendingAllow => "Megamod_Content_PendingAllow".Translate(),
            ContentChoiceStatus.PendingExclude => "Megamod_Content_PendingExclude".Translate(),
            ContentChoiceStatus.NextLoad => "Megamod_Content_NextLoad".Translate(),
            _ => "Megamod_Content_ApplicationUnavailable".Translate()
        };
    }

    private void DrawNavigation(Rect rect)
    {
        int count = browse.VisibleRanks().Count;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 44), "Megamod_Content_IncidentScope".Translate());
        float half = Math.Max(0, (rect.width - 8) / 2);
        var typeRect = new Rect(rect.x, rect.y + 48, half, 28);
        if (Widgets.ButtonText(typeRect,
            browse.TypeName == null ? "Megamod_Content_AllTypes".Translate() : "Megamod_Content_IncidentKind".Translate()))
            OpenTypes();
        if (browse.TypeName != null) TooltipHandler.TipRegion(typeRect, browse.TypeName);
        string source = browse.SourcePackageId == null ? "Megamod_Content_AllSources".Translate() :
            rows.First(row => row.Key.SourcePackageId == browse.SourcePackageId).SourceLabel + " [" + browse.SourcePackageId + "]";
        var sourceRect = new Rect(rect.x + half + 8, rect.y + 48, half, 28);
        if (Widgets.ButtonText(sourceRect, source)) OpenSources();
        TooltipHandler.TipRegion(sourceRect, source);
        Widgets.Label(new Rect(rect.x, rect.y + 80, rect.width, 24), "Megamod_Content_Search".Translate());
        browse.SetQuery(Widgets.TextField(new Rect(rect.x, rect.y + 106, rect.width, 28), browse.Query));
        Widgets.Label(new Rect(rect.x, rect.y + 138, rect.width, 24),
            count == 0 ? "Megamod_Content_NoMatches".Translate() : "Megamod_Content_Matching".Translate(count, rows.Length));
    }

    private void OpenTypes()
    {
        var options = new List<FloatMenuOption>
        {
            new("Megamod_Content_AllTypes".Translate(), () => browse.SelectType(null))
        };
        foreach (string type in rows.Select(row => row.Key.FullTypeName).Distinct().OrderBy(value => value, StringComparer.Ordinal))
            options.Add(new FloatMenuOption("Megamod_Content_IncidentKind".Translate() + " [" + type + "]", () =>
            {
                if (rows.Any(row => row.Key.FullTypeName == type && IsCurrent(row))) browse.SelectType(type);
                else StaleChoice();
            }));
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenSources()
    {
        var options = new List<FloatMenuOption>
        {
            new("Megamod_Content_AllSources".Translate(), () => browse.SelectSource(null))
        };
        foreach (var source in rows.GroupBy(row => row.Key.SourcePackageId)
            .OrderBy(group => group.First().SourceLabel, StringComparer.OrdinalIgnoreCase).ThenBy(group => group.Key, StringComparer.Ordinal))
        {
            string identity = source.Key;
            options.Add(new FloatMenuOption(source.First().SourceLabel + " [" + identity + "]", () =>
            {
                if (rows.Any(row => row.Key.SourcePackageId == identity && IsCurrent(row))) browse.SelectSource(identity);
                else StaleChoice();
            }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void StaleChoice() => Messages.Message("Megamod_Content_StaleChoice".Translate(), MessageTypeDefOf.RejectInput);
}
