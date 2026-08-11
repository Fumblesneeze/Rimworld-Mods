<#
.SYNOPSIS
Plans or runs isolated RimWorld performance benchmarks.

.DESCRIPTION
Discovers marked performance fixture projects through the host metadata reader, validates exact
mod/lens families, and expands every benchmark repetition into its own fresh-process plan.

-DryRun builds and validates fixture assemblies and prints deterministic process/report paths. It
does not stage a bundle, write artifacts, alter game settings, or launch RimWorld.

Exit codes: 0 success, 1 execution/infrastructure failure, 2 invalid usage or discovery.

.EXAMPLE
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -DryRun -Output json

.EXAMPLE
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -DryRun -BenchmarkId gateway.circinus-calibration.instrumented -Repetitions 3
#>
[CmdletBinding()]
param(
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [string[]]$AvailableModIds = @(),

    [string[]]$GroupId = @(),

    [string[]]$BenchmarkId = @(),

    [ValidateRange(0, [int]::MaxValue)]
    [Nullable[int]]$WarmUpTicks,

    [ValidateRange(1, [int]::MaxValue)]
    [Nullable[int]]$SampleTicks,

    [ValidateRange(1, 32)]
    [Nullable[int]]$Repetitions,

    [string]$ArtifactsPath,

    [switch]$DryRun,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput {
    param([Parameter(Mandatory)][string]$Message)
    [Console]::Error.WriteLine($Message)
    exit 2
}

function Test-PackageId {
    param([AllowEmptyString()][string]$Value)
    return -not [string]::IsNullOrWhiteSpace($Value) -and
        $Value.Length -le 128 -and
        $Value -cmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$'
}

function Get-InstalledPackageIds {
    param(
        [Parameter(Mandatory)][string]$GamePath,
        [Parameter(Mandatory)][string]$WorkshopPath
    )

    $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $null = $ids.Add('ludeon.rimworld')
    foreach ($root in @((Join-Path $GamePath 'Data'), (Join-Path $GamePath 'Mods'), $WorkshopPath)) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        foreach ($aboutFile in @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter 'About.xml' -ErrorAction SilentlyContinue)) {
            try {
                [xml]$about = Get-Content -LiteralPath $aboutFile.FullName -Raw -ErrorAction Stop
                $nodes = @($about.SelectNodes('/*[local-name()="ModMetaData"]/*[local-name()="packageId"]'))
                $id = if ($nodes.Count -eq 1) { [string]$nodes[0].InnerText } else { '' }
                if (Test-PackageId $id.Trim()) { $null = $ids.Add($id.Trim().ToLowerInvariant()) }
            }
            catch {
                # An unrelated malformed download is not a resolvable exact package.
            }
        }
    }
    return @($ids | Sort-Object)
}

function Invoke-BoundedProcess {
    param(
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][int]$TimeoutMilliseconds
    )
    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $Executable
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $null = $info.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Could not start the performance host.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            try { $process.Kill($true); $process.WaitForExit() } catch { }
            throw "Performance host exceeded its $TimeoutMilliseconds ms deadline."
        }
        return [pscustomobject]@{
            ExitCode = [int]$process.ExitCode
            StandardOutput = [string]$stdout.GetAwaiter().GetResult()
            StandardError = [string]$stderr.GetAwaiter().GetResult()
        }
    }
    finally { $process.Dispose() }
}

if (-not (Test-Path -LiteralPath $RimWorldPath -PathType Container)) {
    Exit-InvalidInput "RimWorld path does not exist: $RimWorldPath"
}
if (-not (Test-Path -LiteralPath $SteamModContentFolder -PathType Container)) {
    Exit-InvalidInput "Steam Workshop content path does not exist: $SteamModContentFolder"
}

$game = (Resolve-Path -LiteralPath $RimWorldPath).Path
$workshop = (Resolve-Path -LiteralPath $SteamModContentFolder).Path
if (-not (Test-Path -LiteralPath (Join-Path $game 'RimWorldWin64.exe') -PathType Leaf)) {
    Exit-InvalidInput "RimWorld executable does not exist under: $game"
}
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$hostProject = Join-Path $repositoryRoot 'tools\RimWorldDevGateway.EndToEndHost\RimWorldDevGateway.EndToEndHost.csproj'
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = Join-Path $repositoryRoot 'artifacts\PerformanceRuns\Current'
}
$artifactRoot = [System.IO.Path]::GetFullPath($ArtifactsPath)

$packages = if ($AvailableModIds.Count -eq 0) {
    @(Get-InstalledPackageIds -GamePath $game -WorkshopPath $workshop)
}
else {
    @($AvailableModIds |
        ForEach-Object { $_ -split ',' } |
        ForEach-Object { $_.Trim().ToLowerInvariant() } |
        Sort-Object -Unique)
}
foreach ($required in @('brrainz.harmony', 'ludeon.rimworld', 'astryl.circinus', 'fumblesneeze.rimworlddevgateway')) {
    if ($packages -notcontains $required) { Exit-InvalidInput "Available mod IDs must include $required." }
}
foreach ($package in $packages) {
    if (-not (Test-PackageId $package)) { Exit-InvalidInput "Invalid available mod package ID: '$package'" }
}
foreach ($id in @($GroupId) + @($BenchmarkId)) {
    if ([string]::IsNullOrWhiteSpace($id) -or $id.Length -gt 256 -or $id -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
        Exit-InvalidInput "Invalid performance filter: '$id'"
    }
}

$arguments = [System.Collections.Generic.List[string]]::new()
foreach ($value in @(
    'run', '--project', $hostProject, '--configuration', 'Release', '--', 'performance-plan',
    '--repository-root', $repositoryRoot,
    '--mods-root', (Join-Path $game 'Mods'),
    '--rimworld-version', '1.6',
    '--artifact-root', $artifactRoot,
    '--output', 'json')) { $arguments.Add($value) }
foreach ($package in $packages) { $arguments.Add('--package-id'); $arguments.Add($package) }
foreach ($id in $GroupId) { $arguments.Add('--group-id'); $arguments.Add($id) }
foreach ($id in $BenchmarkId) { $arguments.Add('--benchmark-id'); $arguments.Add($id) }
if ($null -ne $WarmUpTicks) { $arguments.Add('--warm-up-ticks'); $arguments.Add([string]$WarmUpTicks) }
if ($null -ne $SampleTicks) { $arguments.Add('--sample-ticks'); $arguments.Add([string]$SampleTicks) }
if ($null -ne $Repetitions) { $arguments.Add('--repetitions'); $arguments.Add([string]$Repetitions) }

$hostResult = Invoke-BoundedProcess -Executable 'dotnet' -Arguments @($arguments) -TimeoutMilliseconds 300000
if ($hostResult.ExitCode -ne 0) {
    [Console]::Error.WriteLine($hostResult.StandardError.Trim())
    exit $(if ($hostResult.ExitCode -eq 2) { 2 } else { 1 })
}
try { $plan = $hostResult.StandardOutput | ConvertFrom-Json -ErrorAction Stop }
catch { [Console]::Error.WriteLine("Performance host returned invalid JSON: $($_.Exception.Message)"); exit 1 }

if (-not $DryRun) {
    [Console]::Error.WriteLine('Performance execution is not available until the staged in-game runner is installed; use -DryRun.')
    exit 1
}

$result = [pscustomobject]@{
    Status = 'dry-run'
    MutatedGame = $false
    AvailablePackageCount = $packages.Count
    Groups = @($plan.groups)
    Processes = @($plan.processes)
    Reports = $plan.reports
}
if ($Output -eq 'json') { $result | ConvertTo-Json -Depth 16 -Compress }
else { $result | Format-List }
