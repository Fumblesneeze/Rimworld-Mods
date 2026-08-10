using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndBundleCatalog : IGatewayEndToEndBundleInspector
{
    private const string ManifestSuffix = ".e2etests.json";
    private readonly IGatewayEndToEndAssemblyLoader assemblyLoader;

    public GatewayEndToEndBundleCatalog(IGatewayEndToEndAssemblyLoader? assemblyLoader = null)
    {
        this.assemblyLoader = assemblyLoader ?? new GatewayEndToEndAssemblyByteLoader();
    }

    public IReadOnlyList<GatewayEndToEndBundleInspectionResult> InspectAll(
        IEnumerable<GatewayEndToEndManifestCandidate> candidates,
        IReadOnlyList<string> activePackageIds)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return new ReadOnlyCollection<GatewayEndToEndBundleInspectionResult>(
            candidates
                .OrderBy(candidate => candidate.ManifestPath, StringComparer.Ordinal)
                .Select(candidate => Inspect(candidate, activePackageIds))
                .ToArray());
    }

    public GatewayEndToEndBundleInspectionResult Inspect(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        try
        {
            return InspectGuarded(candidate, ValidateActivePackages(activePackageIds));
        }
        catch (GatewayEndToEndCatalogException exception)
        {
            return GatewayEndToEndBundleInspectionResult.Failed(exception.Code, exception.Message);
        }
        catch
        {
            return GatewayEndToEndBundleInspectionResult.Failed(
                "bundle_inspection_failed",
                "The staged E2E bundle failed during guarded inspection; arbitrary exception text was suppressed.");
        }
    }

    private GatewayEndToEndBundleInspectionResult InspectGuarded(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds)
    {
        var manifestPath = candidate.ManifestPath;
        var manifestFileName = Path.GetFileName(manifestPath);
        if (!manifestFileName.EndsWith(ManifestSuffix, StringComparison.Ordinal) ||
            !File.Exists(manifestPath))
        {
            throw Failure("manifest_missing", "The exact staged E2E manifest is missing.");
        }

        EnsureRegularFile(manifestPath, "manifest_unsafe", "The staged E2E manifest is not a regular file.");
        var directory = Path.GetDirectoryName(manifestPath) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no containing directory.");
        if (!StringComparer.Ordinal.Equals(Path.GetFileName(directory), "DevEndToEndTests"))
        {
            throw Failure("manifest_path_invalid", "The staged E2E manifest is outside DevEndToEndTests.");
        }

        var versionDirectory = Path.GetDirectoryName(directory) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no version directory.");
        var ownerDirectory = Path.GetDirectoryName(versionDirectory) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no owner directory.");
        if (!StringComparer.OrdinalIgnoreCase.Equals(
                Path.GetFileName(ownerDirectory),
                candidate.ContainingPackageId))
        {
            throw Failure("manifest_path_invalid", "The staged E2E manifest owner path does not match its containing mod.");
        }

        EndToEndStageMarker marker;
        EndToEndBundleManifest manifest;
        try
        {
            marker = GatewayContractJson.ReadFile<EndToEndStageMarker>(
                Path.Combine(directory, EndToEndStageMarker.FileName));
            manifest = GatewayContractJson.ReadFile<EndToEndBundleManifest>(manifestPath);
        }
        catch
        {
            throw Failure("manifest_invalid", "The staged E2E marker or manifest is missing or invalid.");
        }

        ValidateMarker(
            marker,
            candidate,
            Path.GetFileName(versionDirectory),
            manifestFileName,
            manifest.Assembly);
        ValidateManifest(manifest, candidate, activePackageIds, manifestFileName);
        var matchingTests = manifest.Tests
            .Where(test => MatchesActiveGroup(test.ActivePackageIds, activePackageIds))
            .OrderBy(test => test.Id, StringComparer.Ordinal)
            .ToArray();
        if (matchingTests.Length == 0)
        {
            return GatewayEndToEndBundleInspectionResult.Skipped();
        }

        var assemblyPath = Path.GetFullPath(Path.Combine(directory, manifest.Assembly));
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(assemblyPath), directory) ||
            !File.Exists(assemblyPath))
        {
            throw Failure("assembly_missing", "The exact sibling E2E assembly is missing.");
        }

        EnsureRegularFile(assemblyPath, "assembly_unsafe", "The staged E2E assembly is not a regular file.");
        var assemblyBytes = File.ReadAllBytes(assemblyPath);
        if (assemblyBytes.LongLength != manifest.AssemblyLength)
        {
            throw Failure("assembly_length_mismatch", "The staged E2E assembly length differs from discovery.");
        }

        var hash = ComputeSha256(assemblyBytes);
        if (!StringComparer.Ordinal.Equals(hash, manifest.AssemblySha256))
        {
            throw Failure("assembly_hash_mismatch", "The staged E2E assembly hash differs from discovery.");
        }

        Assembly assembly;
        try
        {
            assembly = assemblyLoader.Load(assemblyBytes);
        }
        catch
        {
            throw Failure(
                "assembly_byte_load_failed",
                "The staged E2E assembly could not be byte-loaded; arbitrary exception text was suppressed.");
        }

        if (!StringComparer.Ordinal.Equals(
                GatewayEndToEndAssemblyIdentity.FormatDefinition(assembly.GetName()),
                manifest.AssemblyIdentity))
        {
            throw Failure("assembly_identity_mismatch", "The loaded E2E assembly identity differs from discovery.");
        }

        if (!Guid.TryParse(manifest.ModuleVersionId, out var expectedMvid) ||
            assembly.ManifestModule.ModuleVersionId != expectedMvid)
        {
            throw Failure("assembly_mvid_mismatch", "The loaded E2E assembly MVID differs from discovery.");
        }

        ValidateDependencies(assembly, manifest.Dependencies);
        var compiledTests = ReadCompiledTests(assembly);
        ValidateCompiledManifest(compiledTests, manifest.Tests);
        var compiledById = compiledTests.ToDictionary(test => test.Id, StringComparer.Ordinal);
        var admitted = matchingTests.Select(test =>
        {
            var compiled = compiledById[test.Id];
            return new GatewayEndToEndRuntimeTestDescriptor(
                test.Id,
                test.OwnerPackageId,
                test.ActivePackageIds,
                test.TypeName,
                test.MaxFrames,
                test.MaxGameTicks,
                test.MaxWallClockSeconds,
                compiled.TestType);
        }).ToArray();

        return GatewayEndToEndBundleInspectionResult.Loaded(
            new GatewayEndToEndAssemblySource(
                candidate.ContainingPackageId,
                manifestPath,
                manifest.AssemblyIdentity,
                manifest.AssemblySha256,
                assembly,
                admitted));
    }

    private static IReadOnlyList<string> ValidateActivePackages(IReadOnlyList<string> activePackageIds)
    {
        if (activePackageIds is null || activePackageIds.Count == 0)
        {
            throw Failure("active_package_list_invalid", "The active package list is empty.");
        }

        var normalized = activePackageIds.Select(Normalize).ToArray();
        if (normalized.Any(string.IsNullOrEmpty) ||
            normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length ||
            !StringComparer.OrdinalIgnoreCase.Equals(
                normalized[normalized.Length - 1],
                EndToEndTestContract.GatewayPackageId))
        {
            throw Failure(
                "active_package_list_invalid",
                "The active package list must be unique and end with the Dev Gateway.");
        }

        return normalized;
    }

    private static void ValidateMarker(
        EndToEndStageMarker marker,
        GatewayEndToEndManifestCandidate candidate,
        string rimWorldVersion,
        string manifestFileName,
        string assemblyFileName)
    {
        if (!StringComparer.Ordinal.Equals(marker.Schema, EndToEndStageMarker.SchemaValue) ||
            !StringComparer.OrdinalIgnoreCase.Equals(marker.OwnerPackageId, candidate.ContainingPackageId) ||
            !StringComparer.Ordinal.Equals(marker.RimWorldVersion, rimWorldVersion) ||
            string.IsNullOrWhiteSpace(marker.TransactionId) ||
            marker.Manifests is null ||
            !marker.Manifests.Contains(manifestFileName, StringComparer.Ordinal) ||
            marker.Assemblies is null ||
            !marker.Assemblies.Contains(assemblyFileName, StringComparer.Ordinal))
        {
            throw Failure("stage_marker_mismatch", "The staged E2E ownership marker does not match its bundle.");
        }
    }

    private static void ValidateManifest(
        EndToEndBundleManifest manifest,
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds,
        string manifestFileName)
    {
        if (!StringComparer.Ordinal.Equals(manifest.Schema, EndToEndBundleManifest.SchemaValue))
        {
            throw Failure("manifest_schema_mismatch", "The staged E2E manifest schema is unsupported.");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(manifest.OwnerPackageId, candidate.ContainingPackageId))
        {
            throw Failure("manifest_owner_mismatch", "The staged E2E manifest owner differs from its containing mod.");
        }

        if (!activePackageIds.Contains(candidate.ContainingPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Failure("manifest_owner_inactive", "The staged E2E manifest owner is not active.");
        }

        var manifestBaseName = manifestFileName.Substring(0, manifestFileName.Length - ManifestSuffix.Length);
        if (string.IsNullOrWhiteSpace(manifest.Assembly) ||
            !StringComparer.Ordinal.Equals(manifest.Assembly, manifestBaseName + ".dll") ||
            !StringComparer.Ordinal.Equals(Path.GetFileName(manifest.Assembly), manifest.Assembly))
        {
            throw Failure("manifest_assembly_invalid", "The E2E manifest does not name its exact sibling assembly.");
        }

        if (manifest.Tests is null || manifest.Tests.Length == 0)
        {
            throw Failure("manifest_tests_invalid", "The staged E2E manifest contains no tests.");
        }

        var duplicateId = manifest.Tests
            .GroupBy(test => test.Id ?? string.Empty, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw Failure("manifest_tests_invalid", "The staged E2E manifest contains a duplicate test ID.");
        }

        foreach (var test in manifest.Tests)
        {
            ValidateManifestTest(test, candidate.ContainingPackageId);
        }
    }

    private static void ValidateManifestTest(EndToEndBundleTest test, string containingPackageId)
    {
        var owner = Normalize(test.OwnerPackageId);
        var packages = (test.ActivePackageIds ?? Array.Empty<string>()).Select(Normalize).ToArray();
        if (string.IsNullOrWhiteSpace(test.Id) ||
            string.IsNullOrWhiteSpace(test.TypeName) ||
            !StringComparer.OrdinalIgnoreCase.Equals(owner, containingPackageId) ||
            packages.Length == 0 ||
            !EndToEndTestContract.HasValidLaunchPrefix(packages) ||
            packages.Any(string.IsNullOrEmpty) ||
            packages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != packages.Length ||
            packages.Contains(EndToEndTestContract.GatewayPackageId, StringComparer.OrdinalIgnoreCase) ||
            (!StringComparer.OrdinalIgnoreCase.Equals(owner, EndToEndTestContract.GatewayPackageId) &&
             !packages.Contains(owner, StringComparer.OrdinalIgnoreCase)) ||
            test.MaxFrames <= 0 ||
            test.MaxGameTicks <= 0 ||
            test.MaxWallClockSeconds <= 0)
        {
            throw Failure("manifest_tests_invalid", "A staged E2E test declaration is invalid.");
        }
    }

    private static bool MatchesActiveGroup(
        IReadOnlyList<string> declaredPackageIds,
        IReadOnlyList<string> activePackageIds)
    {
        if (declaredPackageIds.Count + 1 != activePackageIds.Count)
        {
            return false;
        }

        for (var index = 0; index < declaredPackageIds.Count; index++)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(
                    Normalize(declaredPackageIds[index]),
                    Normalize(activePackageIds[index])))
            {
                return false;
            }
        }

        return StringComparer.OrdinalIgnoreCase.Equals(
            Normalize(activePackageIds[activePackageIds.Count - 1]),
            EndToEndTestContract.GatewayPackageId);
    }

    private static void ValidateDependencies(
        Assembly assembly,
        IReadOnlyList<EndToEndBundleDependency> manifestDependencies)
    {
        if (manifestDependencies is null)
        {
            throw Failure("dependency_identity_mismatch", "The E2E dependency identity list is missing.");
        }

        var manifestIdentities = manifestDependencies
            .Select(ValidateDependencyDocument)
            .OrderBy(identity => identity, StringComparer.Ordinal)
            .ToArray();
        var references = assembly.GetReferencedAssemblies();
        var compiledIdentities = references
            .Select(GatewayEndToEndAssemblyIdentity.FormatReference)
            .OrderBy(identity => identity, StringComparer.Ordinal)
            .ToArray();
        if (!manifestIdentities.SequenceEqual(compiledIdentities, StringComparer.Ordinal))
        {
            throw Failure(
                "dependency_identity_mismatch",
                "The compiled E2E dependency identities differ from discovery.");
        }

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var reference in references)
        {
            var matchingLoaded = loadedAssemblies.Any(loaded =>
            {
                try
                {
                    return GatewayIntegrationTestAssemblyIdentity.Matches(reference, loaded.GetName());
                }
                catch
                {
                    return false;
                }
            });
            if (!matchingLoaded)
            {
                throw Failure(
                    "dependency_identity_mismatch",
                    "An exact compiled E2E dependency identity is not loaded.");
            }
        }
    }

    private static string ValidateDependencyDocument(EndToEndBundleDependency dependency)
    {
        if (dependency is null ||
            string.IsNullOrWhiteSpace(dependency.Name) ||
            string.IsNullOrWhiteSpace(dependency.Version) ||
            string.IsNullOrWhiteSpace(dependency.Culture) ||
            (dependency.KeyKind != "PublicKey" && dependency.KeyKind != "PublicKeyToken") ||
            string.IsNullOrWhiteSpace(dependency.PublicKeyOrToken) ||
            string.IsNullOrWhiteSpace(dependency.Identity))
        {
            throw Failure("dependency_identity_mismatch", "An E2E dependency identity document is incomplete.");
        }

        var reconstructed = dependency.Name + ", Version=" + dependency.Version +
                            ", Culture=" + dependency.Culture + ", " +
                            dependency.KeyKind + "=" + dependency.PublicKeyOrToken;
        if (!StringComparer.Ordinal.Equals(reconstructed, dependency.Identity))
        {
            throw Failure(
                "dependency_identity_mismatch",
                "An E2E dependency identity document is internally inconsistent.");
        }

        return dependency.Identity;
    }

    private static IReadOnlyList<CompiledTest> ReadCompiledTests(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch
        {
            throw Failure(
                "compiled_test_reflection_failed",
                "The staged E2E test types could not be reflected; arbitrary exception text was suppressed.");
        }

        var tests = new List<CompiledTest>();
        foreach (var type in types.OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            var attributes = CustomAttributeData.GetCustomAttributes(type)
                .Where(attribute => attribute.AttributeType == typeof(RimWorldEndToEndTestAttribute))
                .ToArray();
            if (attributes.Length == 0)
            {
                continue;
            }

            if (attributes.Length != 1)
            {
                throw Failure("compiled_test_manifest_mismatch", "A compiled E2E type has multiple test attributes.");
            }

            tests.Add(ReadCompiledTest(type, attributes[0]));
        }

        return tests.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
    }

    private static CompiledTest ReadCompiledTest(Type type, CustomAttributeData attribute)
    {
        if (!type.IsClass || type.IsAbstract ||
            !typeof(IRimWorldEndToEndTest).IsAssignableFrom(type) ||
            type.GetConstructor(Type.EmptyTypes) is null ||
            attribute.ConstructorArguments.Count != 3)
        {
            throw Failure("compiled_test_manifest_mismatch", "A compiled E2E type violates the runtime contract.");
        }

        var id = attribute.ConstructorArguments[0].Value as string ?? string.Empty;
        var owner = Normalize(attribute.ConstructorArguments[1].Value as string);
        var packages = ReadAttributeStringArray(attribute.ConstructorArguments[2]);
        var maxFrames = 3_600;
        var maxGameTicks = 60_000;
        var maxWallClockSeconds = 120;
        foreach (var named in attribute.NamedArguments)
        {
            var value = named.TypedValue.Value is int number ? number : 0;
            switch (named.MemberName)
            {
                case "MaxFrames":
                    maxFrames = value;
                    break;
                case "MaxGameTicks":
                    maxGameTicks = value;
                    break;
                case "MaxWallClockSeconds":
                    maxWallClockSeconds = value;
                    break;
            }
        }

        return new CompiledTest(
            id,
            owner,
            packages,
            type.FullName ?? type.Name,
            maxFrames,
            maxGameTicks,
            maxWallClockSeconds,
            type);
    }

    private static IReadOnlyList<string> ReadAttributeStringArray(CustomAttributeTypedArgument argument)
    {
        if (argument.Value is not IList<CustomAttributeTypedArgument> values)
        {
            return Array.Empty<string>();
        }

        return values.Select(value => Normalize(value.Value as string)).ToArray();
    }

    private static void ValidateCompiledManifest(
        IReadOnlyList<CompiledTest> compiled,
        IReadOnlyList<EndToEndBundleTest> manifest)
    {
        var orderedManifest = manifest.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
        if (compiled.Count != orderedManifest.Length)
        {
            throw Failure("compiled_test_manifest_mismatch", "The compiled E2E test list differs from discovery.");
        }

        for (var index = 0; index < compiled.Count; index++)
        {
            var actual = compiled[index];
            var expected = orderedManifest[index];
            if (!StringComparer.Ordinal.Equals(actual.Id, expected.Id) ||
                !StringComparer.Ordinal.Equals(actual.TypeName, expected.TypeName) ||
                !StringComparer.OrdinalIgnoreCase.Equals(actual.OwnerPackageId, expected.OwnerPackageId) ||
                !actual.ActivePackageIds.SequenceEqual(expected.ActivePackageIds, StringComparer.OrdinalIgnoreCase) ||
                actual.MaxFrames != expected.MaxFrames ||
                actual.MaxGameTicks != expected.MaxGameTicks ||
                actual.MaxWallClockSeconds != expected.MaxWallClockSeconds)
            {
                throw Failure("compiled_test_manifest_mismatch", "The compiled E2E test metadata differs from discovery.");
            }
        }
    }

    private static void EnsureRegularFile(string path, string code, string message)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length <= 0)
        {
            throw Failure(code, message);
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return GatewayEndToEndAssemblyIdentity.FormatKey(sha256.ComputeHash(bytes));
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static GatewayEndToEndCatalogException Failure(string code, string message) => new(code, message);

    private sealed class CompiledTest
    {
        public CompiledTest(
            string id,
            string ownerPackageId,
            IReadOnlyList<string> activePackageIds,
            string typeName,
            int maxFrames,
            int maxGameTicks,
            int maxWallClockSeconds,
            Type testType)
        {
            Id = id;
            OwnerPackageId = ownerPackageId;
            ActivePackageIds = activePackageIds;
            TypeName = typeName;
            MaxFrames = maxFrames;
            MaxGameTicks = maxGameTicks;
            MaxWallClockSeconds = maxWallClockSeconds;
            TestType = testType;
        }

        public string Id { get; }
        public string OwnerPackageId { get; }
        public IReadOnlyList<string> ActivePackageIds { get; }
        public string TypeName { get; }
        public int MaxFrames { get; }
        public int MaxGameTicks { get; }
        public int MaxWallClockSeconds { get; }
        public Type TestType { get; }
    }

    private sealed class GatewayEndToEndCatalogException : Exception
    {
        public GatewayEndToEndCatalogException(string code, string message) : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
