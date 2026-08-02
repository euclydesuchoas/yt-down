using Microsoft.Extensions.DependencyInjection;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;

namespace YTDown.Application.DependencyInjection;

/// <summary>
/// Registra os servicos da camada Application.
/// </summary>
/// <remarks>
/// Cada camada expoe seu proprio registro, para que o composition root nao
/// precise conhecer as implementacoes concretas de nenhuma delas.
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IVideoInfoService, VideoInfoService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IToolMaintenanceService, ToolMaintenanceService>();

        return services;
    }
}
