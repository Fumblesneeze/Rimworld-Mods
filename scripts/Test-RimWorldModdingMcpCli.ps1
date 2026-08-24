[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [switch]$ExpectFailure
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

$project = Join-Path $RepositoryRoot 'tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    if ($ExpectFailure) {
        Write-Output '{"status":"expected-red","reason":"tool-project-missing"}'
        exit 0
    }

    Write-Error "RimWorldModding.Mcp project is missing: $project"
    exit 1
}

$json = & dotnet run --project $project -- tool call repository_status --repository-root $RepositoryRoot --arguments '{}' --output json
if ($LASTEXITCODE -ne 0) {
    throw "repository_status CLI call failed with exit code $LASTEXITCODE."
}

$status = $json | ConvertFrom-Json
if ($status.repositoryRoot -ne (Resolve-Path -LiteralPath $RepositoryRoot).Path) {
    throw 'repository_status returned the wrong repository root.'
}

$table = & dotnet run --project $project -- tool list --repository-root $RepositoryRoot --output table
if ($LASTEXITCODE -ne 0 -or ($table -join "`n") -notmatch 'repository_status') {
    throw 'tool list table output did not contain repository_status.'
}

$savedErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $invalidOutput = & dotnet run --project $project -- tool call repository_status --repository-root $RepositoryRoot --arguments '{}' --output yaml 2>&1
}
finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($LASTEXITCODE -ne 2 -or ($invalidOutput -join "`n") -notmatch 'output') {
    throw 'invalid output format did not return exit code 2 and an actionable diagnostic.'
}

$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = 'dotnet'
$psi.ArgumentList.Add('run')
$psi.ArgumentList.Add('--project')
$psi.ArgumentList.Add($project)
$psi.ArgumentList.Add('--')
$psi.ArgumentList.Add('serve')
$psi.ArgumentList.Add('--repository-root')
$psi.ArgumentList.Add($RepositoryRoot)
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $psi
if (-not $process.Start()) {
    throw 'Could not start the MCP server.'
}

try {
    $initialize = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"rimworld-mcp-test","version":"1.0"}}}'
    $list = '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
    $call = '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"repository_status","arguments":{}}}'

    $process.StandardInput.WriteLine($initialize)
    $process.StandardInput.Flush()
    $initializeResult = $process.StandardOutput.ReadLine()
    if ($initializeResult -notmatch '"id":1') {
        throw "MCP initialize failed: $initializeResult"
    }

    $process.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}')
    $process.StandardInput.WriteLine($list)
    $process.StandardInput.Flush()
    $listResult = $process.StandardOutput.ReadLine()
    if ($listResult -notmatch 'repository_status') {
        throw "MCP tools/list omitted repository_status: $listResult"
    }

    $process.StandardInput.WriteLine($call)
    $process.StandardInput.Flush()
    $callResult = $process.StandardOutput.ReadLine()
    if ($callResult -notmatch 'fumblesneeze.guestbedgizmo') {
        throw "MCP tools/call returned the wrong repository payload: $callResult"
    }
}
finally {
    try { $process.StandardInput.Close() } catch {}
    if (-not $process.WaitForExit(5000)) {
        $process.Kill($true)
        $process.WaitForExit()
    }
    $process.Dispose()
}

Write-Output '{"status":"passed","operations":["repository_status","operation_list"],"transports":["cli","stdio-mcp"]}'
exit 0
