using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// O que o usuário escolheu antes de baixar.
/// </summary>
/// <param name="MaximumHeight">
/// Altura máxima em pixels, como 1080 ou 720. Ausente significa a melhor
/// qualidade disponível. Ignorado quando apenas o áudio é pedido.
/// </param>
/// <param name="DestinationDirectory">
/// Pasta escolhida para este download. Ausente significa a pasta padrão das
/// configurações: quem organiza os arquivos por assunto troca de pasta a cada
/// download, e mandar essa pessoa às configurações toda vez seria atrito.
/// </param>
/// <param name="FileName">
/// Nome escolhido para o arquivo, sem extensão. Ausente significa o título do
/// vídeo, que é o que o yt-dlp usaria sozinho.
/// </param>
public sealed record DownloadOptionsDto(
    MediaKind Kind = MediaKind.Video,
    int? MaximumHeight = null,
    string? DestinationDirectory = null,
    string? FileName = null)
{
    public static DownloadOptionsDto BestVideo { get; } = new();

    public static DownloadOptionsDto AudioOnly { get; } = new(MediaKind.AudioOnly);
}
