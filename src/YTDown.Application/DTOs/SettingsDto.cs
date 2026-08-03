namespace YTDown.Application.DTOs;

/// <summary>
/// O que o usuário pode decidir uma vez e não precisar repetir.
/// </summary>
/// <param name="DestinationDirectory">
/// Onde salvar. <c>null</c> significa a pasta Downloads do usuário, que é o
/// destino de quem nunca abriu as configurações.
/// </param>
/// <param name="MaximumHeight">
/// Teto de qualidade, e não a qualidade exigida: um vídeo que só exista em 480p
/// continua sendo baixado em 480p. <c>null</c> significa a melhor disponível.
/// </param>
public sealed record SettingsDto(string? DestinationDirectory = null, int? MaximumHeight = null)
{
    /// <summary>O que vale antes de o usuário mudar qualquer coisa.</summary>
    public static SettingsDto Default { get; } = new();
}
