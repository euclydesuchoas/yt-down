using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// O que o usuario escolheu antes de baixar.
/// </summary>
/// <param name="MaximumHeight">
/// Altura maxima em pixels, como 1080 ou 720. Ausente significa a melhor
/// qualidade disponivel. Ignorado quando apenas o audio e pedido.
/// </param>
public sealed record DownloadOptionsDto(MediaKind Kind = MediaKind.Video, int? MaximumHeight = null)
{
    public static DownloadOptionsDto BestVideo { get; } = new();

    public static DownloadOptionsDto AudioOnly { get; } = new(MediaKind.AudioOnly);
}
