Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-RimWorldXml10Text {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Text
    )

    if ($null -eq $Text) {
        return ''
    }

    $builder = [System.Text.StringBuilder]::new($Text.Length)
    for ($index = 0; $index -lt $Text.Length; $index++) {
        $value = [int]$Text[$index]
        if ($value -ge 0xD800 -and $value -le 0xDBFF) {
            if ($index + 1 -lt $Text.Length) {
                $next = [int]$Text[$index + 1]
                if ($next -ge 0xDC00 -and $next -le 0xDFFF) {
                    $null = $builder.Append($Text[$index])
                    $null = $builder.Append($Text[$index + 1])
                    $index++
                    continue
                }
            }

            $null = $builder.Append([char]0xFFFD)
            continue
        }

        if (($value -ge 0xDC00 -and $value -le 0xDFFF) -or
            -not ($value -eq 0x09 -or $value -eq 0x0A -or $value -eq 0x0D -or
                  ($value -ge 0x20 -and $value -le 0xD7FF) -or
                  ($value -ge 0xE000 -and $value -le 0xFFFD))) {
            $null = $builder.Append([char]0xFFFD)
            continue
        }

        $null = $builder.Append($Text[$index])
    }

    return $builder.ToString()
}

function Write-RimWorldEndToEndJUnitReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$GroupResults
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $failures = @($GroupResults | Where-Object { [string]$_.Status -ne 'passed' }).Count
        $writer.WriteStartDocument()
        $writer.WriteStartElement('testsuite')
        $writer.WriteAttributeString('name', 'RimWorld E2E exact mod groups')
        $writer.WriteAttributeString('tests', [string]$GroupResults.Count)
        $writer.WriteAttributeString('failures', [string]$failures)
        foreach ($groupResult in $GroupResults) {
            $writer.WriteStartElement('testcase')
            $writer.WriteAttributeString('classname', 'RimWorld.EndToEnd.ModGroup')
            $writer.WriteAttributeString(
                'name',
                (ConvertTo-RimWorldXml10Text -Text ([string]$groupResult.GroupId)))
            if ([string]$groupResult.Status -ne 'passed') {
                $writer.WriteStartElement('failure')
                $writer.WriteAttributeString(
                    'message',
                    (ConvertTo-RimWorldXml10Text -Text ([string]$groupResult.Message)))
                $failureDetail = if (Test-Path -LiteralPath ([string]$groupResult.StandardError) -PathType Leaf) {
                    Get-Content -LiteralPath ([string]$groupResult.StandardError) -Raw
                }
                else {
                    [string]$groupResult.Message
                }
                $writer.WriteString((ConvertTo-RimWorldXml10Text -Text $failureDetail))
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Get-RimWorldDeployedProductEvidence {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PackageId,

        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$RimWorldPath,

        [Parameter(Mandatory)]
        [string]$BuildLogPath
    )

    $resolvedProject = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
    $resolvedGame = (Resolve-Path -LiteralPath $RimWorldPath -ErrorAction Stop).Path
    $resolvedBuildLog = (Resolve-Path -LiteralPath $BuildLogPath -ErrorAction Stop).Path
    $packageRoot = Join-Path (Join-Path $resolvedGame 'Mods') $PackageId
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
        throw "Deployed product package '$PackageId' does not exist at '$packageRoot'."
    }

    $packageRoot = (Resolve-Path -LiteralPath $packageRoot -ErrorAction Stop).Path
    $packageRootInfo = Get-Item -LiteralPath $packageRoot -Force -ErrorAction Stop
    if (($packageRootInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Deployed product package '$PackageId' must not be a reparse point."
    }

    $entries = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop)
    foreach ($entry in $entries) {
        if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Deployed product package '$PackageId' contains a reparse entry."
        }
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @($entries | Where-Object { -not $_.PSIsContainer })) {
        $relativePath = $file.FullName.Substring($packageRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $segments = @($relativePath -split '/')
        if (@($segments | Where-Object { $_ -ieq 'DevEndToEndTests' }).Count -gt 0) {
            continue
        }

        $files.Add([pscustomobject]@{
            RelativePath = $relativePath
            Length = [long]$file.Length
            Sha256 = (Get-FileHash `
                -LiteralPath $file.FullName `
                -Algorithm SHA256 `
                -ErrorAction Stop).Hash.ToUpperInvariant()
        })
    }

    $orderedFiles = @($files | Sort-Object -Property RelativePath -CaseSensitive)
    if ($orderedFiles.Count -eq 0) {
        throw "Deployed product package '$PackageId' contains no product files."
    }

    return [pscustomobject]@{
        PackageId = $PackageId
        Project = $resolvedProject
        PackageRoot = $packageRoot
        BuildLog = $resolvedBuildLog
        Files = $orderedFiles
    }
}

function ConvertTo-RimWorldProductEvidenceJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Evidence
    )

    return ConvertTo-Json -InputObject @($Evidence) -Depth 8 -Compress
}

Export-ModuleMember -Function `
    ConvertTo-RimWorldXml10Text, `
    Write-RimWorldEndToEndJUnitReport, `
    Get-RimWorldDeployedProductEvidence, `
    ConvertTo-RimWorldProductEvidenceJson
