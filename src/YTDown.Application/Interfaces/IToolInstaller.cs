using YTDown.Application.Common;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Garante que as ferramentas externas estejam em um local utilizável.
/// </summary>
public interface IToolInstaller
{
    Task<Result<bool>> EnsureInstalledAsync(CancellationToken cancellationToken);
}
