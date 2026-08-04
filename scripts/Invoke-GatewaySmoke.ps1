<#
.SYNOPSIS
Proves the packaged RimWorld Dev Gateway inside an isolated RimWorld process.

.DESCRIPTION
Builds and deploys only the developer gateway, writes an isolated Core-plus-gateway ModsConfig,
forces runInBackground and mutes music in isolated preferences, launches one exact RimWorld PID minimized by default,
discovers its session manifest, exercises authenticated EmbedIO
status/UI/log routes and the in-process raw C# REPL, rejects an unauthenticated call, and verifies
that the normal ModsConfig hash did not change. With -Quicktest it waits for a playable map without
running setup mutations. -Scenario gateway-regression explicitly runs the idempotent quickstart,
semantic-control, FlaUI, and raw-input regression; another named scenario loads its descriptor from
scripts/Scenarios. With -RunIntegrationTests
it builds/stages startup-gated tests, verifies the selected lifecycle, and retains finalized-Def and
test-result artifacts; -IntegrationFailureProbe is the Gateway fixture's expected-failure map run.
Pass -VisibleWindow only when desktop UI or computer-use interaction is required; the explicit
gateway-regression scenario selects a normal visible window automatically.
Exit codes: 0 success,
1 verification/runtime failure, 2 for a path or semantic input rejected after parameter binding.
PowerShell rejects invalid ValidateRange or ValidateSet values before the script runs and reports
its own nonzero parameter-binding exit (normally 1).

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -DryRun -Output json

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -TimeoutSeconds 180

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -TimeoutSeconds 300

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -RunIntegrationTests -TimeoutSeconds 180

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -RunIntegrationTests -IntegrationFailureProbe -TimeoutSeconds 300

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -InteractiveHoldSeconds 900

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -VisibleWindow -InteractiveHoldSeconds 900

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -Scenario gateway-regression -TimeoutSeconds 300

.EXAMPLE
.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest -Scenario immersive-chefs-caravan-dining -InteractiveHoldSeconds 900
#>
[CmdletBinding()]
param(
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [string[]]$AdditionalModIds = @(),

    [string[]]$AdditionalModProjectPaths = @(),

    [string[]]$ExpectedLogMarkers = @(),

    [string[]]$ExpectedIntegrationTests = @(),

    [switch]$RequireRawClick,

    [switch]$VisibleWindow,

    [string]$ArtifactsPath,

    [ValidateRange(30, 600)]
    [int]$TimeoutSeconds = 180,

    [ValidateRange(0, 3600)]
    [int]$InteractiveHoldSeconds = 0,

    [switch]$Quicktest,

    [string]$Scenario,

    [switch]$RunIntegrationTests,

    [switch]$IntegrationFailureProbe,

    [string]$QuickstartDescriptorPath,

    [switch]$DryRun,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Wait-GatewayStoppedTombstone {
    param(
        [Parameter(Mandatory)]
        [string]$LiveManifestPath,

        [Parameter(Mandatory)]
        [string]$TombstonePath,

        [ValidateRange(1, 60000)]
        [int]$TimeoutMilliseconds = 15000
    )

    $deadline = [datetime]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
    do {
        if (-not (Test-Path -LiteralPath $LiveManifestPath -PathType Leaf) -and
            (Test-Path -LiteralPath $TombstonePath -PathType Leaf)) {
            try {
                $candidate = Get-Content -LiteralPath $TombstonePath -Raw | ConvertFrom-Json
                $hasToken = $candidate.PSObject.Properties.Name -contains 'token' -and
                    -not [string]::IsNullOrEmpty([string]$candidate.token)
                if ([string]$candidate.state -eq 'stopped' -and -not $hasToken) {
                    return $candidate
                }
            }
            catch {
                # The atomic replace may still be in progress. Poll until the same bounded deadline.
            }
        }

        Start-Sleep -Milliseconds 25
    }
    while ([datetime]::UtcNow -lt $deadline)

    if (Test-Path -LiteralPath $LiveManifestPath -PathType Leaf) {
        throw 'Controlled shutdown did not remove the live gateway manifest.'
    }

    throw "Controlled shutdown did not leave a readable credential-free stopped tombstone at $TombstonePath"
}

$knownExpansionIds = @(
    'ludeon.rimworld.royalty',
    'ludeon.rimworld.ideology',
    'ludeon.rimworld.biotech',
    'ludeon.rimworld.anomaly',
    'ludeon.rimworld.odyssey'
)

function Resolve-GatewaySmokeScenario {
    param(
        [AllowEmptyString()][string]$ScenarioName,
        [Parameter(Mandatory)][string]$ScenarioDirectory,
        [Parameter(Mandatory)][bool]$Quicktest
    )

    if ([string]::IsNullOrWhiteSpace($ScenarioName)) {
        return [pscustomobject]@{
            Name = 'none'
            RunsGatewayRegression = $false
            DescriptorPath = $null
            Descriptor = $null
        }
    }

    $normalized = $ScenarioName.Trim().ToLowerInvariant()
    if (-not $Quicktest) {
        throw "Gateway scenario '$normalized' requires -Quicktest."
    }

    if ($normalized -ceq 'gateway-regression') {
        return [pscustomobject]@{
            Name = $normalized
            RunsGatewayRegression = $true
            DescriptorPath = $null
            Descriptor = $null
        }
    }

    if ($normalized -cnotmatch '^[a-z0-9][a-z0-9._-]{0,63}$') {
        throw "Gateway scenario name is invalid: '$ScenarioName'"
    }

    $descriptorPath = [System.IO.Path]::GetFullPath((Join-Path $ScenarioDirectory "$normalized.json"))
    if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
        throw "Gateway scenario does not exist: $descriptorPath"
    }

    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json -ErrorAction Stop
    if ([int]$descriptor.schemaVersion -ne 1 -or [string]$descriptor.name -cne $normalized) {
        throw "Gateway scenario descriptor identity is invalid: $descriptorPath"
    }
    if ($null -eq $descriptor.steps) {
        throw "Gateway scenario descriptor has no steps: $descriptorPath"
    }

    $screenshotFileNames = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($step in @($descriptor.steps)) {
        if ([string]$step.kind -cne 'screenshot') {
            continue
        }

        $fileName = [string]$step.fileName
        if ($fileName -cnotmatch '^scenario-[a-z0-9][a-z0-9._-]{0,54}\.png$') {
            throw "Gateway scenario screenshot file names must begin with 'scenario-': '$fileName'"
        }
        if (-not $screenshotFileNames.Add($fileName)) {
            throw "Gateway scenario '$normalized' has duplicate screenshot file name '$fileName'."
        }
    }

    return [pscustomobject]@{
        Name = $normalized
        RunsGatewayRegression = $false
        DescriptorPath = $descriptorPath
        Descriptor = $descriptor
    }
}

function Assert-GatewayScenarioRequiredPackages {
    param(
        [Parameter(Mandatory)][object]$ScenarioPlan,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$PackageIds,
        [Parameter(Mandatory)][ValidateSet('configured', 'loaded')][string]$PackageState
    )

    if ($null -eq $ScenarioPlan.Descriptor) {
        return
    }

    foreach ($requiredPackageIdValue in @($ScenarioPlan.Descriptor.requiredPackageIds)) {
        $requiredPackageId = [string]$requiredPackageIdValue
        if (-not (Test-GatewayPackageId -Value $requiredPackageId)) {
            throw "Gateway scenario '$($ScenarioPlan.Name)' has invalid required package ID '$requiredPackageId'."
        }
        if ($PackageIds -inotcontains $requiredPackageId) {
            throw "Gateway scenario '$($ScenarioPlan.Name)' requires $PackageState mod '$requiredPackageId'."
        }
    }
}

function Write-Result {
    param([pscustomobject]$Result)

    if ($Output -eq 'json') {
        $Result | ConvertTo-Json -Compress -Depth 6
        return
    }

    $Result | Format-List
}

function Exit-InvalidInput {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

function Test-GatewayPackageId {
    param([string]$Value)

    return -not [string]::IsNullOrWhiteSpace($Value) -and
        $Value.Length -le 256 -and
        $Value -cmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$'
}

function Add-GatewaySmokeFailure {
    param(
        [Parameter(Mandatory)][System.Collections.IList]$Failures,
        [Parameter(Mandatory)][string]$Category,
        [Parameter(Mandatory)][string]$Message
    )

    $safeCategory = if ([string]::IsNullOrWhiteSpace($Category)) {
        'unspecified'
    }
    else {
        $Category.Trim()
    }
    $safeMessage = if ([string]::IsNullOrWhiteSpace($Message)) {
        'No cleanup failure detail was available.'
    }
    elseif ($Message.Length -gt 8192) {
        $Message.Substring(0, 8192)
    }
    else {
        $Message
    }
    $null = $Failures.Add([pscustomobject]@{
        Category = $safeCategory
        Message = $safeMessage
    })
}

function New-GatewaySmokeCleanupStatus {
    param(
        [Parameter(Mandatory)][string]$ProcessStatus,
        [Parameter(Mandatory)][string]$CredentialStatus,
        [Parameter(Mandatory)][string]$StageStatus,
        [Parameter(Mandatory)][System.Collections.IList]$Failures
    )

    return [pscustomobject]@{
        Status = if ($Failures.Count -eq 0) { 'completed' } else { 'failed' }
        CompletedUtc = [datetime]::UtcNow.ToString(
            'O',
            [Globalization.CultureInfo]::InvariantCulture)
        Process = [pscustomobject]@{ Status = $ProcessStatus }
        Credentials = [pscustomobject]@{ Status = $CredentialStatus }
        IntegrationTestStage = [pscustomobject]@{ Status = $StageStatus }
        Failures = @($Failures)
    }
}

function Set-GatewaySmokeCompletionMetadata {
    param(
        [Parameter(Mandatory)][object]$Result,
        [AllowNull()][object]$NormalConfigHashAfter,
        [AllowNull()][object]$NormalPrefsHashAfter,
        [Parameter(Mandatory)][bool]$IntegrationTestStageCleaned,
        [Parameter(Mandatory)][bool]$CredentialsSanitized,
        [Parameter(Mandatory)][string]$CredentialCleanup,
        [Parameter(Mandatory)][string]$IntegrationTestStageCleanup,
        [Parameter(Mandatory)][string]$CleanupStatus
    )

    if ($null -ne $NormalConfigHashAfter -and $NormalConfigHashAfter -isnot [string]) {
        throw 'NormalConfigHashAfter must be null or a string.'
    }
    if ($null -ne $NormalPrefsHashAfter -and $NormalPrefsHashAfter -isnot [string]) {
        throw 'NormalPrefsHashAfter must be null or a string.'
    }

    $metadata = [ordered]@{
        NormalConfigHashAfter = $NormalConfigHashAfter
        NormalPrefsHashAfter = $NormalPrefsHashAfter
        IntegrationTestStageCleaned = $IntegrationTestStageCleaned
        CredentialsSanitized = $CredentialsSanitized
        CredentialCleanup = $CredentialCleanup
        IntegrationTestStageCleanup = $IntegrationTestStageCleanup
        CleanupStatus = $CleanupStatus
    }
    foreach ($entry in $metadata.GetEnumerator()) {
        $Result | Add-Member `
            -MemberType NoteProperty `
            -Name ([string]$entry.Key) `
            -Value $entry.Value `
            -Force
    }

    return $Result
}

function Write-GatewaySmokeEvidenceSummary {
    param(
        [Parameter(Mandatory)][object]$Result,
        [Parameter(Mandatory)][string]$Path,
        [AllowEmptyString()][string]$BearerToken
    )

    $json = $Result | ConvertTo-Json -Depth 12
    if (-not [string]::IsNullOrEmpty($BearerToken) -and
        $json.Contains($BearerToken, [StringComparison]::Ordinal)) {
        throw 'Refusing to persist a smoke evidence summary containing the bearer token.'
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $directory = [System.IO.Path]::GetDirectoryName($resolvedPath)
    if ([string]::IsNullOrWhiteSpace($directory) -or
        -not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Smoke evidence summary directory does not exist: $directory"
    }

    $temporaryPath = $resolvedPath + '.tmp-' + [guid]::NewGuid().ToString('N')
    try {
        [System.IO.File]::WriteAllText(
            $temporaryPath,
            $json,
            [System.Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $resolvedPath) {
            throw "Smoke evidence summary already exists: $resolvedPath"
        }

        [System.IO.File]::Move($temporaryPath, $resolvedPath)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            [System.IO.File]::Delete($temporaryPath)
        }
    }
}

function Get-GatewaySmokeAssemblyEvidence {
    param(
        [Parameter(Mandatory)][string]$AssembliesPath,
        [Parameter(Mandatory)][string[]]$RequiredAssemblies
    )

    $root = [System.IO.Path]::GetFullPath($AssembliesPath)
    $rootItem = Get-Item -LiteralPath $root -Force -ErrorAction Stop
    if (-not $rootItem.PSIsContainer -or
        ($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Gateway assembly evidence requires a non-reparse directory: $root"
    }

    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($assemblyName in $RequiredAssemblies) {
        if ([string]::IsNullOrWhiteSpace($assemblyName) -or
            [System.IO.Path]::GetFileName($assemblyName) -cne $assemblyName -or
            -not $assemblyName.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase) -or
            -not $seen.Add($assemblyName)) {
            throw "Invalid or duplicate required Gateway assembly name: '$assemblyName'"
        }

        $assemblyPath = [System.IO.Path]::GetFullPath((Join-Path $root $assemblyName))
        $item = Get-Item -LiteralPath $assemblyPath -Force -ErrorAction Stop
        if ($item.PSIsContainer -or
            ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
            $item.Length -le 0 -or $item.Length -gt 32MB) {
            throw "Deployed Gateway assembly must be a bounded non-reparse file: $assemblyPath"
        }

        try {
            $identity = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath).FullName
        }
        catch {
            throw "Deployed Gateway assembly has no readable managed identity: $assemblyPath"
        }
        if ([string]::IsNullOrWhiteSpace($identity) -or $identity.Length -gt 1024) {
            throw "Deployed Gateway assembly identity is missing or unbounded: $assemblyPath"
        }

        [pscustomobject]@{
            FileName = $assemblyName
            AssemblyIdentity = $identity
            Length = [long]$item.Length
            Sha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
        }
    }
}

function Get-GatewaySmokeFileEvidence {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $item = Get-Item -LiteralPath ([System.IO.Path]::GetFullPath($Path)) -Force -ErrorAction Stop
    if ($item.PSIsContainer -or
        ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $item.Length -le 0) {
        throw "Product evidence requires a non-empty, non-reparse file: $($item.FullName)"
    }

    return [pscustomobject]@{
        RelativePath = $RelativePath.Replace('\', '/')
        Length = [long]$item.Length
        Sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
    }
}

function Get-GatewaySmokeManagedAssemblyEvidence {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $stream = $null
    $peReader = $null
    try {
        $stream = [System.IO.File]::Open(
            $resolvedPath,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::Read)
        $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        if (-not $peReader.HasMetadata) {
            return $null
        }

        $metadataReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
        if (-not $metadataReader.IsAssembly) {
            throw 'Metadata-bearing DLL has no assembly definition and cannot be admitted to an ordinary product package.'
        }

        $createIdentity = {
            param(
                [string]$Name,
                [version]$Version,
                [string]$Culture,
                [byte[]]$PublicKeyOrToken,
                [System.Reflection.AssemblyFlags]$Flags
            )

            $assemblyName = [System.Reflection.AssemblyName]::new()
            $assemblyName.Name = $Name
            $assemblyName.Version = $Version
            $assemblyName.CultureInfo = if ([string]::IsNullOrEmpty($Culture)) {
                [System.Globalization.CultureInfo]::InvariantCulture
            }
            else {
                [System.Globalization.CultureInfo]::GetCultureInfo($Culture)
            }
            if (($Flags -band [System.Reflection.AssemblyFlags]::PublicKey) -ne 0) {
                $assemblyName.SetPublicKey($PublicKeyOrToken)
            }
            else {
                $assemblyName.SetPublicKeyToken($PublicKeyOrToken)
            }
            if (($Flags -band [System.Reflection.AssemblyFlags]::Retargetable) -ne 0) {
                $assemblyName.Flags = $assemblyName.Flags -bor [System.Reflection.AssemblyNameFlags]::Retargetable
            }
            if (($Flags -band [System.Reflection.AssemblyFlags]::WindowsRuntime) -ne 0) {
                $assemblyName.ContentType = [System.Reflection.AssemblyContentType]::WindowsRuntime
            }

            return $assemblyName.FullName
        }

        $definition = $metadataReader.GetAssemblyDefinition()
        $identity = & $createIdentity `
            -Name $metadataReader.GetString($definition.Name) `
            -Version $definition.Version `
            -Culture $(if ($definition.Culture.IsNil) { '' } else { $metadataReader.GetString($definition.Culture) }) `
            -PublicKeyOrToken $metadataReader.GetBlobBytes($definition.PublicKey) `
            -Flags $definition.Flags

        $referencesByIdentity = [System.Collections.Generic.SortedDictionary[string, System.Collections.Generic.List[object]]]::new(
            [System.StringComparer]::Ordinal)
        foreach ($referenceHandle in $metadataReader.AssemblyReferences) {
            $reference = $metadataReader.GetAssemblyReference($referenceHandle)
            $referenceName = $metadataReader.GetString($reference.Name)
            $referenceIdentity = & $createIdentity `
                -Name $referenceName `
                -Version $reference.Version `
                -Culture $(if ($reference.Culture.IsNil) { '' } else { $metadataReader.GetString($reference.Culture) }) `
                -PublicKeyOrToken $metadataReader.GetBlobBytes($reference.PublicKeyOrToken) `
                -Flags $reference.Flags
            $referenceKey = $referenceName + [char]0 + $referenceIdentity
            if (-not $referencesByIdentity.ContainsKey($referenceKey)) {
                $referencesByIdentity.Add(
                    $referenceKey,
                    [System.Collections.Generic.List[object]]::new())
            }
            $referencesByIdentity[$referenceKey].Add([pscustomobject]@{
                MetadataRow = [System.Reflection.Metadata.Ecma335.MetadataTokens]::GetRowNumber(
                    $referenceHandle)
                Name = $referenceName
                AssemblyIdentity = $referenceIdentity
            })
        }

        $orderedReferences = [System.Collections.Generic.List[object]]::new()
        foreach ($referenceGroup in $referencesByIdentity.Values) {
            foreach ($referenceEvidence in $referenceGroup) {
                $orderedReferences.Add($referenceEvidence)
            }
        }

        return [pscustomobject]@{
            RelativePath = $RelativePath.Replace('\', '/')
            AssemblyIdentity = $identity
            References = @($orderedReferences)
        }
    }
    catch [System.BadImageFormatException] {
        return $null
    }
    catch {
        throw "Managed assembly metadata could not be read without loading code: $resolvedPath. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $peReader) {
            $peReader.Dispose()
        }
        if ($null -ne $stream) {
            $stream.Dispose()
        }
    }
}

function Get-GatewaySmokeAdditionalModProject {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string[]]$AdditionalPackageIds
    )

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    $resolvedProjectPath = if ([System.IO.Path]::IsPathRooted($ProjectPath)) {
        [System.IO.Path]::GetFullPath($ProjectPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $root $ProjectPath))
    }
    $rootPrefix = $root + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedProjectPath.StartsWith(
            $rootPrefix,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        [System.IO.Path]::GetExtension($resolvedProjectPath) -cne '.csproj') {
        throw "Additional mod project must be one repository-local .csproj: $resolvedProjectPath"
    }

    $projectFile = Get-Item -LiteralPath $resolvedProjectPath -Force -ErrorAction Stop
    if (($projectFile.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $projectFile.Length -le 0 -or $projectFile.Length -gt 1MB) {
        throw "Additional mod project is linked, empty, or exceeds its XML input bound: $resolvedProjectPath"
    }
    $current = $projectFile.Directory
    while ($null -ne $current) {
        if ($current.Exists -and
            ($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Additional mod project traverses a reparse point: $($current.FullName)"
        }
        if ([string]::Equals($current.FullName, $root, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $current = $current.Parent
    }
    if ($null -eq $current) {
        throw "Additional mod project root is not an ancestor: $resolvedProjectPath"
    }

    try {
        [xml]$projectXml = [System.IO.File]::ReadAllText($resolvedProjectPath)
    }
    catch {
        throw "Additional mod project XML could not be read: $resolvedProjectPath. $($_.Exception.Message)"
    }
    $packageNodes = @($projectXml.SelectNodes("//*[local-name()='RimWorldPackageId']"))
    $assemblyNodes = @($projectXml.SelectNodes("//*[local-name()='AssemblyName']"))
    if ($packageNodes.Count -ne 1 -or $assemblyNodes.Count -ne 1) {
        throw "Additional mod project must declare one literal RimWorldPackageId and AssemblyName: $resolvedProjectPath"
    }
    $packageId = $packageNodes[0].InnerText.Trim()
    $assemblyName = $assemblyNodes[0].InnerText.Trim()
    if (-not (Test-GatewayPackageId -Value $packageId) -or
        [string]::IsNullOrWhiteSpace($assemblyName) -or
        $assemblyName.Length -gt 256 -or
        $assemblyName -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
        throw "Additional mod project contains an invalid literal package or assembly identity: $resolvedProjectPath"
    }
    $activeAdditionalSet = [System.Collections.Generic.HashSet[string]]::new(
        $AdditionalPackageIds,
        [System.StringComparer]::OrdinalIgnoreCase)
    if (-not $activeAdditionalSet.Contains($packageId)) {
        throw "Additional mod project package '$packageId' is not present in AdditionalModIds."
    }

    $projectDirectory = $projectFile.DirectoryName
    $patchRoot = Join-Path $projectDirectory 'Patches'
    $sourcePatchFiles = [System.Collections.Generic.List[object]]::new()
    if (Test-Path -LiteralPath $patchRoot) {
        $patchRootItem = Get-Item -LiteralPath $patchRoot -Force
        if (-not $patchRootItem.PSIsContainer -or
            ($patchRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Additional mod Patches path must be a non-reparse directory: $patchRoot"
        }
        foreach ($entry in @(Get-ChildItem -LiteralPath $patchRoot -Force -Recurse)) {
            if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Additional mod Patches tree contains a reparse point: $($entry.FullName)"
            }
            if (-not $entry.PSIsContainer -and $entry.Extension -ieq '.xml') {
                $sourcePatchFiles.Add([pscustomobject]@{
                    RelativePath = [System.IO.Path]::GetRelativePath($patchRoot, $entry.FullName).Replace('\', '/')
                    SourcePath = $entry.FullName
                })
            }
        }
    }

    return [pscustomobject]@{
        PackageId = $packageId
        AssemblyName = $assemblyName
        ProjectPath = $resolvedProjectPath
        RelativeProjectPath = [System.IO.Path]::GetRelativePath($root, $resolvedProjectPath).Replace('\', '/')
        SourcePatchFiles = @($sourcePatchFiles | Sort-Object RelativePath)
    }
}

function Get-GatewaySmokeProductPackageEvidence {
    param(
        [Parameter(Mandatory)][object]$Project,
        [Parameter(Mandatory)][string]$RimWorldPath,
        [Parameter(Mandatory)][string]$BuildLogPath
    )

    $packageRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $RimWorldPath "Mods\$($Project.PackageId)"))
    $packageRootItem = Get-Item -LiteralPath $packageRoot -Force -ErrorAction Stop
    if (-not $packageRootItem.PSIsContainer -or
        ($packageRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Deployed product package must be a non-reparse directory: $packageRoot"
    }
    $packageEntries = @(Get-ChildItem -LiteralPath $packageRoot -Force -Recurse)
    if (@($packageEntries | Where-Object {
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        }).Count -ne 0) {
        throw "Deployed product package contains a reparse point: $packageRoot"
    }
    if (@($packageEntries | Where-Object {
            -not $_.PSIsContainer -and
            (($_.Name.StartsWith(
                        'RimWorldDevGateway',
                        [System.StringComparison]::OrdinalIgnoreCase) -and
                    $_.Name.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase)) -or
                $_.FullName -match '(?i)[\\/]DevIntegrationTests[\\/]')
        }).Count -ne 0) {
        throw "Ordinary product package contains a staged test assembly or RimWorld Dev Gateway DLL: $packageRoot"
    }

    $filesByPath = [System.Collections.Generic.SortedDictionary[string, object]]::new(
        [System.StringComparer]::Ordinal)
    $managedAssembliesByPath = [System.Collections.Generic.SortedDictionary[string, object]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($packageFile in @($packageEntries | Where-Object { -not $_.PSIsContainer })) {
        $relativePath = [System.IO.Path]::GetRelativePath(
            $packageRoot,
            $packageFile.FullName).Replace('\', '/')
        $filesByPath.Add(
            $relativePath,
            [pscustomobject]@{
                RelativePath = $relativePath
                Length = [long]$packageFile.Length
                Sha256 = (Get-FileHash -LiteralPath $packageFile.FullName -Algorithm SHA256).Hash
            })

        if ($packageFile.Extension -ieq '.dll') {
            $managedAssembly = Get-GatewaySmokeManagedAssemblyEvidence `
                -Path $packageFile.FullName `
                -RelativePath $relativePath
            if ($null -ne $managedAssembly) {
                $managedAssembliesByPath.Add($relativePath, $managedAssembly)
            }
        }
    }
    $gatewayReferenceEvidence = @($managedAssembliesByPath.Values | ForEach-Object {
            $managedAssembly = $_
            @($managedAssembly.References | Where-Object {
                    $_.Name.StartsWith(
                        'RimWorldDevGateway',
                        [System.StringComparison]::OrdinalIgnoreCase)
                } | ForEach-Object {
                    [pscustomobject]@{
                        RelativePath = [string]$managedAssembly.RelativePath
                        Reference = [string]$_.AssemblyIdentity
                    }
                })
        })
    if ($gatewayReferenceEvidence.Count -ne 0) {
        $references = @($gatewayReferenceEvidence | ForEach-Object {
                "$($_.RelativePath) -> $($_.Reference)"
            })
        throw "Ordinary product managed assembly references forbidden RimWorldDevGateway assembly identities: $($references -join '; ')"
    }

    $assembliesPath = Join-Path $packageRoot '1.6\Assemblies'
    $assemblyFileName = [string]$Project.AssemblyName + '.dll'
    $assembly = @(Get-GatewaySmokeAssemblyEvidence `
        -AssembliesPath $assembliesPath `
        -RequiredAssemblies @($assemblyFileName))
    if ($assembly.Count -ne 1 -or
        -not ([string]$assembly[0].AssemblyIdentity).StartsWith(
            ([string]$Project.AssemblyName + ','),
            [System.StringComparison]::Ordinal)) {
        throw "Deployed product assembly identity does not match its project AssemblyName: $assemblyFileName"
    }
    $productAssemblyRelativePath = "1.6/Assemblies/$assemblyFileName"
    $productManagedAssembly = if ($managedAssembliesByPath.ContainsKey($productAssemblyRelativePath)) {
        $managedAssembliesByPath[$productAssemblyRelativePath]
    }
    else {
        $null
    }
    if ($null -eq $productManagedAssembly) {
        throw "Deployed product assembly is not a managed assembly: $assemblyFileName"
    }

    $about = Get-GatewaySmokeFileEvidence `
        -Path (Join-Path $packageRoot 'About\About.xml') `
        -RelativePath 'About/About.xml'
    $sourcePatches = @($Project.SourcePatchFiles)
    $deployedPatchRoot = Join-Path $packageRoot '1.6\Patches'
    $deployedPatchFiles = @()
    if (Test-Path -LiteralPath $deployedPatchRoot) {
        $deployedPatchFiles = @(Get-ChildItem -LiteralPath $deployedPatchRoot -File -Recurse -Force |
            Where-Object { $_.Extension -ieq '.xml' })
    }
    $sourceRelativePaths = @($sourcePatches | ForEach-Object { [string]$_.RelativePath } | Sort-Object)
    $deployedRelativePaths = @($deployedPatchFiles | ForEach-Object {
            [System.IO.Path]::GetRelativePath($deployedPatchRoot, $_.FullName).Replace('\', '/')
        } | Sort-Object)
    $patchSetsDiffer = $sourceRelativePaths.Count -ne $deployedRelativePaths.Count
    if (-not $patchSetsDiffer -and $sourceRelativePaths.Count -gt 0) {
        $patchSetsDiffer = @(Compare-Object -CaseSensitive `
                -ReferenceObject $sourceRelativePaths `
                -DifferenceObject $deployedRelativePaths).Count -ne 0
    }
    if ($patchSetsDiffer) {
        throw "Deployed product Patches directory does not contain the exact source patch set: $packageRoot"
    }

    $patchEvidence = [System.Collections.Generic.List[object]]::new()
    foreach ($sourcePatch in $sourcePatches) {
        $relativePath = [string]$sourcePatch.RelativePath
        $deployedPath = Join-Path $deployedPatchRoot $relativePath.Replace('/', '\')
        $sourceEvidence = Get-GatewaySmokeFileEvidence `
            -Path ([string]$sourcePatch.SourcePath) `
            -RelativePath ("Patches/$relativePath")
        $deployedEvidence = Get-GatewaySmokeFileEvidence `
            -Path $deployedPath `
            -RelativePath ("Patches/$relativePath")
        if ([string]$sourceEvidence.Sha256 -cne [string]$deployedEvidence.Sha256) {
            throw "Deployed product patch differs from its repository source: $relativePath"
        }
        $patchEvidence.Add($deployedEvidence)
    }

    return [pscustomobject]@{
        PackageId = [string]$Project.PackageId
        Project = [string]$Project.RelativeProjectPath
        PackageRoot = $packageRoot
        BuildLog = $BuildLogPath
        Files = @($filesByPath.Values)
        ManagedAssemblies = @($managedAssembliesByPath.Values)
        Assembly = $assembly[0]
        About = $about
        Patches = @($patchEvidence)
    }
}

function Invoke-GatewaySmokeBestEffortCleanupAction {
    param(
        [Parameter(Mandatory)][System.Collections.IList]$Failures,
        [Parameter(Mandatory)][string]$Category,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][scriptblock]$Operation
    )

    try {
        $null = & $Operation
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $Failures `
            -Category $Category `
            -Message "$Description failed: $($_.Exception.Message)"
    }
}

function Get-GatewaySmokeOptionalHashSafely {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$DisplayName = 'Normal RimWorld ModsConfig.xml',
        [Parameter(Mandatory)][System.Collections.IList]$Failures
    )

    try {
        return [pscustomobject]@{
            Succeeded = $true
            Hash = Get-OptionalFileHash -Path $Path
        }
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $Failures `
            -Category 'primary' `
            -Message "$DisplayName hash read failed: $($_.Exception.Message)"
        return [pscustomobject]@{
            Succeeded = $false
            Hash = $null
        }
    }
}

function Assert-GatewaySmokeUnchangedHash {
    param(
        [Parameter(Mandatory)][string]$DisplayName,
        [AllowNull()][object]$BeforeHash,
        [Parameter(Mandatory)][object]$ReadResult,
        [Parameter(Mandatory)][System.Collections.IList]$Failures
    )

    $afterHash = $ReadResult.Hash
    if ([bool]$ReadResult.Succeeded -and $BeforeHash -ne $afterHash) {
        Add-GatewaySmokeFailure `
            -Failures $Failures `
            -Category 'primary' `
            -Message "$DisplayName changed during isolated verification. Before=$BeforeHash After=$afterHash"
    }

    return $afterHash
}

function Get-GatewayRuntimeLogErrors {
    param([Parameter(Mandatory)][string]$Path)

    return @(Select-String `
        -LiteralPath $Path `
        -Pattern '(?i)(FileNotFoundException|TypeLoadException|MissingMethodException|ReflectionTypeLoadException|Root level exception|Exception:|Error while instantiating a mod|Could not instantiate|Could not resolve cross-reference|XML error|not a Def type or could not be found|Patch operation .* failed|EmbedIO.*(error|exception)|Mono\.CSharp.*(error|exception)|Swan\.Lite.*(error|exception)|System\.ValueTuple.*(error|exception))')
}

function Assert-GatewayFinalPlayerLog {
    param(
        [Parameter(Mandatory)][string]$Path,
        [AllowEmptyString()][string]$BearerToken
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Final Player.log is missing: $Path"
    }

    $finalPlayerLog = Get-Content -LiteralPath $Path -Raw
    if (-not [string]::IsNullOrEmpty($BearerToken) -and
        $finalPlayerLog.Contains($BearerToken, [StringComparison]::Ordinal)) {
        throw 'The bearer token leaked into Player.log after scenario execution or shutdown.'
    }

    $finalRuntimeErrors = @(Get-GatewayRuntimeLogErrors -Path $Path)
    if ($finalRuntimeErrors.Count -gt 0) {
        throw "Player.log contains a mod or gateway load/runtime error after scenario execution and exact-process shutdown. See $Path"
    }
}

function Read-ValidatedGatewayCurrentManifest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$SavedDataPath,
        [Parameter(Mandatory)][int]$ExpectedProcessId,
        [Parameter(Mandatory)][datetimeoffset]$ExpectedProcessStartUtc
    )

    $resolvedSavedDataPath = [System.IO.Path]::GetFullPath($SavedDataPath)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $expectedPath = [System.IO.Path]::GetFullPath(
        (Join-Path $resolvedSavedDataPath 'DevGateway\current.json'))
    if (-not [string]::Equals(
            $resolvedPath,
            $expectedPath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Gateway current manifest path is not the exact isolated current.json locator: $resolvedPath"
    }

    $file = [System.IO.FileInfo]::new($resolvedPath)
    if (-not $file.Exists) {
        throw "Gateway current manifest does not exist: $resolvedPath"
    }
    if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Gateway current manifest is a reparse point: $resolvedPath"
    }

    $current = $file.Directory
    while ($null -ne $current) {
        if ($current.Exists -and
            ($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Gateway current manifest traverses a reparse point: $($current.FullName)"
        }

        if ([string]::Equals(
                $current.FullName,
                $resolvedSavedDataPath,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }

        $current = $current.Parent
    }
    if ($null -eq $current) {
        throw "Gateway current manifest escaped its exact isolated save-data folder: $resolvedPath"
    }

    $stream = $null
    $document = $null
    try {
        $stream = [System.IO.FileStream]::new(
            $resolvedPath,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::Read,
            4096,
            [System.IO.FileOptions]::SequentialScan)
        if ($stream.Length -le 0 -or $stream.Length -gt 64KB) {
            throw "Gateway current manifest must be non-empty and at most 64 KiB: $resolvedPath"
        }

        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) {
                throw "Gateway current manifest ended before its bounded payload was read: $resolvedPath"
            }

            $offset += $read
        }

        try {
            $json = [System.Text.UTF8Encoding]::new($false, $true).GetString($bytes)
            $document = [System.Text.Json.JsonDocument]::Parse($json)
        }
        catch {
            throw "Gateway current manifest is not strict UTF-8 JSON: $resolvedPath. $($_.Exception.Message)"
        }

        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            throw 'Gateway current manifest root must be one JSON object.'
        }

        $expectedNames = @(
            'apiVersion',
            'runId',
            'state',
            'baseUrl',
            'token',
            'processId',
            'processStartUtc',
            'startedUtc',
            'gameVersion',
            'modVersion',
            'unrestrictedExecution',
            'warning'
        )
        $actualNames = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::Ordinal)
        $values = [System.Collections.Generic.Dictionary[string,System.Text.Json.JsonElement]]::new(
            [System.StringComparer]::Ordinal)
        foreach ($property in $document.RootElement.EnumerateObject()) {
            $propertyName = [string]$property.Name
            if (-not $actualNames.Add($propertyName)) {
                throw "Gateway current manifest contains duplicate property '$propertyName'."
            }

            $values.Add($propertyName, $property.Value.Clone())
        }
        if ($actualNames.Count -ne $expectedNames.Count -or
            @($expectedNames | Where-Object { -not $actualNames.Contains($_) }).Count -ne 0) {
            throw 'Gateway current manifest does not have the exact active-session schema.'
        }

        $readString = {
            param([string]$Name)
            $element = $values[$Name]
            if ($element.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                throw "Gateway current manifest property '$Name' must be a JSON string."
            }

            return $element.GetString()
        }
        $apiVersion = & $readString 'apiVersion'
        $currentRunId = & $readString 'runId'
        $state = & $readString 'state'
        $baseUrl = & $readString 'baseUrl'
        $token = & $readString 'token'
        $processStartText = & $readString 'processStartUtc'
        $startedText = & $readString 'startedUtc'
        $gameVersion = [string](& $readString 'gameVersion')
        $modVersion = [string](& $readString 'modVersion')
        $warning = [string](& $readString 'warning')

        if ([string]$apiVersion -cne '1' -or [string]$state -cne 'active') {
            throw 'Gateway current manifest must describe one active version-one session.'
        }
        if ([string]::IsNullOrWhiteSpace([string]$currentRunId) -or
            [string]$currentRunId -cnotmatch '^[A-Za-z0-9_-]{1,128}$') {
            throw 'Gateway current manifest contains an unsafe run ID.'
        }
        if ([string]::IsNullOrWhiteSpace([string]$token) -or
            [string]$token -cnotmatch '^[A-Za-z0-9_-]{43}$') {
            throw 'Gateway current manifest contains an invalid bearer-token shape.'
        }

        $baseUri = $null
        try {
            $baseUri = [System.Uri]::new([string]$baseUrl, [System.UriKind]::Absolute)
        }
        catch {
            throw 'Gateway current manifest baseUrl is not an absolute URI.'
        }
        $exactBaseUrl = "http://127.0.0.1:$($baseUri.Port)/api/v1"
        if ($baseUri.Scheme -cne 'http' -or
            $baseUri.Host -cne '127.0.0.1' -or
            $baseUri.Port -lt 1 -or
            $baseUri.Port -gt 65535 -or
            -not [string]::IsNullOrEmpty($baseUri.UserInfo) -or
            -not [string]::IsNullOrEmpty($baseUri.Query) -or
            -not [string]::IsNullOrEmpty($baseUri.Fragment) -or
            [string]$baseUrl -cne $exactBaseUrl) {
            throw 'Gateway current manifest baseUrl must be exact loopback HTTP /api/v1.'
        }

        $processIdElement = $values['processId']
        if ($processIdElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) {
            throw "Gateway current manifest property 'processId' must be a JSON integer."
        }
        try {
            $currentProcessId = $processIdElement.GetInt32()
        }
        catch {
            throw "Gateway current manifest property 'processId' must be a JSON Int32."
        }
        if ($currentProcessId -ne $ExpectedProcessId) {
            throw "Gateway current manifest PID $currentProcessId does not match launched PID $ExpectedProcessId."
        }

        try {
            $processStart = [datetimeoffset]::ParseExact(
                [string]$processStartText,
                'O',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind)
            $null = [datetimeoffset]::ParseExact(
                [string]$startedText,
                'O',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind)
        }
        catch {
            throw 'Gateway current manifest timestamps must use the exact round-trip format.'
        }
        if ($processStart.ToUniversalTime().Ticks -ne
            $ExpectedProcessStartUtc.ToUniversalTime().Ticks) {
            throw 'Gateway current manifest process start does not match the exact launched process identity.'
        }

        $unrestricted = $values['unrestrictedExecution']
        if ($unrestricted.ValueKind -ne [System.Text.Json.JsonValueKind]::True -or
            -not $unrestricted.GetBoolean()) {
            throw "Gateway current manifest property 'unrestrictedExecution' must be JSON boolean true."
        }
        if ([string]::IsNullOrWhiteSpace($gameVersion) -or $gameVersion.Length -gt 4096) {
            throw "Gateway current manifest property 'gameVersion' must be present and bounded."
        }
        if ([string]::IsNullOrWhiteSpace($modVersion) -or $modVersion.Length -gt 4096) {
            throw "Gateway current manifest property 'modVersion' must be present and bounded."
        }
        if ([string]::IsNullOrWhiteSpace($warning) -or $warning.Length -gt 4096) {
            throw "Gateway current manifest property 'warning' must be present and bounded."
        }

        return [pscustomobject]@{
            apiVersion = [string]$apiVersion
            runId = [string]$currentRunId
            state = [string]$state
            baseUrl = [string]$baseUrl
            token = [string]$token
            processId = [int]$currentProcessId
            processStartUtc = [string]$processStartText
            startedUtc = [string]$startedText
            gameVersion = [string]$gameVersion
            modVersion = [string]$modVersion
            unrestrictedExecution = $true
            warning = [string]$warning
        }
    }
    finally {
        if ($null -ne $document) {
            $document.Dispose()
        }
        if ($null -ne $stream) {
            $stream.Dispose()
        }
    }
}

function Get-StagedIntegrationTestAssemblyRecord {
    param(
        [Parameter(Mandatory)][string]$OwnerPackageId,
        [Parameter(Mandatory)][string]$AssemblyPath,
        [Parameter(Mandatory)][string]$ManifestPath
    )

    if ([string]::IsNullOrWhiteSpace($OwnerPackageId) -or
        $OwnerPackageId.Length -gt 256 -or
        $OwnerPackageId -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
        throw "Staged integration-test assembly has an invalid owner package ID: $OwnerPackageId"
    }

    $resolvedAssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
    $resolvedManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    $assembly = Get-Item -LiteralPath $resolvedAssemblyPath -Force
    $manifest = Get-Item -LiteralPath $resolvedManifestPath -Force
    if ($assembly.PSIsContainer -or $manifest.PSIsContainer -or
        ($assembly.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        ($manifest.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $assembly.Length -le 0 -or $assembly.Length -gt 32MB -or
        $manifest.Length -le 0 -or $manifest.Length -gt 64KB) {
        throw 'Staged integration-test assembly/manifest must be bounded regular files.'
    }
    if (-not [string]::Equals(
            $assembly.DirectoryName,
            $manifest.DirectoryName,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Staged integration-test assembly and manifest must be exact siblings.'
    }

    $expectedManifestName =
        [System.IO.Path]::GetFileNameWithoutExtension($assembly.Name) + '.integrationtests.json'
    if (-not $assembly.Name.EndsWith(
            '.IntegrationTests.dll',
            [System.StringComparison]::Ordinal) -or
        $manifest.Name -cne $expectedManifestName) {
        throw 'Staged integration-test assembly and manifest names do not form one exact bundle pair.'
    }

    try {
        $assemblyIdentity = [System.Reflection.AssemblyName]::GetAssemblyName(
            $resolvedAssemblyPath).FullName
    }
    catch {
        throw "Staged integration-test DLL identity could not be read: $resolvedAssemblyPath. $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($assemblyIdentity) -or $assemblyIdentity.Length -gt 1024) {
        throw "Staged integration-test DLL has no bounded full assembly identity: $resolvedAssemblyPath"
    }

    return [pscustomobject]@{
        OwningPackageId = $OwnerPackageId
        AssemblyFileName = $assembly.Name
        ManifestFileName = $manifest.Name
        SourceIdentity = "$OwnerPackageId/$($manifest.Name)"
        AssemblyIdentity = $assemblyIdentity
    }
}

function Assert-IntegrationTestAssemblyProvenance {
    param(
        [Parameter(Mandatory)][object]$Snapshot,
        [Parameter(Mandatory)][object[]]$ExpectedAssemblies
    )

    $expected = @($ExpectedAssemblies)
    $discovered = @($Snapshot.DiscoveredAssemblies)
    if ($expected.Count -le 0 -or
        $discovered.Count -ne $expected.Count -or
        [int]$Snapshot.DiscoveredAssemblyCount -ne $expected.Count) {
        throw "Integration-test discovered assembly count does not match the exact staged DLL count ($($expected.Count))."
    }

    $expectedSourceKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    $discoveredSourceKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    $discoveredAssemblyKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($assembly in $discovered) {
        $owner = [string]$assembly.OwningPackageId
        $sourceIdentity = [string]$assembly.SourceIdentity
        $assemblyIdentity = [string]$assembly.AssemblyIdentity
        $sourceKey = "$owner|$sourceIdentity"
        $assemblyKey = "$owner|$assemblyIdentity"
        if ([string]::IsNullOrWhiteSpace($owner) -or
            [string]::IsNullOrWhiteSpace($sourceIdentity) -or
            [string]::IsNullOrWhiteSpace($assemblyIdentity) -or
            -not $discoveredSourceKeys.Add($sourceKey) -or
            -not $discoveredAssemblyKeys.Add($assemblyKey)) {
            throw 'Integration-test discovered assembly source/full-identity mapping is missing or not one-to-one.'
        }
    }

    foreach ($assembly in $expected) {
        $owner = [string]$assembly.OwningPackageId
        $sourceIdentity = [string]$assembly.SourceIdentity
        $assemblyIdentity = [string]$assembly.AssemblyIdentity
        $sourceKey = "$owner|$sourceIdentity"
        if (-not $expectedSourceKeys.Add($sourceKey)) {
            throw 'Integration-test staged source identities are not one-to-one.'
        }

        $matches = @($discovered | Where-Object {
                [string]$_.OwningPackageId -ceq $owner -and
                [string]$_.SourceIdentity -ceq $sourceIdentity -and
                [string]$_.AssemblyIdentity -ceq $assemblyIdentity
            })
        if ($matches.Count -ne 1) {
            throw "Integration-test staged source/full assembly identity was not discovered exactly once: $sourceIdentity"
        }
    }

    $descriptors = @($Snapshot.DiscoveredTests)
    if ($descriptors.Count -ne [int]$Snapshot.DiscoveredTestCount) {
        throw 'Integration-test descriptor count does not match DiscoveredTestCount.'
    }
    $descriptorKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($descriptor in $descriptors) {
        $assemblyKey = [string]$descriptor.OwningPackageId + '|' +
            [string]$descriptor.AssemblyIdentity
        if (-not $discoveredAssemblyKeys.Contains($assemblyKey)) {
            throw "Integration-test descriptor does not belong to one exact staged/discovered assembly: $($descriptor.TestName)"
        }

        $descriptorKey = $assemblyKey + '|' + [string]$descriptor.TestName + '|' +
            [string]$descriptor.RunAt
        if (-not $descriptorKeys.Add($descriptorKey)) {
            throw "Integration-test descriptor identity is not one-to-one: $($descriptor.TestName)"
        }
    }

    foreach ($assembly in $expected) {
        $owner = [string]$assembly.OwningPackageId
        $assemblyIdentity = [string]$assembly.AssemblyIdentity
        $matchingDescriptors = @($descriptors | Where-Object {
                [string]$_.OwningPackageId -ceq $owner -and
                [string]$_.AssemblyIdentity -ceq $assemblyIdentity
            })
        if ($matchingDescriptors.Count -eq 0) {
            throw "Staged integration-test assembly contributed no discovered test descriptor: $owner / $assemblyIdentity"
        }
    }

    $resultKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($result in @($Snapshot.Results)) {
        $assemblyKey = [string]$result.OwningPackageId + '|' +
            [string]$result.AssemblyIdentity
        $descriptorKey = $assemblyKey + '|' + [string]$result.TestName + '|' +
            [string]$result.RunAt
        if (-not $discoveredAssemblyKeys.Contains($assemblyKey) -or
            -not $descriptorKeys.Contains($descriptorKey)) {
            throw "Integration-test result does not map to one exact staged assembly and discovered descriptor: $($result.TestName)"
        }
        if (-not $resultKeys.Add($descriptorKey)) {
            throw "Integration-test result identity is not one-to-one: $($result.TestName)"
        }
    }
}

function Test-GatewayDefProjectionFailureMarker {
    param([AllowNull()][object]$Root)

    $stack = [System.Collections.Generic.Stack[object]]::new()
    if ($null -ne $Root) {
        $stack.Push($Root)
    }
    $visited = 0
    while ($stack.Count -gt 0) {
        $node = $stack.Pop()
        if ($null -eq $node -or
            $node -is [string] -or
            $node -is [char] -or
            $node -is [bool] -or
            $node -is [byte] -or
            $node -is [sbyte] -or
            $node -is [int16] -or
            $node -is [uint16] -or
            $node -is [int32] -or
            $node -is [uint32] -or
            $node -is [int64] -or
            $node -is [uint64] -or
            $node -is [single] -or
            $node -is [double] -or
            $node -is [decimal]) {
            continue
        }

        $visited++
        if ($visited -gt 100000) {
            throw 'Finalized Def export projection-marker inspection exceeded its 100,000-node bound.'
        }

        if ($node -is [System.Collections.IEnumerable]) {
            foreach ($item in $node) {
                if ($null -ne $item) {
                    $stack.Push($item)
                }
            }
            continue
        }

        foreach ($property in @($node.PSObject.Properties)) {
            $name = [string]$property.Name
            if ($name -ceq '$byteLimit' -or
                $name -ceq '$projection' -or
                $name -ceq '$projectionLimit') {
                return $true
            }
            if ($name -ceq 'Kind' -and
                $property.Value -is [string] -and
                ([string]$property.Value -ceq 'limit' -or
                    [string]$property.Value -ceq 'projectionError')) {
                return $true
            }
            if ($null -ne $property.Value) {
                $stack.Push($property.Value)
            }
        }
    }

    return $false
}

function Get-IntegrationTestTerminalFingerprint {
    param([Parameter(Mandatory)][object]$Snapshot)

    $timestamp = {
        param([object]$Value)
        if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
            return $null
        }

        return ([datetimeoffset]$Value).ToUniversalTime().ToString(
            'O',
            [Globalization.CultureInfo]::InvariantCulture)
    }
    $orderedObjects = {
        param([object[]]$Values, [scriptblock]$Projection)
        $projected = [System.Collections.Generic.List[object]]::new()
        foreach ($value in @($Values)) {
            $projected.Add((& $Projection $value))
        }

        return @($projected | Sort-Object SortKey)
    }

    $assemblies = & $orderedObjects @($Snapshot.DiscoveredAssemblies) {
        param($item)
        [ordered]@{
            SortKey = [string]$item.OwningPackageId + '|' + [string]$item.SourceIdentity
            OwningPackageId = [string]$item.OwningPackageId
            SourceIdentity = [string]$item.SourceIdentity
            AssemblyIdentity = [string]$item.AssemblyIdentity
        }
    }
    $tests = & $orderedObjects @($Snapshot.DiscoveredTests) {
        param($item)
        [ordered]@{
            SortKey = [string]$item.OwningPackageId + '|' + [string]$item.TestName + '|' + [string]$item.RunAt
            OwningPackageId = [string]$item.OwningPackageId
            AssemblyIdentity = [string]$item.AssemblyIdentity
            TestName = [string]$item.TestName
            RunAt = [string]$item.RunAt
        }
    }
    $failures = & $orderedObjects @($Snapshot.Failures) {
        param($item)
        [ordered]@{
            SortKey = [string]$item.Code + '|' + [string]$item.OwningPackageId + '|' + [string]$item.TestName
            Code = [string]$item.Code
            Message = [string]$item.Message
            OwningPackageId = [string]$item.OwningPackageId
            AssemblyIdentity = [string]$item.AssemblyIdentity
            TestName = [string]$item.TestName
            ExceptionType = [string]$item.ExceptionType
            StackTrace = [string]$item.StackTrace
        }
    }
    $results = & $orderedObjects @($Snapshot.Results) {
        param($item)
        [ordered]@{
            SortKey = [string]$item.OwningPackageId + '|' + [string]$item.TestName + '|' + [string]$item.RunAt
            OwningPackageId = [string]$item.OwningPackageId
            AssemblyIdentity = [string]$item.AssemblyIdentity
            TestName = [string]$item.TestName
            RunAt = [string]$item.RunAt
            State = [string]$item.State
            StartedUtc = & $timestamp $item.StartedUtc
            CompletedUtc = & $timestamp $item.CompletedUtc
            DurationMilliseconds = if ($null -eq $item.DurationMilliseconds) { $null } else { [long]$item.DurationMilliseconds }
            ExceptionType = [string]$item.ExceptionType
            Message = [string]$item.Message
            StackTrace = [string]$item.StackTrace
        }
    }
    $lifecycles = & $orderedObjects @($Snapshot.LifecyclePoints) {
        param($item)
        [ordered]@{
            SortKey = [string]$item.RunAt
            RunAt = [string]$item.RunAt
            State = [string]$item.State
            StartedUtc = & $timestamp $item.StartedUtc
            CompletedUtc = & $timestamp $item.CompletedUtc
            ExecutedCount = [int]$item.ExecutedCount
            PassedCount = [int]$item.PassedCount
            FailedCount = [int]$item.FailedCount
        }
    }
    $canonical = [ordered]@{
        Enabled = [bool]$Snapshot.Enabled
        DiscoveryState = [string]$Snapshot.DiscoveryState
        DiscoveredAssemblyCount = [int]$Snapshot.DiscoveredAssemblyCount
        DiscoveredTestCount = [int]$Snapshot.DiscoveredTestCount
        DiscoveredAssemblies = $assemblies
        DiscoveredTests = $tests
        Failures = $failures
        Results = $results
        LifecyclePoints = $lifecycles
        OmittedFailureCount = [int]$Snapshot.OmittedFailureCount
        OmittedResultCount = [int]$Snapshot.OmittedResultCount
    }
    return $canonical | ConvertTo-Json -Compress -Depth 10
}

function Read-IntegrationTestSnapshotFile {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Integration-test snapshot file does not exist: $Path"
    }

    $file = Get-Item -LiteralPath $Path -Force
    if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $file.Length -le 0 -or
        $file.Length -gt 4MB) {
        throw "Integration-test snapshot file is linked, empty, or exceeds its byte limit: $Path"
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Integration-test snapshot file is not valid JSON: $Path. $($_.Exception.Message)"
    }
}

function Assert-MatchingIntegrationTestSnapshots {
    param(
        [Parameter(Mandatory)][object]$Expected,
        [Parameter(Mandatory)][object]$Actual
    )

    $expectedFingerprint = Get-IntegrationTestTerminalFingerprint -Snapshot $Expected
    $actualFingerprint = Get-IntegrationTestTerminalFingerprint -Snapshot $Actual
    if ($expectedFingerprint -cne $actualFingerprint) {
        throw 'Integration-test terminal snapshots differ in identity, timestamps, or terminal state.'
    }
}

function Assert-IntegrationTestTerminalSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Snapshot,
        [Parameter(Mandatory)][string]$TargetRunAt,
        [switch]$FailureProbe,
        [string]$ExpectedFailureTestName =
            'RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.A_DeliberateFailureProbeIsStartupControlled',
        [int]$MinimumDiscoveredTestCount = 1,
        [object[]]$ExpectedTests = @()
    )

    $gatewayOwner = 'fumblesneeze.rimworlddevgateway'
    $mainMenuA = 'RimWorldDevGateway.InGame.IntegrationTests.FinalizedDefIntegrationTests.CoreSteelDefExistsInFinalizedDatabase'
    $mainMenuB = 'RimWorldDevGateway.InGame.IntegrationTests.FinalizedDefIntegrationTests.GatewayProbeContainsItsFinalXmlPatch'
    $mapA = 'RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.A_DeliberateFailureProbeIsStartupControlled'
    $mapB = 'RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.B_LaterTestStillRunsAfterTheFailureProbe'
    if ($ExpectedFailureTestName -cne $mapA) {
        throw "The smoke probe expected failure name must remain the exact Gateway map-A fixture: $mapA"
    }

    if (-not [bool]$Snapshot.Enabled -or
        [string]$Snapshot.DiscoveryState -cne 'completed' -or
        [int]$Snapshot.DiscoveredTestCount -lt $MinimumDiscoveredTestCount) {
        throw "Integration-test discovery did not complete with at least $MinimumDiscoveredTestCount tests."
    }

    $discoveredTests = @($Snapshot.DiscoveredTests)
    if ($discoveredTests.Count -ne [int]$Snapshot.DiscoveredTestCount) {
        throw 'Integration-test discovery did not expose every discovered test descriptor.'
    }
    $validLifecycleRunAts = @('MainMenuLoaded', 'PlayableMapLoaded')
    $unknownRunAtDescriptors = @($discoveredTests |
        Where-Object { $validLifecycleRunAts -cnotcontains [string]$_.RunAt })
    if ($unknownRunAtDescriptors.Count -ne 0) {
        throw "Discovered integration test '$([string]$unknownRunAtDescriptors[0].TestName)' uses unsupported lifecycle '$([string]$unknownRunAtDescriptors[0].RunAt)'."
    }

    $gatewayDescriptors = @($discoveredTests |
        Where-Object { [string]$_.OwningPackageId -ieq $gatewayOwner })
    $expectedDescriptors = @(
        "$mainMenuA|MainMenuLoaded",
        "$mainMenuB|MainMenuLoaded",
        "$mapA|PlayableMapLoaded",
        "$mapB|PlayableMapLoaded"
    )
    $actualDescriptors = @($gatewayDescriptors |
        ForEach-Object { [string]$_.TestName + '|' + [string]$_.RunAt } |
        Sort-Object)
    if ($gatewayDescriptors.Count -ne 4 -or
        @(Compare-Object -CaseSensitive `
            -ReferenceObject @($expectedDescriptors | Sort-Object) `
            -DifferenceObject $actualDescriptors).Count -ne 0) {
        throw 'Integration-test discovery did not expose the four exact Gateway fixture descriptors.'
    }

    $topLevelFailures = @($Snapshot.Failures)
    if ($topLevelFailures.Count -ne 0) {
        throw "Integration-test discovery/infrastructure Failures were reported: $($topLevelFailures.Count)."
    }
    if ([int]$Snapshot.OmittedFailureCount -ne 0 -or [int]$Snapshot.OmittedResultCount -ne 0) {
        throw 'Integration-test snapshot omitted failures or results and cannot prove the complete run.'
    }

    $lifecyclePoints = @($Snapshot.LifecyclePoints)
    $mainMenuLifecycles = @($lifecyclePoints |
        Where-Object { [string]$_.RunAt -ceq 'MainMenuLoaded' })
    if ($mainMenuLifecycles.Count -ne 1) {
        throw 'Integration-test snapshot must contain exactly one MainMenuLoaded lifecycle result.'
    }
    $playableMapLifecycles = @($lifecyclePoints |
        Where-Object { [string]$_.RunAt -ceq 'PlayableMapLoaded' })
    if ($playableMapLifecycles.Count -ne 1) {
        throw 'Integration-test snapshot must contain exactly one PlayableMapLoaded lifecycle result.'
    }
    if ($lifecyclePoints.Count -ne 2) {
        throw 'Integration-test snapshot must contain only the two defined lifecycle results.'
    }

    $targetLifecycles = @($lifecyclePoints |
        Where-Object { [string]$_.RunAt -ceq $TargetRunAt })
    if ($targetLifecycles.Count -ne 1) {
        throw "Integration-test snapshot must contain exactly one $TargetRunAt lifecycle result."
    }

    $targetLifecycle = $targetLifecycles[0]
    $expectedLifecycleState = if ($FailureProbe) { 'failed' } else { 'completed' }
    if ([string]$targetLifecycle.State -cne $expectedLifecycleState) {
        throw "Integration-test lifecycle $TargetRunAt was '$($targetLifecycle.State)', expected '$expectedLifecycleState'."
    }

    $allResults = @($Snapshot.Results)
    $unknownRunAtResults = @($allResults |
        Where-Object { $validLifecycleRunAts -cnotcontains [string]$_.RunAt })
    if ($unknownRunAtResults.Count -ne 0) {
        throw "Integration-test result '$([string]$unknownRunAtResults[0].TestName)' uses unsupported lifecycle '$([string]$unknownRunAtResults[0].RunAt)'."
    }

    $expectedTestKeys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($expectedTest in @($ExpectedTests)) {
        $expectedOwner = [string]$expectedTest.OwningPackageId
        $expectedRunAt = [string]$expectedTest.RunAt
        $expectedTestName = [string]$expectedTest.TestName
        $expectedKey = $expectedOwner.ToUpperInvariant() + '|' +
            $expectedRunAt + '|' + $expectedTestName
        if (-not (Test-GatewayPackageId -Value $expectedOwner) -or
            $validLifecycleRunAts -cnotcontains $expectedRunAt -or
            [string]::IsNullOrWhiteSpace($expectedTestName) -or
            $expectedTestName.Length -gt 1024 -or
            $expectedTestName -cnotmatch '^[A-Za-z_][A-Za-z0-9_.+`]*$' -or
            -not $expectedTestKeys.Add($expectedKey)) {
            throw "Invalid or duplicate expected integration test contract: $expectedOwner|$expectedRunAt|$expectedTestName"
        }

        $expectedDescriptors = @($discoveredTests | Where-Object {
                [string]$_.OwningPackageId -ieq $expectedOwner -and
                [string]$_.RunAt -ceq $expectedRunAt -and
                [string]$_.TestName -ceq $expectedTestName
            })
        if ($expectedDescriptors.Count -ne 1) {
            throw "Expected integration test descriptor was not discovered exactly once: $expectedOwner|$expectedRunAt|$expectedTestName"
        }

        $expectedResults = @($allResults | Where-Object {
                [string]$_.OwningPackageId -ieq $expectedOwner -and
                [string]$_.AssemblyIdentity -ceq [string]$expectedDescriptors[0].AssemblyIdentity -and
                [string]$_.RunAt -ceq $expectedRunAt -and
                [string]$_.TestName -ceq $expectedTestName
            })
        if ($expectedResults.Count -ne 1 -or
            [string]$expectedResults[0].State -cne 'passed') {
            $actualState = if ($expectedResults.Count -eq 1) {
                [string]$expectedResults[0].State
            }
            else {
                "result-count-$($expectedResults.Count)"
            }
            throw "Expected integration test did not produce one passed result ($actualState): $expectedOwner|$expectedRunAt|$expectedTestName"
        }
    }
    foreach ($lifecycle in $lifecyclePoints) {
        $lifecycleRunAt = [string]$lifecycle.RunAt
        $lifecycleState = [string]$lifecycle.State
        $lifecycleDescriptors = @($discoveredTests |
            Where-Object { [string]$_.RunAt -ceq $lifecycleRunAt })
        $lifecycleResults = @($allResults |
            Where-Object { [string]$_.RunAt -ceq $lifecycleRunAt })

        if ($lifecycleState -ceq 'pending') {
            if ($lifecycleResults.Count -ne 0 -or
                [int]$lifecycle.ExecutedCount -ne 0 -or
                [int]$lifecycle.PassedCount -ne 0 -or
                [int]$lifecycle.FailedCount -ne 0 -or
                $null -ne $lifecycle.StartedUtc -or
                $null -ne $lifecycle.CompletedUtc) {
                throw "Pending integration-test lifecycle $lifecycleRunAt must have null timestamps, zero counters, and no results."
            }
            continue
        }
        if ($lifecycleState -cne 'completed' -and $lifecycleState -cne 'failed') {
            throw "Integration-test lifecycle $lifecycleRunAt has unsupported nonterminal state '$lifecycleState'."
        }

        foreach ($descriptor in $lifecycleDescriptors) {
            $matchingResults = @($lifecycleResults |
                Where-Object {
                    [string]$_.OwningPackageId -ceq [string]$descriptor.OwningPackageId -and
                    [string]$_.AssemblyIdentity -ceq [string]$descriptor.AssemblyIdentity -and
                    [string]$_.TestName -ceq [string]$descriptor.TestName -and
                    [string]$_.RunAt -ceq [string]$descriptor.RunAt
                })
            if ($matchingResults.Count -ne 1) {
                throw "Discovered integration test '$([string]$descriptor.TestName)' in $lifecycleRunAt does not have exactly one terminal result."
            }
        }
        foreach ($lifecycleResult in $lifecycleResults) {
            $matchingDescriptors = @($lifecycleDescriptors |
                Where-Object {
                    [string]$_.OwningPackageId -ceq [string]$lifecycleResult.OwningPackageId -and
                    [string]$_.AssemblyIdentity -ceq [string]$lifecycleResult.AssemblyIdentity -and
                    [string]$_.TestName -ceq [string]$lifecycleResult.TestName -and
                    [string]$_.RunAt -ceq [string]$lifecycleResult.RunAt
                })
            if ($matchingDescriptors.Count -ne 1) {
                throw "Integration-test result '$([string]$lifecycleResult.TestName)' in $lifecycleRunAt does not map to exactly one discovered descriptor."
            }
        }

        $lifecyclePassed = @($lifecycleResults |
            Where-Object { [string]$_.State -ceq 'passed' })
        $lifecycleFailed = @($lifecycleResults |
            Where-Object { [string]$_.State -ceq 'failed' })
        if ($lifecycleResults.Count -ne [int]$lifecycle.ExecutedCount -or
            $lifecyclePassed.Count -ne [int]$lifecycle.PassedCount -or
            $lifecycleFailed.Count -ne [int]$lifecycle.FailedCount -or
            ($lifecyclePassed.Count + $lifecycleFailed.Count) -ne $lifecycleResults.Count) {
            throw "Integration-test lifecycle $lifecycleRunAt contains incomplete or inconsistent results."
        }
    }

    $mainMenuLifecycle = $mainMenuLifecycles[0]
    $mainMenuResults = @($allResults |
        Where-Object { [string]$_.RunAt -ceq 'MainMenuLoaded' })
    $mainMenuFailed = @($mainMenuResults |
        Where-Object { [string]$_.State -ceq 'failed' })

    $mainMenuState = [string]$mainMenuLifecycle.State
    if ($TargetRunAt -ceq 'PlayableMapLoaded' -and
        $mainMenuState -cne 'pending' -and
        $mainMenuState -cne 'completed') {
        $failedMainMenuNames = @($mainMenuFailed |
            ForEach-Object { [string]$_.TestName })
        $failureDetail = if ($failedMainMenuNames.Count -eq 0) {
            'no failing result was reported'
        }
        else {
            $failedMainMenuNames -join ', '
        }
        throw "Integration-test MainMenuLoaded lifecycle was '$mainMenuState'; Quicktest requires pending or completed. Failures: $failureDetail"
    }
    $expectedGatewayResultNames = @()
    if ($TargetRunAt -ceq 'MainMenuLoaded' -or $mainMenuState -ceq 'completed') {
        $expectedGatewayResultNames += @($mainMenuA, $mainMenuB)
    }
    if ($TargetRunAt -ceq 'PlayableMapLoaded') {
        $expectedGatewayResultNames += @($mapA, $mapB)
    }
    $gatewayResults = @($allResults |
        Where-Object { [string]$_.OwningPackageId -ieq $gatewayOwner })
    $gatewayResultNames = @($gatewayResults | ForEach-Object { [string]$_.TestName } | Sort-Object)
    if ($gatewayResults.Count -ne $expectedGatewayResultNames.Count -or
        @(Compare-Object -CaseSensitive `
            -ReferenceObject @($expectedGatewayResultNames | Sort-Object) `
            -DifferenceObject $gatewayResultNames).Count -ne 0) {
        $missingNames = @($expectedGatewayResultNames |
            Where-Object { $gatewayResultNames -cnotcontains $_ })
        throw "Integration-test results did not contain each exact expected Gateway fixture result name: $($missingNames -join ', ')"
    }

    $targetResults = @($allResults | Where-Object { [string]$_.RunAt -ceq $TargetRunAt })
    $targetPassed = @($targetResults | Where-Object { [string]$_.State -ceq 'passed' })
    $targetFailed = @($targetResults | Where-Object { [string]$_.State -ceq 'failed' })
    if ($targetResults.Count -ne [int]$targetLifecycle.ExecutedCount -or
        $targetPassed.Count -ne [int]$targetLifecycle.PassedCount -or
        $targetFailed.Count -ne [int]$targetLifecycle.FailedCount -or
        ($targetPassed.Count + $targetFailed.Count) -ne $targetResults.Count) {
        throw "Integration-test lifecycle $TargetRunAt contains incomplete or inconsistent target results."
    }

    $globalFailedResults = @($allResults | Where-Object { [string]$_.State -ceq 'failed' })
    $globalNonTerminalResults = @($allResults |
        Where-Object { [string]$_.State -cne 'passed' -and [string]$_.State -cne 'failed' })
    if ($globalNonTerminalResults.Count -ne 0) {
        throw "Integration-test terminal snapshot contains non-terminal results: $($globalNonTerminalResults[0].TestName)"
    }

    if (-not $FailureProbe) {
        if ($globalFailedResults.Count -ne 0) {
            throw "Integration-test result failed outside the accepted probe contract: $($globalFailedResults[0].TestName)"
        }
        return $targetLifecycle
    }

    $mapAResults = @($globalFailedResults |
        Where-Object {
            [string]$_.OwningPackageId -ieq $gatewayOwner -and
            [string]$_.TestName -ceq $mapA -and
            [string]$_.RunAt -ceq 'PlayableMapLoaded'
        })
    $mapBResults = @($gatewayResults |
        Where-Object {
            [string]$_.TestName -ceq $mapB -and
            [string]$_.RunAt -ceq 'PlayableMapLoaded' -and
            [string]$_.State -ceq 'passed'
        })
    if ($globalFailedResults.Count -ne 1 -or $mapAResults.Count -ne 1 -or $mapBResults.Count -ne 1) {
        $unexpectedFailureNames = @($globalFailedResults |
            Where-Object {
                [string]$_.OwningPackageId -ine $gatewayOwner -or
                [string]$_.TestName -cne $mapA -or
                [string]$_.RunAt -cne 'PlayableMapLoaded'
            } |
            ForEach-Object { [string]$_.TestName })
        throw "The deliberate probe requires exactly failed map A and passed map B, with every other result passing. Required: $mapA ; $mapB. Unexpected failures: $($unexpectedFailureNames -join ', ')"
    }

    return $targetLifecycle
}

function ConvertTo-PowerShellSingleQuotedLiteral {
    param([string]$Value)

    return "'$($Value.Replace("'", "''"))'"
}

function Write-IntegrationBundleInvocation {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$BundleScriptPath,
        [Parameter(Mandatory)][string[]]$ActivePackageIds,
        [Parameter(Mandatory)][string]$ModsRoot,
        [Parameter(Mandatory)][string]$RimWorldPath,
        [Parameter(Mandatory)][string]$WorkshopPath,
        [switch]$DryRun
    )

    $activePackageLiterals = [System.Collections.Generic.List[string]]::new()
    foreach ($activePackageId in $ActivePackageIds) {
        $activePackageLiterals.Add(
            (ConvertTo-PowerShellSingleQuotedLiteral -Value $activePackageId))
    }
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in @(
            '$parameters = @{',
            "    ActivePackageIds = @($($activePackageLiterals -join ', '))",
            "    Configuration = 'Release'",
            "    ArtifactsModsRoot = $(ConvertTo-PowerShellSingleQuotedLiteral -Value $ModsRoot)",
            "    RimWorldPath = $(ConvertTo-PowerShellSingleQuotedLiteral -Value $RimWorldPath)",
            "    SteamModContentFolder = $(ConvertTo-PowerShellSingleQuotedLiteral -Value $WorkshopPath)",
            "    RimWorldVersion = '1.6'",
            "    Output = 'json'")) {
        $lines.Add($line)
    }
    if ($DryRun) {
        $lines.Add('    DryRun = $true')
    }
    $lines.Add('}')
    $lines.Add("& $(ConvertTo-PowerShellSingleQuotedLiteral -Value $BundleScriptPath) @parameters")
    $lines.Add('exit $LASTEXITCODE')
    [System.IO.File]::WriteAllLines(
        $Path,
        $lines,
        [System.Text.UTF8Encoding]::new($false))
}

function Assert-IntegrationTestStagePath {
    param(
        [string]$StagePath,
        [string]$ModsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $resolvedModsRoot = [System.IO.Path]::GetFullPath($ModsRoot)
    $resolvedStagePath = [System.IO.Path]::GetFullPath($StagePath)
    $expectedStagePath = [System.IO.Path]::GetFullPath(
        (Join-Path $resolvedModsRoot "$OwnerPackageId\$Version\DevIntegrationTests"))
    if (-not [string]::Equals(
            $resolvedStagePath,
            $expectedStagePath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to use an unexpected integration-test stage path: $resolvedStagePath"
    }

    $modsRootPrefix = $resolvedModsRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStagePath.StartsWith(
            $modsRootPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to use an integration-test stage outside RimWorld's local Mods root: $resolvedStagePath"
    }

    $current = [System.IO.DirectoryInfo]::new($resolvedStagePath)
    while ($null -ne $current) {
        if ($current.Exists -and
            ($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to use an integration-test stage through a reparse point: $($current.FullName)"
        }

        if ([string]::Equals(
                $current.FullName,
                $resolvedModsRoot,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }

        $current = $current.Parent
    }

    if ($null -eq $current) {
        throw "Refusing to use an integration-test stage whose Mods root is not an ancestor: $resolvedStagePath"
    }

    return $resolvedStagePath
}

function Assert-OwnedIntegrationTestStage {
    param(
        [string]$StagePath,
        [string]$ModsRoot,
        [string]$OwnerPackageId,
        [string]$Version
    )

    $resolvedStagePath = Assert-IntegrationTestStagePath `
        -StagePath $StagePath `
        -ModsRoot $ModsRoot `
        -OwnerPackageId $OwnerPackageId `
        -Version $Version

    $markerPath = Join-Path $resolvedStagePath '.rimworld-integration-test-stage.owner'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Integration-test stage has no ownership marker: $resolvedStagePath"
    }

    $marker = Get-Item -LiteralPath $markerPath -Force
    if (($marker.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $marker.Length -le 0 -or
        $marker.Length -gt 1024) {
        throw "Integration-test stage has an invalid ownership marker: $resolvedStagePath"
    }

    $expectedOwnership = "RimWorldInGameIntegrationTests/v1|$OwnerPackageId|$Version"
    if ([System.IO.File]::ReadAllText($markerPath) -cne $expectedOwnership) {
        throw "Integration-test stage ownership marker does not match its owner and version: $resolvedStagePath"
    }

    return $resolvedStagePath
}

function Register-IntegrationTestStageCandidates {
    param(
        [Parameter(Mandatory)][object]$PlanResult,
        [Parameter(Mandatory)][string[]]$ActivePackageIds,
        [Parameter(Mandatory)][string]$ModsRoot,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][System.Collections.IList]$Records
    )

    $projects = @($PlanResult.Projects)
    if ([string]$PlanResult.Status -cne 'planned' -or
        -not [bool]$PlanResult.DryRun -or
        [int]$PlanResult.ProjectCount -le 0 -or
        $projects.Count -ne [int]$PlanResult.ProjectCount) {
        throw 'In-game integration-test dry run returned an inconsistent project plan.'
    }

    $activePackageSet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($activePackageId in $ActivePackageIds) {
        if (-not (Test-GatewayPackageId -Value $activePackageId) -or
            -not $activePackageSet.Add($activePackageId)) {
            throw "Integration-test staging received an invalid or duplicate active package ID: $activePackageId"
        }
    }

    $registeredOwners = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $initialRecordCount = $Records.Count
    foreach ($record in $Records) {
        $null = $registeredOwners.Add([string]$record.OwnerPackageId)
    }

    $stageGroups = @($projects | Group-Object OwnerPackageId | Sort-Object Name)
    foreach ($stageGroup in $stageGroups) {
        $ownerPackageId = [string]$stageGroup.Name
        if (-not (Test-GatewayPackageId -Value $ownerPackageId) -or
            -not $activePackageSet.Contains($ownerPackageId)) {
            throw "Integration-test plan reported an invalid owner or one outside the active mod set: $ownerPackageId"
        }

        $reportedDestinations = @($stageGroup.Group |
            Select-Object -ExpandProperty Destination -Unique)
        if ($reportedDestinations.Count -ne 1) {
            throw "Integration-test plan owner '$ownerPackageId' reported multiple stage destinations."
        }

        $stagePath = Assert-IntegrationTestStagePath `
            -StagePath ([string]$reportedDestinations[0]) `
            -ModsRoot $ModsRoot `
            -OwnerPackageId $ownerPackageId `
            -Version $Version
        if (-not $registeredOwners.Add($ownerPackageId)) {
            throw "Integration-test stage candidate was registered more than once: $ownerPackageId"
        }

        $null = $Records.Add([pscustomobject]@{
            OwnerPackageId = $ownerPackageId
            StagePath = $stagePath
        })
    }

    if ($stageGroups.Count -eq 0 -or
        ($Records.Count - $initialRecordCount) -ne $stageGroups.Count) {
        throw 'Integration-test dry-run plan did not register every exact owner stage candidate.'
    }
}

function Assert-IntegrationTestBundleMatchesPlan {
    param(
        [Parameter(Mandatory)][object]$PlanResult,
        [Parameter(Mandatory)][object]$ActualResult
    )

    $plannedProjects = @($PlanResult.Projects)
    $actualProjects = @($ActualResult.Projects)
    if ([string]$PlanResult.Status -cne 'planned' -or
        -not [bool]$PlanResult.DryRun -or
        [string]$ActualResult.Status -cne 'staged' -or
        [bool]$ActualResult.DryRun -or
        $plannedProjects.Count -ne [int]$PlanResult.ProjectCount -or
        $actualProjects.Count -ne [int]$ActualResult.ProjectCount -or
        $plannedProjects.Count -ne $actualProjects.Count) {
        throw 'In-game integration-test staged result does not match its pre-registered dry-run plan.'
    }

    $plannedIdentities = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($project in $plannedProjects) {
        $identity = ([string]$project.OwnerPackageId).ToUpperInvariant() + "`n" +
            [string]$project.Project + "`n" +
            ([System.IO.Path]::GetFullPath([string]$project.Destination)).ToUpperInvariant()
        if (-not $plannedIdentities.Add($identity)) {
            throw 'In-game integration-test dry-run plan contains a duplicate project identity.'
        }
    }

    foreach ($project in $actualProjects) {
        $identity = ([string]$project.OwnerPackageId).ToUpperInvariant() + "`n" +
            [string]$project.Project + "`n" +
            ([System.IO.Path]::GetFullPath([string]$project.Destination)).ToUpperInvariant()
        if (-not $plannedIdentities.Remove($identity)) {
            throw 'In-game integration-test staged result does not match its pre-registered dry-run plan.'
        }
    }

    if ($plannedIdentities.Count -ne 0) {
        throw 'In-game integration-test staged result does not match its pre-registered dry-run plan.'
    }
}

$validatedAdditionalModIds = [System.Collections.Generic.List[string]]::new()
$activePackageIdSet = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$null = $activePackageIdSet.Add('ludeon.rimworld')
$null = $activePackageIdSet.Add('fumblesneeze.rimworlddevgateway')
foreach ($packageId in $AdditionalModIds) {
    if (-not (Test-GatewayPackageId -Value $packageId)) {
        Exit-InvalidInput "Additional mod package IDs must be at most 256 ASCII package characters and begin with a letter or digit: '$packageId'"
    }

    if (-not $activePackageIdSet.Add($packageId)) {
        Exit-InvalidInput "Duplicate active mod package ID under OrdinalIgnoreCase identity: '$packageId'"
    }

    $validatedAdditionalModIds.Add($packageId)
}

$validatedLogMarkers = [System.Collections.Generic.List[string]]::new()
foreach ($marker in $ExpectedLogMarkers) {
    if ([string]::IsNullOrWhiteSpace($marker) -or
        $marker.Length -gt 512 -or
        $marker.IndexOfAny([char[]]"`r`n") -ge 0) {
        Exit-InvalidInput 'Expected log markers must be non-empty single-line strings of at most 512 characters.'
    }

    if (-not $validatedLogMarkers.Contains($marker)) {
        $validatedLogMarkers.Add($marker)
    }
}

$validatedExpectedIntegrationTests = [System.Collections.Generic.List[object]]::new()
$validatedExpectedIntegrationTestSpecs = [System.Collections.Generic.List[string]]::new()
$expectedIntegrationTestKeys = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::Ordinal)
foreach ($expectation in $ExpectedIntegrationTests) {
    $parts = @($expectation.Split([char]'|'))
    if ($parts.Count -ne 3) {
        Exit-InvalidInput 'Expected integration tests must use ownerPackageId|RunAt|Fully.Qualified.TestName.'
    }

    $ownerPackageId = $parts[0]
    $runAt = $parts[1]
    $testName = $parts[2]
    $key = $ownerPackageId.ToUpperInvariant() + '|' + $runAt + '|' + $testName
    if (-not (Test-GatewayPackageId -Value $ownerPackageId) -or
        @('MainMenuLoaded', 'PlayableMapLoaded') -cnotcontains $runAt -or
        [string]::IsNullOrWhiteSpace($testName) -or
        $testName.Length -gt 1024 -or
        $testName -cnotmatch '^[A-Za-z_][A-Za-z0-9_.+`]*$' -or
        -not $activePackageIdSet.Contains($ownerPackageId) -or
        -not $expectedIntegrationTestKeys.Add($key)) {
        Exit-InvalidInput "Invalid, inactive-owner, or duplicate expected integration test: '$expectation'"
    }

    $validatedExpectedIntegrationTests.Add([pscustomobject]@{
        OwningPackageId = $ownerPackageId
        RunAt = $runAt
        TestName = $testName
    })
    $validatedExpectedIntegrationTestSpecs.Add($expectation)
}

$activeModIds = @('ludeon.rimworld') + @($validatedAdditionalModIds) + @('fumblesneeze.rimworlddevgateway')

if ($IntegrationFailureProbe -and (-not $RunIntegrationTests -or -not $Quicktest)) {
    Exit-InvalidInput '-IntegrationFailureProbe requires both -RunIntegrationTests and -Quicktest.'
}
if ($validatedExpectedIntegrationTests.Count -gt 0 -and -not $RunIntegrationTests) {
    Exit-InvalidInput '-ExpectedIntegrationTests requires -RunIntegrationTests.'
}

function Get-OptionalFileHash {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Write-MinimalModsConfig {
    param(
        [string]$Path,
        [string]$Version
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('ModsConfigData')
        $writer.WriteElementString('version', $Version)
        $writer.WriteStartElement('activeMods')
        foreach ($packageId in $activeModIds) {
            $writer.WriteElementString('li', $packageId)
        }
        $writer.WriteEndElement()
        $writer.WriteStartElement('knownExpansions')
        foreach ($packageId in $knownExpansionIds) {
            $writer.WriteElementString('li', $packageId)
        }
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Write-MinimalPrefs {
    param([Parameter(Mandatory)][string]$Path)

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('PrefsData')
        $writer.WriteElementString('volumeMusic', '0')
        $writer.WriteElementString('runInBackground', 'True')
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Start-GatewayRimWorldProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string[]]$LaunchArguments,
        [Parameter(Mandatory)][ValidateSet('Minimized', 'Normal')][string]$WindowStyle
    )

    return Start-Process `
        -FilePath $ExecutablePath `
        -ArgumentList $LaunchArguments `
        -WindowStyle $WindowStyle `
        -PassThru
}

function Invoke-GatewaySmokeBoundedProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][ValidateRange(1, 600000)][int]$TimeoutMilliseconds,
        [Parameter(Mandatory)][string]$DisplayName
    )

    $resolvedExecutablePath = [System.IO.Path]::GetFullPath($ExecutablePath)
    if (-not (Test-Path -LiteralPath $resolvedExecutablePath -PathType Leaf)) {
        throw "$DisplayName executable does not exist: $resolvedExecutablePath"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $resolvedExecutablePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add([string]$argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "$DisplayName did not start."
        }

        $childProcessId = $process.Id
        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            $terminationFailure = $null
            try {
                $process.Kill($true)
            }
            catch {
                $terminationFailure = $_.Exception.Message
                try {
                    $process.Kill()
                    $terminationFailure = $null
                }
                catch {
                    $terminationFailure = $_.Exception.Message
                }
            }

            $null = $process.WaitForExit(2000)
            if (-not $process.HasExited) {
                $terminationException = [System.TimeoutException]::new(
                    "$DisplayName timed out after $TimeoutMilliseconds ms and child PID $childProcessId could not be confirmed terminated: $terminationFailure")
                $terminationException.Data['GatewaySmokeChildTerminationConfirmed'] = $false
                throw $terminationException
            }

            $terminationDiagnostic = if ([string]::IsNullOrWhiteSpace($terminationFailure)) {
                ''
            }
            else {
                " Kill diagnostic before the retained handle confirmed exit: $terminationFailure"
            }
            if ($terminationDiagnostic.Length -gt 8192) {
                $terminationDiagnostic = $terminationDiagnostic.Substring(0, 8192)
            }
            $timeoutException = [System.TimeoutException]::new(
                "$DisplayName timed out after $TimeoutMilliseconds ms; retained handle confirmed child PID $childProcessId exited.$terminationDiagnostic")
            $timeoutException.Data['GatewaySmokeChildTerminationConfirmed'] = $true
            throw $timeoutException
        }

        $process.WaitForExit()
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StandardOutput = $standardOutputTask.GetAwaiter().GetResult()
            StandardError = $standardErrorTask.GetAwaiter().GetResult()
            ProcessId = $childProcessId
        }
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-GatewaySmokeFlaUiCommand {
    param(
        [string[]]$Arguments,
        [ValidateRange(1, 600000)][int]$TimeoutMilliseconds = 20000,
        [string]$ExecutablePath
    )

    $resolvedExecutablePath = if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
        (Get-Command flaui -CommandType Application -ErrorAction Stop).Source
    }
    else {
        [System.IO.Path]::GetFullPath($ExecutablePath)
    }
    $displayName = "flaui $($Arguments -join ' ')"
    $processResult = Invoke-GatewaySmokeBoundedProcess `
        -ExecutablePath $resolvedExecutablePath `
        -Arguments $Arguments `
        -TimeoutMilliseconds $TimeoutMilliseconds `
        -DisplayName $displayName
    if ([int]$processResult.ExitCode -ne 0) {
        $diagnostic = (([string]$processResult.StandardOutput).Trim() + [Environment]::NewLine +
            ([string]$processResult.StandardError).Trim()).Trim()
        if ($diagnostic.Length -gt 8192) {
            $diagnostic = $diagnostic.Substring(0, 8192)
        }
        throw "FlaUI command failed (exit $($processResult.ExitCode)): $displayName`n$diagnostic"
    }

    return $processResult
}

function Invoke-FlaUiJson {
    param(
        [string[]]$Arguments,
        [ValidateRange(1, 600000)][int]$TimeoutMilliseconds = 20000
    )

    $displayName = "flaui $($Arguments -join ' ')"
    $processResult = Invoke-GatewaySmokeFlaUiCommand `
        -Arguments $Arguments `
        -TimeoutMilliseconds $TimeoutMilliseconds
    $text = ([string]$processResult.StandardOutput).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        $text = ([string]$processResult.StandardError).Trim()
    }
    try {
        $payload = $text | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "FlaUI returned invalid JSON for 'flaui $($Arguments -join ' ')': $text"
    }

    if ($payload.PSObject.Properties.Name -contains 'success' -and -not $payload.success) {
        $errorText = if ($payload.PSObject.Properties.Name -contains 'error') { $payload.error } else { $text }
        throw "FlaUI command reported failure: $errorText"
    }

    return $payload
}

function Complete-GatewaySmokeFlaUiConnectionCleanup {
    param([Parameter(Mandatory)][scriptblock]$JsonInvoker)

    $attemptErrors = [System.Collections.Generic.List[string]]::new()
    try {
        $null = & $JsonInvoker -Arguments @('disconnect')
    }
    catch {
        if ($_.Exception.Data.Contains('GatewaySmokeChildTerminationConfirmed') -and
            -not [bool]$_.Exception.Data['GatewaySmokeChildTerminationConfirmed']) {
            throw
        }
        $attemptErrors.Add("disconnect: $($_.Exception.Message)")
    }

    $status = $null
    try {
        $status = & $JsonInvoker -Arguments @('status')
        if ($null -eq $status -or
            -not ($status.PSObject.Properties.Name -contains 'data') -or
            $null -eq $status.data -or
            -not ($status.data.PSObject.Properties.Name -contains 'connected')) {
            throw 'FlaUI status omitted data.connected.'
        }
    }
    catch {
        if ($_.Exception.Data.Contains('GatewaySmokeChildTerminationConfirmed') -and
            -not [bool]$_.Exception.Data['GatewaySmokeChildTerminationConfirmed']) {
            throw
        }
        $attemptErrors.Add("status: $($_.Exception.Message)")
        $status = $null
    }

    if ($null -ne $status -and -not [bool]$status.data.connected) {
        return [pscustomobject]@{
            Status = 'completed'
            AttemptErrors = @($attemptErrors)
        }
    }

    $details = ($attemptErrors -join '; ')
    if ($details.Length -gt 8192) {
        $details = $details.Substring(0, 8192)
    }
    if ($null -ne $status) {
        throw "FlaUI is still connected after cleanup. $details"
    }
    throw "FlaUI disconnected state could not be confirmed after cleanup. $details"
}

function Complete-GatewaySmokeFlaUiServiceCleanup {
    param(
        [Parameter(Mandatory)][scriptblock]$RawInvoker,
        [Parameter(Mandatory)][scriptblock]$JsonInvoker,
        [ValidateRange(0, 5000)][int]$RetryDelayMilliseconds = 250
    )

    $attemptErrors = [System.Collections.Generic.List[string]]::new()
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        try {
            $null = & $RawInvoker -Arguments @('service', 'stop')
        }
        catch {
            if ($_.Exception.Data.Contains('GatewaySmokeChildTerminationConfirmed') -and
                -not [bool]$_.Exception.Data['GatewaySmokeChildTerminationConfirmed']) {
                throw
            }
            $attemptErrors.Add("stop attempt $attempt`: $($_.Exception.Message)")
        }
        if ($attempt -lt 2 -and $RetryDelayMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $RetryDelayMilliseconds
        }
    }

    $status = $null
    try {
        $status = & $JsonInvoker -Arguments @('service', 'status')
        if ($null -eq $status -or
            -not ($status.PSObject.Properties.Name -contains 'running')) {
            throw 'FlaUI service status omitted running.'
        }
    }
    catch {
        if ($_.Exception.Data.Contains('GatewaySmokeChildTerminationConfirmed') -and
            -not [bool]$_.Exception.Data['GatewaySmokeChildTerminationConfirmed']) {
            throw
        }
        $attemptErrors.Add("service status: $($_.Exception.Message)")
        $status = $null
    }

    if ($null -ne $status -and -not [bool]$status.running) {
        return [pscustomobject]@{
            Status = 'completed'
            AttemptErrors = @($attemptErrors)
        }
    }

    $details = ($attemptErrors -join '; ')
    if ($details.Length -gt 8192) {
        $details = $details.Substring(0, 8192)
    }
    if ($null -ne $status) {
        throw "FlaUI service is still running after cleanup. $details"
    }
    throw "FlaUI stopped state could not be confirmed after cleanup. $details"
}

function Get-GatewaySmokeFlaUiDesktopEvidence {
    param(
        [Parameter(Mandatory)][string]$GatewayScreenshotPath,
        [Parameter(Mandatory)][string]$FlaUiScreenshotPath,
        [Parameter(Mandatory)][string]$ArtifactPath,
        [Parameter(Mandatory)][scriptblock]$FlaUiInvoker
    )

    $resolvedGatewayScreenshotPath = [System.IO.Path]::GetFullPath($GatewayScreenshotPath)
    $resolvedFlaUiScreenshotPath = [System.IO.Path]::GetFullPath($FlaUiScreenshotPath)
    $resolvedArtifactPath = [System.IO.Path]::GetFullPath($ArtifactPath)
    if ($resolvedGatewayScreenshotPath.Equals(
            $resolvedFlaUiScreenshotPath,
            [StringComparison]::OrdinalIgnoreCase) -or
        $resolvedGatewayScreenshotPath.Equals(
            $resolvedArtifactPath,
            [StringComparison]::OrdinalIgnoreCase) -or
        $resolvedFlaUiScreenshotPath.Equals(
            $resolvedArtifactPath,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Gateway, FlaUI, and FlaUI evidence artifact paths must be distinct.'
    }

    foreach ($outputPath in @($resolvedFlaUiScreenshotPath, $resolvedArtifactPath)) {
        $outputDirectory = [System.IO.Path]::GetDirectoryName($outputPath)
        if ([string]::IsNullOrWhiteSpace($outputDirectory) -or
            -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
            throw "FlaUI evidence output directory does not exist: $outputDirectory"
        }
    }

    if (Test-Path -LiteralPath $resolvedArtifactPath) {
        throw "FlaUI evidence artifact already exists: $resolvedArtifactPath"
    }

    $status = 'completed'
    $windowCount = $null
    $screenshot = $null
    $errorDetail = $null
    $missingMandatoryGatewayScreenshot = $false
    $fatalFlaUiChildTerminationFailure = $false
    try {
        if (Test-Path -LiteralPath $resolvedFlaUiScreenshotPath) {
            [System.IO.File]::Delete($resolvedFlaUiScreenshotPath)
        }

        $windows = & $FlaUiInvoker -Arguments @('window', 'list')
        if ($null -eq $windows -or
            -not ($windows.PSObject.Properties.Name -contains 'data')) {
            throw 'FlaUI window listing omitted its data collection.'
        }
        $windowCount = @($windows.data).Count

        $null = & $FlaUiInvoker -Arguments @(
            'screenshot',
            '--output',
            $resolvedFlaUiScreenshotPath)
        if (-not (Test-Path -LiteralPath $resolvedFlaUiScreenshotPath -PathType Leaf) -or
            (Get-Item -LiteralPath $resolvedFlaUiScreenshotPath -Force).Length -le 0) {
            throw "FlaUI did not create a non-empty screenshot at $resolvedFlaUiScreenshotPath"
        }

        $screenshot = $resolvedFlaUiScreenshotPath
    }
    catch {
        if ($_.Exception.Data.Contains('GatewaySmokeChildTerminationConfirmed') -and
            -not [bool]$_.Exception.Data['GatewaySmokeChildTerminationConfirmed']) {
            $fatalFlaUiChildTerminationFailure = $true
        }
        $errorDetail = [string]$_.Exception.Message
        if ([string]::IsNullOrWhiteSpace($errorDetail)) {
            $errorDetail = 'FlaUI desktop evidence failed without exception detail.'
        }
        elseif ($errorDetail.Length -gt 8192) {
            $errorDetail = $errorDetail.Substring(0, 8192)
        }

        if (Test-Path -LiteralPath $resolvedFlaUiScreenshotPath -PathType Leaf) {
            try {
                [System.IO.File]::Delete($resolvedFlaUiScreenshotPath)
            }
            catch {
                $partialCleanupDetail = " Partial FlaUI screenshot cleanup failed: $($_.Exception.Message)"
                $errorDetail = $errorDetail + $partialCleanupDetail
                if ($errorDetail.Length -gt 8192) {
                    $errorDetail = $errorDetail.Substring(0, 8192)
                }
            }
        }

        $status = 'unavailable'
        $screenshot = $null
    }

    $gatewayScreenshotExists =
        (Test-Path -LiteralPath $resolvedGatewayScreenshotPath -PathType Leaf) -and
        (Get-Item -LiteralPath $resolvedGatewayScreenshotPath -Force).Length -gt 0
    $missingMandatoryGatewayScreenshot = -not $gatewayScreenshotExists

    $evidence = [pscustomobject][ordered]@{
        Status = $status
        WindowCount = $windowCount
        Screenshot = $screenshot
        Error = $errorDetail
    }
    $json = $evidence | ConvertTo-Json -Depth 5
    $temporaryArtifactPath = $resolvedArtifactPath + '.tmp-' + [guid]::NewGuid().ToString('N')
    try {
        [System.IO.File]::WriteAllText(
            $temporaryArtifactPath,
            $json,
            [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::Move($temporaryArtifactPath, $resolvedArtifactPath)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryArtifactPath) {
            [System.IO.File]::Delete($temporaryArtifactPath)
        }
    }

    if ($fatalFlaUiChildTerminationFailure) {
        throw $errorDetail
    }

    if ($missingMandatoryGatewayScreenshot) {
        throw "FlaUI desktop evidence failed and the mandatory Gateway screenshot is missing or empty: $errorDetail"
    }

    return $evidence
}

function Invoke-GatewayGet {
    param(
        [string]$Uri,
        [string]$Token,
        [string]$RequestId
    )

    $headers = @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    }
    return Invoke-TrackedGatewayRequest -Method 'GET' -Uri $Uri -RequestId $RequestId -Operation {
        Invoke-WebRequest -Uri $Uri -Method Get -Headers $headers -TimeoutSec 20 -SkipHttpErrorCheck
    }
}

function Invoke-GatewayTextPost {
    param(
        [string]$Uri,
        [string]$Token,
        [string]$RequestId,
        [string]$Source
    )

    $headers = @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    }
    return Invoke-TrackedGatewayRequest -Method 'POST' -Uri $Uri -RequestId $RequestId -Operation {
        Invoke-WebRequest `
            -Uri $Uri `
            -Method Post `
            -Headers $headers `
            -ContentType 'text/plain; charset=utf-8' `
            -Body ([System.Text.Encoding]::UTF8.GetBytes($Source)) `
            -TimeoutSec 30 `
            -SkipHttpErrorCheck
    }
}

function Invoke-GatewayJsonPost {
    param(
        [string]$Uri,
        [string]$Token,
        [string]$RequestId,
        [Parameter(Mandatory)]
        [object]$Body,
        [int]$TimeoutSec = 30
    )

    $headers = @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    }
    $json = if ($Body -is [string]) {
        [string]$Body
    }
    else {
        $Body | ConvertTo-Json -Compress -Depth 12
    }
    return Invoke-TrackedGatewayRequest -Method 'POST' -Uri $Uri -RequestId $RequestId -Operation {
        Invoke-WebRequest `
            -Uri $Uri `
            -Method Post `
            -Headers $headers `
            -ContentType 'application/json; charset=utf-8' `
            -Body ([System.Text.Encoding]::UTF8.GetBytes($json)) `
            -TimeoutSec $TimeoutSec `
            -SkipHttpErrorCheck
    }
}

function Write-HostRequestRecord {
    param([Parameter(Mandatory)][object]$Record)

    $script:lastHostRequestRecord = $Record
    if ([string]::IsNullOrWhiteSpace($script:hostRequestJournalPath)) {
        return
    }

    $Record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $script:hostRequestJournalPath -Encoding UTF8
}

function Invoke-TrackedGatewayRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [string]$RequestId,
        [Parameter(Mandatory)][scriptblock]$Operation
    )

    $startedUtc = [datetime]::UtcNow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $record = [ordered]@{
        RequestId = $RequestId
        Method = $Method
        Uri = $Uri
        State = 'started'
        StartedUtc = $startedUtc.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
        CompletedUtc = $null
        ElapsedMilliseconds = $null
        StatusCode = $null
        GatewayErrorCode = $null
        GatewayErrorMessage = $null
        ExceptionType = $null
        ExceptionMessage = $null
    }
    Write-HostRequestRecord -Record $record

    try {
        $response = & $Operation
        $record.StatusCode = [int]$response.StatusCode
        if ($record.StatusCode -ge 400) {
            try {
                $errorEnvelope = $response.Content | ConvertFrom-Json -ErrorAction Stop
                if ($null -ne $errorEnvelope.error) {
                    $record.GatewayErrorCode = [string]$errorEnvelope.error.code
                    $record.GatewayErrorMessage = [string]$errorEnvelope.error.message
                }
            }
            catch {
                # Keep the HTTP status as the primary diagnostic when an error body is not JSON.
            }

            $record.State = if ($record.StatusCode -eq 504 -and
                $record.GatewayErrorCode -eq 'response_timeout_after_start') {
                'timed-out'
            }
            else {
                'failed'
            }
        }
        else {
            $record.State = 'completed'
        }

        return $response
    }
    catch {
        $record.State = if ($_.Exception -is [System.TimeoutException] -or
            $_.Exception.Message -match '(?i)timed out|timeout') { 'timed-out' } else { 'failed' }
        $record.ExceptionType = $_.Exception.GetType().FullName
        $record.ExceptionMessage = $_.Exception.Message
        throw
    }
    finally {
        $record.CompletedUtc = [datetime]::UtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
        $record.ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
        Write-HostRequestRecord -Record $record
    }
}

function Save-GatewayScreenshot {
    param(
        [Parameter(Mandatory)][string]$BaseUrl,
        [Parameter(Mandatory)][string]$Token,
        [Parameter(Mandatory)][string]$RequestId,
        [Parameter(Mandatory)][string]$ArtifactPath
    )

    $headers = @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    }
    $response = Invoke-TrackedGatewayRequest `
        -Method 'POST' `
        -Uri "$BaseUrl/screenshots" `
        -RequestId $RequestId `
        -Operation {
            Invoke-WebRequest `
                -Uri "$BaseUrl/screenshots" `
                -Method Post `
                -Headers $headers `
                -ContentType 'application/json' `
                -Body '{}' `
                -TimeoutSec 30 `
                -OutFile $ArtifactPath `
                -PassThru `
                -SkipHttpErrorCheck
        }
    if ([int]$response.StatusCode -ne 200) {
        throw "Gateway screenshot '$RequestId' failed with HTTP $([int]$response.StatusCode)."
    }

    $pngSignature = @(Get-Content -LiteralPath $ArtifactPath -AsByteStream -TotalCount 8)
    $expectedPngSignature = @(0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a)
    if (($pngSignature -join ',') -ne ($expectedPngSignature -join ',')) {
        throw "Gateway screenshot is not a valid PNG: $ArtifactPath"
    }
}

function Capture-OwnedProcessDump {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$DumpPath
    )

    $rundll32 = (Get-Command rundll32.exe -ErrorAction SilentlyContinue).Source
    if ([string]::IsNullOrWhiteSpace($rundll32)) {
        return [pscustomobject]@{ Status = 'unavailable'; Path = $null; Detail = 'rundll32.exe was not found.' }
    }

    $comSvcs = Join-Path $env:SystemRoot 'System32\comsvcs.dll'
    if (-not (Test-Path -LiteralPath $comSvcs -PathType Leaf)) {
        return [pscustomobject]@{ Status = 'unavailable'; Path = $null; Detail = "comsvcs.dll was not found at $comSvcs" }
    }

    $helper = $null
    try {
        $arguments = @(
            "`"$comSvcs`",MiniDump",
            [string]$Process.Id,
            "`"$DumpPath`"",
            'full'
        )
        $helper = Start-Process `
            -FilePath $rundll32 `
            -ArgumentList $arguments `
            -PassThru `
            -WindowStyle Hidden
        if (-not $helper.WaitForExit(20000)) {
            Stop-Process -Id $helper.Id -Force -ErrorAction SilentlyContinue
            return [pscustomobject]@{ Status = 'timed-out'; Path = $null; Detail = 'Dump helper exceeded 20 seconds.' }
        }

        if ($helper.ExitCode -eq 0 -and (Test-Path -LiteralPath $DumpPath -PathType Leaf)) {
            return [pscustomobject]@{ Status = 'captured'; Path = $DumpPath; Detail = 'Windows comsvcs MiniDump completed.' }
        }

        return [pscustomobject]@{
            Status = 'failed'
            Path = if (Test-Path -LiteralPath $DumpPath -PathType Leaf) { $DumpPath } else { $null }
            Detail = "Dump helper exited with code $($helper.ExitCode)."
        }
    }
    catch {
        return [pscustomobject]@{ Status = 'failed'; Path = $null; Detail = $_.Exception.Message }
    }
    finally {
        if ($null -ne $helper) {
            $helper.Dispose()
        }
    }
}

function Stop-OwnedProcessGracefully {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$DumpPath
    )

    $Process.Refresh()
    if ($Process.HasExited) {
        return [pscustomobject]@{
            Method = 'already-exited'
            CloseRequested = $false
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $null
        }
    }

    $closeRequested = $Process.CloseMainWindow()
    if ($Process.WaitForExit(15000)) {
        return [pscustomobject]@{
            Method = 'window-close'
            CloseRequested = $closeRequested
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $null
        }
    }

    $dump = Capture-OwnedProcessDump -Process $Process -DumpPath $DumpPath
    $Process.Refresh()
    if ($Process.HasExited) {
        return [pscustomobject]@{
            Method = 'exited-during-dump'
            CloseRequested = $closeRequested
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $dump
        }
    }

    Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    Wait-Process -Id $Process.Id -Timeout 15 -ErrorAction SilentlyContinue
    $Process.Refresh()
    if (-not $Process.HasExited) {
        throw "Owned RimWorld PID $($Process.Id) remained alive after the exact-PID force fallback."
    }

    return [pscustomobject]@{
        Method = 'force-fallback'
        CloseRequested = $closeRequested
        Forced = $true
        ExitCode = $Process.ExitCode
        Dump = $dump
    }
}

function Assert-SafeGatewayArtifactPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$RootPath
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($RootPath)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = $resolvedRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith(
            $rootPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Gateway credential artifact escaped its exact run root: $resolvedPath"
    }

    $current = [System.IO.FileInfo]::new($resolvedPath).Directory
    while ($null -ne $current) {
        if ($current.Exists -and
            ($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Gateway credential artifact traverses a reparse point: $($current.FullName)"
        }

        if ([string]::Equals(
                $current.FullName,
                $resolvedRoot,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            return $resolvedPath
        }

        $current = $current.Parent
    }

    throw "Gateway credential artifact root is not an ancestor: $resolvedPath"
}

function Test-FileContainsBearerToken {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Token
    )

    if ([string]::IsNullOrEmpty($Token)) {
        return $false
    }

    if ($Token.Length -gt 1024 -or $Token -cnotmatch '^[\x21-\x7e]+$') {
        throw 'A retained bearer token must be printable ASCII and at most 1024 characters.'
    }

    $tokenBytes = [System.Text.Encoding]::ASCII.GetBytes($Token)
    $buffer = [byte[]]::new(64 * 1024)
    $tail = [byte[]]::new(0)
    $stream = [System.IO.FileStream]::new(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete,
        $buffer.Length,
        [System.IO.FileOptions]::SequentialScan)
    try {
        while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $combined = [byte[]]::new($tail.Length + $read)
            if ($tail.Length -gt 0) {
                [System.Array]::Copy($tail, 0, $combined, 0, $tail.Length)
            }
            [System.Array]::Copy($buffer, 0, $combined, $tail.Length, $read)
            $text = [System.Text.Encoding]::ASCII.GetString($combined)
            if ($text.IndexOf($Token, [System.StringComparison]::Ordinal) -ge 0) {
                return $true
            }

            $tailLength = [Math]::Min($tokenBytes.Length - 1, $combined.Length)
            if ($tailLength -le 0) {
                $tail = [byte[]]::new(0)
                continue
            }

            $tail = [byte[]]::new($tailLength)
            [System.Array]::Copy(
                $combined,
                $combined.Length - $tailLength,
                $tail,
                0,
                $tailLength)
        }
    }
    finally {
        $stream.Dispose()
    }

    return $false
}

function Assert-NoRetainedBearerToken {
    param(
        [Parameter(Mandatory)][string]$RootPath,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$BearerTokens
    )

    if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
        return
    }

    $root = Get-Item -LiteralPath $RootPath -Force
    if (($root.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing to scan a reparse-point run directory for retained credentials: $RootPath"
    }

    $tokens = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($token in $BearerTokens) {
        if (-not [string]::IsNullOrEmpty($token)) {
            $null = $tokens.Add($token)
        }
    }
    if ($tokens.Count -eq 0) {
        return
    }

    $entries = @(Get-ChildItem -LiteralPath $root.FullName -Force -Recurse)
    foreach ($entry in $entries) {
        if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to scan a reparse-point artifact for retained credentials: $($entry.FullName)"
        }

        if ($entry.PSIsContainer) {
            continue
        }

        foreach ($token in $tokens) {
            if (Test-FileContainsBearerToken -Path $entry.FullName -Token $token) {
                throw "Retained Gateway artifact still retains a bearer token: $($entry.FullName)"
            }
        }
    }
}

function Read-GatewayCredentialManifest {
    param([Parameter(Mandatory)][string]$Path)

    $file = Get-Item -LiteralPath $Path -Force
    if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $file.Length -le 0 -or
        $file.Length -gt 64KB) {
        throw "Gateway credential manifest is linked, empty, or exceeds its byte limit: $Path"
    }

    $stream = $null
    $document = $null
    try {
        $stream = [System.IO.FileStream]::new(
            $file.FullName,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::Read,
            4096,
            [System.IO.FileOptions]::SequentialScan)
        if ($stream.Length -le 0 -or $stream.Length -gt 64KB) {
            throw "Gateway credential manifest is empty or exceeds its byte limit: $Path"
        }

        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) {
                throw "Gateway credential manifest ended before its bounded payload was read: $Path"
            }

            $offset += $read
        }

        $json = [System.Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        $document = [System.Text.Json.JsonDocument]::Parse($json)
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            throw 'Gateway credential manifest root must be one JSON object.'
        }

        $runIds = [System.Collections.Generic.List[string]]::new()
        $processIds = [System.Collections.Generic.List[int]]::new()
        $states = [System.Collections.Generic.List[string]]::new()
        $bearerTokens = [System.Collections.Generic.List[string]]::new()
        foreach ($property in $document.RootElement.EnumerateObject()) {
            $propertyName = [string]$property.Name
            if ($propertyName -ceq 'runId') {
                if ($property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    throw "Gateway credential manifest property 'runId' must be a JSON string."
                }

                $runIds.Add([string]$property.Value.GetString())
                continue
            }
            if ($propertyName -ceq 'processId') {
                if ($property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) {
                    throw "Gateway credential manifest property 'processId' must be a JSON integer."
                }

                $processId = 0
                if (-not $property.Value.TryGetInt32([ref]$processId)) {
                    throw "Gateway credential manifest property 'processId' must be a JSON Int32."
                }

                $processIds.Add($processId)
                continue
            }
            if ($propertyName -ceq 'state') {
                if ($property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    throw "Gateway credential manifest property 'state' must be a JSON string."
                }

                $states.Add([string]$property.Value.GetString())
                continue
            }
            if ([string]::Equals(
                    $propertyName,
                    'token',
                    [System.StringComparison]::OrdinalIgnoreCase) -and
                $property.Value.ValueKind -eq [System.Text.Json.JsonValueKind]::String) {
                $tokenValue = [string]$property.Value.GetString()
                if (-not [string]::IsNullOrEmpty($tokenValue)) {
                    $bearerTokens.Add($tokenValue)
                }
            }
        }

        if ($runIds.Count -ne 1 -or $processIds.Count -ne 1 -or $states.Count -ne 1) {
            throw 'Exactly one case-sensitive runId, processId, and state property is required.'
        }

        return [pscustomobject]@{
            runId = $runIds[0]
            processId = $processIds[0]
            state = $states[0]
            BearerTokens = @($bearerTokens)
            RawJson = $json
        }
    }
    catch {
        throw "Gateway credential manifest is not valid JSON: $Path. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $document) {
            $document.Dispose()
        }
        if ($null -ne $stream) {
            $stream.Dispose()
        }
    }
}

function ConvertTo-GatewayCredentialFreeManifestJson {
    param(
        [Parameter(Mandatory)][string]$Json,
        [Parameter(Mandatory)][string]$StoppedUtc
    )

    $document = $null
    $output = $null
    $writer = $null
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($Json)
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            throw 'Gateway credential manifest root must be one JSON object.'
        }

        $output = [System.IO.MemoryStream]::new()
        $writer = [System.Text.Json.Utf8JsonWriter]::new($output)
        $writer.WriteStartObject()
        foreach ($property in $document.RootElement.EnumerateObject()) {
            $propertyName = [string]$property.Name
            if ([string]::Equals(
                    $propertyName,
                    'token',
                    [System.StringComparison]::OrdinalIgnoreCase) -or
                [string]::Equals(
                    $propertyName,
                    'state',
                    [System.StringComparison]::OrdinalIgnoreCase) -or
                [string]::Equals(
                    $propertyName,
                    'stoppedUtc',
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $property.WriteTo($writer)
        }

        $writer.WriteString('state', 'host-sanitized')
        $writer.WriteString('stoppedUtc', $StoppedUtc)
        $writer.WriteEndObject()
        $writer.Flush()
        return [System.Text.Encoding]::UTF8.GetString($output.ToArray())
    }
    finally {
        if ($null -ne $writer) {
            $writer.Dispose()
        }
        if ($null -ne $output) {
            $output.Dispose()
        }
        if ($null -ne $document) {
            $document.Dispose()
        }
    }
}

function Set-GatewayCredentialFreeSessionManifestAtomically {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$SanitizedJson,
        [Parameter(Mandatory)][string]$RunDirectory,
        [AllowNull()][scriptblock]$AfterReplace
    )

    $resolvedPath = Assert-SafeGatewayArtifactPath `
        -Path $Path `
        -RootPath $RunDirectory
    $temporaryPath = $resolvedPath + '.sanitize.' + [Guid]::NewGuid().ToString('N')
    $null = Assert-SafeGatewayArtifactPath `
        -Path $temporaryPath `
        -RootPath $RunDirectory
    try {
        [System.IO.File]::WriteAllText(
            $temporaryPath,
            $SanitizedJson,
            [System.Text.UTF8Encoding]::new($false))
        # PowerShell's overload binder coerces a null backup path to an empty string, which
        # File.Replace rejects. Reflection preserves the documented null/no-backup argument.
        $replaceMethod = [System.IO.File].GetMethod(
            'Replace',
            [Type[]]@([string], [string], [string]))
        if ($null -eq $replaceMethod) {
            throw 'The runtime does not expose the expected File.Replace(string,string,string) overload.'
        }
        $null = $replaceMethod.Invoke(
            $null,
            [object[]]@($temporaryPath, $resolvedPath, $null))
        if ($null -ne $AfterReplace) {
            & $AfterReplace
        }
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

function Protect-GatewayCredentialArtifacts {
    param(
        [Parameter(Mandatory)][string]$RunDirectory,
        [Parameter(Mandatory)][string]$SavedDataPath,
        [Parameter(Mandatory)][int]$ExpectedProcessId,
        [AllowNull()][string]$ExpectedRunId,
        [AllowNull()][string]$BearerToken,
        [Parameter(Mandatory)][bool]$ProcessHasExited
    )

    if (-not $ProcessHasExited) {
        throw "Owned RimWorld PID $ExpectedProcessId must be confirmed dead before credential artifacts are sanitized."
    }

    $resolvedRunDirectory = [System.IO.Path]::GetFullPath($RunDirectory)
    $resolvedSavedDataPath = [System.IO.Path]::GetFullPath($SavedDataPath)
    $savedDataPrefix = $resolvedRunDirectory.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedSavedDataPath.StartsWith(
            $savedDataPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The disposable SavedData path is not inside the exact smoke run directory.'
    }

    $knownTokens = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    if (-not [string]::IsNullOrEmpty($BearerToken)) {
        $null = $knownTokens.Add($BearerToken)
    }

    $resolvedRunId = $ExpectedRunId
    if (-not [string]::IsNullOrWhiteSpace($resolvedRunId) -and
        $resolvedRunId -cnotmatch '^[A-Za-z0-9_-]{1,128}$') {
        throw "Gateway run ID is not a safe session path segment: $resolvedRunId"
    }

    $currentPath = Assert-SafeGatewayArtifactPath `
        -Path (Join-Path $resolvedSavedDataPath 'DevGateway\current.json') `
        -RootPath $resolvedRunDirectory
    $matchingCurrent = $false
    if (Test-Path -LiteralPath $currentPath -PathType Leaf) {
        $currentManifest = Read-GatewayCredentialManifest -Path $currentPath
        if ([int]$currentManifest.processId -eq $ExpectedProcessId) {
            $currentRunId = [string]$currentManifest.runId
            if ($currentRunId -cnotmatch '^[A-Za-z0-9_-]{1,128}$') {
                throw "Matching Gateway current.json contains an unsafe run ID: $currentRunId"
            }
            if (-not [string]::IsNullOrWhiteSpace($resolvedRunId) -and
                $currentRunId -cne $resolvedRunId) {
                throw "Gateway current.json run ID '$currentRunId' does not match the observed session '$resolvedRunId'."
            }

            $resolvedRunId = $currentRunId
            foreach ($currentToken in @($currentManifest.BearerTokens)) {
                $null = $knownTokens.Add([string]$currentToken)
            }
            $matchingCurrent = $true
        }
    }

    $sessionRoot = Assert-SafeGatewayArtifactPath `
        -Path (Join-Path $resolvedSavedDataPath 'DevGateway\Sessions') `
        -RootPath $resolvedRunDirectory
    $sessionPaths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    if (-not [string]::IsNullOrWhiteSpace($resolvedRunId)) {
        $null = $sessionPaths.Add((Join-Path $sessionRoot "$resolvedRunId\session.json"))
    }
    if (Test-Path -LiteralPath $sessionRoot -PathType Container) {
        $sessionRootItem = Get-Item -LiteralPath $sessionRoot -Force
        if (($sessionRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to inspect a reparse-point Gateway session root: $sessionRoot"
        }
        $sessionDirectories = @(Get-ChildItem -LiteralPath $sessionRoot -Directory -Force)
        if ($sessionDirectories.Count -gt 128) {
            throw 'Gateway session directory exceeds the bounded credential-cleanup limit of 128 entries.'
        }
        foreach ($sessionDirectory in $sessionDirectories) {
            if (($sessionDirectory.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Refusing to inspect a reparse-point Gateway session directory: $($sessionDirectory.FullName)"
            }
            if ($sessionDirectory.Name -cnotmatch '^[A-Za-z0-9_-]{1,128}$') {
                throw "Gateway session directory has an unsafe run ID: $($sessionDirectory.Name)"
            }

            $null = $sessionPaths.Add((Join-Path $sessionDirectory.FullName 'session.json'))
        }
    }

    $matchingSessions = [System.Collections.Generic.List[object]]::new()
    $matchingSessionRunIds = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($sessionPathCandidate in $sessionPaths) {
        $sessionPath = Assert-SafeGatewayArtifactPath `
            -Path $sessionPathCandidate `
            -RootPath $resolvedRunDirectory
        if (-not (Test-Path -LiteralPath $sessionPath -PathType Leaf)) {
            continue
        }

        $sessionManifest = Read-GatewayCredentialManifest -Path $sessionPath
        if ([int]$sessionManifest.processId -ne $ExpectedProcessId) {
            continue
        }

        $sessionRunId = [string]$sessionManifest.runId
        if ($sessionRunId -cnotmatch '^[A-Za-z0-9_-]{1,128}$' -or
            [System.IO.Path]::GetFileName((Split-Path -Parent $sessionPath)) -cne $sessionRunId) {
            throw "Matching Gateway session manifest has an unsafe or inconsistent run ID: $sessionPath"
        }

        $null = $matchingSessionRunIds.Add($sessionRunId)
        foreach ($sessionToken in @($sessionManifest.BearerTokens)) {
            $null = $knownTokens.Add([string]$sessionToken)
        }
        $matchingSessions.Add([pscustomobject]@{
            Path = $sessionPath
            Manifest = $sessionManifest
        })
    }

    $reportedRunId = $resolvedRunId
    if ([string]::IsNullOrWhiteSpace($reportedRunId) -and
        $matchingSessionRunIds.Count -eq 1) {
        $reportedRunId = @($matchingSessionRunIds)[0]
    }
    elseif ([string]::IsNullOrWhiteSpace($reportedRunId)) {
        $reportedRunId = $null
    }

    $sanitizationFailures = [System.Collections.Generic.List[string]]::new()
    $sanitizedSessionCount = 0
    foreach ($matchingSession in $matchingSessions) {
        try {
            $sessionManifest = $matchingSession.Manifest
            $stoppedUtc = [datetime]::UtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
            $sanitizedJson = ConvertTo-GatewayCredentialFreeManifestJson `
                -Json ([string]$sessionManifest.RawJson) `
                -StoppedUtc $stoppedUtc
            Set-GatewayCredentialFreeSessionManifestAtomically `
                -Path ([string]$matchingSession.Path) `
                -SanitizedJson $sanitizedJson `
                -RunDirectory $resolvedRunDirectory
            $sanitizedSessionCount++
        }
        catch {
            $sessionFailure = $_.Exception.Message
            # A credential-bearing session file is less valuable than the unrestricted token it
            # contains. If atomic redaction fails after the process is dead, remove that exact file.
            foreach ($credentialPath in @([string]$matchingSession.Path)) {
                try {
                    if (-not [string]::IsNullOrWhiteSpace($credentialPath) -and
                        (Test-Path -LiteralPath $credentialPath -PathType Leaf)) {
                        Remove-Item -LiteralPath $credentialPath -Force
                    }
                }
                catch {
                    $sessionFailure += " Exact-file fallback removal also failed for '$credentialPath': $($_.Exception.Message)"
                }
            }
            $sanitizationFailures.Add("Session '$($matchingSession.Path)': $sessionFailure")
        }
    }

    if ($matchingCurrent -and (Test-Path -LiteralPath $currentPath -PathType Leaf)) {
        try {
            Remove-Item -LiteralPath $currentPath -Force
        }
        catch {
            $sanitizationFailures.Add("Current locator '$currentPath': $($_.Exception.Message)")
        }
    }

    # Full-memory dumps can contain the bearer token in process memory. They are useful only
    # transiently for diagnosis and are never retained after exact-PID cleanup.
    $hangDumpPath = Assert-SafeGatewayArtifactPath `
        -Path (Join-Path $resolvedRunDirectory 'RimWorldWin64-hang.dmp') `
        -RootPath $resolvedRunDirectory
    if (Test-Path -LiteralPath $hangDumpPath -PathType Leaf) {
        try {
            Remove-Item -LiteralPath $hangDumpPath -Force
        }
        catch {
            $sanitizationFailures.Add("Memory dump '$hangDumpPath': $($_.Exception.Message)")
        }
    }

    try {
        Assert-NoRetainedBearerToken `
            -RootPath $resolvedRunDirectory `
            -BearerTokens @($knownTokens)
    }
    catch {
        $sanitizationFailures.Add($_.Exception.Message)
    }

    if ($sanitizationFailures.Count -gt 0) {
        throw "Gateway credential sanitation was incomplete: $($sanitizationFailures -join '; ')"
    }

    return [pscustomobject]@{
        ProcessId = $ExpectedProcessId
        RunId = $reportedRunId
        CurrentLocatorRemoved = $matchingCurrent
        SessionsSanitized = $sanitizedSessionCount
        TokensChecked = $knownTokens.Count
    }
}

function Resolve-IntegrationTestPollResponse {
    param(
        [Parameter(Mandatory)][object]$Response,
        [Parameter(Mandatory)][string]$ReadyArtifactPath,
        [Parameter(Mandatory)][string]$PendingArtifactPath,
        [Parameter(Mandatory)][string]$Operation,
        [switch]$ReadyAlreadyObserved
    )

    $content = [string]$Response.Content
    try {
        $envelope = $content | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        [System.IO.File]::WriteAllText(
            $ReadyArtifactPath,
            $content,
            [System.Text.UTF8Encoding]::new($false))
        throw "$Operation returned invalid JSON with HTTP $([int]$Response.StatusCode). See $ReadyArtifactPath"
    }

    $propertyNames = @($envelope.PSObject.Properties.Name)
    $hasOk = $propertyNames -contains 'ok'
    $hasError = $propertyNames -contains 'error' -and $null -ne $envelope.error
    $errorCode = if ($hasError -and
        @($envelope.error.PSObject.Properties.Name) -contains 'code') {
        [string]$envelope.error.code
    }
    else {
        'unknown_error'
    }
    $retryableProperties = @()
    if ($hasError) {
        $retryableProperties = @($envelope.error.PSObject.Properties |
            Where-Object { [string]$_.Name -ceq 'retryable' })
    }
    $hasExactRetryable = $retryableProperties.Count -eq 1 -and
        $retryableProperties[0].Value -is [bool] -and
        [bool]$retryableProperties[0].Value
    $isExactInitialPending =
        [int]$Response.StatusCode -eq 503 -and
        $hasOk -and
        -not [bool]$envelope.ok -and
        $errorCode -ceq 'integration_test_status_pending' -and
        $hasExactRetryable
    if ($isExactInitialPending) {
        [System.IO.File]::WriteAllText(
            $PendingArtifactPath,
            $content,
            [System.Text.UTF8Encoding]::new($false))
        if ($ReadyAlreadyObserved) {
            throw "$Operation regressed to initial-artifact pending after a ready response. See $PendingArtifactPath"
        }

        return [pscustomobject]@{
            State = 'pending'
            Envelope = $envelope
        }
    }

    [System.IO.File]::WriteAllText(
        $ReadyArtifactPath,
        $content,
        [System.Text.UTF8Encoding]::new($false))
    if ([int]$Response.StatusCode -lt 200 -or
        [int]$Response.StatusCode -ge 300 -or
        -not $hasOk -or
        -not [bool]$envelope.ok) {
        throw "$Operation failed with HTTP $([int]$Response.StatusCode) ($errorCode). See $ReadyArtifactPath"
    }

    return [pscustomobject]@{
        State = 'ready'
        Envelope = $envelope
    }
}

function Assert-GatewaySuccess {
    param(
        [Parameter(Mandatory)]
        [object]$Response,
        [string]$ArtifactPath,
        [string]$Operation
    )

    $Response.Content | Set-Content -LiteralPath $ArtifactPath -Encoding UTF8
    try {
        $envelope = $Response.Content | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "$Operation returned invalid JSON with HTTP $([int]$Response.StatusCode). See $ArtifactPath"
    }

    if ([int]$Response.StatusCode -lt 200 -or
        [int]$Response.StatusCode -ge 300 -or
        -not $envelope.ok) {
        $errorCode = if ($null -ne $envelope.error) { [string]$envelope.error.code } else { 'unknown_error' }
        throw "$Operation failed with HTTP $([int]$Response.StatusCode) ($errorCode). See $ArtifactPath"
    }

    return $envelope
}

function New-GatewayDefSmokeRequest {
    return [ordered]@{
        format = 'json'
        defTypes = @('Verse.ThingDef')
        defNames = @('Steel')
        fieldNames = @('label', 'statBases', 'modExtensions')
        pageSize = 1
    }
}

function Assert-GatewayDefExportSmokeResult {
    param(
        [Parameter(Mandatory)][object]$Response,
        [Parameter(Mandatory)][string]$ArtifactPath
    )

    $content = [string]$Response.Content
    $bodyBytes = [System.Text.Encoding]::UTF8.GetByteCount($content)
    if ($bodyBytes -le 0 -or $bodyBytes -gt 32MB) {
        [System.IO.File]::WriteAllText(
            $ArtifactPath,
            $content,
            [System.Text.UTF8Encoding]::new($false))
        throw "Finalized Def export body must be complete, non-empty, and at most 32 MiB. See $ArtifactPath"
    }

    $envelope = Assert-GatewaySuccess `
        -Response $Response `
        -ArtifactPath $ArtifactPath `
        -Operation 'Finalized Def export'
    if (Test-GatewayDefProjectionFailureMarker -Root $envelope) {
        throw "Finalized Def export contains a projection failure marker. See $ArtifactPath"
    }

    $requiredProperty = {
        param(
            [AllowNull()][object]$Object,
            [string]$Name,
            [string]$Context
        )

        if ($null -eq $Object) {
            throw "$Context is null; required property '$Name' cannot be inspected."
        }

        $matches = @($Object.PSObject.Properties |
            Where-Object { [string]$_.Name -ceq $Name })
        if ($matches.Count -ne 1) {
            throw "$Context must contain exactly one case-sensitive '$Name' property."
        }

        return $matches[0]
    }

    $result = (& $requiredProperty $envelope 'result' 'Finalized Def export envelope').Value
    $items = @((& $requiredProperty $result 'Items' 'Finalized Def export result').Value)
    if ($items.Count -ne 1) {
        throw "Finalized Def export must contain exactly one Steel Verse.ThingDef; found $($items.Count). See $ArtifactPath"
    }

    $item = $items[0]
    $databaseType = (& $requiredProperty $item 'DatabaseType' 'Finalized Def export item').Value
    $defName = (& $requiredProperty $item 'DefName' 'Finalized Def export item').Value
    if ($databaseType -isnot [string] -or
        [string]$databaseType -cne 'Verse.ThingDef' -or
        $defName -isnot [string] -or
        [string]$defName -cne 'Steel') {
        throw "Finalized Def export must contain exactly one Steel Verse.ThingDef. See $ArtifactPath"
    }

    $itemWarnings = & $requiredProperty $item 'Warnings' 'Finalized Def export item'
    if ($itemWarnings.Value -isnot [System.Array] -or @($itemWarnings.Value).Count -ne 0) {
        throw "Finalized Steel Def export must contain no item warnings. See $ArtifactPath"
    }

    $fields = (& $requiredProperty $item 'Fields' 'Finalized Def export item').Value
    if ($null -eq $fields) {
        throw "Finalized Def export fields must contain exactly label, statBases, and modExtensions. See $ArtifactPath"
    }
    $fieldProperties = @($fields.PSObject.Properties)
    $requiredFieldNames = @('label', 'statBases', 'modExtensions')
    $actualFieldNames = @($fieldProperties | ForEach-Object { [string]$_.Name })
    if ($actualFieldNames.Count -ne $requiredFieldNames.Count -or
        @(Compare-Object `
            -CaseSensitive `
            -ReferenceObject @($requiredFieldNames | Sort-Object) `
            -DifferenceObject @($actualFieldNames | Sort-Object)).Count -ne 0) {
        throw "Finalized Def export fields must contain exactly label, statBases, and modExtensions. See $ArtifactPath"
    }

    $label = (& $requiredProperty $fields 'label' 'Finalized Def export Fields').Value
    if ($label -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$label)) {
        throw "Finalized Steel projection must contain a non-empty label. See $ArtifactPath"
    }

    $statBases = @((& $requiredProperty $fields 'statBases' 'Finalized Def export Fields').Value)
    if ($statBases.Count -eq 0) {
        throw "Finalized Steel projection must contain non-empty statBases. See $ArtifactPath"
    }

    $modExtensions = @((& $requiredProperty $fields 'modExtensions' 'Finalized Def export Fields').Value)
    $hasFinalizedProbeMarker = $false
    foreach ($extension in $modExtensions) {
        if ($null -eq $extension) {
            continue
        }

        $markerProperties = @($extension.PSObject.Properties |
            Where-Object { [string]$_.Name -ceq 'marker' })
        if ($markerProperties.Count -eq 1 -and
            $markerProperties[0].Value -is [string] -and
            [string]$markerProperties[0].Value -ceq 'patched-by-gateway-xml') {
            $hasFinalizedProbeMarker = $true
            break
        }
    }
    if (-not $hasFinalizedProbeMarker) {
        throw "Finalized Steel modExtensions must contain the Gateway finalized XML-patch marker. See $ArtifactPath"
    }

    $truncated = (& $requiredProperty $result 'Truncated' 'Finalized Def export result').Value
    if ($truncated -isnot [bool] -or [bool]$truncated) {
        throw "Finalized Def export must be a complete, untruncated page. See $ArtifactPath"
    }

    $nextCursor = (& $requiredProperty $result 'NextCursor' 'Finalized Def export result').Value
    if ($null -ne $nextCursor) {
        throw "Finalized Def export must have a null NextCursor. See $ArtifactPath"
    }

    $sourceWarnings = & $requiredProperty $result 'SourceWarnings' 'Finalized Def export result'
    if ($sourceWarnings.Value -isnot [System.Array] -or @($sourceWarnings.Value).Count -ne 0) {
        throw "Finalized Steel Def export must contain no source warnings. See $ArtifactPath"
    }

    $canonicalXml = (& $requiredProperty $result 'IsCanonicalSourceXml' 'Finalized Def export result').Value
    if ($canonicalXml -isnot [bool] -or [bool]$canonicalXml) {
        throw "Finalized Def export must identify its JSON snapshot as non-canonical source XML. See $ArtifactPath"
    }

    return $envelope
}

function Select-GatewayCameraMovementTarget {
    param(
        [Parameter(Mandatory)][object]$Camera,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Candidates,
        [ValidateRange(1, 10000)][int]$MinimumDistance = 20
    )

    if ($null -eq $Camera.Center -or $null -eq $Camera.ViewRect -or
        [int]$Camera.MapWidth -le 0 -or [int]$Camera.MapHeight -le 0) {
        throw 'Camera movement target selection requires a complete current-camera snapshot.'
    }

    $centerX = [long]$Camera.Center.X
    $centerZ = [long]$Camera.Center.Z
    $leftExtent = [Math]::Max(0L, $centerX - [long]$Camera.ViewRect.MinX)
    $rightExtent = [Math]::Max(0L, [long]$Camera.ViewRect.MaxX - $centerX)
    $bottomExtent = [Math]::Max(0L, $centerZ - [long]$Camera.ViewRect.MinZ)
    $topExtent = [Math]::Max(0L, [long]$Camera.ViewRect.MaxZ - $centerZ)
    $minimumX = $leftExtent
    $maximumX = [long]$Camera.MapWidth - 1L - $rightExtent
    $minimumZ = $bottomExtent
    $maximumZ = [long]$Camera.MapHeight - 1L - $topExtent
    $minimumDistanceSquared = [long]$MinimumDistance * [long]$MinimumDistance

    $scored = foreach ($candidate in $Candidates) {
        if ($null -eq $candidate -or $null -eq $candidate.Position) {
            continue
        }

        $x = [long]$candidate.Position.X
        $z = [long]$candidate.Position.Z
        if ($x -lt $minimumX -or $x -gt $maximumX -or
            $z -lt $minimumZ -or $z -gt $maximumZ) {
            continue
        }

        $deltaX = $x - $centerX
        $deltaZ = $z - $centerZ
        $distanceSquared = ($deltaX * $deltaX) + ($deltaZ * $deltaZ)
        if ($distanceSquared -lt $minimumDistanceSquared) {
            continue
        }

        [pscustomobject]@{
            Thing = $candidate
            DistanceSquared = [long]$distanceSquared
        }
    }

    $selected = @($scored | Sort-Object `
        @{ Expression = { [long]$_.DistanceSquared }; Descending = $true }, `
        @{ Expression = { [string]$_.Thing.Handle }; Ascending = $true } |
        Select-Object -First 1)
    if ($selected.Count -ne 1) {
        throw "No queried object at least $MinimumDistance cells away can be centered without camera edge clamping."
    }

    return $selected[0]
}

if (-not (Test-Path -LiteralPath $RimWorldPath -PathType Container)) {
    Exit-InvalidInput "RimWorld path does not exist: $RimWorldPath"
}

if (-not (Test-Path -LiteralPath $SteamModContentFolder -PathType Container)) {
    Exit-InvalidInput "Steam Workshop content path does not exist: $SteamModContentFolder"
}

$resolvedRimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
$resolvedWorkshopPath = (Resolve-Path -LiteralPath $SteamModContentFolder).Path
$rimWorldExecutable = Join-Path $resolvedRimWorldPath 'RimWorldWin64.exe'
$versionPath = Join-Path $resolvedRimWorldPath 'Version.txt'
$managedPath = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed'
if (-not (Test-Path -LiteralPath $rimWorldExecutable -PathType Leaf)) {
    Exit-InvalidInput "RimWorld executable does not exist: $rimWorldExecutable"
}

if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
    Exit-InvalidInput "RimWorld version file does not exist: $versionPath"
}

$rimWorldVersion = (Get-Content -LiteralPath $versionPath -Raw).Trim()
if (-not $rimWorldVersion.StartsWith('1.6.', [StringComparison]::Ordinal)) {
    Exit-InvalidInput "RimWorld Dev Gateway supports RimWorld 1.6; installed version is '$rimWorldVersion'."
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validatedAdditionalModProjects = [System.Collections.Generic.List[object]]::new()
$additionalModProjectPackages = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($additionalModProjectPath in $AdditionalModProjectPaths) {
    $additionalModProject = Get-GatewaySmokeAdditionalModProject `
        -RepositoryRoot $repositoryRoot `
        -ProjectPath $additionalModProjectPath `
        -AdditionalPackageIds @($validatedAdditionalModIds)
    if (-not $additionalModProjectPackages.Add([string]$additionalModProject.PackageId)) {
        Exit-InvalidInput "Duplicate additional mod project package: '$($additionalModProject.PackageId)'"
    }
    $validatedAdditionalModProjects.Add($additionalModProject)
}
foreach ($expectedIntegrationTest in $validatedExpectedIntegrationTests) {
    $expectedOwner = [string]$expectedIntegrationTest.OwningPackageId
    if ($expectedOwner -ine 'fumblesneeze.rimworlddevgateway' -and
        -not $additionalModProjectPackages.Contains($expectedOwner)) {
        Exit-InvalidInput "Expected product integration test owner '$expectedOwner' requires its repo-local project in -AdditionalModProjectPaths."
    }
}
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = Join-Path $repositoryRoot 'artifacts\GatewaySmoke'
}

$scenarioDirectory = Join-Path $repositoryRoot 'scripts\Scenarios'
try {
    $scenarioPlan = Resolve-GatewaySmokeScenario `
        -ScenarioName $Scenario `
        -ScenarioDirectory $scenarioDirectory `
        -Quicktest ([bool]$Quicktest)
}
catch {
    Exit-InvalidInput $_.Exception.Message
}
$runGatewayRegressionScenario = [bool]$scenarioPlan.RunsGatewayRegression
if ($RequireRawClick -and -not $runGatewayRegressionScenario) {
    Exit-InvalidInput '-RequireRawClick is only valid with -Scenario gateway-regression.'
}
try {
    Assert-GatewayScenarioRequiredPackages `
        -ScenarioPlan $scenarioPlan `
        -PackageIds $activeModIds `
        -PackageState 'configured'
}
catch {
    Exit-InvalidInput $_.Exception.Message
}

if ([string]::IsNullOrWhiteSpace($QuickstartDescriptorPath)) {
    $QuickstartDescriptorPath = Join-Path $repositoryRoot 'scripts\Fixtures\GatewayQuickstartSmoke.json'
}

$resolvedQuickstartDescriptorPath = [System.IO.Path]::GetFullPath($QuickstartDescriptorPath)
if ($runGatewayRegressionScenario -and -not (Test-Path -LiteralPath $resolvedQuickstartDescriptorPath -PathType Leaf)) {
    Exit-InvalidInput "Quickstart descriptor does not exist: $resolvedQuickstartDescriptorPath"
}

$artifactRoot = [System.IO.Path]::GetFullPath($ArtifactsPath)
$runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
$runDirectory = Join-Path $artifactRoot $runId
$savedDataPath = Join-Path $runDirectory 'SavedData'
$configDirectory = Join-Path $savedDataPath 'Config'
$modsConfigPath = Join-Path $configDirectory 'ModsConfig.xml'
$prefsPath = Join-Path $configDirectory 'Prefs.xml'
$playerLogPath = Join-Path $runDirectory 'Player.log'
$screenshotPath = Join-Path $runDirectory 'main-menu.png'
$flaUiEvidencePath = Join-Path $runDirectory 'flaui-evidence.json'
$buildLogPath = Join-Path $runDirectory 'dotnet-build.log'
$integrationBundlePlanLogPath = Join-Path $runDirectory 'integration-test-bundle-plan.log'
$integrationBundleLogPath = Join-Path $runDirectory 'integration-test-bundle.log'
$integrationBundleEvidencePath = Join-Path $runDirectory 'IntegrationTestBundle'
$statusPath = Join-Path $runDirectory 'status.json'
$defExportPath = Join-Path $runDirectory 'def-export.json'
$integrationTestsPath = Join-Path $runDirectory 'integration-tests.json'
$integrationTestsPendingPath = Join-Path $runDirectory 'integration-tests-pending.json'
$uiStatePath = Join-Path $runDirectory 'ui-state.json'
$logsPath = Join-Path $runDirectory 'logs.json'
$postQuickstartStatusPath = Join-Path $runDirectory 'post-quickstart-status.json'
$postQuickstartUiStatePath = Join-Path $runDirectory 'post-quickstart-ui-state.json'
$postQuickstartLogsPath = Join-Path $runDirectory 'post-quickstart-logs.json'
$actionsPath = Join-Path $runDirectory 'actions.json'
$automationsPath = Join-Path $runDirectory 'automations.json'
$gatewayScreenshotPath = Join-Path $runDirectory 'gateway-screenshot.png'
$clickPath = Join-Path $runDirectory 'click.json'
$executionPath = Join-Path $runDirectory 'execution.json'
$executionStatePath = Join-Path $runDirectory 'execution-state.json'
$executionRestorePath = Join-Path $runDirectory 'execution-restore.json'
$quickstartPath = Join-Path $runDirectory 'quickstart.json'
$quickstartReplayPath = Join-Path $runDirectory 'quickstart-replay.json'
$gameStateInitialPath = Join-Path $runDirectory 'game-state-initial.json'
$gameStateEnableDevPath = Join-Path $runDirectory 'game-state-enable-dev.json'
$gameStatePausePath = Join-Path $runDirectory 'game-state-pause.json'
$gameStateSpeedPath = Join-Path $runDirectory 'game-state-speed.json'
$gameStateRestorePath = Join-Path $runDirectory 'game-state-restore.json'
$thingsMapPath = Join-Path $runDirectory 'things-map.json'
$thingsViewPath = Join-Path $runDirectory 'things-view.json'
$thingInspectBuildingPath = Join-Path $runDirectory 'thing-inspect-building.json'
$thingInspectPawnPath = Join-Path $runDirectory 'thing-inspect-pawn.json'
$selectionSetPath = Join-Path $runDirectory 'selection-set.json'
$selectionQueryPath = Join-Path $runDirectory 'selection-query.json'
$selectionRestorePath = Join-Path $runDirectory 'selection-restore.json'
$directSpawnPath = Join-Path $runDirectory 'direct-spawn.json'
$directSpawnReplayPath = Join-Path $runDirectory 'direct-spawn-replay.json'
$directSpawnQueryPath = Join-Path $runDirectory 'direct-spawn-query.json'
$directSpawnCleanupPath = Join-Path $runDirectory 'direct-spawn-cleanup.json'
$directSpawnCleanupQueryPath = Join-Path $runDirectory 'direct-spawn-cleanup-query.json'
$cameraBeforePath = Join-Path $runDirectory 'camera-before.json'
$cameraMovePath = Join-Path $runDirectory 'camera-move.json'
$cameraAfterMoveStatePath = Join-Path $runDirectory 'camera-after-move-state.json'
$cameraZoomPath = Join-Path $runDirectory 'camera-zoom.json'
$cameraAfterZoomStatePath = Join-Path $runDirectory 'camera-after-zoom-state.json'
$cameraRestorePath = Join-Path $runDirectory 'camera-restore.json'
$cameraRestoredStatePath = Join-Path $runDirectory 'camera-restored-state.json'
$cameraBeforeScreenshotPath = Join-Path $runDirectory 'camera-before.png'
$cameraAfterMoveScreenshotPath = Join-Path $runDirectory 'camera-after-move.png'
$cameraAfterZoomScreenshotPath = Join-Path $runDirectory 'camera-after-zoom.png'
$cameraRestoredScreenshotPath = Join-Path $runDirectory 'camera-restored.png'
$debugActionsQueryPath = Join-Path $runDirectory 'debug-actions-query.json'
$debugActionInvokePath = Join-Path $runDirectory 'debug-action-invoke.json'
$pawnGizmosQueryPath = Join-Path $runDirectory 'gizmos-pawn-query.json'
$pawnGizmoTogglePath = Join-Path $runDirectory 'gizmo-pawn-toggle.json'
$pawnGizmosRestoreQueryPath = Join-Path $runDirectory 'gizmos-pawn-restore-query.json'
$pawnGizmoRestorePath = Join-Path $runDirectory 'gizmo-pawn-toggle-restore.json'
$architectGizmosQueryPath = Join-Path $runDirectory 'gizmos-architect-query.json'
$dragInvokePath = Join-Path $runDirectory 'gizmo-drag-invoke.json'
$interactionCurrentPath = Join-Path $runDirectory 'interaction-current.json'
$interactionCancelPath = Join-Path $runDirectory 'interaction-cancel.json'
$dragReinvokePath = Join-Path $runDirectory 'gizmo-drag-reinvoke.json'
$dragApplyPath = Join-Path $runDirectory 'interaction-drag-apply.json'
$planCreatedPath = Join-Path $runDirectory 'interaction-plan-created.json'
$dragRemoveInvokePath = Join-Path $runDirectory 'gizmo-drag-remove-invoke.json'
$dragRemoveApplyPath = Join-Path $runDirectory 'interaction-drag-remove-apply.json'
$interactionFinalPath = Join-Path $runDirectory 'interaction-final.json'
$planCleanupPath = Join-Path $runDirectory 'interaction-plan-cleanup.json'
$scenarioResultPath = Join-Path $runDirectory 'scenario.json'
$interactiveHoldPath = Join-Path $runDirectory 'interactive-hold.json'
$shutdownPath = Join-Path $runDirectory 'shutdown.json'
$hostRequestJournalPath = Join-Path $runDirectory 'last-host-request.json'
$failureDiagnosticsPath = Join-Path $runDirectory 'failure-diagnostics.json'
$processCleanupPath = Join-Path $runDirectory 'process-cleanup.json'
$credentialCleanupPath = Join-Path $runDirectory 'credential-cleanup.json'
$integrationStageCleanupPath = Join-Path $runDirectory 'integration-test-stage-cleanup.json'
$cleanupStatusPath = Join-Path $runDirectory 'cleanup-status.json'
$evidenceSummaryPath = Join-Path $runDirectory 'evidence-summary.json'
$hangDumpPath = Join-Path $runDirectory 'RimWorldWin64-hang.dmp'
$manifestPath = Join-Path $savedDataPath 'DevGateway\current.json'
$script:hostRequestJournalPath = $hostRequestJournalPath
$script:lastHostRequestRecord = $null
$null = New-Item -Path $configDirectory -ItemType Directory -Force
Write-MinimalModsConfig -Path $modsConfigPath -Version $rimWorldVersion
Write-MinimalPrefs -Path $prefsPath
$launchVisible = [bool]$VisibleWindow -or $runGatewayRegressionScenario
$launchWindowStyle = if ($launchVisible) { 'Normal' } else { 'Minimized' }

$normalConfigDirectory = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config'
$normalModsConfigPath = Join-Path $normalConfigDirectory 'ModsConfig.xml'
$normalPrefsPath = Join-Path $normalConfigDirectory 'Prefs.xml'
$normalConfigHashBefore = Get-OptionalFileHash -Path $normalModsConfigPath
$normalPrefsHashBefore = Get-OptionalFileHash -Path $normalPrefsPath
$launchArguments = @(
    "-savedatafolder=`"$savedDataPath`"",
    '-logFile',
    "`"$playerLogPath`""
)
if ($Quicktest) {
    $launchArguments += '-quicktest'
}
if ($RunIntegrationTests) {
    $launchArguments += '-devGatewayRunIntegrationTests'
}
if ($IntegrationFailureProbe) {
    $launchArguments += '-devGatewayForceIntegrationTestFailure'
}

if ($DryRun) {
    Write-Result ([pscustomobject]@{
        Status = 'dry-run'
        Version = $rimWorldVersion
        ActiveMods = $activeModIds
        ExpectedLogMarkers = @($validatedLogMarkers)
        ExpectedIntegrationTests = @($validatedExpectedIntegrationTestSpecs)
        AdditionalModProjects = @($validatedAdditionalModProjects | ForEach-Object {
            [pscustomobject]@{
                PackageId = [string]$_.PackageId
                Project = [string]$_.RelativeProjectPath
            }
        })
        RequireRawClick = [bool]$RequireRawClick
        InteractiveHoldSeconds = [int]$InteractiveHoldSeconds
        ModsConfig = $modsConfigPath
        Prefs = $prefsPath
        RunInBackground = $true
        MusicVolume = 0
        LaunchWindowStyle = $launchWindowStyle
        VisibleWindow = $launchVisible
        VisibleWindowRequested = [bool]$VisibleWindow
        Manifest = $manifestPath
        PlayerLog = $playerLogPath
        Screenshot = $screenshotPath
        FlaUiEvidence = if ($runGatewayRegressionScenario) { $flaUiEvidencePath } else { $null }
        CSharpEndpoint = '/api/v1/executions/csharp'
        Quicktest = [bool]$Quicktest
        Scenario = [string]$scenarioPlan.Name
        ScenarioDescriptor = if ($null -ne $scenarioPlan.Descriptor) { [string]$scenarioPlan.DescriptorPath } else { $null }
        RunsGatewayRegressionScenario = $runGatewayRegressionScenario
        RunIntegrationTests = [bool]$RunIntegrationTests
        IntegrationFailureProbe = [bool]$IntegrationFailureProbe
        QuickstartDescriptor = if ($runGatewayRegressionScenario) { $resolvedQuickstartDescriptorPath } else { $null }
        LaunchArguments = $launchArguments
        NormalConfigHashBefore = $normalConfigHashBefore
        NormalConfigHashAfter = Get-OptionalFileHash -Path $normalModsConfigPath
        NormalPrefsHashBefore = $normalPrefsHashBefore
        NormalPrefsHashAfter = Get-OptionalFileHash -Path $normalPrefsPath
    })
    exit 0
}

if ($runGatewayRegressionScenario -and $null -eq (Get-Command flaui -ErrorAction SilentlyContinue)) {
    Exit-InvalidInput 'FlaUI CLI is not installed or not available on PATH.'
}

$existingRimWorld = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
if ($existingRimWorld.Count -gt 0) {
    Exit-InvalidInput "RimWorld is already running (PID(s): $($existingRimWorld.Id -join ', ')). Close it before gateway verification."
}

$launchedProcess = $null
$flaUiServiceWasRunning = $true
$flaUiServiceStartAttempted = $false
$flaUiConnectionAttempted = $false
$flaUiDesktopEvidence = [pscustomobject]@{
    WindowCount = 0
    Screenshot = $null
}
$clickOutcome = 'not-requested'
$loadedModIds = @()
$failureMessage = $null
$failureRecords = [System.Collections.Generic.List[object]]::new()
$result = $null
$manifest = $null
$launchedProcessStartUtc = $null
$processCleanup = $null
$credentialCleanup = $null
$credentialsSanitized = $false
$gatewayRequestJournalPath = $null
$gatewayLastRequestPath = $null
$integrationTestsStageCleaned = -not $RunIntegrationTests
$integrationTestStageRecords = [System.Collections.Generic.List[object]]::new()
$integrationTestAssemblyRecords = [System.Collections.Generic.List[object]]::new()
$requiredAssemblies = @()
$requiredAssemblyEvidence = @()
$productPackageEvidence = [System.Collections.Generic.List[object]]::new()
$processCleanupStatus = 'not-started'
$credentialCleanupStatus = 'not-started'
$integrationStageCleanupStatus = if ($RunIntegrationTests) { 'not-staged' } else { 'not-requested' }

try {
    foreach ($additionalModProject in $validatedAdditionalModProjects) {
        $productBuildLogPath = Join-Path `
            $runDirectory `
            ("product-build-$($additionalModProject.PackageId).log")
        $productBuildArguments = @(
            'build', [string]$additionalModProject.ProjectPath,
            '--configuration', 'Release',
            '--nologo',
            '-p:DeployToGame=true',
            "-p:RimWorldPath=$resolvedRimWorldPath",
            "-p:RimWorldManagedPath=$managedPath",
            "-p:SteamModContentFolder=$resolvedWorkshopPath"
        )
        $productBuildOutput = @(& dotnet @productBuildArguments 2>&1)
        $productBuildExitCode = $LASTEXITCODE
        $productBuildOutput | Set-Content -LiteralPath $productBuildLogPath -Encoding UTF8
        if ($productBuildExitCode -ne 0) {
            throw "Additional product mod '$($additionalModProject.PackageId)' build/deploy failed with exit $productBuildExitCode. See $productBuildLogPath"
        }

        $productPackageEvidence.Add(
            (Get-GatewaySmokeProductPackageEvidence `
                -Project $additionalModProject `
                -RimWorldPath $resolvedRimWorldPath `
                -BuildLogPath $productBuildLogPath))
    }

    $projectPath = Join-Path $repositoryRoot 'mods\RimWorldDevGateway\RimWorldDevGateway.csproj'
    $buildArguments = @(
        'build', $projectPath,
        '--configuration', 'Release',
        '-p:DeployToGame=true',
        "-p:RimWorldPath=$resolvedRimWorldPath",
        "-p:RimWorldManagedPath=$managedPath",
        "-p:SteamModContentFolder=$resolvedWorkshopPath"
    )
    $buildOutput = @(& dotnet @buildArguments 2>&1)
    $buildExitCode = $LASTEXITCODE
    $buildOutput | Set-Content -LiteralPath $buildLogPath -Encoding UTF8
    if ($buildExitCode -ne 0) {
        throw "Gateway build/deploy failed with exit $buildExitCode. See $buildLogPath"
    }

    if ($RunIntegrationTests) {
        $integrationBundleScript = Join-Path $repositoryRoot 'scripts\Build-InGameIntegrationTests.ps1'
        $integrationBundlePlanInvocationPath = Join-Path $runDirectory 'plan-integration-test-bundle.ps1'
        $integrationBundleInvocationPath = Join-Path $runDirectory 'invoke-integration-test-bundle.ps1'
        $integrationModsRoot = Join-Path $resolvedRimWorldPath 'Mods'
        Write-IntegrationBundleInvocation `
            -Path $integrationBundlePlanInvocationPath `
            -BundleScriptPath $integrationBundleScript `
            -ActivePackageIds $activeModIds `
            -ModsRoot $integrationModsRoot `
            -RimWorldPath $resolvedRimWorldPath `
            -WorkshopPath $resolvedWorkshopPath `
            -DryRun
        $integrationBundlePlanOutput = @(& pwsh `
            -NoProfile `
            -NonInteractive `
            -File $integrationBundlePlanInvocationPath 2>&1)
        $integrationBundlePlanExitCode = $LASTEXITCODE
        $integrationBundlePlanOutput | Set-Content -LiteralPath $integrationBundlePlanLogPath -Encoding UTF8
        if ($integrationBundlePlanExitCode -ne 0) {
            throw "In-game integration-test dry-run plan failed with exit $integrationBundlePlanExitCode. See $integrationBundlePlanLogPath"
        }

        $integrationBundlePlanJsonLines = @($integrationBundlePlanOutput |
            Where-Object { $_.ToString().TrimStart().StartsWith('{', [System.StringComparison]::Ordinal) })
        if ($integrationBundlePlanJsonLines.Count -ne 1) {
            throw "In-game integration-test dry run returned $($integrationBundlePlanJsonLines.Count) JSON results; expected one. See $integrationBundlePlanLogPath"
        }

        $integrationBundlePlanResult = $integrationBundlePlanJsonLines[0].ToString() | ConvertFrom-Json
        Register-IntegrationTestStageCandidates `
            -PlanResult $integrationBundlePlanResult `
            -ActivePackageIds $activeModIds `
            -ModsRoot $integrationModsRoot `
            -Version '1.6' `
            -Records $integrationTestStageRecords

        Write-IntegrationBundleInvocation `
            -Path $integrationBundleInvocationPath `
            -BundleScriptPath $integrationBundleScript `
            -ActivePackageIds $activeModIds `
            -ModsRoot $integrationModsRoot `
            -RimWorldPath $resolvedRimWorldPath `
            -WorkshopPath $resolvedWorkshopPath
        $integrationBundleArguments = @(
            '-NoProfile',
            '-NonInteractive',
            '-File', $integrationBundleInvocationPath
        )
        $integrationBundleOutput = @(& pwsh @integrationBundleArguments 2>&1)
        $integrationBundleExitCode = $LASTEXITCODE
        $integrationBundleOutput | Set-Content -LiteralPath $integrationBundleLogPath -Encoding UTF8
        if ($integrationBundleExitCode -ne 0) {
            throw "In-game integration-test build/stage failed with exit $integrationBundleExitCode. See $integrationBundleLogPath"
        }

        $integrationBundleJsonLines = @($integrationBundleOutput |
            Where-Object { $_.ToString().TrimStart().StartsWith('{', [System.StringComparison]::Ordinal) })
        if ($integrationBundleJsonLines.Count -ne 1) {
            throw "In-game integration-test build/stage returned $($integrationBundleJsonLines.Count) JSON results; expected one. See $integrationBundleLogPath"
        }

        $integrationBundleResult = $integrationBundleJsonLines[0].ToString() | ConvertFrom-Json
        Assert-IntegrationTestBundleMatchesPlan `
            -PlanResult $integrationBundlePlanResult `
            -ActualResult $integrationBundleResult
        $bundleProjects = @($integrationBundleResult.Projects)

        $activePackageSet = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
        foreach ($activePackageId in $activeModIds) {
            $null = $activePackageSet.Add($activePackageId)
        }

        $null = New-Item -Path $integrationBundleEvidencePath -ItemType Directory -Force
        $stageGroups = @($bundleProjects | Group-Object OwnerPackageId | Sort-Object Name)
        foreach ($stageGroup in $stageGroups) {
            $ownerPackageId = [string]$stageGroup.Name
            if (-not $activePackageSet.Contains($ownerPackageId)) {
                throw "Integration-test stage reported an owner outside the active mod set: $ownerPackageId"
            }

            $reportedDestinations = @($stageGroup.Group |
                Select-Object -ExpandProperty Destination -Unique)
            if ($reportedDestinations.Count -ne 1) {
                throw "Integration-test owner '$ownerPackageId' reported multiple stage destinations."
            }

            $stagePath = Assert-OwnedIntegrationTestStage `
                -StagePath ([string]$reportedDestinations[0]) `
                -ModsRoot $integrationModsRoot `
                -OwnerPackageId $ownerPackageId `
                -Version '1.6'

            $expectedFileNames = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::Ordinal)
            $null = $expectedFileNames.Add('.rimworld-integration-test-stage.owner')
            foreach ($bundleProject in $stageGroup.Group) {
                $assemblyName = [string]$bundleProject.Assembly
                $manifestName = [string]$bundleProject.Manifest
                if (-not $assemblyName.EndsWith('.IntegrationTests.dll', [System.StringComparison]::Ordinal) -or
                    -not $manifestName.EndsWith('.IntegrationTests.integrationtests.json', [System.StringComparison]::Ordinal) -or
                    -not $expectedFileNames.Add($assemblyName) -or
                    -not $expectedFileNames.Add($manifestName)) {
                    throw "Integration-test owner '$ownerPackageId' reported an invalid or duplicate bundle pair."
                }
            }

            $stagedEntries = @(Get-ChildItem -LiteralPath $stagePath -Force)
            $stagedFiles = @($stagedEntries | Where-Object { -not $_.PSIsContainer })
            if ($stagedEntries.Count -ne $expectedFileNames.Count -or
                $stagedFiles.Count -ne $expectedFileNames.Count -or
                @($stagedFiles | Where-Object { -not $expectedFileNames.Contains($_.Name) }).Count -ne 0) {
                throw "Integration-test owner '$ownerPackageId' stage did not contain exactly its reported bundle pairs and ownership marker: $stagePath"
            }

            foreach ($bundleProject in $stageGroup.Group) {
                $assemblyRecord = Get-StagedIntegrationTestAssemblyRecord `
                    -OwnerPackageId $ownerPackageId `
                    -AssemblyPath (Join-Path $stagePath ([string]$bundleProject.Assembly)) `
                    -ManifestPath (Join-Path $stagePath ([string]$bundleProject.Manifest))
                $integrationTestAssemblyRecords.Add($assemblyRecord)
            }

            $ownerEvidencePath = Join-Path $integrationBundleEvidencePath "$ownerPackageId\1.6\DevIntegrationTests"
            $null = New-Item -Path $ownerEvidencePath -ItemType Directory -Force
            foreach ($bundleFile in $stagedFiles) {
                Copy-Item -LiteralPath $bundleFile.FullName -Destination $ownerEvidencePath -Force
            }
        }
        if ($integrationTestAssemblyRecords.Count -ne $bundleProjects.Count) {
            throw 'Integration-test staging did not capture one exact full assembly identity for every staged DLL.'
        }
        @($integrationTestAssemblyRecords) |
            ConvertTo-Json -Depth 5 |
            Set-Content `
                -LiteralPath (Join-Path $integrationBundleEvidencePath 'assembly-identities.json') `
                -Encoding UTF8
    }

    $deployedAssemblies = Join-Path $resolvedRimWorldPath 'Mods\fumblesneeze.rimworlddevgateway\1.6\Assemblies'
    $requiredAssemblies = @(
        'RimWorldDevGateway.dll',
        'RimWorldDevGateway.Contracts.dll',
        'RimWorldDevGateway.IntegrationTesting.dll',
        'EmbedIO.dll',
        'Mono.CSharp.dll',
        'Swan.Lite.dll',
        'System.ValueTuple.dll'
    )
    $requiredAssemblyEvidence = @(Get-GatewaySmokeAssemblyEvidence `
        -AssembliesPath $deployedAssemblies `
        -RequiredAssemblies $requiredAssemblies)
    if ($requiredAssemblyEvidence.Count -ne $requiredAssemblies.Count) {
        throw 'Deployed Gateway assembly evidence is incomplete.'
    }

    $launchedProcess = Start-GatewayRimWorldProcess `
        -ExecutablePath $rimWorldExecutable `
        -LaunchArguments $launchArguments `
        -WindowStyle $launchWindowStyle
    $launchedProcessStartUtc = [datetimeoffset]::new(
        $launchedProcess.StartTime.ToUniversalTime(),
        [timespan]::Zero)
    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([datetime]::UtcNow -lt $deadline) {
        $launchedProcess.Refresh()
        if ($launchedProcess.HasExited) {
            throw "RimWorld exited before gateway readiness with code $($launchedProcess.ExitCode). See $playerLogPath"
        }

        if ((Test-Path -LiteralPath $manifestPath -PathType Leaf) -and $launchedProcess.MainWindowHandle -ne 0) {
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Timed out after $TimeoutSeconds seconds waiting for $manifestPath. See $playerLogPath"
    }

    $manifest = Read-ValidatedGatewayCurrentManifest `
        -Path $manifestPath `
        -SavedDataPath $savedDataPath `
        -ExpectedProcessId $launchedProcess.Id `
        -ExpectedProcessStartUtc $launchedProcessStartUtc

    $baseUrl = [string]$manifest.baseUrl
    $unauthorized = Invoke-TrackedGatewayRequest `
        -Method 'GET' `
        -Uri "$baseUrl/status" `
        -RequestId 'gateway-smoke-unauthorized' `
        -Operation {
            Invoke-WebRequest -Uri "$baseUrl/status" -Method Get -TimeoutSec 20 -SkipHttpErrorCheck
        }
    if ([int]$unauthorized.StatusCode -ne 401) {
        throw "Unauthenticated status returned HTTP $([int]$unauthorized.StatusCode), expected 401."
    }

    $statusResponse = Invoke-GatewayGet -Uri "$baseUrl/status" -Token $manifest.token -RequestId 'gateway-smoke-status'
    $statusResponse.Content | Set-Content -LiteralPath $statusPath -Encoding UTF8
    $status = $statusResponse.Content | ConvertFrom-Json
    if ([int]$statusResponse.StatusCode -ne 200 -or -not $status.ok) {
        throw "Authenticated status failed with HTTP $([int]$statusResponse.StatusCode). See $statusPath"
    }

    if (-not $status.result.developerOnly -or -not $status.result.unrestrictedExecutionEnabled) {
        throw 'Status did not report developerOnly and unrestrictedExecutionEnabled.'
    }

    if ($Quicktest) {
        $playableDeadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
        while ([datetime]::UtcNow -lt $playableDeadline) {
            $playableStatusResponse = Invoke-GatewayGet `
                -Uri "$baseUrl/status" `
                -Token $manifest.token `
                -RequestId 'gateway-smoke-wait-playing-before-def-export'
            if ([int]$playableStatusResponse.StatusCode -eq 200) {
                $playableStatus = $playableStatusResponse.Content | ConvertFrom-Json
                if ($playableStatus.ok -and
                    [string]$playableStatus.result.programState -eq 'Playing' -and
                    $null -ne $playableStatus.result.map) {
                    $status = $playableStatus
                    $playableStatusResponse.Content | Set-Content -LiteralPath $statusPath -Encoding UTF8
                    break
                }
            }

            Start-Sleep -Seconds 1
        }

        if ([string]$status.result.programState -ne 'Playing' -or $null -eq $status.result.map) {
            throw "Timed out after $TimeoutSeconds seconds waiting for a playable quicktest map before main-thread verification. See $playerLogPath"
        }
    }

    $defExportResponse = Invoke-GatewayJsonPost `
        -Uri "$baseUrl/defs/export" `
        -Token $manifest.token `
        -RequestId 'gateway-smoke-def-export' `
        -Body (New-GatewayDefSmokeRequest)
    $defExportEnvelope = Assert-GatewayDefExportSmokeResult `
        -Response $defExportResponse `
        -ArtifactPath $defExportPath

    $integrationTestsEnvelope = $null
    $integrationTestsPersistedPath = $null
    if ($RunIntegrationTests) {
        $targetIntegrationLifecycle = if ($Quicktest) { 'PlayableMapLoaded' } else { 'MainMenuLoaded' }
        $expectedIntegrationLifecycleState = if ($IntegrationFailureProbe) { 'failed' } else { 'completed' }
        $expectedIntegrationFailureTestName =
            'RimWorldDevGateway.InGame.IntegrationTests.PlayableMapIntegrationTests.A_DeliberateFailureProbeIsStartupControlled'
        $integrationDeadline = [datetime]::UtcNow.AddSeconds([Math]::Min($TimeoutSeconds, 180))
        $integrationAttempt = 0
        $integrationReadyObserved = $false
        $targetLifecycle = @()
        do {
            $integrationAttempt++
            $integrationTestsResponse = Invoke-GatewayGet `
                -Uri "$baseUrl/integration-tests" `
                -Token $manifest.token `
                -RequestId "gateway-smoke-integration-tests-$integrationAttempt"
            $integrationPollResponse = Resolve-IntegrationTestPollResponse `
                -Response $integrationTestsResponse `
                -ReadyArtifactPath $integrationTestsPath `
                -PendingArtifactPath $integrationTestsPendingPath `
                -Operation 'In-game integration-test status' `
                -ReadyAlreadyObserved:$integrationReadyObserved
            if ([string]$integrationPollResponse.State -ceq 'pending') {
                $launchedProcess.Refresh()
                if ($launchedProcess.HasExited) {
                    throw "RimWorld exited while initial in-game integration-test status was pending. See $playerLogPath"
                }

                Start-Sleep -Milliseconds 250
                continue
            }

            $integrationReadyObserved = $true
            $integrationTestsEnvelope = $integrationPollResponse.Envelope
            $targetLifecycle = @($integrationTestsEnvelope.result.LifecyclePoints |
                Where-Object { [string]$_.RunAt -eq $targetIntegrationLifecycle } |
                Select-Object -First 1)
            if ([string]$integrationTestsEnvelope.result.DiscoveryState -eq 'failed' -or
                @($integrationTestsEnvelope.result.Failures).Count -ne 0 -or
                [int]$integrationTestsEnvelope.result.OmittedFailureCount -ne 0 -or
                [int]$integrationTestsEnvelope.result.OmittedResultCount -ne 0 -or
                ($targetLifecycle.Count -eq 1 -and
                    [string]$targetLifecycle[0].State -eq 'failed' -and
                    -not $IntegrationFailureProbe)) {
                throw "In-game integration tests failed. See $integrationTestsPath"
            }

            if ($targetLifecycle.Count -eq 1 -and
                [string]$targetLifecycle[0].State -eq $expectedIntegrationLifecycleState) {
                break
            }

            $launchedProcess.Refresh()
            if ($launchedProcess.HasExited) {
                throw "RimWorld exited while waiting for in-game integration tests. See $playerLogPath"
            }

            Start-Sleep -Milliseconds 250
        }
        while ([datetime]::UtcNow -lt $integrationDeadline)

        if ($null -eq $integrationTestsEnvelope -or $targetLifecycle.Count -ne 1) {
            throw "In-game integration tests did not reach the expected $targetIntegrationLifecycle '$expectedIntegrationLifecycleState' state. See $integrationTestsPath and $integrationTestsPendingPath"
        }
        $null = Assert-IntegrationTestTerminalSnapshot `
            -Snapshot $integrationTestsEnvelope.result `
            -TargetRunAt $targetIntegrationLifecycle `
            -FailureProbe:$IntegrationFailureProbe `
            -ExpectedFailureTestName $expectedIntegrationFailureTestName `
            -MinimumDiscoveredTestCount 4 `
            -ExpectedTests @($validatedExpectedIntegrationTests)
        Assert-IntegrationTestAssemblyProvenance `
            -Snapshot $integrationTestsEnvelope.result `
            -ExpectedAssemblies @($integrationTestAssemblyRecords)

        $terminalIntegrationSnapshot = $integrationTestsEnvelope.result
        Start-Sleep -Milliseconds 750
        $repeatIntegrationResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/integration-tests" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-integration-tests-once-check'
        $repeatIntegrationEnvelope = Assert-GatewaySuccess `
            -Response $repeatIntegrationResponse `
            -ArtifactPath $integrationTestsPath `
            -Operation 'In-game integration-test once-per-process check'
        $null = Assert-IntegrationTestTerminalSnapshot `
            -Snapshot $repeatIntegrationEnvelope.result `
            -TargetRunAt $targetIntegrationLifecycle `
            -FailureProbe:$IntegrationFailureProbe `
            -ExpectedFailureTestName $expectedIntegrationFailureTestName `
            -MinimumDiscoveredTestCount 4 `
            -ExpectedTests @($validatedExpectedIntegrationTests)
        Assert-IntegrationTestAssemblyProvenance `
            -Snapshot $repeatIntegrationEnvelope.result `
            -ExpectedAssemblies @($integrationTestAssemblyRecords)
        Assert-MatchingIntegrationTestSnapshots `
            -Expected $terminalIntegrationSnapshot `
            -Actual $repeatIntegrationEnvelope.result
        $integrationTestsEnvelope = $repeatIntegrationEnvelope

        $integrationTestsPersistedPath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\integration-tests.json"
        if ((Get-Content -LiteralPath $integrationTestsPersistedPath -Raw).Contains([string]$manifest.token)) {
            throw 'The bearer token leaked into the persisted integration-test artifact.'
        }
        $persistedIntegrationSnapshot = Read-IntegrationTestSnapshotFile `
            -Path $integrationTestsPersistedPath
        $null = Assert-IntegrationTestTerminalSnapshot `
            -Snapshot $persistedIntegrationSnapshot `
            -TargetRunAt $targetIntegrationLifecycle `
            -FailureProbe:$IntegrationFailureProbe `
            -ExpectedFailureTestName $expectedIntegrationFailureTestName `
            -MinimumDiscoveredTestCount 4 `
            -ExpectedTests @($validatedExpectedIntegrationTests)
        Assert-IntegrationTestAssemblyProvenance `
            -Snapshot $persistedIntegrationSnapshot `
            -ExpectedAssemblies @($integrationTestAssemblyRecords)
        Assert-MatchingIntegrationTestSnapshots `
            -Expected $repeatIntegrationEnvelope.result `
            -Actual $persistedIntegrationSnapshot
    }
    else {
        $integrationTestsResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/integration-tests" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-integration-tests-disabled'
        $integrationTestsEnvelope = Assert-GatewaySuccess `
            -Response $integrationTestsResponse `
            -ArtifactPath $integrationTestsPath `
            -Operation 'Disabled in-game integration-test status'
        if ([bool]$integrationTestsEnvelope.result.Enabled -or
            [string]$integrationTestsEnvelope.result.DiscoveryState -ne 'disabled') {
            throw "Integration-test discovery was not disabled without its startup flag. See $integrationTestsPath"
        }
    }

    $executionUri = "$baseUrl/executions/csharp"
    $stateResponse = Invoke-GatewayTextPost `
        -Uri $executionUri `
        -Token $manifest.token `
        -RequestId 'gateway-smoke-execution-state' `
        -Source 'string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}|{5}|{6}", UnityEngine.Time.frameCount, Thread.CurrentThread.ManagedThreadId, Current.ProgramState, typeof(RimWorldDevGateway.GatewayDispatcher).FullName, typeof(EmbedIO.WebServer).FullName, typeof(Mono.CSharp.Evaluator).FullName, string.Join(",", LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId)))'
    $stateResponse.Content | Set-Content -LiteralPath $executionStatePath -Encoding UTF8
    $stateEnvelope = $stateResponse.Content | ConvertFrom-Json
    if ([int]$stateResponse.StatusCode -ne 200 -or
        -not $stateEnvelope.ok -or
        -not $stateEnvelope.result.Succeeded -or
        [string]$stateEnvelope.result.Type -ne 'System.String') {
        throw "Raw C# health probe failed with HTTP $([int]$stateResponse.StatusCode). See $executionStatePath"
    }

    $probeParts = ([string]$stateEnvelope.result.Value).Split('|')
    if ($probeParts.Count -ne 7 -or
        [int]$probeParts[0] -lt 0 -or
        [int]$probeParts[1] -le 0 -or
        [string]::IsNullOrWhiteSpace([string]$probeParts[2]) -or
        [string]$probeParts[3] -ne 'RimWorldDevGateway.GatewayDispatcher' -or
        [string]$probeParts[4] -ne 'EmbedIO.WebServer' -or
        [string]$probeParts[5] -ne 'Mono.CSharp.Evaluator') {
        throw "Raw C# health probe did not prove live Unity, Gateway, EmbedIO, and Mono.CSharp access. See $executionStatePath"
    }

    $loadedModIds = @(([string]$probeParts[6]).Split(',', [System.StringSplitOptions]::RemoveEmptyEntries))
    if ($loadedModIds.Count -ne $activeModIds.Count) {
        throw "RimWorld loaded $($loadedModIds.Count) mods instead of the $($activeModIds.Count) requested isolated mods. Loaded: $($loadedModIds -join ', ')"
    }
    for ($index = 0; $index -lt $activeModIds.Count; $index++) {
        if (-not [string]::Equals(
                [string]$loadedModIds[$index],
                [string]$activeModIds[$index],
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "RimWorld did not load the requested ordered mod list. Requested: $($activeModIds -join ', '); loaded: $($loadedModIds -join ', ')"
        }
    }
    Assert-GatewayScenarioRequiredPackages `
        -ScenarioPlan $scenarioPlan `
        -PackageIds $loadedModIds `
        -PackageState 'loaded'

    if ($runGatewayRegressionScenario) {
        $declarationResponse = Invoke-GatewayTextPost `
            -Uri $executionUri `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-execution-declare' `
            -Source 'var gatewaySmokeOriginalDevMode = Prefs.DevMode; Prefs.DevMode = !gatewaySmokeOriginalDevMode; Prefs.DevMode != gatewaySmokeOriginalDevMode'
        $declarationResponse.Content | Set-Content -LiteralPath $executionPath -Encoding UTF8
        $declarationEnvelope = $declarationResponse.Content | ConvertFrom-Json
        if ([int]$declarationResponse.StatusCode -ne 200 -or
            -not $declarationEnvelope.ok -or
            -not $declarationEnvelope.result.Succeeded -or
            -not [bool]$declarationEnvelope.result.Value) {
            throw "Raw C# declaration/mutation failed with HTTP $([int]$declarationResponse.StatusCode). See $executionPath"
        }

        $restoreResponse = Invoke-GatewayTextPost `
            -Uri $executionUri `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-execution-restore' `
            -Source '(new System.Func<bool>(() => { var changed = Prefs.DevMode != gatewaySmokeOriginalDevMode; Prefs.DevMode = gatewaySmokeOriginalDevMode; return changed && Prefs.DevMode == gatewaySmokeOriginalDevMode; }))()'
        $restoreResponse.Content | Set-Content -LiteralPath $executionRestorePath -Encoding UTF8
        $restoreEnvelope = $restoreResponse.Content | ConvertFrom-Json
        if ([int]$restoreResponse.StatusCode -ne 200 -or
            -not $restoreEnvelope.ok -or
            -not $restoreEnvelope.result.Succeeded -or
            -not [bool]$restoreEnvelope.result.Value) {
            throw "Raw C# persistent-state restoration failed with HTTP $([int]$restoreResponse.StatusCode). See $executionRestorePath"
        }
    }

    $uiResponse = Invoke-GatewayGet -Uri "$baseUrl/ui-state" -Token $manifest.token -RequestId 'gateway-smoke-ui'
    $uiResponse.Content | Set-Content -LiteralPath $uiStatePath -Encoding UTF8
    if ([int]$uiResponse.StatusCode -ne 200) {
        throw "UI-state request failed with HTTP $([int]$uiResponse.StatusCode)."
    }

    $logResponse = Invoke-GatewayGet -Uri "$baseUrl/logs?after=0&limit=100" -Token $manifest.token -RequestId 'gateway-smoke-logs'
    $logResponse.Content | Set-Content -LiteralPath $logsPath -Encoding UTF8
    if ([int]$logResponse.StatusCode -ne 200) {
        throw "Log request failed with HTTP $([int]$logResponse.StatusCode)."
    }
    $logEnvelope = $logResponse.Content | ConvertFrom-Json
    $quickstartLogCursor = if ($logEnvelope.ok) { [long]$logEnvelope.result.NewestCursor } else { 0L }

    $actionsResponse = Invoke-GatewayGet -Uri "$baseUrl/actions" -Token $manifest.token -RequestId 'gateway-smoke-actions'
    $actionsResponse.Content | Set-Content -LiteralPath $actionsPath -Encoding UTF8
    if ([int]$actionsResponse.StatusCode -ne 200) {
        throw "Semantic-action discovery failed with HTTP $([int]$actionsResponse.StatusCode)."
    }

    $automationsResponse = Invoke-GatewayGet -Uri "$baseUrl/automations" -Token $manifest.token -RequestId 'gateway-smoke-automations'
    $automationsResponse.Content | Set-Content -LiteralPath $automationsPath -Encoding UTF8
    if ([int]$automationsResponse.StatusCode -ne 200) {
        throw "Automation discovery failed with HTTP $([int]$automationsResponse.StatusCode)."
    }

    $quickstartRun = $null
    if ($Quicktest) {
        $playableDeadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
        while ([datetime]::UtcNow -lt $playableDeadline) {
            $playableStatusResponse = Invoke-GatewayGet `
                -Uri "$baseUrl/status" `
                -Token $manifest.token `
                -RequestId 'gateway-smoke-wait-playing'
            if ([int]$playableStatusResponse.StatusCode -eq 200) {
                $playableStatus = $playableStatusResponse.Content | ConvertFrom-Json
                if ($playableStatus.ok -and
                    [string]$playableStatus.result.programState -eq 'Playing' -and
                    $null -ne $playableStatus.result.map) {
                    $status = $playableStatus
                    $playableStatusResponse.Content | Set-Content -LiteralPath $statusPath -Encoding UTF8
                    break
                }
            }

            Start-Sleep -Seconds 1
        }

        if ([string]$status.result.programState -ne 'Playing' -or $null -eq $status.result.map) {
            throw "Timed out after $TimeoutSeconds seconds waiting for a playable quicktest map. See $playerLogPath"
        }

        if ($runGatewayRegressionScenario) {
        $gameStateInitialResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-game-state-initial'
        $gameStateInitialEnvelope = Assert-GatewaySuccess `
            -Response $gameStateInitialResponse `
            -ArtifactPath $gameStateInitialPath `
            -Operation 'Initial game-state capture'
        $initialGameState = $gameStateInitialEnvelope.result
        foreach ($propertyName in @('DevMode', 'GodMode', 'Paused', 'Speed')) {
            if ($initialGameState.PSObject.Properties.Name -notcontains $propertyName) {
                throw "Initial game-state capture omitted $propertyName. See $gameStateInitialPath"
            }
        }

        $gameStateEnableResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-game-state-enable-dev' `
            -Body ([ordered]@{ devMode = $true; godMode = $true })
        $gameStateEnableEnvelope = Assert-GatewaySuccess `
            -Response $gameStateEnableResponse `
            -ArtifactPath $gameStateEnableDevPath `
            -Operation 'Developer/god-mode enable'
        if (-not $gameStateEnableEnvelope.result.After.DevMode -or
            -not $gameStateEnableEnvelope.result.After.GodMode -or
            -not $gameStateEnableEnvelope.result.After.EffectiveGodMode) {
            throw "Game-state mutation did not enable developer and effective god mode. See $gameStateEnableDevPath"
        }

        $gamePauseRequestIds = [System.Collections.Generic.List[string]]::new()
        $gameStatePauseResponse = $null
        for ($pauseAttempt = 1; $pauseAttempt -le 10; $pauseAttempt++) {
            $pauseRequestId = if ($pauseAttempt -eq 1) {
                'gateway-smoke-game-state-pause'
            }
            else {
                "gateway-smoke-game-state-pause-retry-$pauseAttempt"
            }
            $gamePauseRequestIds.Add($pauseRequestId)
            $gameStatePauseResponse = Invoke-GatewayJsonPost `
                -Uri "$baseUrl/game-state" `
                -Token $manifest.token `
                -RequestId $pauseRequestId `
                -Body ([ordered]@{ paused = $true })
            if ([int]$gameStatePauseResponse.StatusCode -ge 200 -and
                [int]$gameStatePauseResponse.StatusCode -lt 300) {
                break
            }

            $pauseAttemptPath = Join-Path $runDirectory "game-state-pause-attempt-$pauseAttempt.json"
            $gameStatePauseResponse.Content | Set-Content -LiteralPath $pauseAttemptPath -Encoding UTF8
            $pauseAttemptEnvelope = $gameStatePauseResponse.Content | ConvertFrom-Json -ErrorAction Stop
            if ([string]$pauseAttemptEnvelope.error.code -ne 'game_state_rejected' -or
                $pauseAttempt -eq 10) {
                break
            }

            Start-Sleep -Milliseconds 500
        }
        $gameStatePauseEnvelope = Assert-GatewaySuccess `
            -Response $gameStatePauseResponse `
            -ArtifactPath $gameStatePausePath `
            -Operation 'Game pause'
        if (-not $gameStatePauseEnvelope.result.After.Paused -or
            [string]$gameStatePauseEnvelope.result.After.Speed -ne 'Paused') {
            throw "Game-state mutation did not pause the game. See $gameStatePausePath"
        }

        $gameStateSpeedResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-game-state-speed' `
            -Body ([ordered]@{ speed = 'Normal' })
        $gameStateSpeedEnvelope = Assert-GatewaySuccess `
            -Response $gameStateSpeedResponse `
            -ArtifactPath $gameStateSpeedPath `
            -Operation 'Game speed control'
        if ($gameStateSpeedEnvelope.result.After.Paused -or
            [string]$gameStateSpeedEnvelope.result.After.Speed -ne 'Normal') {
            throw "Game-state mutation did not unpause at normal speed. See $gameStateSpeedPath"
        }

        $descriptorText = Get-Content -LiteralPath $resolvedQuickstartDescriptorPath -Raw
        $null = $descriptorText | ConvertFrom-Json -ErrorAction Stop
        $quickstartBody = '{"arguments":' + $descriptorText.Trim() + ',"idempotencyKey":"gateway-smoke-quickstart"}'
        $quickstartHeaders = @{
            Authorization = "Bearer $($manifest.token)"
            'X-Request-Id' = 'gateway-smoke-quickstart'
        }
        $quickstartResponse = Invoke-TrackedGatewayRequest `
            -Method 'POST' `
            -Uri "$baseUrl/automations/quickstart.spawn/runs" `
            -RequestId 'gateway-smoke-quickstart' `
            -Operation {
                Invoke-WebRequest `
                    -Uri "$baseUrl/automations/quickstart.spawn/runs" `
                    -Method Post `
                    -Headers $quickstartHeaders `
                    -ContentType 'application/json; charset=utf-8' `
                    -Body ([System.Text.Encoding]::UTF8.GetBytes($quickstartBody)) `
                    -TimeoutSec 60 `
                    -SkipHttpErrorCheck
            }
        $quickstartResponse.Content | Set-Content -LiteralPath $quickstartPath -Encoding UTF8
        $quickstartEnvelope = $quickstartResponse.Content | ConvertFrom-Json
        $quickstartRun = $quickstartEnvelope.result
        if ([int]$quickstartResponse.StatusCode -ne 200 -or
            -not $quickstartEnvelope.ok -or
            [string]$quickstartRun.State -ne 'succeeded' -or
            [string]::IsNullOrWhiteSpace([string]$quickstartRun.RunId) -or
            @($quickstartRun.Result.Spawned).Count -lt 1 -or
            @($quickstartRun.Result.Mutations).Count -lt 1) {
            throw "quickstart.spawn did not return a completed scene with handles and mutations. See $quickstartPath"
        }

        foreach ($spawned in @($quickstartRun.Result.Spawned)) {
            if ([string]::IsNullOrWhiteSpace([string]$spawned.Handle) -or $null -eq $spawned.Position) {
                throw "quickstart.spawn returned a spawned object without a stable handle or position. See $quickstartPath"
            }
        }

        $quickstartHeaders['X-Request-Id'] = 'gateway-smoke-quickstart-replay'
        $quickstartReplayResponse = Invoke-TrackedGatewayRequest `
            -Method 'POST' `
            -Uri "$baseUrl/automations/quickstart.spawn/runs" `
            -RequestId 'gateway-smoke-quickstart-replay' `
            -Operation {
                Invoke-WebRequest `
                    -Uri "$baseUrl/automations/quickstart.spawn/runs" `
                    -Method Post `
                    -Headers $quickstartHeaders `
                    -ContentType 'application/json; charset=utf-8' `
                    -Body ([System.Text.Encoding]::UTF8.GetBytes($quickstartBody)) `
                    -TimeoutSec 60 `
                    -SkipHttpErrorCheck
            }
        $quickstartReplayResponse.Content | Set-Content -LiteralPath $quickstartReplayPath -Encoding UTF8
        $quickstartReplayEnvelope = $quickstartReplayResponse.Content | ConvertFrom-Json
        if ([int]$quickstartReplayResponse.StatusCode -ne 200 -or
            -not $quickstartReplayEnvelope.ok -or
            [string]$quickstartReplayEnvelope.result.RunId -ne [string]$quickstartRun.RunId -or
            @($quickstartReplayEnvelope.result.Result.Spawned).Count -ne @($quickstartRun.Result.Spawned).Count) {
            throw "quickstart.spawn idempotent replay did not return the original run without duplicates. See $quickstartReplayPath"
        }

        $quickstartObjects = @($quickstartRun.Result.Spawned)
        $quickstartDefNames = @($quickstartObjects |
            ForEach-Object { [string]$_.DefName } |
            Sort-Object -Unique)
        $thingMapResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/things/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-things-map' `
            -Body ([ordered]@{
                scope = 'map'
                defNames = $quickstartDefNames
                limit = 1000
            })
        $thingMapEnvelope = Assert-GatewaySuccess `
            -Response $thingMapResponse `
            -ArtifactPath $thingsMapPath `
            -Operation 'Filtered map thing query'
        $mapThings = @($thingMapEnvelope.result.Things)
        $quickstartResolvedThings = foreach ($spawned in $quickstartObjects) {
            $matches = @($mapThings | Where-Object {
                [string]$_.LoadId -eq [string]$spawned.Handle
            })
            if ($matches.Count -ne 1) {
                throw "Filtered map thing query did not uniquely resolve quickstart LoadId '$($spawned.Handle)'. See $thingsMapPath"
            }

            $matches[0]
        }
        $quickstartHandles = @($quickstartResolvedThings | ForEach-Object { [string]$_.Handle })
        foreach ($thing in $mapThings) {
            if ($quickstartDefNames -notcontains [string]$thing.DefName) {
                throw "Filtered map thing query returned unexpected Def '$($thing.DefName)'. See $thingsMapPath"
            }
        }

        $quickstartBuilding = $quickstartResolvedThings |
            Where-Object { [string]$_.Kind -eq 'building' } |
            Select-Object -First 1
        $quickstartPawn = $quickstartResolvedThings |
            Where-Object { [string]$_.Kind -eq 'pawn' } |
            Select-Object -First 1
        if ($null -eq $quickstartBuilding -or $null -eq $quickstartPawn) {
            throw "Quickstart scene did not expose both a building and pawn for inspection. See $quickstartPath"
        }

        $buildingInspectResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/things/$([Uri]::EscapeDataString([string]$quickstartBuilding.Handle))" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-thing-inspect-building'
        $buildingInspectEnvelope = Assert-GatewaySuccess `
            -Response $buildingInspectResponse `
            -ArtifactPath $thingInspectBuildingPath `
            -Operation 'Building inspection'
        if ([string]$buildingInspectEnvelope.result.Summary.Handle -ne [string]$quickstartBuilding.Handle -or
            $null -eq $buildingInspectEnvelope.result.Building) {
            throw "Building inspection omitted its exact summary or building details. See $thingInspectBuildingPath"
        }

        $pawnInspectResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/things/$([Uri]::EscapeDataString([string]$quickstartPawn.Handle))" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-thing-inspect-pawn'
        $pawnInspectEnvelope = Assert-GatewaySuccess `
            -Response $pawnInspectResponse `
            -ArtifactPath $thingInspectPawnPath `
            -Operation 'Pawn inspection'
        if ([string]$pawnInspectEnvelope.result.Summary.Handle -ne [string]$quickstartPawn.Handle -or
            $null -eq $pawnInspectEnvelope.result.Pawn -or
            [string]::IsNullOrWhiteSpace([string]$pawnInspectEnvelope.result.Pawn.KindDefName)) {
            throw "Pawn inspection omitted its exact summary or bounded pawn details. See $thingInspectPawnPath"
        }

        $selectionHandles = @(
            [string]$quickstartBuilding.Handle,
            [string]$quickstartPawn.Handle
        )
        $selectionSetResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/selection" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-selection-set' `
            -Body ([ordered]@{ operation = 'replace'; handles = $selectionHandles })
        $selectionSetEnvelope = Assert-GatewaySuccess `
            -Response $selectionSetResponse `
            -ArtifactPath $selectionSetPath `
            -Operation 'Semantic selection'
        $initialSelectionHandles = @($selectionSetEnvelope.result.Before |
            ForEach-Object { [string]$_.Handle })
        $selectedAfterHandles = @($selectionSetEnvelope.result.After |
            ForEach-Object { [string]$_.Handle })
        if (($selectedAfterHandles -join '|') -ne ($selectionHandles -join '|')) {
            throw "Semantic selection did not preserve the two requested handles in order. See $selectionSetPath"
        }

        $selectionQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/things/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-selection-query' `
            -Body ([ordered]@{
                scope = 'map'
                selected = $true
                defNames = $quickstartDefNames
                limit = 100
            })
        $selectionQueryEnvelope = Assert-GatewaySuccess `
            -Response $selectionQueryResponse `
            -ArtifactPath $selectionQueryPath `
            -Operation 'Selected-thing filter query'
        $selectedQueryHandles = @($selectionQueryEnvelope.result.Things |
            ForEach-Object { [string]$_.Handle })
        $selectedQueryKey = (@($selectedQueryHandles | Sort-Object) -join '|')
        $selectionKey = (@($selectionHandles | Sort-Object) -join '|')
        if ($selectedQueryKey -ne $selectionKey) {
            throw "Selected-thing filter did not return exactly the semantic selection. See $selectionQueryPath"
        }

        $directSpawnArguments = [ordered]@{
            version = 1
            center = [ordered]@{
                x = [int]$quickstartRun.Result.Center.X
                z = [int]$quickstartRun.Result.Center.Z
            }
            buildings = @(
                [ordered]@{
                    defName = 'DiningChair'
                    stuff = 'Steel'
                    count = 1
                    quality = 'Normal'
                    offset = [ordered]@{ x = 6; z = 6 }
                }
            )
            pawns = @(
                [ordered]@{
                    kindDefName = 'Colonist'
                    count = 1
                    offset = [ordered]@{ x = -6; z = 6 }
                }
            )
        }
        $directSpawnRequest = [ordered]@{
            arguments = $directSpawnArguments
            idempotencyKey = 'gateway-smoke-direct-spawn'
        }
        $directSpawnResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/dev-tools/spawn" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-direct-spawn' `
            -Body $directSpawnRequest `
            -TimeoutSec 60
        $directSpawnEnvelope = Assert-GatewaySuccess `
            -Response $directSpawnResponse `
            -ArtifactPath $directSpawnPath `
            -Operation 'Direct developer spawn'
        $directSpawnRun = $directSpawnEnvelope.result
        $directSpawned = @($directSpawnRun.Result.Spawned)
        if ([string]$directSpawnRun.State -ne 'succeeded' -or
            [string]::IsNullOrWhiteSpace([string]$directSpawnRun.RunId) -or
            $directSpawned.Count -ne 2 -or
            @($directSpawned | Where-Object { [string]$_.Category -eq 'building' }).Count -ne 1 -or
            @($directSpawned | Where-Object { [string]$_.Category -eq 'pawn' }).Count -ne 1) {
            throw "Direct developer spawn did not create exactly one building and one pawn. See $directSpawnPath"
        }

        $directSpawnReplayResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/dev-tools/spawn" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-direct-spawn-replay' `
            -Body $directSpawnRequest `
            -TimeoutSec 60
        $directSpawnReplayEnvelope = Assert-GatewaySuccess `
            -Response $directSpawnReplayResponse `
            -ArtifactPath $directSpawnReplayPath `
            -Operation 'Direct developer spawn replay'
        $directSpawnLoadIds = @($directSpawned | ForEach-Object { [string]$_.Handle })
        $directReplayLoadIds = @($directSpawnReplayEnvelope.result.Result.Spawned |
            ForEach-Object { [string]$_.Handle })
        if ([string]$directSpawnReplayEnvelope.result.RunId -ne [string]$directSpawnRun.RunId -or
            ($directReplayLoadIds -join '|') -ne ($directSpawnLoadIds -join '|')) {
            throw "Direct developer spawn replay was not idempotent. See $directSpawnReplayPath"
        }

        $directSpawnQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/things/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-direct-spawn-query' `
            -Body ([ordered]@{
                scope = 'map'
                defNames = @($directSpawned | ForEach-Object { [string]$_.DefName } | Sort-Object -Unique)
                limit = 1000
            })
        $directSpawnQueryEnvelope = Assert-GatewaySuccess `
            -Response $directSpawnQueryResponse `
            -ArtifactPath $directSpawnQueryPath `
            -Operation 'Direct developer spawn query'
        $directSpawnQueryThings = @($directSpawnQueryEnvelope.result.Things)
        $directSpawnResolvedThings = foreach ($spawned in $directSpawned) {
            $matches = @($directSpawnQueryThings | Where-Object {
                [string]$_.LoadId -eq [string]$spawned.Handle
            })
            if ($matches.Count -ne 1) {
                throw "Direct spawn query did not uniquely resolve spawned LoadId '$($spawned.Handle)'. See $directSpawnQueryPath"
            }

            $matches[0]
        }
        $directSpawnHandles = @($directSpawnResolvedThings | ForEach-Object { [string]$_.Handle })

        $directSpawnHandleLiterals = @($directSpawnHandles | ForEach-Object {
            '"' + $_.Replace('\', '\\').Replace('"', '\"') + '"'
        })
        $directCleanupSource =
            'var gatewaySmokeDirectHandles = new System.Collections.Generic.HashSet<string>(new[] {' +
            ($directSpawnHandleLiterals -join ',') +
            '}); var gatewaySmokeDirectThings = Find.CurrentMap.listerThings.AllThings.Where(thing => gatewaySmokeDirectHandles.Contains(thing.ThingID)).ToList(); foreach (var gatewaySmokeDirectThing in gatewaySmokeDirectThings) gatewaySmokeDirectThing.Destroy(DestroyMode.Vanish); gatewaySmokeDirectThings.Count'
        $directSpawnCleanupResponse = Invoke-GatewayTextPost `
            -Uri $executionUri `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-direct-spawn-cleanup' `
            -Source $directCleanupSource
        $directSpawnCleanupEnvelope = Assert-GatewaySuccess `
            -Response $directSpawnCleanupResponse `
            -ArtifactPath $directSpawnCleanupPath `
            -Operation 'Direct-spawn cleanup'
        if (-not $directSpawnCleanupEnvelope.result.Succeeded -or
            [int]$directSpawnCleanupEnvelope.result.Value -ne $directSpawnHandles.Count) {
            throw "Direct-spawn cleanup did not destroy every exact spawned handle. See $directSpawnCleanupPath"
        }

        $directCleanupQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/things/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-direct-spawn-cleanup-query' `
            -Body ([ordered]@{
                scope = 'map'
                defNames = @($directSpawned | ForEach-Object { [string]$_.DefName } | Sort-Object -Unique)
                limit = 1000
            })
        $directCleanupQueryEnvelope = Assert-GatewaySuccess `
            -Response $directCleanupQueryResponse `
            -ArtifactPath $directSpawnCleanupQueryPath `
            -Operation 'Direct-spawn cleanup query'
        $remainingHandles = @($directCleanupQueryEnvelope.result.Things |
            ForEach-Object { [string]$_.Handle })
        foreach ($handle in $directSpawnHandles) {
            if ($remainingHandles -contains $handle) {
                throw "Direct-spawn cleanup left '$handle' on the current map. See $directSpawnCleanupQueryPath"
            }
        }

        $debugActionQueryRequestIds = [System.Collections.Generic.List[string]]::new()
        $debugActionsQueryResponse = $null
        for ($debugQueryAttempt = 1; $debugQueryAttempt -le 1; $debugQueryAttempt++) {
            $debugQueryRequestId = if ($debugQueryAttempt -eq 1) {
                'gateway-smoke-debug-actions-query'
            }
            else {
                "gateway-smoke-debug-actions-query-retry-$debugQueryAttempt"
            }
            $debugActionQueryRequestIds.Add($debugQueryRequestId)
            try {
                $debugActionsQueryResponse = Invoke-GatewayJsonPost `
                    -Uri "$baseUrl/dev-tools/actions/query" `
                    -Token $manifest.token `
                    -RequestId $debugQueryRequestId `
                    -Body ([ordered]@{
                        search = 'Log pathfinder state'
                        categories = @('Pathing')
                        modes = @('Immediate')
                        limit = 100
                    }) `
                    -TimeoutSec 75
            }
            catch {
                $attemptPath = Join-Path $runDirectory "debug-actions-query-attempt-$debugQueryAttempt.txt"
                $_.Exception.ToString() | Set-Content -LiteralPath $attemptPath -Encoding UTF8
                if ($debugQueryAttempt -eq 1) {
                    throw "Debug-action discovery transport failed after $debugQueryAttempt attempts. See $attemptPath"
                }

                Start-Sleep -Seconds 1
                continue
            }
            if ([int]$debugActionsQueryResponse.StatusCode -ge 200 -and
                [int]$debugActionsQueryResponse.StatusCode -lt 300) {
                break
            }

            $attemptPath = Join-Path $runDirectory "debug-actions-query-attempt-$debugQueryAttempt.json"
            $debugActionsQueryResponse.Content | Set-Content -LiteralPath $attemptPath -Encoding UTF8
            $attemptEnvelope = $debugActionsQueryResponse.Content | ConvertFrom-Json -ErrorAction Stop
            if ([string]$attemptEnvelope.error.code -ne 'response_timeout_after_start' -or
                $debugQueryAttempt -eq 1) {
                break
            }

            Start-Sleep -Seconds 1
        }
        $debugActionsQueryEnvelope = Assert-GatewaySuccess `
            -Response $debugActionsQueryResponse `
            -ArtifactPath $debugActionsQueryPath `
            -Operation 'Debug-action discovery'
        $safeDebugAction = @($debugActionsQueryEnvelope.result.Items |
            Where-Object {
                [string]$_.Label -eq 'Log Pathfinder State' -and
                [string]$_.Path -eq 'Actions\Log Pathfinder State' -and
                [string]$_.Category -eq 'Pathing' -and
                [string]$_.Mode -eq 'Immediate' -and
                [string]$_.SourceType -eq 'Verse.DebugActionsMisc' -and
                $null -eq $_.DiscoveryError -and
                $_.Visible -and
                $_.Active
            })
        if ($safeDebugAction.Count -ne 1 -or
            [string]::IsNullOrWhiteSpace([string]$safeDebugAction[0].Handle)) {
            throw "Debug-action discovery did not resolve exactly one safe immediate action. See $debugActionsQueryPath"
        }

        $encodedDebugActionHandle = [Uri]::EscapeDataString([string]$safeDebugAction[0].Handle)
        $debugActionInvokeResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/dev-tools/actions/$encodedDebugActionHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-debug-action-invoke' `
            -Body '{}' `
            -TimeoutSec 75
        $debugActionInvokeEnvelope = Assert-GatewaySuccess `
            -Response $debugActionInvokeResponse `
            -ArtifactPath $debugActionInvokePath `
            -Operation 'Immediate debug-action invocation'
        if (-not $debugActionInvokeEnvelope.result.Completed -or
            $debugActionInvokeEnvelope.result.PointerRequired -or
            [string]$debugActionInvokeEnvelope.result.Mode -ne 'Immediate' -or
            [string]$debugActionInvokeEnvelope.result.Handle -ne [string]$safeDebugAction[0].Handle) {
            throw "Safe debug action did not complete immediately through its discovered handle. See $debugActionInvokePath"
        }

        $pawnGizmosQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmos-pawn-query' `
            -Body ([ordered]@{
                ownerScope = 'explicitOwners'
                ownerHandles = @([string]$quickstartPawn.Handle)
                architectCategoryDefNames = @()
                limit = 100
            })
        $pawnGizmosQueryEnvelope = Assert-GatewaySuccess `
            -Response $pawnGizmosQueryResponse `
            -ArtifactPath $pawnGizmosQueryPath `
            -Operation 'Pawn gizmo discovery'
        $draftGizmos = @($pawnGizmosQueryEnvelope.result.Items | Where-Object {
            [string]$_.InteractionKind -eq 'Toggle' -and
            [string]$_.HotKey -eq 'Command_ColonistDraft' -and
            -not $_.Disabled
        })
        if ($draftGizmos.Count -ne 1 -or
            [string]::IsNullOrWhiteSpace([string]$draftGizmos[0].Handle)) {
            throw "Pawn gizmo discovery did not resolve exactly one enabled native draft toggle. See $pawnGizmosQueryPath"
        }

        $initialDrafted = [bool]$pawnInspectEnvelope.result.Pawn.Drafted
        if ([bool]$draftGizmos[0].ToggleState -ne $initialDrafted) {
            throw "Draft gizmo state disagreed with bounded pawn inspection. See $pawnGizmosQueryPath"
        }
        $encodedDraftGizmoHandle = [Uri]::EscapeDataString([string]$draftGizmos[0].Handle)
        $pawnGizmoToggleResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/$encodedDraftGizmoHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmo-pawn-toggle' `
            -Body '{}'
        $pawnGizmoToggleEnvelope = Assert-GatewaySuccess `
            -Response $pawnGizmoToggleResponse `
            -ArtifactPath $pawnGizmoTogglePath `
            -Operation 'Pawn draft gizmo toggle'
        if (-not $pawnGizmoToggleEnvelope.result.Completed -or
            [string]$pawnGizmoToggleEnvelope.result.InteractionKind -ne 'Toggle' -or
            [bool]$pawnGizmoToggleEnvelope.result.ToggleBefore -ne $initialDrafted -or
            [bool]$pawnGizmoToggleEnvelope.result.ToggleAfter -ne (-not $initialDrafted)) {
            throw "Pawn draft gizmo did not invert its observable toggle state. See $pawnGizmoTogglePath"
        }

        $pawnGizmosRestoreQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmos-pawn-restore-query' `
            -Body ([ordered]@{
                ownerScope = 'explicitOwners'
                ownerHandles = @([string]$quickstartPawn.Handle)
                architectCategoryDefNames = @()
                limit = 100
            })
        $pawnGizmosRestoreQueryEnvelope = Assert-GatewaySuccess `
            -Response $pawnGizmosRestoreQueryResponse `
            -ArtifactPath $pawnGizmosRestoreQueryPath `
            -Operation 'Pawn gizmo rediscovery for restoration'
        $restoreDraftGizmos = @($pawnGizmosRestoreQueryEnvelope.result.Items | Where-Object {
            [string]$_.InteractionKind -eq 'Toggle' -and
            [string]$_.HotKey -eq 'Command_ColonistDraft' -and
            -not $_.Disabled
        })
        if ($restoreDraftGizmos.Count -ne 1 -or
            [bool]$restoreDraftGizmos[0].ToggleState -ne (-not $initialDrafted) -or
            [string]::IsNullOrWhiteSpace([string]$restoreDraftGizmos[0].Handle)) {
            throw "Pawn gizmo rediscovery did not expose the exact inverted draft toggle for restoration. See $pawnGizmosRestoreQueryPath"
        }

        $encodedRestoreDraftGizmoHandle = [Uri]::EscapeDataString([string]$restoreDraftGizmos[0].Handle)
        $pawnGizmoRestoreResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/$encodedRestoreDraftGizmoHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmo-pawn-toggle-restore' `
            -Body '{}'
        $pawnGizmoRestoreEnvelope = Assert-GatewaySuccess `
            -Response $pawnGizmoRestoreResponse `
            -ArtifactPath $pawnGizmoRestorePath `
            -Operation 'Pawn draft gizmo restoration'
        if (-not $pawnGizmoRestoreEnvelope.result.Completed -or
            [bool]$pawnGizmoRestoreEnvelope.result.ToggleBefore -ne (-not $initialDrafted) -or
            [bool]$pawnGizmoRestoreEnvelope.result.ToggleAfter -ne $initialDrafted) {
            throw "Pawn draft gizmo did not restore its initial state. See $pawnGizmoRestorePath"
        }

        $architectGizmosQueryResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmos-architect-query' `
            -Body ([ordered]@{
                ownerScope = 'explicitOwners'
                ownerHandles = @()
                architectCategoryDefNames = @('Orders')
                limit = 100
            })
        $architectGizmosQueryEnvelope = Assert-GatewaySuccess `
            -Response $architectGizmosQueryResponse `
            -ArtifactPath $architectGizmosQueryPath `
            -Operation 'Architect gizmo discovery'
        $planAddGizmos = @($architectGizmosQueryEnvelope.result.Items | Where-Object {
            [string]$_.RuntimeType -eq 'RimWorld.Designator_Plan_Add' -and
            [string]$_.InteractionKind -eq 'Drag' -and
            -not $_.Disabled
        })
        $planRemoveGizmos = @($architectGizmosQueryEnvelope.result.Items | Where-Object {
            [string]$_.RuntimeType -eq 'RimWorld.Designator_Plan_Remove' -and
            [string]$_.InteractionKind -eq 'Drag' -and
            -not $_.Disabled
        })
        if ($planAddGizmos.Count -ne 1 -or $planRemoveGizmos.Count -ne 1) {
            throw "Architect gizmo discovery did not resolve exact enabled add/remove plan drag adapters. See $architectGizmosQueryPath"
        }

        $planCornerA = [ordered]@{
            x = [int]$quickstartRun.Result.Center.X + 1
            z = [int]$quickstartRun.Result.Center.Z - 3
        }
        $planCornerB = [ordered]@{
            x = [int]$quickstartRun.Result.Center.X + 2
            z = [int]$quickstartRun.Result.Center.Z - 2
        }
        $encodedPlanAddHandle = [Uri]::EscapeDataString([string]$planAddGizmos[0].Handle)
        $dragInvokeResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/$encodedPlanAddHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmo-drag-invoke' `
            -Body '{}'
        $dragInvokeEnvelope = Assert-GatewaySuccess `
            -Response $dragInvokeResponse `
            -ArtifactPath $dragInvokePath `
            -Operation 'Architect drag interaction start'
        $dragInteraction = $dragInvokeEnvelope.result.Interaction
        if ($dragInvokeEnvelope.result.Completed -or
            [string]$dragInvokeEnvelope.result.InteractionKind -ne 'Drag' -or
            $null -eq $dragInteraction -or
            [string]::IsNullOrWhiteSpace([string]$dragInteraction.Handle)) {
            throw "Plan gizmo did not start a semantic drag interaction. See $dragInvokePath"
        }

        $interactionCurrentResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/interactions/current" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-current'
        $interactionCurrentEnvelope = Assert-GatewaySuccess `
            -Response $interactionCurrentResponse `
            -ArtifactPath $interactionCurrentPath `
            -Operation 'Current interaction capture'
        if ([string]$interactionCurrentEnvelope.result.Handle -ne [string]$dragInteraction.Handle) {
            throw "Current interaction did not report the started drag. See $interactionCurrentPath"
        }

        $encodedDragInteractionHandle = [Uri]::EscapeDataString([string]$dragInteraction.Handle)
        $interactionCancelResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/interactions/$encodedDragInteractionHandle/cancel" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-cancel' `
            -Body '{}'
        $interactionCancelEnvelope = Assert-GatewaySuccess `
            -Response $interactionCancelResponse `
            -ArtifactPath $interactionCancelPath `
            -Operation 'Semantic interaction cancellation'
        if (-not $interactionCancelEnvelope.result.Cancelled -or
            [string]$interactionCancelEnvelope.result.InteractionHandle -ne [string]$dragInteraction.Handle) {
            throw "Semantic drag interaction did not cancel cleanly. See $interactionCancelPath"
        }

        $dragReinvokeResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/$encodedPlanAddHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmo-drag-reinvoke' `
            -Body '{}'
        $dragReinvokeEnvelope = Assert-GatewaySuccess `
            -Response $dragReinvokeResponse `
            -ArtifactPath $dragReinvokePath `
            -Operation 'Architect drag interaction restart'
        $activePlanInteraction = $dragReinvokeEnvelope.result.Interaction
        if ($null -eq $activePlanInteraction -or
            [string]::IsNullOrWhiteSpace([string]$activePlanInteraction.Handle)) {
            throw "Plan gizmo did not restart after cancellation. See $dragReinvokePath"
        }

        $encodedActivePlanInteraction = [Uri]::EscapeDataString([string]$activePlanInteraction.Handle)
        $dragApplyResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/interactions/$encodedActivePlanInteraction/apply" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-drag-apply' `
            -Body ([ordered]@{
                kind = 'rectangle'
                cornerA = $planCornerA
                cornerB = $planCornerB
            })
        $dragApplyEnvelope = Assert-GatewaySuccess `
            -Response $dragApplyResponse `
            -ArtifactPath $dragApplyPath `
            -Operation 'Architect rectangle application'
        if (-not $dragApplyEnvelope.result.Completed -or
            @($dragApplyEnvelope.result.Accepted).Count -ne 4 -or
            @($dragApplyEnvelope.result.Rejected).Count -ne 0) {
            throw "Plan drag did not accept and apply its exact 2x2 rectangle. See $dragApplyPath"
        }

        $planCellsSource = 'var gatewaySmokePlanCells = new[] {' +
            "new IntVec3($($planCornerA.x),0,$($planCornerA.z))," +
            "new IntVec3($($planCornerA.x),0,$($planCornerB.z))," +
            "new IntVec3($($planCornerB.x),0,$($planCornerA.z))," +
            "new IntVec3($($planCornerB.x),0,$($planCornerB.z))" +
            '}; gatewaySmokePlanCells.Count(cell => Find.CurrentMap.planManager.PlanAt(cell) != null)'
        $planCreatedResponse = Invoke-GatewayTextPost `
            -Uri $executionUri `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-plan-created' `
            -Source $planCellsSource
        $planCreatedEnvelope = Assert-GatewaySuccess `
            -Response $planCreatedResponse `
            -ArtifactPath $planCreatedPath `
            -Operation 'Plan interaction mutation check'
        if (-not $planCreatedEnvelope.result.Succeeded -or
            [int]$planCreatedEnvelope.result.Value -ne 4) {
            throw "Plan drag did not create all four expected plan designations. See $planCreatedPath"
        }

        $encodedPlanRemoveHandle = [Uri]::EscapeDataString([string]$planRemoveGizmos[0].Handle)
        $dragRemoveInvokeResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/gizmos/$encodedPlanRemoveHandle/invoke" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-gizmo-drag-remove-invoke' `
            -Body '{}'
        $dragRemoveInvokeEnvelope = Assert-GatewaySuccess `
            -Response $dragRemoveInvokeResponse `
            -ArtifactPath $dragRemoveInvokePath `
            -Operation 'Plan-removal drag interaction start'
        $removePlanInteraction = $dragRemoveInvokeEnvelope.result.Interaction
        if ($null -eq $removePlanInteraction -or
            [string]::IsNullOrWhiteSpace([string]$removePlanInteraction.Handle)) {
            throw "Plan-removal gizmo did not start a semantic drag interaction. See $dragRemoveInvokePath"
        }

        $encodedRemovePlanInteraction = [Uri]::EscapeDataString([string]$removePlanInteraction.Handle)
        $dragRemoveApplyResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/interactions/$encodedRemovePlanInteraction/apply" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-drag-remove-apply' `
            -Body ([ordered]@{
                kind = 'rectangle'
                cornerA = $planCornerA
                cornerB = $planCornerB
            })
        $dragRemoveApplyEnvelope = Assert-GatewaySuccess `
            -Response $dragRemoveApplyResponse `
            -ArtifactPath $dragRemoveApplyPath `
            -Operation 'Plan-removal rectangle application'
        if (-not $dragRemoveApplyEnvelope.result.Completed -or
            @($dragRemoveApplyEnvelope.result.Accepted).Count -ne 4 -or
            @($dragRemoveApplyEnvelope.result.Rejected).Count -ne 0) {
            throw "Plan-removal drag did not clean the exact 2x2 rectangle. See $dragRemoveApplyPath"
        }

        $interactionFinalResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/interactions/current" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-final'
        $interactionFinalEnvelope = Assert-GatewaySuccess `
            -Response $interactionFinalResponse `
            -ArtifactPath $interactionFinalPath `
            -Operation 'Final interaction capture'
        if ($null -ne $interactionFinalEnvelope.result) {
            throw "A semantic interaction remained active after the completed removal. See $interactionFinalPath"
        }

        $planCleanupResponse = Invoke-GatewayTextPost `
            -Uri $executionUri `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-interaction-plan-cleanup' `
            -Source 'gatewaySmokePlanCells.Count(cell => Find.CurrentMap.planManager.PlanAt(cell) != null)'
        $planCleanupEnvelope = Assert-GatewaySuccess `
            -Response $planCleanupResponse `
            -ArtifactPath $planCleanupPath `
            -Operation 'Plan interaction cleanup check'
        if (-not $planCleanupEnvelope.result.Succeeded -or
            [int]$planCleanupEnvelope.result.Value -ne 0) {
            throw "Plan-removal interaction left disposable designations behind. See $planCleanupPath"
        }

        $cameraBeforeResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-before'
        $cameraBeforeEnvelope = Assert-GatewaySuccess `
            -Response $cameraBeforeResponse `
            -ArtifactPath $cameraBeforePath `
            -Operation 'Fresh camera capture before movement'
        $initialCamera = $cameraBeforeEnvelope.result.Camera
        if ($null -eq $initialCamera) {
            throw "Fresh pre-move game-state capture did not contain a map camera. See $cameraBeforePath"
        }
        Save-GatewayScreenshot `
            -BaseUrl $baseUrl `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-before-screenshot' `
            -ArtifactPath $cameraBeforeScreenshotPath

        $cameraTarget = Select-GatewayCameraMovementTarget `
            -Camera $initialCamera `
            -Candidates $mapThings `
            -MinimumDistance 20
        $cameraTargetThing = $cameraTarget.Thing

        $cameraMoveResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/camera" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-move' `
            -Body ([ordered]@{
                mapHandle = [string]$thingMapEnvelope.result.MapHandle
                center = [ordered]@{
                    x = [int]$cameraTargetThing.Position.X
                    z = [int]$cameraTargetThing.Position.Z
                }
            })
        $cameraMoveEnvelope = Assert-GatewaySuccess `
            -Response $cameraMoveResponse `
            -ArtifactPath $cameraMovePath `
            -Operation 'Absolute camera movement'
        Save-GatewayScreenshot `
            -BaseUrl $baseUrl `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-after-move-screenshot' `
            -ArtifactPath $cameraAfterMoveScreenshotPath
        $cameraAfterMoveStateResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-after-move-state'
        $cameraAfterMoveStateEnvelope = Assert-GatewaySuccess `
            -Response $cameraAfterMoveStateResponse `
            -ArtifactPath $cameraAfterMoveStatePath `
            -Operation 'Settled camera capture after movement'
        $settledCameraAfterMove = $cameraAfterMoveStateEnvelope.result.Camera
        if ([int]$cameraMoveEnvelope.result.Before.Center.X -ne [int]$initialCamera.Center.X -or
            [int]$cameraMoveEnvelope.result.Before.Center.Z -ne [int]$initialCamera.Center.Z -or
            [Math]::Abs([single]$cameraMoveEnvelope.result.Before.RootSize - [single]$initialCamera.RootSize) -ge 0.1 -or
            [int]$cameraMoveEnvelope.result.Before.ViewRect.MinX -ne [int]$initialCamera.ViewRect.MinX -or
            [int]$cameraMoveEnvelope.result.Before.ViewRect.MinZ -ne [int]$initialCamera.ViewRect.MinZ -or
            [int]$cameraMoveEnvelope.result.Before.ViewRect.MaxX -ne [int]$initialCamera.ViewRect.MaxX -or
            [int]$cameraMoveEnvelope.result.Before.ViewRect.MaxZ -ne [int]$initialCamera.ViewRect.MaxZ) {
            throw "Camera changed between the pre-move capture and the absolute movement request. See $cameraBeforePath and $cameraMovePath"
        }
        if ($null -eq $settledCameraAfterMove -or
            [int]$cameraMoveEnvelope.result.After.Center.X -ne [int]$cameraTargetThing.Position.X -or
            [int]$cameraMoveEnvelope.result.After.Center.Z -ne [int]$cameraTargetThing.Position.Z -or
            [int]$settledCameraAfterMove.Center.X -ne [int]$cameraTargetThing.Position.X -or
            [int]$settledCameraAfterMove.Center.Z -ne [int]$cameraTargetThing.Position.Z -or
            [Math]::Abs([single]$settledCameraAfterMove.RootSize - [single]$initialCamera.RootSize) -ge 0.1 -or
            [int]$cameraMoveEnvelope.result.After.ViewRect.MinX -ne [int]$settledCameraAfterMove.ViewRect.MinX -or
            [int]$cameraMoveEnvelope.result.After.ViewRect.MinZ -ne [int]$settledCameraAfterMove.ViewRect.MinZ -or
            [int]$cameraMoveEnvelope.result.After.ViewRect.MaxX -ne [int]$settledCameraAfterMove.ViewRect.MaxX -or
            [int]$cameraMoveEnvelope.result.After.ViewRect.MaxZ -ne [int]$settledCameraAfterMove.ViewRect.MaxZ) {
            throw "Camera did not remain centered on the distant queried map object at the rendered-frame boundary. See $cameraMovePath, $cameraAfterMoveStatePath, and $cameraAfterMoveScreenshotPath"
        }

        $cameraAfterMove = $settledCameraAfterMove
        $rootSize = [single]$cameraAfterMove.RootSize
        $minimumRootSize = [single]$cameraAfterMove.MinimumRootSize
        $maximumRootSize = [single]$cameraAfterMove.MaximumRootSize
        $zoomRootSize = if (($rootSize - $minimumRootSize) -ge 1.0) {
            [single]($rootSize - [Math]::Min(5.0, ($rootSize - $minimumRootSize) / 2.0))
        }
        else {
            [single]($rootSize + [Math]::Min(5.0, ($maximumRootSize - $rootSize) / 2.0))
        }
        if ([Math]::Abs($zoomRootSize - $rootSize) -lt 0.01) {
            throw "Camera did not expose a second supported root size for zoom verification. See $cameraMovePath"
        }

        $cameraZoomResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/camera" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-zoom' `
            -Body ([ordered]@{
                mapHandle = [string]$thingMapEnvelope.result.MapHandle
                rootSize = $zoomRootSize
            })
        $cameraZoomEnvelope = Assert-GatewaySuccess `
            -Response $cameraZoomResponse `
            -ArtifactPath $cameraZoomPath `
            -Operation 'Absolute camera zoom'
        Save-GatewayScreenshot `
            -BaseUrl $baseUrl `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-after-zoom-screenshot' `
            -ArtifactPath $cameraAfterZoomScreenshotPath
        $cameraAfterZoomStateResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-after-zoom-state'
        $cameraAfterZoomStateEnvelope = Assert-GatewaySuccess `
            -Response $cameraAfterZoomStateResponse `
            -ArtifactPath $cameraAfterZoomStatePath `
            -Operation 'Settled camera capture after zoom'
        $settledCameraAfterZoom = $cameraAfterZoomStateEnvelope.result.Camera
        if ($null -eq $settledCameraAfterZoom -or
            [Math]::Abs([single]$cameraZoomEnvelope.result.After.RootSize - $zoomRootSize) -ge 0.1 -or
            [Math]::Abs([single]$settledCameraAfterZoom.RootSize - $zoomRootSize) -ge 0.1 -or
            [int]$settledCameraAfterZoom.Center.X -ne [int]$cameraTargetThing.Position.X -or
            [int]$settledCameraAfterZoom.Center.Z -ne [int]$cameraTargetThing.Position.Z -or
            [int]$cameraZoomEnvelope.result.After.ViewRect.MinX -ne [int]$settledCameraAfterZoom.ViewRect.MinX -or
            [int]$cameraZoomEnvelope.result.After.ViewRect.MinZ -ne [int]$settledCameraAfterZoom.ViewRect.MinZ -or
            [int]$cameraZoomEnvelope.result.After.ViewRect.MaxX -ne [int]$settledCameraAfterZoom.ViewRect.MaxX -or
            [int]$cameraZoomEnvelope.result.After.ViewRect.MaxZ -ne [int]$settledCameraAfterZoom.ViewRect.MaxZ) {
            throw "Camera did not retain the requested native zoom and center at the rendered-frame boundary. See $cameraZoomPath, $cameraAfterZoomStatePath, and $cameraAfterZoomScreenshotPath"
        }

        $thingViewResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/things/query" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-things-view' `
            -Body ([ordered]@{
                scope = 'view'
                defNames = @([string]$cameraTargetThing.DefName)
                labelContains = [string]$cameraTargetThing.Label
                limit = 100
            })
        $thingViewEnvelope = Assert-GatewaySuccess `
            -Response $thingViewResponse `
            -ArtifactPath $thingsViewPath `
            -Operation 'Filtered current-view thing query'
        $viewThings = @($thingViewEnvelope.result.Things)
        if (@($viewThings | Where-Object { [string]$_.Handle -eq [string]$cameraTargetThing.Handle }).Count -ne 1) {
            throw "Filtered current-view query did not return the centered map object. See $thingsViewPath"
        }
        foreach ($thing in $viewThings) {
            if ([string]$thing.DefName -ne [string]$cameraTargetThing.DefName -or
                ([string]$thing.Label).IndexOf(
                    [string]$cameraTargetThing.Label,
                    [StringComparison]::OrdinalIgnoreCase) -lt 0) {
                throw "Filtered current-view query returned an object outside its Def/label filters. See $thingsViewPath"
            }
        }

        $selectionRestoreResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/selection" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-selection-restore' `
            -Body ([ordered]@{ operation = 'replace'; handles = $initialSelectionHandles })
        $selectionRestoreEnvelope = Assert-GatewaySuccess `
            -Response $selectionRestoreResponse `
            -ArtifactPath $selectionRestorePath `
            -Operation 'Selection restoration'
        $restoredSelectionHandles = @($selectionRestoreEnvelope.result.After |
            ForEach-Object { [string]$_.Handle })
        if (($restoredSelectionHandles -join '|') -ne ($initialSelectionHandles -join '|')) {
            throw "Selection restoration did not reproduce the prior selection. See $selectionRestorePath"
        }

        $cameraRestoreResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/camera" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-restore' `
            -Body ([ordered]@{
                mapHandle = [string]$initialCamera.MapHandle
                center = [ordered]@{
                    x = [int]$initialCamera.Center.X
                    z = [int]$initialCamera.Center.Z
                }
                rootSize = [single]$initialCamera.RootSize
            })
        $cameraRestoreEnvelope = Assert-GatewaySuccess `
            -Response $cameraRestoreResponse `
            -ArtifactPath $cameraRestorePath `
            -Operation 'Camera restoration'
        Save-GatewayScreenshot `
            -BaseUrl $baseUrl `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-restored-screenshot' `
            -ArtifactPath $cameraRestoredScreenshotPath
        $cameraRestoredStateResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-camera-restored-state'
        $cameraRestoredStateEnvelope = Assert-GatewaySuccess `
            -Response $cameraRestoredStateResponse `
            -ArtifactPath $cameraRestoredStatePath `
            -Operation 'Settled camera capture after restoration'
        $settledRestoredCamera = $cameraRestoredStateEnvelope.result.Camera
        if ($null -eq $settledRestoredCamera -or
            [int]$cameraRestoreEnvelope.result.After.Center.X -ne [int]$initialCamera.Center.X -or
            [int]$cameraRestoreEnvelope.result.After.Center.Z -ne [int]$initialCamera.Center.Z -or
            [Math]::Abs([single]$cameraRestoreEnvelope.result.After.RootSize - [single]$initialCamera.RootSize) -ge 0.1 -or
            [int]$settledRestoredCamera.Center.X -ne [int]$initialCamera.Center.X -or
            [int]$settledRestoredCamera.Center.Z -ne [int]$initialCamera.Center.Z -or
            [Math]::Abs([single]$settledRestoredCamera.RootSize - [single]$initialCamera.RootSize) -ge 0.1 -or
            [int]$cameraRestoreEnvelope.result.After.ViewRect.MinX -ne [int]$settledRestoredCamera.ViewRect.MinX -or
            [int]$cameraRestoreEnvelope.result.After.ViewRect.MinZ -ne [int]$settledRestoredCamera.ViewRect.MinZ -or
            [int]$cameraRestoreEnvelope.result.After.ViewRect.MaxX -ne [int]$settledRestoredCamera.ViewRect.MaxX -or
            [int]$cameraRestoreEnvelope.result.After.ViewRect.MaxZ -ne [int]$settledRestoredCamera.ViewRect.MaxZ) {
            throw "Camera restoration did not remain at the initial state across a rendered-frame boundary. See $cameraRestorePath, $cameraRestoredStatePath, and $cameraRestoredScreenshotPath"
        }

        # Developer mode intentionally remains enabled through all semantic developer-action and gizmo probes.
        $gameStateRestoreResponse = Invoke-GatewayJsonPost `
            -Uri "$baseUrl/game-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-game-state-restore' `
            -Body ([ordered]@{
                devMode = [bool]$initialGameState.DevMode
                godMode = [bool]$initialGameState.GodMode
                speed = [string]$initialGameState.Speed
            })
        $gameStateRestoreEnvelope = Assert-GatewaySuccess `
            -Response $gameStateRestoreResponse `
            -ArtifactPath $gameStateRestorePath `
            -Operation 'Game-state restoration'
        if ([bool]$gameStateRestoreEnvelope.result.After.DevMode -ne [bool]$initialGameState.DevMode -or
            [bool]$gameStateRestoreEnvelope.result.After.GodMode -ne [bool]$initialGameState.GodMode -or
            [bool]$gameStateRestoreEnvelope.result.After.Paused -ne [bool]$initialGameState.Paused -or
            [string]$gameStateRestoreEnvelope.result.After.Speed -ne [string]$initialGameState.Speed) {
            throw "Game-state restoration did not reproduce the initial controls. See $gameStateRestorePath"
        }

        $postStatusResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/status" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-post-quickstart-status'
        $postStatusResponse.Content | Set-Content -LiteralPath $postQuickstartStatusPath -Encoding UTF8
        $postStatusEnvelope = $postStatusResponse.Content | ConvertFrom-Json
        if ([int]$postStatusResponse.StatusCode -ne 200 -or -not $postStatusEnvelope.ok) {
            throw "Post-quickstart status capture failed. See $postQuickstartStatusPath"
        }
        $status = $postStatusEnvelope

        $postUiResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/ui-state" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-post-quickstart-ui'
        $postUiResponse.Content | Set-Content -LiteralPath $postQuickstartUiStatePath -Encoding UTF8
        if ([int]$postUiResponse.StatusCode -ne 200) {
            throw "Post-quickstart UI-state capture failed. See $postQuickstartUiStatePath"
        }

        $postLogsResponse = Invoke-GatewayGet `
            -Uri "$baseUrl/logs?after=$quickstartLogCursor&limit=500" `
            -Token $manifest.token `
            -RequestId 'gateway-smoke-post-quickstart-logs'
        $postLogsResponse.Content | Set-Content -LiteralPath $postQuickstartLogsPath -Encoding UTF8
        if ([int]$postLogsResponse.StatusCode -ne 200) {
            throw "Post-quickstart correlated log capture failed. See $postQuickstartLogsPath"
        }
        $postLogsEnvelope = $postLogsResponse.Content | ConvertFrom-Json -ErrorAction Stop
        if (-not $postLogsEnvelope.ok -or $postLogsEnvelope.result.PageTruncated) {
            throw "Post-quickstart log page was unsuccessful or truncated. See $postQuickstartLogsPath"
        }
        $semanticRequestIds = @(
            'gateway-smoke-game-state-initial',
            'gateway-smoke-game-state-enable-dev',
            'gateway-smoke-game-state-speed',
            'gateway-smoke-things-map',
            'gateway-smoke-thing-inspect-building',
            'gateway-smoke-thing-inspect-pawn',
            'gateway-smoke-selection-set',
            'gateway-smoke-selection-query',
            'gateway-smoke-direct-spawn',
            'gateway-smoke-direct-spawn-replay',
            'gateway-smoke-direct-spawn-query',
            'gateway-smoke-direct-spawn-cleanup',
            'gateway-smoke-direct-spawn-cleanup-query',
            'gateway-smoke-debug-action-invoke',
            'gateway-smoke-gizmos-pawn-query',
            'gateway-smoke-gizmo-pawn-toggle',
            'gateway-smoke-gizmos-pawn-restore-query',
            'gateway-smoke-gizmo-pawn-toggle-restore',
            'gateway-smoke-gizmos-architect-query',
            'gateway-smoke-gizmo-drag-invoke',
            'gateway-smoke-interaction-current',
            'gateway-smoke-interaction-cancel',
            'gateway-smoke-gizmo-drag-reinvoke',
            'gateway-smoke-interaction-drag-apply',
            'gateway-smoke-interaction-plan-created',
            'gateway-smoke-gizmo-drag-remove-invoke',
            'gateway-smoke-interaction-drag-remove-apply',
            'gateway-smoke-interaction-final',
            'gateway-smoke-interaction-plan-cleanup',
            'gateway-smoke-camera-before',
            'gateway-smoke-camera-before-screenshot',
            'gateway-smoke-camera-move',
            'gateway-smoke-camera-after-move-screenshot',
            'gateway-smoke-camera-after-move-state',
            'gateway-smoke-camera-zoom',
            'gateway-smoke-camera-after-zoom-screenshot',
            'gateway-smoke-camera-after-zoom-state',
            'gateway-smoke-things-view',
            'gateway-smoke-selection-restore',
            'gateway-smoke-camera-restore',
            'gateway-smoke-camera-restored-screenshot',
            'gateway-smoke-camera-restored-state',
            'gateway-smoke-game-state-restore'
        ) + @($gamePauseRequestIds) + @($debugActionQueryRequestIds)
        $capturedRequestIds = @($postLogsEnvelope.result.Entries |
            ForEach-Object { [string]$_.RequestId } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique)
        foreach ($requestId in $semanticRequestIds) {
            if ($capturedRequestIds -notcontains $requestId) {
                throw "Post-quickstart logs omitted correlated request '$requestId'. See $postQuickstartLogsPath"
            }
        }
        $pathfinderStateLogEntries = @($postLogsEnvelope.result.Entries |
            Where-Object { [string]$_.Message -like 'Pathfinder State*' })
        if ($pathfinderStateLogEntries.Count -ne 1) {
            throw "Immediate debug-action evidence did not contain exactly one emitted 'Pathfinder State' log entry. See $postQuickstartLogsPath"
        }
        }
    }

    $gatewayHeaders = @{
        Authorization = "Bearer $($manifest.token)"
        'X-Request-Id' = 'gateway-smoke-screenshot'
    }
    $gatewayScreenshotResponse = Invoke-TrackedGatewayRequest `
        -Method 'POST' `
        -Uri "$baseUrl/screenshots" `
        -RequestId 'gateway-smoke-screenshot' `
        -Operation {
            Invoke-WebRequest `
                -Uri "$baseUrl/screenshots" `
                -Method Post `
                -Headers $gatewayHeaders `
                -ContentType 'application/json' `
                -Body '{}' `
                -TimeoutSec 30 `
                -OutFile $gatewayScreenshotPath `
                -PassThru `
                -SkipHttpErrorCheck
        }

    if ([int]$gatewayScreenshotResponse.StatusCode -ne 200) {
        throw "Gateway screenshot failed with HTTP $([int]$gatewayScreenshotResponse.StatusCode)."
    }

    $pngSignature = @(Get-Content -LiteralPath $gatewayScreenshotPath -AsByteStream -TotalCount 8)
    $expectedPngSignature = @(0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a)
    if (($pngSignature -join ',') -ne ($expectedPngSignature -join ',')) {
        throw "Gateway screenshot is not a valid PNG: $gatewayScreenshotPath"
    }

    if ((Get-Content -LiteralPath $playerLogPath -Raw).Contains([string]$manifest.token)) {
        throw 'The bearer token leaked into Player.log.'
    }

    $liveVersionMarker = "RimWorld $([string]$manifest.gameVersion)"
    if (-not (Select-String -LiteralPath $playerLogPath -SimpleMatch -Pattern $liveVersionMarker -Quiet)) {
        throw "Player.log does not identify the exact manifest runtime version '$liveVersionMarker'. See $playerLogPath"
    }

    foreach ($marker in $validatedLogMarkers) {
        if (-not (Select-String -LiteralPath $playerLogPath -SimpleMatch -Pattern $marker -Quiet)) {
            throw "Player.log does not contain the expected target-mod startup marker '$marker'. See $playerLogPath"
        }
    }

    $runtimeErrors = @(Get-GatewayRuntimeLogErrors -Path $playerLogPath)
    if ($runtimeErrors.Count -gt 0) {
        throw "Player.log contains a mod or gateway load/runtime error. See $playerLogPath"
    }

    if ($runGatewayRegressionScenario) {
    $serviceStatusBefore = Invoke-FlaUiJson -Arguments @('service', 'status')
    if (-not ($serviceStatusBefore.PSObject.Properties.Name -contains 'running')) {
        throw 'FlaUI service status omitted running before setup.'
    }
    $flaUiServiceWasRunning = [bool]$serviceStatusBefore.running
    if (-not $flaUiServiceWasRunning) {
        $flaUiServiceStartAttempted = $true
        $null = Invoke-FlaUiJson -Arguments @('service', 'start')
    }

    $statusBefore = Invoke-FlaUiJson -Arguments @('status')
    if ($statusBefore.data.connected) {
        throw "FlaUI is already connected to PID $($statusBefore.data.processId); refusing to disrupt that session."
    }

    $flaUiConnectionAttempted = $true
    $null = Invoke-FlaUiJson -Arguments @('connect', '--pid', $launchedProcess.Id.ToString([Globalization.CultureInfo]::InvariantCulture))
    $flaUiStatus = Invoke-FlaUiJson -Arguments @('status')
    if (-not $flaUiStatus.data.connected -or [int]$flaUiStatus.data.processId -ne $launchedProcess.Id) {
        throw 'FlaUI did not remain connected to the launched RimWorld process.'
    }

    $flaUiDesktopEvidence = Get-GatewaySmokeFlaUiDesktopEvidence `
        -GatewayScreenshotPath $gatewayScreenshotPath `
        -FlaUiScreenshotPath $screenshotPath `
        -ArtifactPath $flaUiEvidencePath `
        -FlaUiInvoker {
            param([string[]]$Arguments)
            Invoke-FlaUiJson -Arguments $Arguments
        }

    $clickHeaders = @{
        Authorization = "Bearer $($manifest.token)"
        'X-Request-Id' = 'gateway-smoke-click'
    }
    $clickResponse = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $null = Invoke-FlaUiJson -Arguments @('window', 'focus')
        }
        catch {
            # Unity can expose no UIA window even though the connected process has a valid Win32 main window.
        }

        $automationShell = $null
        try {
            $automationShell = New-Object -ComObject WScript.Shell
            $null = $automationShell.AppActivate($launchedProcess.Id)
        }
        finally {
            if ($null -ne $automationShell) {
                $null = [Runtime.InteropServices.Marshal]::FinalReleaseComObject($automationShell)
            }
        }

        Start-Sleep -Milliseconds 250
        $clickResponse = Invoke-TrackedGatewayRequest `
            -Method 'POST' `
            -Uri "$baseUrl/input/click" `
            -RequestId 'gateway-smoke-click' `
            -Operation {
                Invoke-WebRequest `
                    -Uri "$baseUrl/input/click" `
                    -Method Post `
                    -Headers $clickHeaders `
                    -ContentType 'application/json' `
                    -Body '{"x":1000,"y":100,"button":"left","activate":true}' `
                    -TimeoutSec 20 `
                    -SkipHttpErrorCheck
            }
        if ([int]$clickResponse.StatusCode -eq 200) {
            break
        }

        $clickError = $clickResponse.Content | ConvertFrom-Json
        if ([string]$clickError.error.code -ne 'focus_lost') {
            break
        }
    }

    $clickResponse.Content | Set-Content -LiteralPath $clickPath -Encoding UTF8
    $clickOutcome = 'injected'
    if ([int]$clickResponse.StatusCode -ne 200) {
        $clickError = $clickResponse.Content | ConvertFrom-Json
        if ([int]$clickResponse.StatusCode -eq 400 -and
            [string]$clickError.error.code -eq 'focus_lost' -and
            -not $RequireRawClick) {
            $clickOutcome = 'focus-guard-rejected'
        }
        else {
            throw "Gateway raw click failed with HTTP $([int]$clickResponse.StatusCode). See $clickPath"
        }
    }
    }

    if ($null -ne $scenarioPlan.Descriptor) {
        $scenarioStepResults = [System.Collections.Generic.List[object]]::new()
        $scenarioDescriptorDirectory = Split-Path -Parent ([string]$scenarioPlan.DescriptorPath)
        $scenarioDescriptorPrefix = $scenarioDescriptorDirectory.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
        $scenarioStepIndex = 0
        foreach ($step in @($scenarioPlan.Descriptor.steps)) {
            $scenarioStepIndex++
            $stepId = [string]$step.id
            if ($stepId -cnotmatch '^[a-z0-9][a-z0-9._-]{0,63}$') {
                throw "Gateway scenario '$($scenarioPlan.Name)' has invalid step id '$stepId'."
            }

            $kind = [string]$step.kind
            switch ($kind) {
                'csharp' {
                    $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $scenarioDescriptorDirectory ([string]$step.sourceFile)))
                    if (-not $sourcePath.StartsWith($scenarioDescriptorPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                        -not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
                        throw "Gateway scenario '$($scenarioPlan.Name)' has invalid C# source path for step '$stepId'."
                    }

                    $stepArtifactPath = Join-Path $runDirectory "scenario-$scenarioStepIndex-$stepId.json"
                    $stepResponse = Invoke-GatewayTextPost `
                        -Uri "$baseUrl/executions/csharp" `
                        -Token $manifest.token `
                        -RequestId "gateway-scenario-$($scenarioPlan.Name)-$stepId" `
                        -Source (Get-Content -LiteralPath $sourcePath -Raw)
                    $stepResponse.Content | Set-Content -LiteralPath $stepArtifactPath -Encoding UTF8
                    $stepEnvelope = $stepResponse.Content | ConvertFrom-Json -ErrorAction Stop
                    if ([int]$stepResponse.StatusCode -ne 200 -or
                        -not $stepEnvelope.ok -or
                        -not $stepEnvelope.result.Succeeded) {
                        throw "Gateway scenario '$($scenarioPlan.Name)' C# step '$stepId' failed. See $stepArtifactPath"
                    }

                    $scenarioStepResults.Add([pscustomobject]@{
                        Id = $stepId
                        Kind = $kind
                        Artifact = $stepArtifactPath
                    })
                }
                'screenshot' {
                    $fileName = [string]$step.fileName
                    if ($fileName -cnotmatch '^[a-z0-9][a-z0-9._-]{0,63}\.png$') {
                        throw "Gateway scenario '$($scenarioPlan.Name)' has invalid screenshot file name '$fileName'."
                    }

                    $stepArtifactPath = Join-Path $runDirectory $fileName
                    Save-GatewayScreenshot `
                        -BaseUrl $baseUrl `
                        -Token $manifest.token `
                        -RequestId "gateway-scenario-$($scenarioPlan.Name)-$stepId" `
                        -ArtifactPath $stepArtifactPath
                    $scenarioStepResults.Add([pscustomobject]@{
                        Id = $stepId
                        Kind = $kind
                        Artifact = $stepArtifactPath
                    })
                }
                default {
                    throw "Gateway scenario '$($scenarioPlan.Name)' has unsupported step kind '$kind'."
                }
            }
        }

        [pscustomobject][ordered]@{
            Name = [string]$scenarioPlan.Name
            Descriptor = [string]$scenarioPlan.DescriptorPath
            Status = 'completed'
            Steps = @($scenarioStepResults)
        } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $scenarioResultPath -Encoding UTF8
    }

    if ($InteractiveHoldSeconds -gt 0) {
        $holdStartedUtc = [datetime]::UtcNow
        $holdDeadline = $holdStartedUtc.AddSeconds($InteractiveHoldSeconds)
        [pscustomobject][ordered]@{
            Status = 'active'
            ProcessId = [int]$launchedProcess.Id
            StartedUtc = $holdStartedUtc.ToString('O')
            UntilUtc = $holdDeadline.ToString('O')
            Manifest = $manifestPath
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $interactiveHoldPath -Encoding UTF8

        Write-Host "Interactive verification hold active until $($holdDeadline.ToLocalTime().ToString('T')); state: $interactiveHoldPath"
        while ([datetime]::UtcNow -lt $holdDeadline) {
            $launchedProcess.Refresh()
            if ($launchedProcess.HasExited) {
                throw "RimWorld exited during the interactive verification hold. See $playerLogPath"
            }

            Start-Sleep -Seconds 1
        }

        [pscustomobject][ordered]@{
            Status = 'completed'
            ProcessId = [int]$launchedProcess.Id
            StartedUtc = $holdStartedUtc.ToString('O')
            CompletedUtc = [datetime]::UtcNow.ToString('O')
            Manifest = $manifestPath
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $interactiveHoldPath -Encoding UTF8
    }

    $shutdownHeaders = @{
        Authorization = "Bearer $($manifest.token)"
        'X-Request-Id' = 'gateway-smoke-shutdown'
    }
    $shutdownResponse = Invoke-TrackedGatewayRequest `
        -Method 'POST' `
        -Uri "$baseUrl/server/shutdown" `
        -RequestId 'gateway-smoke-shutdown' `
        -Operation {
            Invoke-WebRequest `
                -Uri "$baseUrl/server/shutdown" `
                -Method Post `
                -Headers $shutdownHeaders `
                -ContentType 'application/json' `
                -Body '{}' `
                -TimeoutSec 20 `
                -SkipHttpErrorCheck
        }
    $shutdownResponse.Content | Set-Content -LiteralPath $shutdownPath -Encoding UTF8
    if ([int]$shutdownResponse.StatusCode -ne 202) {
        throw "Controlled gateway shutdown failed with HTTP $([int]$shutdownResponse.StatusCode). See $shutdownPath"
    }

    $tombstonePath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\session.json"
    $gatewayRequestJournalPath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\requests.jsonl"
    $gatewayLastRequestPath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\last-request.json"
    $tombstone = Wait-GatewayStoppedTombstone `
        -LiveManifestPath $manifestPath `
        -TombstonePath $tombstonePath `
        -TimeoutMilliseconds 15000

    $result = [pscustomobject]@{
        Status = 'passed'
        Version = $rimWorldVersion
        ConfiguredGameVersion = $rimWorldVersion
        LiveGameVersion = [string]$manifest.gameVersion
        GatewayModVersion = [string]$manifest.modVersion
        ProcessId = $launchedProcess.Id
        InteractiveHoldSeconds = [int]$InteractiveHoldSeconds
        ActiveMods = $activeModIds
        LoadedMods = $loadedModIds
        ExpectedLogMarkers = @($validatedLogMarkers)
        ExpectedIntegrationTests = @($validatedExpectedIntegrationTestSpecs)
        ProductPackageEvidence = @($productPackageEvidence)
        BaseUrl = $baseUrl
        ProgramState = $status.result.programState
        UnrestrictedExecutionEnabled = $status.result.unrestrictedExecutionEnabled
        UnauthorizedStatusCode = [int]$unauthorized.StatusCode
        FlaUiWindowCount = $flaUiDesktopEvidence.WindowCount
        FlaUiEvidence = if ($runGatewayRegressionScenario) { $flaUiEvidencePath } else { $null }
        ModsConfig = $modsConfigPath
        Prefs = $prefsPath
        RunInBackground = $true
        MusicVolume = 0
        LaunchWindowStyle = $launchWindowStyle
        VisibleWindow = $launchVisible
        VisibleWindowRequested = [bool]$VisibleWindow
        Manifest = $manifestPath
        PlayerLog = $playerLogPath
        StatusResponse = $statusPath
        DefExportResponse = $defExportPath
        IntegrationTestsResponse = $integrationTestsPath
        IntegrationTestsPendingResponse = if ($RunIntegrationTests) { $integrationTestsPendingPath } else { $null }
        IntegrationTestsPersisted = $integrationTestsPersistedPath
        RunIntegrationTests = [bool]$RunIntegrationTests
        IntegrationFailureProbe = [bool]$IntegrationFailureProbe
        UiStateResponse = $uiStatePath
        LogsResponse = $logsPath
        PostQuickstartStatusResponse = if ($runGatewayRegressionScenario) { $postQuickstartStatusPath } else { $null }
        PostQuickstartUiStateResponse = if ($runGatewayRegressionScenario) { $postQuickstartUiStatePath } else { $null }
        PostQuickstartLogsResponse = if ($runGatewayRegressionScenario) { $postQuickstartLogsPath } else { $null }
        ActionsResponse = $actionsPath
        AutomationsResponse = $automationsPath
        GatewayScreenshot = $gatewayScreenshotPath
        ClickResponse = if ($runGatewayRegressionScenario) { $clickPath } else { $null }
        ClickOutcome = $clickOutcome
        ExecutionResponse = if ($runGatewayRegressionScenario) { $executionPath } else { $null }
        ExecutionStateResponse = $executionStatePath
        ExecutionRestoreResponse = if ($runGatewayRegressionScenario) { $executionRestorePath } else { $null }
        Quicktest = [bool]$Quicktest
        Scenario = [string]$scenarioPlan.Name
        ScenarioDescriptor = if ($null -ne $scenarioPlan.Descriptor) { [string]$scenarioPlan.DescriptorPath } else { $null }
        ScenarioResult = if ($null -ne $scenarioPlan.Descriptor) { $scenarioResultPath } else { $null }
        RunsGatewayRegressionScenario = $runGatewayRegressionScenario
        QuickstartDescriptor = if ($runGatewayRegressionScenario) { $resolvedQuickstartDescriptorPath } else { $null }
        QuickstartResponse = if ($runGatewayRegressionScenario) { $quickstartPath } else { $null }
        QuickstartReplayResponse = if ($runGatewayRegressionScenario) { $quickstartReplayPath } else { $null }
        QuickstartRunId = if ($runGatewayRegressionScenario) { [string]$quickstartRun.RunId } else { $null }
        GameStateInitialResponse = if ($runGatewayRegressionScenario) { $gameStateInitialPath } else { $null }
        GameStateEnableDevResponse = if ($runGatewayRegressionScenario) { $gameStateEnableDevPath } else { $null }
        GameStatePauseResponse = if ($runGatewayRegressionScenario) { $gameStatePausePath } else { $null }
        GameStateSpeedResponse = if ($runGatewayRegressionScenario) { $gameStateSpeedPath } else { $null }
        GameStateRestoreResponse = if ($runGatewayRegressionScenario) { $gameStateRestorePath } else { $null }
        ThingsMapResponse = if ($runGatewayRegressionScenario) { $thingsMapPath } else { $null }
        ThingsViewResponse = if ($runGatewayRegressionScenario) { $thingsViewPath } else { $null }
        ThingInspectBuildingResponse = if ($runGatewayRegressionScenario) { $thingInspectBuildingPath } else { $null }
        ThingInspectPawnResponse = if ($runGatewayRegressionScenario) { $thingInspectPawnPath } else { $null }
        SelectionSetResponse = if ($runGatewayRegressionScenario) { $selectionSetPath } else { $null }
        SelectionQueryResponse = if ($runGatewayRegressionScenario) { $selectionQueryPath } else { $null }
        SelectionRestoreResponse = if ($runGatewayRegressionScenario) { $selectionRestorePath } else { $null }
        DirectSpawnResponse = if ($runGatewayRegressionScenario) { $directSpawnPath } else { $null }
        DirectSpawnReplayResponse = if ($runGatewayRegressionScenario) { $directSpawnReplayPath } else { $null }
        DirectSpawnQueryResponse = if ($runGatewayRegressionScenario) { $directSpawnQueryPath } else { $null }
        DirectSpawnCleanupResponse = if ($runGatewayRegressionScenario) { $directSpawnCleanupPath } else { $null }
        DirectSpawnCleanupQueryResponse = if ($runGatewayRegressionScenario) { $directSpawnCleanupQueryPath } else { $null }
        CameraBeforeResponse = if ($runGatewayRegressionScenario) { $cameraBeforePath } else { $null }
        CameraMoveResponse = if ($runGatewayRegressionScenario) { $cameraMovePath } else { $null }
        CameraAfterMoveStateResponse = if ($runGatewayRegressionScenario) { $cameraAfterMoveStatePath } else { $null }
        CameraZoomResponse = if ($runGatewayRegressionScenario) { $cameraZoomPath } else { $null }
        CameraAfterZoomStateResponse = if ($runGatewayRegressionScenario) { $cameraAfterZoomStatePath } else { $null }
        CameraRestoreResponse = if ($runGatewayRegressionScenario) { $cameraRestorePath } else { $null }
        CameraRestoredStateResponse = if ($runGatewayRegressionScenario) { $cameraRestoredStatePath } else { $null }
        CameraBeforeScreenshot = if ($runGatewayRegressionScenario) { $cameraBeforeScreenshotPath } else { $null }
        CameraAfterMoveScreenshot = if ($runGatewayRegressionScenario) { $cameraAfterMoveScreenshotPath } else { $null }
        CameraAfterZoomScreenshot = if ($runGatewayRegressionScenario) { $cameraAfterZoomScreenshotPath } else { $null }
        CameraRestoredScreenshot = if ($runGatewayRegressionScenario) { $cameraRestoredScreenshotPath } else { $null }
        DebugActionsQueryResponse = if ($runGatewayRegressionScenario) { $debugActionsQueryPath } else { $null }
        DebugActionInvokeResponse = if ($runGatewayRegressionScenario) { $debugActionInvokePath } else { $null }
        PawnGizmosQueryResponse = if ($runGatewayRegressionScenario) { $pawnGizmosQueryPath } else { $null }
        PawnGizmoToggleResponse = if ($runGatewayRegressionScenario) { $pawnGizmoTogglePath } else { $null }
        PawnGizmosRestoreQueryResponse = if ($runGatewayRegressionScenario) { $pawnGizmosRestoreQueryPath } else { $null }
        PawnGizmoRestoreResponse = if ($runGatewayRegressionScenario) { $pawnGizmoRestorePath } else { $null }
        ArchitectGizmosQueryResponse = if ($runGatewayRegressionScenario) { $architectGizmosQueryPath } else { $null }
        DragInvokeResponse = if ($runGatewayRegressionScenario) { $dragInvokePath } else { $null }
        InteractionCurrentResponse = if ($runGatewayRegressionScenario) { $interactionCurrentPath } else { $null }
        InteractionCancelResponse = if ($runGatewayRegressionScenario) { $interactionCancelPath } else { $null }
        DragReinvokeResponse = if ($runGatewayRegressionScenario) { $dragReinvokePath } else { $null }
        DragApplyResponse = if ($runGatewayRegressionScenario) { $dragApplyPath } else { $null }
        PlanCreatedResponse = if ($runGatewayRegressionScenario) { $planCreatedPath } else { $null }
        DragRemoveInvokeResponse = if ($runGatewayRegressionScenario) { $dragRemoveInvokePath } else { $null }
        DragRemoveApplyResponse = if ($runGatewayRegressionScenario) { $dragRemoveApplyPath } else { $null }
        InteractionFinalResponse = if ($runGatewayRegressionScenario) { $interactionFinalPath } else { $null }
        PlanCleanupResponse = if ($runGatewayRegressionScenario) { $planCleanupPath } else { $null }
        CorrelatedSemanticRequestIds = if ($runGatewayRegressionScenario) { $semanticRequestIds } else { @() }
        ShutdownResponse = $shutdownPath
        SessionTombstone = $tombstonePath
        HostRequestJournal = $hostRequestJournalPath
        GatewayRequestJournal = $gatewayRequestJournalPath
        GatewayLastRequest = $gatewayLastRequestPath
        FailureDiagnostics = $failureDiagnosticsPath
        ProcessCleanup = $processCleanupPath
        Screenshot = $flaUiDesktopEvidence.Screenshot
        BuildLog = $buildLogPath
        IntegrationTestBundleLog = if ($RunIntegrationTests) { $integrationBundleLogPath } else { $null }
        IntegrationTestBundle = if ($RunIntegrationTests) { $integrationBundleEvidencePath } else { $null }
        IntegrationTestStageCleaned = $false
        RequiredAssemblies = $requiredAssemblies
        RequiredAssemblyEvidence = @($requiredAssemblyEvidence)
        NormalConfigHashBefore = $normalConfigHashBefore
        NormalConfigHashAfter = $null
        NormalPrefsHashBefore = $normalPrefsHashBefore
        NormalPrefsHashAfter = $null
    }
}
catch {
    $failureMessage = $_.Exception.Message
    Add-GatewaySmokeFailure `
        -Failures $failureRecords `
        -Category 'primary' `
        -Message $failureMessage
    $processAlive = $false
    $processExitCode = $null
    if ($null -ne $launchedProcess) {
        $launchedProcess.Refresh()
        $processAlive = -not $launchedProcess.HasExited
        if (-not $processAlive) {
            $processExitCode = $launchedProcess.ExitCode
        }
    }

    $classification = if ($null -eq $launchedProcess) {
        'startup-or-build-failure'
    }
    elseif (-not $processAlive) {
        'process-exited'
    }
    elseif ($null -ne $script:lastHostRequestRecord -and
        [string]$script:lastHostRequestRecord.State -eq 'timed-out') {
        'suspected-main-thread-hang'
    }
    else {
        'verification-failure-process-alive'
    }
    if ($null -ne $manifest -and -not [string]::IsNullOrWhiteSpace([string]$manifest.runId)) {
        $gatewayRequestJournalPath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\requests.jsonl"
        $gatewayLastRequestPath = Join-Path $savedDataPath "DevGateway\Sessions\$($manifest.runId)\last-request.json"
    }

    try {
        [pscustomobject]@{
            Classification = $classification
            FailureUtc = [datetime]::UtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
            Message = $failureMessage
            ProcessId = if ($null -ne $launchedProcess) { $launchedProcess.Id } else { $null }
            ProcessStartUtc = if ($null -ne $launchedProcessStartUtc) {
                $launchedProcessStartUtc.ToUniversalTime().ToString(
                    'O',
                    [Globalization.CultureInfo]::InvariantCulture)
            }
            else {
                $null
            }
            ProcessAlive = $processAlive
            ExitCode = $processExitCode
            LastHostRequest = $script:lastHostRequestRecord
            GatewayRequestJournal = $gatewayRequestJournalPath
            GatewayLastRequest = $gatewayLastRequestPath
            PlayerLog = $playerLogPath
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $failureDiagnosticsPath -Encoding UTF8
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $failureRecords `
            -Category 'primary' `
            -Message "Failure-diagnostics persistence failed: $($_.Exception.Message)"
    }
}
finally {
    if ($flaUiConnectionAttempted) {
        Invoke-GatewaySmokeBestEffortCleanupAction `
            -Failures $failureRecords `
            -Category 'desktop-cleanup' `
            -Description 'FlaUI disconnected-state reconciliation' `
            -Operation {
                $null = Complete-GatewaySmokeFlaUiConnectionCleanup -JsonInvoker {
                    param([string[]]$Arguments)
                    Invoke-FlaUiJson -Arguments $Arguments
                }
            }
    }

    if ($flaUiServiceStartAttempted -and -not $flaUiServiceWasRunning) {
        Invoke-GatewaySmokeBestEffortCleanupAction `
            -Failures $failureRecords `
            -Category 'desktop-cleanup' `
            -Description 'FlaUI stopped-state reconciliation' `
            -Operation {
                $null = Complete-GatewaySmokeFlaUiServiceCleanup `
                    -RawInvoker {
                        param([string[]]$Arguments)
                        Invoke-GatewaySmokeFlaUiCommand -Arguments $Arguments
                    } `
                    -JsonInvoker {
                        param([string[]]$Arguments)
                        Invoke-FlaUiJson -Arguments $Arguments
                    }
            }
    }

    $processCleanupArtifact = [pscustomobject]@{
        Status = 'not-started'
        Result = $null
        Error = $null
    }
    $processConfirmedExited = $false
    if ($null -ne $launchedProcess) {
        try {
            $processCleanup = Stop-OwnedProcessGracefully `
                -Process $launchedProcess `
                -DumpPath $hangDumpPath
            $processCleanupStatus = 'completed'
            $processCleanupArtifact.Status = $processCleanupStatus
            $processCleanupArtifact.Result = $processCleanup
        }
        catch {
            $processCleanupStatus = 'failed'
            $processCleanupArtifact.Status = $processCleanupStatus
            $processCleanupArtifact.Error = $_.Exception.Message
            Add-GatewaySmokeFailure `
                -Failures $failureRecords `
                -Category 'process-cleanup' `
                -Message "Owned RimWorld process cleanup failed: $($_.Exception.Message)"
        }

        try {
            $launchedProcess.Refresh()
            $processConfirmedExited = $launchedProcess.HasExited
        }
        catch {
            $processCleanupStatus = 'failed'
            $processCleanupArtifact.Status = $processCleanupStatus
            $processCleanupArtifact.Error = $_.Exception.Message
            Add-GatewaySmokeFailure `
                -Failures $failureRecords `
                -Category 'process-cleanup' `
                -Message "Owned RimWorld process exit confirmation failed: $($_.Exception.Message)"
        }
    }
    try {
        $processCleanupArtifact |
            ConvertTo-Json -Depth 8 |
            Set-Content -LiteralPath $processCleanupPath -Encoding UTF8
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $failureRecords `
            -Category 'process-cleanup' `
            -Message "Process-cleanup status persistence failed: $($_.Exception.Message)"
    }

    $credentialCleanupArtifact = [pscustomobject]@{
        Status = 'not-started'
        Result = $null
        Error = $null
    }
    if ($null -ne $launchedProcess) {
        if ($processConfirmedExited) {
            try {
                $credentialCleanupParameters = @{
                    RunDirectory = $runDirectory
                    SavedDataPath = $savedDataPath
                    ExpectedProcessId = $launchedProcess.Id
                    ExpectedRunId = if ($null -ne $manifest) { [string]$manifest.runId } else { $null }
                    BearerToken = if ($null -ne $manifest) { [string]$manifest.token } else { $null }
                    ProcessHasExited = $true
                }
                $credentialCleanup = Protect-GatewayCredentialArtifacts @credentialCleanupParameters
                $credentialCleanupStatus = 'completed'
                $credentialCleanupArtifact.Status = $credentialCleanupStatus
                $credentialCleanupArtifact.Result = $credentialCleanup
                $credentialsSanitized = $true
            }
            catch {
                $credentialCleanupStatus = 'failed'
                $credentialCleanupArtifact.Status = $credentialCleanupStatus
                $credentialCleanupArtifact.Error = $_.Exception.Message
                Add-GatewaySmokeFailure `
                    -Failures $failureRecords `
                    -Category 'credential-cleanup' `
                    -Message "Gateway credential cleanup failed: $($_.Exception.Message)"
            }
        }
        else {
            $credentialCleanupStatus = 'blocked-process-alive'
            $credentialCleanupArtifact.Status = $credentialCleanupStatus
            $credentialCleanupArtifact.Error =
                "Owned RimWorld PID $($launchedProcess.Id) was not confirmed dead."
            Add-GatewaySmokeFailure `
                -Failures $failureRecords `
                -Category 'credential-cleanup' `
                -Message "Gateway credentials were not sanitized because owned RimWorld PID $($launchedProcess.Id) was not confirmed dead."
        }
    }
    try {
        $credentialCleanupArtifact |
            ConvertTo-Json -Depth 8 |
            Set-Content -LiteralPath $credentialCleanupPath -Encoding UTF8
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $failureRecords `
            -Category 'credential-cleanup' `
            -Message "Credential-cleanup status persistence failed: $($_.Exception.Message)"
    }

    $stageCleanupDetails = [System.Collections.Generic.List[object]]::new()
    if ($RunIntegrationTests) {
        $integrationTestsStageCleaned = $true
        $integrationStageCleanupStatus = if ($integrationTestStageRecords.Count -eq 0) {
            'not-staged'
        }
        else {
            'completed'
        }
        foreach ($stageRecord in $integrationTestStageRecords) {
            if (-not (Test-Path -LiteralPath $stageRecord.StagePath)) {
                $stageCleanupDetails.Add([pscustomobject]@{
                    OwnerPackageId = [string]$stageRecord.OwnerPackageId
                    StagePath = [string]$stageRecord.StagePath
                    Status = 'already-absent'
                })
                continue
            }

            try {
                $ownedStagePath = Assert-OwnedIntegrationTestStage `
                    -StagePath ([string]$stageRecord.StagePath) `
                    -ModsRoot (Join-Path $resolvedRimWorldPath 'Mods') `
                    -OwnerPackageId ([string]$stageRecord.OwnerPackageId) `
                    -Version '1.6'
                Remove-Item -LiteralPath $ownedStagePath -Recurse -Force
                if (Test-Path -LiteralPath $ownedStagePath) {
                    throw "Owned integration-test stage remained after cleanup: $ownedStagePath"
                }
                $stageCleanupDetails.Add([pscustomobject]@{
                    OwnerPackageId = [string]$stageRecord.OwnerPackageId
                    StagePath = [string]$stageRecord.StagePath
                    Status = 'removed'
                })
            }
            catch {
                $integrationTestsStageCleaned = $false
                $integrationStageCleanupStatus = 'failed'
                $stageCleanupDetails.Add([pscustomobject]@{
                    OwnerPackageId = [string]$stageRecord.OwnerPackageId
                    StagePath = [string]$stageRecord.StagePath
                    Status = 'failed'
                    Error = $_.Exception.Message
                })
                Add-GatewaySmokeFailure `
                    -Failures $failureRecords `
                    -Category 'stage-cleanup' `
                    -Message "Integration-test stage cleanup failed: $($_.Exception.Message)"
            }
        }
    }
    try {
        [pscustomobject]@{
            Status = $integrationStageCleanupStatus
            Requested = [bool]$RunIntegrationTests
            Cleaned = [bool]$integrationTestsStageCleaned
            Stages = @($stageCleanupDetails)
        } | ConvertTo-Json -Depth 8 |
            Set-Content -LiteralPath $integrationStageCleanupPath -Encoding UTF8
    }
    catch {
        Add-GatewaySmokeFailure `
            -Failures $failureRecords `
            -Category 'stage-cleanup' `
            -Message "Integration-test stage-cleanup status persistence failed: $($_.Exception.Message)"
    }
}

$finalBearerToken = if ($null -ne $manifest) { [string]$manifest.token } else { '' }
try {
    Assert-GatewayFinalPlayerLog `
        -Path $playerLogPath `
        -BearerToken $finalBearerToken
}
catch {
    Add-GatewaySmokeFailure `
        -Failures $failureRecords `
        -Category 'primary' `
        -Message "Final Player.log verification failed: $($_.Exception.Message)"
}

$normalConfigHashRead = Get-GatewaySmokeOptionalHashSafely `
    -Path $normalModsConfigPath `
    -Failures $failureRecords
$normalConfigHashAfter = Assert-GatewaySmokeUnchangedHash `
    -DisplayName 'Normal RimWorld ModsConfig.xml' `
    -BeforeHash $normalConfigHashBefore `
    -ReadResult $normalConfigHashRead `
    -Failures $failureRecords

$normalPrefsHashRead = Get-GatewaySmokeOptionalHashSafely `
    -Path $normalPrefsPath `
    -DisplayName 'Normal RimWorld Prefs.xml' `
    -Failures $failureRecords
$normalPrefsHashAfter = Assert-GatewaySmokeUnchangedHash `
    -DisplayName 'Normal RimWorld Prefs.xml' `
    -BeforeHash $normalPrefsHashBefore `
    -ReadResult $normalPrefsHashRead `
    -Failures $failureRecords

$cleanupStatus = $null
try {
    $cleanupStatus = New-GatewaySmokeCleanupStatus `
        -ProcessStatus $processCleanupStatus `
        -CredentialStatus $credentialCleanupStatus `
        -StageStatus $integrationStageCleanupStatus `
        -Failures $failureRecords
}
catch {
    Add-GatewaySmokeFailure `
        -Failures $failureRecords `
        -Category 'primary' `
        -Message "Aggregate cleanup-status construction failed: $($_.Exception.Message)"
    $cleanupStatus = [pscustomobject]@{
        Status = 'failed'
        CompletedUtc = [datetime]::UtcNow.ToString(
            'O',
            [Globalization.CultureInfo]::InvariantCulture)
        Process = [pscustomobject]@{ Status = $processCleanupStatus }
        Credentials = [pscustomobject]@{ Status = $credentialCleanupStatus }
        IntegrationTestStage = [pscustomobject]@{ Status = $integrationStageCleanupStatus }
        Failures = @($failureRecords)
    }
}
try {
    $cleanupStatus |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $cleanupStatusPath -Encoding UTF8
}
catch {
    Add-GatewaySmokeFailure `
        -Failures $failureRecords `
        -Category 'primary' `
        -Message "Aggregate cleanup-status persistence failed: $($_.Exception.Message)"
}

if ($failureRecords.Count -ne 0) {
    foreach ($failure in $failureRecords) {
        [Console]::Error.WriteLine("[$($failure.Category)] $($failure.Message)")
    }
    [Console]::Error.WriteLine("Gateway smoke artifacts: $runDirectory")
    exit 1
}

$result = Set-GatewaySmokeCompletionMetadata `
    -Result $result `
    -NormalConfigHashAfter $normalConfigHashAfter `
    -NormalPrefsHashAfter $normalPrefsHashAfter `
    -IntegrationTestStageCleaned $integrationTestsStageCleaned `
    -CredentialsSanitized $credentialsSanitized `
    -CredentialCleanup $credentialCleanupPath `
    -IntegrationTestStageCleanup $integrationStageCleanupPath `
    -CleanupStatus $cleanupStatusPath
$result | Add-Member `
    -MemberType NoteProperty `
    -Name 'EvidenceSummary' `
    -Value $evidenceSummaryPath `
    -Force
try {
    $summaryBearerToken = if ($null -ne $manifest) { [string]$manifest.token } else { '' }
    Write-GatewaySmokeEvidenceSummary `
        -Result $result `
        -Path $evidenceSummaryPath `
        -BearerToken $summaryBearerToken
}
catch {
    [Console]::Error.WriteLine("[primary] Final evidence-summary persistence failed: $($_.Exception.Message)")
    [Console]::Error.WriteLine("Gateway smoke artifacts: $runDirectory")
    exit 1
}
Write-Result $result
exit 0
