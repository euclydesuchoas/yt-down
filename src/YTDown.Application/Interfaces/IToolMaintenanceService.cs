namespace YTDown.Application.Interfaces;

/// <summary>
/// Deixa as ferramentas externas prontas para uso.
/// </summary>
public interface IToolMaintenanceService
{
    /// <param name="onStatusChanged">
    /// Recebe uma descricao curta do que esta acontecendo, para que a tela possa
    /// informar em vez de parecer travada.
    /// </param>
    Task PrepareAsync(IProgress<ToolMaintenanceStatus> onStatusChanged, CancellationToken cancellationToken);
}

/// <summary>
/// O que a manutencao das ferramentas esta fazendo.
/// </summary>
public enum ToolMaintenanceStatus
{
    Installing,
    CheckingForUpdate,
    Ready,

    /// <summary>
    /// A atualizacao falhou, mas o aplicativo continua utilizavel com a versao
    /// que ja possui.
    /// </summary>
    UpdateUnavailable
}
