using System.Collections.Generic;

namespace Megamod.Settings;

// Startup-owned, nonpersistent inputs. No package detection or mutable settings live here.
public sealed class SettingsEffectiveOff
{
    private readonly Dictionary<SettingDefinition<bool>, string> reasons;

    public SettingsEffectiveOff(IReadOnlyDictionary<SettingDefinition<bool>, string> reasons)
        => this.reasons = new(reasons);

    public static SettingsEffectiveOff Combine(params SettingsEffectiveOff[] sources)
    {
        var combined = new Dictionary<SettingDefinition<bool>, string>();
        foreach (var source in sources)
            foreach (var reason in source.reasons)
                if (!combined.ContainsKey(reason.Key)) combined.Add(reason.Key, reason.Value);
        return new(combined);
    }

    internal string? ReasonFor<T>(SettingDefinition<T> definition) =>
        definition is SettingDefinition<bool> boolean && reasons.TryGetValue(boolean, out string? reason) ? reason : null;

    internal SettingReadResult<T> Apply<T>(SettingDefinition<T> definition, SettingReadResult<T> requested) =>
        ReasonFor(definition) != null ? new((T)(object)false, requested.ErrorKey) : requested;
}
