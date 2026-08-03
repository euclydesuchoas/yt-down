using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YTDown.Application.DependencyInjection;
using YTDown.Infrastructure.DependencyInjection;
using YTDown.UI.ViewModels;
using YTDown.UI.Views;

namespace YTDown.UI;

/// <summary>
/// Composition root do aplicativo.
/// </summary>
/// <remarks>
/// Cada camada registra os próprios serviços, então aqui basta compor as
/// camadas e abrir a janela. Nenhuma implementação concreta é citada.
/// </remarks>
public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        // O histórico é uma janela por abertura, e não uma só reaproveitada: a
        // lista precisa chegar recém-lida do disco.
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<HistoryWindow>();
        services.AddSingleton<Func<HistoryWindow>>(provider => provider.GetRequiredService<HistoryWindow>);

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<Func<SettingsWindow>>(provider => provider.GetRequiredService<SettingsWindow>);

        _services = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();

        base.OnExit(e);
    }
}
