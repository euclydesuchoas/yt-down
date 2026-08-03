using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Ponto de entrada da apresentação para consultar um vídeo.
/// </summary>
public interface IVideoInfoService
{
    /// <param name="rawUrl">Texto exatamente como digitado ou colado pelo usuário.</param>
    Task<Result<VideoInfoDto>> GetVideoInfoAsync(string? rawUrl, CancellationToken cancellationToken);
}
