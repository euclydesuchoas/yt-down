<#
.SYNOPSIS
    Baixa as ferramentas externas (yt-dlp e FFmpeg) exigidas pelo YTDown.

.DESCRIPTION
    Os binários não são versionados no Git: são grandes e atualizados com frequência.
    As versões usadas ficam fixadas em tools/tools.lock.json, junto do SHA256 esperado.

    Um arquivo já presente e com hash correto não é baixado novamente, então rodar
    o script várias vezes é barato e seguro.

.PARAMETER Force
    Rebaixa as ferramentas mesmo que já estejam presentes e íntegras.

.EXAMPLE
    pwsh ./scripts/bootstrap-tools.ps1
#>
[CmdletBinding()]
param(
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$toolsDirectory = Join-Path $repositoryRoot 'tools'
$manifestPath = Join-Path $toolsDirectory 'tools.lock.json'

function Test-ToolIsUpToDate {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ExpectedSha256
    )

    if (-not (Test-Path -LiteralPath $Path)) { return $false }

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    return $actual -eq $ExpectedSha256
}

function Save-DirectDownload {
    param(
        [Parameter(Mandatory)] [string] $Url,
        [Parameter(Mandatory)] [string] $DestinationPath
    )

    Invoke-WebRequest -Uri $Url -OutFile $DestinationPath -UseBasicParsing
}

function Save-ZipEntry {
    param(
        [Parameter(Mandatory)] [string] $Url,
        [Parameter(Mandatory)] [string] $EntryName,
        [Parameter(Mandatory)] [string] $DestinationPath
    )

    $temporaryZip = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName() + '.zip')
    try {
        Invoke-WebRequest -Uri $Url -OutFile $temporaryZip -UseBasicParsing

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($temporaryZip)
        try {
            # O pacote do FFmpeg traz os executáveis em bin/, então localizamos pelo nome.
            $entry = $archive.Entries | Where-Object { $_.Name -eq $EntryName } | Select-Object -First 1
            if ($null -eq $entry) {
                throw "Entrada '$EntryName' não encontrada no pacote baixado de $Url."
            }

            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $DestinationPath, $true)
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        Remove-Item -LiteralPath $temporaryZip -Force -ErrorAction SilentlyContinue
    }
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Manifesto não encontrado em $manifestPath."
}

if (-not (Test-Path -LiteralPath $toolsDirectory)) {
    New-Item -ItemType Directory -Path $toolsDirectory | Out-Null
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

foreach ($tool in $manifest.tools) {
    $destinationPath = Join-Path $toolsDirectory $tool.fileName

    if (-not $Force -and (Test-ToolIsUpToDate -Path $destinationPath -ExpectedSha256 $tool.sha256)) {
        Write-Host "[ok]       $($tool.name) $($tool.version) já está presente e íntegro."
        continue
    }

    Write-Host "[baixando] $($tool.name) $($tool.version)..."

    switch ($tool.kind) {
        'direct' { Save-DirectDownload -Url $tool.url -DestinationPath $destinationPath }
        'zip'    { Save-ZipEntry -Url $tool.url -EntryName $tool.entryName -DestinationPath $destinationPath }
        default  { throw "Tipo de download desconhecido: '$($tool.kind)'." }
    }

    $actualSha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
    if ($actualSha256 -ne $tool.sha256) {
        Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
        throw "SHA256 divergente para $($tool.name). Esperado $($tool.sha256), obtido $actualSha256."
    }

    Write-Host "[ok]       $($tool.name) $($tool.version) baixado e verificado."
}

Write-Host ''
Write-Host "Ferramentas prontas em $toolsDirectory"
