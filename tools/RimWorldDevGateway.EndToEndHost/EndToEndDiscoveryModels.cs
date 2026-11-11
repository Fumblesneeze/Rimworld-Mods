using System.Collections.ObjectModel;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class EndToEndMetadataDeclaration
{
    public IReadOnlyList<EndToEndDiagnosticExpectation> ExpectedDiagnostics { get; }
    public bool LegacyXmlDeserializer { get; }
    public EndToEndMetadataDeclaration(
        string typeName,
        string id,
        string ownerPackageId,
        IEnumerable<string> activePackageIds,
        int maxFrames,
        int maxGameTicks,
        int maxWallClockSeconds,
        bool isConcrete,
        bool implementsContract,
        bool hasPublicParameterlessConstructor,
        bool legacyXmlDeserializer = false,
        IEnumerable<EndToEndDiagnosticExpectation>? expectedDiagnostics = null)
    {
        TypeName = typeName;
        Id = id;
        OwnerPackageId = ownerPackageId;
        ActivePackageIds = Copy(activePackageIds);
        MaxFrames = maxFrames;
        MaxGameTicks = maxGameTicks;
        MaxWallClockSeconds = maxWallClockSeconds;
        IsConcrete = isConcrete;
        ImplementsContract = implementsContract;
        HasPublicParameterlessConstructor = hasPublicParameterlessConstructor;
        LegacyXmlDeserializer = legacyXmlDeserializer;
        ExpectedDiagnostics = new ReadOnlyCollection<EndToEndDiagnosticExpectation>(
            (expectedDiagnostics ?? Array.Empty<EndToEndDiagnosticExpectation>()).ToArray());
    }

    public string TypeName { get; }

    public string Id { get; }

    public string OwnerPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public int MaxFrames { get; }

    public int MaxGameTicks { get; }

    public int MaxWallClockSeconds { get; }

    public bool IsConcrete { get; }

    public bool ImplementsContract { get; }

    public bool HasPublicParameterlessConstructor { get; }

    private static IReadOnlyList<string> Copy(IEnumerable<string> values) =>
        new ReadOnlyCollection<string>(values.ToArray());
}

public sealed class EndToEndAssemblyMetadata
{
    public EndToEndAssemblyMetadata(
        string assemblyPath,
        string assemblyName,
        string assemblyVersion,
        string assemblyCulture,
        string assemblyPublicKey,
        Guid moduleVersionId,
        long length,
        string sha256,
        IEnumerable<EndToEndAssemblyReference> assemblyReferences,
        IEnumerable<EndToEndMetadataDeclaration> declarations)
    {
        AssemblyPath = assemblyPath;
        AssemblyName = assemblyName;
        AssemblyVersion = assemblyVersion;
        AssemblyCulture = assemblyCulture;
        AssemblyPublicKey = assemblyPublicKey;
        AssemblyIdentity =
            $"{assemblyName}, Version={assemblyVersion}, Culture={assemblyCulture}, PublicKey={assemblyPublicKey}";
        ModuleVersionId = moduleVersionId;
        Length = length;
        Sha256 = sha256;
        AssemblyReferences = new ReadOnlyCollection<EndToEndAssemblyReference>(assemblyReferences.ToArray());
        Declarations = new ReadOnlyCollection<EndToEndMetadataDeclaration>(declarations.ToArray());
    }

    public string AssemblyPath { get; }

    public string AssemblyName { get; }

    public string AssemblyVersion { get; }

    public string AssemblyCulture { get; }

    public string AssemblyPublicKey { get; }

    public string AssemblyIdentity { get; }

    public Guid ModuleVersionId { get; }

    public long Length { get; }

    public string Sha256 { get; }

    public IReadOnlyList<EndToEndAssemblyReference> AssemblyReferences { get; }

    public IReadOnlyList<EndToEndMetadataDeclaration> Declarations { get; }
}

public sealed class EndToEndAssemblyReference
{
    public EndToEndAssemblyReference(
        string name,
        string version,
        string culture,
        string keyKind,
        string publicKeyOrToken)
    {
        Name = name;
        Version = version;
        Culture = culture;
        KeyKind = keyKind;
        PublicKeyOrToken = publicKeyOrToken;
        Identity =
            $"{name}, Version={version}, Culture={culture}, {keyKind}={publicKeyOrToken}";
    }

    public string Name { get; }

    public string Version { get; }

    public string Culture { get; }

    public string KeyKind { get; }

    public string PublicKeyOrToken { get; }

    public string Identity { get; }
}

public sealed class EndToEndAssemblyCandidate
{
    public EndToEndAssemblyCandidate(
        string projectPath,
        string expectedOwnerPackageId,
        string expectedAssemblyName,
        EndToEndAssemblyMetadata metadata)
    {
        ProjectPath = projectPath;
        ExpectedOwnerPackageId = expectedOwnerPackageId;
        ExpectedAssemblyName = expectedAssemblyName;
        Metadata = metadata;
    }

    public string ProjectPath { get; }

    public string ExpectedOwnerPackageId { get; }

    public string ExpectedAssemblyName { get; }

    public EndToEndAssemblyMetadata Metadata { get; }
}

public sealed class EndToEndDiscoveredTest
{
    public IReadOnlyList<EndToEndDiagnosticExpectation> ExpectedDiagnostics { get; } = Array.Empty<EndToEndDiagnosticExpectation>();
    internal EndToEndDiscoveredTest(EndToEndAssemblyCandidate candidate, EndToEndMetadataDeclaration declaration)
    {
        ExpectedDiagnostics = declaration.ExpectedDiagnostics.ToArray();
        LegacyXmlDeserializer = declaration.LegacyXmlDeserializer;
        Id = declaration.Id.Trim();
        TypeName = declaration.TypeName;
        OwnerPackageId = declaration.OwnerPackageId.Trim().ToLowerInvariant();
        ActivePackageIds = new ReadOnlyCollection<string>(
            declaration.ActivePackageIds.Select(value => value.Trim().ToLowerInvariant()).ToArray());
        MaxFrames = declaration.MaxFrames;
        MaxGameTicks = declaration.MaxGameTicks;
        MaxWallClockSeconds = declaration.MaxWallClockSeconds;
        ProjectPath = candidate.ProjectPath;
        AssemblyPath = candidate.Metadata.AssemblyPath;
        AssemblyName = candidate.Metadata.AssemblyName;
        AssemblyVersion = candidate.Metadata.AssemblyVersion;
        AssemblyIdentity = candidate.Metadata.AssemblyIdentity;
        ModuleVersionId = candidate.Metadata.ModuleVersionId;
        AssemblyLength = candidate.Metadata.Length;
        AssemblySha256 = candidate.Metadata.Sha256;
        AssemblyReferences = candidate.Metadata.AssemblyReferences;
    }

    public string Id { get; }

    public string TypeName { get; }

    public string OwnerPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public int MaxFrames { get; }

    public int MaxGameTicks { get; }

    public int MaxWallClockSeconds { get; }

    public string ProjectPath { get; }

    public bool LegacyXmlDeserializer { get; }

    public string AssemblyPath { get; }

    public string AssemblyName { get; }

    public string AssemblyVersion { get; }

    public string AssemblyIdentity { get; }

    public Guid ModuleVersionId { get; }

    public long AssemblyLength { get; }

    public string AssemblySha256 { get; }

    public IReadOnlyList<EndToEndAssemblyReference> AssemblyReferences { get; }
}

public sealed class EndToEndTestGroup
{
    internal EndToEndTestGroup(string groupId, IEnumerable<string> activePackageIds, IEnumerable<EndToEndDiscoveredTest> tests)
    {
        GroupId = groupId;
        ActivePackageIds = new ReadOnlyCollection<string>(activePackageIds.ToArray());
        Tests = new ReadOnlyCollection<EndToEndDiscoveredTest>(tests.ToArray());
    }

    public string GroupId { get; }

    public bool LegacyXmlDeserializer => Tests[0].LegacyXmlDeserializer;

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<EndToEndDiscoveredTest> Tests { get; }
}

public sealed class EndToEndDiscoveryResult
{
    internal EndToEndDiscoveryResult(IEnumerable<EndToEndTestGroup> groups,
        IEnumerable<EndToEndDiscoveredTest>? selectedTests = null)
    {
        Groups = new ReadOnlyCollection<EndToEndTestGroup>(groups.ToArray());
        SelectedTests = new ReadOnlyCollection<EndToEndDiscoveredTest>((selectedTests ?? Groups.SelectMany(group => group.Tests)).ToArray());
    }

    public IReadOnlyList<EndToEndTestGroup> Groups { get; }
    public IReadOnlyList<EndToEndDiscoveredTest> SelectedTests { get; }
}

public sealed class EndToEndProjectRecord
{
    public EndToEndProjectRecord(string projectPath, string ownerPackageId, string assemblyName, string targetFramework)
    {
        ProjectPath = projectPath;
        OwnerPackageId = ownerPackageId;
        AssemblyName = assemblyName;
        TargetFramework = targetFramework;
    }

    public string ProjectPath { get; }

    public string OwnerPackageId { get; }

    public string AssemblyName { get; }

    public string TargetFramework { get; }
}

public sealed class EndToEndDiscoveryException : Exception
{
    public EndToEndDiscoveryException(string message) : base(message)
    {
    }
}
