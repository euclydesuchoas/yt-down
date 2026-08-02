using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Onde as configuracoes ficam guardadas entre uma execucao e outra.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Le o que esta gravado, ou <c>null</c> quando nao ha nada utilizavel.
    /// </summary>
    Task<SettingsDto?> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(SettingsDto settings, CancellationToken cancellationToken);
}
