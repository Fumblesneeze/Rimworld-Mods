[CmdletBinding()]
param(
    [string]$RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')),
    [string]$ModsRoot = 'F:\Steam\steamapps\common\RimWorld\Mods',
    [string]$RimWorldVersion = '1.6'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$project = Join-Path $RepositoryRoot 'tools\RimWorldDevGateway.EndToEndHost\RimWorldDevGateway.EndToEndHost.csproj'
$arguments = @(
    'run', '--project', $project, '--configuration', 'Release', '--',
    'plan',
    '--repository-root', $RepositoryRoot,
    '--mods-root', $ModsRoot,
    '--rimworld-version', $RimWorldVersion,
    '--package-id', 'ludeon.rimworld',
    '--package-id', 'fumblesneeze.rimworlddevgateway',
    '--output', 'json'
)
$lines = @(& dotnet @arguments 2>&1)
if ($LASTEXITCODE -ne 0) {
    throw "E2E host plan failed:`n$($lines -join [Environment]::NewLine)"
}

$payload = ($lines -join [Environment]::NewLine) | ConvertFrom-Json
if ([string]$payload.status -ne 'planned' -or @($payload.groups).Count -lt 1) {
    throw 'E2E host plan did not return at least one valid group.'
}

$payload
