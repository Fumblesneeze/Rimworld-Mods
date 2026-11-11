using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Megamod.Settings;

public sealed record SaveSettingValue(bool UseGlobal, string? Override);

public sealed class SaveSettingsSnapshot
{
    public SaveSettingsSnapshot(string saveIdentity, IDictionary<string, SaveSettingValue>? values = null)
    {
        SaveIdentity = saveIdentity;
        Values = new ReadOnlyDictionary<string, SaveSettingValue>(values == null
            ? new Dictionary<string, SaveSettingValue>() : new Dictionary<string, SaveSettingValue>(values));
    }
    public string SaveIdentity { get; }
    public IReadOnlyDictionary<string, SaveSettingValue> Values { get; }
}

public sealed class SaveSettingsBinding
{
    private int revoked;
    internal SaveSettingsBinding(string saveIdentity, long generation)
    {
        SaveIdentity = saveIdentity;
        Generation = generation;
    }
    public string SaveIdentity { get; }
    public long Generation { get; }
    public bool IsRevoked => System.Threading.Volatile.Read(ref revoked) != 0;
    public void Revoke() => System.Threading.Interlocked.Exchange(ref revoked, 1);
}

public readonly record struct SettingEditResult(bool Succeeded, string? ErrorKey = null);
public readonly record struct SettingReadResult<T>(T Value, string? ErrorKey = null);
public readonly record struct SettingsRevision(long Global, string? SaveIdentity, long Activation, long Save);
