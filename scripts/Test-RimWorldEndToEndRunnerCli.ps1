<#
.SYNOPSIS
Checks the grouped RimWorld E2E runner's real dry-run CLI contract.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..'),

    [string[]]$ExpectedTestIds = @(
        'gateway.semantic-loop.first',
        'gateway.semantic-loop.second'
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$runner = Join-Path $root 'scripts\Invoke-RimWorldEndToEndTests.ps1'
$stagePath = 'F:\Steam\steamapps\common\RimWorld\Mods\fumblesneeze.rimworlddevgateway\1.6\DevEndToEndTests'
$stageExistedBefore = Test-Path -LiteralPath $stagePath
$processIdsBefore = @(Get-Process -Name RimWorldWin64 -ErrorAction SilentlyContinue |
    ForEach-Object { $_.Id })

$raw = & pwsh `
    -NoProfile `
    -NonInteractive `
    -File $runner `
    -DryRun `
    -GroupId ludeon.rimworld `
    -Output json
if ($LASTEXITCODE -ne 0) {
    throw "Grouped E2E runner dry run exited $LASTEXITCODE."
}
$result = $raw | ConvertFrom-Json -ErrorAction Stop
$groups = @($result.Groups)
if ([string]$result.Status -cne 'dry-run' -or
    [bool]$result.MutatedGame -or
    $groups.Count -ne 1 -or
    [string]$groups[0].GroupId -cne 'ludeon.rimworld') {
    throw 'Grouped E2E runner returned an invalid Core dry-run plan.'
}
foreach ($testId in $ExpectedTestIds) {
    if (@($groups[0].Tests) -cnotcontains $testId) {
        throw "Grouped E2E dry run omitted expected test '$testId'."
    }
}
$command = @($groups[0].Command)
foreach ($requiredArgument in @('-Quicktest', '-RunEndToEndTests', '-SkipBuildDeploy')) {
    if ($command -cnotcontains $requiredArgument) {
        throw "Grouped E2E dry-run command omitted '$requiredArgument'."
    }
}

$stageExistsAfter = Test-Path -LiteralPath $stagePath
$processIdsAfter = @(Get-Process -Name RimWorldWin64 -ErrorAction SilentlyContinue |
    ForEach-Object { $_.Id })
if ($stageExistedBefore -ne $stageExistsAfter) {
    throw 'Grouped E2E dry run changed the installed test-stage state.'
}
if (@(Compare-Object -ReferenceObject $processIdsBefore -DifferenceObject $processIdsAfter).Count -ne 0) {
    throw 'Grouped E2E dry run changed the running RimWorld process set.'
}

[pscustomobject]@{
    Status = 'passed'
    AvailablePackageCount = [int]$result.AvailablePackageCount
    GroupCount = $groups.Count
    Tests = @($groups[0].Tests)
    MutatedGame = [bool]$result.MutatedGame
} | ConvertTo-Json -Compress
