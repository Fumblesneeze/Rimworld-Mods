<#
.SYNOPSIS
Runs the final player-workflow smoke from one exact subscribed Immersive Chefs Workshop directory.

.DESCRIPTION
Temporarily removes any repository-local product copy, leaves the subscribed package read-only,
and runs a Gateway-owned dynamic E2E test. The test verifies the exact loaded mod root, invokes
RimWorld's native Prioritize and Consume float-menu callbacks, and visibly observes plated cooking,
dining, and dirty service-ware return. The original local-mod presence or absence is restored in guaranteed cleanup.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[1-9][0-9]{5,19}$')][string]$PublishedFileId,
    [Parameter(Mandatory)][string]$ExpectedInstallPath,
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',
    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',
    [string]$ArtifactsPath,
    [ValidateRange(120, 900)][int]$TimeoutSeconds = 420,
    [ValidateSet('table', 'json')][string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-Json([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resolvedGame = [IO.Path]::GetFullPath($RimWorldPath).TrimEnd('\')
$resolvedWorkshop = [IO.Path]::GetFullPath($SteamModContentFolder).TrimEnd('\')
$expectedInstall = [IO.Path]::GetFullPath($ExpectedInstallPath).TrimEnd('\')
$expectedDirectInstall = Join-Path $resolvedWorkshop $PublishedFileId
if (-not [string]::Equals($expectedInstall, $expectedDirectInstall, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $expectedInstall -PathType Container)) {
    throw 'The subscribed package is not the exact declared direct Workshop item directory.'
}
$identityPath = Join-Path $expectedInstall 'About\PublishedFileId.txt'
if (-not (Test-Path -LiteralPath $identityPath -PathType Leaf) -or
    (Get-Content -LiteralPath $identityPath -Raw).Trim() -cne $PublishedFileId) {
    throw 'The subscribed package identity file is missing or mismatched.'
}

$runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = Join-Path $repositoryRoot "artifacts\Releases\fumblesneeze.immersivechefs\subscribed-smoke\$runId"
}
$runRoot = [IO.Path]::GetFullPath($ArtifactsPath)
if (Test-Path -LiteralPath $runRoot) { throw "Subscribed-smoke output already exists: $runRoot" }

$localProduct = Join-Path $resolvedGame 'Mods\fumblesneeze.immersivechefs'
$backupProduct = Join-Path $resolvedGame ".immersive-chefs-subscribed-smoke-$runId"
$localProductInitiallyPresent = Test-Path -LiteralPath $localProduct -PathType Container
$movedLocalProduct = $false
$priorExpectedRoot = [Environment]::GetEnvironmentVariable('IMMERSIVE_CHEFS_RELEASE_EXPECTED_ROOT', 'Process')
$primaryError = $null
$cleanupFailures = [System.Collections.Generic.List[string]]::new()

try {
    if ($localProductInitiallyPresent) {
        if (Test-Path -LiteralPath $backupProduct) { throw 'The exact local-mod backup path already exists.' }
        [xml]$localAbout = Get-Content -LiteralPath (Join-Path $localProduct 'About\About.xml') -Raw
        if ([string]$localAbout.ModMetaData.packageId -cne 'fumblesneeze.immersivechefs') {
            throw 'The local product directory does not declare the expected package ID.'
        }
        Move-Item -LiteralPath $localProduct -Destination $backupProduct
        $movedLocalProduct = $true
    }

    [Environment]::SetEnvironmentVariable('IMMERSIVE_CHEFS_RELEASE_EXPECTED_ROOT', $expectedInstall, 'Process')
    $runner = Join-Path $repositoryRoot 'scripts\Invoke-RimWorldEndToEndTests.ps1'
    $releaseSmokeProject = Join-Path $repositoryRoot 'tests\RimWorldDevGateway.ReleaseSmoke.EndToEndTests\RimWorldDevGateway.ReleaseSmoke.EndToEndTests.csproj'
    $runnerOutput = @(& $runner `
        -RimWorldPath $resolvedGame `
        -SteamModContentFolder $resolvedWorkshop `
        -TestId 'release.immersive-chefs-subscribed-native-cooking-dining' `
        -ProjectPath $releaseSmokeProject `
        -ArtifactsPath $runRoot `
        -TimeoutSeconds ([Math]::Min(600, $TimeoutSeconds)) `
        -Output json 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Subscribed Workshop E2E runner failed: $($runnerOutput -join [Environment]::NewLine)"
    }
    try { $runnerResult = ($runnerOutput -join [Environment]::NewLine).Trim() | ConvertFrom-Json }
    catch { throw "Subscribed Workshop E2E runner returned invalid JSON: $($runnerOutput -join [Environment]::NewLine)" }
    if ([string]$runnerResult.Status -cne 'passed' -or [int]$runnerResult.CompletedGroupCount -ne 1) {
        throw 'The subscribed Workshop E2E group did not pass exactly once.'
    }

    $evidenceFiles = @(Get-ChildItem -LiteralPath $runRoot -Recurse -Filter end-to-end-tests.json -File)
    if ($evidenceFiles.Count -ne 1) { throw 'The subscribed Workshop E2E evidence is missing or ambiguous.' }
    $evidence = Read-Json $evidenceFiles[0].FullName
    $testResult = @($evidence.result.Execution.Results | Where-Object {
        [string]$_.Id -ceq 'release.immersive-chefs-subscribed-native-cooking-dining'
    })
    if ($testResult.Count -ne 1 -or [string]$testResult[0].Status -cne 'passed' -or
        [string]$testResult[0].CleanupState -cne 'passed') {
        throw 'The exact subscribed Workshop native cooking/dining result did not pass and clean up.'
    }
    $checkpoint = @($testResult[0].Steps | Where-Object {
        [string]$_.Name -ceq 'subscribed Workshop native cooking and dining result'
    })
    if ($checkpoint.Count -ne 1) { throw 'The subscribed player-workflow checkpoint is missing.' }
    $loadedRoot = [string]$checkpoint[0].Artifacts.loadedRoot
    if (-not [string]::Equals([IO.Path]::GetFullPath($loadedRoot).TrimEnd('\'), $expectedInstall, [StringComparison]::OrdinalIgnoreCase)) {
        throw "RimWorld loaded Immersive Chefs from '$loadedRoot', not the subscribed Workshop directory."
    }
    $screenshots = @(Get-ChildItem -LiteralPath $evidenceFiles[0].Directory.FullName -Filter 'e2e-screenshot-*.png' -File |
        Sort-Object Name | ForEach-Object { $_.FullName })
    if ($screenshots.Count -lt 3) { throw 'The subscribed player workflow did not retain before/action/after screenshots.' }

    $result = [pscustomobject][ordered]@{
        status = 'passed'
        publishedFileId = $PublishedFileId
        loadedPackagePath = $loadedRoot
        playerAction = 'native Prioritize and Consume float-menu callbacks'
        observedResult = 'one plated simple meal cooked and eaten, with plate, cutlery, and cookware returned dirty'
        screenshots = $screenshots
        gatewayEvidence = $evidenceFiles[0].Directory.FullName
        localProductRestored = $false
    }
}
catch { $primaryError = $_ }
finally {
    [Environment]::SetEnvironmentVariable('IMMERSIVE_CHEFS_RELEASE_EXPECTED_ROOT', $priorExpectedRoot, 'Process')
    if ($movedLocalProduct) {
        try {
            if (Test-Path -LiteralPath $localProduct) { throw 'The local product path reappeared before restoration.' }
            Move-Item -LiteralPath $backupProduct -Destination $localProduct
        }
        catch { $cleanupFailures.Add("Local product restoration failed: $($_.Exception.Message)") }
    }
}

if ($null -ne $primaryError) {
    if ($cleanupFailures.Count -ne 0) { throw "$($primaryError.Exception.Message) Cleanup also failed: $($cleanupFailures -join '; ')" }
    throw $primaryError
}
if ($cleanupFailures.Count -ne 0) { throw ($cleanupFailures -join '; ') }
$localProductFinallyPresent = Test-Path -LiteralPath $localProduct -PathType Container
$result.localProductRestored = $localProductFinallyPresent -eq $localProductInitiallyPresent -and
    -not (Test-Path -LiteralPath $backupProduct)
if (-not $result.localProductRestored) { throw 'The local product presence/absence was not restored exactly.' }
if ($Output -eq 'json') { $result | ConvertTo-Json -Depth 8 -Compress } else { $result | Format-List }
