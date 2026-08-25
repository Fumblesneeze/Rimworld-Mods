<#
.SYNOPSIS
Builds the Thin Walls About preview and Steam Workshop cards from accepted in-game captures.

.DESCRIPTION
Uses only hash-bound screenshots from the accepted minimized RimWorld run, a repository-local
OFL font, and deterministic ImageMagick crop/layout operations. It never generates or redraws
wall geometry. The default output root is the repository; pass -OutputRoot for a clean rerender.

.PARAMETER ImageMagickPath
ImageMagick executable. The exact supported version is pinned in presentation.json.

.PARAMETER OutputRoot
Root beneath which repository-relative output paths are written.

.PARAMETER Output
Human-readable table output or stable JSON.

.PARAMETER VerifyOnly
Verify existing outputs without rendering them.

.PARAMETER RefreshManifest
Authoring-only mode that renders and refreshes approved output byte counts and SHA-256 values.

.EXAMPLE
.\scripts\Build-ThinWallsWorkshopPresentation.ps1 -Output json

.EXAMPLE
.\scripts\Build-ThinWallsWorkshopPresentation.ps1 -OutputRoot C:\Temp\ThinWallsRender -Output json
#>
[CmdletBinding()]
param(
    [string]$ImageMagickPath = 'magick',
    [string]$OutputRoot,
    [ValidateSet('table', 'json')][string]$Output = 'table',
    [switch]$VerifyOnly,
    [switch]$RefreshManifest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$script:panelMeasurements = @{}

if ($VerifyOnly -and $RefreshManifest) { throw '-VerifyOnly and -RefreshManifest cannot be combined.' }

function Get-Sha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Invoke-Magick([string[]]$Arguments) {
    $nativeOutput = @(& $ImageMagickPath @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE.`n$($nativeOutput -join [Environment]::NewLine)"
    }
}

function Invoke-MagickCapture([string[]]$Arguments) {
    $nativeOutput = @(& $ImageMagickPath @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE.`n$($nativeOutput -join [Environment]::NewLine)"
    }
    return ($nativeOutput -join [Environment]::NewLine).Trim()
}

function Assert-TextFits([string]$Text, [int]$PointSize, [int]$MaximumWidth, [string]$FontPath) {
    $widthText = Invoke-MagickCapture -Arguments @('-font', $FontPath, '-pointsize', [string]$PointSize, "label:$Text", '-format', '%w', 'info:')
    try { [int]$width = [Convert]::ToInt32($widthText, [Globalization.CultureInfo]::InvariantCulture) }
    catch { throw "ImageMagick returned an invalid text width for '$Text': '$widthText'." }
    if ($width -gt $MaximumWidth) {
        throw "Presentation text does not fit its declared box: '$Text' ($width/$MaximumWidth px)."
    }
}

function Get-Source([string]$Token) {
    $matches = @($script:manifest.sources | Where-Object { [string]$_.token -ceq $Token })
    if ($matches.Count -ne 1) { throw "Presentation source token '$Token' did not resolve exactly once." }
    return $matches[0]
}

function Get-OutputDefinition([string]$Token) {
    $matches = @($script:manifest.outputs | Where-Object { [string]$_.token -ceq $Token })
    if ($matches.Count -ne 1) { throw "Presentation output token '$Token' did not resolve exactly once." }
    return $matches[0]
}

function Get-Card([string]$Token) {
    $matches = @($script:manifest.cards | Where-Object { [string]$_.token -ceq $Token })
    if ($matches.Count -ne 1) { throw "Presentation card token '$Token' did not resolve exactly once." }
    return $matches[0]
}

function Resolve-SourcePath([string]$Token) {
    $source = Get-Source $Token
    return [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$source.path).Replace('/', '\')))
}

function Get-MeasurementInput([string]$Token) {
    $matches = @($script:manifest.measurementInputs | Where-Object { [string]$_.token -ceq $Token })
    if ($matches.Count -ne 1) { throw "Measurement input token '$Token' did not resolve exactly once." }
    return $matches[0]
}

function Resolve-MeasurementPath([string]$Token) {
    $input = Get-MeasurementInput $Token
    return [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$input.path).Replace('/', '\')))
}

function Resolve-OutputPath([string]$Token) {
    $definition = Get-OutputDefinition $Token
    return [IO.Path]::GetFullPath((Join-Path $script:resolvedOutputRoot ([string]$definition.path).Replace('/', '\')))
}

function Get-RequiredPlacementScopes {
    $scopes = [ordered]@{}
    foreach ($token in @('workbenches','room-closed','room-presentation-clean','door-detail-closed','door-detail-open')) {
        $scopes["source:$token"] = 'itemized-realistic-scene'
    }
    foreach ($token in @('preview-south','preview-east','junction-l','junction-t','junction-plus','regular-union','damage-stone','damage-wood','damage-steel')) {
        $scopes["source:$token"] = 'technical-catalog-no-realistic-fixtures'
    }
    foreach ($token in @('title','about','edge-placement','thin-doors','contact-sheet')) {
        $scopes["output:$token"] = 'itemized-realistic-scene'
    }
    foreach ($token in @('continuous-joins','materials-damage')) {
        $scopes["output:$token"] = 'technical-catalog-no-realistic-fixtures'
    }
    return $scopes
}

function Get-RequiredPlacementAuditCategories {
    return [ordered]@{
        'source:workbenches' = @('workbench','primary-aisle')
        'source:room-closed' = @('bed','chair','table-counter','door','primary-aisle')
        'source:room-presentation-clean' = @('bed','chair','table-counter','door','primary-aisle')
        'source:door-detail-closed' = @('door')
        'source:door-detail-open' = @('door')
        'output:title' = @('bed','chair','table-counter','door','primary-aisle')
        'output:about' = @('bed','chair','table-counter','door','primary-aisle')
        'output:edge-placement' = @('workbench','primary-aisle')
        'output:thin-doors' = @('bed','chair','table-counter','door','primary-aisle')
        'output:contact-sheet' = @('bed','chair','table-counter','workbench','door','primary-aisle')
    }
}

function New-Canvas([int]$Width, [int]$Height, [string]$Path) {
    Invoke-Magick @('-size', "${Width}x${Height}", 'xc:#1b2838', '-fill', '#111418', '-draw', "roundrectangle 12,12,$($Width-13),$($Height-13),22,22", '-strip', $Path)
}

function Measure-StagedDifferenceBounds(
    [string]$BeforePath,
    [string]$AfterPath,
    [int]$SearchX = 0,
    [int]$SearchY = 0,
    [int]$SearchWidth = 0,
    [int]$SearchHeight = 0
) {
    $before = [Drawing.Bitmap]::new($BeforePath)
    $after = [Drawing.Bitmap]::new($AfterPath)
    try {
        if ($before.Width -ne $after.Width -or $before.Height -ne $after.Height) {
            throw "Staged measurement images do not share one exact viewport: '$BeforePath' / '$AfterPath'."
        }
        if ($SearchWidth -le 0) { $SearchWidth = $after.Width }
        if ($SearchHeight -le 0) { $SearchHeight = $after.Height }
        if ($SearchX -lt 0 -or $SearchY -lt 0 -or
            ($SearchX + $SearchWidth) -gt $after.Width -or
            ($SearchY + $SearchHeight) -gt $after.Height) {
            throw "The staged measurement region escapes '$AfterPath'."
        }
        $minX = $after.Width
        $minY = $after.Height
        $maxX = -1
        $maxY = -1
        $pixels = [Collections.Generic.HashSet[int]]::new()
        for ($y = $SearchY; $y -lt ($SearchY + $SearchHeight); $y++) {
            for ($x = $SearchX; $x -lt ($SearchX + $SearchWidth); $x++) {
                $a = $before.GetPixel($x, $y)
                $b = $after.GetPixel($x, $y)
                $delta = [Math]::Max([Math]::Abs([int]$a.R - [int]$b.R), [Math]::Max(
                    [Math]::Abs([int]$a.G - [int]$b.G),
                    [Math]::Abs([int]$a.B - [int]$b.B)))
                if ($delta -lt 1) { continue }
                $null = $pixels.Add(($y * $after.Width) + $x)
                $minX = [Math]::Min($minX, $x)
                $minY = [Math]::Min($minY, $y)
                $maxX = [Math]::Max($maxX, $x)
                $maxY = [Math]::Max($maxY, $y)
            }
        }

        if ($pixels.Count -lt 64 -or $maxX -lt $minX -or $maxY -lt $minY) {
            throw "The staged pair '$BeforePath' / '$AfterPath' did not produce a measurable complete structural difference."
        }

        return [pscustomobject][ordered]@{
            minX = $minX
            minY = $minY
            maxX = $maxX
            maxY = $maxY
            midpointX = ($minX + $maxX) / 2.0
            midpointY = ($minY + $maxY) / 2.0
            width = $after.Width
            height = $after.Height
            pixels = $pixels
            pixelCount = $pixels.Count
        }
    }
    finally {
        $before.Dispose()
        $after.Dispose()
    }
}

function Select-RelevantDifferenceComponent(
    [object]$Mask,
    [double]$ExpectedBoundary,
    [bool]$NormalAxisIsY,
    [string]$Label
) {
    $remaining = [Collections.Generic.HashSet[int]]::new()
    foreach ($key in $Mask.pixels) { $null = $remaining.Add([int]$key) }
    $components = [Collections.Generic.List[object]]::new()
    $queue = [Collections.Generic.Queue[int]]::new()
    while ($remaining.Count -gt 0) {
        $enumerator = $remaining.GetEnumerator()
        $null = $enumerator.MoveNext()
        $seed = [int]$enumerator.Current
        $enumerator.Dispose()
        $null = $remaining.Remove($seed)
        $queue.Enqueue($seed)
        $pixels = [Collections.Generic.HashSet[int]]::new()
        $null = $pixels.Add($seed)
        $minimumX = [int]::MaxValue
        $minimumY = [int]::MaxValue
        $maximumX = [int]::MinValue
        $maximumY = [int]::MinValue
        while ($queue.Count -gt 0) {
            $key = $queue.Dequeue()
            $x = $key % [int]$Mask.width
            $y = [Math]::Floor($key / [int]$Mask.width)
            $minimumX = [Math]::Min($minimumX, $x)
            $minimumY = [Math]::Min($minimumY, $y)
            $maximumX = [Math]::Max($maximumX, $x)
            $maximumY = [Math]::Max($maximumY, $y)
            for ($dy = -1; $dy -le 1; $dy++) {
                for ($dx = -1; $dx -le 1; $dx++) {
                    if ($dx -eq 0 -and $dy -eq 0) { continue }
                    $neighborX = $x + $dx
                    $neighborY = $y + $dy
                    if ($neighborX -lt 0 -or $neighborX -ge [int]$Mask.width -or $neighborY -lt 0 -or $neighborY -ge [int]$Mask.height) { continue }
                    $neighbor = ($neighborY * [int]$Mask.width) + $neighborX
                    if ($remaining.Remove($neighbor)) {
                        $null = $pixels.Add($neighbor)
                        $queue.Enqueue($neighbor)
                    }
                }
            }
        }
        if ($pixels.Count -ge 64) {
            $components.Add([pscustomobject][ordered]@{
                minX = $minimumX
                minY = $minimumY
                maxX = $maximumX
                maxY = $maximumY
                midpointX = ($minimumX + $maximumX) / 2.0
                midpointY = ($minimumY + $maximumY) / 2.0
                width = [int]$Mask.width
                height = [int]$Mask.height
                pixels = $pixels
                pixelCount = $pixels.Count
            })
        }
    }
    if ($components.Count -eq 0) { throw "$Label has no complete staged structural component." }
    $selected = @($components | Sort-Object `
        @{ Expression={ [Math]::Abs((if ($NormalAxisIsY) { [double]$_.midpointY } else { [double]$_.midpointX }) - $ExpectedBoundary) } }, `
        @{ Expression={ -[int]$_.pixelCount } })[0]
    if ([int]$selected.pixelCount -lt 500) { throw "$Label staged structural component is too small." }
    if (([int]$Mask.pixelCount - [int]$selected.pixelCount) -gt 16) {
        throw "$Label contains unrelated staged drift outside the expected structural component."
    }
    return $selected
}

function Get-SharedEdgeProjection([string]$SourceToken, [string]$ExpectedOwnedSide) {
    $source = Get-Source $SourceToken
    $projection = $source.sharedEdgeProjection
    if ($null -eq $projection) {
        throw "Preview source '$SourceToken' is missing its Gateway-recorded shared-edge projection."
    }
    $requiredFields = @(
        'mapId', 'cameraWorldX', 'cameraWorldZ', 'cameraRootSize', 'viewportX', 'viewportY',
        'screenX', 'screenYTop',
        'startScreenX', 'startScreenYTop',
        'endScreenX', 'endScreenYTop',
        'ownerCellCenterScreenX', 'ownerCellCenterScreenYTop',
        'oppositeCellCenterScreenX', 'oppositeCellCenterScreenYTop',
        'screenshotWidth', 'screenshotHeight', 'ownedSide')
    $availableFields = @($projection.PSObject.Properties.Name)
    foreach ($requiredField in $requiredFields) {
        if ($availableFields -cnotcontains $requiredField) {
            throw "Preview source '$SourceToken' is missing shared-edge projection field '$requiredField'."
        }
    }
    if ([string]$projection.ownedSide -cne $ExpectedOwnedSide -or
        [int]$projection.screenshotWidth -ne [int]$source.width -or
        [int]$projection.screenshotHeight -ne [int]$source.height) {
        throw "Preview source '$SourceToken' has projection provenance for the wrong side or screenshot dimensions."
    }
    return $projection
}

function Assert-PreviewCameraIdentity([object]$Source, [object]$Projection) {
    $baseline = Get-MeasurementInput ([string]$Source.backgroundMeasurementToken)
    $requiredFields = @('mapId','cameraWorldX','cameraWorldZ','cameraRootSize','viewportX','viewportY','width','height')
    foreach ($field in $requiredFields) {
        if (@($baseline.PSObject.Properties.Name) -cnotcontains $field) {
            throw "Preview background '$($baseline.token)' is missing exact-camera field '$field'."
        }
    }
    if ([string]::IsNullOrWhiteSpace([string]$baseline.mapId) -or [double]$baseline.cameraRootSize -le 0) {
        throw "Preview background '$($baseline.token)' has invalid exact-camera identity."
    }
    if ([string]$baseline.mapId -cne [string]$Projection.mapId -or
        [double]$baseline.cameraWorldX -ne [double]$Projection.cameraWorldX -or
        [double]$baseline.cameraWorldZ -ne [double]$Projection.cameraWorldZ -or
        [double]$baseline.cameraRootSize -ne [double]$Projection.cameraRootSize -or
        [int]$baseline.viewportX -ne [int]$Projection.viewportX -or
        [int]$baseline.viewportY -ne [int]$Projection.viewportY -or
        [int]$baseline.width -ne [int]$Projection.screenshotWidth -or
        [int]$baseline.height -ne [int]$Projection.screenshotHeight) {
        throw "Preview source '$($Source.token)' and its staged background do not share the exact map, camera root, zoom, and viewport."
    }
}

function Convert-SourcePointToPanel(
    [double]$SourceX,
    [double]$SourceY,
    [int]$CropX,
    [int]$CropY,
    [int]$CropWidth,
    [int]$CropHeight,
    [int]$PanelWidth,
    [int]$PanelHeight
) {
    $scale = [Math]::Max(
        [double]$PanelWidth / [double]$CropWidth,
        [double]$PanelHeight / [double]$CropHeight)
    $resizedWidth = [Math]::Round($CropWidth * $scale, 0, [MidpointRounding]::AwayFromZero)
    $resizedHeight = [Math]::Round($CropHeight * $scale, 0, [MidpointRounding]::AwayFromZero)
    $extentOffsetX = ($resizedWidth - $PanelWidth) / 2.0
    $extentOffsetY = ($resizedHeight - $PanelHeight) / 2.0
    return [pscustomobject][ordered]@{
        x = (($SourceX - $CropX) * $scale) - $extentOffsetX
        y = (($SourceY - $CropY) * $scale) - $extentOffsetY
    }
}

function Assert-SourcePreviewProjection([string]$SourceToken, [string]$ExpectedOwnedSide, [bool]$NormalAxisIsY) {
    $source = Get-Source $SourceToken
    $projection = Get-SharedEdgeProjection $SourceToken $ExpectedOwnedSide
    if ([string]::IsNullOrWhiteSpace([string]$source.backgroundMeasurementToken)) {
        throw "Preview source '$SourceToken' is missing its same-camera background measurement token."
    }
    Assert-PreviewCameraIdentity $source $projection
    $recordedBoundary = if ($NormalAxisIsY) {
        ([double]$projection.startScreenYTop + [double]$projection.endScreenYTop) / 2.0
    } else {
        ([double]$projection.startScreenX + [double]$projection.endScreenX) / 2.0
    }
    $difference = Measure-StagedDifferenceBounds `
        -BeforePath (Resolve-MeasurementPath ([string]$source.backgroundMeasurementToken)) `
        -AfterPath (Resolve-SourcePath $SourceToken) `
        -SearchX 445 -SearchY 195 -SearchWidth 260 -SearchHeight 260
    $ghost = Select-RelevantDifferenceComponent $difference $recordedBoundary $NormalAxisIsY "Preview source '$SourceToken'"
    $tangentMinimum = if ($NormalAxisIsY) {
        [Math]::Min([double]$projection.startScreenX, [double]$projection.endScreenX)
    } else {
        [Math]::Min([double]$projection.startScreenYTop, [double]$projection.endScreenYTop)
    }
    $tangentMaximum = if ($NormalAxisIsY) {
        [Math]::Max([double]$projection.startScreenX, [double]$projection.endScreenX)
    } else {
        [Math]::Max([double]$projection.startScreenYTop, [double]$projection.endScreenYTop)
    }
    $normalMinimum = if ($NormalAxisIsY) {
        [Math]::Min([double]$projection.ownerCellCenterScreenYTop, [double]$projection.oppositeCellCenterScreenYTop)
    } else {
        [Math]::Min([double]$projection.ownerCellCenterScreenX, [double]$projection.oppositeCellCenterScreenX)
    }
    $normalMaximum = if ($NormalAxisIsY) {
        [Math]::Max([double]$projection.ownerCellCenterScreenYTop, [double]$projection.oppositeCellCenterScreenYTop)
    } else {
        [Math]::Max([double]$projection.ownerCellCenterScreenX, [double]$projection.oppositeCellCenterScreenX)
    }
    if ($NormalAxisIsY) {
        $escapesEnvelope = [int]$ghost.minX -lt ([Math]::Floor($tangentMinimum) - 4) -or
            [int]$ghost.maxX -gt ([Math]::Ceiling($tangentMaximum) + 4) -or
            [int]$ghost.minY -lt ([Math]::Floor($normalMinimum) - 4) -or
            [int]$ghost.maxY -gt ([Math]::Ceiling($normalMaximum) + 4)
    } else {
        $escapesEnvelope = [int]$ghost.minX -lt ([Math]::Floor($normalMinimum) - 4) -or
            [int]$ghost.maxX -gt ([Math]::Ceiling($normalMaximum) + 4) -or
            [int]$ghost.minY -lt ([Math]::Floor($tangentMinimum) - 4) -or
            [int]$ghost.maxY -gt ([Math]::Ceiling($tangentMaximum) + 4)
    }
    if ($escapesEnvelope) {
        throw "Preview source '$SourceToken' escapes the independently projected one-edge structural envelope."
    }
    $ghostMidpoint = if ($NormalAxisIsY) { [double]$ghost.midpointY } else { [double]$ghost.midpointX }
    if ([Math]::Abs($recordedBoundary - $ghostMidpoint) -gt 1.0) {
        throw "Preview source '$SourceToken' does not center its rendered structure on the Gateway-recorded shared edge ($ghostMidpoint vs $recordedBoundary)."
    }
}

function Test-StagedMasksOverlap([object]$First, [object]$Second) {
    if ($First.pixels.Count -le $Second.pixels.Count) {
        $smaller = $First.pixels
        $larger = $Second.pixels
    } else {
        $smaller = $Second.pixels
        $larger = $First.pixels
    }
    foreach ($pixel in $smaller) {
        if ($larger.Contains([int]$pixel)) { return $true }
    }
    return $false
}

function Assert-WorkbenchStageClearance(
    [string]$EmptyPath,
    [string]$WallOnlyPath,
    [string]$NorthAndWallPath,
    [string]$NoShadowFinalPath,
    [string]$PublicPath,
    [string]$Label
) {
    $wall = Measure-StagedDifferenceBounds $EmptyPath $WallOnlyPath
    $north = Measure-StagedDifferenceBounds $WallOnlyPath $NorthAndWallPath
    $south = Measure-StagedDifferenceBounds $NorthAndWallPath $NoShadowFinalPath
    if (Test-StagedMasksOverlap $north $wall) {
        throw "$Label north workbench overlaps or fuses with the complete Thin Wall mask."
    }
    if (Test-StagedMasksOverlap $south $wall) {
        throw "$Label south workbench overlaps or fuses with the complete Thin Wall mask."
    }
    if (Test-StagedMasksOverlap $north $south) {
        throw "$Label opposing workbench masks overlap."
    }

    $northGap = [int]$wall.minY - [int]$north.maxY - 1
    $southGap = [int]$south.minY - [int]$wall.maxY - 1
    $screenPixelsPerCell = ([int]$wall.maxX - [int]$wall.minX + 1) / 3.0
    $maximumAllowedGap = [Math]::Ceiling((2.0 / 60.0) * $screenPixelsPerCell)
    if ($northGap -lt 1 -or $southGap -lt 1) {
        throw "$Label has a fused, clipped, or overlapping complete bench/wall silhouette ($northGap/$southGap px)."
    }
    if ($northGap -gt $maximumAllowedGap -or $southGap -gt $maximumAllowedGap) {
        throw "$Label exceeds the minimal one-pixel safety band plus one 1/60-cell quantization allowance ($northGap/$southGap px; max $maximumAllowedGap)."
    }

    $noShadow = [Drawing.Bitmap]::new($NoShadowFinalPath)
    $public = [Drawing.Bitmap]::new($PublicPath)
    try {
        if ($noShadow.Width -ne $public.Width -or $noShadow.Height -ne $public.Height) {
            throw "$Label public and no-shadow final captures do not share one exact viewport."
        }
        $structuralPixels = [Collections.Generic.HashSet[int]]::new()
        foreach ($mask in @($wall, $north, $south)) {
            foreach ($key in $mask.pixels) { $null = $structuralPixels.Add([int]$key) }
        }
        foreach ($key in $structuralPixels) {
            $x = [int]$key % $noShadow.Width
            $y = [Math]::Floor([int]$key / $noShadow.Width)
            $a = $noShadow.GetPixel($x, $y)
            $b = $public.GetPixel($x, $y)
            $delta = [Math]::Max([Math]::Abs([int]$a.R - [int]$b.R), [Math]::Max(
                [Math]::Abs([int]$a.G - [int]$b.G),
                [Math]::Abs([int]$a.B - [int]$b.B)))
            if ($delta -gt 12) {
                throw "$Label public render clips or repaints a structural pixel at ($x,$y)."
            }
        }
        return [pscustomobject][ordered]@{
            northGapPixels = $northGap
            southGapPixels = $southGap
            maximumAllowedGapPixels = $maximumAllowedGap
            wallWidthPixels = ([int]$wall.maxX - [int]$wall.minX + 1)
        }
    }
    finally {
        $noShadow.Dispose()
        $public.Dispose()
    }
}

function Convert-ToPanelFile(
    [string]$InputPath,
    [string]$OutputPath,
    [int]$Width,
    [int]$Height,
    [string]$CropGeometry = ''
) {
    $arguments = [Collections.Generic.List[string]]::new()
    $arguments.Add($InputPath)
    if (-not [string]::IsNullOrWhiteSpace($CropGeometry)) {
        $arguments.Add('-crop')
        $arguments.Add($CropGeometry)
        $arguments.Add('+repage')
    }
    foreach ($argument in @('-filter', 'Lanczos', '-resize', "${Width}x${Height}^", '-gravity', 'center', '-extent', "${Width}x${Height}", '-strip', $OutputPath)) {
        $arguments.Add([string]$argument)
    }
    Invoke-Magick -Arguments $arguments.ToArray()
}

function Add-Panel(
    [string]$Canvas,
    [string]$SourceToken,
    [int]$X,
    [int]$Y,
    [int]$Width,
    [int]$Height,
    [string]$TemporaryRoot,
    [string]$Tag,
    [string]$Gravity = 'center',
    [string]$CropGeometry = ''
) {
    $sourcePath = Resolve-SourcePath $SourceToken
    $panelPath = Join-Path $TemporaryRoot ($Tag + '-panel.png')
    $framedPath = Join-Path $TemporaryRoot ($Tag + '-framed.png')
    $nextPath = Join-Path $TemporaryRoot ($Tag + '-next.png')
    $panelArguments = [System.Collections.Generic.List[string]]::new()
    $panelArguments.Add($sourcePath)
    if (-not [string]::IsNullOrWhiteSpace($CropGeometry)) {
        $panelArguments.Add('-crop')
        $panelArguments.Add($CropGeometry)
        $panelArguments.Add('+repage')
    }
    foreach ($argument in @('-filter', 'Lanczos', '-resize', "${Width}x${Height}^", '-gravity', $Gravity, '-extent', "${Width}x${Height}", '-strip', $panelPath)) {
        $panelArguments.Add([string]$argument)
    }
    Invoke-Magick -Arguments $panelArguments.ToArray()
    if ($Tag -ceq 'edge-south' -or $Tag -ceq 'edge-east') {
        $source = Get-Source $SourceToken
        if ([string]::IsNullOrWhiteSpace([string]$source.backgroundMeasurementToken)) {
            throw "Preview panel '$SourceToken' is missing its same-camera background measurement token."
        }
        $backgroundPanel = Join-Path $TemporaryRoot ($Tag + '-background-panel.png')
        Convert-ToPanelFile `
            -InputPath (Resolve-MeasurementPath ([string]$source.backgroundMeasurementToken)) `
            -OutputPath $backgroundPanel `
            -Width $Width `
            -Height $Height `
            -CropGeometry $CropGeometry
        $difference = Measure-StagedDifferenceBounds $backgroundPanel $panelPath
        if ($Tag -ceq 'edge-south') {
            $projection = Get-SharedEdgeProjection $SourceToken 'South'
            $start = Convert-SourcePointToPanel ([double]$projection.startScreenX) ([double]$projection.startScreenYTop) 445 195 260 260 $Width $Height
            $end = Convert-SourcePointToPanel ([double]$projection.endScreenX) ([double]$projection.endScreenYTop) 445 195 260 260 $Width $Height
            $expectedBoundary = ([double]$start.y + [double]$end.y) / 2.0
            $script:panelMeasurements[$Tag] = Select-RelevantDifferenceComponent $difference $expectedBoundary $true "Preview panel '$SourceToken'"
        } else {
            $projection = Get-SharedEdgeProjection $SourceToken 'East'
            $start = Convert-SourcePointToPanel ([double]$projection.startScreenX) ([double]$projection.startScreenYTop) 445 195 260 260 $Width $Height
            $end = Convert-SourcePointToPanel ([double]$projection.endScreenX) ([double]$projection.endScreenYTop) 445 195 260 260 $Width $Height
            $expectedBoundary = ([double]$start.x + [double]$end.x) / 2.0
            $script:panelMeasurements[$Tag] = Select-RelevantDifferenceComponent $difference $expectedBoundary $false "Preview panel '$SourceToken'"
        }
    }
    Invoke-Magick @('-size', "$($Width+8)x$($Height+8)", 'xc:#e8c993', $panelPath, '-geometry', '+4+4', '-composite', '-strip', $framedPath)
    Invoke-Magick @($Canvas, $framedPath, '-geometry', "+$($X-4)+$($Y-4)", '-composite', '-strip', $nextPath)
    return $nextPath
}

function Add-CellBoundaryOverlay(
    [string]$Canvas,
    [string]$TemporaryRoot
) {
    if (-not $script:panelMeasurements.ContainsKey('edge-south') -or
        -not $script:panelMeasurements.ContainsKey('edge-east')) {
        throw 'Both measured in-game preview panels are required before drawing cell boundaries.'
    }

    $south = $script:panelMeasurements['edge-south']
    $east = $script:panelMeasurements['edge-east']
    $southProjection = Get-SharedEdgeProjection 'preview-south' 'South'
    $eastProjection = Get-SharedEdgeProjection 'preview-east' 'East'
    $southStart = Convert-SourcePointToPanel `
        -SourceX ([double]$southProjection.startScreenX) `
        -SourceY ([double]$southProjection.startScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $southEnd = Convert-SourcePointToPanel `
        -SourceX ([double]$southProjection.endScreenX) `
        -SourceY ([double]$southProjection.endScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $southOwner = Convert-SourcePointToPanel `
        -SourceX ([double]$southProjection.ownerCellCenterScreenX) `
        -SourceY ([double]$southProjection.ownerCellCenterScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $southOpposite = Convert-SourcePointToPanel `
        -SourceX ([double]$southProjection.oppositeCellCenterScreenX) `
        -SourceY ([double]$southProjection.oppositeCellCenterScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $eastStart = Convert-SourcePointToPanel `
        -SourceX ([double]$eastProjection.startScreenX) `
        -SourceY ([double]$eastProjection.startScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $eastEnd = Convert-SourcePointToPanel `
        -SourceX ([double]$eastProjection.endScreenX) `
        -SourceY ([double]$eastProjection.endScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $eastOwner = Convert-SourcePointToPanel `
        -SourceX ([double]$eastProjection.ownerCellCenterScreenX) `
        -SourceY ([double]$eastProjection.ownerCellCenterScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $eastOpposite = Convert-SourcePointToPanel `
        -SourceX ([double]$eastProjection.oppositeCellCenterScreenX) `
        -SourceY ([double]$eastProjection.oppositeCellCenterScreenYTop) `
        -CropX 445 -CropY 195 -CropWidth 260 -CropHeight 260 -PanelWidth 350 -PanelHeight 330
    $southProjectionY = ([double]$southStart.y + [double]$southEnd.y) / 2.0
    $eastProjectionX = ([double]$eastStart.x + [double]$eastEnd.x) / 2.0
    if ([Math]::Abs([double]$south.midpointY - $southProjectionY) -gt 1.0 -or
        [Math]::Abs([double]$east.midpointX - $eastProjectionX) -gt 1.0) {
        throw 'The final preview panels drifted from the Gateway-recorded shared-edge projection.'
    }
    $southBoundary = 180 + [Math]::Floor($southProjectionY - 0.5)
    $southLeft = 25 + [Math]::Round([Math]::Min([double]$southStart.x, [double]$southEnd.x), 0, [MidpointRounding]::AwayFromZero)
    $southRight = 25 + [Math]::Round([Math]::Max([double]$southStart.x, [double]$southEnd.x), 0, [MidpointRounding]::AwayFromZero)
    $southCellSpan = [Math]::Round([Math]::Abs([double]$southOwner.y - [double]$southOpposite.y), 0, [MidpointRounding]::AwayFromZero)
    $southTop = $southBoundary - $southCellSpan
    $southBottom = $southBoundary + 1 + $southCellSpan
    $eastBoundary = 407 + [Math]::Floor($eastProjectionX)
    $eastTop = 180 + [Math]::Round([Math]::Min([double]$eastStart.y, [double]$eastEnd.y), 0, [MidpointRounding]::AwayFromZero)
    $eastBottom = 180 + [Math]::Round([Math]::Max([double]$eastStart.y, [double]$eastEnd.y), 0, [MidpointRounding]::AwayFromZero)
    $eastCellSpan = [Math]::Round([Math]::Abs([double]$eastOwner.x - [double]$eastOpposite.x), 0, [MidpointRounding]::AwayFromZero)
    $eastLeft = $eastBoundary - $eastCellSpan
    $eastRight = $eastBoundary + 1 + $eastCellSpan

    $nextPath = Join-Path $TemporaryRoot 'edge-cell-boundaries.png'
    Invoke-Magick @(
        $Canvas,
        '-fill', 'none',
        '-stroke', '#e8c993',
        '-strokewidth', '2',
        '-draw', "rectangle $southLeft,$southTop $southRight,$southBoundary rectangle $southLeft,$($southBoundary+1) $southRight,$southBottom",
        '-draw', "rectangle $eastLeft,$eastTop $eastBoundary,$eastBottom rectangle $($eastBoundary+1),$eastTop $eastRight,$eastBottom",
        '-strip',
        $nextPath)
    return $nextPath
}

function Add-Text([string]$Canvas, [string]$TemporaryRoot, [string]$Tag, [object[]]$TextRuns) {
    $nextPath = Join-Path $TemporaryRoot ($Tag + '-text.png')
    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add($Canvas)
    $arguments.Add('-font')
    $arguments.Add($script:fontPath)
    foreach ($run in $TextRuns) {
        Assert-TextFits -Text ([string]$run.text) -PointSize ([int]$run.size) -MaximumWidth ([int]$run.maxWidth) -FontPath $script:fontPath
        foreach ($argument in @(
            '-fill', [string]$run.fill,
            '-stroke', [string]$run.stroke,
            '-strokewidth', [string]$run.strokeWidth,
            '-pointsize', [string]$run.size,
            '-gravity', 'northwest',
            '-annotate', "+$([int]$run.x)+$([int]$run.y)",
            [string]$run.text)) {
            $arguments.Add([string]$argument)
        }
    }
    $arguments.Add('-strip')
    $arguments.Add($nextPath)
    Invoke-Magick -Arguments $arguments.ToArray()
    return $nextPath
}

function Finish-Png([string]$InputPath, [string]$OutputPath) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force
    Invoke-Magick @($InputPath, '-alpha', 'off', '-colorspace', 'sRGB', '-strip', '-define', 'png:compression-level=9', '-define', 'png:compression-filter=5', "PNG24:$OutputPath")
}

function Render-Title([string]$TemporaryRoot) {
    $copy = $script:manifest.title.copy
    $canvas = Join-Path $TemporaryRoot 'title-0.png'
    New-Canvas 1280 720 $canvas
    $canvas = Add-Panel $canvas 'room-presentation-clean' 565 50 665 620 $TemporaryRoot 'title-room' 'center' '500x465+700+180'
    $canvas = Add-Text $canvas $TemporaryRoot 'title' @(
        [pscustomobject]@{ text=[string]$copy.title; size=76; maxWidth=490; x=50; y=130; fill='#fff0d4'; stroke='#050505'; strokeWidth=2 },
        [pscustomobject]@{ text=[string]$copy.kicker; size=34; maxWidth=470; x=55; y=235; fill='#d85a36'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$copy.lines[0]; size=28; maxWidth=475; x=55; y=315; fill='#ead7b5'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$copy.lines[1]; size=25; maxWidth=475; x=55; y=370; fill='#ead7b5'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$copy.lines[2]; size=25; maxWidth=475; x=55; y=415; fill='#ead7b5'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$copy.footers[0]; size=21; maxWidth=475; x=55; y=540; fill='#9fb3c8'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$copy.footers[1]; size=21; maxWidth=475; x=55; y=585; fill='#9fb3c8'; stroke='none'; strokeWidth=0 }
    )
    Finish-Png $canvas (Resolve-OutputPath 'title')

    $aboutOutput = Resolve-OutputPath 'about'
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $aboutOutput) -Force
    Invoke-Magick @((Resolve-OutputPath 'title'), '-filter', 'Lanczos', '-resize', '640x360!', '-alpha', 'off', '-strip', '-define', 'png:compression-level=9', '-define', 'png:compression-filter=5', "PNG24:$aboutOutput")
}

function Render-EdgePlacementCard([string]$TemporaryRoot) {
    $card = Get-Card 'edge-placement'
    $canvas = Join-Path $TemporaryRoot 'edge-0.png'
    New-Canvas 1164 655 $canvas
    $canvas = Add-Text $canvas $TemporaryRoot 'edge-header' @(
        [pscustomobject]@{ text=[string]$card.title; size=52; maxWidth=850; x=45; y=24; fill='#fff0d4'; stroke='#050505'; strokeWidth=2 },
        [pscustomobject]@{ text=[string]$card.lines[0]; size=25; maxWidth=510; x=48; y=104; fill='#d85a36'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text=[string]$card.lines[1]; size=25; maxWidth=610; x=545; y=104; fill='#ead7b5'; stroke='none'; strokeWidth=0 }
    )
    $canvas = Add-Panel $canvas 'preview-south' 25 180 350 330 $TemporaryRoot 'edge-south' 'center' '260x260+445+195'
    $canvas = Add-Panel $canvas 'preview-east' 407 180 350 330 $TemporaryRoot 'edge-east' 'center' '260x260+445+195'
    $canvas = Add-Panel $canvas 'workbenches' 789 180 350 330 $TemporaryRoot 'edge-workbenches'
    $canvas = Add-CellBoundaryOverlay $canvas $TemporaryRoot
    $canvas = Add-Text $canvas $TemporaryRoot 'edge-labels' @(
        [pscustomobject]@{ text='SOUTH EDGE'; size=24; maxWidth=200; x=125; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='EAST EDGE'; size=24; maxWidth=200; x=520; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='BOTH SIDES USED'; size=24; maxWidth=260; x=860; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 }
    )
    Finish-Png $canvas (Resolve-OutputPath 'edge-placement')
}

function Render-ContinuousJoinsCard([string]$TemporaryRoot) {
    $card = Get-Card 'continuous-joins'
    $canvas = Join-Path $TemporaryRoot 'joins-0.png'
    New-Canvas 1164 655 $canvas
    $canvas = Add-Text $canvas $TemporaryRoot 'joins-header' @(
        [pscustomobject]@{ text=[string]$card.title; size=49; maxWidth=970; x=45; y=24; fill='#fff0d4'; stroke='#050505'; strokeWidth=2 },
        [pscustomobject]@{ text=[string]$card.lines[0]; size=24; maxWidth=1070; x=48; y=104; fill='#ead7b5'; stroke='none'; strokeWidth=0 }
    )
    $canvas = Add-Panel $canvas 'junction-l' 25 175 250 330 $TemporaryRoot 'joins-l'
    $canvas = Add-Panel $canvas 'junction-t' 295 175 250 330 $TemporaryRoot 'joins-t'
    $canvas = Add-Panel $canvas 'junction-plus' 565 175 250 330 $TemporaryRoot 'joins-plus'
    $canvas = Add-Panel $canvas 'regular-union' 835 175 304 330 $TemporaryRoot 'joins-union'
    $canvas = Add-Text $canvas $TemporaryRoot 'joins-labels' @(
        [pscustomobject]@{ text='L'; size=24; maxWidth=80; x=135; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='T'; size=24; maxWidth=80; x=405; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='+'; size=24; maxWidth=80; x=675; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='REGULAR WALL UNION'; size=24; maxWidth=260; x=875; y=535; fill='#e8c993'; stroke='none'; strokeWidth=0 }
    )
    Finish-Png $canvas (Resolve-OutputPath 'continuous-joins')
}

function Render-ThinDoorsCard([string]$TemporaryRoot) {
    $card = Get-Card 'thin-doors'
    $canvas = Join-Path $TemporaryRoot 'doors-0.png'
    New-Canvas 1164 655 $canvas
    $canvas = Add-Text $canvas $TemporaryRoot 'doors-header' @(
        [pscustomobject]@{ text=[string]$card.title; size=53; maxWidth=880; x=45; y=24; fill='#fff0d4'; stroke='#050505'; strokeWidth=2 },
        [pscustomobject]@{ text=[string]$card.lines[0]; size=24; maxWidth=1050; x=48; y=104; fill='#ead7b5'; stroke='none'; strokeWidth=0 }
    )
    $canvas = Add-Panel $canvas 'room-closed' 25 175 540 405 $TemporaryRoot 'doors-room' 'center' '540x405+520+80'
    $canvas = Add-Panel $canvas 'door-detail-closed' 599 175 540 170 $TemporaryRoot 'doors-closed' 'center' '330x70+100+100'
    $canvas = Add-Panel $canvas 'door-detail-open' 599 410 540 170 $TemporaryRoot 'doors-open' 'center' '330x70+100+100'
    $canvas = Add-Text $canvas $TemporaryRoot 'doors-labels' @(
        [pscustomobject]@{ text='FURNISHED ROOM'; size=24; maxWidth=240; x=205; y=600; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='CLOSED EDGE'; size=24; maxWidth=220; x=780; y=360; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='OPEN CROSSING'; size=24; maxWidth=240; x=775; y=600; fill='#e8c993'; stroke='none'; strokeWidth=0 }
    )
    Finish-Png $canvas (Resolve-OutputPath 'thin-doors')
}

function Render-MaterialsDamageCard([string]$TemporaryRoot) {
    $card = Get-Card 'materials-damage'
    $canvas = Join-Path $TemporaryRoot 'damage-0.png'
    New-Canvas 1164 655 $canvas
    $canvas = Add-Text $canvas $TemporaryRoot 'damage-header' @(
        [pscustomobject]@{ text=[string]$card.title; size=48; maxWidth=1030; x=45; y=24; fill='#fff0d4'; stroke='#050505'; strokeWidth=2 },
        [pscustomobject]@{ text=[string]$card.lines[0]; size=23; maxWidth=1080; x=48; y=104; fill='#ead7b5'; stroke='none'; strokeWidth=0 }
    )
    $canvas = Add-Panel $canvas 'damage-stone' 45 205 455 122 $TemporaryRoot 'damage-stone' 'center' '260x70+18+30'
    $canvas = Add-Panel $canvas 'damage-wood' 664 205 455 122 $TemporaryRoot 'damage-wood' 'center' '260x70+18+30'
    $canvas = Add-Panel $canvas 'damage-steel' 355 455 455 122 $TemporaryRoot 'damage-steel' 'center' '260x70+18+30'
    $canvas = Add-Text $canvas $TemporaryRoot 'damage-labels' @(
        [pscustomobject]@{ text='STONE  -  MODERATE  /  HEAVY  /  SEVERE'; size=20; maxWidth=455; x=45; y=165; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='WOOD  -  MODERATE  /  HEAVY  /  SEVERE'; size=20; maxWidth=455; x=664; y=165; fill='#e8c993'; stroke='none'; strokeWidth=0 },
        [pscustomobject]@{ text='STEEL  -  MODERATE  /  HEAVY  /  SEVERE'; size=20; maxWidth=455; x=355; y=415; fill='#e8c993'; stroke='none'; strokeWidth=0 }
    )
    Finish-Png $canvas (Resolve-OutputPath 'materials-damage')
}

function Render-ContactSheet([string]$TemporaryRoot) {
    $canvas = Join-Path $TemporaryRoot 'contact-0.png'
    New-Canvas 1164 655 $canvas
    $placements = @(
        @{ token='edge-placement'; x=8; y=8 },
        @{ token='continuous-joins'; x=586; y=8 },
        @{ token='thin-doors'; x=8; y=331 },
        @{ token='materials-damage'; x=586; y=331 }
    )
    $index = 0
    foreach ($placement in $placements) {
        $index++
        $thumbnail = Join-Path $TemporaryRoot ("contact-thumb-$index.png")
        $next = Join-Path $TemporaryRoot ("contact-$index.png")
        Invoke-Magick @((Resolve-OutputPath ([string]$placement.token)), '-filter', 'Lanczos', '-resize', '570x319!', '-strip', $thumbnail)
        Invoke-Magick @($canvas, $thumbnail, '-geometry', "+$([int]$placement.x)+$([int]$placement.y)", '-composite', '-strip', $next)
        $canvas = $next
    }
    Finish-Png $canvas (Resolve-OutputPath 'contact-sheet')
}

function Assert-InputContracts {
    $expectedVersion = [string]$script:manifest.renderer.imageMagickVersion
    $versionLine = (Invoke-MagickCapture @('-version')).Split([Environment]::NewLine)[0]
    if ($versionLine -cne "Version: ImageMagick $expectedVersion d4e4b2b:20260322 https://imagemagick.org") {
        throw "Unsupported ImageMagick build. Expected '$expectedVersion'; observed '$versionLine'."
    }
    if ((Get-Sha256 $script:rendererPath) -cne [string]$script:manifest.renderer.sha256) { throw 'Presentation renderer hash does not match the manifest.' }
    if ((Get-Sha256 $script:fontPath) -cne [string]$script:manifest.renderer.fontSha256) { throw 'Presentation font hash does not match the manifest.' }
    if ([bool]$script:manifest.imageGenerationUsed) { throw 'Generated-image provenance is forbidden for Thin Walls presentation inputs.' }
    if ([string]$script:manifest.schema -cne 'ThinWalls/WorkshopPresentation/v3') {
        throw 'The presentation manifest schema is not supported.'
    }
    $expectedRuns = [ordered]@{
        '20260820T013908908Z' = '489b6eb405ee428fa2473995e8da2e31'
        '20260820T020606902Z' = '3f7b3b5426ae495899118400e2612571'
        '20260820T021022450Z' = '6609d360a92d43a7b4ec6a7637bea1fc'
    }
    $acceptedRuns = @($script:manifest.acceptedRuns)
    if ($acceptedRuns.Count -ne $expectedRuns.Count) { throw 'The presentation manifest does not name the exact accepted Thin Walls runs.' }
    foreach ($runId in $expectedRuns.Keys) {
        $matchingRuns = @($acceptedRuns | Where-Object { [string]$_.runId -ceq $runId })
        if ($matchingRuns.Count -ne 1 -or
            [string]$matchingRuns[0].sessionId -cne [string]$expectedRuns[$runId] -or
            [string]$matchingRuns[0].launchWindowStyle -cne 'Minimized' -or
            [bool]$matchingRuns[0].visibleWindow -or
            [string]$matchingRuns[0].reviewVerdict -cne 'passed') {
            throw "Accepted run provenance is incomplete or changed: $runId"
        }
    }

    foreach ($source in @($script:manifest.sources)) {
        $path = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$source.path).Replace('/', '\')))
        $sourceRunId = [string]$source.runId
        if (-not $expectedRuns.Contains($sourceRunId) -or [string]$source.sessionId -cne [string]$expectedRuns[$sourceRunId]) {
            throw "Source is not bound to an accepted run/session: $($source.token)"
        }
        $allowedRoot = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot "mods\ThinWalls\Release\workshop\sources\in-game\accepted-$sourceRunId")) + [IO.Path]::DirectorySeparatorChar
        if (-not $path.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Source escaped the accepted in-game directory: $path" }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing accepted in-game source: $path" }
        if ((Get-Sha256 $path) -cne [string]$source.sha256) { throw "Accepted source hash changed: $($source.token)" }
        $dimensions = Invoke-MagickCapture @('identify', '-format', '%wx%h', $path)
        if ($dimensions -cne "$([int]$source.width)x$([int]$source.height)") { throw "Accepted source dimensions changed: $($source.token)" }
        if ([string]$source.reviewVerdict -cne 'passed' -or [bool]$source.visibleWindow -or [string]$source.launchWindowStyle -cne 'Minimized') {
            throw "Source is not accepted minimized-run evidence: $($source.token)"
        }
        if ([string]$source.placementReviewScope -cne 'itemized-realistic-scene' -and
            [string]$source.placementReviewScope -cne 'technical-catalog-no-realistic-fixtures') {
            throw "Source has no explicit publication-placement review scope: $($source.token)"
        }
    }

    foreach ($input in @($script:manifest.measurementInputs)) {
        $path = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$input.path).Replace('/', '\')))
        $runId = [string]$input.runId
        if (-not $expectedRuns.Contains($runId) -or [string]$input.sessionId -cne [string]$expectedRuns[$runId]) {
            throw "Measurement input is not bound to an accepted run/session: $($input.token)"
        }
        $allowedRoot = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot "mods\ThinWalls\Release\workshop\measurements\in-game\accepted-$runId")) + [IO.Path]::DirectorySeparatorChar
        if (-not $path.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Measurement input escaped its accepted supporting directory: $path" }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing staged measurement input: $path" }
        if ((Get-Sha256 $path) -cne [string]$input.sha256) { throw "Staged measurement input hash changed: $($input.token)" }
        $dimensions = Invoke-MagickCapture @('identify', '-format', '%wx%h', $path)
        if ($dimensions -cne "$([int]$input.width)x$([int]$input.height)") { throw "Staged measurement input dimensions changed: $($input.token)" }
    }

    $requiredScopes = Get-RequiredPlacementScopes
    $observedScopes = [ordered]@{}
    foreach ($source in @($script:manifest.sources)) {
        $observedScopes['source:' + [string]$source.token] = [string]$source.placementReviewScope
    }
    foreach ($definition in @($script:manifest.outputs)) {
        $observedScopes['output:' + [string]$definition.token] = [string]$definition.placementReviewScope
    }
    $requiredScopeKeys = @($requiredScopes.Keys | Sort-Object)
    $observedScopeKeys = @($observedScopes.Keys | Sort-Object)
    if ((Compare-Object $requiredScopeKeys $observedScopeKeys).Count -ne 0) {
        throw 'Publication targets do not match the pinned placement-review scope inventory.'
    }
    foreach ($target in $requiredScopeKeys) {
        if ([string]$observedScopes[$target] -cne [string]$requiredScopes[$target]) {
            throw "Publication target selected the wrong placement-review scope: $target"
        }
    }

    $requiredCategories = Get-RequiredPlacementAuditCategories
    $itemizedTargets = @($requiredCategories.Keys | Sort-Object)
    $auditedTargets = @($script:manifest.reviews.placementAudits |
        ForEach-Object { [string]$_.targetKind + ':' + [string]$_.targetToken } |
        Sort-Object)
    if ((Compare-Object -ReferenceObject $itemizedTargets -DifferenceObject $auditedTargets).Count -ne 0) {
        throw 'The itemized publication sources/outputs and retained placement audits do not match exactly.'
    }
    foreach ($audit in @($script:manifest.reviews.placementAudits)) {
        $target = [string]$audit.targetKind + ':' + [string]$audit.targetToken
        if ([string]::IsNullOrWhiteSpace([string]$audit.reviewerTask) -or
            [string]$audit.verdict -cne 'passed' -or
            @($audit.items).Count -eq 0) {
            throw "Placement audit is incomplete: $($audit.targetKind)/$($audit.targetToken)"
        }
        $observedCategories = @($audit.items | ForEach-Object { [string]$_.category })
        if ((Compare-Object -SyncWindow 0 -ReferenceObject @($requiredCategories[$target]) -DifferenceObject $observedCategories).Count -ne 0) {
            throw "Placement audit does not enumerate the exact visible categories in pinned order: $target"
        }
        foreach ($item in @($audit.items)) {
            if ([int]$item.visibleCount -le 0 -or
                [int]$item.reviewedCount -ne [int]$item.visibleCount -or
                [int]$item.unresolvedFindingCount -ne 0 -or
                [string]::IsNullOrWhiteSpace([string]$item.observation)) {
                throw "Placement audit item is incomplete: $($audit.targetKind)/$($audit.targetToken)/$($item.category)"
            }
        }
    }

    $sourceRoot = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot 'mods\ThinWalls\Release\workshop\sources\in-game'))
    $expectedSourcePaths = @($script:manifest.sources | ForEach-Object {
        [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$_.path).Replace('/', '\')))
    } | Sort-Object)
    $actualSourcePaths = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.png' |
        ForEach-Object { $_.FullName } |
        Sort-Object)
    $sourceDifference = @(Compare-Object -ReferenceObject $expectedSourcePaths -DifferenceObject $actualSourcePaths)
    if ($expectedSourcePaths.Count -ne $actualSourcePaths.Count -or $sourceDifference.Count -ne 0) {
        throw 'The accepted in-game source tree contains superseded or unmanifested PNG captures.'
    }

    $measurementRoot = [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot 'mods\ThinWalls\Release\workshop\measurements\in-game'))
    $expectedMeasurementPaths = @($script:manifest.measurementInputs | ForEach-Object {
        [IO.Path]::GetFullPath((Join-Path $script:repositoryRoot ([string]$_.path).Replace('/', '\')))
    } | Sort-Object)
    $actualMeasurementPaths = if (Test-Path -LiteralPath $measurementRoot -PathType Container) {
        @(Get-ChildItem -LiteralPath $measurementRoot -Recurse -File -Filter '*.png' | ForEach-Object { $_.FullName } | Sort-Object)
    } else { @() }
    if ((Compare-Object -ReferenceObject $expectedMeasurementPaths -DifferenceObject $actualMeasurementPaths).Count -ne 0) {
        throw 'The accepted staged-measurement tree contains superseded or unmanifested PNG captures.'
    }

    $workbenchSource = Get-Source 'workbenches'
    Assert-SourcePreviewProjection -SourceToken 'preview-south' -ExpectedOwnedSide 'South' -NormalAxisIsY $true
    Assert-SourcePreviewProjection -SourceToken 'preview-east' -ExpectedOwnedSide 'East' -NormalAxisIsY $false
    $null = Assert-WorkbenchStageClearance `
        -EmptyPath (Resolve-MeasurementPath ([string]$workbenchSource.emptyMeasurementToken)) `
        -WallOnlyPath (Resolve-MeasurementPath ([string]$workbenchSource.wallOnlyMeasurementToken)) `
        -NorthAndWallPath (Resolve-MeasurementPath ([string]$workbenchSource.northAndWallMeasurementToken)) `
        -NoShadowFinalPath (Resolve-MeasurementPath ([string]$workbenchSource.noShadowFinalMeasurementToken)) `
        -PublicPath (Resolve-SourcePath 'workbenches') `
        -Label 'Promoted in-game workbench source'
}

function Assert-Outputs([switch]$AllowRefresh) {
    $verified = $true
    foreach ($definition in @($script:manifest.outputs)) {
        $path = [IO.Path]::GetFullPath((Join-Path $script:resolvedOutputRoot ([string]$definition.path).Replace('/', '\')))
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing rendered presentation output: $path" }
        $dimensions = Invoke-MagickCapture @('identify', '-format', '%wx%h', $path)
        if ($dimensions -cne "$([int]$definition.width)x$([int]$definition.height)") { throw "Output dimensions changed: $($definition.token)" }
        $bytes = (Get-Item -LiteralPath $path).Length
        $sha256 = Get-Sha256 $path
        if ($AllowRefresh) {
            $definition.bytes = $bytes
            $definition.sha256 = $sha256
        }
        elseif ($bytes -ne [long]$definition.bytes -or $sha256 -cne [string]$definition.sha256) {
            $verified = $false
            throw "Rendered presentation output does not match its approved hash: $($definition.token)"
        }
        if ([string]$definition.role -ceq 'workshop-card' -and $bytes -ge 1MB) { throw "Workshop card exceeds 1 MiB: $($definition.token)" }
        $lastX = [int]$definition.width - 1
        $lastY = [int]$definition.height - 1
        $cornerFormat = "%[pixel:p{0,0}]|%[pixel:p{${lastX},${lastY}}]"
        $corners = Invoke-MagickCapture -Arguments @($path, '-format', $cornerFormat, 'info:')
        if ($corners -notmatch '^srgb\(27,40,56\)\|srgb\(27,40,56\)$') { throw "Workshop page matte is missing from output corners: $($definition.token) ($corners)" }
    }
    $workbenchSource = Get-Source 'workbenches'
    $measurementTemp = Join-Path ([IO.Path]::GetTempPath()) ('thin-walls-workbench-measurement-' + [Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $measurementTemp
    try {
        $emptyPanel = Join-Path $measurementTemp 'empty.png'
        $wallPanel = Join-Path $measurementTemp 'wall.png'
        $northPanel = Join-Path $measurementTemp 'north.png'
        $finalPanel = Join-Path $measurementTemp 'final.png'
        $publicPanel = Join-Path $measurementTemp 'public.png'
        Convert-ToPanelFile (Resolve-MeasurementPath ([string]$workbenchSource.emptyMeasurementToken)) $emptyPanel 350 330
        Convert-ToPanelFile (Resolve-MeasurementPath ([string]$workbenchSource.wallOnlyMeasurementToken)) $wallPanel 350 330
        Convert-ToPanelFile (Resolve-MeasurementPath ([string]$workbenchSource.northAndWallMeasurementToken)) $northPanel 350 330
        Convert-ToPanelFile (Resolve-MeasurementPath ([string]$workbenchSource.noShadowFinalMeasurementToken)) $finalPanel 350 330
        Invoke-Magick @((Resolve-OutputPath 'edge-placement'), '-crop', '350x330+789+180', '+repage', '-strip', $publicPanel)
        $null = Assert-WorkbenchStageClearance $emptyPanel $wallPanel $northPanel $finalPanel $publicPanel 'Final edge-placement workbench panel'
    }
    finally {
        if (Test-Path -LiteralPath $measurementTemp) { Remove-Item -LiteralPath $measurementTemp -Recurse -Force }
    }
    return $verified
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$rendererPath = [IO.Path]::GetFullPath($MyInvocation.MyCommand.Path)
$manifestPath = Join-Path $repositoryRoot 'mods\ThinWalls\Release\workshop\presentation.json'
$fontPath = (Join-Path $repositoryRoot 'release\templates\workshop\fonts\Oswald-SemiBold.ttf').Replace('\', '/')
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$resolvedOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $repositoryRoot } else { [IO.Path]::GetFullPath($OutputRoot) }

Assert-InputContracts

$temporaryRoot = $null
try {
    if (-not $VerifyOnly) {
        $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('thin-walls-presentation-' + [Guid]::NewGuid().ToString('N'))
        $null = New-Item -ItemType Directory -Path $temporaryRoot
        Render-Title $temporaryRoot
        Render-EdgePlacementCard $temporaryRoot
        Render-ContinuousJoinsCard $temporaryRoot
        Render-ThinDoorsCard $temporaryRoot
        Render-MaterialsDamageCard $temporaryRoot
        Render-ContactSheet $temporaryRoot
    }

    $verified = Assert-Outputs -AllowRefresh:$RefreshManifest
    if ($RefreshManifest) {
        $manifest | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
        $verified = $true
    }

    $result = [pscustomobject][ordered]@{
        schema = [string]$manifest.schema
        verified = [bool]$verified
        outputRoot = $resolvedOutputRoot
        outputs = @($manifest.outputs).Count
        cards = @($manifest.cards).Count
        imageGenerationUsed = [bool]$manifest.imageGenerationUsed
    }
    if ($Output -ceq 'json') { $result | ConvertTo-Json -Compress }
    else { $result | Format-List }
}
finally {
    if ($null -ne $temporaryRoot -and (Test-Path -LiteralPath $temporaryRoot)) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
