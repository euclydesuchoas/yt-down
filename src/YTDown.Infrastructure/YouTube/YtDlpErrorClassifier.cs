using YTDown.Application.Common;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Traduz a saida de erro do yt-dlp em um motivo que a aplicacao entende.
/// </summary>
/// <remarks>
/// O yt-dlp so distingue seus erros pelo texto da mensagem. A ordem das regras
/// importa: uma restricao de idade ou de regiao tambem menciona
/// "Video unavailable", entao precisa ser reconhecida antes.
/// </remarks>
public static class YtDlpErrorClassifier
{
    private static readonly (ErrorCode Code, string[] Markers)[] Rules =
    [
        (ErrorCode.AgeRestricted,
        [
            "confirm your age",
            "age-restricted",
            "inappropriate for some users"
        ]),

        // Marcador curto de proposito: evita o apostrofo, que o yt-dlp emite
        // como aspa tipografica, e nao colide com "sign in to confirm your age".
        (ErrorCode.BotCheckRequired, ["not a bot"]),

        (ErrorCode.RegionBlocked,
        [
            "in your country",
            "available from your location",
            "not available in your location",
            "geo restriction"
        ]),

        (ErrorCode.VideoUnavailable,
        [
            "video unavailable",
            "private video",
            "this video is private",
            "has been removed",
            "has been terminated",
            "is no longer available",
            "does not exist",
            "incomplete or invalid"
        ]),

        (ErrorCode.NetworkError,
        [
            "getaddrinfo",
            "temporary failure in name resolution",
            "failed to resolve",
            "urlopen error",
            "network is unreachable",
            "connection refused",
            "connection reset",
            "timed out",
            "unable to download webpage"
        ])
    ];

    public static ErrorCode Classify(string? standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return ErrorCode.ToolFailure;
        }

        var normalized = standardError.ToLowerInvariant();

        foreach (var (code, markers) in Rules)
        {
            if (markers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal)))
            {
                return code;
            }
        }

        return ErrorCode.ToolFailure;
    }
}
