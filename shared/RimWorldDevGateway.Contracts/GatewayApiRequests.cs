using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RimWorldDevGateway.Contracts;

[DataContract]
public sealed class GatewayDefExportApiRequest
{
    [DataMember(Name = "format", Order = 1)]
    public string Format { get; set; } = "json";

    [DataMember(Name = "defTypes", Order = 2)]
    public List<string> DefTypes { get; set; } = new();

    [DataMember(Name = "defNames", Order = 3)]
    public List<string> DefNames { get; set; } = new();

    [DataMember(Name = "sourcePackageIds", Order = 4)]
    public List<string> SourcePackageIds { get; set; } = new();

    [DataMember(Name = "fieldNames", Order = 5)]
    public List<string> FieldNames { get; set; } = new();

    [DataMember(Name = "cursor", Order = 6, EmitDefaultValue = false)]
    public string? Cursor { get; set; }

    [DataMember(Name = "pageSize", Order = 7, EmitDefaultValue = false)]
    public int? PageSize { get; set; }

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Format = "json";
        DefTypes = new List<string>();
        DefNames = new List<string>();
        SourcePackageIds = new List<string>();
        FieldNames = new List<string>();
    }
}

[DataContract]
public sealed class GatewayGameStateMutationRequest
{
    [DataMember(Name = "devMode", Order = 1, EmitDefaultValue = false)]
    public bool? DevMode { get; set; }

    [DataMember(Name = "godMode", Order = 2, EmitDefaultValue = false)]
    public bool? GodMode { get; set; }

    [DataMember(Name = "paused", Order = 3, EmitDefaultValue = false)]
    public bool? Paused { get; set; }

    [DataMember(Name = "speed", Order = 4, EmitDefaultValue = false)]
    public string? Speed { get; set; }
}

[DataContract]
public sealed class GatewayMapCellRequest
{
    [DataMember(Name = "x", Order = 1, IsRequired = true)]
    public int X { get; set; }

    [DataMember(Name = "z", Order = 2, IsRequired = true)]
    public int Z { get; set; }
}

[DataContract]
public sealed class GatewayCameraMutationRequest
{
    [DataMember(Name = "mapHandle", Order = 1, EmitDefaultValue = false)]
    public string? MapHandle { get; set; }

    [DataMember(Name = "center", Order = 2, EmitDefaultValue = false)]
    public GatewayMapCellRequest? Center { get; set; }

    [DataMember(Name = "rootSize", Order = 3, EmitDefaultValue = false)]
    public float? RootSize { get; set; }
}

[DataContract]
public sealed class GatewayThingQueryRequest
{
    [DataMember(Name = "scope", Order = 1)]
    public string Scope { get; set; } = "map";

    [DataMember(Name = "defNames", Order = 2)]
    public List<string> DefNames { get; set; } = new();

    [DataMember(Name = "kinds", Order = 3)]
    public List<string> Kinds { get; set; } = new();

    [DataMember(Name = "runtimeTypes", Order = 4)]
    public List<string> RuntimeTypes { get; set; } = new();

    [DataMember(Name = "factionNames", Order = 5)]
    public List<string> FactionNames { get; set; } = new();

    [DataMember(Name = "factionRelations", Order = 6)]
    public List<string> FactionRelations { get; set; } = new();

    [DataMember(Name = "labelContains", Order = 7, EmitDefaultValue = false)]
    public string? LabelContains { get; set; }

    [DataMember(Name = "fogged", Order = 8, EmitDefaultValue = false)]
    public bool? Fogged { get; set; }

    [DataMember(Name = "selected", Order = 9, EmitDefaultValue = false)]
    public bool? Selected { get; set; }

    [DataMember(Name = "after", Order = 10, EmitDefaultValue = false)]
    public string? After { get; set; }

    [DataMember(Name = "limit", Order = 11)]
    public int Limit { get; set; } = 100;

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Scope = "map";
        DefNames = new List<string>();
        Kinds = new List<string>();
        RuntimeTypes = new List<string>();
        FactionNames = new List<string>();
        FactionRelations = new List<string>();
        Limit = 100;
    }
}

[DataContract]
public sealed class GatewaySelectionRequest
{
    [DataMember(Name = "operation", Order = 1, IsRequired = true)]
    public string Operation { get; set; } = string.Empty;

    [DataMember(Name = "handles", Order = 2)]
    public List<string> Handles { get; set; } = new();

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Handles = new List<string>();
    }
}

[DataContract]
public sealed class GatewayAssemblyExecutionRequest
{
    [DataMember(Name = "assemblyBase64", Order = 1, IsRequired = true)]
    public string AssemblyBase64 { get; set; } = string.Empty;

    [DataMember(Name = "entryType", Order = 2, IsRequired = true)]
    public string EntryType { get; set; } = string.Empty;

    [DataMember(Name = "entryMethod", Order = 3, IsRequired = true)]
    public string EntryMethod { get; set; } = "Execute";

    [DataMember(Name = "requestJson", Order = 4)]
    public string RequestJson { get; set; } = "{}";

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        EntryMethod = "Execute";
        RequestJson = "{}";
    }
}

[DataContract]
public sealed class GatewayClickRequest
{
    [DataMember(Name = "x", Order = 1, IsRequired = true)]
    public int X { get; set; }

    [DataMember(Name = "y", Order = 2, IsRequired = true)]
    public int Y { get; set; }

    [DataMember(Name = "button", Order = 3)]
    public string Button { get; set; } = "left";

    [DataMember(Name = "activate", Order = 4)]
    public bool Activate { get; set; } = true;

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Button = "left";
        Activate = true;
    }
}

[DataContract]
public sealed class GatewayDragRequest
{
    [DataMember(Name = "startX", Order = 1, IsRequired = true)]
    public int StartX { get; set; }

    [DataMember(Name = "startY", Order = 2, IsRequired = true)]
    public int StartY { get; set; }

    [DataMember(Name = "endX", Order = 3, IsRequired = true)]
    public int EndX { get; set; }

    [DataMember(Name = "endY", Order = 4, IsRequired = true)]
    public int EndY { get; set; }

    [DataMember(Name = "button", Order = 5)]
    public string Button { get; set; } = "left";

    [DataMember(Name = "durationMs", Order = 6)]
    public int DurationMs { get; set; } = 250;

    [DataMember(Name = "steps", Order = 7)]
    public int Steps { get; set; } = 10;

    [DataMember(Name = "activate", Order = 8)]
    public bool Activate { get; set; } = true;

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Button = "left";
        DurationMs = 250;
        Steps = 10;
        Activate = true;
    }
}

[DataContract]
public sealed class GatewayKeysRequest
{
    [DataMember(Name = "key", Order = 1)]
    public string? Key { get; set; }

    [DataMember(Name = "modifiers", Order = 2)]
    public List<string> Modifiers { get; set; } = new();

    [DataMember(Name = "text", Order = 3)]
    public string? Text { get; set; }

    [DataMember(Name = "activate", Order = 4)]
    public bool Activate { get; set; } = true;

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Modifiers = new List<string>();
        Activate = true;
    }
}

[DataContract]
public sealed class GatewaySemanticActionRequest
{
    [DataMember(Name = "arguments", Order = 1)]
    public Dictionary<string, object?> Arguments { get; set; } = new();

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Arguments = new Dictionary<string, object?>();
    }
}

[DataContract]
public sealed class GatewayAutomationRunRequest
{
    [DataMember(Name = "arguments", Order = 1)]
    public Dictionary<string, object?> Arguments { get; set; } = new();

    [DataMember(Name = "idempotencyKey", Order = 2)]
    public string? IdempotencyKey { get; set; }

    [OnDeserializing]
    private void ApplyDefaults(StreamingContext context)
    {
        Arguments = new Dictionary<string, object?>();
    }
}
