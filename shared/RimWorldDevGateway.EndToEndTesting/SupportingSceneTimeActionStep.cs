using System;

namespace RimWorldDevGateway.EndToEndTesting;

public sealed class SupportingSceneTimeActionStep : EndToEndStep
{
    public SupportingSceneTimeActionStep(string name, string mapHandle, int minuteOfDay)
        : base(name, EndToEndStepKind.Act)
    {
        if (string.IsNullOrWhiteSpace(mapHandle) || mapHandle.Length > 256)
            throw new ArgumentException("An exact current map handle is required.", nameof(mapHandle));
        if (minuteOfDay < 0 || minuteOfDay > 1439) throw new ArgumentOutOfRangeException(nameof(minuteOfDay));
        MapHandle = mapHandle;
        MinuteOfDay = minuteOfDay;
    }
    public string MapHandle { get; }
    public int MinuteOfDay { get; }
}
