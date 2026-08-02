using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// As preferencias do usuario.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// As configuracoes em vigor. Nunca <c>null</c>: sem nada gravado, valem os
    /// padroes.
    /// </summary>
    Task<SettingsDto> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken);
}
