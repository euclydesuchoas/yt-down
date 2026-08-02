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

    /// <summary>
    /// As pastas usadas mais recentemente, sem repeticao e da mais recente para
    /// a mais antiga.
    /// </summary>
    /// <remarks>
    /// Sai do proprio historico, que ja guarda o caminho completo de cada
    /// arquivo: as pastas que aparecem la sao exatamente as que o usuario vem
    /// usando. Guardar essa lista em separado seria manter uma segunda verdade
    /// sobre o mesmo fato.
    /// </remarks>
    Task<IReadOnlyList<string>> GetRecentFoldersAsync(int maximum, CancellationToken cancellationToken);

    Task RecordAsync(DownloadHistoryEntryDto entry, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
