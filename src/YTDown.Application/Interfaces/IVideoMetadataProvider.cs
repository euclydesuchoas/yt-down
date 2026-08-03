using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Domain.ValueObjects;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Obtém os dados de um vídeo junto ao YouTube.
/// </summary>
/// <remarks>
/// Implementado pela Infrastructure. A Application não sabe que existe yt-dlp.
/// </remarks>
public interface IVideoMetadataProvider
{
    Task<Result<VideoInfoDto>> GetMetadataAsync(VideoUrl videoUrl, CancellationToken cancellationToken);
}
