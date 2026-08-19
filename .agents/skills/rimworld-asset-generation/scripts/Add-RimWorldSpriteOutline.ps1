<#
.SYNOPSIS
Adds a deterministic Core-style exterior contour to a RimWorld PNG.

.DESCRIPTION
Invokes the bundled Pillow processor. Existing nontransparent RGBA pixels are preserved exactly;
new pixels use a rounded Euclidean contour rasterized at 4x source resolution and filtered down once,
and remain limited to originally transparent, canvas-edge-connected background. The required baseline
and topology approval manifest validate source identity, class policy, components, holes, protected
gap runs, and an optional asset-specific outline color.
When a Stuff mask is supplied, its original pixels are preserved and new contour pixels are black.

Exit code 0 is success, 2 is an admitted invalid value or missing prerequisite, and 1 is a
processing/approval failure. PowerShell itself returns 1 before this script runs for command-line
binding errors such as an unknown parameter.

.EXAMPLE
& .\Add-RimWorldSpriteOutline.ps1 -InputPath .\Bench_north.png `
  -OutputPath .\Bench_north.out.png -StrokePixels 8 `
  -Baseline .\docs\SpriteOutlineBaseline.xml `
  -TopologyManifest .\docs\SpriteOutlineApprovals.xml -AssetId BenchNorth `
  -MaskInputPath .\Bench_north_m.png -MaskOutputPath .\Bench_north.out_m.png -Output json
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$InputPath,

    [Parameter(Position = 1)]
    [string]$OutputPath,

    [string]$StrokePixels,

    [string]$OutlineColor = '#17130F',

    [string]$Baseline,

    [string]$TopologyManifest,

    [string]$AssetId,

    [string]$MaskInputPath,

    [string]$MaskOutputPath,

    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput {
    param([Parameter(Mandatory)][string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

try {
    if ([string]::IsNullOrWhiteSpace($InputPath) -or
        [string]::IsNullOrWhiteSpace($OutputPath)) {
        Exit-InvalidInput 'InputPath and OutputPath are required.'
    }
    $parsedStrokePixels = 0
    if (-not [int]::TryParse($StrokePixels, [ref]$parsedStrokePixels) -or
        $parsedStrokePixels -lt 1 -or
        $parsedStrokePixels -gt 64) {
        Exit-InvalidInput 'StrokePixels must be an integer from 1 through 64.'
    }
    if ($OutlineColor -notmatch '^#[0-9A-Fa-f]{6}$') {
        Exit-InvalidInput 'OutlineColor must use #RRGGBB syntax.'
    }
    if ($Output -notin @('table', 'json')) {
        Exit-InvalidInput 'Output must be table or json.'
    }
    if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
        Exit-InvalidInput "Input PNG does not exist: $InputPath"
    }
    if ([IO.Path]::GetExtension($InputPath) -ine '.png' -or
        [IO.Path]::GetExtension($OutputPath) -ine '.png') {
        Exit-InvalidInput 'InputPath and OutputPath must both name PNG files.'
    }
    $resolvedInputPath = (Resolve-Path -LiteralPath $InputPath).Path
    $resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
    if ($resolvedInputPath -eq $resolvedOutputPath) {
        Exit-InvalidInput 'OutputPath must differ from InputPath; promote a reviewed candidate explicitly.'
    }
    $hasBaseline = -not [string]::IsNullOrWhiteSpace($Baseline)
    $hasManifest = -not [string]::IsNullOrWhiteSpace($TopologyManifest)
    $hasAssetId = -not [string]::IsNullOrWhiteSpace($AssetId)
    if (-not ($hasBaseline -and $hasManifest -and $hasAssetId)) {
        Exit-InvalidInput 'Baseline, TopologyManifest, and AssetId are required.'
    }
    if (-not [string]::IsNullOrWhiteSpace($Baseline) -and
        -not (Test-Path -LiteralPath $Baseline -PathType Leaf)) {
        Exit-InvalidInput "Outline baseline does not exist: $Baseline"
    }
    if (-not [string]::IsNullOrWhiteSpace($TopologyManifest) -and
        -not (Test-Path -LiteralPath $TopologyManifest -PathType Leaf)) {
        Exit-InvalidInput "Topology approval manifest does not exist: $TopologyManifest"
    }
    if ([string]::IsNullOrWhiteSpace($MaskInputPath) -xor
        [string]::IsNullOrWhiteSpace($MaskOutputPath)) {
        Exit-InvalidInput 'MaskInputPath and MaskOutputPath must be supplied together.'
    }
    if (-not [string]::IsNullOrWhiteSpace($MaskInputPath)) {
        if (-not (Test-Path -LiteralPath $MaskInputPath -PathType Leaf)) {
            Exit-InvalidInput "Mask PNG does not exist: $MaskInputPath"
        }
        if ([IO.Path]::GetExtension($MaskInputPath) -ine '.png' -or
            [IO.Path]::GetExtension($MaskOutputPath) -ine '.png') {
            Exit-InvalidInput 'MaskInputPath and MaskOutputPath must both name PNG files.'
        }
        $resolvedMaskInputPath = (Resolve-Path -LiteralPath $MaskInputPath).Path
        $resolvedMaskOutputPath = [IO.Path]::GetFullPath($MaskOutputPath)
        if ($resolvedMaskInputPath -eq $resolvedMaskOutputPath) {
            Exit-InvalidInput 'MaskOutputPath must differ from MaskInputPath.'
        }
    }

    $imagePaths = @($resolvedInputPath, $resolvedOutputPath)
    if (-not [string]::IsNullOrWhiteSpace($MaskInputPath)) {
        $imagePaths += @($resolvedMaskInputPath, $resolvedMaskOutputPath)
    }
    $uniquePaths = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)
    foreach ($imagePath in $imagePaths) {
        if (-not $uniquePaths.Add($imagePath)) {
            Exit-InvalidInput 'Diffuse and mask input/output paths must all be distinct.'
        }
    }
    if (Test-Path -LiteralPath $resolvedOutputPath) {
        Exit-InvalidInput "OutputPath already exists: $resolvedOutputPath"
    }
    if (-not [string]::IsNullOrWhiteSpace($MaskOutputPath) -and
        (Test-Path -LiteralPath $resolvedMaskOutputPath)) {
        Exit-InvalidInput "MaskOutputPath already exists: $resolvedMaskOutputPath"
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        Exit-InvalidInput 'Python 3 with Pillow 12.2.0 is required, but `python` is not available on PATH.'
    }
    $processor = Join-Path $PSScriptRoot 'add_rimworld_sprite_outline.py'
    if (-not (Test-Path -LiteralPath $processor -PathType Leaf)) {
        throw "Bundled outline processor is missing: $processor"
    }

    $arguments = @(
        $processor,
        '--input', $resolvedInputPath,
        '--output', $resolvedOutputPath,
        '--stroke-pixels', [string]$parsedStrokePixels,
        '--outline-color', $OutlineColor,
        '--output-mode', $Output
    )
    if (-not [string]::IsNullOrWhiteSpace($TopologyManifest)) {
        $arguments += @(
            '--baseline', (Resolve-Path -LiteralPath $Baseline).Path,
            '--topology-manifest', (Resolve-Path -LiteralPath $TopologyManifest).Path,
            '--asset-id', $AssetId
        )
    }
    if (-not [string]::IsNullOrWhiteSpace($MaskInputPath)) {
        $arguments += @(
            '--mask-input', $resolvedMaskInputPath,
            '--mask-output', $resolvedMaskOutputPath
        )
    }

    $processorOutput = & $python.Source @arguments
    $exitCode = $LASTEXITCODE
    if ($null -ne $processorOutput) {
        $processorOutput
    }
    exit $exitCode
}
catch {
    [Console]::Error.WriteLine("Could not add RimWorld sprite outline: $($_.Exception.Message)")
    exit 1
}
