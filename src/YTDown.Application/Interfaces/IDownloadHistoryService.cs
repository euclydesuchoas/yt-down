using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// O que o aplicativo ja baixou.
/// </summary>
public interface IDownloadHistoryService
{
    /// <summary>
    /// Os downloads mais recentes, do ultimo para o primeiro.
    /// </summary>
    Task<IReadOnlyList<DownloadHistoryEntryDto>> GetRecentAsync(CancellationToken cancellationToken);

    Task RecordAsync(DownloadHistoryEntryDto entry, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
