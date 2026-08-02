namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Ambiente exigido pelo yt-dlp.
/// </summary>
internal static class YtDlpEnvironment
{
    /// <summary>
    /// Forca o yt-dlp a escrever em UTF-8.
    /// </summary>
    /// <remarks>
    /// O yt-dlp e escrito em Python, e no Windows o Python usa a code page ANSI
    /// quando a saida esta redirecionada para um pipe em vez de um console.
    /// Sem isto, titulos e caminhos com ideogramas, acentos ou emoji chegam
    /// mutilados, e o caminho do arquivo final deixa de existir em disco.
    /// Nao ha opcao de linha de comando equivalente.
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> Variables =
        new Dictionary<string, string> { ["PYTHONIOENCODING"] = "utf-8" };
}
