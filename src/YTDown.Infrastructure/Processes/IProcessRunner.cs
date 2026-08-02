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
    Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
