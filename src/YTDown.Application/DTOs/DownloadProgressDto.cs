using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// Andamento de um download, ja agregado em um unico valor.
/// </summary>
/// <param name="Percentage">
/// Percentual do trabalho inteiro, de 0 a 100. Nunca retrocede, mesmo que o
/// download seja composto de varias etapas.
/// </param>
/// <param name="BytesPerSecond">Velocidade instantanea, ausente quando desconhecida.</param>
/// <param name="TimeRemaining">Estimativa da ferramenta, ausente quando desconhecida.</param>
public sealed record DownloadProgressDto(
    int Percentage,
    DownloadStage Stage,
    double? BytesPerSecond,
    TimeSpan? TimeRemaining);
