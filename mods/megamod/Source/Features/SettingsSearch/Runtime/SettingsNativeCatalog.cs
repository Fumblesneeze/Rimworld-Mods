using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// Owns process-local page descriptors and their exact native objects, never foreign option values.
// The caller supplies a provider-completed Options instance and the normal reader GUI boundary.
public sealed class SettingsNativeCatalog
{
    private const int MaximumPages = 4096;
    private const int MaximumTextUnits = 524288;
    private sealed record Request(Dialog_Options Source, string Identity, bool Startup = false);
    private sealed record Entry(SettingsPage Page, Mod Native);
    private readonly int thread = Environment.CurrentManagedThreadId;
    private Dictionary<SettingsPage, Mod> native = new(ReferenceEqualityComparer.Instance);
    private Request? pending;
    private long generation;
    public IReadOnlyList<SettingsPage> Pages { get; private set; } = Array.Empty<SettingsPage>();
    public bool Pending => pending != null;

    public void Refresh(Dialog_Options completedOptions)
    {
        ArgumentNullException.ThrowIfNull(completedOptions);
        Clear();
        pending = new(completedOptions, Guid.NewGuid().ToString("N"));
    }

    public void RefreshStartup()
    {
        Clear();
        // Our exact native carrier is constructed before isolation. Foreign window construction
        // during PostOpen/enumeration remains refused by the reader's constructor guard.
        long currentGeneration = generation;
        var source = new Dialog_Options { soundAppear = null, soundAmbient = null };
        if (generation == currentGeneration)
            pending = new(source, Guid.NewGuid().ToString("N"), true);
    }

    public SettingsNeutralReadStatus ReadPending(SettingsNeutralReader reader, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(reader);
        RequireThread();
        var request = pending;
        reason = null;
        if (request == null)
        {
            reason = "settings-catalog-no-request";
            return SettingsNeutralReadStatus.Unavailable;
        }
        Entry[]? candidate = null;
        var observation = new SettingsPage(request.Identity, request.Identity, null, null, null);
        var status = reader.Read(observation, "catalog", () => candidate = Materialize(request), out _, out reason);
        // Enumeration, metadata or a diagnostic callback can refresh/cancel synchronously. Never
        // publish the old request or clear a replacement which now owns the pending slot.
        if (!ReferenceEquals(pending, request))
        {
            reason = "settings-catalog-request-replaced";
            return SettingsNeutralReadStatus.Failed;
        }
        if (status == SettingsNeutralReadStatus.Deferred) return status;
        pending = null;
        if (status != SettingsNeutralReadStatus.Captured) return status;
        if (candidate == null)
        {
            reason = "settings-catalog-no-capture";
            return SettingsNeutralReadStatus.Failed;
        }
        var map = new Dictionary<SettingsPage, Mod>(ReferenceEqualityComparer.Instance);
        foreach (var entry in candidate) map.Add(entry.Page, entry.Native);
        var descriptors = Array.AsReadOnly(candidate.Select(entry => entry.Page).ToArray());
        native = map;
        Pages = descriptors;
        return SettingsNeutralReadStatus.Captured;
    }

    public bool TryResolve(SettingsPage page, out Mod? mod)
    {
        ArgumentNullException.ThrowIfNull(page);
        RequireThread();
        return native.TryGetValue(page, out mod);
    }

    public void Clear()
    {
        RequireThread();
        generation++;
        pending = null;
        Pages = Array.Empty<SettingsPage>();
        native.Clear();
    }

    private static Entry[] Materialize(Request request)
    {
        var source = request.Source;
        if (request.Startup) source.PostOpen();
        var field = typeof(Dialog_Options).GetField("cachedModsWithSettings",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (field == null || !typeof(IEnumerable<Mod>).IsAssignableFrom(field.FieldType))
            throw Refuse("settings-catalog-native-collection-unavailable");
        if (field.GetValue(source) is not IEnumerable<Mod> collection)
            throw Refuse("settings-catalog-uninitialized-collection");
        var entries = new List<Entry>();
        int textUnits = 0;
        // Deferred enumeration, metadata and Dispose all finish inside the neutral reader. A
        // failure in any of them leaves the candidate private and prevents publication.
        using (var iterator = collection.GetEnumerator())
        {
            if (iterator == null) throw Refuse("settings-catalog-missing-enumerator");
            while (iterator.MoveNext())
            {
                if (entries.Count >= MaximumPages) throw Refuse("settings-catalog-page-limit");
                var mod = iterator.Current ?? throw Refuse("settings-catalog-null-page");
                var content = mod.Content;
                var title = mod.SettingsCategory();
                var name = content?.Name;
                var author = content?.ModMetaData?.AuthorsString;
                var package = content?.PackageId;
                foreach (string? text in new[] { title, name, author, package })
                {
                    int units = text?.Length ?? 0;
                    if (units > MaximumTextUnits - textUnits) throw Refuse("settings-catalog-text-limit");
                    textUnits += units;
                }
                string identity = request.Identity + ":" + entries.Count.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
                entries.Add(new(new(identity, identity, title, name, author, package), mod));
            }
        }
        if (!ReferenceEquals(field.GetValue(source), collection)) throw Refuse("settings-catalog-source-replaced");
        return entries.ToArray();
    }

    private static InvalidOperationException Refuse(string reason) =>
        SettingsReadScope.Current?.Refuse(reason) ?? new InvalidOperationException(reason);

    private void RequireThread()
    {
        if (Environment.CurrentManagedThreadId != thread || !UnityData.IsInMainThread)
            throw new InvalidOperationException("settings-catalog-gui-thread-required");
    }
}
