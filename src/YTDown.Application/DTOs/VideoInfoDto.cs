namespace YTDown.Application.DTOs;

/// <summary>
/// Dados de um vídeo suficientes para o usuário confirmar que escolheu o certo
/// e decidir em que qualidade quer baixá-lo.
/// </summary>
/// <param name="Duration">Zero em transmissões ao vivo, que não têm duração definida.</param>
/// <param name="AvailableHeights">
/// Alturas que podem ser entregues, da maior para a menor. Contém apenas as que
/// existem em H.264, únicas que podem ser empacotadas em MP4 sem reconverter:
/// oferecer 2160p e entregar outra coisa seria enganar o usuário.
/// </param>
public sealed record VideoInfoDto(
    string VideoId,
    string Title,
    string ChannelName,
    TimeSpan Duration,
    string? ThumbnailUrl,
    string Url,
    IReadOnlyList<int> AvailableHeights);
