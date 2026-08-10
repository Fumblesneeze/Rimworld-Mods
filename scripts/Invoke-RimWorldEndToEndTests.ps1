<#
.SYNOPSIS
Runs dynamically loaded RimWorld E2E tests once per exact active-mod group.

.DESCRIPTION
Discovers and builds marked E2E projects through the Gateway host tool, groups tests by their
complete active package set, deploys repo-owned product mods before staging tests, launches the
existing isolated Gateway smoke harness once per selected group, and writes aggregate JSON plus
JUnit. Test stages are marker/lease-owned and are cleaned in a finally block.

-DryRun builds/discovers test assemblies and prints the deterministic launch plan without changing
the installed game, staging a bundle, writing run artifacts, or launching RimWorld.

Exit codes: 0 all selected groups passed, 1 execution/infrastructure failure, 2 invalid input.

.EXAMPLE
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -DryRun -Output json

.EXAMPLE
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -GroupId ludeon.rimworld -Output table

.EXAMPLE
.\scripts\Invoke-RimWorldEndToEndTests.ps1 -TestId immersive-chefs.countertop-microwave-support-loss -Output table
#>
[CmdletBinding()]
param(
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [string[]]$AvailableModIds = @(),

    [string[]]$GroupId = @(),

    [string[]]$TestId = @(),

    [string]$ArtifactsPath,

    [ValidatePattern('^[A-Za-z][A-Za-z0-9]{0,63}$')]
    [string]$Language = 'English',

    [ValidateRange(30, 600)]
    [int]$TimeoutSeconds = 300,

    [switch]$DryRun,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RimWorldEndToEndRunner.Support.psm1') -Force
$testIdFilters = @($TestId)

function Exit-InvalidInput {
    param([Parameter(Mandatory)][string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

function Write-RunnerResult {
    param([Parameter(Mandatory)][object]$Value)

    if ($Output -eq 'json') {
        $Value | ConvertTo-Json -Depth 16 -Compress
        return
    }

    $Value | Format-List
}

function Test-PackageId {
    param([AllowEmptyString()][string]$Value)

    return -not [string]::IsNullOrWhiteSpace($Value) -and
        $Value.Length -le 256 -and
        $Value -cmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$'
}

function Get-ProjectProperty {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$PropertyName
    )

    try {
        [xml]$document = Get-Content -LiteralPath $ProjectPath -Raw -ErrorAction Stop
    }
    catch {
        throw "Could not parse project '$ProjectPath': $($_.Exception.Message)"
    }

    $values = @($document.SelectNodes("//*[local-name()='$PropertyName']") |
        ForEach-Object { [string]$_.InnerText } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() } |
        Select-Object -Unique)
    if ($values.Count -eq 0) {
        return $null
    }
    if ($values.Count -ne 1) {
        throw "Project '$ProjectPath' declares multiple $PropertyName values."
    }

    return [string]$values[0]
}

function Get-RepoProductProjects {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $result = @{}
    $modsDirectory = Join-Path $RepositoryRoot 'mods'
    foreach ($project in @(Get-ChildItem -LiteralPath $modsDirectory -Recurse -File -Filter '*.csproj')) {
        $packageId = Get-ProjectProperty -ProjectPath $project.FullName -PropertyName 'RimWorldPackageId'
        if ([string]::IsNullOrWhiteSpace($packageId)) {
            continue
        }
        if (-not (Test-PackageId -Value $packageId)) {
            throw "Repo product project has an invalid RimWorldPackageId '$packageId': $($project.FullName)"
        }

        $key = $packageId.ToLowerInvariant()
        if ($result.ContainsKey($key)) {
            throw "Multiple repo product projects declare package '$key'."
        }
        $result[$key] = $project.FullName
    }

    return $result
}

function Get-InstalledPackageIds {
    param(
        [Parameter(Mandatory)][string]$GamePath,
        [Parameter(Mandatory)][string]$WorkshopPath
    )

    $ids = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $null = $ids.Add('ludeon.rimworld')
    $roots = @(
        (Join-Path $GamePath 'Data'),
        (Join-Path $GamePath 'Mods'),
        $WorkshopPath
    )
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) {
            continue
        }

        foreach ($aboutFile in @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter 'About.xml' -ErrorAction SilentlyContinue)) {
            try {
                [xml]$about = Get-Content -LiteralPath $aboutFile.FullName -Raw -ErrorAction Stop
                $packageNodes = @($about.SelectNodes(
                    '/*[local-name()="ModMetaData"]/*[local-name()="packageId"]'))
                $packageId = if ($packageNodes.Count -eq 1) {
                    [string]$packageNodes[0].InnerText
                }
                else {
                    ''
                }
                if (Test-PackageId -Value $packageId.Trim()) {
                    $null = $ids.Add($packageId.Trim().ToLowerInvariant())
                }
            }
            catch {
                # Malformed unrelated downloaded mods are not resolvable for an exact E2E group.
            }
        }
    }

    return @($ids | Sort-Object)
}

function Invoke-BoundedProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Arguments,
        [Parameter(Mandatory)][ValidateRange(1000, 1800000)][int]$TimeoutMilliseconds,
        [Parameter(Mandatory)][string]$Description
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ExecutablePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        $null = $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Could not start $Description."
        }
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            try {
                $process.Kill($true)
            }
            catch {
                # Preserve the primary timeout outcome.
            }
            throw "$Description exceeded its $TimeoutMilliseconds ms host deadline."
        }

        $outputText = $standardOutput.GetAwaiter().GetResult()
        $errorText = $standardError.GetAwaiter().GetResult()
        return [pscustomobject]@{
            ExitCode = [int]$process.ExitCode
            StandardOutput = [string]$outputText
            StandardError = [string]$errorText
        }
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-HostTool {
    param(
        [Parameter(Mandatory)][string]$Operation,
        [Parameter(Mandatory)][string]$HostProject,
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$ModsRoot,
        [Parameter(Mandatory)][string[]]$ResolvablePackageIds,
        [AllowNull()][string]$LeaseFile
    )

    $packageIdFile = $null
    try {
        $arguments = [System.Collections.Generic.List[string]]::new()
        foreach ($argument in @('run', '--project', $HostProject, '--configuration', 'Release', '--', $Operation)) {
            $arguments.Add($argument)
        }
        if ($Operation -ne 'clean') {
            foreach ($argument in @(
                '--repository-root', $RepositoryRoot,
                '--mods-root', $ModsRoot,
                '--rimworld-version', '1.6')) {
                $arguments.Add($argument)
            }
            $packageIdFile = Join-Path `
                ([System.IO.Path]::GetTempPath()) `
                ("rimworld-e2e-packages-$([guid]::NewGuid().ToString('N')).txt")
            [System.IO.File]::WriteAllLines(
                $packageIdFile,
                $ResolvablePackageIds,
                [System.Text.UTF8Encoding]::new($false))
            $arguments.Add('--package-id-file')
            $arguments.Add($packageIdFile)
        }
        if (-not [string]::IsNullOrWhiteSpace($LeaseFile)) {
            $arguments.Add('--lease-file')
            $arguments.Add($LeaseFile)
        }
        $arguments.Add('--output')
        $arguments.Add('json')

        return Invoke-BoundedProcess `
            -ExecutablePath 'dotnet' `
            -Arguments @($arguments) `
            -TimeoutMilliseconds 300000 `
            -Description "E2E host $Operation"
    }
    finally {
        if (-not [string]::IsNullOrWhiteSpace($packageIdFile) -and
            (Test-Path -LiteralPath $packageIdFile -PathType Leaf)) {
            Remove-Item -LiteralPath $packageIdFile -Force
        }
    }
}

function ConvertFrom-RequiredJson {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Description
    )

    try {
        return $Text | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "$Description returned invalid JSON: $($_.Exception.Message)"
    }
}

function Get-SafeGroupDirectoryName {
    param(
        [Parameter(Mandatory)][string]$GroupId,
        [Parameter(Mandatory)][int]$Index
    )

    $safe = [regex]::Replace($GroupId.ToLowerInvariant(), '[^a-z0-9._-]', '_')
    if ($safe.Length -gt 80) {
        $safe = $safe.Substring(0, 80)
    }
    return '{0:D3}-{1}' -f $Index, $safe
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
$managedPath = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed'
$modsRoot = Join-Path $resolvedRimWorldPath 'Mods'
if (-not (Test-Path -LiteralPath $rimWorldExecutable -PathType Leaf)) {
    Exit-InvalidInput "RimWorld executable does not exist: $rimWorldExecutable"
}
if (-not (Test-Path -LiteralPath $managedPath -PathType Container)) {
    Exit-InvalidInput "RimWorld managed assembly path does not exist: $managedPath"
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$hostProject = Join-Path $repositoryRoot 'tools\RimWorldDevGateway.EndToEndHost\RimWorldDevGateway.EndToEndHost.csproj'
$smokeScript = Join-Path $repositoryRoot 'scripts\Invoke-GatewaySmoke.ps1'
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = Join-Path $repositoryRoot 'artifacts\EndToEndRuns\Grouped'
}
$artifactRoot = [System.IO.Path]::GetFullPath($ArtifactsPath)

$packageIds = if ($AvailableModIds.Count -eq 0) {
    @(Get-InstalledPackageIds -GamePath $resolvedRimWorldPath -WorkshopPath $resolvedWorkshopPath)
}
else {
    @($AvailableModIds | ForEach-Object { $_.Trim().ToLowerInvariant() } | Sort-Object -Unique)
}
if ($packageIds.Count -eq 0 -or $packageIds -notcontains 'ludeon.rimworld') {
    Exit-InvalidInput 'Available mod IDs must include ludeon.rimworld.'
}
foreach ($packageId in $packageIds) {
    if (-not (Test-PackageId -Value $packageId)) {
        Exit-InvalidInput "Invalid available mod package ID: '$packageId'"
    }
}
foreach ($requestedTest in $testIdFilters) {
    if ([string]::IsNullOrWhiteSpace($requestedTest) -or
        $requestedTest.Length -gt 160 -or
        $requestedTest -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
        Exit-InvalidInput "Invalid E2E test ID: '$requestedTest'"
    }
}
if (@($testIdFilters | Select-Object -Unique).Count -ne $testIdFilters.Count) {
    Exit-InvalidInput 'The E2E test filter contains a duplicate test ID.'
}

$planInvocation = Invoke-HostTool `
    -Operation 'plan' `
    -HostProject $hostProject `
    -RepositoryRoot $repositoryRoot `
    -ModsRoot $modsRoot `
    -ResolvablePackageIds $packageIds `
    -LeaseFile $null
if ($planInvocation.ExitCode -ne 0) {
    [Console]::Error.WriteLine($planInvocation.StandardError.Trim())
    exit $(if ($planInvocation.ExitCode -eq 2) { 2 } else { 1 })
}
$plan = ConvertFrom-RequiredJson -Text $planInvocation.StandardOutput -Description 'E2E host plan'
$candidateGroups = @($plan.groups | Where-Object {
    $GroupId.Count -eq 0 -or $GroupId -contains [string]$_.groupId
})
if ($candidateGroups.Count -eq 0) {
    Exit-InvalidInput 'The E2E group filter selected zero groups.'
}
foreach ($requestedGroup in $GroupId) {
    if (@($candidateGroups | Where-Object { [string]$_.groupId -ceq $requestedGroup }).Count -ne 1) {
        Exit-InvalidInput "Unknown or duplicate E2E group ID: '$requestedGroup'"
    }
}
foreach ($requestedTest in $testIdFilters) {
    $matches = @($plan.groups | Where-Object { @($_.tests) -ccontains $requestedTest })
    if ($matches.Count -ne 1) {
        Exit-InvalidInput "Unknown or duplicate E2E test ID: '$requestedTest'"
    }
}
$selectedGroups = @(foreach ($group in $candidateGroups) {
    $selectedTests = @(if ($testIdFilters.Count -eq 0) {
        $group.tests
    }
    else {
        $group.tests | Where-Object { $testIdFilters -ccontains [string]$_ }
    })
    if ($selectedTests.Count -gt 0) {
        [pscustomobject]@{
            groupId = [string]$group.groupId
            activePackageIds = @($group.activePackageIds)
            tests = $selectedTests
        }
    }
})
if ($selectedGroups.Count -eq 0) {
    Exit-InvalidInput 'The combined E2E group and test filters selected zero tests.'
}

$launchPlans = [System.Collections.Generic.List[object]]::new()
$planIndex = 0
foreach ($group in $selectedGroups) {
    $planIndex++
    $additionalIds = @($group.activePackageIds | Where-Object {
        [string]$_ -ine 'ludeon.rimworld' -and [string]$_ -ine 'fumblesneeze.rimworlddevgateway'
    })
    $plannedGroupDirectory = Join-Path $artifactRoot (
        Get-SafeGroupDirectoryName -GroupId ([string]$group.groupId) -Index $planIndex)
    $plannedAdditionalIdsFile = if ($additionalIds.Count -gt 0) {
        Join-Path $plannedGroupDirectory 'additional-mod-ids.txt'
    }
    else {
        $null
    }
    $plannedCommand = @(
        'pwsh', '-NoProfile', '-NonInteractive', '-File', $smokeScript,
        '-Quicktest', '-RunEndToEndTests', '-SkipBuildDeploy',
        '-Language', $Language,
        '-TimeoutSeconds', [string]$TimeoutSeconds,
        '-Output', 'json'
    )
    if ($testIdFilters.Count -gt 0) {
        $plannedCommand += @('-EndToEndTestIds', (@($group.tests) -join ','))
    }
    if ($null -ne $plannedAdditionalIdsFile) {
        $plannedCommand += @('-AdditionalModIdsFile', $plannedAdditionalIdsFile)
    }

    $launchPlans.Add([pscustomobject]@{
        GroupId = [string]$group.groupId
        Language = $Language
        ActivePackageIds = @($group.activePackageIds) + @('fumblesneeze.rimworlddevgateway')
        Tests = @($group.tests)
        AdditionalModIds = $additionalIds
        AdditionalModIdsFile = $plannedAdditionalIdsFile
        ArtifactsPath = $plannedGroupDirectory
        Command = $plannedCommand
    })
}

if ($DryRun) {
    Write-RunnerResult ([pscustomobject]@{
        Status = 'dry-run'
        AvailablePackageCount = $packageIds.Count
        Groups = @($launchPlans)
        StageOwners = @($plan.owners)
        MutatedGame = $false
    })
    exit 0
}

$existingRimWorld = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
if ($existingRimWorld.Count -ne 0) {
    Exit-InvalidInput "RimWorld is already running (PID(s): $($existingRimWorld.Id -join ', '))."
}

$runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
$runDirectory = Join-Path $artifactRoot $runId
$null = New-Item -Path $runDirectory -ItemType Directory -Force
$leaseFile = Join-Path $runDirectory 'stage.lease.json'
$stageOutputPath = Join-Path $runDirectory 'stage.json'
$productDeploymentEvidencePath = Join-Path $runDirectory 'product-deployment-evidence.json'
$aggregatePath = Join-Path $runDirectory 'aggregate.json'
$junitPath = Join-Path $runDirectory 'results.junit.xml'
$groupResults = [System.Collections.Generic.List[object]]::new()
$productDeploymentEvidence = [System.Collections.Generic.List[object]]::new()
$stagePublished = $false
$stageCleaned = $false
$infrastructureFailure = $null

try {
    $productProjects = Get-RepoProductProjects -RepositoryRoot $repositoryRoot
    $ownerIds = @($plan.owners.ownerPackageId) + @('fumblesneeze.rimworlddevgateway') |
        ForEach-Object { ([string]$_).ToLowerInvariant() } |
        Sort-Object -Unique
    foreach ($ownerId in $ownerIds) {
        if (-not $productProjects.ContainsKey($ownerId)) {
            throw "No repo product project declares E2E owner package '$ownerId'."
        }

        $buildLogPath = Join-Path $runDirectory "deploy-$ownerId.log"
        $build = Invoke-BoundedProcess `
            -ExecutablePath 'dotnet' `
            -Arguments @(
                'build', [string]$productProjects[$ownerId],
                '--configuration', 'Release', '--nologo',
                '-p:DeployToGame=true',
                "-p:RimWorldPath=$resolvedRimWorldPath",
                "-p:RimWorldManagedPath=$managedPath",
                "-p:SteamModContentFolder=$resolvedWorkshopPath") `
            -TimeoutMilliseconds 300000 `
            -Description "deploy product mod $ownerId"
        [System.IO.File]::WriteAllText(
            $buildLogPath,
            $build.StandardOutput + $build.StandardError,
            [System.Text.UTF8Encoding]::new($false))
        if ($build.ExitCode -ne 0) {
            throw "Product mod '$ownerId' deployment failed. See $buildLogPath"
        }

        $productDeploymentEvidence.Add(
            (Get-RimWorldDeployedProductEvidence `
                -PackageId $ownerId `
                -ProjectPath ([string]$productProjects[$ownerId]) `
                -RimWorldPath $resolvedRimWorldPath `
                -BuildLogPath $buildLogPath))
    }

    [System.IO.File]::WriteAllText(
        $productDeploymentEvidencePath,
        (ConvertTo-RimWorldProductEvidenceJson -Evidence @($productDeploymentEvidence)),
        [System.Text.UTF8Encoding]::new($false))

    $stage = Invoke-HostTool `
        -Operation 'stage' `
        -HostProject $hostProject `
        -RepositoryRoot $repositoryRoot `
        -ModsRoot $modsRoot `
        -ResolvablePackageIds $packageIds `
        -LeaseFile $leaseFile
    [System.IO.File]::WriteAllText(
        $stageOutputPath,
        $stage.StandardOutput + $stage.StandardError,
        [System.Text.UTF8Encoding]::new($false))
    if ($stage.ExitCode -ne 0) {
        throw "E2E stage publication failed. See $stageOutputPath"
    }
    $stagePublished = $true

    $groupIndex = 0
    foreach ($group in $selectedGroups) {
        $groupIndex++
        $groupDirectory = Join-Path $runDirectory (
            Get-SafeGroupDirectoryName -GroupId ([string]$group.groupId) -Index $groupIndex)
        $null = New-Item -Path $groupDirectory -ItemType Directory -Force
        $groupArtifactRoot = Join-Path $runDirectory ('smoke-{0:D3}' -f $groupIndex)
        $stdoutPath = Join-Path $groupDirectory 'stdout.json'
        $stderrPath = Join-Path $groupDirectory 'stderr.txt'
        $additionalIds = @($group.activePackageIds | Where-Object {
            [string]$_ -ine 'ludeon.rimworld' -and [string]$_ -ine 'fumblesneeze.rimworlddevgateway'
        })
        $additionalModIdsPath = Join-Path $groupDirectory 'additional-mod-ids.txt'
        $smokeArguments = [System.Collections.Generic.List[string]]::new()
        foreach ($argument in @(
            '-NoProfile', '-NonInteractive', '-File', $smokeScript,
            '-Quicktest', '-RunEndToEndTests', '-SkipBuildDeploy',
            '-Language', $Language,
            '-TimeoutSeconds', [string]$TimeoutSeconds,
            '-ArtifactsPath', $groupArtifactRoot,
            '-Output', 'json')) {
            $smokeArguments.Add($argument)
        }
        if ($testIdFilters.Count -gt 0) {
            $smokeArguments.Add('-EndToEndTestIds')
            $smokeArguments.Add((@($group.tests) -join ','))
        }
        if ($additionalIds.Count -gt 0) {
            [System.IO.File]::WriteAllLines(
                $additionalModIdsPath,
                $additionalIds,
                [System.Text.UTF8Encoding]::new($false))
            $smokeArguments.Add('-AdditionalModIdsFile')
            $smokeArguments.Add($additionalModIdsPath)
        }

        try {
            $smoke = Invoke-BoundedProcess `
                -ExecutablePath 'pwsh' `
                -Arguments @($smokeArguments) `
                -TimeoutMilliseconds (($TimeoutSeconds + 180) * 1000) `
                -Description "RimWorld E2E group '$($group.groupId)'"
            [System.IO.File]::WriteAllText($stdoutPath, $smoke.StandardOutput, [System.Text.UTF8Encoding]::new($false))
            [System.IO.File]::WriteAllText($stderrPath, $smoke.StandardError, [System.Text.UTF8Encoding]::new($false))

            $smokeResult = $null
            $message = ''
            if ($smoke.ExitCode -eq 0) {
                try {
                    $smokeResult = ConvertFrom-RequiredJson `
                        -Text $smoke.StandardOutput `
                        -Description "RimWorld E2E group '$($group.groupId)'"
                }
                catch {
                    $message = $_.Exception.Message
                }
            }
            else {
                $message = "Smoke launcher exited $($smoke.ExitCode)."
            }

            $groupPassed = $smoke.ExitCode -eq 0 -and $null -ne $smokeResult -and
                [string]$smokeResult.Status -eq 'passed'
            $groupResults.Add([pscustomobject]@{
                GroupId = [string]$group.groupId
                Language = $Language
                ActivePackageIds = @($group.activePackageIds) + @('fumblesneeze.rimworlddevgateway')
                PlannedTests = @($group.tests)
                Status = if ($groupPassed) { 'passed' } else { 'failed' }
                Message = if ($groupPassed) { '' } elseif (-not [string]::IsNullOrWhiteSpace($message)) {
                    $message
                }
                else {
                    $smoke.StandardError.Trim()
                }
                ExitCode = $smoke.ExitCode
                StandardOutput = $stdoutPath
                StandardError = $stderrPath
                SmokeResult = $smokeResult
            })
        }
        catch {
            $groupFailure = $_.Exception.Message
            if (-not (Test-Path -LiteralPath $stdoutPath -PathType Leaf)) {
                [System.IO.File]::WriteAllText($stdoutPath, '', [System.Text.UTF8Encoding]::new($false))
            }
            [System.IO.File]::WriteAllText($stderrPath, $groupFailure, [System.Text.UTF8Encoding]::new($false))
            $groupResults.Add([pscustomobject]@{
                GroupId = [string]$group.groupId
                Language = $Language
                ActivePackageIds = @($group.activePackageIds) + @('fumblesneeze.rimworlddevgateway')
                PlannedTests = @($group.tests)
                Status = 'failed'
                Message = $groupFailure
                ExitCode = $null
                StandardOutput = $stdoutPath
                StandardError = $stderrPath
                SmokeResult = $null
            })
        }
    }
}
catch {
    $infrastructureFailure = $_.Exception.Message
}
finally {
    if ($stagePublished -and (Test-Path -LiteralPath $leaseFile -PathType Leaf)) {
        try {
            $clean = Invoke-HostTool `
                -Operation 'clean' `
                -HostProject $hostProject `
                -RepositoryRoot $repositoryRoot `
                -ModsRoot $modsRoot `
                -ResolvablePackageIds $packageIds `
                -LeaseFile $leaseFile
            $clean | ConvertTo-Json -Depth 6 |
                Set-Content -LiteralPath (Join-Path $runDirectory 'stage-cleanup.json') -Encoding UTF8
            $stageCleaned = $clean.ExitCode -eq 0 -and -not (Test-Path -LiteralPath $leaseFile)
            if (-not $stageCleaned -and [string]::IsNullOrWhiteSpace($infrastructureFailure)) {
                $infrastructureFailure = 'E2E stage cleanup failed.'
            }
        }
        catch {
            $stageCleaned = $false
            if ([string]::IsNullOrWhiteSpace($infrastructureFailure)) {
                $infrastructureFailure = "E2E stage cleanup failed: $($_.Exception.Message)"
            }
        }
    }
    elseif (-not $stagePublished) {
        $stageCleaned = $true
    }
}

$allGroupsPassed = $groupResults.Count -eq $selectedGroups.Count -and
    @($groupResults | Where-Object { [string]$_.Status -ne 'passed' }).Count -eq 0
$passed = $allGroupsPassed -and [string]::IsNullOrWhiteSpace($infrastructureFailure) -and $stageCleaned
$aggregate = [pscustomobject]@{
    Status = if ($passed) { 'passed' } else { 'failed' }
    RunId = $runId
    RunDirectory = $runDirectory
    Language = $Language
    AvailablePackageCount = $packageIds.Count
    PlannedGroupCount = $selectedGroups.Count
    CompletedGroupCount = $groupResults.Count
    StagePublished = $stagePublished
    StageCleaned = $stageCleaned
    InfrastructureFailure = $infrastructureFailure
    ProductDeploymentEvidence = @($productDeploymentEvidence)
    ProductDeploymentEvidencePath = $productDeploymentEvidencePath
    Groups = @($groupResults)
    Aggregate = $aggregatePath
    JUnit = $junitPath
}
$aggregate | ConvertTo-Json -Depth 16 |
    Set-Content -LiteralPath $aggregatePath -Encoding UTF8
Write-RimWorldEndToEndJUnitReport -Path $junitPath -GroupResults @($groupResults)

if (-not $passed) {
    [Console]::Error.WriteLine(
        $(if ([string]::IsNullOrWhiteSpace($infrastructureFailure)) {
            "One or more E2E mod groups failed. See $aggregatePath"
        }
        else {
            "$infrastructureFailure See $aggregatePath"
        }))
    exit 1
}

Write-RunnerResult ([pscustomobject]@{
    Status = $aggregate.Status
    RunId = $aggregate.RunId
    RunDirectory = $aggregate.RunDirectory
    Language = $aggregate.Language
    PlannedGroupCount = $aggregate.PlannedGroupCount
    CompletedGroupCount = $aggregate.CompletedGroupCount
    StageCleaned = $aggregate.StageCleaned
    ProductDeploymentEvidence = $aggregate.ProductDeploymentEvidencePath
    Groups = @($groupResults | Select-Object GroupId, Status, ExitCode)
    Aggregate = $aggregatePath
    JUnit = $junitPath
})
exit 0
