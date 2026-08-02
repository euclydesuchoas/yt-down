using YTDown.Application.Common;

namespace YTDown.UI.Resources;

/// <summary>
/// Descreve um registro do historico em linguagem comum.
/// </summary>
internal static class DownloadHistoryText
{
    private const double BytesPerKilobyte = 1024d;
    private const double BytesPerMegabyte = BytesPerKilobyte * 1024d;
    private const double BytesPerGigabyte = BytesPerMegabyte * 1024d;

    public static string SizeOf(long sizeInBytes) => sizeInBytes switch
    {
        >= (long)BytesPerGigabyte => $"{sizeInBytes / BytesPerGigabyte:0.0} GB",
        >= (long)BytesPerMegabyte => $"{sizeInBytes / BytesPerMegabyte:0.0} MB",
        _ => $"{sizeInBytes / BytesPerKilobyte:0} KB"
    };

    public static string KindOf(MediaKind kind) => kind switch
    {
        MediaKind.AudioOnly => "Áudio",
        _ => "Vídeo"
    };

    /// <summary>
    /// Quando o download terminou.
    /// </summary>
    /// <remarks>
    /// Quem abre o historico procura o que baixou ha pouco, entao os dias
    /// recentes aparecem pelo nome: uma data por extenso obrigaria o usuario a
    /// conferir o calendario para saber se foi hoje.
    /// </remarks>
    public static string WhenOf(DateTimeOffset completedAt, DateTimeOffset now)
    {
        var days = (now.Date - completedAt.Date).Days;

        return days switch
        {
            0 => $"hoje às {completedAt:HH:mm}",
            1 => $"ontem às {completedAt:HH:mm}",
            _ => $"{completedAt:dd/MM/yyyy} às {completedAt:HH:mm}"
        };
    }
}
