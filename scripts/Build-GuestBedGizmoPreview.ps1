<#
.SYNOPSIS
Renders the Guest Bed Gizmo Steam and About previews from one accepted in-game frame.

.DESCRIPTION
Validates the pinned source, menu crop, layout, and ImageMagick build in the mod-owned
presentation manifest, then creates a menu-focused 1164x655 Steam card and 640x360 About derivative.

.EXAMPLE
.\scripts\Build-GuestBedGizmoPreview.ps1 -Output table

.EXAMPLE
.\scripts\Build-GuestBedGizmoPreview.ps1 -Output json
#>
[CmdletBinding()]
param(
    [string]$ManifestPath,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $repositoryRoot 'mods\GuestBedGizmo\Release\workshop\presentation.json'
}
$ManifestPath = [IO.Path]::GetFullPath($ManifestPath)

function Exit-InvalidInput {
    param([Parameter(Mandatory)][string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

function Resolve-RepositoryPath {
    param([Parameter(Mandatory)][string]$RelativePath)

    $resolved = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $RelativePath))
    $prefix = $repositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        Exit-InvalidInput "Manifest path escapes the repository: $RelativePath"
    }
    return $resolved
}

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Invoke-Magick {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $nativeOutput = @(& $script:magick.Source @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE.`n$($nativeOutput -join [Environment]::NewLine)"
    }
}

function Assert-Image {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Dimensions
    )

    $actual = (& $script:magick.Source identify -format '%wx%h %[channels]' $Path).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $actual.StartsWith("$Dimensions ", [StringComparison]::Ordinal)) {
        throw "Preview '$Path' is '$actual'; expected $Dimensions and an explicit channel description."
    }
    $length = (Get-Item -LiteralPath $Path).Length
    if ($length -ge 1MB) {
        throw "Preview '$Path' is $length bytes; expected less than 1 MiB."
    }
    return [pscustomobject]@{
        Path = [IO.Path]::GetFullPath($Path)
        Dimensions = $Dimensions
        Channels = $actual.Substring($Dimensions.Length + 1)
        Bytes = $length
        Sha256 = Get-Sha256 -Path $Path
    }
}

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    Exit-InvalidInput "Presentation manifest does not exist: $ManifestPath"
}

try {
    $manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json -ErrorAction Stop
}
catch {
    Exit-InvalidInput "Presentation manifest is not valid JSON: $($_.Exception.Message)"
}

if ($manifest.schema -cne 'GuestBedGizmo/WorkshopPresentation/v1') {
    Exit-InvalidInput "Unsupported presentation schema '$($manifest.schema)'."
}

$sourcePath = Resolve-RepositoryPath -RelativePath ([string]$manifest.source.path)
$rendererPath = Resolve-RepositoryPath -RelativePath ([string]$manifest.renderer.path)
$workshopPath = Resolve-RepositoryPath -RelativePath ([string]$manifest.outputs.workshop.path)
$aboutPath = Resolve-RepositoryPath -RelativePath ([string]$manifest.outputs.about.path)
foreach ($requiredPath in @($sourcePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        Exit-InvalidInput "Required presentation input does not exist: $requiredPath"
    }
}

if ((Get-Sha256 -Path $sourcePath) -cne [string]$manifest.source.sha256) {
    Exit-InvalidInput 'Accepted source hash differs from presentation.json.'
}
if ([string]$manifest.renderer.sha256 -cne 'PENDING' -and
    (Get-Sha256 -Path $rendererPath) -cne [string]$manifest.renderer.sha256) {
    Exit-InvalidInput 'Pinned presentation renderer hash differs from presentation.json.'
}

$magick = Get-Command magick -ErrorAction SilentlyContinue
if ($null -eq $magick) {
    Exit-InvalidInput 'ImageMagick 7 (magick) is required to render the preview.'
}
$versionLine = @(& $magick.Source -version 2>&1)[0]
if ($versionLine -cne [string]$manifest.renderer.imageMagickVersionLine) {
    Exit-InvalidInput "Unsupported ImageMagick build. Expected '$($manifest.renderer.imageMagickVersionLine)'; observed '$versionLine'."
}

$sourceDimensions = (& $magick.Source identify -format '%wx%h' $sourcePath).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceDimensions -cne "$($manifest.source.width)x$($manifest.source.height)") {
    Exit-InvalidInput "Accepted source dimensions are '$sourceDimensions', expected $($manifest.source.width)x$($manifest.source.height)."
}

$workshopParent = Split-Path -Parent $workshopPath
$aboutParent = Split-Path -Parent $aboutPath
New-Item -ItemType Directory -Force -Path $workshopParent, $aboutParent | Out-Null
$temporaryRoot = Join-Path $repositoryRoot ('artifacts\GuestBedGizmoPreview\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null
$menuCropPath = Join-Path $temporaryRoot 'menu-crop.png'
$masterPath = Join-Path $temporaryRoot 'preview-main.png'
$aboutTempPath = Join-Path $temporaryRoot 'about-preview.png'

try {
    if ([string]$manifest.layout.mode -cne 'menu-focus') {
        Exit-InvalidInput "Unsupported Guest Bed presentation mode '$($manifest.layout.mode)'."
    }

    $menuCrop = $manifest.layout.menuCrop
    $menuCropGeometry = '{0}x{1}+{2}+{3}' -f $menuCrop.width, $menuCrop.height, $menuCrop.x, $menuCrop.y
    Invoke-Magick -Arguments @(
        $sourcePath,
        '-crop', $menuCropGeometry,
        '+repage',
        '-filter', 'Lanczos',
        '-resize', "$($manifest.layout.menuImage.width)x$($manifest.layout.menuImage.height)!",
        '-strip',
        "PNG24:$menuCropPath"
    )

    $menuBorder = 'rectangle {0},{1} {2},{3}' -f `
        ($manifest.layout.menuImage.x - 4), `
        ($manifest.layout.menuImage.y - 4), `
        ($manifest.layout.menuImage.x + $manifest.layout.menuImage.width + 4), `
        ($manifest.layout.menuImage.y + $manifest.layout.menuImage.height + 4)
    Invoke-Magick -Arguments @(
        '-size', "$($manifest.layout.canvas.width)x$($manifest.layout.canvas.height)",
        "xc:$($manifest.colors.outer)",
        '-fill', [string]$manifest.colors.panel,
        '-stroke', 'none',
        '-draw', 'roundrectangle 14,14 1150,641 26,26',
        $menuCropPath,
        '-geometry', "+$($manifest.layout.menuImage.x)+$($manifest.layout.menuImage.y)",
        '-compose', 'over',
        '-composite',
        '-fill', 'none',
        '-stroke', [string]$manifest.colors.frame,
        '-strokewidth', '3',
        '-draw', $menuBorder,
        '-strip',
        '-define', 'png:compression-level=9',
        "PNG24:$masterPath"
    )

    Invoke-Magick -Arguments @(
        $masterPath,
        '-filter', 'Lanczos',
        '-resize', '640x360!',
        '-strip',
        '-define', 'png:compression-level=9',
        "PNG24:$aboutTempPath"
    )

    $workshop = Assert-Image -Path $masterPath -Dimensions '1164x655'
    $about = Assert-Image -Path $aboutTempPath -Dimensions '640x360'
    if ([string]$manifest.outputs.workshop.sha256 -cne 'PENDING' -and
        ($workshop.Sha256 -cne [string]$manifest.outputs.workshop.sha256 -or
         $workshop.Bytes -ne [long]$manifest.outputs.workshop.bytes)) {
        throw 'Rendered Workshop preview differs from the pinned output identity.'
    }
    if ([string]$manifest.outputs.about.sha256 -cne 'PENDING' -and
        ($about.Sha256 -cne [string]$manifest.outputs.about.sha256 -or
         $about.Bytes -ne [long]$manifest.outputs.about.bytes)) {
        throw 'Rendered About preview differs from the pinned output identity.'
    }
    Copy-Item -LiteralPath $masterPath -Destination $workshopPath -Force
    Copy-Item -LiteralPath $aboutTempPath -Destination $aboutPath -Force

    $result = [pscustomobject]@{
        Status = 'passed'
        Manifest = $ManifestPath
        Source = [pscustomobject]@{
            Path = $sourcePath
            Dimensions = $sourceDimensions
            Sha256 = Get-Sha256 -Path $sourcePath
        }
        Workshop = $workshop
        About = $about
    }
    if ($Output -eq 'json') {
        $result | ConvertTo-Json -Depth 5
    }
    else {
        $result.Workshop, $result.About | Format-Table Path, Dimensions, Channels, Bytes, Sha256 -AutoSize
    }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot -PathType Container) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
