using Microsoft.Extensions.DependencyInjection;
using YTDown.Application.Interfaces;
using YTDown.Infrastructure.FileSystem;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;
using YTDown.Infrastructure.YouTube;

namespace YTDown.Infrastructure.DependencyInjection;

/// <summary>
/// Registra as integracoes externas.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        services.AddSingleton<ToolLocations>();
        services.AddSingleton<IToolLocator, ManagedToolLocator>();
        services.AddSingleton<IToolInstaller, YtDlpInstaller>();
        services.AddSingleton<IToolUpdater, YtDlpUpdater>();
        services.AddSingleton<IVideoMetadataProvider, YtDlpMetadataProvider>();
        services.AddSingleton<IVideoDownloader, YtDlpVideoDownloader>();
        services.AddSingleton<IDownloadLocationProvider, WindowsDownloadLocationProvider>();
        services.AddSingleton<IFileExplorer, WindowsFileExplorer>();
        services.AddSingleton<IDownloadHistoryStore, JsonDownloadHistoryStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        return services;
    }
}
