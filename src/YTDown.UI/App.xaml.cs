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
/// Cada camada registra os proprios servicos, entao aqui basta compor as
/// camadas e abrir a janela. Nenhuma implementacao concreta e citada.
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
