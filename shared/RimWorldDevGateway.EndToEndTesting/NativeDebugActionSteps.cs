using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RimWorldDevGateway.EndToEndTesting;

public interface IEndToEndDebugActionCatalog
{
    EndToEndDebugActionPage Query(string? search = null, IReadOnlyList<string>? categories = null,
        string? after = null, int limit = 100);
}

public sealed class EndToEndDebugAction
{
    public EndToEndDebugAction(string handle, string path, string label, string category,
        string sourceType, string allowedGameStates, bool visible, bool active, string? discoveryError)
    {
        Handle = handle; Path = path; Label = label; Category = category; SourceType = sourceType;
        AllowedGameStates = allowedGameStates; Visible = visible; Active = active; DiscoveryError = discoveryError;
    }
    public string Handle { get; }
    public string Path { get; }
    public string Label { get; }
    public string Category { get; }
    public string SourceType { get; }
    public string AllowedGameStates { get; }
    public bool Visible { get; }
    public bool Active { get; }
    public string? DiscoveryError { get; }
}

public sealed class EndToEndDebugActionPage
{
    public EndToEndDebugActionPage(IEnumerable<EndToEndDebugAction> items, bool pageTruncated)
    {
        Items = new ReadOnlyCollection<EndToEndDebugAction>(items.ToArray());
        PageTruncated = pageTruncated;
    }
    public IReadOnlyList<EndToEndDebugAction> Items { get; }
    public bool PageTruncated { get; }
}

/// <summary>Invoke one current immediate native debug action; queued work needs a later wait.</summary>
public sealed class NativeDebugActionStep : EndToEndStep
{
    public NativeDebugActionStep(string name, string handle) : base(name, EndToEndStepKind.Act)
        => Handle = StepValues.Required(handle, nameof(handle));
    public string Handle { get; }
}
