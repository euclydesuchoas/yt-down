using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using YTDown.Application.DTOs;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Lê a resposta JSON do yt-dlp.
/// </summary>
/// <remarks>
/// Sem estado e sem dependências de propósito: é a parte que mais tende a
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
                // channel é o nome de exibição; uploader é o reserva para respostas antigas.
                GetString(root, "channel") ?? GetString(root, "uploader") ?? string.Empty,
                GetDuration(root),
                GetString(root, "thumbnail"),
                GetString(root, "webpage_url") ?? $"https://www.youtube.com/watch?v={videoId}",
                GetDeliverableHeights(root));

            return true;
        }
    }

    /// <summary>
    /// Alturas que podem ser entregues, da maior para a menor.
    /// </summary>
    /// <remarks>
    /// Somente as que existem em H.264, único codec que o FFmpeg empacota em MP4
    /// sem reconverter. Um vídeo em 4K aparece com 1080p no máximo, porque acima
    /// disso o YouTube só oferece VP9 e AV1: listar 2160p e entregar outra coisa
    /// seria enganar o usuário.
    /// </remarks>
    private static IReadOnlyList<int> GetDeliverableHeights(JsonElement root)
    {
        if (!root.TryGetProperty("formats", out var formats) || formats.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var heights = new SortedSet<int>();

        foreach (var format in formats.EnumerateArray())
        {
            if (GetString(format, "vcodec") is not { } videoCodec ||
                !videoCodec.StartsWith("avc1", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (format.TryGetProperty("height", out var height) &&
                height.ValueKind == JsonValueKind.Number &&
                height.TryGetInt32(out var value) &&
                value > 0)
            {
                heights.Add(value);
            }
        }

        return [.. heights.Reverse()];
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    /// <summary>
    /// Transmissões ao vivo não têm duração definida e chegam com <c>duration</c> nulo.
    /// </summary>
    private static TimeSpan GetDuration(JsonElement root) =>
        root.TryGetProperty("duration", out var duration) && duration.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(duration.GetDouble())
            : TimeSpan.Zero;
}
