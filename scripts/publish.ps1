<#
.SYNOPSIS
    Gera uma versão do YTDown pronta para ser entregue a outra pessoa.

.DESCRIPTION
    Publica o aplicativo em modo self-contained: o .NET vai junto, então a máquina
    de destino não precisa ter nada instalado. Esse é o ponto para o público deste
    aplicativo, que não vai instalar um runtime antes de baixar um vídeo.

    O resultado é uma pasta com o executável e um zip dela. Não é um instalador:
    quem receber extrai e executa YTDown.exe.

    As ferramentas externas entram no pacote a partir de tools/, e são baixadas
    antes caso estejam faltando.

.PARAMETER Configuration
    Release por padrão. Debug só faz sentido para investigar o próprio empacotamento.

.PARAMETER OutputDirectory
    Onde gravar. Por padrão dist/, na raiz do repositório.

.PARAMETER SkipZip
    Não compacta a pasta publicada.

.PARAMETER SkipInstaller
    Não gera o instalador, mesmo com o Inno Setup presente.

.EXAMPLE
    ./scripts/publish.ps1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutputDirectory,
    [switch] $SkipZip,
    [switch] $SkipInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src/YTDown.UI/YTDown.UI.csproj'
$toolsDirectory = Join-Path $repositoryRoot 'tools'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot 'dist'
}

# O identificador fixa 64 bits: o FFmpeg e o yt-dlp empacotados são dessa
# arquitetura, e um Windows de 32 bits não executaria nem um nem outro.
$runtimeIdentifier = 'win-x64'

function Find-InnoSetupCompiler {
    $candidates = @(
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'))

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }

    $onPath = Get-Command 'iscc' -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    return $null
}

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
# reflexão para carregar XAML e o recorte removeria o que ele procura em tempo
# de execução.
& dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $runtimeIdentifier `
    --self-contained true `
    --output $stagingDirectory `
    --nologo `
    -p:PublishTrimmed=false | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou com código $LASTEXITCODE."
}

# O aplicativo não funciona sem as ferramentas, e a ausência só apareceria para
# quem recebesse o pacote. Conferir aqui transforma isso em falha de quem publica.
$expectedTools = @('yt-dlp.exe', 'ffmpeg.exe', 'tools.lock.json')
$publishedToolsDirectory = Join-Path $stagingDirectory 'tools'

foreach ($tool in $expectedTools) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishedToolsDirectory $tool))) {
        throw "O pacote saiu sem tools/$tool. O aplicativo não funcionaria na máquina de destino."
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

if (-not $SkipInstaller) {
    $compilerPath = Find-InnoSetupCompiler

    if (-not $compilerPath) {
        Write-Host '[instalador]  Inno Setup não encontrado; apenas a pasta e o zip foram gerados.'
    }
    else {
        Write-Host '[instalador]  compilando (compressão sólida no máximo, costuma demorar)...'

        $scriptPath = Join-Path $repositoryRoot 'installer/YTDown.iss'

        & $compilerPath `
            "/DAppVersion=$version" `
            "/DPublishedDirectory=$stagingDirectory" `
            "/DOutputDirectory=$OutputDirectory" `
            $scriptPath | Out-Host

        if ($LASTEXITCODE -ne 0) {
            throw "A compilação do instalador falhou com código $LASTEXITCODE."
        }

        $installerPath = Join-Path $OutputDirectory "YTDown-$version-setup.exe"
        $installerInMegabytes = [math]::Round(((Get-Item -LiteralPath $installerPath).Length / 1MB), 1)

        Write-Host "[ok]          $installerPath ($installerInMegabytes MB)"
    }
}

Write-Host ''
Write-Host 'O instalador não pede administrador: instala para o usuário atual.'
Write-Host 'O zip serve a quem preferir não instalar nada: extrair e executar YTDown.exe.'
Write-Host 'Nos dois casos o Windows avisa que o programa é de origem desconhecida,'
Write-Host 'porque o executável não é assinado.'
