using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;

namespace YTDown.Application.Services;

/// <inheritdoc cref="IVideoInfoService" />
public sealed class VideoInfoService : IVideoInfoService
{
    private readonly IVideoMetadataProvider _metadataProvider;

    public VideoInfoService(IVideoMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public Task<Result<VideoInfoDto>> GetVideoInfoAsync(string? rawUrl, CancellationToken cancellationToken)
    {
        // Recusar aqui evita iniciar um processo externo para uma entrada que já
        // sabemos inválida, e devolve ao usuário um erro imediato e específico.
        if (!VideoUrl.TryCreate(rawUrl, out var videoUrl))
        {
            return Task.FromResult(Result<VideoInfoDto>.Failure(ErrorCode.InvalidUrl));
        }

        return _metadataProvider.GetMetadataAsync(videoUrl, cancellationToken);
    }
}
