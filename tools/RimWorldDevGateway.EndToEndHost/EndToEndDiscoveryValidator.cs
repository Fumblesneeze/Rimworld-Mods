using System.Collections.ObjectModel;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndDiscoveryValidator
{
    public const string HarmonyPackageId = "brrainz.harmony";
    public const string CorePackageId = "ludeon.rimworld";
    public const string GatewayPackageId = "fumblesneeze.rimworlddevgateway";

    public static EndToEndDiscoveryResult ValidateAndGroup(
        IEnumerable<EndToEndAssemblyCandidate> assemblyCandidates,
        IEnumerable<string> resolvablePackageIds)
    {
        if (assemblyCandidates is null)
        {
            throw new ArgumentNullException(nameof(assemblyCandidates));
        }

        if (resolvablePackageIds is null)
        {
            throw new ArgumentNullException(nameof(resolvablePackageIds));
        }

        var candidates = assemblyCandidates
            .OrderBy(candidate => candidate.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new EndToEndDiscoveryException("E2E discovery selected zero marked assemblies.");
        }

        var packageCatalog = new HashSet<string>(
            resolvablePackageIds
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var discovered = new List<EndToEndDiscoveredTest>();
        foreach (var candidate in candidates)
        {
            ValidateAssembly(candidate, packageCatalog, errors, discovered);
        }

        foreach (var duplicate in discovered
                     .GroupBy(test => test.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            errors.Add($"duplicate test ID '{duplicate.Key}' is declared by: " +
                       string.Join(", ", duplicate.Select(test => test.TypeName).OrderBy(value => value, StringComparer.Ordinal)));
        }

        if (errors.Count > 0)
        {
            throw new EndToEndDiscoveryException(
                "E2E metadata discovery failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => "- " + error)));
        }

        var groups = discovered
            .GroupBy(test => string.Join("|", test.ActivePackageIds), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new EndToEndTestGroup(
                group.Key,
                group.First().ActivePackageIds,
                group.OrderBy(test => test.Id, StringComparer.Ordinal)))
            .ToArray();
        return new EndToEndDiscoveryResult(groups);
    }

    private static void ValidateAssembly(
        EndToEndAssemblyCandidate candidate,
        ISet<string> packageCatalog,
        ICollection<string> errors,
        ICollection<EndToEndDiscoveredTest> discovered)
    {
        if (!StringComparer.Ordinal.Equals(candidate.ExpectedAssemblyName, candidate.Metadata.AssemblyName))
        {
            errors.Add(
                $"assembly output '{candidate.Metadata.AssemblyName}' does not match expected output " +
                $"'{candidate.ExpectedAssemblyName}' for project '{candidate.ProjectPath}'");
        }

        if (candidate.Metadata.Declarations.Count == 0)
        {
            errors.Add($"marked project '{candidate.ProjectPath}' contains no compiled E2E declarations");
            return;
        }

        var expectedOwner = Normalize(candidate.ExpectedOwnerPackageId);
        foreach (var declaration in candidate.Metadata.Declarations.OrderBy(item => item.TypeName, StringComparer.Ordinal))
        {
            ValidateDeclaration(candidate, declaration, expectedOwner, packageCatalog, errors);
            discovered.Add(new EndToEndDiscoveredTest(candidate, declaration));
        }
    }

    private static void ValidateDeclaration(
        EndToEndAssemblyCandidate candidate,
        EndToEndMetadataDeclaration declaration,
        string expectedOwner,
        ISet<string> packageCatalog,
        ICollection<string> errors)
    {
        var displayId = string.IsNullOrWhiteSpace(declaration.Id) ? declaration.TypeName : declaration.Id;
        if (string.IsNullOrWhiteSpace(declaration.Id))
        {
            errors.Add($"'{declaration.TypeName}' has an empty test ID");
        }

        if (!declaration.IsConcrete)
        {
            errors.Add($"'{displayId}' must be a concrete class");
        }

        if (!declaration.ImplementsContract)
        {
            errors.Add($"'{displayId}' must implement IRimWorldEndToEndTest");
        }

        if (!declaration.HasPublicParameterlessConstructor)
        {
            errors.Add($"'{displayId}' must expose a public parameterless constructor");
        }

        var owner = Normalize(declaration.OwnerPackageId);
        if (string.IsNullOrEmpty(owner))
        {
            errors.Add($"'{displayId}' has an empty owner package ID");
        }
        else if (!StringComparer.OrdinalIgnoreCase.Equals(owner, expectedOwner))
        {
            errors.Add(
                $"project owner '{candidate.ExpectedOwnerPackageId}' does not match declared owner " +
                $"'{declaration.OwnerPackageId}' for '{displayId}'");
        }

        var packages = declaration.ActivePackageIds.Select(Normalize).ToArray();
        if (packages.Length == 0)
        {
            errors.Add($"'{displayId}' has an empty active package sequence");
        }
        else
        {
            ValidateLaunchPrefix(packages, displayId, errors);
        }

        var duplicate = packages
            .Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            errors.Add($"'{displayId}' contains duplicate active package '{duplicate.Key}'");
        }

        if (packages.Contains(GatewayPackageId, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"'{displayId}' must not list implicit Gateway package {GatewayPackageId}");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(owner, GatewayPackageId) &&
            !packages.Contains(owner, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"'{displayId}' active packages do not contain owner '{owner}'");
        }

        foreach (var package in packages.Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!packageCatalog.Contains(package))
            {
                errors.Add($"'{displayId}' declares unresolvable package '{package}'");
            }
        }

        if (declaration.MaxFrames <= 0 ||
            declaration.MaxGameTicks <= 0 ||
            declaration.MaxWallClockSeconds <= 0)
        {
            errors.Add($"'{displayId}' must declare positive frame, game-tick, and wall-clock deadline values");
        }
    }

    private static void ValidateLaunchPrefix(string[] packages, string displayId, ICollection<string> errors)
    {
        var harmonyIndex = Array.FindIndex(
            packages,
            packageId => StringComparer.OrdinalIgnoreCase.Equals(packageId, HarmonyPackageId));
        if (harmonyIndex < 0)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(packages[0], CorePackageId))
            {
                errors.Add($"'{displayId}' must list {CorePackageId} first when {HarmonyPackageId} is absent");
            }

            return;
        }

        if (harmonyIndex != 0)
        {
            errors.Add($"'{displayId}' must list {HarmonyPackageId} first when Harmony is active");
            return;
        }

        if (packages.Length < 2 || !StringComparer.OrdinalIgnoreCase.Equals(packages[1], CorePackageId))
        {
            errors.Add($"'{displayId}' must list {CorePackageId} immediately after {HarmonyPackageId}");
        }
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
