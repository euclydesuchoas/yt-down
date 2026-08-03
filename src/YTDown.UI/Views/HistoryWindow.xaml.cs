using System.Windows;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryViewModel _viewModel;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += (_, _) => viewModel.LoadCommand.Execute(null);
    }

    /// <summary>
    /// Limpar não apaga arquivo nenhum, mas também não tem como ser desfeito.
    /// </summary>
    /// <remarks>
    /// A pergunta fica na janela, e não no ViewModel: quem decide como perguntar
    /// é a apresentação.
    /// </remarks>
    private void OnClearRequested(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            this,
            "Esquecer todos os registros? Os arquivos já baixados continuam onde estão.",
            "Limpar histórico",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
        {
            _viewModel.ClearCommand.Execute(null);
        }
    }
}
