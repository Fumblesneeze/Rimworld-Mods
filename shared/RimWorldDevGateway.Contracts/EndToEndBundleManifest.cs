using System.Runtime.Serialization;

namespace RimWorldDevGateway.Contracts;

[DataContract]
public sealed class EndToEndBundleManifest
{
    public const string SchemaValue = "RimWorldDevGateway.EndToEndTests/v1";

    [DataMember(Name = "schema", IsRequired = true, Order = 1)]
    public string Schema { get; set; } = SchemaValue;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 2)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "assembly", IsRequired = true, Order = 3)]
    public string Assembly { get; set; } = string.Empty;

    [DataMember(Name = "assemblyIdentity", IsRequired = true, Order = 4)]
    public string AssemblyIdentity { get; set; } = string.Empty;

    [DataMember(Name = "moduleVersionId", IsRequired = true, Order = 5)]
    public string ModuleVersionId { get; set; } = string.Empty;

    [DataMember(Name = "assemblyLength", IsRequired = true, Order = 6)]
    public long AssemblyLength { get; set; }

    [DataMember(Name = "assemblySha256", IsRequired = true, Order = 7)]
    public string AssemblySha256 { get; set; } = string.Empty;

    [DataMember(Name = "dependencies", IsRequired = true, Order = 8)]
    public EndToEndBundleDependency[] Dependencies { get; set; } = System.Array.Empty<EndToEndBundleDependency>();

    [DataMember(Name = "tests", IsRequired = true, Order = 9)]
    public EndToEndBundleTest[] Tests { get; set; } = System.Array.Empty<EndToEndBundleTest>();
}

[DataContract]
public sealed class EndToEndBundleDependency
{
    [DataMember(Name = "name", IsRequired = true, Order = 1)]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "version", IsRequired = true, Order = 2)]
    public string Version { get; set; } = string.Empty;

    [DataMember(Name = "culture", IsRequired = true, Order = 3)]
    public string Culture { get; set; } = string.Empty;

    [DataMember(Name = "keyKind", IsRequired = true, Order = 4)]
    public string KeyKind { get; set; } = string.Empty;

    [DataMember(Name = "publicKeyOrToken", IsRequired = true, Order = 5)]
    public string PublicKeyOrToken { get; set; } = string.Empty;

    [DataMember(Name = "identity", IsRequired = true, Order = 6)]
    public string Identity { get; set; } = string.Empty;
}

[DataContract]
public sealed class EndToEndBundleTest
{
    [DataMember(Name = "id", IsRequired = true, Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Name = "typeName", IsRequired = true, Order = 2)]
    public string TypeName { get; set; } = string.Empty;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 3)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "activePackageIds", IsRequired = true, Order = 4)]
    public string[] ActivePackageIds { get; set; } = System.Array.Empty<string>();

    [DataMember(Name = "maxFrames", IsRequired = true, Order = 5)]
    public int MaxFrames { get; set; }

    [DataMember(Name = "maxGameTicks", IsRequired = true, Order = 6)]
    public int MaxGameTicks { get; set; }

    [DataMember(Name = "maxWallClockSeconds", IsRequired = true, Order = 7)]
    public int MaxWallClockSeconds { get; set; }
}

[DataContract]
public sealed class EndToEndStageMarker
{
    public const string SchemaValue = "RimWorldDevGateway.EndToEndStage/v1";
    public const string FileName = ".rimworld-dev-gateway-e2e-stage.json";

    [DataMember(Name = "schema", IsRequired = true, Order = 1)]
    public string Schema { get; set; } = SchemaValue;

    [DataMember(Name = "ownerPackageId", IsRequired = true, Order = 2)]
    public string OwnerPackageId { get; set; } = string.Empty;

    [DataMember(Name = "rimWorldVersion", IsRequired = true, Order = 3)]
    public string RimWorldVersion { get; set; } = string.Empty;

    [DataMember(Name = "transactionId", IsRequired = true, Order = 4)]
    public string TransactionId { get; set; } = string.Empty;

    [DataMember(Name = "assemblies", IsRequired = true, Order = 5)]
    public string[] Assemblies { get; set; } = System.Array.Empty<string>();

    [DataMember(Name = "manifests", IsRequired = true, Order = 6)]
    public string[] Manifests { get; set; } = System.Array.Empty<string>();
}
