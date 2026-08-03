using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// As preferências do usuário.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// As configurações em vigor. Nunca <c>null</c>: sem nada gravado, valem os
    /// padrões.
    /// </summary>
    Task<SettingsDto> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken);
}
