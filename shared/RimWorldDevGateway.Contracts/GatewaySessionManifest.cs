using System.Runtime.Serialization;

namespace RimWorldDevGateway.Contracts;

[DataContract]
public sealed class GatewaySessionManifest
{
    [DataMember(Name = "apiVersion", Order = 1)]
    public string ApiVersion { get; set; } = "1";

    [DataMember(Name = "runId", Order = 2)]
    public string RunId { get; set; } = string.Empty;

    [DataMember(Name = "state", Order = 3)]
    public string State { get; set; } = string.Empty;

    [DataMember(Name = "baseUrl", Order = 4)]
    public string BaseUrl { get; set; } = string.Empty;

    [DataMember(Name = "token", Order = 5, EmitDefaultValue = false)]
    public string? Token { get; set; }

    [DataMember(Name = "processId", Order = 6)]
    public int ProcessId { get; set; }

    [DataMember(Name = "processStartUtc", Order = 7)]
    public string ProcessStartUtc { get; set; } = string.Empty;

    [DataMember(Name = "startedUtc", Order = 8)]
    public string StartedUtc { get; set; } = string.Empty;

    [DataMember(Name = "stoppedUtc", Order = 9, EmitDefaultValue = false)]
    public string? StoppedUtc { get; set; }

    [DataMember(Name = "gameVersion", Order = 10)]
    public string GameVersion { get; set; } = string.Empty;

    [DataMember(Name = "modVersion", Order = 11)]
    public string ModVersion { get; set; } = string.Empty;

    [DataMember(Name = "unrestrictedExecution", Order = 12)]
    public bool UnrestrictedExecution { get; set; }

    [DataMember(Name = "warning", Order = 13)]
    public string Warning { get; set; } = string.Empty;
}
