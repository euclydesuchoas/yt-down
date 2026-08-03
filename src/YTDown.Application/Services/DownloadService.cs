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
            // Escolha explícita não vira outra coisa em silêncio. Cair para a
            // pasta Downloads aqui entregaria o arquivo longe de onde o usuário
            // acabou de apontar, e ele só descobriria ao procurar.
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

        // O nome é limpo aqui, e não na tela: a apresentação pode ajudar o
        // usuário enquanto ele digita, mas quem garante que o nome serve ao
        // sistema e esta camada, por onde todo download passa.
        var sanitized = OutputFileName.TryCreate(options.FileName, out var fileName)
            ? options with { FileName = fileName.Value }
            : options with { FileName = null };

        var result = await _videoDownloader.DownloadAsync(
            videoUrl,
            sanitized,
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
    /// Sem o token de cancelamento de propósito: o arquivo já está no disco, e
    /// um cancelamento que chegue exatamente aqui deixaria o usuário com um
    /// arquivo que o histórico não conhece.
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
