using System.Windows;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class MainWindow : Window
{
    private readonly Func<HistoryWindow> _createHistoryWindow;

    public MainWindow(MainViewModel viewModel, Func<HistoryWindow> createHistoryWindow)
    {
        InitializeComponent();

        DataContext = viewModel;
        _createHistoryWindow = createHistoryWindow;

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
}
