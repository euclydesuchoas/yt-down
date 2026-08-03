namespace YTDown.Application.Interfaces;

/// <summary>
/// Deixa as ferramentas externas prontas para uso.
/// </summary>
public interface IToolMaintenanceService
{
    /// <param name="onStatusChanged">
    /// Recebe uma descrição curta do que está acontecendo, para que a tela possa
    /// informar em vez de parecer travada.
    /// </param>
    Task PrepareAsync(IProgress<ToolMaintenanceStatus> onStatusChanged, CancellationToken cancellationToken);
}

/// <summary>
/// O que a manutenção das ferramentas está fazendo.
/// </summary>
public enum ToolMaintenanceStatus
{
    Installing,
    CheckingForUpdate,
    Ready,

    /// <summary>
    /// A atualização falhou, mas o aplicativo continua utilizável com a versão
    /// que já possui.
    /// </summary>
    UpdateUnavailable
}
