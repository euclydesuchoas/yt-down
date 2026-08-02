namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Executa um programa externo e devolve o que ele escreveu.
/// </summary>
/// <remarks>
/// Mantido sem qualquer conhecimento de yt-dlp ou FFmpeg: quem sabe montar os
/// argumentos e interpretar a saida sao as classes especificas de cada
/// ferramenta, que assim podem ser testadas sem iniciar processo nenhum.
/// </remarks>
public interface IProcessRunner
{
    /// <param name="onStandardOutputLine">
    /// Recebe cada linha assim que ela e emitida, em ordem. Necessario para
    /// acompanhar progresso, que perde o sentido se so chegar no fim.
    /// E um delegate, e nao IProgress, justamente para preservar a ordem: as
    /// implementacoes de IProgress podem entregar fora de ordem.
    /// </param>
    Task<ProcessResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine,
        CancellationToken cancellationToken);
}
