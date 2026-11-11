using System;

namespace Megamod.Settings;

public enum SettingScope { GlobalOnly, GlobalAndSave }
public enum SettingApplicationTiming { NextDisplay, NextEligibleOccurrence, NewGameOrLoad, Restart }

public sealed class SettingDefinition<T>
{
    public SettingDefinition(string id, T builtInDefault, SettingScope scope, string categoryId,
        int order, SettingApplicationTiming applicationTiming, ISettingCodec<T> codec,
        Func<T, string?>? validation = null)
    {
        Id = id;
        BuiltInDefault = builtInDefault;
        Scope = scope;
        CategoryId = categoryId;
        Order = order;
        ApplicationTiming = applicationTiming;
        Codec = codec;
        Validation = validation;
    }

    public string Id { get; }
    public T BuiltInDefault { get; }
    public SettingScope Scope { get; }
    public string CategoryId { get; }
    public int Order { get; }
    public SettingApplicationTiming ApplicationTiming { get; }
    public ISettingCodec<T> Codec { get; }
    public Func<T, string?>? Validation { get; }
}
