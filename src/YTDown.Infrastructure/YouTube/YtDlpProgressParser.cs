using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Le as linhas que o yt-dlp emite durante um download.
/// </summary>
/// <remarks>
/// O formato nao e o padrao da ferramenta: e imposto por nos atraves de
/// <see cref="ProgressTemplate"/>, justamente para nao depender do texto que o
/// yt-dlp mostra ao usuario, que muda entre versoes.
/// </remarks>
public static class YtDlpProgressParser
{
    /// <summary>Formato exigido do yt-dlp, campo a campo, separado por barra vertical.</summary>
    public const string ProgressTemplate =
        "PROG|%(info.vcodec)s|%(progress.status)s|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.speed)s|%(progress.eta)s";

    /// <summary>Faz o yt-dlp anunciar o caminho definitivo, ja depois da juncao.</summary>
    public const string FinalFileTemplate = "after_move:FINAL|%(filepath)s";

    public const string FinalFilePrefix = "FINAL|";
    public const string DestinationPrefix = "[download] Destination: ";
    public const string MergingPrefix = "[Merger]";

    private const string ProgressPrefix = "PROG|";
    private const string Unknown = "NA";
    private const int FieldCount = 7;

    public static bool TryParse(string? line, [NotNullWhen(true)] out YtDlpProgressLine? progress)
    {
        progress = null;

        if (line is null || !line.StartsWith(ProgressPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var fields = line.Split('|');

        if (fields.Length != FieldCount)
        {
            return false;
        }

        var totalBytes = ParseLong(fields[4]);

        // Sem o total nao ha percentual possivel, e uma barra inventada e pior
        // do que nenhuma.
        if (totalBytes is null or <= 0)
        {
            return false;
        }

        progress = new YtDlpProgressLine(
            IsVideoStream: !string.Equals(fields[1], "none", StringComparison.OrdinalIgnoreCase),
            IsFinished: string.Equals(fields[2], "finished", StringComparison.OrdinalIgnoreCase),
            DownloadedBytes: ParseLong(fields[3]) ?? 0,
            TotalBytes: totalBytes.Value,
            BytesPerSecond: ParseDouble(fields[5]),
            TimeRemaining: ParseSeconds(fields[6]));

        return true;
    }

    /// <summary>Extrai o caminho anunciado por uma linha com o prefixo informado.</summary>
    public static bool TryParsePath(string? line, string prefix, [NotNullWhen(true)] out string? path)
    {
        path = null;

        if (line is null || !line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = line[prefix.Length..].Trim();

        if (candidate.Length == 0)
        {
            return false;
        }

        path = candidate;
        return true;
    }

    private static long? ParseLong(string field) =>
        IsKnown(field) && long.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static double? ParseDouble(string field) =>
        IsKnown(field) && double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static TimeSpan? ParseSeconds(string field) =>
        ParseDouble(field) is { } seconds and >= 0
            ? TimeSpan.FromSeconds(seconds)
            : null;

    private static bool IsKnown(string field) =>
        field.Length > 0 && !string.Equals(field, Unknown, StringComparison.Ordinal);
}
