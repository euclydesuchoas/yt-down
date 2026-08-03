using YTDown.Application.Interfaces;

namespace YTDown.Application.Services;

/// <inheritdoc cref="IToolMaintenanceService" />
public sealed class ToolMaintenanceService : IToolMaintenanceService
{
    private readonly IToolInstaller _toolInstaller;
    private readonly IToolUpdater _toolUpdater;

    public ToolMaintenanceService(IToolInstaller toolInstaller, IToolUpdater toolUpdater)
    {
        _toolInstaller = toolInstaller;
        _toolUpdater = toolUpdater;
    }

    public async Task PrepareAsync(
        IProgress<ToolMaintenanceStatus> onStatusChanged,
        CancellationToken cancellationToken)
    {
        onStatusChanged.Report(ToolMaintenanceStatus.Installing);

        var installation = await _toolInstaller.EnsureInstalledAsync(cancellationToken);

        // Falhar a instalação na pasta gravável não impede o uso: o aplicativo
        // recorre à cópia que acompanha a instalação. Só a atualização se perde.
        if (!installation.IsSuccess)
        {
            onStatusChanged.Report(ToolMaintenanceStatus.UpdateUnavailable);
            return;
        }

        onStatusChanged.Report(ToolMaintenanceStatus.CheckingForUpdate);

        var update = await _toolUpdater.UpdateAsync(cancellationToken);

        onStatusChanged.Report(update.IsSuccess
            ? ToolMaintenanceStatus.Ready
            : ToolMaintenanceStatus.UpdateUnavailable);
    }
}
