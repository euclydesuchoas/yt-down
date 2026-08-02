namespace YTDown.Application.DTOs;

/// <summary>
/// Dados de um video suficientes para o usuario confirmar que escolheu o certo
/// e decidir em que qualidade quer baixa-lo.
/// </summary>
/// <param name="Duration">Zero em transmissoes ao vivo, que nao tem duracao definida.</param>
/// <param name="AvailableHeights">
/// Alturas que podem ser entregues, da maior para a menor. Contem apenas as que
/// existem em H.264, unicas que podem ser empacotadas em MP4 sem reconverter:
/// oferecer 2160p e entregar outra coisa seria enganar o usuario.
/// </param>
public sealed record VideoInfoDto(
    string VideoId,
    string Title,
    string ChannelName,
    TimeSpan Duration,
    string? ThumbnailUrl,
    string Url,
    IReadOnlyList<int> AvailableHeights);
