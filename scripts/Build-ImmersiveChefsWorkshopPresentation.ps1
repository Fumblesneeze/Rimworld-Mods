<#
.SYNOPSIS
Renders the committed Immersive Chefs Steam Workshop feature cards.

.DESCRIPTION
Uses one repository-owned SVG frame, a repository-local OFL font, the mod presentation
manifest, and the exact shipped sprites. ImageMagick is the only external renderer.
#>
[CmdletBinding()]
param(
    [string]$ImageMagickPath = 'magick'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Magick {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & $ImageMagickPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "ImageMagick failed with exit code $LASTEXITCODE." }
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$workshopRoot = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release\workshop'
$textureRoot = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Textures\ImmersiveChefs'
$manifestPath = Join-Path $workshopRoot 'presentation.json'
$templatePath = Join-Path $repositoryRoot 'release\templates\workshop\feature-card.svg'
$fontPath = (Join-Path $repositoryRoot 'release\templates\workshop\fonts\Oswald-SemiBold.ttf').Replace('\', '/')
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('immersive-chefs-workshop-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporaryRoot

try {
    foreach ($card in @($manifest.cards)) {
        $outputPath = Join-Path $workshopRoot ([string]$card.path).Replace('/', '\')
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force

        $layout = if ($null -ne $card.PSObject.Properties['layout']) { [string]$card.layout } else { 'feature' }
        if ($layout -ceq 'hero') {
            $sourcePath = Join-Path $workshopRoot ([string]$card.source)
            Invoke-Magick @($sourcePath, '-filter', 'Lanczos', '-resize', '1164x655^', '-gravity', 'center', '-extent', '1164x655', '-strip', '-define', 'png:compression-level=9', $outputPath)
            continue
        }

        $currentPath = Join-Path $temporaryRoot (([string]$card.token) + '-0.png')
        Invoke-Magick @('-background', 'none', $templatePath, '-strip', $currentPath)
        $index = 0
        foreach ($art in @($card.art)) {
            $index++
            $source = [string]$art.source
            $artPath = if ($source.StartsWith('workshop:', [StringComparison]::Ordinal)) {
                Join-Path $workshopRoot $source.Substring('workshop:'.Length).Replace('/', '\')
            }
            else {
                Join-Path $textureRoot $source.Replace('/', '\')
            }
            if (-not (Test-Path -LiteralPath $artPath -PathType Leaf)) { throw "Missing presentation sprite: $artPath" }
            $resizedPath = Join-Path $temporaryRoot (([string]$card.token) + "-art-$index.png")
            $nextPath = Join-Path $temporaryRoot (([string]$card.token) + "-$index.png")
            Invoke-Magick @($artPath, '-filter', 'Lanczos', '-resize', ([string]$art.width + 'x'), '-trim', '+repage', $resizedPath)
            Invoke-Magick @($currentPath, $resizedPath, '-geometry', ('+' + [int]$art.x + '+' + [int]$art.y), '-composite', $nextPath)
            $currentPath = $nextPath
        }

        $titleSize = if ($null -ne $card.PSObject.Properties['titleSize']) { [int]$card.titleSize } else { 58 }
        $textPath = Join-Path $temporaryRoot (([string]$card.token) + '-text.png')
        $args = [System.Collections.Generic.List[string]]::new()
        foreach ($arg in @(
            $currentPath,
            '-font', $fontPath,
            '-fill', '#d85a36', '-pointsize', '24', '-gravity', 'northwest', '-annotate', '+69+62', ([string]$card.kicker),
            '-fill', '#fff0d4', '-stroke', '#050505', '-strokewidth', '2', '-pointsize', [string]$titleSize, '-annotate', '+67+128', ([string]$card.title),
            '-stroke', 'none', '-fill', '#ead7b5', '-pointsize', '29')) { $args.Add([string]$arg) }
        $lineY = 242
        foreach ($line in @($card.lines)) {
            foreach ($arg in @('-fill', '#ead7b5', '-pointsize', '29', '-annotate', ('+70+' + $lineY), [string]$line)) { $args.Add([string]$arg) }
            $lineY += 64
        }
        foreach ($arg in @('-strip', '-define', 'png:compression-level=9', $textPath)) { $args.Add([string]$arg) }
        Invoke-Magick -Arguments $args.ToArray()
        Copy-Item -LiteralPath $textPath -Destination $outputPath -Force
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) { Remove-Item -LiteralPath $temporaryRoot -Recurse -Force }
}

[pscustomobject][ordered]@{
    schema = [string]$manifest.schema
    cards = @($manifest.cards).Count
    outputRoot = Join-Path $workshopRoot 'assets'
} | ConvertTo-Json -Compress
