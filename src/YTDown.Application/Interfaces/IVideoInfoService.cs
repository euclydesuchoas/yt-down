using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Ponto de entrada da apresentacao para consultar um video.
/// </summary>
public interface IVideoInfoService
{
    /// <param name="rawUrl">Texto exatamente como digitado ou colado pelo usuario.</param>
    Task<Result<VideoInfoDto>> GetVideoInfoAsync(string? rawUrl, CancellationToken cancellationToken);
}
