namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Ambiente exigido pelo yt-dlp.
/// </summary>
internal static class YtDlpEnvironment
{
    /// <summary>
    /// Força o yt-dlp a escrever em UTF-8.
    /// </summary>
    /// <remarks>
    /// O yt-dlp é escrito em Python, e no Windows o Python usa a code page ANSI
    /// quando a saída está redirecionada para um pipe em vez de um console.
    /// Sem isto, títulos e caminhos com ideogramas, acentos ou emoji chegam
    /// mutilados, e o caminho do arquivo final deixa de existir em disco.
    /// Não há opção de linha de comando equivalente.
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> Variables =
        new Dictionary<string, string> { ["PYTHONIOENCODING"] = "utf-8" };
}
