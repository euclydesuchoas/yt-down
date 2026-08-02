using YTDown.Application.Interfaces;

namespace YTDown.UI.Resources;

/// <summary>
/// Descreve a preparacao das ferramentas em linguagem comum.
/// </summary>
internal static class ToolMaintenanceText
{
    /// <returns>Nulo quando nao ha nada que valha a pena dizer.</returns>
    public static string? For(ToolMaintenanceStatus status) => status switch
    {
        ToolMaintenanceStatus.Installing => "Preparando os componentes...",
        ToolMaintenanceStatus.CheckingForUpdate => "Verificando atualizacoes...",

        // Ficar sem atualizar quase sempre significa estar sem internet. Dito
        // assim, informa sem assustar, e deixa claro que da para usar mesmo
        // assim.
        ToolMaintenanceStatus.UpdateUnavailable =>
            "Nao foi possivel verificar atualizacoes. O aplicativo continua funcionando.",

        _ => null
    };
}
