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
    /// Limpar nao apaga arquivo nenhum, mas tambem nao tem como ser desfeito.
    /// </summary>
    /// <remarks>
    /// A pergunta fica na janela, e nao no ViewModel: quem decide como perguntar
    /// e a apresentacao.
    /// </remarks>
    private void OnClearRequested(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            this,
            "Esquecer todos os registros? Os arquivos ja baixados continuam onde estao.",
            "Limpar historico",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
        {
            _viewModel.ClearCommand.Execute(null);
        }
    }
}
