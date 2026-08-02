namespace YTDown.Application.DTOs;

/// <summary>
/// O que o usuario pode decidir uma vez e nao precisar repetir.
/// </summary>
/// <param name="DestinationDirectory">
/// Onde salvar. <c>null</c> significa a pasta Downloads do usuario, que e o
/// destino de quem nunca abriu as configuracoes.
/// </param>
/// <param name="MaximumHeight">
/// Teto de qualidade, e nao a qualidade exigida: um video que so exista em 480p
/// continua sendo baixado em 480p. <c>null</c> significa a melhor disponivel.
/// </param>
public sealed record SettingsDto(string? DestinationDirectory = null, int? MaximumHeight = null)
{
    /// <summary>O que vale antes de o usuario mudar qualquer coisa.</summary>
    public static SettingsDto Default { get; } = new();
}
