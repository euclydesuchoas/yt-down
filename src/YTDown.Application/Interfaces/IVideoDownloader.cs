using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Domain.ValueObjects;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Baixa um video para o disco.
/// </summary>
/// <remarks>
/// Implementado pela Infrastructure. A Application nao sabe que existe yt-dlp
/// nem FFmpeg.
/// </remarks>
public interface IVideoDownloader
{
    Task<Result<DownloadedFileDto>> DownloadAsync(
        VideoUrl videoUrl,
        DownloadOptionsDto options,
        string destinationDirectory,
        IProgress<DownloadProgressDto> progress,
        CancellationToken cancellationToken);
}
