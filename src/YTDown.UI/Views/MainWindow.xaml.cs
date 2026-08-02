using System.Windows;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Func<HistoryWindow> _createHistoryWindow;
    private readonly Func<SettingsWindow> _createSettingsWindow;

    public MainWindow(
        MainViewModel viewModel,
        Func<HistoryWindow> createHistoryWindow,
        Func<SettingsWindow> createSettingsWindow)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        _createHistoryWindow = createHistoryWindow;
        _createSettingsWindow = createSettingsWindow;

        // A preparacao das ferramentas comeca junto com a janela e corre em
        // paralelo: bloquear a abertura para verificar atualizacao deixaria o
        // aplicativo parecendo lento sem necessidade.
        Loaded += (_, _) => viewModel.InitializeCommand.Execute(null);
    }

    /// <summary>
    /// Abre o historico.
    /// </summary>
    /// <remarks>
    /// Uma janela nova a cada abertura, para que a lista chegue recem-lida: o
    /// download que acabou de terminar precisa estar la.
    /// </remarks>
    private void OnHistoryRequested(object sender, RoutedEventArgs e)
    {
        var history = _createHistoryWindow();

        history.Owner = this;
        history.ShowDialog();
    }

    /// <remarks>
    /// A tela principal rele as preferencias ao fechar a de configuracoes, para
    /// que a escolha valha no proximo download sem reabrir o aplicativo.
    /// </remarks>
    private void OnSettingsRequested(object sender, RoutedEventArgs e)
    {
        var settings = _createSettingsWindow();

        settings.Owner = this;

        if (settings.ShowDialog() == true)
        {
            _viewModel.RefreshSettingsCommand.Execute(null);
        }
    }
}
