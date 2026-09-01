[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$magick = (Get-Command magick -ErrorAction Stop).Source
$sprite = Join-Path $repositoryRoot 'mods\ImmersiveSignalFire\Textures\ImmersiveSignalFire\Things\Building\SignalFire\SignalFire.png'
$workshopOutput = Join-Path $repositoryRoot 'mods\ImmersiveSignalFire\Release\workshop\preview-main.png'
$aboutOutput = Join-Path $repositoryRoot 'mods\ImmersiveSignalFire\About\Preview.png'
$temporaryContainer = Join-Path $repositoryRoot 'artifacts\AssetGeneration\ImmersiveSignalFire\Preview'
$temporaryRoot = Join-Path $temporaryContainer ([guid]::NewGuid().ToString('N'))
$background = Join-Path $temporaryRoot 'background.png'
$scaledSprite = Join-Path $temporaryRoot 'signal-fire.png'
$candidateWorkshop = Join-Path $temporaryRoot 'preview-workshop.png'
$candidateAbout = Join-Path $temporaryRoot 'preview-about.png'
$workshopBackup = Join-Path $temporaryRoot 'preview-workshop.backup.png'
$aboutBackup = Join-Path $temporaryRoot 'preview-about.backup.png'
$publishedWorkshop = $false
$publishedAbout = $false
$workshopExisted = Test-Path -LiteralPath $workshopOutput
$aboutExisted = Test-Path -LiteralPath $aboutOutput

New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null
try {
    & $magick -size '640x360' 'gradient:#151719-#514433' -depth 8 $background
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed to render the Immersive Signal Fire preview background."
    }

    & $magick $sprite -trim +repage -resize '245x190>' -depth 8 -define 'png:color-type=6' $scaledSprite
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed to normalize the Immersive Signal Fire preview sprite."
    }

    & $magick $background `
        $scaledSprite -geometry '+378+84' -composite `
        -font 'Segoe-UI-Light' -pointsize 45 -fill '#f1e8d5' -gravity northwest -annotate '+30+101' 'IMMERSIVE' `
        -fill '#df9a35' -annotate '+30+153' 'SIGNAL FIRE' `
        -font 'Segoe-UI' -pointsize 19 -fill '#e2dac8' -annotate '+31+224' 'Allies answer the smoke.' `
        -pointsize 14 -fill '#a99f8d' -annotate '+31+255' 'A single-use Neolithic signal ritual' `
        -depth 8 -define 'png:color-type=2' $candidateWorkshop
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed to render the Immersive Signal Fire preview."
    }

    Copy-Item -LiteralPath $candidateWorkshop -Destination $candidateAbout
    foreach ($candidate in @($candidateWorkshop, $candidateAbout)) {
        $shape = & $magick identify -format '%w %h %[channels] %[depth]' $candidate
        if ($LASTEXITCODE -ne 0 -or $shape -notmatch '^640 360 srgb\s+3\.0 8$') {
            throw "Preview validation failed for '$candidate': $shape"
        }

        $length = (Get-Item -LiteralPath $candidate).Length
        if ($length -lt 10kb -or $length -gt 1mb) {
            throw "Preview size is outside the reviewed 10 KiB through 1 MiB range: $length bytes."
        }
    }

    $candidateHashes = @(
        (Get-FileHash -LiteralPath $candidateWorkshop -Algorithm SHA256).Hash,
        (Get-FileHash -LiteralPath $candidateAbout -Algorithm SHA256).Hash)
    if ($candidateHashes[0] -ne $candidateHashes[1]) {
        throw 'About and Workshop preview candidates are not byte-identical.'
    }

    if ($workshopExisted) {
        [IO.File]::Replace($candidateWorkshop, $workshopOutput, $workshopBackup, $true)
    }
    else {
        [IO.File]::Move($candidateWorkshop, $workshopOutput)
    }
    $publishedWorkshop = $true

    if ($aboutExisted) {
        [IO.File]::Replace($candidateAbout, $aboutOutput, $aboutBackup, $true)
    }
    else {
        [IO.File]::Move($candidateAbout, $aboutOutput)
    }
    $publishedAbout = $true

    Get-Item -LiteralPath $workshopOutput, $aboutOutput |
        Select-Object FullName, Length
}
catch {
    if ($publishedAbout -and (Test-Path -LiteralPath $aboutBackup)) {
        [IO.File]::Replace($aboutBackup, $aboutOutput, $null, $true)
    }
    elseif ($publishedAbout -and -not $aboutExisted -and (Test-Path -LiteralPath $aboutOutput)) {
        Remove-Item -LiteralPath $aboutOutput -Force
    }
    if ($publishedWorkshop -and (Test-Path -LiteralPath $workshopBackup)) {
        [IO.File]::Replace($workshopBackup, $workshopOutput, $null, $true)
    }
    elseif ($publishedWorkshop -and -not $workshopExisted -and (Test-Path -LiteralPath $workshopOutput)) {
        Remove-Item -LiteralPath $workshopOutput -Force
    }
    throw
}
finally {
    $resolvedContainer = [IO.Path]::GetFullPath($temporaryContainer).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedTemporary = [IO.Path]::GetFullPath($temporaryRoot)
    if (-not $resolvedTemporary.StartsWith($resolvedContainer, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an unexpected preview path: $resolvedTemporary"
    }
    if (Test-Path -LiteralPath $resolvedTemporary) {
        Remove-Item -LiteralPath $resolvedTemporary -Recurse -Force
    }
}
