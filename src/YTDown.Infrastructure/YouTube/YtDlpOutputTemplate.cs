using YTDown.Application.Common;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Decide com que nome o yt-dlp grava o arquivo.
/// </summary>
public static class YtDlpOutputTemplate
{
    /// <summary>
    /// O título entra no nome, limitado para não estourar o caminho máximo do
    /// Windows.
    /// </summary>
    public const string FromTitle = "%(title).100s.%(ext)s";

    /// <remarks>
    /// Forcadas pelos argumentos do download: MP4 pela junção, MP3 pela extração
    /// de áudio.
    /// </remarks>
    public static string ExtensionFor(MediaKind kind) => kind == MediaKind.AudioOnly ? "mp3" : "mp4";

    /// <summary>
    /// Monta o template de saída para um nome escolhido pelo usuário.
    /// </summary>
    /// <remarks>
    /// O <c>%</c> abre um campo no template do yt-dlp: um nome com "100%" seria
    /// lido como instrução, e "%(title)s" viraria o título do vídeo. Duplicado,
    /// ele volta a ser um por cento literal.
    /// </remarks>
    public static string For(string? chosenName, MediaKind kind, string destinationDirectory)
    {
        if (chosenName is not { Length: > 0 })
        {
            return FromTitle;
        }

        var free = FindFreeName(destinationDirectory, chosenName, ExtensionFor(kind));

        return $"{free.Replace("%", "%%")}.%(ext)s";
    }

    /// <summary>
    /// Encontra um nome que ainda não exista na pasta, ao modo do navegador:
    /// "Música", depois "Música (2)", depois "Música (3)".
    /// </summary>
    /// <remarks>
    /// Existe porque o yt-dlp, ao encontrar o arquivo já gravado, **pula o
    /// download, termina com sucesso e informa o caminho como se tivesse
    /// baixado**. Sem isto, pedir outro vídeo com um nome já usado devolveria o
    /// arquivo antigo e o aplicativo diria que deu tudo certo. Sobrescrever
    /// resolveria a mentira, mas apagaria em silêncio o que o usuário já tinha.
    /// </remarks>
    private static string FindFreeName(string directory, string name, string extension)
    {
        if (!File.Exists(Path.Combine(directory, $"{name}.{extension}")))
        {
            return name;
        }

        for (var attempt = 2; attempt <= 999; attempt++)
        {
            var candidate = $"{name} ({attempt})";

            if (!File.Exists(Path.Combine(directory, $"{candidate}.{extension}")))
            {
                return candidate;
            }
        }

        // Novecentos e noventa e oito arquivos com o mesmo nome não acontece,
        // mas um laco sem saída acontece.
        return $"{name} ({Guid.NewGuid():N})";
    }
}
