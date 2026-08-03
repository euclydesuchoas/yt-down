using YTDown.Application.DTOs;
using YTDown.UI.Resources;

namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma linha do histórico, pronta para leitura.
/// </summary>
/// <remarks>
/// O registro guardado tem bytes e um instante; a tela precisa de "18,6 MB" e
/// "hoje às 14:32". A conversão acontece aqui, uma vez, e não a cada exibição.
/// </remarks>
public sealed class DownloadHistoryItem
{
    public DownloadHistoryItem(DownloadHistoryEntryDto entry, DateTimeOffset now)
    {
        Entry = entry;
        Description = string.Join(
            "   ·   ",
            DownloadHistoryText.WhenOf(entry.CompletedAt, now),
            DownloadHistoryText.KindOf(entry.Kind),
            DownloadHistoryText.SizeOf(entry.SizeInBytes));
    }

    public DownloadHistoryEntryDto Entry { get; }

    public string FileName => Entry.FileName;

    /// <summary>Quando, o que e quanto, em uma linha só.</summary>
    public string Description { get; }
}
