<#
.SYNOPSIS
Runs the final native Immersive Chefs smoke from one exact subscribed Workshop directory.

.DESCRIPTION
Temporarily moves the repository-local product mod outside RimWorld's Mods directory, launches a
fresh isolated minimized Gateway quicktest with the exact subscribed item, runs the checked-in
kitchenware-fabrication scenario, resumes its two vanilla DoBill jobs through the native speed
action, and captures the resulting primitive and modern kitchenware inspectors. The local mod is
restored in guaranteed cleanup. The Workshop directory is read-only.
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

function Invoke-GatewayText {
    param([string]$Uri, [string]$Token, [string]$RequestId, [string]$Source)
    $response = Invoke-WebRequest -Uri $Uri -Method Post -Headers @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    } -ContentType 'text/plain; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($Source)) `
      -TimeoutSec 60 -SkipHttpErrorCheck
    $envelope = [string]$response.Content | ConvertFrom-Json
    if ([int]$response.StatusCode -ne 200 -or -not [bool]$envelope.ok -or
        -not [bool]$envelope.result.Succeeded) {
        throw "Gateway C# observation failed ($RequestId): $($response.Content)"
    }
    return [string]$envelope.result.Value
}

function Invoke-GatewayJson {
    param([string]$Uri, [string]$Token, [string]$RequestId, [object]$Body)
    $response = Invoke-WebRequest -Uri $Uri -Method Post -Headers @{
        Authorization = "Bearer $Token"
        'X-Request-Id' = $RequestId
    } -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 8 -Compress) `
      -TimeoutSec 60 -SkipHttpErrorCheck
    $envelope = [string]$response.Content | ConvertFrom-Json
    if ([int]$response.StatusCode -ne 200 -or -not [bool]$envelope.ok) {
        throw "Gateway operation failed ($RequestId): $($response.Content)"
    }
    return $envelope
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
$null = New-Item -ItemType Directory -Path $runRoot

$localProduct = Join-Path $resolvedGame 'Mods\fumblesneeze.immersivechefs'
$backupProduct = Join-Path $resolvedGame ".immersive-chefs-subscribed-smoke-$runId"
$completionFile = Join-Path $runRoot 'smoke-complete.signal'
$gatewayRoot = Join-Path $runRoot 'gateway'
$launcherOut = Join-Path $runRoot 'launcher.out.log'
$launcherErr = Join-Path $runRoot 'launcher.err.log'
$launcherProcess = $null
$movedLocalProduct = $false
$primaryError = $null
$cleanupFailures = [Collections.Generic.List[string]]::new()

try {
    if (Test-Path -LiteralPath $localProduct) {
        if (Test-Path -LiteralPath $backupProduct) { throw 'The exact local-mod backup path already exists.' }
        [xml]$localAbout = Get-Content -LiteralPath (Join-Path $localProduct 'About\About.xml') -Raw
        if ([string]$localAbout.ModMetaData.packageId -cne 'fumblesneeze.immersivechefs') {
            throw 'The local product directory does not declare the expected package ID.'
        }
        Move-Item -LiteralPath $localProduct -Destination $backupProduct
        $movedLocalProduct = $true
    }

    $shell = (Get-Process -Id $PID).Path
    $launcher = Join-Path $repositoryRoot 'scripts\Invoke-GatewaySmoke.ps1'
    $activeModIdsFile = Join-Path $runRoot 'active-mod-ids.txt'
    [IO.File]::WriteAllLines(
        $activeModIdsFile,
        @('brrainz.harmony', 'fumblesneeze.immersivechefs'),
        [Text.UTF8Encoding]::new($false))
    $launcherArguments = @(
        '-NoProfile', '-File', $launcher,
        '-RimWorldPath', $resolvedGame,
        '-SteamModContentFolder', $resolvedWorkshop,
        '-ArtifactsPath', $gatewayRoot,
        '-Quicktest',
        '-Scenario', 'immersive-chefs-kitchenware-fabrication',
        '-AdditionalModIdsFile', $activeModIdsFile,
        '-InteractiveHoldSeconds', [string]$TimeoutSeconds,
        '-InteractiveCompletionFile', $completionFile,
        '-TimeoutSeconds', '300',
        '-Output', 'json')
    $launcherProcess = Start-Process -FilePath $shell -ArgumentList $launcherArguments -PassThru `
        -WindowStyle Hidden -RedirectStandardOutput $launcherOut -RedirectStandardError $launcherErr

    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    $hold = $null
    do {
        $launcherProcess.Refresh()
        if ($launcherProcess.HasExited) { throw "Subscribed smoke exited before hold: $(Get-Content $launcherErr -Raw -ErrorAction SilentlyContinue)" }
        $holdFiles = @(Get-ChildItem -LiteralPath $gatewayRoot -Recurse -Filter interactive-hold.json -File -ErrorAction SilentlyContinue)
        if ($holdFiles.Count -gt 1) { throw 'More than one subscribed-smoke Gateway hold appeared.' }
        if ($holdFiles.Count -eq 1) {
            $candidate = Read-Json $holdFiles[0].FullName
            if ([string]$candidate.Status -ceq 'active') { $hold = $candidate }
        }
        if ($null -eq $hold) { Start-Sleep -Milliseconds 500 }
    } while ($null -eq $hold -and [datetime]::UtcNow -lt $deadline)
    if ($null -eq $hold) { throw 'The subscribed-smoke process did not reach its bounded hold.' }

    $manifest = Read-Json ([string]$hold.Manifest)
    $baseUrl = [string]$manifest.baseUrl
    $token = [string]$manifest.token
    $gamePid = [int]$hold.ProcessId
    $client = Join-Path $repositoryRoot 'artifacts\HostTools\Release\net480\RimWorldDevGateway.Client.exe'
    $rootSource = @'
var product = LoadedModManager.RunningModsListForReading.Single(mod =>
    string.Equals(mod.PackageId, "fumblesneeze.immersivechefs", System.StringComparison.OrdinalIgnoreCase));
product.RootDir
'@
    $loadedRoot = Invoke-GatewayText -Uri "$baseUrl/executions/csharp" -Token $token `
        -RequestId 'release-subscribed-root' -Source $rootSource
    if (-not [string]::Equals([IO.Path]::GetFullPath($loadedRoot).TrimEnd('\'), $expectedInstall, [StringComparison]::OrdinalIgnoreCase)) {
        throw "RimWorld loaded Immersive Chefs from '$loadedRoot', not the subscribed Workshop directory."
    }

    $actionOutput = @(& $client action game.speed --arguments '{"speed":"normal"}' `
        --manifest ([string]$hold.Manifest) --pid $gamePid -o json 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Native game-speed action failed: $($actionOutput -join [Environment]::NewLine)" }

    $productSource = @'
var products = Find.CurrentMap.listerThings.AllThings
    .Where(thing => thing.def.defName == "ImmersiveChefs_PrimitiveCookware" ||
                    thing.def.defName == "ImmersiveChefs_Cookware")
    .OrderBy(thing => thing.def.defName)
    .ThenBy(thing => thing.ThingID)
    .ToList();
products.Count >= 2 ? string.Join("|", products.Select(thing => thing.ThingID).ToArray()) : "pending"
'@
    $productHandles = $null
    do {
        $observed = Invoke-GatewayText -Uri "$baseUrl/executions/csharp" -Token $token `
            -RequestId ('release-products-' + [guid]::NewGuid().ToString('N')) -Source $productSource
        if ($observed -cne 'pending') { $productHandles = @($observed.Split([char]'|')) }
        if ($null -eq $productHandles) { Start-Sleep -Seconds 1 }
    } while ($null -eq $productHandles -and [datetime]::UtcNow -lt $deadline)
    if ($null -eq $productHandles -or $productHandles.Count -lt 2) {
        throw 'The native fabrication jobs did not visibly produce both kitchenware sets before the deadline.'
    }

    $pauseOutput = @(& $client action game.pause --arguments '{"paused":true}' `
        --manifest ([string]$hold.Manifest) --pid $gamePid -o json 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Native pause action failed: $($pauseOutput -join [Environment]::NewLine)" }

    $screenshots = [Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt 2; $index++) {
        $handle = $productHandles[$index]
        $null = Invoke-GatewayJson -Uri "$baseUrl/selection" -Token $token `
            -RequestId "release-select-$index" -Body @{ operation = 'replace'; handles = @($handle) }
        $path = Join-Path $runRoot "subscribed-product-$index.png"
        $shotOutput = @(& $client screenshot --file $path --manifest ([string]$hold.Manifest) --pid $gamePid -o json 2>&1)
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Subscribed product screenshot failed: $($shotOutput -join [Environment]::NewLine)"
        }
        $screenshots.Add($path)
    }

    $scenarioFiles = @(Get-ChildItem -LiteralPath $gatewayRoot -Recurse -Filter scenario.json -File)
    if ($scenarioFiles.Count -ne 1) { throw 'The subscribed native scenario result is missing or ambiguous.' }
    $scenario = Read-Json $scenarioFiles[0].FullName
    if ([string]$scenario.Status -cne 'completed') { throw 'The subscribed native scenario did not complete.' }

    $result = [pscustomobject][ordered]@{
        status = 'passed'
        publishedFileId = $PublishedFileId
        loadedPackagePath = $loadedRoot
        processId = $gamePid
        playerAction = 'native game.speed Normal after two vanilla WorkGiver_DoBill jobs were accepted'
        observedResult = 'primitive and modern kitchenware products spawned from completed native bills'
        productHandles = $productHandles
        screenshots = @($screenshots)
        gatewayEvidence = [IO.DirectoryInfo]::new($scenarioFiles[0].FullName).Parent.FullName
        localProductRestored = $false
    }
}
catch { $primaryError = $_ }
finally {
    try { [IO.File]::WriteAllText($completionFile, [datetime]::UtcNow.ToString('O'), [Text.UTF8Encoding]::new($false)) }
    catch { $cleanupFailures.Add("Could not signal smoke completion: $($_.Exception.Message)") }
    if ($null -ne $launcherProcess) {
        try {
            if (-not $launcherProcess.WaitForExit(120000)) {
                $launcherProcess.Kill()
                $launcherProcess.WaitForExit()
                throw 'The subscribed-smoke launcher required exact-process force cleanup.'
            }
            if ($launcherProcess.ExitCode -ne 0) { throw "Subscribed-smoke launcher failed: $(Get-Content $launcherErr -Raw -ErrorAction SilentlyContinue)" }
        }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
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
$result.localProductRestored = -not (Test-Path -LiteralPath $backupProduct) -and
    (Test-Path -LiteralPath $localProduct -PathType Container)
if ($Output -eq 'json') { $result | ConvertTo-Json -Depth 8 -Compress } else { $result | Format-List }
