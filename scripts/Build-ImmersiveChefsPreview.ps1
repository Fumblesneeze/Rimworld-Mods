<#
.SYNOPSIS
Renders the selected Immersive Chefs preview composition with exact text.

.DESCRIPTION
Creates the 1280x720 Workshop master and its 640x360 RimWorld About derivative.
Both outputs are stripped, palette-compressed PNGs and must remain below 1 MiB.

.EXAMPLE
.\scripts\Build-ImmersiveChefsPreview.ps1 -Output table

.EXAMPLE
.\scripts\Build-ImmersiveChefsPreview.ps1 -Output json
#>
[CmdletBinding()]
param(
    [string]$Source,
    [string]$WorkshopOutput,
    [string]$AboutOutput,
    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Source)) {
    $Source = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release\workshop\source\preview-blank.png'
}
if ([string]::IsNullOrWhiteSpace($WorkshopOutput)) {
    $WorkshopOutput = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release\workshop\preview-main.png'
}
if ([string]::IsNullOrWhiteSpace($AboutOutput)) {
    $AboutOutput = Join-Path $repositoryRoot 'mods\ImmersiveChefs\About\Preview.png'
}

$Source = [IO.Path]::GetFullPath($Source)
$WorkshopOutput = [IO.Path]::GetFullPath($WorkshopOutput)
$AboutOutput = [IO.Path]::GetFullPath($AboutOutput)
if ([string]::Equals($WorkshopOutput, $AboutOutput, [StringComparison]::OrdinalIgnoreCase)) {
    [Console]::Error.WriteLine('WorkshopOutput and AboutOutput must be distinct paths.')
    exit 2
}
if ([string]::Equals($Source, $WorkshopOutput, [StringComparison]::OrdinalIgnoreCase) -or
    [string]::Equals($Source, $AboutOutput, [StringComparison]::OrdinalIgnoreCase)) {
    [Console]::Error.WriteLine('Preview outputs must be distinct from the selected source path.')
    exit 2
}

if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
    [Console]::Error.WriteLine("Preview source does not exist: $Source")
    exit 2
}

$magick = Get-Command magick -ErrorAction SilentlyContinue
if ($null -eq $magick) {
    [Console]::Error.WriteLine('ImageMagick 7 (magick) is required to render the preview.')
    exit 2
}

$workshopParent = Split-Path -Parent $WorkshopOutput
$aboutParent = Split-Path -Parent $AboutOutput
New-Item -ItemType Directory -Force -Path $workshopParent, $aboutParent | Out-Null

$temporaryMaster = Join-Path $workshopParent ('.preview-' + [Guid]::NewGuid().ToString('N') + '.png')
$temporaryAbout = Join-Path $aboutParent ('.preview-' + [Guid]::NewGuid().ToString('N') + '.png')

try {
    $renderArguments = @(
        $Source,
        '-resize', '1280x720^',
        '-gravity', 'center',
        '-extent', '1280x720',
        '-font', 'Impact',
        '-pointsize', '70',
        '-fill', '#21130b',
        '-stroke', '#fff3dc',
        '-strokewidth', '1',
        '-gravity', 'northeast',
        '-annotate', '+205+92',
        'YOU DONKEY!',
        '-font', 'Impact',
        '-pointsize', '92',
        '-fill', '#fff4dc',
        '-stroke', '#250500',
        '-strokewidth', '4',
        '-gravity', 'south',
        '-annotate', '+0+38',
        'IMMERSIVE CHEFS',
        '-strip',
        '-colors', '256',
        '-set', 'comment', 'Immersive Chefs texture release 2026-09-16',
        '-define', 'png:include-chunk=tEXt',
        "PNG8:$temporaryMaster"
    )
    & $magick.Source @renderArguments
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed while rendering the Workshop preview (exit $LASTEXITCODE)."
    }

    $aboutArguments = @(
        $temporaryMaster,
        '-filter', 'Lanczos',
        '-resize', '640x360!',
        '-strip',
        '-colors', '256',
        "PNG8:$temporaryAbout"
    )
    & $magick.Source @aboutArguments
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed while rendering the About preview (exit $LASTEXITCODE)."
    }

    function Assert-PreviewContract {
        param(
            [Parameter(Mandatory)] [string]$Path,
            [Parameter(Mandatory)] [string]$Dimensions
        )

        $actualDimensions = (& $magick.Source identify -format '%wx%h' $Path).Trim()
        if ($LASTEXITCODE -ne 0 -or $actualDimensions -ne $Dimensions) {
            throw "Preview $Path is $actualDimensions; expected $Dimensions."
        }

        $length = (Get-Item -LiteralPath $Path).Length
        if ($length -ge 1MB) {
            throw "Preview $Path is $length bytes; expected less than 1 MiB."
        }

        [pscustomobject]@{
            Path = [IO.Path]::GetFullPath($Path)
            Dimensions = $actualDimensions
            Bytes = $length
        }
    }

    $workshop = Assert-PreviewContract -Path $temporaryMaster -Dimensions '1280x720'
    $about = Assert-PreviewContract -Path $temporaryAbout -Dimensions '640x360'
    $workshopBackup = Join-Path $workshopParent ('.preview-backup-' + [Guid]::NewGuid().ToString('N') + '.png')
    $aboutBackup = Join-Path $aboutParent ('.preview-backup-' + [Guid]::NewGuid().ToString('N') + '.png')
    $workshopExisted = Test-Path -LiteralPath $WorkshopOutput
    $aboutExisted = Test-Path -LiteralPath $AboutOutput
    $workshopPublished = $false
    $aboutPublished = $false
    try {
        if ($workshopExisted) {
            Copy-Item -LiteralPath $WorkshopOutput -Destination $workshopBackup
        }
        if ($aboutExisted) {
            Copy-Item -LiteralPath $AboutOutput -Destination $aboutBackup
        }

        Move-Item -LiteralPath $temporaryMaster -Destination $WorkshopOutput -Force
        $workshopPublished = $true
        Move-Item -LiteralPath $temporaryAbout -Destination $AboutOutput -Force
        $aboutPublished = $true
    }
    catch {
        if ($workshopPublished) {
            Remove-Item -LiteralPath $WorkshopOutput -Force -ErrorAction SilentlyContinue
            if ($workshopExisted) {
                Move-Item -LiteralPath $workshopBackup -Destination $WorkshopOutput -Force
            }
        }
        if ($aboutPublished) {
            Remove-Item -LiteralPath $AboutOutput -Force -ErrorAction SilentlyContinue
        }
        if ($aboutExisted -and (Test-Path -LiteralPath $aboutBackup)) {
            Move-Item -LiteralPath $aboutBackup -Destination $AboutOutput -Force
        }
        throw
    }
    finally {
        Remove-Item -LiteralPath $workshopBackup, $aboutBackup -Force -ErrorAction SilentlyContinue
    }

    $result = [pscustomobject]@{
        Source = [IO.Path]::GetFullPath($Source)
        Text = @('YOU DONKEY!', 'IMMERSIVE CHEFS')
        Workshop = [pscustomobject]@{
            Path = [IO.Path]::GetFullPath($WorkshopOutput)
            Dimensions = $workshop.Dimensions
            Bytes = $workshop.Bytes
        }
        About = [pscustomobject]@{
            Path = [IO.Path]::GetFullPath($AboutOutput)
            Dimensions = $about.Dimensions
            Bytes = $about.Bytes
        }
    }

    if ($Output -eq 'json') {
        $result | ConvertTo-Json -Depth 4
    }
    else {
        $result.Workshop, $result.About | Format-Table Path, Dimensions, Bytes -AutoSize
    }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    Remove-Item -LiteralPath $temporaryMaster, $temporaryAbout -Force -ErrorAction SilentlyContinue
}
