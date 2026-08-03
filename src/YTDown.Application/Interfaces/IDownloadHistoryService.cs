using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// O que o aplicativo já baixou.
/// </summary>
public interface IDownloadHistoryService
{
    /// <summary>
    /// Os downloads mais recentes, do último para o primeiro.
    /// </summary>
    Task<IReadOnlyList<DownloadHistoryEntryDto>> GetRecentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// As pastas usadas mais recentemente, sem repetição e da mais recente para
    /// a mais antiga.
    /// </summary>
    /// <remarks>
    /// Sai do próprio histórico, que já guarda o caminho completo de cada
    /// arquivo: as pastas que aparecem lá são exatamente as que o usuário vem
    /// usando. Guardar essa lista em separado seria manter uma segunda verdade
    /// sobre o mesmo fato.
    /// </remarks>
    Task<IReadOnlyList<string>> GetRecentFoldersAsync(int maximum, CancellationToken cancellationToken);

    Task RecordAsync(DownloadHistoryEntryDto entry, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
