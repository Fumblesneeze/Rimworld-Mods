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

    [string]$BaselineDirectory,

    [string]$BaselinePolicyPath,

    [switch]$CreateBaselineCandidate,

    [string]$BaselineCandidatePath,

    [switch]$InformationalCrossVersion,

    [ValidateRange(60, [int]::MaxValue)]
    [int]$TimeoutSeconds = 600,

    [switch]$DryRun,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RimWorldEndToEndRunner.Support.psm1') -Force

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
    if ([System.IO.Path]::GetFileNameWithoutExtension($Executable) -ieq 'dotnet') {
        # A reusable MSBuild worker can inherit the redirected output handles after `dotnet run`
        # exits, leaving ReadToEndAsync blocked forever. Performance host invocations are bounded
        # and isolated, so disable both worker-reuse mechanisms for the exact child process.
        $info.Environment['MSBUILDDISABLENODEREUSE'] = '1'
        $info.Environment['DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER'] = '1'
    }
    foreach ($argument in $Arguments) { $null = $info.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Could not start the performance host.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            $terminationFailure = $null
            try { $process.Kill($true) }
            catch { $terminationFailure = $_.Exception.Message }
            if (-not $process.WaitForExit(10000)) {
                throw "Performance host exceeded its $TimeoutMilliseconds ms deadline and its retained process tree did not terminate within 10000 ms."
            }
            if (-not [string]::IsNullOrWhiteSpace($terminationFailure)) {
                throw "Performance host exceeded its $TimeoutMilliseconds ms deadline; retained process-tree termination reported: $terminationFailure"
            }
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

function Get-ProjectProperty {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$PropertyName
    )
    [xml]$document = Get-Content -LiteralPath $ProjectPath -Raw -ErrorAction Stop
    $values = @($document.SelectNodes("//*[local-name()='$PropertyName']") |
        ForEach-Object { [string]$_.InnerText } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() } |
        Select-Object -Unique)
    if ($values.Count -eq 0) { return $null }
    if ($values.Count -ne 1) { throw "Project '$ProjectPath' declares multiple $PropertyName values." }
    return [string]$values[0]
}

function Get-RepoProductProjects {
    param([Parameter(Mandatory)][string]$RepositoryRoot)
    $result = @{}
    foreach ($project in @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'mods') -Recurse -File -Filter '*.csproj')) {
        $packageId = Get-ProjectProperty -ProjectPath $project.FullName -PropertyName 'RimWorldPackageId'
        if ([string]::IsNullOrWhiteSpace($packageId)) { continue }
        if (-not (Test-PackageId $packageId)) {
            throw "Repo product project has an invalid RimWorldPackageId '$packageId'."
        }
        $key = $packageId.ToLowerInvariant()
        if ($result.ContainsKey($key)) { throw "Multiple repo projects declare package '$key'." }
        $result[$key] = $project.FullName
    }
    return $result
}

function Invoke-PerformanceHostOperation {
    param(
        [Parameter(Mandatory)][ValidateSet('performance-stage', 'clean')][string]$Operation,
        [Parameter(Mandatory)][string]$HostProject,
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$ModsRoot,
        [Parameter(Mandatory)][string[]]$PackageIds,
        [Parameter(Mandatory)][string]$LeaseFile
    )
    $packageFile = $null
    try {
        $arguments = [System.Collections.Generic.List[string]]::new()
        foreach ($value in @('run', '--project', $HostProject, '--configuration', 'Release', '--', $Operation)) {
            $arguments.Add($value)
        }
        if ($Operation -ne 'clean') {
            foreach ($value in @(
                '--repository-root', $RepositoryRoot,
                '--mods-root', $ModsRoot,
                '--rimworld-version', '1.6')) { $arguments.Add($value) }
            $packageFile = Join-Path ([System.IO.Path]::GetTempPath()) (
                "rimworld-performance-stage-$([guid]::NewGuid().ToString('N')).txt")
            [System.IO.File]::WriteAllLines(
                $packageFile, $PackageIds, [System.Text.UTF8Encoding]::new($false))
            $arguments.Add('--package-id-file')
            $arguments.Add($packageFile)
        }
        $arguments.Add('--lease-file')
        $arguments.Add($LeaseFile)
        $arguments.Add('--output')
        $arguments.Add('json')
        return Invoke-BoundedProcess -Executable 'dotnet' -Arguments @($arguments) -TimeoutMilliseconds 300000
    }
    finally {
        if ($null -ne $packageFile -and (Test-Path -LiteralPath $packageFile -PathType Leaf)) {
            Remove-Item -LiteralPath $packageFile -Force
        }
    }
}

function Write-CircinusLocalOnlySettings {
    param([Parameter(Mandatory)][string]$Directory)
    $null = New-Item -Path $Directory -ItemType Directory -Force
    $path = Join-Path $Directory 'Mod_3773680130_CircinusMod.xml'
    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<SettingsBlock>
  <ModSettings Class="Circinus.Bootstrap.CircinusSettings">
    <autoStartProfiler>false</autoStartProfiler>
    <autoArmProfiler>false</autoArmProfiler>
    <autoProfile>false</autoProfile>
    <autoProfileAsked>1</autoProfileAsked>
    <armedTargetKeys IsNull="True" />
    <showWarmupWindow>false</showWarmupWindow>
    <warmupSeconds>0</warmupSeconds>
    <ingestEnabled>false</ingestEnabled>
    <consentVersion>2</consentVersion>
    <autoRecord>false</autoRecord>
  </ModSettings>
</SettingsBlock>
'@
    [System.IO.File]::WriteAllText($path, $content.TrimStart(), [System.Text.UTF8Encoding]::new($false))
    return $path
}

function Get-OptionalFileIdentity {
    param([Parameter(Mandatory)][string]$Path)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return [pscustomobject]@{ Path = $fullPath; Exists = $false; Sha256 = $null; Length = 0L }
    }
    $item = Get-Item -LiteralPath $fullPath -Force
    return [pscustomobject]@{
        Path = $fullPath
        Exists = $true
        Sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
        Length = [long]$item.Length
    }
}

function Test-SameFileIdentity {
    param(
        [Parameter(Mandatory)][object]$Before,
        [Parameter(Mandatory)][object]$After
    )
    return [bool]$Before.Exists -eq [bool]$After.Exists -and
        [long]$Before.Length -eq [long]$After.Length -and
        [string]$Before.Sha256 -ceq [string]$After.Sha256
}

function Copy-ExactPerformanceArtifact {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )
    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Performance artifact is missing: $Source"
    }
    $parent = Split-Path -Parent $Destination
    $null = New-Item -Path $parent -ItemType Directory -Force
    [System.IO.File]::Copy($Source, $Destination, $false)
    $sourceHash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
    if ($sourceHash -cne $destinationHash) {
        throw "Performance artifact hash mismatch: $Destination"
    }
    return [pscustomobject]@{ Path = $Destination; Sha256 = $destinationHash }
}

function Write-PerformanceProcessReports {
    param(
        [Parameter(Mandatory)][object]$Normalized,
        [Parameter(Mandatory)][object]$ProcessPlan
    )
    $rows = @(
        @($Normalized.metrics) | ForEach-Object {
            [pscustomobject]@{
                Scope = [string]$_.scope; Key = [string]$_.key; Name = [string]$_.name
                Value = [double]$_.value; Unit = [string]$_.unit
                Denominator = [string]$_.denominator; Claim = [string]$_.claim
            }
        }
        @($Normalized.checkpoints) | ForEach-Object {
            [pscustomobject]@{
                Scope = 'checkpoint'; Key = [string]$_.id; Name = [string]$_.id
                Value = [double]$_.value; Unit = [string]$_.unit
                Denominator = 'sample-window'; Claim = 'control'
            }
        }
    )
    @($rows) | Export-Csv -LiteralPath ([string]$ProcessPlan.csvReportPath) -NoTypeInformation -Encoding utf8
    $summary = [System.Collections.Generic.List[string]]::new()
    $summary.Add("# Performance sample: $([string]$ProcessPlan.benchmarkId)")
    $summary.Add('')
    $summary.Add("- Evidence lens: $([string]$Normalized.evidenceLens)")
    $summary.Add("- Workload: $([string]$Normalized.workloadVersion)")
    $summary.Add("- Repetition: $([int]$ProcessPlan.repetition)")
    $summary.Add("- Circinus run: $([string]$Normalized.runId)")
    $summary.Add("- Recorded profiler cycles: $([int]$Normalized.profilerPolicy.recordedCycles)")
    $summary.Add("- Native samples inside Gateway markers: $(@($Normalized.samples).Count)")
    $summary.Add('')
    $summary.Add('Raw Circinus attribution is gross measurement; it is not causal net impact.')
    [System.IO.File]::WriteAllLines(
        [string]$ProcessPlan.markdownReportPath,
        $summary,
        [System.Text.UTF8Encoding]::new($false))
    return @($rows)
}

function Get-ExactAssemblyIdentity {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required performance dependency assembly is missing: $Path"
    }
    $name = [System.Reflection.AssemblyName]::GetAssemblyName($Path).FullName
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    return "$name|sha256:$hash"
}

function Get-DeployedProductAssemblyIdentity {
    param(
        [Parameter(Mandatory)][object[]]$DeploymentEvidence,
        [Parameter(Mandatory)][hashtable]$Projects,
        [Parameter(Mandatory)][string]$PackageId
    )
    $deployment = @($DeploymentEvidence | Where-Object { [string]$_.PackageId -ieq $PackageId })
    if ($deployment.Count -ne 1) {
        throw "Expected one deployed product identity for '$PackageId', observed $($deployment.Count)."
    }
    if (-not $Projects.ContainsKey($PackageId.ToLowerInvariant())) {
        throw "No repository project exists for measured subject '$PackageId'."
    }
    $project = [string]$Projects[$PackageId.ToLowerInvariant()]
    $assemblyName = Get-ProjectProperty -ProjectPath $project -PropertyName 'AssemblyName'
    if ([string]::IsNullOrWhiteSpace($assemblyName)) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    }
    $relative = "1.6/Assemblies/$assemblyName.dll"
    $file = @($deployment[0].Files | Where-Object {
        ([string]$_.RelativePath).Replace('\', '/') -ceq $relative
    })
    if ($file.Count -ne 1) {
        throw "Deployed product '$PackageId' did not expose one exact '$relative' identity."
    }
    return "$assemblyName|sha256:$([string]$file[0].Sha256)"
}

function New-PerformanceCurrentSnapshot {
    param(
        [Parameter(Mandatory)][object]$Plan,
        [Parameter(Mandatory)][object[]]$ProcessResults,
        [Parameter(Mandatory)][object[]]$DeploymentEvidence,
        [Parameter(Mandatory)][hashtable]$Projects,
        [Parameter(Mandatory)][string]$CircinusAssemblyIdentity,
        [Parameter(Mandatory)][string]$SettingsSha256,
        [Parameter(Mandatory)][string]$CreatedUtc,
        [AllowNull()][string]$InfrastructureFailure
    )
    $runtimeErrors = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($InfrastructureFailure)) {
        $runtimeErrors.Add($InfrastructureFailure)
    }
    foreach ($failed in @($ProcessResults | Where-Object { [string]$_.Status -ne 'passed' })) {
        $runtimeErrors.Add("$([string]$failed.BenchmarkId): $([string]$failed.Failure)")
    }

    $observations = [System.Collections.Generic.List[object]]::new()
    foreach ($result in @($ProcessResults | Where-Object Status -eq 'passed')) {
        $processPlan = @($Plan.processes | Where-Object {
            [int]$_.sequence -eq [int]$result.Sequence
        })
        if ($processPlan.Count -ne 1) {
            throw "Could not resolve exact performance plan sequence $([int]$result.Sequence)."
        }
        $processPlan = $processPlan[0]
        $raw = Get-Content -LiteralPath ([string]$result.Raw.Path) -Raw | ConvertFrom-Json -ErrorAction Stop
        $normalized = Get-Content -LiteralPath ([string]$result.Normalized.Path) -Raw | ConvertFrom-Json -ErrorAction Stop
        foreach ($problem in @(
            @{ Name = 'incomplete'; Value = [bool]$raw.incomplete },
            @{ Name = 'errorsDropped'; Value = [long]$raw.errorsDropped -gt 0 },
            @{ Name = 'patchesDropped'; Value = [long]$raw.patchesDropped -gt 0 },
            @{ Name = 'loadErrorsDropped'; Value = [long]$raw.loadErrorsDropped -gt 0 }
        )) {
            if ([bool]$problem.Value) {
                $runtimeErrors.Add("$([string]$result.BenchmarkId): Circinus reported $([string]$problem.Name).")
            }
        }
        $severeLoadErrors = @($raw.loadErrors | Where-Object {
            [string]$_.severity -in @('error', 'exception', 'fatal')
        })
        if ($severeLoadErrors.Count -gt 0) {
            $runtimeErrors.Add("$([string]$result.BenchmarkId): Circinus reported $($severeLoadErrors.Count) runtime error(s).")
        }

        $hardwareHash = [string]$raw.env.hardware.hash
        $runtimeFingerprint = "os=$([string]$raw.install.os)|hardware=$hardwareHash"
        $selectors = @($processPlan.methodSelectors | ForEach-Object {
            "$([string]$_.kind):$([string]$_.value):$([string]$_.category)"
        })
        $profilingPolicy = "control=$([string]$normalized.controlMode)|selectors=$($selectors -join ';')"
        $measurements = [System.Collections.Generic.List[object]]::new()
        foreach ($metric in @($result.Metrics)) {
            $sidecars = @($normalized.profilerSidecars | Where-Object {
                [string]$_.rowKey -ceq [string]$metric.Key
            })
            if ($sidecars.Count -gt 1) {
                throw "Metric '$([string]$metric.Key)' resolved multiple Circinus sidecars."
            }
            $sidecar = if ($sidecars.Count -eq 1) { $sidecars[0] } else { $null }
            $exactMethod = if ($null -ne $sidecar) { [string]$sidecar.methodIdentity } else { '' }
            $selector = if (-not [string]::IsNullOrWhiteSpace($exactMethod)) {
                $exactMethod
            }
            else {
                "$([string]$metric.Scope):$([string]$metric.Key)"
            }
            $hasWindowContext = $null -ne $sidecar -or [string]$metric.Scope -ceq 'mod'
            $measurements.Add([pscustomobject]@{
                Scope = [string]$metric.Scope
                Selector = $selector
                MetricName = [string]$metric.Name
                Value = [double]$metric.Value
                Unit = [string]$metric.Unit
                Denominator = [string]$metric.Denominator
                Claim = [string]$metric.Claim
                Calls = if ($null -ne $sidecar) { [double]$sidecar.totalCalls } else { $null }
                TimedCalls = if ($null -ne $sidecar) { [double]$sidecar.totalTimedCalls } else { $null }
                DutyPercent = if ($hasWindowContext) { [double]$normalized.profilerPolicy.dutyPercent } else { $null }
                SampleShift = if ($null -ne $sidecar) { [double]$sidecar.sampleShift } else { $null }
                RecordedCycles = if ($hasWindowContext) { [double]$normalized.profilerPolicy.recordedCycles } else { $null }
                ProfilerWindowMilliseconds = if ($hasWindowContext) { [double]$normalized.profilerPolicy.windowMilliseconds } else { $null }
                ProfilerWindowTicks = if ($hasWindowContext) { [double]$normalized.profilerPolicy.windowTicks } else { $null }
                SamplingContextIdentity = if ($null -ne $sidecar) {
                    "cycles=$([double]$normalized.profilerPolicy.recordedCycles)|windowMs=$([double]$normalized.profilerPolicy.windowMilliseconds)|windowTicks=$([double]$normalized.profilerPolicy.windowTicks)|duty=$([double]$normalized.profilerPolicy.dutyPercent)|shift=$([double]$sidecar.sampleShift)|calls=$([double]$sidecar.totalCalls)|timed=$([double]$sidecar.totalTimedCalls)"
                }
                elseif ([string]$metric.Scope -ceq 'mod') {
                    "cycles=$([double]$normalized.profilerPolicy.recordedCycles)|windowMs=$([double]$normalized.profilerPolicy.windowMilliseconds)|windowTicks=$([double]$normalized.profilerPolicy.windowTicks)|duty=$([double]$normalized.profilerPolicy.dutyPercent)"
                }
                else { '' }
                ExactMethod = $exactMethod
            })
        }
        $compatibility = [ordered]@{
            BenchmarkId = [string]$processPlan.benchmarkId
            GroupId = [string]$processPlan.groupId
            WorkloadVersion = [string]$processPlan.workloadVersion
            EvidenceLens = [string]$normalized.controlMode
            ActivePackageIds = @($processPlan.activePackageIds | ForEach-Object { [string]$_ })
            GameVersion = [string]$result.SmokeResult.LiveGameVersion
            ProductAssemblyIdentity = Get-DeployedProductAssemblyIdentity `
                -DeploymentEvidence $DeploymentEvidence `
                -Projects $Projects `
                -PackageId ([string]$processPlan.measuredSubjectPackageId)
            TestAssemblyIdentity = "$([string]$processPlan.assemblyIdentity)|mvid:$([string]$processPlan.moduleVersionId)|sha256:$([string]$processPlan.assemblySha256)"
            CircinusAssemblyIdentity = $CircinusAssemblyIdentity
            CircinusSchemaIdentity = "$([int]$raw.schemaMajor).$([int]$raw.schemaMinor)"
            ProfilingPolicyIdentity = $profilingPolicy
            SamplingPolicyIdentity = "circinus-local-settings:$SettingsSha256|native-adaptive/v1"
            HardwareRuntimeFingerprint = $runtimeFingerprint
            DeterministicSeed = [int]$processPlan.deterministicSeed
            WarmUpTicks = [int]$processPlan.warmUpTicks
            SampleTicks = [int]$processPlan.sampleTicks
            GameSpeed = [int]$processPlan.gameSpeed
            RepetitionCount = 1
            AggregationPolicyIdentity = 'arithmetic-mean/v1'
        }
        $identityJson = $compatibility | ConvertTo-Json -Depth 8 -Compress
        $observations.Add([pscustomobject]@{
            IdentityJson = $identityJson
            Compatibility = [pscustomobject]$compatibility
            Measurements = @($measurements)
        })
    }

    $cases = [System.Collections.Generic.List[object]]::new()
    foreach ($group in @($observations | Group-Object IdentityJson)) {
        $members = @($group.Group)
        $expectedRepetitions = $members.Count
        $combined = [System.Collections.Generic.List[object]]::new()
        $metricGroups = @($members | ForEach-Object { @($_.Measurements) } | Group-Object {
            "$([string]$_.Scope)`n$([string]$_.Selector)`n$([string]$_.MetricName)"
        })
        foreach ($metricGroup in $metricGroups) {
            $metrics = @($metricGroup.Group)
            if ($metrics.Count -ne $expectedRepetitions) {
                $runtimeErrors.Add("$([string]$members[0].Compatibility.BenchmarkId): repetition metric set drifted for '$([string]$metricGroup.Name)'.")
                continue
            }
            $units = @($metrics | ForEach-Object { [string]$_.Unit } | Sort-Object -Unique)
            $denominators = @($metrics | ForEach-Object { [string]$_.Denominator } | Sort-Object -Unique)
            $claims = @($metrics | ForEach-Object { [string]$_.Claim } | Sort-Object -Unique)
            $methods = @($metrics | ForEach-Object { [string]$_.ExactMethod } | Sort-Object -Unique)
            if ($units.Count -ne 1 -or $denominators.Count -ne 1 -or $claims.Count -ne 1 -or $methods.Count -ne 1) {
                $runtimeErrors.Add("$([string]$members[0].Compatibility.BenchmarkId): repetition metadata drifted for '$([string]$metricGroup.Name)'.")
                continue
            }
            $calls = @($metrics | Where-Object { $null -ne $_.Calls } | ForEach-Object { [double]$_.Calls })
            $timed = @($metrics | Where-Object { $null -ne $_.TimedCalls } | ForEach-Object { [double]$_.TimedCalls })
            $duty = @($metrics | Where-Object { $null -ne $_.DutyPercent } | ForEach-Object { [double]$_.DutyPercent })
            $shift = @($metrics | Where-Object { $null -ne $_.SampleShift } | ForEach-Object { [double]$_.SampleShift })
            $cycles = @($metrics | Where-Object { $null -ne $_.RecordedCycles } | ForEach-Object { [double]$_.RecordedCycles })
            $windowMilliseconds = @($metrics | Where-Object { $null -ne $_.ProfilerWindowMilliseconds } | ForEach-Object { [double]$_.ProfilerWindowMilliseconds })
            $windowTicks = @($metrics | Where-Object { $null -ne $_.ProfilerWindowTicks } | ForEach-Object { [double]$_.ProfilerWindowTicks })
            $combined.Add([pscustomobject]@{
                Scope = [string]$metrics[0].Scope
                Selector = [string]$metrics[0].Selector
                MetricName = [string]$metrics[0].MetricName
                Value = [double](($metrics | Measure-Object -Property Value -Average).Average)
                Unit = [string]$units[0]
                Denominator = [string]$denominators[0]
                Claim = [string]$claims[0]
                Calls = if ($calls.Count -eq $expectedRepetitions) { [double](($calls | Measure-Object -Average).Average) } else { $null }
                TimedCalls = if ($timed.Count -eq $expectedRepetitions) { [double](($timed | Measure-Object -Average).Average) } else { $null }
                DutyPercent = if ($duty.Count -eq $expectedRepetitions) { [double](($duty | Measure-Object -Average).Average) } else { $null }
                SampleShift = if ($shift.Count -eq $expectedRepetitions) { [double](($shift | Measure-Object -Average).Average) } else { $null }
                RecordedCycles = if ($cycles.Count -eq $expectedRepetitions) { [double](($cycles | Measure-Object -Average).Average) } else { $null }
                ProfilerWindowMilliseconds = if ($windowMilliseconds.Count -eq $expectedRepetitions) { [double](($windowMilliseconds | Measure-Object -Average).Average) } else { $null }
                ProfilerWindowTicks = if ($windowTicks.Count -eq $expectedRepetitions) { [double](($windowTicks | Measure-Object -Average).Average) } else { $null }
                SamplingContextIdentity = @($metrics | ForEach-Object { [string]$_.SamplingContextIdentity }) -join '||'
                ExactMethod = [string]$methods[0]
            })
        }
        $compatibility = $members[0].Compatibility
        $compatibility.RepetitionCount = $expectedRepetitions
        $compatibility.AggregationPolicyIdentity = 'arithmetic-mean/v1'
        $cases.Add([pscustomobject]@{
            Compatibility = $compatibility
            Measurements = @($combined)
        })
    }
    return [pscustomobject]@{
        SchemaVersion = 1
        Status = 'current'
        CreatedUtc = $CreatedUtc
        RuntimeErrors = @($runtimeErrors | Sort-Object -Unique)
        Cases = @($cases)
    }
}

function Invoke-PerformanceBaselineOperation {
    param(
        [Parameter(Mandatory)][string]$HostProject,
        [Parameter(Mandatory)][string]$CurrentSnapshot,
        [string]$BaselineDirectory,
        [string]$Policy,
        [string]$Report,
        [string]$CandidateOutput,
        [switch]$CrossVersion
    )
    $arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @(
        'run', '--project', $HostProject, '--configuration', 'Release', '--',
        'performance-baseline', '--current-snapshot', $CurrentSnapshot, '--output', 'json')) {
        $arguments.Add($value)
    }
    if (-not [string]::IsNullOrWhiteSpace($CandidateOutput)) {
        $arguments.Add('--candidate-output')
        $arguments.Add($CandidateOutput)
    }
    else {
        foreach ($value in @(
            '--baseline-directory', $BaselineDirectory,
            '--policy', $Policy,
            '--report', $Report)) { $arguments.Add($value) }
        if ($CrossVersion) { $arguments.Add('--informational-cross-version') }
    }
    return Invoke-BoundedProcess -Executable 'dotnet' -Arguments @($arguments) -TimeoutMilliseconds 300000
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
if ([string]::IsNullOrWhiteSpace($BaselineDirectory)) {
    $BaselineDirectory = Join-Path $repositoryRoot 'performance\baselines'
}
if ([string]::IsNullOrWhiteSpace($BaselinePolicyPath)) {
    $BaselinePolicyPath = Join-Path $repositoryRoot 'performance\thresholds.json'
}
if ($CreateBaselineCandidate -and $InformationalCrossVersion) {
    Exit-InvalidInput '-CreateBaselineCandidate cannot be combined with -InformationalCrossVersion.'
}
if (-not $CreateBaselineCandidate) {
    if (-not (Test-Path -LiteralPath $BaselineDirectory -PathType Container)) {
        Exit-InvalidInput "Performance baseline directory does not exist: $BaselineDirectory"
    }
    if (-not (Test-Path -LiteralPath $BaselinePolicyPath -PathType Leaf)) {
        Exit-InvalidInput "Performance threshold policy does not exist: $BaselinePolicyPath"
    }
}
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = if ($DryRun) {
        Join-Path $repositoryRoot 'artifacts\PerformanceRuns\Current'
    }
    else {
        Join-Path $repositoryRoot 'artifacts\PerformanceRuns'
    }
}
$runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
$artifactBase = [System.IO.Path]::GetFullPath($ArtifactsPath)
$artifactRoot = if ($DryRun) { $artifactBase } else { Join-Path $artifactBase $runId }
if ([string]::IsNullOrWhiteSpace($BaselineCandidatePath)) {
    $BaselineCandidatePath = Join-Path $artifactRoot 'baseline-candidate.json'
}

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
$packageIdFile = Join-Path ([System.IO.Path]::GetTempPath()) (
    "rimworld-performance-packages-$([guid]::NewGuid().ToString('N')).txt")
[System.IO.File]::WriteAllLines($packageIdFile, $packages, [System.Text.UTF8Encoding]::new($false))
$arguments.Add('--package-id-file')
$arguments.Add($packageIdFile)
foreach ($id in $GroupId) { $arguments.Add('--group-id'); $arguments.Add($id) }
foreach ($id in $BenchmarkId) { $arguments.Add('--benchmark-id'); $arguments.Add($id) }
if ($null -ne $WarmUpTicks) { $arguments.Add('--warm-up-ticks'); $arguments.Add([string]$WarmUpTicks) }
if ($null -ne $SampleTicks) { $arguments.Add('--sample-ticks'); $arguments.Add([string]$SampleTicks) }
if ($null -ne $Repetitions) { $arguments.Add('--repetitions'); $arguments.Add([string]$Repetitions) }

$hostResult = $null
try {
    $hostResult = Invoke-BoundedProcess -Executable 'dotnet' -Arguments @($arguments) -TimeoutMilliseconds 300000
}
finally {
    if (Test-Path -LiteralPath $packageIdFile -PathType Leaf) {
        Remove-Item -LiteralPath $packageIdFile -Force
    }
}
if ($hostResult.ExitCode -ne 0) {
    [Console]::Error.WriteLine($hostResult.StandardError.Trim())
    exit $(if ($hostResult.ExitCode -eq 2) { 2 } else { 1 })
}
try { $plan = $hostResult.StandardOutput | ConvertFrom-Json -ErrorAction Stop }
catch { [Console]::Error.WriteLine("Performance host returned invalid JSON: $($_.Exception.Message)"); exit 1 }

if ($DryRun) {
    $result = [pscustomobject]@{
        Status = 'dry-run'
        MutatedGame = $false
        AvailablePackageCount = $packages.Count
        Groups = @($plan.groups)
        Processes = @($plan.processes)
        Reports = $plan.reports
        Baseline = [pscustomobject]@{
            Mode = if ($CreateBaselineCandidate) { 'candidate' } else { 'compare' }
            Directory = [System.IO.Path]::GetFullPath($BaselineDirectory)
            Policy = [System.IO.Path]::GetFullPath($BaselinePolicyPath)
            Candidate = [System.IO.Path]::GetFullPath($BaselineCandidatePath)
            InformationalCrossVersion = [bool]$InformationalCrossVersion
        }
    }
    if ($Output -eq 'json') { $result | ConvertTo-Json -Depth 16 -Compress }
    else { $result | Format-List }
    exit 0
}

if ($null -ne $WarmUpTicks -or $null -ne $SampleTicks -or $null -ne $Repetitions) {
    Exit-InvalidInput 'Non-dry runtime overrides are not accepted until the staged descriptor override contract is implemented.'
}
$existingRimWorld = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
if ($existingRimWorld.Count -ne 0) {
    Exit-InvalidInput "RimWorld is already running (PID(s): $($existingRimWorld.Id -join ', '))."
}

$modsRoot = Join-Path $game 'Mods'
$managedPath = Join-Path $game 'RimWorldWin64_Data\Managed'
$smokeScript = Join-Path $repositoryRoot 'scripts\Invoke-GatewaySmoke.ps1'
$leaseFile = Join-Path $artifactRoot 'performance-stage.lease.json'
$stagePath = Join-Path $artifactRoot 'performance-stage.json'
$stageCleanupPath = Join-Path $artifactRoot 'performance-stage-cleanup.json'
$deploymentPath = Join-Path $artifactRoot 'product-deployment.json'
$settingsInput = Join-Path $artifactRoot 'prelaunch-config'
$normalUserRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
$NormalCircinusSettingsPath = Join-Path $normalUserRoot 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\Mod_3773680130_CircinusMod.xml'
$NormalCircinusSettingsBefore = Get-OptionalFileIdentity -Path $NormalCircinusSettingsPath
$NormalCircinusSettingsAfter = $null
$NormalCircinusSettingsUnchanged = $false
$aggregateJsonPath = [string]$plan.reports.aggregateJsonPath
$aggregateCsvPath = [string]$plan.reports.aggregateCsvPath
$aggregateMarkdownPath = [string]$plan.reports.summaryMarkdownPath
$currentSnapshotPath = Join-Path $artifactRoot 'performance-current.json'
$baselineComparisonPath = Join-Path $artifactRoot 'baseline-comparison.json'
$null = New-Item -Path $artifactRoot -ItemType Directory -Force
$settingsPath = Write-CircinusLocalOnlySettings -Directory $settingsInput
$stagePublished = $false
$stageAttempted = $false
$stageCleaned = $false
$infrastructureFailure = $null
$processResults = [System.Collections.Generic.List[object]]::new()
$deploymentEvidence = [System.Collections.Generic.List[object]]::new()
$projects = @{}
$circinusAssemblyPath = Join-Path $workshop '3773680130\Assemblies\Circinus.dll'
$circinusAssemblyIdentity = Get-ExactAssemblyIdentity -Path $circinusAssemblyPath
$baselineResult = $null

try {
    $projects = Get-RepoProductProjects -RepositoryRoot $repositoryRoot
    $ownerIds = @($plan.processes | ForEach-Object { [string]$_.stagingOwnerPackageId }) +
        @('fumblesneeze.rimworlddevgateway') |
        ForEach-Object { $_.ToLowerInvariant() } |
        Sort-Object -Unique
    foreach ($ownerId in $ownerIds) {
        if (-not $projects.ContainsKey($ownerId)) {
            throw "No repo product project declares performance staging owner '$ownerId'."
        }
    }
    $subjectIds = @($plan.processes | ForEach-Object { [string]$_.measuredSubjectPackageId }) |
        ForEach-Object { $_.ToLowerInvariant() } |
        Where-Object { $projects.ContainsKey($_) }
    $deployIds = @($ownerIds) + @($subjectIds) | Sort-Object -Unique
    foreach ($ownerId in $deployIds) {
        $buildLog = Join-Path $artifactRoot "deploy-$ownerId.log"
        $build = Invoke-BoundedProcess -Executable 'dotnet' -Arguments @(
            'build', [string]$projects[$ownerId], '--configuration', 'Release', '--nologo',
            '-p:DeployToGame=true', "-p:RimWorldPath=$game", "-p:RimWorldManagedPath=$managedPath",
            "-p:SteamModContentFolder=$workshop") -TimeoutMilliseconds 300000
        [System.IO.File]::WriteAllText(
            $buildLog, $build.StandardOutput + $build.StandardError,
            [System.Text.UTF8Encoding]::new($false))
        if ($build.ExitCode -ne 0) { throw "Product deployment failed for '$ownerId'. See $buildLog" }
        $deploymentEvidence.Add((Get-RimWorldDeployedProductEvidence `
            -PackageId $ownerId `
            -ProjectPath ([string]$projects[$ownerId]) `
            -RimWorldPath $game `
            -BuildLogPath $buildLog))
    }
    [System.IO.File]::WriteAllText(
        $deploymentPath,
        (ConvertTo-RimWorldProductEvidenceJson -Evidence @($deploymentEvidence)),
        [System.Text.UTF8Encoding]::new($false))

    $stageAttempted = $true
    $stage = Invoke-PerformanceHostOperation `
        -Operation 'performance-stage' `
        -HostProject $hostProject `
        -RepositoryRoot $repositoryRoot `
        -ModsRoot $modsRoot `
        -PackageIds $packages `
        -LeaseFile $leaseFile
    [System.IO.File]::WriteAllText(
        $stagePath, $stage.StandardOutput + $stage.StandardError,
        [System.Text.UTF8Encoding]::new($false))
    if ($stage.ExitCode -ne 0) { throw "Performance bundle staging failed. See $stagePath" }
    $stagePublished = $true

    foreach ($processPlan in @($plan.processes)) {
        $unexpectedProcesses = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
        if ($unexpectedProcesses.Count -ne 0) {
            $infrastructureFailure = "Fresh-process isolation failed before sequence $([int]$processPlan.sequence); RimWorld PID(s) still exist: $($unexpectedProcesses.Id -join ', ')."
            break
        }
        $processDirectory = [string]$processPlan.processDirectory
        $null = New-Item -Path $processDirectory -ItemType Directory -Force
        $smokeRoot = Join-Path $processDirectory 'smoke'
        $stdoutPath = Join-Path $processDirectory 'smoke.stdout.json'
        $stderrPath = Join-Path $processDirectory 'smoke.stderr.txt'
        $packageFile = Join-Path $processDirectory 'additional-mod-ids.txt'
        $additional = @($processPlan.activePackageIds | Where-Object {
            [string]$_ -ine 'ludeon.rimworld' -and
            [string]$_ -ine 'fumblesneeze.rimworlddevgateway'
        })
        [System.IO.File]::WriteAllLines(
            $packageFile, $additional, [System.Text.UTF8Encoding]::new($false))
        $smokeArguments = @(
            '-NoProfile', '-NonInteractive', '-File', $smokeScript,
            '-RimWorldPath', $game,
            '-SteamModContentFolder', $workshop,
            '-Quicktest', '-RunEndToEndTests', '-SkipBuildDeploy',
            '-EndToEndTestIds', [string]$processPlan.benchmarkId,
            '-AdditionalModIdsFile', $packageFile,
            '-PrelaunchConfigDirectory', $settingsInput,
            '-ArtifactsPath', $smokeRoot,
            '-TimeoutSeconds', [string]([Math]::Max(
                $TimeoutSeconds,
                ([int]$processPlan.maxWallClockSeconds + 180))),
            '-Output', 'json')
        try {
            $smokeTimeoutSeconds = [Math]::Max(
                $TimeoutSeconds,
                ([int]$processPlan.maxWallClockSeconds + 180))
            $smoke = Invoke-BoundedProcess `
                -Executable 'pwsh' `
                -Arguments $smokeArguments `
                -TimeoutMilliseconds ([Math]::Min(
                    [int]::MaxValue,
                    ([long]$smokeTimeoutSeconds + 180L) * 1000L))
            [System.IO.File]::WriteAllText(
                $stdoutPath, $smoke.StandardOutput, [System.Text.UTF8Encoding]::new($false))
            [System.IO.File]::WriteAllText(
                $stderrPath, $smoke.StandardError, [System.Text.UTF8Encoding]::new($false))
            if ($smoke.ExitCode -ne 0) {
                throw "RimWorld performance process exited $($smoke.ExitCode)."
            }
            $smokeResult = $smoke.StandardOutput | ConvertFrom-Json -ErrorAction Stop
            if ([string]$smokeResult.Status -ne 'passed') { throw 'Gateway smoke did not report passed.' }

            $normalizedSources = @(Get-ChildItem -LiteralPath $smokeRoot -Recurse -File -Filter 'performance.normalized.json')
            $inMemorySources = @(Get-ChildItem -LiteralPath $smokeRoot -Recurse -File -Filter 'circinus.in-memory.json')
            $persistedSources = @(Get-ChildItem -LiteralPath $smokeRoot -Recurse -File -Filter 'circinus.persisted.json')
            if ($normalizedSources.Count -ne 1 -or $inMemorySources.Count -ne 1 -or $persistedSources.Count -ne 1) {
                throw 'Performance process did not produce one exact normalized/in-memory/persisted artifact set.'
            }
            $raw = Copy-ExactPerformanceArtifact `
                -Source $inMemorySources[0].FullName `
                -Destination ([string]$processPlan.rawCircinusJsonPath)
            $persistedDestination = Join-Path $processDirectory 'circinus.persisted.json'
            $persisted = Copy-ExactPerformanceArtifact `
                -Source $persistedSources[0].FullName `
                -Destination $persistedDestination
            $normalizedCopy = Copy-ExactPerformanceArtifact `
                -Source $normalizedSources[0].FullName `
                -Destination ([string]$processPlan.normalizedJsonPath)
            $normalized = Get-Content -LiteralPath $normalizedCopy.Path -Raw | ConvertFrom-Json -ErrorAction Stop
            $rows = Write-PerformanceProcessReports -Normalized $normalized -ProcessPlan $processPlan
            $processResults.Add([pscustomobject]@{
                Sequence = [int]$processPlan.sequence
                BenchmarkId = [string]$processPlan.benchmarkId
                GroupId = [string]$processPlan.groupId
                EvidenceLens = [string]$normalized.evidenceLens
                Repetition = [int]$processPlan.repetition
                Status = 'passed'
                ActivePackageIds = @($processPlan.activePackageIds)
                SmokeResult = $smokeResult
                Raw = $raw
                Persisted = $persisted
                Normalized = $normalizedCopy
                Metrics = @($rows)
                StandardOutput = $stdoutPath
                StandardError = $stderrPath
            })
        }
        catch {
            $processResults.Add([pscustomobject]@{
                Sequence = [int]$processPlan.sequence
                BenchmarkId = [string]$processPlan.benchmarkId
                GroupId = [string]$processPlan.groupId
                EvidenceLens = [int]$processPlan.evidenceLens
                Repetition = [int]$processPlan.repetition
                Status = 'failed'
                ActivePackageIds = @($processPlan.activePackageIds)
                Failure = $_.Exception.Message
                StandardOutput = $stdoutPath
                StandardError = $stderrPath
            })
        }
        $unexpectedProcesses = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
        if ($unexpectedProcesses.Count -ne 0) {
            $infrastructureFailure = "Fresh-process cleanup failed after sequence $([int]$processPlan.sequence); RimWorld PID(s) remain: $($unexpectedProcesses.Id -join ', ')."
            break
        }
    }
}
catch {
    $infrastructureFailure = $_.Exception.Message
}
finally {
    try {
        $NormalCircinusSettingsAfter = Get-OptionalFileIdentity -Path $NormalCircinusSettingsPath
        $NormalCircinusSettingsUnchanged = Test-SameFileIdentity `
            -Before $NormalCircinusSettingsBefore `
            -After $NormalCircinusSettingsAfter
        if (-not $NormalCircinusSettingsUnchanged -and [string]::IsNullOrWhiteSpace($infrastructureFailure)) {
            $infrastructureFailure = 'The normal user Circinus settings changed during the isolated performance run.'
        }
    }
    catch {
        $NormalCircinusSettingsUnchanged = $false
        if ([string]::IsNullOrWhiteSpace($infrastructureFailure)) {
            $infrastructureFailure = "Could not verify the normal user Circinus settings: $($_.Exception.Message)"
        }
    }
    if (Test-Path -LiteralPath $leaseFile -PathType Leaf) {
        try {
            $clean = Invoke-PerformanceHostOperation `
                -Operation 'clean' `
                -HostProject $hostProject `
                -RepositoryRoot $repositoryRoot `
                -ModsRoot $modsRoot `
                -PackageIds $packages `
                -LeaseFile $leaseFile
            [System.IO.File]::WriteAllText(
                $stageCleanupPath, $clean.StandardOutput + $clean.StandardError,
                [System.Text.UTF8Encoding]::new($false))
            $stageCleaned = $clean.ExitCode -eq 0 -and -not (Test-Path -LiteralPath $leaseFile)
        }
        catch {
            $stageCleaned = $false
            if ([string]::IsNullOrWhiteSpace($infrastructureFailure)) {
                $infrastructureFailure = "Performance stage cleanup failed: $($_.Exception.Message)"
            }
        }
    }
    elseif ($stageAttempted -or -not $stagePublished) {
        $stageCleaned = $true
    }
}

$allPassed = $processResults.Count -eq @($plan.processes).Count -and
    @($processResults | Where-Object { [string]$_.Status -ne 'passed' }).Count -eq 0
$passed = $allPassed -and $stageCleaned -and [string]::IsNullOrWhiteSpace($infrastructureFailure)
if ($passed) {
    try {
        $currentSnapshot = New-PerformanceCurrentSnapshot `
            -Plan $plan `
            -ProcessResults @($processResults) `
            -DeploymentEvidence @($deploymentEvidence) `
            -Projects $projects `
            -CircinusAssemblyIdentity $circinusAssemblyIdentity `
            -SettingsSha256 ((Get-FileHash -LiteralPath $settingsPath -Algorithm SHA256).Hash) `
            -CreatedUtc ([datetime]::UtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)) `
            -InfrastructureFailure $infrastructureFailure
        $currentSnapshot | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $currentSnapshotPath -Encoding utf8
        $baselineRun = if ($CreateBaselineCandidate) {
            Invoke-PerformanceBaselineOperation `
                -HostProject $hostProject `
                -CurrentSnapshot $currentSnapshotPath `
                -CandidateOutput ([System.IO.Path]::GetFullPath($BaselineCandidatePath))
        }
        else {
            Invoke-PerformanceBaselineOperation `
                -HostProject $hostProject `
                -CurrentSnapshot $currentSnapshotPath `
                -BaselineDirectory ([System.IO.Path]::GetFullPath($BaselineDirectory)) `
                -Policy ([System.IO.Path]::GetFullPath($BaselinePolicyPath)) `
                -Report $baselineComparisonPath `
                -CrossVersion:$InformationalCrossVersion
        }
        if (-not [string]::IsNullOrWhiteSpace($baselineRun.StandardOutput)) {
            $baselineResult = $baselineRun.StandardOutput | ConvertFrom-Json -ErrorAction Stop
        }
        if ($baselineRun.ExitCode -ne 0) {
            $passed = $false
            if ($null -eq $baselineResult) {
                $infrastructureFailure = "Performance baseline operation failed: $($baselineRun.StandardError.Trim())"
            }
        }
    }
    catch {
        $passed = $false
        $infrastructureFailure = "Performance baseline operation failed: $($_.Exception.Message)"
    }
}
$performanceOutcome = if (-not $passed) {
    'failed'
}
elseif ($null -ne $baselineResult -and [string]$baselineResult.status -ceq 'informational') {
    'informational'
}
else {
    'passed'
}
$aggregateRows = @($processResults | Where-Object Status -eq 'passed' | ForEach-Object {
    $result = $_
    @($result.Metrics) | ForEach-Object {
        [pscustomobject]@{
            BenchmarkId = [string]$result.BenchmarkId
            EvidenceLens = [string]$result.EvidenceLens
            Repetition = [int]$result.Repetition
            Scope = [string]$_.Scope; Key = [string]$_.Key; Name = [string]$_.Name
            Value = [double]$_.Value; Unit = [string]$_.Unit
            Denominator = [string]$_.Denominator; Claim = [string]$_.Claim
        }
    }
})
@($aggregateRows) | Export-Csv -LiteralPath $aggregateCsvPath -NoTypeInformation -Encoding utf8
$markdown = @(
    '# RimWorld performance run', '',
    "- Run: $runId",
    "- Status: $performanceOutcome",
    "- Processes: $($processResults.Count)/$(@($plan.processes).Count)",
    "- Stage cleanup: $stageCleaned",
    "- Baseline: $(if ($null -eq $baselineResult) { 'not-run' } else { [string]$baselineResult.status })", '',
    'Circinus attribution is gross measurement. Instrumented, armed-disabled, and fully-disarmed lenses must be compared only within one compatible workload family.'
)
[System.IO.File]::WriteAllLines($aggregateMarkdownPath, $markdown, [System.Text.UTF8Encoding]::new($false))
$aggregate = [pscustomobject]@{
    Status = $performanceOutcome
    RunId = $runId
    RunDirectory = $artifactRoot
    AvailablePackageCount = $packages.Count
    StagePublished = $stagePublished
    StageCleaned = $stageCleaned
    InfrastructureFailure = $infrastructureFailure
    Baseline = [pscustomobject]@{
        Mode = if ($CreateBaselineCandidate) { 'candidate' } else { 'compare' }
        CurrentSnapshot = $currentSnapshotPath
        AcceptedDirectory = [System.IO.Path]::GetFullPath($BaselineDirectory)
        ThresholdPolicy = [System.IO.Path]::GetFullPath($BaselinePolicyPath)
        Candidate = if ($CreateBaselineCandidate) { [System.IO.Path]::GetFullPath($BaselineCandidatePath) } else { $null }
        Comparison = if (-not $CreateBaselineCandidate) { $baselineComparisonPath } else { $null }
        Result = $baselineResult
    }
    CircinusSettings = [pscustomobject]@{
        Path = $settingsPath
        Sha256 = (Get-FileHash -LiteralPath $settingsPath -Algorithm SHA256).Hash
        LocalOnly = $true
        NormalPath = $NormalCircinusSettingsPath
        NormalBefore = $NormalCircinusSettingsBefore
        NormalAfter = $NormalCircinusSettingsAfter
        NormalUnchanged = $NormalCircinusSettingsUnchanged
    }
    ProductDeployment = $deploymentPath
    Processes = @($processResults)
    Reports = [pscustomobject]@{
        AggregateJson = $aggregateJsonPath
        AggregateCsv = $aggregateCsvPath
        SummaryMarkdown = $aggregateMarkdownPath
        CurrentSnapshot = $currentSnapshotPath
        BaselineComparison = if (-not $CreateBaselineCandidate) { $baselineComparisonPath } else { $null }
    }
}
$aggregate | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $aggregateJsonPath -Encoding utf8
if (-not $passed) {
    [Console]::Error.WriteLine("Performance run failed. See $aggregateJsonPath")
    exit 1
}
$result = [pscustomobject]@{
    Status = $performanceOutcome
    RunId = $runId
    RunDirectory = $artifactRoot
    ProcessCount = $processResults.Count
    StageCleaned = $stageCleaned
    Aggregate = $aggregateJsonPath
    Csv = $aggregateCsvPath
    Summary = $aggregateMarkdownPath
    Baseline = $baselineResult
}
if ($Output -eq 'json') { $result | ConvertTo-Json -Depth 8 -Compress }
else { $result | Format-List }
exit 0
