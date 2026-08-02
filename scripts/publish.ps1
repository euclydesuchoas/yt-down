<#
.SYNOPSIS
    Gera uma versao do YTDown pronta para ser entregue a outra pessoa.

.DESCRIPTION
    Publica o aplicativo em modo self-contained: o .NET vai junto, entao a maquina
    de destino nao precisa ter nada instalado. Esse e o ponto para o publico deste
    aplicativo, que nao vai instalar um runtime antes de baixar um video.

    O resultado e uma pasta com o executavel e um zip dela. Nao e um instalador:
    quem receber extrai e executa YTDown.exe.

    As ferramentas externas entram no pacote a partir de tools/, e sao baixadas
    antes caso estejam faltando.

.PARAMETER Configuration
    Release por padrao. Debug so faz sentido para investigar o proprio empacotamento.

.PARAMETER OutputDirectory
    Onde gravar. Por padrao dist/, na raiz do repositorio.

.PARAMETER SkipZip
    Gera apenas a pasta, sem compactar.

.EXAMPLE
    ./scripts/publish.ps1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutputDirectory,
    [switch] $SkipZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src/YTDown.UI/YTDown.UI.csproj'
$toolsDirectory = Join-Path $repositoryRoot 'tools'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot 'dist'
}

# O identificador fixa 64 bits: o FFmpeg e o yt-dlp empacotados sao dessa
# arquitetura, e um Windows de 32 bits nao executaria nem um nem outro.
$runtimeIdentifier = 'win-x64'

function Get-ProjectVersion {
    $content = Get-Content -LiteralPath $projectPath -Raw
    if ($content -match '<Version>([^<]+)</Version>') { return $Matches[1] }

    return '0.0.0'
}

# O @( ) externo importa: sem ele, nenhum resultado vira $null, e $null.Count
# derruba o script sob Set-StrictMode no Windows PowerShell.
$missingTools = @(
    @('yt-dlp.exe', 'ffmpeg.exe') |
        Where-Object { -not (Test-Path -LiteralPath (Join-Path $toolsDirectory $_)) })

if ($missingTools.Count -gt 0) {
    Write-Host "[ferramentas] ausentes ($($missingTools -join ', ')). Baixando..."
    & (Join-Path $PSScriptRoot 'bootstrap-tools.ps1')
}

$version = Get-ProjectVersion
$stagingDirectory = Join-Path $OutputDirectory "YTDown-$version-$runtimeIdentifier"

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

Write-Host "[publish]     YTDown $version ($Configuration, $runtimeIdentifier, self-contained)..."

# --self-contained leva o .NET junto; sem trimming, porque o WPF depende de
# reflexao para carregar XAML e o recorte removeria o que ele procura em tempo
# de execucao.
& dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $runtimeIdentifier `
    --self-contained true `
    --output $stagingDirectory `
    --nologo `
    -p:PublishTrimmed=false | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou com codigo $LASTEXITCODE."
}

# O aplicativo nao funciona sem as ferramentas, e a ausencia so apareceria para
# quem recebesse o pacote. Conferir aqui transforma isso em falha de quem publica.
$expectedTools = @('yt-dlp.exe', 'ffmpeg.exe', 'tools.lock.json')
$publishedToolsDirectory = Join-Path $stagingDirectory 'tools'

foreach ($tool in $expectedTools) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishedToolsDirectory $tool))) {
        throw "O pacote saiu sem tools/$tool. O aplicativo nao funcionaria na maquina de destino."
    }
}

$executablePath = Join-Path $stagingDirectory 'YTDown.exe'
if (-not (Test-Path -LiteralPath $executablePath)) {
    throw "O pacote saiu sem YTDown.exe."
}

$sizeInMegabytes = [math]::Round(
    ((Get-ChildItem -LiteralPath $stagingDirectory -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)

Write-Host "[ok]          pasta pronta: $stagingDirectory ($sizeInMegabytes MB)"

if (-not $SkipZip) {
    $archivePath = "$stagingDirectory.zip"

    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    Write-Host '[zip]         compactando...'
    Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $archivePath

    $archiveInMegabytes = [math]::Round(((Get-Item -LiteralPath $archivePath).Length / 1MB), 1)
    Write-Host "[ok]          $archivePath ($archiveInMegabytes MB)"
}

Write-Host ''
Write-Host 'Quem receber o pacote extrai e executa YTDown.exe. Nada precisa ser instalado.'
Write-Host 'O Windows vai avisar que o programa e de origem desconhecida: o executavel nao e assinado.'
