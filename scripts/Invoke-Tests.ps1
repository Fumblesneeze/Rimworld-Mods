<#
.SYNOPSIS
Runs repository RimWorld mod tests and rejects false-green zero-test runs.

.DESCRIPTION
Runs one or every framework-backed NUnit suite, writes per-suite TRX/log artifacts, and validates
each result counter independently. Grouped selections continue after a failed project so every
environment produces a result. Exit code 0 means every selected suite executed at least one test
and none failed; 1 means test/runtime failure; 2 means a path or semantic input rejected after
parameter binding. PowerShell rejects invalid ValidateSet values before the script runs and reports
its own nonzero parameter-binding exit (normally 1).

.EXAMPLE
.\scripts\Invoke-Tests.ps1

.EXAMPLE
.\scripts\Invoke-Tests.ps1 -Suite RimWorldDevGateway -Configuration Release -Output json

.EXAMPLE
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs

.EXAMPLE
.\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Harmony -HarmonyAssemblyPath 'F:\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll'
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [ValidateSet('All', 'ImmersiveChefs', 'ImmersiveChefs.Unit', 'ImmersiveChefs.Harmony', 'ImmersiveChefs.Defs', 'RimWorldDevGateway', 'RimWorldDevGateway.Snapshots')]
    [string]$Suite = 'All',

    [string]$HarmonyAssemblyPath,

    [string]$TestFilter,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Result {
    param([pscustomobject]$Result)

    if ($Output -eq 'json') {
        $Result | ConvertTo-Json -Compress -Depth 5
        return
    }

    $Result.Suites |
        Select-Object Suite, Status, Passed, Failed, NotExecuted, Executed, Total, Framework |
        Format-Table -AutoSize
    Write-Host (
        "Total: {0} passed, {1} failed, {2} not executed, {3} executed ({4} discovered)" -f
        $Result.Passed,
        $Result.Failed,
        $Result.NotExecuted,
        $Result.Executed,
        $Result.Total)
    Write-Host ("Artifacts: {0}" -f $Result.ResultsDirectory)
}

function Get-TrxCounter {
    param(
        [System.Xml.XmlElement]$Counters,
        [string]$Name
    )

    $value = $Counters.GetAttribute($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return 0
    }

    return [int]::Parse($value, [Globalization.CultureInfo]::InvariantCulture)
}

function Exit-InvalidInput {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

try {
    if (-not (Test-Path -LiteralPath $RimWorldPath -PathType Container)) {
        Exit-InvalidInput "RimWorld path does not exist: $RimWorldPath"
    }

    if (-not (Test-Path -LiteralPath $SteamModContentFolder -PathType Container)) {
        Exit-InvalidInput "Steam Workshop content path does not exist: $SteamModContentFolder"
    }

    $resolvedRimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
    $resolvedWorkshopPath = (Resolve-Path -LiteralPath $SteamModContentFolder).Path
    $managedPath = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed'
    $assemblyPath = Join-Path $managedPath 'Assembly-CSharp.dll'

    if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
        Exit-InvalidInput "RimWorld managed assembly does not exist: $assemblyPath"
    }

    $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $availableSuites = @(
        [pscustomobject]@{
            Name = 'ImmersiveChefs.Unit'
            Group = 'ImmersiveChefs'
            Project = Join-Path $repositoryRoot 'tests\ImmersiveChefs.Tests\ImmersiveChefs.Tests.csproj'
        },
        [pscustomobject]@{
            Name = 'ImmersiveChefs.Harmony'
            Group = 'ImmersiveChefs'
            Project = Join-Path $repositoryRoot 'tests\ImmersiveChefs.Harmony.Tests\ImmersiveChefs.Harmony.Tests.csproj'
        },
        [pscustomobject]@{
            Name = 'ImmersiveChefs.Defs'
            Group = 'ImmersiveChefs'
            Project = Join-Path $repositoryRoot 'tests\ImmersiveChefs.Defs.Tests\ImmersiveChefs.Defs.Tests.csproj'
        },
        [pscustomobject]@{
            Name = 'RimWorldDevGateway'
            Group = 'RimWorldDevGateway'
            Project = Join-Path $repositoryRoot 'tests\RimWorldDevGateway.Tests\RimWorldDevGateway.Tests.csproj'
        }
        [pscustomobject]@{
            Name = 'RimWorldDevGateway.Snapshots'
            Group = 'RimWorldDevGateway'
            Project = Join-Path $repositoryRoot 'tests\RimWorldDevGateway.Snapshots.Tests\RimWorldDevGateway.Snapshots.Tests.csproj'
        }
    )
    $selectedSuites = if ($Suite -eq 'All') {
        @($availableSuites)
    }
    elseif ($Suite -eq 'ImmersiveChefs') {
        @($availableSuites | Where-Object Group -EQ 'ImmersiveChefs')
    }
    else {
        @($availableSuites | Where-Object Name -EQ $Suite)
    }

    if ($selectedSuites.Count -eq 0) {
        Exit-InvalidInput "No test project is registered for suite '$Suite'."
    }

    if (-not [string]::IsNullOrWhiteSpace($TestFilter) -and $selectedSuites.Count -ne 1) {
        Exit-InvalidInput "TestFilter requires an exact suite selection; '$Suite' selects $($selectedSuites.Count) projects."
    }

    foreach ($selected in $selectedSuites) {
        if (-not (Test-Path -LiteralPath $selected.Project -PathType Leaf)) {
            Exit-InvalidInput "Test project does not exist: $($selected.Project)"
        }
    }

    $resolvedHarmonyAssemblyPath = $null
    $harmonyDependency = $null
    if (@($selectedSuites | Where-Object Name -EQ 'ImmersiveChefs.Harmony').Count -gt 0) {
        $candidateHarmonyPath = if ([string]::IsNullOrWhiteSpace($HarmonyAssemblyPath)) {
            Join-Path $resolvedWorkshopPath '2009463077\Current\Assemblies\0Harmony.dll'
        }
        else {
            $HarmonyAssemblyPath
        }

        if (-not (Test-Path -LiteralPath $candidateHarmonyPath -PathType Leaf)) {
            Exit-InvalidInput "Harmony test assembly does not exist: $candidateHarmonyPath"
        }

        $resolvedHarmonyAssemblyPath = (Resolve-Path -LiteralPath $candidateHarmonyPath).Path
        try {
            $harmonyIdentity = [Reflection.AssemblyName]::GetAssemblyName($resolvedHarmonyAssemblyPath)
            if ($harmonyIdentity.Name -ne '0Harmony') {
                Exit-InvalidInput "Harmony test assembly has simple name '$($harmonyIdentity.Name)', expected '0Harmony': $resolvedHarmonyAssemblyPath"
            }

            $harmonyLoadedForIdentity = [Reflection.Assembly]::LoadFile($resolvedHarmonyAssemblyPath)
            $harmonyDependency = [pscustomobject]@{
                Name = $harmonyIdentity.Name
                Version = $harmonyIdentity.Version.ToString()
                Mvid = $harmonyLoadedForIdentity.ManifestModule.ModuleVersionId.ToString('D')
                Sha256 = (Get-FileHash -LiteralPath $resolvedHarmonyAssemblyPath -Algorithm SHA256).Hash
                SourcePath = $resolvedHarmonyAssemblyPath
            }
        }
        catch {
            Exit-InvalidInput "Harmony test assembly identity could not be read: $resolvedHarmonyAssemblyPath. $($_.Exception.Message)"
        }
    }

    $runId = "{0}-{1}-{2}" -f
        [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture),
        $PID,
        [Guid]::NewGuid().ToString('N')
    $resultsDirectory = Join-Path $repositoryRoot "artifacts\TestResults\$runId"
    $null = New-Item -Path $resultsDirectory -ItemType Directory
    $suiteResults = [System.Collections.Generic.List[object]]::new()
    $failureMessages = [System.Collections.Generic.List[string]]::new()

    $harmonyDependencyEvidencePath = $null
    if ($null -ne $harmonyDependency) {
        $harmonyDependencyEvidencePath = Join-Path $resultsDirectory 'ImmersiveChefs.Harmony.dependencies.json'
        $harmonyDependency |
            ConvertTo-Json -Depth 3 |
            Set-Content -LiteralPath $harmonyDependencyEvidencePath -Encoding UTF8
    }

    foreach ($selected in $selectedSuites) {
        $trxName = "$($selected.Name).trx"
        $trxPath = Join-Path $resultsDirectory $trxName
        $logPath = Join-Path $resultsDirectory "$($selected.Name).dotnet-test.log"
        $dotnetOutput = @()
        $dotnetExitCode = -1
        $total = 0
        $executed = 0
        $passed = 0
        $failed = 0
        $notExecuted = 0
        $suiteFailure = $null
        $arguments = @(
            'test',
            $selected.Project,
            '--configuration', $Configuration,
            '--logger', "trx;LogFileName=$trxName",
            '--results-directory', $resultsDirectory,
            "-p:RimWorldPath=$resolvedRimWorldPath",
            "-p:RimWorldManagedPath=$managedPath",
            "-p:SteamModContentFolder=$resolvedWorkshopPath"
        )

        if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
            $arguments += @('--filter', $TestFilter)
        }

        if ($selected.Name -eq 'ImmersiveChefs.Harmony') {
            $arguments += "-p:HarmonyAssemblyPath=$resolvedHarmonyAssemblyPath"
        }

        try {
            $dotnetOutput = @(& dotnet @arguments 2>&1)
            $dotnetExitCode = $LASTEXITCODE

            if (-not (Test-Path -LiteralPath $trxPath -PathType Leaf)) {
                throw "Suite '$($selected.Name)' did not produce a TRX result. See $logPath"
            }

            [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
            $counters = $trx.TestRun.ResultSummary.Counters
            if ($null -eq $counters) {
                throw "Suite '$($selected.Name)' has no TRX counters. See $trxPath"
            }

            $total = Get-TrxCounter -Counters $counters -Name 'total'
            $executed = Get-TrxCounter -Counters $counters -Name 'executed'
            $passed = Get-TrxCounter -Counters $counters -Name 'passed'
            $failed = Get-TrxCounter -Counters $counters -Name 'failed'
            # VSTest/NUnit can report an ignored test as total=1, executed=0, notExecuted=0.
            # The discovered-minus-executed difference is the reliable aggregate here.
            $notExecuted = [Math]::Max(
                $total - $executed,
                (Get-TrxCounter -Counters $counters -Name 'notExecuted'))

            if ($executed -eq 0) {
                $suiteFailure = "Suite '$($selected.Name)' executed no tests ($total discovered, $notExecuted not executed). This is a failed verification. See $logPath"
            }
            elseif ($dotnetExitCode -ne 0 -or $failed -ne 0) {
                $suiteFailure = "Suite '$($selected.Name)' failed (dotnet exit $dotnetExitCode, failed $failed/$executed executed). See $logPath"
            }
        }
        catch {
            $suiteFailure = $_.Exception.Message
        }
        try {
            $dotnetOutput | Set-Content -LiteralPath $logPath -Encoding UTF8
        }
        catch {
            $artifactFailure = "Suite '$($selected.Name)' could not write its dotnet log '$logPath': $($_.Exception.Message)"
            $suiteFailure = if ($null -eq $suiteFailure) {
                $artifactFailure
            }
            else {
                "$suiteFailure $artifactFailure"
            }
        }

        $suiteResults.Add([pscustomobject]@{
            Suite = $selected.Name
            Status = if ($null -eq $suiteFailure) { 'passed' } else { 'failed' }
            Total = $total
            Executed = $executed
            Passed = $passed
            Failed = $failed
            NotExecuted = $notExecuted
            Framework = '.NET Framework 4.8'
            Results = $trxPath
            Log = $logPath
            DependencyEvidence = if ($selected.Name -eq 'ImmersiveChefs.Harmony') { $harmonyDependencyEvidencePath } else { $null }
            ExitCode = $dotnetExitCode
            Failure = $suiteFailure
        })

        if ($null -ne $suiteFailure) {
            $failureMessages.Add($suiteFailure)
        }
    }

    $totalDiscovered = ($suiteResults | Measure-Object -Property Total -Sum).Sum
    $totalExecuted = ($suiteResults | Measure-Object -Property Executed -Sum).Sum
    $totalPassed = ($suiteResults | Measure-Object -Property Passed -Sum).Sum
    $totalFailed = ($suiteResults | Measure-Object -Property Failed -Sum).Sum
    $totalNotExecuted = ($suiteResults | Measure-Object -Property NotExecuted -Sum).Sum
    $result = [pscustomobject]@{
        Status = if ($failureMessages.Count -eq 0) { 'passed' } else { 'failed' }
        SuiteSelection = $Suite
        Total = $totalDiscovered
        Executed = $totalExecuted
        Passed = $totalPassed
        Failed = $totalFailed
        NotExecuted = $totalNotExecuted
        ResultsDirectory = $resultsDirectory
        Suites = @($suiteResults)
    }
    Write-Result $result

    if ($failureMessages.Count -gt 0) {
        [Console]::Error.WriteLine(($failureMessages -join [Environment]::NewLine))
        exit 1
    }

    exit 0
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
