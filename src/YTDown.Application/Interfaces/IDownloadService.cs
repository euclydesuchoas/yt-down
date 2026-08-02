using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Ponto de entrada da apresentacao para baixar um video.
/// </summary>
public interface IDownloadService
{
    /// <param name="rawUrl">Texto exatamente como digitado ou colado pelo usuario.</param>
    Task<Result<DownloadedFileDto>> DownloadAsync(
        string? rawUrl,
        DownloadOptionsDto options,
        IProgress<DownloadProgressDto> progress,
        CancellationToken cancellationToken);
}
