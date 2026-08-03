using System.Diagnostics.CodeAnalysis;
using YTDown.Domain.Exceptions;

namespace YTDown.Domain.ValueObjects;

/// <summary>
/// Referência a um único vídeo do YouTube, em forma canônica.
/// </summary>
/// <remarks>
/// O usuário cola a URL direto da barra de endereços, do botão de compartilhar
/// ou do aplicativo móvel, então a mesma referência chega em muitos formatos.
/// Este tipo reduz todos eles ao identificador do vídeo e descarta tudo o que
/// não identifica o vídeo: parâmetros de playlist, de tempo, de rastreamento e
/// de posição.
///
/// Duas instâncias criadas a partir de formatos diferentes do mesmo vídeo são
/// iguais.
/// </remarks>
public sealed record VideoUrl
{
    private const int VideoIdLength = 11;

    private static readonly string[] SupportedHosts =
    [
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
        "youtu.be",
        "www.youtu.be"
    ];

    private static readonly string[] ShortLinkHosts =
    [
        "youtu.be",
        "www.youtu.be"
    ];

    /// <summary>Segmentos que antecedem o identificador do vídeo no caminho.</summary>
    private static readonly string[] VideoIdPathPrefixes =
    [
        "shorts",
        "live",
        "embed",
        "v"
    ];

    private VideoUrl(string videoId) => VideoId = videoId;

    /// <summary>Identificador de 11 caracteres atribuído pelo YouTube.</summary>
    public string VideoId { get; }

    /// <summary>Forma canônica da URL, usada para exibir e para chamar o yt-dlp.</summary>
    public string Value => $"https://www.youtube.com/watch?v={VideoId}";

    /// <summary>
    /// Cria a partir de uma entrada já considerada válida.
    /// </summary>
    /// <exception cref="InvalidVideoUrlException">A entrada não identifica um vídeo.</exception>
    public static VideoUrl Create(string? candidate) =>
        TryCreate(candidate, out var videoUrl)
            ? videoUrl
            : throw new InvalidVideoUrlException(candidate);

    /// <summary>
    /// Tenta interpretar uma entrada digitada ou colada pelo usuário.
    /// </summary>
    public static bool TryCreate(string? candidate, [NotNullWhen(true)] out VideoUrl? videoUrl)
    {
        videoUrl = null;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (!TryExtractVideoId(candidate.Trim(), out var videoId))
        {
            return false;
        }

        videoUrl = new VideoUrl(videoId);
        return true;
    }

    public override string ToString() => Value;

    private static bool TryExtractVideoId(string candidate, [NotNullWhen(true)] out string? videoId)
    {
        videoId = null;

        if (!TryParseHttpUri(candidate, out var uri) || !IsSupportedHost(uri))
        {
            return false;
        }

        var extracted = ExtractFromPath(uri) ?? GetQueryValue(uri, "v");

        if (!IsVideoId(extracted))
        {
            return false;
        }

        videoId = extracted;
        return true;
    }

    /// <summary>
    /// Aceita URLs sem esquema, porque colar "youtu.be/..." é comum.
    /// </summary>
    private static bool TryParseHttpUri(string candidate, [NotNullWhen(true)] out Uri? uri)
    {
        var absolute = candidate.Contains("://", StringComparison.Ordinal)
            ? candidate
            : $"https://{candidate}";

        return Uri.TryCreate(absolute, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static bool IsSupportedHost(Uri uri) =>
        SupportedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);

    private static string? ExtractFromPath(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        // Em youtu.be o caminho inteiro é o identificador: youtu.be/UKcJqQqiXq0
        if (ShortLinkHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return segments[0];
        }

        // Nas demais formas o identificador vem após um segmento conhecido: /shorts/UKcJqQqiXq0
        return segments.Length >= 2 && VideoIdPathPrefixes.Contains(segments[0], StringComparer.OrdinalIgnoreCase)
            ? segments[1]
            : null;
    }

    private static string? GetQueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);

            if (separatorIndex > 0 && pair.AsSpan(0, separatorIndex).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            }
        }

        return null;
    }

    private static bool IsVideoId([NotNullWhen(true)] string? candidate) =>
        candidate is { Length: VideoIdLength }
        && candidate.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
