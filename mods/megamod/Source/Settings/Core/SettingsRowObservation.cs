using System;

namespace Megamod.Settings;

public sealed record SettingsRowValue(string? Text, string? ErrorKey = null);

// Detached current displays from one registered row, never another settings store.
// Effective is the common policy projection, not proof that a delayed feature has applied it.
public sealed record SettingsRowObservation(
    string Id, string CategoryId, string Label, string Help, string Category,
    string? Status, bool Visible, SettingScope Scope, SettingApplicationTiming ApplicationTiming, SettingsRevision Revision,
    SettingsRowValue GlobalRequested, SettingsRowValue? CurrentSaveRequested,
    SettingsRowValue Effective, bool? UsesGlobal,
    string? UnavailableReason, string? EffectiveOffReason)
{
    public static SettingsRowObservation Capture<T>(SettingsStore store, SettingDefinition<T> definition,
        string label, string help, string category, Func<T, string?>? formatValue,
        bool visible = true, string? status = null, string? unavailableReason = null)
    {
        var revision = store.Revision;
        bool hasCurrentSave = definition.Scope == SettingScope.GlobalAndSave && revision.SaveIdentity != null;
        var global = store.ReadGlobal(definition);
        SettingReadResult<T>? current = hasCurrentSave ? store.ReadCurrentRequested(definition) : null;
        var effective = store.ReadEffective(definition);
        bool? usesGlobal = hasCurrentSave ? store.UsesGlobal(definition) : null;
        string? effectiveOffReason = store.EffectiveOffReason(definition);
        SettingsRowValue Display(SettingReadResult<T> value) => new(formatValue?.Invoke(value.Value), value.ErrorKey);
        var globalDisplay = Display(global);
        var currentDisplay = current.HasValue ? Display(current.Value) : null;
        var effectiveDisplay = Display(effective);
        if (store.Revision != revision)
            throw new InvalidOperationException("Settings changed during row observation.");
        return new(definition.Id, definition.CategoryId, label, help, category, status, visible,
            definition.Scope, definition.ApplicationTiming, revision, globalDisplay, currentDisplay, effectiveDisplay,
            usesGlobal, unavailableReason, effectiveOffReason);
    }
}
