<#
.SYNOPSIS
Builds and stages opt-in in-game integration-test assemblies for active RimWorld mods.

.DESCRIPTION
Scans repository project files for the literal RimWorldInGameIntegrationTest opt-in and its owning
package ID. Matching projects are built deterministically, their TargetPath is resolved by MSBuild,
and the assembly plus its sibling .integrationtests.json manifest are staged beneath an isolated
test-mod root. Dry-run mode validates and prints the plan without invoking dotnet or writing.

Exit codes are 0 for success, 1 for build/runtime failure, and 2 for invalid input or an invalid
project/manifest contract.

.EXAMPLE
.\scripts\Build-InGameIntegrationTests.ps1 -ActivePackageIds @('ludeon.rimworld','fumblesneeze.rimworlddevgateway') -RimWorldPath F:\Steam\steamapps\common\RimWorld -SteamModContentFolder F:\Steam\steamapps\workshop\content\294100 -DryRun

.EXAMPLE
.\scripts\Build-InGameIntegrationTests.ps1 -ActivePackageIds @('ludeon.rimworld','fumblesneeze.rimworlddevgateway') -RimWorldPath F:\Steam\steamapps\common\RimWorld -SteamModContentFolder F:\Steam\steamapps\workshop\content\294100 -Configuration Release -Output json
#>
[CmdletBinding()]
param(
    [string[]]$ActivePackageIds,

    [string]$Configuration = 'Release',

    [string]$ArtifactsModsRoot,

    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [string]$RimWorldVersion = '1.6',

    [switch]$DryRun,

    [string]$Output = 'table',

    [Parameter(DontShow)]
    [string]$TestFailCommitAfterBackupOwner,

    [Parameter(DontShow)]
    [string]$TestCrashCommitAfterBackupOwner,

    [Parameter(DontShow)]
    [string]$TestCreateRollbackDestinationOwner,

    [Parameter(DontShow)]
    [string]$TestDeleteBackupBeforeRollbackOwner,

    [Parameter(DontShow)]
    [string]$TestCreateCommitBackupDestinationOwner,

    [Parameter(DontShow)]
    [string]$TestCreatePublishLiveDestinationOwner,

    [Parameter(DontShow)]
    [string]$TestCreateRecoveryLiveDestinationOwner,

    [Parameter(DontShow)]
    [string]$TestReplaceTemporaryWithUnownedOwner
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

function Test-PackageId {
    param([string]$Value)

    return -not [string]::IsNullOrWhiteSpace($Value) -and
        $Value.Length -le 256 -and
        $Value -cmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$'
}

function Test-IsWorkshopPath {
    param([string]$Path)

    $normalized = $Path.Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)
    return $normalized -match '(?i)(^|[\\/])steamapps[\\/]workshop[\\/]content([\\/]|$)'
}

function Test-IsChildPath {
    param(
        [string]$Candidate,
        [string]$Parent
    )

    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    $parentWithSeparator = $Parent.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return $Candidate.StartsWith($parentWithSeparator, $comparison)
}

function Assert-NoReparsePathComponents {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Purpose
    )

    $fullPath = [System.IO.Path]::TrimEndingDirectorySeparator(
        [System.IO.Path]::GetFullPath($Path))
    $current = [System.IO.DirectoryInfo]::new($fullPath)
    while ($null -ne $current) {
        if ($current.Exists -and
            ($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            $resolvedTarget = $null
            try {
                $target = $current.ResolveLinkTarget($true)
                if ($null -ne $target) {
                    $resolvedTarget = [System.IO.Path]::TrimEndingDirectorySeparator(
                        [System.IO.Path]::GetFullPath($target.FullName))
                }
            }
            catch {
                Exit-InvalidInput "$Purpose traverses a reparse point whose physical target could not be resolved: $($current.FullName). $($_.Exception.Message)"
            }

            if (-not [string]::IsNullOrWhiteSpace($resolvedTarget) -and
                (Test-IsWorkshopPath $resolvedTarget)) {
                Exit-InvalidInput "$Purpose traverses a reparse point into Steam Workshop content: $($current.FullName) -> $resolvedTarget"
            }

            Exit-InvalidInput "$Purpose traverses a reparse point and is unsafe for integration-test staging: $($current.FullName) -> $resolvedTarget"
        }

        $current = $current.Parent
    }

    return $fullPath
}

function Write-Result {
    param([pscustomobject]$Result)

    if ($Output -eq 'json') {
        $Result | ConvertTo-Json -Compress -Depth 6
        return
    }

    $rows = @($Result.Projects | Select-Object OwnerPackageId, Project, Assembly, Destination)
    if ($rows.Count -gt 0) {
        $rows | Format-Table -AutoSize
    }
    Write-Output ("Status: {0}; projects: {1}; artifacts: {2}" -f
        $Result.Status,
        $Result.ProjectCount,
        $Result.ArtifactsModsRoot)
}

function Get-IntegrationTestProjects {
    param([string]$RepositoryRoot)

    $ignoredDirectoryNames = @('.git', '.vs', 'artifacts', 'bin', 'obj')
    $projectFiles = @(Get-ChildItem -LiteralPath $RepositoryRoot -Filter '*.csproj' -File -Recurse -Force |
        Where-Object {
            $relative = [System.IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName)
            $segments = @($relative -split '[\\/]')
            @($segments | Where-Object { $ignoredDirectoryNames -ccontains $_ }).Count -eq 0
        } |
        Sort-Object FullName)
    $projects = [System.Collections.Generic.List[object]]::new()

    foreach ($projectFile in $projectFiles) {
        try {
            [xml]$projectXml = [System.IO.File]::ReadAllText($projectFile.FullName)
        }
        catch {
            Exit-InvalidInput "Project XML could not be read: $($projectFile.FullName). $($_.Exception.Message)"
        }

        $optInNodes = @($projectXml.SelectNodes("//*[local-name()='RimWorldInGameIntegrationTest']") |
            Where-Object { $_.InnerText.Trim() -ceq 'true' })
        if ($optInNodes.Count -eq 0) {
            continue
        }

        if ($optInNodes.Count -ne 1) {
            Exit-InvalidInput "Project has duplicate RimWorldInGameIntegrationTest opt-ins: $($projectFile.FullName)"
        }

        $ownerNodes = @($projectXml.SelectNodes("//*[local-name()='RimWorldIntegrationTestOwnerPackageId']"))
        if ($ownerNodes.Count -ne 1) {
            Exit-InvalidInput "Opt-in project must declare exactly one RimWorldIntegrationTestOwnerPackageId: $($projectFile.FullName)"
        }

        $ownerPackageId = $ownerNodes[0].InnerText.Trim()
        if (-not (Test-PackageId $ownerPackageId)) {
            Exit-InvalidInput "Invalid integration-test owner package ID '$ownerPackageId' in $($projectFile.FullName)"
        }

        $assemblyNameNodes = @($projectXml.SelectNodes("//*[local-name()='AssemblyName']"))
        if ($assemblyNameNodes.Count -ne 1) {
            Exit-InvalidInput "Opt-in project must declare exactly one literal AssemblyName: $($projectFile.FullName)"
        }
        $assemblyName = $assemblyNameNodes[0].InnerText.Trim()
        if ([string]::IsNullOrWhiteSpace($assemblyName) -or
            $assemblyName.Length -gt 256 -or
            $assemblyName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
            Exit-InvalidInput "Integration-test AssemblyName must be one safe literal: $($projectFile.FullName)"
        }

        $sourceManifestPath = Join-Path `
            $projectFile.DirectoryName `
            ($assemblyName + '.integrationtests.json')
        if (-not (Test-Path -LiteralPath $sourceManifestPath -PathType Leaf)) {
            Exit-InvalidInput "Opt-in project source manifest does not exist: $sourceManifestPath"
        }
        $sourceManifest = Read-IntegrationTestManifest -Path $sourceManifestPath
        if ([string]$sourceManifest.ownerPackageId -ine $ownerPackageId) {
            Exit-InvalidInput "Source integration-test manifest ownerPackageId '$($sourceManifest.ownerPackageId)' does not match project owner '$ownerPackageId': $sourceManifestPath"
        }
        $expectedAssemblyFileName = $assemblyName + '.dll'
        if ([string]$sourceManifest.assembly -cne $expectedAssemblyFileName) {
            Exit-InvalidInput "Source integration-test manifest assembly '$($sourceManifest.assembly)' does not match project AssemblyName '$expectedAssemblyFileName': $sourceManifestPath"
        }

        $projects.Add([pscustomobject]@{
            OwnerPackageId = $ownerPackageId
            AssemblyName = $assemblyName
            ProjectPath = $projectFile.FullName
            RelativeProjectPath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $projectFile.FullName)
            SourceManifestPath = $sourceManifestPath
            RequiredPackageIds = @($sourceManifest.requiredPackageIds)
            ForbiddenPackageIds = @($sourceManifest.forbiddenPackageIds)
            ActivePackageSetMode = $sourceManifest.activePackageSetMode
            ActivePackageIds = @($sourceManifest.activePackageIds)
        })
    }

    return @($projects |
        Sort-Object @{ Expression = { $_.OwnerPackageId }; Ascending = $true },
            @{ Expression = { $_.RelativeProjectPath }; Ascending = $true })
}

function Test-IntegrationTestProjectMatrix {
    param(
        [Parameter(Mandatory)][object]$Project,
        [Parameter(Mandatory)][System.Collections.Generic.HashSet[string]]$ActivePackageIdSet,
        [Parameter(Mandatory)][string[]]$OrderedActivePackageIds
    )

    if ([string]$Project.ActivePackageSetMode -ceq 'exact') {
        $expected = @($Project.ActivePackageIds)
        if ($expected.Count -ne $OrderedActivePackageIds.Count) {
            return $false
        }
        for ($index = 0; $index -lt $expected.Count; $index++) {
            if ([string]$expected[$index] -ine [string]$OrderedActivePackageIds[$index]) {
                return $false
            }
        }

        return $true
    }

    foreach ($requiredPackageId in @($Project.RequiredPackageIds)) {
        if (-not $ActivePackageIdSet.Contains([string]$requiredPackageId)) {
            return $false
        }
    }
    foreach ($forbiddenPackageId in @($Project.ForbiddenPackageIds)) {
        if ($ActivePackageIdSet.Contains([string]$forbiddenPackageId)) {
            return $false
        }
    }

    return $true
}

function Test-PackageIdMatrixEqual {
    param(
        [AllowNull()][object[]]$Expected,
        [AllowNull()][object[]]$Actual
    )

    $expectedSet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($packageId in @($Expected)) {
        if ($null -ne $packageId -and -not [string]::IsNullOrWhiteSpace([string]$packageId)) {
            $null = $expectedSet.Add([string]$packageId)
        }
    }
    $actualSet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($packageId in @($Actual)) {
        if ($null -ne $packageId -and -not [string]::IsNullOrWhiteSpace([string]$packageId)) {
            $null = $actualSet.Add([string]$packageId)
        }
    }

    return $expectedSet.SetEquals($actualSet)
}

function Test-PackageIdSequenceEqual {
    param(
        [AllowNull()][object[]]$Expected,
        [AllowNull()][object[]]$Actual
    )

    $expectedValues = @($Expected | Where-Object { $null -ne $_ } | ForEach-Object { [string]$_ })
    $actualValues = @($Actual | Where-Object { $null -ne $_ } | ForEach-Object { [string]$_ })
    if ($expectedValues.Count -ne $actualValues.Count) {
        return $false
    }
    for ($index = 0; $index -lt $expectedValues.Count; $index++) {
        if ($expectedValues[$index] -ine $actualValues[$index]) {
            return $false
        }
    }

    return $true
}

function Invoke-DotNetCommand {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    $commandOutput = @(& dotnet @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $detail = ($commandOutput | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        throw "$Description failed with exit code $exitCode.$([Environment]::NewLine)$detail"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Lines = @($commandOutput | ForEach-Object { $_.ToString() })
    }
}

function Resolve-TargetPath {
    param(
        [pscustomobject]$Project,
        [string]$Configuration,
        [string]$RimWorldVersion,
        [string]$ResolvedRimWorldPath,
        [string]$ResolvedSteamModContentFolder
    )

    $arguments = @(
        'msbuild',
        $Project.ProjectPath,
        '-nologo',
        '-getProperty:TargetPath',
        "-property:Configuration=$Configuration",
        "-property:RimWorldVersion=$RimWorldVersion",
        "-property:DefaultRimWorldPath=$ResolvedRimWorldPath",
        "-property:DefaultSteamModContentFolder=$ResolvedSteamModContentFolder"
    )
    $result = Invoke-DotNetCommand `
        -Arguments $arguments `
        -Description "TargetPath query for '$($Project.RelativeProjectPath)'"
    $nonEmptyLines = @($result.Lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($nonEmptyLines.Count -ne 1) {
        throw "TargetPath query for '$($Project.RelativeProjectPath)' returned $($nonEmptyLines.Count) non-empty lines; expected exactly one."
    }

    $targetPathText = $nonEmptyLines[0].Trim().Trim('"')
    if ([string]::IsNullOrWhiteSpace($targetPathText)) {
        throw "TargetPath query for '$($Project.RelativeProjectPath)' returned an empty path."
    }

    if ([System.IO.Path]::IsPathRooted($targetPathText)) {
        return [System.IO.Path]::GetFullPath($targetPathText)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $Project.ProjectPath) $targetPathText))
}

function Read-IntegrationTestManifest {
    param([string]$Path)

    $maximumManifestBytes = 64 * 1024
    $manifestFile = Get-Item -LiteralPath $Path -Force
    if (($manifestFile.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $manifestFile.Length -le 0 -or
        $manifestFile.Length -gt $maximumManifestBytes) {
        Exit-InvalidInput "Integration-test manifest is linked, empty, or exceeds its $maximumManifestBytes byte limit: $Path"
    }

    $manifestBytes = $null
    $stream = [System.IO.FileStream]::new(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read,
        16 * 1024,
        [System.IO.FileOptions]::SequentialScan)
    try {
        if ($stream.Length -le 0 -or
            $stream.Length -gt $maximumManifestBytes -or
            $stream.Length -gt [int]::MaxValue) {
            Exit-InvalidInput "Integration-test manifest is empty or exceeds its $maximumManifestBytes byte limit: $Path"
        }

        $manifestBytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $manifestBytes.Length) {
            $read = $stream.Read($manifestBytes, $offset, $manifestBytes.Length - $offset)
            if ($read -eq 0) {
                Exit-InvalidInput "Integration-test manifest changed while it was read: $Path"
            }

            $offset += $read
        }

        if ($stream.ReadByte() -ne -1) {
            Exit-InvalidInput "Integration-test manifest grew beyond its $maximumManifestBytes byte limit while it was read: $Path"
        }
    }
    finally {
        $stream.Dispose()
    }

    try {
        $manifestJson = [System.Text.UTF8Encoding]::new($false, $true).GetString($manifestBytes)
    }
    catch {
        Exit-InvalidInput "Integration-test manifest is not strict UTF-8: $Path. $($_.Exception.Message)"
    }

    $document = $null
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($manifestJson)
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            Exit-InvalidInput "Integration-test manifest must be a JSON object: $Path"
        }

        $propertyNames = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::Ordinal)
        $values = @{
            ownerPackageId = $null
            assembly = $null
            requiredPackageIds = $null
            forbiddenPackageIds = $null
            activePackageSetMode = $null
            activePackageIds = $null
        }
        foreach ($property in $document.RootElement.EnumerateObject()) {
            if (-not $propertyNames.Add($property.Name)) {
                Exit-InvalidInput "Integration-test manifest contains duplicate JSON property '$($property.Name)': $Path"
            }
            if (-not $values.ContainsKey($property.Name)) {
                Exit-InvalidInput "Integration-test manifest contains unknown JSON property '$($property.Name)': $Path"
            }

            if ($property.Name -ceq 'ownerPackageId' -or
                $property.Name -ceq 'assembly' -or
                $property.Name -ceq 'activePackageSetMode') {
                if ($property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    Exit-InvalidInput "Integration-test manifest property '$($property.Name)' must be a string: $Path"
                }

                $values[$property.Name] = $property.Value.GetString()
                continue
            }

            if ($property.Name -cne 'requiredPackageIds' -and
                $property.Name -cne 'forbiddenPackageIds' -and
                $property.Name -cne 'activePackageIds') {
                continue
            }

            if ($property.Value.ValueKind -eq [System.Text.Json.JsonValueKind]::Null) {
                continue
            }

            if ($property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
                Exit-InvalidInput "Integration-test manifest property '$($property.Name)' must be an array of package-ID strings: $Path"
            }

            if ($property.Value.GetArrayLength() -gt 64) {
                Exit-InvalidInput "Integration-test manifest property '$($property.Name)' exceeds the runtime limit of 64 package IDs: $Path"
            }

            $packageIds = [System.Collections.Generic.List[string]]::new()
            $uniquePackageIds = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::OrdinalIgnoreCase)
            foreach ($packageElement in $property.Value.EnumerateArray()) {
                if ($packageElement.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    Exit-InvalidInput "Integration-test manifest property '$($property.Name)' must contain only package-ID strings: $Path"
                }

                $packageId = $packageElement.GetString()
                if (-not (Test-PackageId $packageId)) {
                    Exit-InvalidInput "Integration-test manifest property '$($property.Name)' contains an invalid package ID '$packageId': $Path"
                }

                if (-not $uniquePackageIds.Add($packageId)) {
                    Exit-InvalidInput "Integration-test manifest property '$($property.Name)' contains a duplicate package ID '$packageId': $Path"
                }

                $packageIds.Add($packageId)
            }

            $values[$property.Name] = @($packageIds)
        }

        $requiredPackageSet = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
        if ($null -ne $values.requiredPackageIds) {
            foreach ($packageId in $values.requiredPackageIds) {
                $null = $requiredPackageSet.Add($packageId)
            }
        }
        if ($null -ne $values.forbiddenPackageIds) {
            foreach ($packageId in $values.forbiddenPackageIds) {
                if ($requiredPackageSet.Contains($packageId)) {
                    Exit-InvalidInput "Integration-test manifest package ID '$packageId' cannot be both required and forbidden: $Path"
                }
            }
        }

        $hasExactMode = $null -ne $values.activePackageSetMode
        $hasExactSet = $null -ne $values.activePackageIds
        if ($hasExactMode -ne $hasExactSet) {
            Exit-InvalidInput "Integration-test manifest activePackageSetMode and activePackageIds must be declared together: $Path"
        }
        if ($hasExactMode) {
            if ([string]$values.activePackageSetMode -cne 'exact') {
                Exit-InvalidInput "Integration-test manifest activePackageSetMode must be the exact literal 'exact': $Path"
            }
            if (@($values.activePackageIds).Count -eq 0) {
                Exit-InvalidInput "Integration-test manifest exact activePackageIds must be nonempty: $Path"
            }
            if (($null -ne $values.requiredPackageIds -and @($values.requiredPackageIds).Count -gt 0) -or
                ($null -ne $values.forbiddenPackageIds -and @($values.forbiddenPackageIds).Count -gt 0)) {
                Exit-InvalidInput "Integration-test manifest exact active package set cannot also declare required or forbidden package IDs: $Path"
            }
        }

        return [pscustomobject]$values
    }
    catch {
        Exit-InvalidInput "Integration-test manifest is not valid JSON: $Path. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $document) {
            $document.Dispose()
        }
    }

}

function Assert-SafeStageDirectory {
    param(
        [string]$Destination,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $expected = [System.IO.Path]::GetFullPath((Join-Path $ArtifactsRoot "$OwnerPackageId\$Version\DevIntegrationTests"))
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    if (-not [string]::Equals($Destination, $expected, $comparison) -or
        -not (Test-IsChildPath -Candidate $Destination -Parent $ArtifactsRoot)) {
        Exit-InvalidInput "Refusing integration-test cleanup outside its exact owned stage directory: $Destination"
    }

    $null = Assert-NoReparsePathComponents `
        -Path $ArtifactsRoot `
        -Purpose 'ArtifactsModsRoot'
    $null = Assert-NoReparsePathComponents `
        -Path $Destination `
        -Purpose "Integration-test stage for '$OwnerPackageId'"
}

function Assert-SafeStageSiblingDirectory {
    param(
        [string]$Destination,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version,
        [ValidateSet('stage', 'backup')]
        [string]$Kind
    )

    $versionDirectory = [System.IO.Path]::GetFullPath((Join-Path $ArtifactsRoot "$OwnerPackageId\$Version"))
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    $candidate = [System.IO.Path]::GetFullPath($Destination)
    $name = [System.IO.Path]::GetFileName($candidate)
    $expectedPrefix = ".DevIntegrationTests.$Kind."
    $suffix = if ($name.StartsWith($expectedPrefix, [System.StringComparison]::Ordinal)) {
        $name.Substring($expectedPrefix.Length)
    }
    else {
        ''
    }
    if (-not [string]::Equals((Split-Path -Parent $candidate), $versionDirectory, $comparison) -or
        $suffix -cnotmatch '^[0-9a-f]{32}$' -or
        -not (Test-IsChildPath -Candidate $candidate -Parent $ArtifactsRoot)) {
        Exit-InvalidInput "Refusing integration-test cleanup outside its exact owned $Kind directory: $Destination"
    }

    $null = Assert-NoReparsePathComponents `
        -Path $ArtifactsRoot `
        -Purpose 'ArtifactsModsRoot'
    $null = Assert-NoReparsePathComponents `
        -Path $candidate `
        -Purpose "Integration-test $Kind stage for '$OwnerPackageId'"
}

function Get-StageOwnershipText {
    param(
        [string]$OwnerPackageId,
        [string]$Version
    )

    return "RimWorldInGameIntegrationTests/v1|$OwnerPackageId|$Version"
}

function Assert-StageOwnership {
    param(
        [string]$Destination,
        [string]$OwnerPackageId,
        [string]$Version,
        [switch]$ThrowOnFailure
    )

    $markerPath = Join-Path $Destination '.rimworld-integration-test-stage.owner'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        $message = "Refusing to clean an integration-test stage without its ownership marker: $Destination"
        if ($ThrowOnFailure) {
            throw $message
        }
        Exit-InvalidInput $message
    }

    $marker = Get-Item -LiteralPath $markerPath -Force
    if (($marker.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $marker.Length -le 0 -or
        $marker.Length -gt 1024) {
        $message = "Refusing to clean an integration-test stage with an invalid ownership marker: $Destination"
        if ($ThrowOnFailure) {
            throw $message
        }
        Exit-InvalidInput $message
    }

    $expectedOwnership = Get-StageOwnershipText `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    $actualOwnership = [System.IO.File]::ReadAllText($markerPath)
    if ($actualOwnership -cne $expectedOwnership) {
        $message = "Refusing to clean an integration-test stage whose ownership marker does not match its owner and version: $Destination"
        if ($ThrowOnFailure) {
            throw $message
        }
        Exit-InvalidInput $message
    }
}

function Write-StageOwnership {
    param(
        [string]$Destination,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $markerPath = Join-Path $Destination '.rimworld-integration-test-stage.owner'
    $ownership = Get-StageOwnershipText `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    [System.IO.File]::WriteAllText(
        $markerPath,
        $ownership,
        [System.Text.UTF8Encoding]::new($false))
}

function Get-StageTransactionOwnershipText {
    param(
        [string]$OwnerPackageId,
        [string]$Version,
        [string]$TransactionId
    )

    return "RimWorldInGameIntegrationTests/transaction-v1|$OwnerPackageId|$Version|$TransactionId"
}

function Assert-SafeStageTransactionMarker {
    param(
        [string]$Path,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $versionDirectory = [System.IO.Path]::GetFullPath(
        (Join-Path $ArtifactsRoot "$OwnerPackageId\$Version"))
    $candidate = [System.IO.Path]::GetFullPath($Path)
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    $name = [System.IO.Path]::GetFileName($candidate)
    if ($name -cnotmatch '^\.DevIntegrationTests\.transaction\.([0-9a-f]{32})\.owner$') {
        Exit-InvalidInput "Refusing an invalid integration-test transaction marker path: $Path"
    }
    $transactionId = $Matches[1]
    if (-not [string]::Equals((Split-Path -Parent $candidate), $versionDirectory, $comparison) -or
        -not (Test-IsChildPath -Candidate $candidate -Parent $ArtifactsRoot)) {
        Exit-InvalidInput "Refusing an integration-test transaction marker outside its exact owner/version directory: $Path"
    }

    $null = Assert-NoReparsePathComponents `
        -Path $ArtifactsRoot `
        -Purpose 'ArtifactsModsRoot'
    $null = Assert-NoReparsePathComponents `
        -Path $candidate `
        -Purpose "Integration-test transaction marker for '$OwnerPackageId'"
    if (Test-Path -LiteralPath $candidate) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            Exit-InvalidInput "Integration-test transaction marker is not a file: $candidate"
        }

        $marker = Get-Item -LiteralPath $candidate -Force
        if (($marker.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
            $marker.Length -le 0 -or
            $marker.Length -gt 1024) {
            Exit-InvalidInput "Integration-test transaction marker is linked, empty, or oversized: $candidate"
        }
    }

    return $transactionId
}

function Assert-StageTransactionOwnership {
    param(
        [string]$Path,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $transactionId = Assert-SafeStageTransactionMarker `
        -Path $Path `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Exit-InvalidInput "Integration-test transaction marker does not exist: $Path"
    }

    $expected = Get-StageTransactionOwnershipText `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version `
        -TransactionId $transactionId
    $actual = [System.IO.File]::ReadAllText($Path)
    if ($actual -cne $expected) {
        Exit-InvalidInput "Integration-test transaction marker ownership does not match its owner, version, and ID: $Path"
    }

    return $transactionId
}

function Write-DurableStageTransactionMarker {
    param(
        [string]$Path,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $transactionId = Assert-SafeStageTransactionMarker `
        -Path $Path `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    $text = Get-StageTransactionOwnershipText `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version `
        -TransactionId $transactionId
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($text)
    $stream = [System.IO.FileStream]::new(
        $Path,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None,
        4096,
        [System.IO.FileOptions]::WriteThrough)
    try {
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
    }
    finally {
        $stream.Dispose()
    }

    $null = Assert-StageTransactionOwnership `
        -Path $Path `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
}

function Remove-OwnedStageTransactionMarker {
    param(
        [string]$Path,
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $null = Assert-StageTransactionOwnership `
        -Path $Path `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    Remove-Item -LiteralPath $Path -Force
    if (Test-Path -LiteralPath $Path) {
        throw "Integration-test transaction marker remained after cleanup: $Path"
    }
}

function Recover-InterruptedStageTransaction {
    param(
        [string]$ArtifactsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $versionDirectory = [System.IO.Path]::GetFullPath(
        (Join-Path $ArtifactsRoot "$OwnerPackageId\$Version"))
    $null = Assert-NoReparsePathComponents `
        -Path $versionDirectory `
        -Purpose "Integration-test version directory for '$OwnerPackageId'"
    if (-not (Test-Path -LiteralPath $versionDirectory)) {
        return
    }
    if (-not (Test-Path -LiteralPath $versionDirectory -PathType Container)) {
        Exit-InvalidInput "Integration-test version path is not a directory: $versionDirectory"
    }

    $transactionEntries = @(Get-ChildItem -LiteralPath $versionDirectory -Force |
        Where-Object {
            $_.Name.StartsWith('.DevIntegrationTests.transaction.', [System.StringComparison]::Ordinal)
        })
    if ($transactionEntries.Count -gt 1) {
        Exit-InvalidInput "Multiple interrupted integration-test transactions require manual inspection for '$OwnerPackageId': $versionDirectory"
    }
    if ($transactionEntries.Count -eq 0) {
        return
    }

    $markerPath = $transactionEntries[0].FullName
    $transactionId = Assert-StageTransactionOwnership `
        -Path $markerPath `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    $live = Join-Path $versionDirectory 'DevIntegrationTests'
    $temporary = Join-Path $versionDirectory ".DevIntegrationTests.stage.$transactionId"
    $backup = Join-Path $versionDirectory ".DevIntegrationTests.backup.$transactionId"

    Assert-SafeStageDirectory `
        -Destination $live `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
    Assert-SafeStageSiblingDirectory `
        -Destination $temporary `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version `
        -Kind stage
    Assert-SafeStageSiblingDirectory `
        -Destination $backup `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version `
        -Kind backup

    foreach ($stage in @($live, $temporary, $backup)) {
        if (-not (Test-Path -LiteralPath $stage)) {
            continue
        }
        if (-not (Test-Path -LiteralPath $stage -PathType Container)) {
            Exit-InvalidInput "Interrupted integration-test transaction path is not a directory: $stage"
        }
        Assert-StageOwnership `
            -Destination $stage `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
    }

    if (-not (Test-Path -LiteralPath $live -PathType Container) -and
        -not (Test-Path -LiteralPath $temporary -PathType Container) -and
        -not (Test-Path -LiteralPath $backup -PathType Container)) {
        Exit-InvalidInput "Interrupted integration-test transaction has no recoverable live, temporary, or backup stage; retaining its marker for manual inspection: $markerPath"
    }

    if (-not (Test-Path -LiteralPath $live) -and
        (Test-Path -LiteralPath $backup -PathType Container)) {
        Assert-SafeStageSiblingDirectory `
            -Destination $backup `
            -ArtifactsRoot $ArtifactsRoot `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version `
            -Kind backup
        Assert-StageOwnership `
            -Destination $backup `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
        Assert-SafeStageDirectory `
            -Destination $live `
            -ArtifactsRoot $ArtifactsRoot `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
        if (-not [string]::IsNullOrWhiteSpace($TestCreateRecoveryLiveDestinationOwner) -and
            $OwnerPackageId -ieq $TestCreateRecoveryLiveDestinationOwner) {
            $null = New-Item -Path $live -ItemType Directory
            [System.IO.File]::WriteAllText(
                (Join-Path $live 'foreign-destination.txt'),
                'foreign destination',
                [System.Text.UTF8Encoding]::new($false))
        }
        Assert-SafeStageDirectory `
            -Destination $live `
            -ArtifactsRoot $ArtifactsRoot `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
        if (Test-Path -LiteralPath $live) {
            throw "Integration-test live destination appeared before interrupted transaction recovery: $live"
        }
        [System.IO.Directory]::Move($backup, $live)
    }

    if (Test-Path -LiteralPath $temporary -PathType Container) {
        Assert-SafeStageSiblingDirectory `
            -Destination $temporary `
            -ArtifactsRoot $ArtifactsRoot `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version `
            -Kind stage
        Assert-StageOwnership `
            -Destination $temporary `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }

    if (Test-Path -LiteralPath $backup -PathType Container) {
        Assert-SafeStageSiblingDirectory `
            -Destination $backup `
            -ArtifactsRoot $ArtifactsRoot `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version `
            -Kind backup
        Assert-StageOwnership `
            -Destination $backup `
            -OwnerPackageId $OwnerPackageId `
            -Version $Version
        Remove-Item -LiteralPath $backup -Recurse -Force
    }

    Remove-OwnedStageTransactionMarker `
        -Path $markerPath `
        -ArtifactsRoot $ArtifactsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version
}

try {
    if ($Configuration -cnotin @('Debug', 'Release')) {
        Exit-InvalidInput "Configuration must be 'Debug' or 'Release': '$Configuration'"
    }

    if ($Output -cnotin @('table', 'json')) {
        Exit-InvalidInput "Output must be 'table' or 'json': '$Output'"
    }

    if (-not [string]::IsNullOrWhiteSpace($TestFailCommitAfterBackupOwner) -and
        -not (Test-PackageId $TestFailCommitAfterBackupOwner)) {
        Exit-InvalidInput "TestFailCommitAfterBackupOwner must be a valid package ID: '$TestFailCommitAfterBackupOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestCrashCommitAfterBackupOwner) -and
        -not (Test-PackageId $TestCrashCommitAfterBackupOwner)) {
        Exit-InvalidInput "TestCrashCommitAfterBackupOwner must be a valid package ID: '$TestCrashCommitAfterBackupOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestCreateRollbackDestinationOwner) -and
        -not (Test-PackageId $TestCreateRollbackDestinationOwner)) {
        Exit-InvalidInput "TestCreateRollbackDestinationOwner must be a valid package ID: '$TestCreateRollbackDestinationOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestDeleteBackupBeforeRollbackOwner) -and
        -not (Test-PackageId $TestDeleteBackupBeforeRollbackOwner)) {
        Exit-InvalidInput "TestDeleteBackupBeforeRollbackOwner must be a valid package ID: '$TestDeleteBackupBeforeRollbackOwner'"
    }
    foreach ($faultOwner in @(
            $TestCreateCommitBackupDestinationOwner,
            $TestCreatePublishLiveDestinationOwner,
            $TestCreateRecoveryLiveDestinationOwner,
            $TestReplaceTemporaryWithUnownedOwner)) {
        if (-not [string]::IsNullOrWhiteSpace($faultOwner) -and
            -not (Test-PackageId $faultOwner)) {
            Exit-InvalidInput "Transaction fault owner must be a valid package ID: '$faultOwner'"
        }
    }

    if ([string]::IsNullOrWhiteSpace($RimWorldVersion) -or $RimWorldVersion -cnotmatch '^[0-9]+(?:\.[0-9]+)*$') {
        Exit-InvalidInput "RimWorldVersion must contain dot-separated decimal components: '$RimWorldVersion'"
    }

    $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    if (Test-IsWorkshopPath $repositoryRoot) {
        Exit-InvalidInput "Repository source beneath Steam Workshop is read-only and cannot be used for integration-test builds: $repositoryRoot"
    }

    if ([string]::IsNullOrWhiteSpace($RimWorldPath) -or
        -not (Test-Path -LiteralPath $RimWorldPath -PathType Container)) {
        Exit-InvalidInput "RimWorldPath does not exist: $RimWorldPath"
    }

    if ([string]::IsNullOrWhiteSpace($SteamModContentFolder) -or
        -not (Test-Path -LiteralPath $SteamModContentFolder -PathType Container)) {
        Exit-InvalidInput "SteamModContentFolder does not exist: $SteamModContentFolder"
    }

    $resolvedRimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
    $resolvedSteamModContentFolder = (Resolve-Path -LiteralPath $SteamModContentFolder).Path
    $rimWorldManagedAssembly = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'
    if (-not (Test-Path -LiteralPath $rimWorldManagedAssembly -PathType Leaf)) {
        Exit-InvalidInput "RimWorldPath does not contain RimWorld's managed Assembly-CSharp.dll: $resolvedRimWorldPath"
    }

    $resolvedArtifactsModsRoot = if ([string]::IsNullOrWhiteSpace($ArtifactsModsRoot)) {
        [System.IO.Path]::TrimEndingDirectorySeparator(
            [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts\InGameIntegrationTests\StagingMods')))
    }
    else {
        [System.IO.Path]::TrimEndingDirectorySeparator(
            [System.IO.Path]::GetFullPath($ArtifactsModsRoot))
    }

    if (Test-IsWorkshopPath $resolvedArtifactsModsRoot) {
        Exit-InvalidInput "ArtifactsModsRoot must never target Steam Workshop content: $resolvedArtifactsModsRoot"
    }
    $null = Assert-NoReparsePathComponents `
        -Path $resolvedArtifactsModsRoot `
        -Purpose 'ArtifactsModsRoot'

    $activePackageIdSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $orderedActivePackageIds = [System.Collections.Generic.List[string]]::new()
    if (-not $PSBoundParameters.ContainsKey('ActivePackageIds') -or
        $null -eq $ActivePackageIds -or
        @($ActivePackageIds).Count -eq 0) {
        Exit-InvalidInput 'At least one active package ID is required; ActivePackageIds must provide the nonempty complete active package set.'
    }
    foreach ($packageId in @($ActivePackageIds)) {
        if (-not (Test-PackageId $packageId)) {
            Exit-InvalidInput "Invalid active package ID: '$packageId'"
        }

        if (-not $activePackageIdSet.Add($packageId)) {
            Exit-InvalidInput "Duplicate active package ID: '$packageId'"
        }
        $orderedActivePackageIds.Add($packageId)
    }

    $allProjects = @(Get-IntegrationTestProjects -RepositoryRoot $repositoryRoot)
    $selectedProjects = @($allProjects | Where-Object {
            $activePackageIdSet.Contains($_.OwnerPackageId) -and
            (Test-IntegrationTestProjectMatrix `
                -Project $_ `
                -ActivePackageIdSet $activePackageIdSet `
                -OrderedActivePackageIds @($orderedActivePackageIds))
        })

    if ($selectedProjects.Count -eq 0) {
        Exit-InvalidInput "No opt-in in-game integration-test project matches the complete active-package matrix: $($orderedActivePackageIds -join ', ')"
    }

    if (-not [string]::IsNullOrWhiteSpace($TestFailCommitAfterBackupOwner) -and
        @($selectedProjects | Where-Object {
            [string]$_.OwnerPackageId -ieq $TestFailCommitAfterBackupOwner
        }).Count -eq 0) {
        Exit-InvalidInput "TestFailCommitAfterBackupOwner does not match a selected integration-test owner: '$TestFailCommitAfterBackupOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestCrashCommitAfterBackupOwner) -and
        @($selectedProjects | Where-Object {
            [string]$_.OwnerPackageId -ieq $TestCrashCommitAfterBackupOwner
        }).Count -eq 0) {
        Exit-InvalidInput "TestCrashCommitAfterBackupOwner does not match a selected integration-test owner: '$TestCrashCommitAfterBackupOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestCreateRollbackDestinationOwner) -and
        @($selectedProjects | Where-Object {
            [string]$_.OwnerPackageId -ieq $TestCreateRollbackDestinationOwner
        }).Count -eq 0) {
        Exit-InvalidInput "TestCreateRollbackDestinationOwner does not match a selected integration-test owner: '$TestCreateRollbackDestinationOwner'"
    }
    if (-not [string]::IsNullOrWhiteSpace($TestDeleteBackupBeforeRollbackOwner) -and
        @($selectedProjects | Where-Object {
            [string]$_.OwnerPackageId -ieq $TestDeleteBackupBeforeRollbackOwner
        }).Count -eq 0) {
        Exit-InvalidInput "TestDeleteBackupBeforeRollbackOwner does not match a selected integration-test owner: '$TestDeleteBackupBeforeRollbackOwner'"
    }
    foreach ($faultOwner in @(
            $TestCreateCommitBackupDestinationOwner,
            $TestCreatePublishLiveDestinationOwner,
            $TestCreateRecoveryLiveDestinationOwner,
            $TestReplaceTemporaryWithUnownedOwner)) {
        if (-not [string]::IsNullOrWhiteSpace($faultOwner) -and
            @($selectedProjects | Where-Object {
                [string]$_.OwnerPackageId -ieq $faultOwner
            }).Count -eq 0) {
            Exit-InvalidInput "Transaction fault owner does not match a selected integration-test owner: '$faultOwner'"
        }
    }

    $plannedProjects = @($selectedProjects | ForEach-Object {
        $destination = [System.IO.Path]::GetFullPath((Join-Path $resolvedArtifactsModsRoot "$($_.OwnerPackageId)\$RimWorldVersion\DevIntegrationTests"))
        if (-not (Test-IsChildPath -Candidate $destination -Parent $resolvedArtifactsModsRoot)) {
            Exit-InvalidInput "Integration-test destination escaped ArtifactsModsRoot: $destination"
        }

        [pscustomobject]@{
            OwnerPackageId = $_.OwnerPackageId
            Project = $_.RelativeProjectPath.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
            Assembly = $null
            Manifest = $null
            Destination = $destination
        }
    })

    if ($DryRun) {
        Write-Result ([pscustomobject]@{
            Status = 'planned'
            DryRun = $true
            Configuration = $Configuration
            RimWorldVersion = $RimWorldVersion
            ArtifactsModsRoot = $resolvedArtifactsModsRoot
            ProjectCount = $plannedProjects.Count
            Projects = $plannedProjects
        })
        exit 0
    }

    $recoveryOwnerPackageIds = @($selectedProjects |
        ForEach-Object { [string]$_.OwnerPackageId } |
        Sort-Object -Unique)
    foreach ($recoveryOwnerPackageId in $recoveryOwnerPackageIds) {
        Recover-InterruptedStageTransaction `
            -ArtifactsRoot $resolvedArtifactsModsRoot `
            -OwnerPackageId $recoveryOwnerPackageId `
            -Version $RimWorldVersion
    }

    $null = Get-Command dotnet -ErrorAction Stop
    foreach ($selectedProject in $selectedProjects) {
        $buildArguments = @(
            'build',
            $selectedProject.ProjectPath,
            '--configuration', $Configuration,
            '--nologo',
            '-p:BuildProjectReferences=true',
            "-p:RimWorldVersion=$RimWorldVersion",
            "-p:DefaultRimWorldPath=$resolvedRimWorldPath",
            "-p:DefaultSteamModContentFolder=$resolvedSteamModContentFolder"
        )
        $null = Invoke-DotNetCommand `
            -Arguments $buildArguments `
            -Description "Build for '$($selectedProject.RelativeProjectPath)'"
    }

    $bundles = [System.Collections.Generic.List[object]]::new()
    $destinationFiles = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($selectedProject in $selectedProjects) {
        $targetPath = Resolve-TargetPath `
            -Project $selectedProject `
            -Configuration $Configuration `
            -RimWorldVersion $RimWorldVersion `
            -ResolvedRimWorldPath $resolvedRimWorldPath `
            -ResolvedSteamModContentFolder $resolvedSteamModContentFolder
        if (-not (Test-IsChildPath -Candidate $targetPath -Parent $repositoryRoot)) {
            Exit-InvalidInput "TargetPath escaped the repository: $targetPath"
        }

        if ([System.IO.Path]::GetExtension($targetPath) -cne '.dll') {
            Exit-InvalidInput "Integration-test TargetPath must be a DLL: $targetPath"
        }

        if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
            throw "Built integration-test assembly does not exist: $targetPath"
        }

        $assemblyFileName = [System.IO.Path]::GetFileName($targetPath)
        if (-not $assemblyFileName.EndsWith(
                '.IntegrationTests.dll',
                [System.StringComparison]::Ordinal)) {
            Exit-InvalidInput "Integration-test assembly must end with '.IntegrationTests.dll': $targetPath"
        }

        $manifestFileName = [System.IO.Path]::GetFileNameWithoutExtension($targetPath) + '.integrationtests.json'
        $manifestPath = Join-Path (Split-Path -Parent $targetPath) $manifestFileName
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            Exit-InvalidInput "Integration-test manifest does not exist beside '$assemblyFileName': $manifestPath"
        }

        $manifest = Read-IntegrationTestManifest -Path $manifestPath

        $manifestPropertyNames = @($manifest.PSObject.Properties.Name)
        if ($manifestPropertyNames -cnotcontains 'ownerPackageId' -or
            [string]::IsNullOrWhiteSpace([string]$manifest.ownerPackageId)) {
            Exit-InvalidInput "Integration-test manifest must contain non-empty ownerPackageId: $manifestPath"
        }

        if ($manifestPropertyNames -cnotcontains 'assembly' -or
            [string]::IsNullOrWhiteSpace([string]$manifest.assembly)) {
            Exit-InvalidInput "Integration-test manifest must contain non-empty assembly: $manifestPath"
        }

        if ([string]$manifest.ownerPackageId -ine $selectedProject.OwnerPackageId) {
            Exit-InvalidInput "Integration-test manifest ownerPackageId '$($manifest.ownerPackageId)' does not match project owner '$($selectedProject.OwnerPackageId)': $manifestPath"
        }

        if ([string]$manifest.assembly -cne $assemblyFileName) {
            Exit-InvalidInput "Integration-test manifest assembly '$($manifest.assembly)' does not match TargetPath assembly '$assemblyFileName': $manifestPath"
        }

        if (-not (Test-PackageIdMatrixEqual `
                -Expected @($selectedProject.RequiredPackageIds) `
                -Actual @($manifest.requiredPackageIds)) -or
            -not (Test-PackageIdMatrixEqual `
                -Expected @($selectedProject.ForbiddenPackageIds) `
                -Actual @($manifest.forbiddenPackageIds)) -or
            [string]$selectedProject.ActivePackageSetMode -cne [string]$manifest.activePackageSetMode -or
            -not (Test-PackageIdSequenceEqual `
                -Expected @($selectedProject.ActivePackageIds) `
                -Actual @($manifest.activePackageIds))) {
            Exit-InvalidInput "Built integration-test manifest matrix does not match its validated prebuild source manifest: $manifestPath"
        }

        $destinationDirectory = [System.IO.Path]::GetFullPath((Join-Path $resolvedArtifactsModsRoot "$($selectedProject.OwnerPackageId)\$RimWorldVersion\DevIntegrationTests"))
        $destinationAssemblyPath = Join-Path $destinationDirectory $assemblyFileName
        $destinationManifestPath = Join-Path $destinationDirectory $manifestFileName
        if (-not $destinationFiles.Add($destinationAssemblyPath) -or
            -not $destinationFiles.Add($destinationManifestPath)) {
            Exit-InvalidInput "Duplicate integration-test bundle destination detected for owner '$($selectedProject.OwnerPackageId)': $assemblyFileName"
        }

        $bundles.Add([pscustomobject]@{
            OwnerPackageId = $selectedProject.OwnerPackageId
            Project = $selectedProject.RelativeProjectPath.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
            SourceAssemblyPath = $targetPath
            SourceManifestPath = $manifestPath
            Assembly = $assemblyFileName
            Manifest = $manifestFileName
            Destination = $destinationDirectory
        })
    }

    $stageGroups = @($bundles |
        Group-Object OwnerPackageId |
        Sort-Object Name)
    foreach ($stageGroup in $stageGroups) {
        $stageDirectory = [string]$stageGroup.Group[0].Destination
        Assert-SafeStageDirectory `
            -Destination $stageDirectory `
            -ArtifactsRoot $resolvedArtifactsModsRoot `
            -OwnerPackageId $stageGroup.Group[0].OwnerPackageId `
            -Version $RimWorldVersion
        if (Test-Path -LiteralPath $stageDirectory) {
            if (-not (Test-Path -LiteralPath $stageDirectory -PathType Container)) {
                Exit-InvalidInput "Integration-test stage path exists but is not a directory: $stageDirectory"
            }

            Assert-StageOwnership `
                -Destination $stageDirectory `
                -OwnerPackageId $stageGroup.Group[0].OwnerPackageId `
                -Version $RimWorldVersion
        }
    }

    $stageTransactions = [System.Collections.Generic.List[object]]::new()
    foreach ($stageGroup in $stageGroups) {
        $stageDirectory = [string]$stageGroup.Group[0].Destination
        $versionDirectory = Split-Path -Parent $stageDirectory
        $transactionId = [Guid]::NewGuid().ToString('N')
        $temporaryDirectory = Join-Path $versionDirectory ".DevIntegrationTests.stage.$transactionId"
        $backupDirectory = Join-Path $versionDirectory ".DevIntegrationTests.backup.$transactionId"
        $transactionMarker = Join-Path $versionDirectory ".DevIntegrationTests.transaction.$transactionId.owner"
        Assert-SafeStageSiblingDirectory `
            -Destination $temporaryDirectory `
            -ArtifactsRoot $resolvedArtifactsModsRoot `
            -OwnerPackageId $stageGroup.Group[0].OwnerPackageId `
            -Version $RimWorldVersion `
            -Kind stage
        Assert-SafeStageSiblingDirectory `
            -Destination $backupDirectory `
            -ArtifactsRoot $resolvedArtifactsModsRoot `
            -OwnerPackageId $stageGroup.Group[0].OwnerPackageId `
            -Version $RimWorldVersion `
            -Kind backup
        $stageTransactions.Add([pscustomobject]@{
            OwnerPackageId = [string]$stageGroup.Group[0].OwnerPackageId
            Live = $stageDirectory
            Temporary = $temporaryDirectory
            Backup = $backupDirectory
            Marker = $transactionMarker
            Bundles = @($stageGroup.Group)
            TemporaryCreated = $false
            MarkerCreated = $false
            ExistingMoved = $false
            Published = $false
        })
    }

    $stagingCompleted = $false
    $commitStarted = $false
    $rollbackCompleted = $false
    try {
        # Prepare every owner's complete bundle beside the live directory before changing any
        # currently published stage. The sibling rename used below is atomic on the same volume.
        foreach ($transaction in $stageTransactions) {
            Assert-SafeStageSiblingDirectory `
                -Destination $transaction.Temporary `
                -ArtifactsRoot $resolvedArtifactsModsRoot `
                -OwnerPackageId $transaction.OwnerPackageId `
                -Version $RimWorldVersion `
                -Kind stage
            $null = New-Item -Path $transaction.Temporary -ItemType Directory
            $transaction.TemporaryCreated = $true
            Assert-SafeStageSiblingDirectory `
                -Destination $transaction.Temporary `
                -ArtifactsRoot $resolvedArtifactsModsRoot `
                -OwnerPackageId $transaction.OwnerPackageId `
                -Version $RimWorldVersion `
                -Kind stage
            Write-StageOwnership `
                -Destination $transaction.Temporary `
                -OwnerPackageId $transaction.OwnerPackageId `
                -Version $RimWorldVersion
            foreach ($bundle in $transaction.Bundles) {
                Assert-SafeStageSiblingDirectory `
                    -Destination $transaction.Temporary `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion `
                    -Kind stage
                Assert-StageOwnership `
                    -Destination $transaction.Temporary `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion
                Copy-Item `
                    -LiteralPath $bundle.SourceAssemblyPath `
                    -Destination (Join-Path $transaction.Temporary $bundle.Assembly) `
                    -Force
                Assert-SafeStageSiblingDirectory `
                    -Destination $transaction.Temporary `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion `
                    -Kind stage
                Assert-StageOwnership `
                    -Destination $transaction.Temporary `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion
                Copy-Item `
                    -LiteralPath $bundle.SourceManifestPath `
                    -Destination (Join-Path $transaction.Temporary $bundle.Manifest) `
                    -Force
            }
        }

        # Every complete temporary bundle receives a durable transaction marker before the first
        # live rename. A process death can therefore recover each owner to a complete old or new
        # stage without guessing whether an unmarked sibling directory belongs to this tool.
        foreach ($transaction in $stageTransactions) {
            Write-DurableStageTransactionMarker `
                -Path $transaction.Marker `
                -ArtifactsRoot $resolvedArtifactsModsRoot `
                -OwnerPackageId $transaction.OwnerPackageId `
                -Version $RimWorldVersion
            $transaction.MarkerCreated = $true
        }

        if (-not [string]::IsNullOrWhiteSpace($TestReplaceTemporaryWithUnownedOwner)) {
            $faultedTransaction = @($stageTransactions | Where-Object {
                    [string]$_.OwnerPackageId -ieq $TestReplaceTemporaryWithUnownedOwner
                } | Select-Object -First 1)
            if ($faultedTransaction.Count -eq 1) {
                Assert-SafeStageSiblingDirectory `
                    -Destination $faultedTransaction[0].Temporary `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $faultedTransaction[0].OwnerPackageId `
                    -Version $RimWorldVersion `
                    -Kind stage
                Assert-StageOwnership `
                    -Destination $faultedTransaction[0].Temporary `
                    -OwnerPackageId $faultedTransaction[0].OwnerPackageId `
                    -Version $RimWorldVersion
                Remove-Item -LiteralPath $faultedTransaction[0].Temporary -Recurse -Force
                $null = New-Item -Path $faultedTransaction[0].Temporary -ItemType Directory
                [System.IO.File]::WriteAllText(
                    (Join-Path $faultedTransaction[0].Temporary 'foreign-destination.txt'),
                    'foreign destination',
                    [System.Text.UTF8Encoding]::new($false))
                throw "Injected unowned replacement of integration-test temporary stage for '$TestReplaceTemporaryWithUnownedOwner'."
            }
        }

        try {
            $commitStarted = $true
            foreach ($transaction in $stageTransactions) {
                if (Test-Path -LiteralPath $transaction.Live) {
                    Assert-SafeStageDirectory `
                        -Destination $transaction.Live `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion
                    Assert-StageOwnership `
                        -Destination $transaction.Live `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion
                    Assert-SafeStageSiblingDirectory `
                        -Destination $transaction.Backup `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion `
                        -Kind backup
                    if (-not [string]::IsNullOrWhiteSpace($TestCreateCommitBackupDestinationOwner) -and
                        [string]$transaction.OwnerPackageId -ieq $TestCreateCommitBackupDestinationOwner) {
                        $null = New-Item -Path $transaction.Backup -ItemType Directory
                        [System.IO.File]::WriteAllText(
                            (Join-Path $transaction.Backup 'foreign-destination.txt'),
                            'foreign destination',
                            [System.Text.UTF8Encoding]::new($false))
                    }
                    Assert-SafeStageSiblingDirectory `
                        -Destination $transaction.Backup `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion `
                        -Kind backup
                    if (Test-Path -LiteralPath $transaction.Backup) {
                        throw "Integration-test backup destination appeared before commit rename: $($transaction.Backup)"
                    }
                    [System.IO.Directory]::Move($transaction.Live, $transaction.Backup)
                    $transaction.ExistingMoved = $true
                    if (-not [string]::IsNullOrWhiteSpace($TestCrashCommitAfterBackupOwner) -and
                        [string]$transaction.OwnerPackageId -ieq $TestCrashCommitAfterBackupOwner) {
                        Stop-Process -Id $PID -Force
                    }
                    if (-not [string]::IsNullOrWhiteSpace($TestFailCommitAfterBackupOwner) -and
                        [string]$transaction.OwnerPackageId -ieq $TestFailCommitAfterBackupOwner) {
                        throw "Injected integration-test stage commit failure after backing up owner '$($transaction.OwnerPackageId)'."
                    }
                }

                Assert-SafeStageSiblingDirectory `
                    -Destination $transaction.Temporary `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion `
                    -Kind stage
                Assert-StageOwnership `
                    -Destination $transaction.Temporary `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion
                Assert-SafeStageDirectory `
                    -Destination $transaction.Live `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion
                if (-not [string]::IsNullOrWhiteSpace($TestCreatePublishLiveDestinationOwner) -and
                    [string]$transaction.OwnerPackageId -ieq $TestCreatePublishLiveDestinationOwner) {
                    $null = New-Item -Path $transaction.Live -ItemType Directory
                    [System.IO.File]::WriteAllText(
                        (Join-Path $transaction.Live 'foreign-destination.txt'),
                        'foreign destination',
                        [System.Text.UTF8Encoding]::new($false))
                }
                Assert-SafeStageDirectory `
                    -Destination $transaction.Live `
                    -ArtifactsRoot $resolvedArtifactsModsRoot `
                    -OwnerPackageId $transaction.OwnerPackageId `
                    -Version $RimWorldVersion
                if (Test-Path -LiteralPath $transaction.Live) {
                    throw "Integration-test live destination appeared before publication: $($transaction.Live)"
                }
                [System.IO.Directory]::Move($transaction.Temporary, $transaction.Live)
                $transaction.Published = $true
            }
        }
        catch {
            $commitFailure = $_
            $rollbackFailures = [System.Collections.Generic.List[string]]::new()
            if (-not [string]::IsNullOrWhiteSpace($TestDeleteBackupBeforeRollbackOwner)) {
                $faultedTransaction = @($stageTransactions | Where-Object {
                        [string]$_.OwnerPackageId -ieq $TestDeleteBackupBeforeRollbackOwner -and
                        $_.ExistingMoved
                    } | Select-Object -First 1)
                if ($faultedTransaction.Count -eq 1 -and
                    (Test-Path -LiteralPath $faultedTransaction[0].Backup -PathType Container)) {
                    Remove-Item -LiteralPath $faultedTransaction[0].Backup -Recurse -Force
                }
            }
            for ($index = $stageTransactions.Count - 1; $index -ge 0; $index--) {
                $transaction = $stageTransactions[$index]
                try {
                    if ($transaction.Published -and (Test-Path -LiteralPath $transaction.Live)) {
                        Assert-SafeStageDirectory `
                            -Destination $transaction.Live `
                            -ArtifactsRoot $resolvedArtifactsModsRoot `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion
                        Assert-StageOwnership `
                            -Destination $transaction.Live `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion
                        Remove-Item -LiteralPath $transaction.Live -Recurse -Force
                    }

                    if ($transaction.ExistingMoved) {
                        if (-not (Test-Path -LiteralPath $transaction.Backup -PathType Container)) {
                            throw "Integration-test expected backup is missing during rollback: $($transaction.Backup)"
                        }
                        Assert-SafeStageSiblingDirectory `
                            -Destination $transaction.Backup `
                            -ArtifactsRoot $resolvedArtifactsModsRoot `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion `
                            -Kind backup
                        Assert-StageOwnership `
                            -Destination $transaction.Backup `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion
                        if (-not [string]::IsNullOrWhiteSpace($TestCreateRollbackDestinationOwner) -and
                            [string]$transaction.OwnerPackageId -ieq $TestCreateRollbackDestinationOwner) {
                            $null = New-Item -Path $transaction.Live -ItemType Directory
                        }
                        Assert-SafeStageDirectory `
                            -Destination $transaction.Live `
                            -ArtifactsRoot $resolvedArtifactsModsRoot `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion
                        if (Test-Path -LiteralPath $transaction.Live) {
                            throw "Integration-test live destination appeared before rollback restore: $($transaction.Live)"
                        }
                        [System.IO.Directory]::Move($transaction.Backup, $transaction.Live)
                    }
                }
                catch {
                    $rollbackFailures.Add("$($transaction.OwnerPackageId): $($_.Exception.Message)")
                }
            }

            if ($rollbackFailures.Count -gt 0) {
                throw "Integration-test stage commit failed: $($commitFailure.Exception.Message) Rollback also failed: $($rollbackFailures -join '; ')"
            }

            $rollbackCompleted = $true
            throw $commitFailure
        }

        $stagingCompleted = $true
    }
    finally {
        $temporaryCleanupFailures = [System.Collections.Generic.List[string]]::new()
        foreach ($transaction in $stageTransactions) {
            try {
                if ($transaction.TemporaryCreated -and
                    (Test-Path -LiteralPath $transaction.Temporary)) {
                    Assert-SafeStageSiblingDirectory `
                        -Destination $transaction.Temporary `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion `
                        -Kind stage
                    Assert-StageOwnership `
                        -Destination $transaction.Temporary `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion `
                        -ThrowOnFailure
                    Remove-Item -LiteralPath $transaction.Temporary -Recurse -Force
                }
            }
            catch {
                $temporaryCleanupFailures.Add("$($transaction.OwnerPackageId): $($_.Exception.Message)")
            }
        }

        $rollbackMarkerCleanupFailures = [System.Collections.Generic.List[string]]::new()
        if (-not $stagingCompleted -and
            $temporaryCleanupFailures.Count -eq 0 -and
            (-not $commitStarted -or $rollbackCompleted)) {
            foreach ($transaction in $stageTransactions) {
                try {
                    if ($transaction.MarkerCreated -and
                        (Test-Path -LiteralPath $transaction.Marker -PathType Leaf)) {
                        Remove-OwnedStageTransactionMarker `
                            -Path $transaction.Marker `
                            -ArtifactsRoot $resolvedArtifactsModsRoot `
                            -OwnerPackageId $transaction.OwnerPackageId `
                            -Version $RimWorldVersion
                    }
                }
                catch {
                    $rollbackMarkerCleanupFailures.Add("$($transaction.OwnerPackageId): $($_.Exception.Message)")
                }
            }
        }
        if ($temporaryCleanupFailures.Count -gt 0 -or $rollbackMarkerCleanupFailures.Count -gt 0) {
            $cleanupFailures = @(
                $temporaryCleanupFailures | ForEach-Object { "temporary stage: $_" }
                $rollbackMarkerCleanupFailures | ForEach-Object { "transaction marker: $_" })
            throw "Integration-test rollback cleanup failed: $($cleanupFailures -join '; ')"
        }
    }

    if ($stagingCompleted) {
        $backupCleanupFailures = [System.Collections.Generic.List[string]]::new()
        foreach ($transaction in $stageTransactions) {
            try {
                if (Test-Path -LiteralPath $transaction.Backup) {
                    Assert-SafeStageSiblingDirectory `
                        -Destination $transaction.Backup `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion `
                        -Kind backup
                    Assert-StageOwnership `
                        -Destination $transaction.Backup `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion
                    Remove-Item -LiteralPath $transaction.Backup -Recurse -Force
                }
            }
            catch {
                $backupCleanupFailures.Add("$($transaction.OwnerPackageId): $($_.Exception.Message)")
            }
        }
        if ($backupCleanupFailures.Count -gt 0) {
            throw "Integration-test backup-stage cleanup failed: $($backupCleanupFailures -join '; ')"
        }

        $markerCleanupFailures = [System.Collections.Generic.List[string]]::new()
        foreach ($transaction in $stageTransactions) {
            try {
                if ($transaction.MarkerCreated -and
                    (Test-Path -LiteralPath $transaction.Marker -PathType Leaf)) {
                    Remove-OwnedStageTransactionMarker `
                        -Path $transaction.Marker `
                        -ArtifactsRoot $resolvedArtifactsModsRoot `
                        -OwnerPackageId $transaction.OwnerPackageId `
                        -Version $RimWorldVersion
                }
            }
            catch {
                $markerCleanupFailures.Add("$($transaction.OwnerPackageId): $($_.Exception.Message)")
            }
        }
        if ($markerCleanupFailures.Count -gt 0) {
            throw "Integration-test transaction-marker cleanup failed: $($markerCleanupFailures -join '; ')"
        }
    }

    $resultProjects = @($bundles | Select-Object OwnerPackageId, Project, Assembly, Manifest, Destination)
    Write-Result ([pscustomobject]@{
        Status = 'staged'
        DryRun = $false
        Configuration = $Configuration
        RimWorldVersion = $RimWorldVersion
        ArtifactsModsRoot = $resolvedArtifactsModsRoot
        ProjectCount = $resultProjects.Count
        Projects = $resultProjects
    })
    exit 0
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
