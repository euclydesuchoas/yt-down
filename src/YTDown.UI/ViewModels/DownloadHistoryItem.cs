using YTDown.Application.DTOs;
using YTDown.UI.Resources;

namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma linha do historico, pronta para leitura.
/// </summary>
/// <remarks>
/// O registro guardado tem bytes e um instante; a tela precisa de "18,6 MB" e
/// "hoje as 14:32". A conversao acontece aqui, uma vez, e nao a cada exibicao.
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

    /// <summary>Quando, o que e quanto, em uma linha so.</summary>
    public string Description { get; }
}
