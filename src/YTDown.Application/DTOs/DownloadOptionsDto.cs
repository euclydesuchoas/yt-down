using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// O que o usuario escolheu antes de baixar.
/// </summary>
/// <param name="MaximumHeight">
/// Altura maxima em pixels, como 1080 ou 720. Ausente significa a melhor
/// qualidade disponivel. Ignorado quando apenas o audio e pedido.
/// </param>
/// <param name="DestinationDirectory">
/// Pasta escolhida para este download. Ausente significa a pasta padrao das
/// configuracoes: quem organiza os arquivos por assunto troca de pasta a cada
/// download, e mandar essa pessoa as configuracoes toda vez seria atrito.
/// </param>
public sealed record DownloadOptionsDto(
    MediaKind Kind = MediaKind.Video,
    int? MaximumHeight = null,
    string? DestinationDirectory = null)
{
    public static DownloadOptionsDto BestVideo { get; } = new();

    public static DownloadOptionsDto AudioOnly { get; } = new(MediaKind.AudioOnly);
}
