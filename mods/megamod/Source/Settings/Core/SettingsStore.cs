using System;
using System.Collections.Generic;

namespace Megamod.Settings;

public sealed class SettingsStore
{
    private readonly SettingsEffectiveOff? effectiveOff;

    public SettingsStore() : this(null) { }
    public SettingsStore(SettingsEffectiveOff? effectiveOff) => this.effectiveOff = effectiveOff;

    // Serialized startup only before publication; all later access belongs to the game/UI thread.
    // Worker consumers use their immutable derived snapshot, never this mutable store.
    public event Action? Changed;

    private readonly Dictionary<string, object> definitions = new(StringComparer.Ordinal);
    private Dictionary<string, string> globals = new(StringComparer.Ordinal);
    private Dictionary<string, SaveSettingValue> saveValues = new(StringComparer.Ordinal);
    private SaveSettingsBinding? currentBinding;
    private long activationGeneration;
    private long globalRevision;
    private long saveRevision;

    public void Register<T>(SettingDefinition<T> definition) => definitions.Add(definition.Id, definition);

    public SettingEditResult SetGlobal<T>(SettingDefinition<T> definition, T value)
    {
        RequireRegistered(definition);
        string? error = definition.Validation?.Invoke(value);
        if (error != null)
            return new(false, error);
        string encoded = definition.Codec.Encode(value);
        if (!globals.TryGetValue(definition.Id, out string? previous) || previous != encoded)
        {
            globals[definition.Id] = encoded;
            globalRevision++;
            Changed?.Invoke();
        }
        return new(true);
    }

    public T GetEffective<T>(SettingDefinition<T> definition) => ReadEffective(definition).Value;

    public SettingReadResult<T> ReadEffective<T>(SettingDefinition<T> definition)
    {
        var requested = ReadCurrentRequested(definition);
        return effectiveOff?.Apply(definition, requested) ?? requested;
    }

    public SettingReadResult<T> ReadCurrentRequested<T>(SettingDefinition<T> definition)
    {
        RequireRegistered(definition);
        if (currentBinding is { IsRevoked: false } && definition.Scope == SettingScope.GlobalAndSave &&
            saveValues.TryGetValue(definition.Id, out SaveSettingValue? saved) && !saved.UseGlobal)
            return ReadStored(definition, saved.Override);
        return ReadGlobal(definition);
    }

    public string? EffectiveOffReason<T>(SettingDefinition<T> definition)
    {
        RequireRegistered(definition);
        return effectiveOff?.ReasonFor(definition);
    }

    public T GetGlobal<T>(SettingDefinition<T> definition) => ReadGlobal(definition).Value;
    public SettingReadResult<T> ReadRequested<T>(SettingDefinition<T> definition, SaveSettingsSnapshot snapshot)
    {
        RequireRegistered(definition);
        if (definition.Scope == SettingScope.GlobalAndSave && snapshot.Values.TryGetValue(definition.Id, out SaveSettingValue? saved)
            && !saved.UseGlobal)
            return ReadStored(definition, saved.Override);
        return ReadGlobal(definition);
    }

    public SettingReadResult<T> ReadGlobal<T>(SettingDefinition<T> definition)
    {
        RequireRegistered(definition);
        return globals.TryGetValue(definition.Id, out string? encoded)
            ? ReadStored(definition, encoded) : new(definition.BuiltInDefault);
    }

    private static SettingReadResult<T> ReadStored<T>(SettingDefinition<T> definition, string? encoded)
        => SettingsSnapshotReader.ReadStored(definition, encoded);

    public string? CurrentSaveIdentity => currentBinding is { IsRevoked: false } ? currentBinding.SaveIdentity : null;
    public bool UsesGlobal<T>(SettingDefinition<T> definition)
    {
        RequireRegistered(definition);
        return currentBinding == null || currentBinding.IsRevoked || definition.Scope == SettingScope.GlobalOnly ||
            !saveValues.TryGetValue(definition.Id, out SaveSettingValue? value) || value.UseGlobal;
    }
    public SettingsRevision Revision => new(globalRevision, CurrentSaveIdentity, activationGeneration, saveRevision);
    public GlobalSettingsSnapshot ExportGlobals() => new(globals);
    public SaveSettingsSnapshot ExportSave(SaveSettingsBinding binding)
    {
        if (!ReferenceEquals(binding, currentBinding) || currentBinding == null)
            throw new InvalidOperationException("The save binding is no longer active.");
        return new(binding.SaveIdentity, saveValues);
    }
    public void LoadGlobals(GlobalSettingsSnapshot snapshot)
    {
        globals = new(snapshot.Values, StringComparer.Ordinal);
        globalRevision++;
        Changed?.Invoke();
    }
    public SaveSettingsBinding ActivateSave(SaveSettingsSnapshot snapshot)
    {
        currentBinding?.Revoke();
        saveValues = new(snapshot.Values, StringComparer.Ordinal);
        currentBinding = new(snapshot.SaveIdentity, ++activationGeneration);
        saveRevision = 0;
        Changed?.Invoke();
        return currentBinding;
    }

    public void DeactivateSave()
    {
        currentBinding?.Revoke();
        currentBinding = null;
        saveValues.Clear();
        activationGeneration++;
        saveRevision = 0;
        Changed?.Invoke();
    }

    public SettingEditResult SetUseGlobal<T>(SaveSettingsBinding binding, SettingDefinition<T> definition, bool useGlobal)
    {
        RequireRegistered(definition);
        if (!ReferenceEquals(binding, currentBinding) || currentBinding == null || binding.IsRevoked)
            return new(false, "Megamod_Settings_NoActiveSave");
        if (definition.Scope != SettingScope.GlobalAndSave)
            return new(false, "Megamod_Settings_GlobalOnly");
        saveValues.TryGetValue(definition.Id, out SaveSettingValue? previous);
        string? dormant = previous?.Override;
        if (!useGlobal && dormant == null)
            dormant = definition.Codec.Encode(GetGlobal(definition));
        var next = new SaveSettingValue(useGlobal, dormant);
        if (previous != next)
        {
            saveValues[definition.Id] = next;
            saveRevision++;
            Changed?.Invoke();
        }
        return new(true);
    }

    public SettingEditResult SetSaveOverride<T>(SaveSettingsBinding binding, SettingDefinition<T> definition, T value)
    {
        RequireRegistered(definition);
        if (!ReferenceEquals(binding, currentBinding) || currentBinding == null || binding.IsRevoked)
            return new(false, "Megamod_Settings_NoActiveSave");
        if (definition.Scope != SettingScope.GlobalAndSave)
            return new(false, "Megamod_Settings_GlobalOnly");
        if (!saveValues.TryGetValue(definition.Id, out SaveSettingValue? previous) || previous.UseGlobal)
            return new(false, "Megamod_Settings_Inheriting");
        string? error = definition.Validation?.Invoke(value);
        if (error != null)
            return new(false, error);
        var next = new SaveSettingValue(false, definition.Codec.Encode(value));
        if (previous != next)
        {
            saveValues[definition.Id] = next;
            saveRevision++;
            Changed?.Invoke();
        }
        return new(true);
    }

    private void RequireRegistered<T>(SettingDefinition<T> definition)
    {
        if (!definitions.TryGetValue(definition.Id, out object? registered) || !ReferenceEquals(registered, definition))
            throw new InvalidOperationException("Use the registered definition for this setting.");
    }
}
