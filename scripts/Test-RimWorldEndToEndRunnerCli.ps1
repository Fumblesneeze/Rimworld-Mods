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
    -Output json
if ($LASTEXITCODE -ne 0) {
    throw "Grouped E2E runner dry run exited $LASTEXITCODE."
}
$result = $raw | ConvertFrom-Json -ErrorAction Stop
$groups = @($result.Groups)
$coreGroups = @($groups | Where-Object { [string]$_.GroupId -ceq 'ludeon.rimworld' })
if ([string]$result.Status -cne 'dry-run' -or
    [bool]$result.MutatedGame -or
    $coreGroups.Count -ne 1) {
    throw 'Grouped E2E runner returned an invalid Core dry-run plan.'
}
$coreGroup = $coreGroups[0]
foreach ($testId in $ExpectedTestIds) {
    if (@($coreGroup.Tests) -cnotcontains $testId) {
        throw "Grouped E2E dry run omitted expected test '$testId'."
    }
}
$dependencyFirstGroupId = 'brrainz.harmony|ludeon.rimworld|imranfish.xmlextensions|oskarpotocki.vanillafactionsexpanded.core|vanillaexpanded.vcooke|vanillaexpanded.vcookebakery|vanillaexpanded.vcookehaute|vanillaexpanded.vcookestews|vanillaexpanded.vcef|vanillaexpanded.vcookesushi|ucp.friedmeals|rabiosus.adaptivemealbill|binchcannon.overcookedmeals|fumblesneeze.immersivechefs'
$dependencyFirstGroups = @($groups | Where-Object {
    [string]$_.GroupId -ceq $dependencyFirstGroupId
})
if ($dependencyFirstGroups.Count -ne 1) {
    throw 'Grouped E2E dry run did not resolve the dependency-first VCE/Fried About metadata.'
}
foreach ($testId in @(
    'immersive-chefs.adaptive-overcooked-final-product',
    'immersive-chefs.vce-fried-native-cooking')) {
    if (@($dependencyFirstGroups[0].Tests) -cnotcontains $testId) {
        throw "Dependency-first VCE/Fried group omitted expected test '$testId'."
    }
}

$command = @($coreGroup.Command)
foreach ($requiredArgument in @('-Quicktest', '-RunEndToEndTests', '-SkipBuildDeploy')) {
    if ($command -cnotcontains $requiredArgument) {
        throw "Grouped E2E dry-run command omitted '$requiredArgument'."
    }
}

$focusedTestId = 'immersive-chefs.countertop-microwave-support-loss'
$focusedRaw = & pwsh `
    -NoProfile `
    -NonInteractive `
    -File $runner `
    -DryRun `
    -TestId $focusedTestId `
    -Output json
if ($LASTEXITCODE -ne 0) {
    throw "Focused E2E runner dry run exited $LASTEXITCODE."
}
$focusedResult = $focusedRaw | ConvertFrom-Json -ErrorAction Stop
$focusedGroups = @($focusedResult.Groups)
if ($focusedGroups.Count -ne 1 -or
    @($focusedGroups[0].Tests).Count -ne 1 -or
    [string]$focusedGroups[0].Tests[0] -cne $focusedTestId) {
    throw 'Focused E2E dry run did not select exactly the requested test.'
}
if (@($focusedGroups[0].Command) -cnotcontains '-EndToEndTestIds') {
    throw 'Focused E2E dry-run command omitted its runtime test selection.'
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
    Tests = @($coreGroup.Tests)
    DependencyFirstGroupTests = @($dependencyFirstGroups[0].Tests)
    FocusedTest = [string]$focusedGroups[0].Tests[0]
    MutatedGame = [bool]$result.MutatedGame
} | ConvertTo-Json -Compress
