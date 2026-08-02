<#
.SYNOPSIS
    Gera assets/ytdown.ico, o icone do aplicativo.

.DESCRIPTION
    O icone e desenhado por codigo, e nao editado em uma ferramenta grafica, para
    que possa ser refeito e ajustado sem depender de nada instalado — do mesmo
    modo que as ferramentas externas sao baixadas por script em vez de
    versionadas prontas.

    O desenho e um triangulo apontando para baixo sobre uma barra: a mesma forma
    le como botao de play e como seta de download.

    Cada tamanho e desenhado no seu proprio tamanho, e nao reduzido do maior, que
    e o que deixa icone borrado nas dimensoes pequenas.

.PARAMETER OutputPath
    Onde gravar. Por padrao assets/ytdown.ico, na raiz do repositorio.

.EXAMPLE
    ./scripts/build-icon.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $OutputPath) {
    $OutputPath = Join-Path $repositoryRoot 'assets/ytdown.ico'
}

# O mesmo azul dos botoes da tela. Vermelho do YouTube ficaria de fora pela mesma
# razao que o nome nao virou "YouTube Downloader": descrever a funcao e diferente
# de vestir a marca de outro.
$brandColor = [System.Drawing.Color]::FromArgb(255, 44, 107, 237)
$glyphColor = [System.Drawing.Color]::White

# Ate 48 o icone vai como DIB, que qualquer contexto do shell le; acima disso
# como PNG, para o arquivo nao inchar.
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$largestDibSize = 48

function New-RoundedRectangle {
    param(
        [Parameter(Mandatory)] [single] $X,
        [Parameter(Mandatory)] [single] $Y,
        [Parameter(Mandatory)] [single] $Width,
        [Parameter(Mandatory)] [single] $Height,
        [Parameter(Mandatory)] [single] $Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2

    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    return $path
}

function New-IconBitmap {
    param([Parameter(Mandatory)] [int] $Size)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $tile = New-RoundedRectangle -X 0 -Y 0 -Width $Size -Height $Size -Radius ($Size * 0.22)
    $graphics.FillPath((New-Object System.Drawing.SolidBrush $brandColor), $tile)

    $triangle = New-Object System.Drawing.Drawing2D.GraphicsPath
    $triangle.AddPolygon(@(
        (New-Object System.Drawing.PointF(($Size * 0.26), ($Size * 0.28))),
        (New-Object System.Drawing.PointF(($Size * 0.74), ($Size * 0.28))),
        (New-Object System.Drawing.PointF(($Size * 0.50), ($Size * 0.66)))))
    $graphics.FillPath((New-Object System.Drawing.SolidBrush $glyphColor), $triangle)

    $bar = New-RoundedRectangle -X ($Size * 0.26) -Y ($Size * 0.74) `
        -Width ($Size * 0.48) -Height ($Size * 0.09) -Radius ($Size * 0.045)
    $graphics.FillPath((New-Object System.Drawing.SolidBrush $glyphColor), $bar)

    $graphics.Dispose()

    return $bitmap
}

<#
.SYNOPSIS
    Converte a imagem no bitmap de 32 bits que o formato ICO espera.
.DESCRIPTION
    As linhas vao de baixo para cima, a altura declarada e o dobro da real e uma
    mascara AND vazia fecha o bloco. Nada disso e opcional: sem a mascara, o
    Windows recusa a entrada.
#>
function ConvertTo-Dib {
    param([Parameter(Mandatory)] [System.Drawing.Bitmap] $Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter $stream

    $writer.Write([int] 40)
    $writer.Write([int] $width)
    $writer.Write([int] ($height * 2))
    $writer.Write([int16] 1)
    $writer.Write([int16] 32)
    $writer.Write([int] 0)
    $writer.Write([int] ($width * $height * 4))
    $writer.Write([int] 0); $writer.Write([int] 0); $writer.Write([int] 0); $writer.Write([int] 0)

    for ($y = $height - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $width; $x++) {
            $pixel = $Bitmap.GetPixel($x, $y)
            $writer.Write([byte] $pixel.B)
            $writer.Write([byte] $pixel.G)
            $writer.Write([byte] $pixel.R)
            $writer.Write([byte] $pixel.A)
        }
    }

    $maskBytesPerRow = [math]::Floor(($width + 31) / 32) * 4
    for ($y = 0; $y -lt $height; $y++) {
        $writer.Write((New-Object byte[] $maskBytesPerRow))
    }

    $writer.Flush()

    # A virgula impede o PowerShell de desenrolar o array em elementos soltos, o
    # que faria o chamador receber uma colecao de objetos em vez de bytes.
    return , $stream.ToArray()
}

function ConvertTo-PngBytes {
    param([Parameter(Mandatory)] [System.Drawing.Bitmap] $Bitmap)

    $stream = New-Object System.IO.MemoryStream
    $Bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)

    return , $stream.ToArray()
}

$images = @()

foreach ($size in $sizes) {
    $bitmap = New-IconBitmap -Size $size

    $bytes = if ($size -le $largestDibSize) {
        ConvertTo-Dib -Bitmap $bitmap
    }
    else {
        ConvertTo-PngBytes -Bitmap $bitmap
    }

    $images += [pscustomobject]@{ Size = $size; Bytes = [byte[]] $bytes }
    $bitmap.Dispose()
}

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

$file = New-Object System.IO.FileStream $OutputPath, ([System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter $file

try {
    $writer.Write([int16] 0)
    $writer.Write([int16] 1)
    $writer.Write([int16] $images.Count)

    $offset = 6 + (16 * $images.Count)

    foreach ($image in $images) {
        # 256 e gravado como zero: o campo tem um byte so.
        $declaredSize = if ($image.Size -ge 256) { 0 } else { $image.Size }

        $writer.Write([byte] $declaredSize)
        $writer.Write([byte] $declaredSize)
        $writer.Write([byte] 0)
        $writer.Write([byte] 0)
        $writer.Write([int16] 1)
        $writer.Write([int16] 32)
        $writer.Write([int] $image.Bytes.Length)
        $writer.Write([int] $offset)

        $offset += $image.Bytes.Length
    }

    foreach ($image in $images) {
        $writer.Write([byte[]] $image.Bytes)
    }

    $writer.Flush()
}
finally {
    $file.Close()
}

$sizeInKilobytes = [math]::Round(((Get-Item -LiteralPath $OutputPath).Length / 1KB), 1)

Write-Host "[ok] $OutputPath ($sizeInKilobytes KB, $($images.Count) tamanhos: $($sizes -join ', '))"
