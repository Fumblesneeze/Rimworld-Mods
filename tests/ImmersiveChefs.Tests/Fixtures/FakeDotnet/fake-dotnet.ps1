Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dotNetArguments = @($args)

function Get-ArgumentValue {
    param([string]$Name)

    for ($index = 0; $index -lt $dotNetArguments.Count - 1; $index++) {
        if ($dotNetArguments[$index] -eq $Name) {
            return $dotNetArguments[$index + 1]
        }
    }

    throw "Fake dotnet did not receive required argument '$Name'."
}

$logger = Get-ArgumentValue '--logger'
$resultsDirectory = Get-ArgumentValue '--results-directory'
$trxName = ($logger -split 'LogFileName=', 2)[1]
$suiteName = [System.IO.Path]::GetFileNameWithoutExtension($trxName)
$mode = $env:IC_TEST_FAKE_DOTNET_MODE
$journalPath = $env:IC_TEST_FAKE_DOTNET_JOURNAL

if ([string]::IsNullOrWhiteSpace($journalPath)) {
    throw 'IC_TEST_FAKE_DOTNET_JOURNAL is required.'
}

[System.IO.File]::AppendAllText($journalPath, "$suiteName$([Environment]::NewLine)")

$total = 1
$executed = 1
$passed = 1
$failed = 0
$notExecuted = 0
$exitCode = 0

if ($mode -eq 'early-failure' -and $suiteName -eq 'ImmersiveChefs.Unit') {
    $passed = 0
    $failed = 1
    $exitCode = 1
}
elseif ($mode -eq 'zero-executed') {
    $executed = 0
    $passed = 0
    $notExecuted = 1
}

$trxPath = Join-Path $resultsDirectory $trxName
$trx = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary>
    <Counters total="$total" executed="$executed" passed="$passed" failed="$failed" notExecuted="$notExecuted" />
  </ResultSummary>
</TestRun>
"@
[System.IO.File]::WriteAllText($trxPath, $trx, [System.Text.UTF8Encoding]::new($false))
Write-Output "Fake dotnet emitted $suiteName ($mode)."
exit $exitCode
