using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

// Owns definitions only. All persisted values and inheritance belong to the common store.
public sealed class ContentSettings
{
    public static readonly SettingDefinition<bool> Enabled = new("content.enabled", true,
        SettingScope.GlobalAndSave, "content", int.MinValue, SettingApplicationTiming.NewGameOrLoad,
        SettingCodecs.Boolean);
    private readonly SettingsStore store;
    private readonly Dictionary<ContentKey, SettingDefinition<bool>> individual = new();
    private readonly Dictionary<ContentKey, SettingDefinition<bool>> presets = new();

    public ContentSettings(SettingsStore store)
    {
        this.store = store;
        store.Register(Enabled);
    }

    public ContentLoadInputs PrepareLoadInputs(ContentCatalog catalog, GlobalSettingsReadSnapshot globals,
        IEnumerable<ContentKey>? presetKeys = null)
    {
        var individualDefinitions = new Dictionary<ContentKey, SettingDefinition<bool>>();
        foreach (var entry in catalog.Entries) individualDefinitions[entry.Key] = Individual(entry.Key);
        var presetDefinitions = new Dictionary<ContentKey, SettingDefinition<bool>>();
        if (presetKeys != null)
            foreach (var key in presetKeys) presetDefinitions[key] = Preset(key);
        return new ContentLoadInputs(catalog, globals, individualDefinitions, presetDefinitions);
    }

    public SettingDefinition<bool> Preset(ContentKey key) => Definition(presets, key, "content.preset-active/", false);

    public RequestedContentSnapshot CaptureRequested(ContentCatalog catalog, SaveSettingsSnapshot? save,
        IEnumerable<ContentKey>? presetKeys = null)
    {
        var values = new Dictionary<ContentKey, SettingReadResult<bool>>();
        foreach (var entry in catalog.Entries)
        {
            var definition = Individual(entry.Key);
            values[entry.Key] = save == null ? store.ReadGlobal(definition) : store.ReadRequested(definition, save);
        }
        var presetValues = new Dictionary<ContentKey, SettingReadResult<bool>>();
        var requestedPresets = new HashSet<ContentKey>(presets.Keys);
        if (presetKeys != null) requestedPresets.UnionWith(presetKeys);
        foreach (string id in store.ExportGlobals().Values.Keys.Concat(save?.Values.Keys ?? Array.Empty<string>()))
            if (id.StartsWith("content.preset-active/", StringComparison.Ordinal) &&
                ContentKey.TryParse(id.Substring("content.preset-active/".Length), out var key))
                requestedPresets.Add(key!);
        foreach (var key in requestedPresets)
        {
            var definition = Preset(key);
            presetValues[key] = save == null ? store.ReadGlobal(definition) : store.ReadRequested(definition, save);
        }
        var enabled = save == null ? store.ReadGlobal(Enabled) : store.ReadRequested(Enabled, save);
        if (store.EffectiveOffReason(Enabled) != null) enabled = new(false);
        return new RequestedContentSnapshot(catalog, save, store.Revision, values, presetValues, enabled);
    }

    public SettingDefinition<bool> Individual(ContentKey key) => Definition(individual, key, "content.allowed/", true);

    private SettingDefinition<bool> Definition(Dictionary<ContentKey, SettingDefinition<bool>> definitions,
        ContentKey key, string prefix, bool builtInDefault)
    {
        if (definitions.TryGetValue(key, out var definition)) return definition;
        definition = new SettingDefinition<bool>(prefix + key.ToStorageToken(), builtInDefault,
            SettingScope.GlobalAndSave, "content", 0, SettingApplicationTiming.NewGameOrLoad,
            SettingCodecs.Boolean);
        store.Register(definition);
        definitions.Add(key, definition);
        return definition;
    }
}

// No mutable store or owner cache is retained. C03 definitions are Boolean with no validator.
public sealed class ContentLoadInputs
{
    private readonly IReadOnlyDictionary<ContentKey, SettingDefinition<bool>> individual;
    private readonly IReadOnlyDictionary<ContentKey, SettingDefinition<bool>> presets;

    internal ContentLoadInputs(ContentCatalog catalog, GlobalSettingsReadSnapshot globals,
        IDictionary<ContentKey, SettingDefinition<bool>> individual, IDictionary<ContentKey, SettingDefinition<bool>> presets)
    {
        Catalog = catalog;
        Globals = globals;
        this.individual = new ReadOnlyDictionary<ContentKey, SettingDefinition<bool>>(new Dictionary<ContentKey, SettingDefinition<bool>>(individual));
        this.presets = new ReadOnlyDictionary<ContentKey, SettingDefinition<bool>>(new Dictionary<ContentKey, SettingDefinition<bool>>(presets));
    }

    public ContentCatalog Catalog { get; }
    public GlobalSettingsReadSnapshot Globals { get; }
    public SettingReadResult<bool> ReadIndividual(ContentKey key, SaveSettingsSnapshot? save = null) =>
        SettingsSnapshotReader.ReadRequested(individual[key], Globals, save);
    public SettingReadResult<bool> ReadPreset(ContentKey key, SaveSettingsSnapshot? save = null) =>
        SettingsSnapshotReader.ReadRequested(presets[key], Globals, save);
}

public sealed class RequestedContentSnapshot
{
    internal RequestedContentSnapshot(ContentCatalog catalog, SaveSettingsSnapshot? save,
        SettingsRevision revision, IDictionary<ContentKey, SettingReadResult<bool>> individual,
        IDictionary<ContentKey, SettingReadResult<bool>> presets, SettingReadResult<bool> enabled)
    {
        Catalog = catalog;
        Save = save;
        Revision = revision;
        Enabled = enabled;
        Individual = new ReadOnlyDictionary<ContentKey, SettingReadResult<bool>>(
            new Dictionary<ContentKey, SettingReadResult<bool>>(individual));
        Presets = new ReadOnlyDictionary<ContentKey, SettingReadResult<bool>>(
            new Dictionary<ContentKey, SettingReadResult<bool>>(presets));
    }

    public ContentCatalog Catalog { get; }
    public SaveSettingsSnapshot? Save { get; }
    public SettingsRevision Revision { get; }
    public SettingReadResult<bool> Enabled { get; }
    public IReadOnlyDictionary<ContentKey, SettingReadResult<bool>> Individual { get; }
    public IReadOnlyDictionary<ContentKey, SettingReadResult<bool>> Presets { get; }
}
