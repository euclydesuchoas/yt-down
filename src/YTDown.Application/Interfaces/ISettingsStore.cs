using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Onde as configurações ficam guardadas entre uma execução e outra.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Lê o que está gravado, ou <c>null</c> quando não há nada utilizável.
    /// </summary>
    Task<SettingsDto?> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(SettingsDto settings, CancellationToken cancellationToken);
}
