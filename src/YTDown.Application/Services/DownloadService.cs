using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;

namespace YTDown.Application.Services;

/// <inheritdoc cref="IDownloadService" />
public sealed class DownloadService : IDownloadService
{
    private readonly IVideoDownloader _videoDownloader;
    private readonly IDownloadLocationProvider _downloadLocationProvider;

    public DownloadService(IVideoDownloader videoDownloader, IDownloadLocationProvider downloadLocationProvider)
    {
        _videoDownloader = videoDownloader;
        _downloadLocationProvider = downloadLocationProvider;
    }

    public Task<Result<DownloadedFileDto>> DownloadAsync(
        string? rawUrl,
        IProgress<DownloadProgressDto> progress,
        CancellationToken cancellationToken)
    {
        if (!VideoUrl.TryCreate(rawUrl, out var videoUrl))
        {
            return Task.FromResult(Result<DownloadedFileDto>.Failure(ErrorCode.InvalidUrl));
        }

        return _videoDownloader.DownloadAsync(
            videoUrl,
            _downloadLocationProvider.GetDestinationDirectory(),
            progress,
            cancellationToken);
    }
}
