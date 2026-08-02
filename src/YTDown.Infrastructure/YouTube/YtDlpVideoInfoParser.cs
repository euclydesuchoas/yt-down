using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using YTDown.Application.DTOs;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Le a resposta JSON do yt-dlp.
/// </summary>
/// <remarks>
/// Sem estado e sem dependencias de proposito: e a parte que mais tende a
/// quebrar quando o yt-dlp muda, e assim pode ser testada contra respostas
/// reais gravadas, sem rede e sem processo externo.
/// </remarks>
public static class YtDlpVideoInfoParser
{
    public static bool TryParse(string? json, [NotNullWhen(true)] out VideoInfoDto? videoInfo)
    {
        videoInfo = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var videoId = GetString(root, "id");
            var title = GetString(root, "title");

            if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(title))
            {
                return false;
            }

            videoInfo = new VideoInfoDto(
                videoId,
                title,
                // channel e o nome de exibicao; uploader e o reserva para respostas antigas.
                GetString(root, "channel") ?? GetString(root, "uploader") ?? string.Empty,
                GetDuration(root),
                GetString(root, "thumbnail"),
                GetString(root, "webpage_url") ?? $"https://www.youtube.com/watch?v={videoId}");

            return true;
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    /// <summary>
    /// Transmissoes ao vivo nao tem duracao definida e chegam com <c>duration</c> nulo.
    /// </summary>
    private static TimeSpan GetDuration(JsonElement root) =>
        root.TryGetProperty("duration", out var duration) && duration.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(duration.GetDouble())
            : TimeSpan.Zero;
}
