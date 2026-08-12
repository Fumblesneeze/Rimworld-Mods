<#
.SYNOPSIS
Measures a RimWorld PNG without modifying it.

.DESCRIPTION
Reports the exact canvas, nonzero-alpha bounds, mean visible alpha coverage, source PNG bit
depth/color type, channels, byte length, and SHA-256. Requires ImageMagick's `magick` command. Exit
code 0 is success, 2 is invalid input or a missing prerequisite, and 1 is an ImageMagick/runtime
failure.

.EXAMPLE
& .\Measure-RimWorldSprite.ps1 -Path .\Bench_north.png -Output table

.EXAMPLE
& .\Measure-RimWorldSprite.ps1 -Path .\Bench_north.png -Output json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Path,

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

try {
    $magick = Get-Command magick -ErrorAction SilentlyContinue
    if ($null -eq $magick) {
        Exit-InvalidInput 'ImageMagick is required, but `magick` is not available on PATH.'
    }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Exit-InvalidInput "PNG does not exist: $Path"
    }

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if ([IO.Path]::GetExtension($resolved) -ine '.png') {
        Exit-InvalidInput "Expected a .png file: $resolved"
    }

    $identifyArguments = @(
        $resolved,
        '-format',
        '%w|%h|%[channels]|%[type]|%[png:IHDR.color_type]|%[png:IHDR.bit_depth]',
        'info:'
    )
    $identity = (& $magick.Source @identifyArguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick identify failed: $identity"
    }
    $parts = ([string]$identity).Split('|')
    if ($parts.Count -ne 6) {
        throw "ImageMagick returned an unexpected identity shape: $identity"
    }

    $coverageArguments = @(
        $resolved,
        '-alpha', 'extract',
        '-format', '%[fx:mean]',
        'info:'
    )
    $coverageText = (& $magick.Source @coverageArguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick alpha measurement failed: $coverageText"
    }
    $coverage = [double]::Parse(
        [string]$coverageText,
        [Globalization.CultureInfo]::InvariantCulture)

    $alphaBounds = '0x0+0+0'
    if ($coverage -gt 0.0) {
        $boundsArguments = @(
            $resolved,
            '-alpha', 'extract',
            '+repage',
            '-bordercolor', 'black',
            '-border', '1',
            '-threshold', '0',
            '-trim',
            '-format', '%wx%h%X%Y',
            'info:'
        )
        $boundsText = [string](& $magick.Source @boundsArguments 2>&1)
        if ($LASTEXITCODE -ne 0) {
            throw "ImageMagick alpha-bounds measurement failed: $boundsText"
        }
        if ($boundsText -notmatch '^(?<Width>\d+)x(?<Height>\d+)(?<X>[+-]\d+)(?<Y>[+-]\d+)$') {
            throw "ImageMagick returned an unexpected alpha-bounds shape: $boundsText"
        }

        $alphaX = [int]$Matches.X - 1
        $alphaY = [int]$Matches.Y - 1
        $alphaBounds = '{0}x{1}{2:+0;-0;+0}{3:+0;-0;+0}' -f `
            [int]$Matches.Width,
            [int]$Matches.Height,
            $alphaX,
            $alphaY
    }

    if ([string]::IsNullOrWhiteSpace($parts[4]) -or [string]::IsNullOrWhiteSpace($parts[5])) {
        throw "ImageMagick did not expose the source PNG color type/bit depth: $identity"
    }

    $record = [pscustomobject]@{
        Path = $resolved
        CanvasWidth = [int]$parts[0]
        CanvasHeight = [int]$parts[1]
        AlphaBounds = $alphaBounds
        MeanAlphaCoverage = $coverage
        Channels = [string]$parts[2]
        ImageType = [string]$parts[3]
        PngColorType = [string]$parts[4]
        PngBitDepth = [int]$parts[5]
        Bytes = (Get-Item -LiteralPath $resolved).Length
        Sha256 = (Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    if ($Output -eq 'json') {
        $record | ConvertTo-Json -Compress
    }
    else {
        $record | Format-List
    }
}
catch {
    [Console]::Error.WriteLine("Could not measure RimWorld sprite: $($_.Exception.Message)")
    exit 1
}
