namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Executa um programa externo e devolve o que ele escreveu.
/// </summary>
/// <remarks>
/// Mantido sem qualquer conhecimento de yt-dlp ou FFmpeg: quem sabe montar os
/// argumentos e interpretar a saída são as classes específicas de cada
/// ferramenta, que assim podem ser testadas sem iniciar processo nenhum.
/// </remarks>
public interface IProcessRunner
{
    /// <param name="onStandardOutputLine">
    /// Recebe cada linha assim que ela é emitida, em ordem. Necessário para
    /// acompanhar progresso, que perde o sentido se só chegar no fim.
    /// É um delegate, e não IProgress, justamente para preservar a ordem: as
    /// implementações de IProgress podem entregar fora de ordem.
    /// </param>
    Task<ProcessResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine,
        CancellationToken cancellationToken);
}
