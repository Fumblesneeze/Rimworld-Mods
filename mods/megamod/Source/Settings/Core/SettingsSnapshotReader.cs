namespace Megamod.Settings;

public sealed record GlobalSettingsReadSnapshot(GlobalSettingsSnapshot Values, long GlobalRevision);

// Worker use requires independently thread-safe codecs/validation (C03 uses Boolean with no validator).
public static class SettingsSnapshotReader
{
    public static SettingReadResult<T> ReadEffective<T>(SettingDefinition<T> definition,
        GlobalSettingsReadSnapshot globals, SettingsEffectiveOff effectiveOff, SaveSettingsSnapshot? save = null)
        => effectiveOff.Apply(definition, ReadRequested(definition, globals, save));

    public static SettingReadResult<T> ReadRequested<T>(SettingDefinition<T> definition,
        GlobalSettingsReadSnapshot globals, SaveSettingsSnapshot? save = null)
    {
        if (definition.Scope == SettingScope.GlobalAndSave && save != null &&
            save.Values.TryGetValue(definition.Id, out var saved) && !saved.UseGlobal)
            return ReadStored(definition, saved.Override);
        return globals.Values.Values.TryGetValue(definition.Id, out var encoded)
            ? ReadStored(definition, encoded) : new(definition.BuiltInDefault);
    }

    internal static SettingReadResult<T> ReadStored<T>(SettingDefinition<T> definition, string? encoded)
    {
        if (encoded == null) return new(definition.BuiltInDefault, "Megamod_Settings_InvalidStoredValue");
        try
        {
            if (!definition.Codec.TryDecode(encoded, out T value))
                return new(definition.BuiltInDefault, "Megamod_Settings_InvalidStoredValue");
            string? error = definition.Validation?.Invoke(value);
            return error == null ? new(value) : new(definition.BuiltInDefault, error);
        }
        catch (Exception) { return new(definition.BuiltInDefault, "Megamod_Settings_InvalidStoredValue"); }
    }
}
