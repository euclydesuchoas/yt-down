namespace YTDown.Application.DTOs;

/// <summary>
/// Dados de um video suficientes para o usuario confirmar que escolheu o certo.
/// </summary>
/// <param name="Duration">Zero em transmissoes ao vivo, que nao tem duracao definida.</param>
public sealed record VideoInfoDto(
    string VideoId,
    string Title,
    string ChannelName,
    TimeSpan Duration,
    string? ThumbnailUrl,
    string Url);
