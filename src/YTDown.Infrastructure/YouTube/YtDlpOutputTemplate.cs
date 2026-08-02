using YTDown.Application.Common;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Decide com que nome o yt-dlp grava o arquivo.
/// </summary>
public static class YtDlpOutputTemplate
{
    /// <summary>
    /// O titulo entra no nome, limitado para nao estourar o caminho maximo do
    /// Windows.
    /// </summary>
    public const string FromTitle = "%(title).100s.%(ext)s";

    /// <remarks>
    /// Forcadas pelos argumentos do download: MP4 pela juncao, MP3 pela extracao
    /// de audio.
    /// </remarks>
    public static string ExtensionFor(MediaKind kind) => kind == MediaKind.AudioOnly ? "mp3" : "mp4";

    /// <summary>
    /// Monta o template de saida para um nome escolhido pelo usuario.
    /// </summary>
    /// <remarks>
    /// O <c>%</c> abre um campo no template do yt-dlp: um nome com "100%" seria
    /// lido como instrucao, e "%(title)s" viraria o titulo do video. Duplicado,
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
    /// Encontra um nome que ainda nao exista na pasta, ao modo do navegador:
    /// "Musica", depois "Musica (2)", depois "Musica (3)".
    /// </summary>
    /// <remarks>
    /// Existe porque o yt-dlp, ao encontrar o arquivo ja gravado, **pula o
    /// download, termina com sucesso e informa o caminho como se tivesse
    /// baixado**. Sem isto, pedir outro video com um nome ja usado devolveria o
    /// arquivo antigo e o aplicativo diria que deu tudo certo. Sobrescrever
    /// resolveria a mentira, mas apagaria em silencio o que o usuario ja tinha.
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

        // Novecentos e noventa e oito arquivos com o mesmo nome nao acontece,
        // mas um laco sem saida acontece.
        return $"{name} ({Guid.NewGuid():N})";
    }
}
