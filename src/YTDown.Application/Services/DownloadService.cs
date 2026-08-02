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
    private readonly IDownloadHistoryService _downloadHistory;
    private readonly TimeProvider _timeProvider;

    public DownloadService(
        IVideoDownloader videoDownloader,
        IDownloadLocationProvider downloadLocationProvider,
        IDownloadHistoryService downloadHistory,
        TimeProvider timeProvider)
    {
        _videoDownloader = videoDownloader;
        _downloadLocationProvider = downloadLocationProvider;
        _downloadHistory = downloadHistory;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DownloadedFileDto>> DownloadAsync(
        string? rawUrl,
        DownloadOptionsDto options,
        IProgress<DownloadProgressDto> progress,
        CancellationToken cancellationToken)
    {
        if (!VideoUrl.TryCreate(rawUrl, out var videoUrl))
        {
            return Result<DownloadedFileDto>.Failure(ErrorCode.InvalidUrl);
        }

        string destinationDirectory;

        if (options.DestinationDirectory is { Length: > 0 } chosen)
        {
            // Escolha explicita nao vira outra coisa em silencio. Cair para a
            // pasta Downloads aqui entregaria o arquivo longe de onde o usuario
            // acabou de apontar, e ele so descobriria ao procurar.
            if (!_downloadLocationProvider.Exists(chosen))
            {
                return Result<DownloadedFileDto>.Failure(ErrorCode.DestinationUnavailable, chosen);
            }

            destinationDirectory = chosen;
        }
        else
        {
            destinationDirectory = await _downloadLocationProvider.GetDestinationDirectoryAsync(cancellationToken);
        }

        var result = await _videoDownloader.DownloadAsync(
            videoUrl,
            options,
            destinationDirectory,
            progress,
            cancellationToken);

        if (result.IsSuccess)
        {
            await RecordAsync(videoUrl, options, result.Value);
        }

        return result;
    }

    /// <summary>
    /// Registra o download que acabou de terminar.
    /// </summary>
    /// <remarks>
    /// Sem o token de cancelamento de proposito: o arquivo ja esta no disco, e
    /// um cancelamento que chegue exatamente aqui deixaria o usuario com um
    /// arquivo que o historico nao conhece.
    /// </remarks>
    private Task RecordAsync(VideoUrl videoUrl, DownloadOptionsDto options, DownloadedFileDto file) =>
        _downloadHistory.RecordAsync(
            new DownloadHistoryEntryDto(
                videoUrl.Value,
                file.FileName,
                file.FilePath,
                file.SizeInBytes,
                options.Kind,
                _timeProvider.GetLocalNow()),
            CancellationToken.None);
}
