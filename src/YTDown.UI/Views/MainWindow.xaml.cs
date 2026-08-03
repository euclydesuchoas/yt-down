using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using YTDown.Domain.ValueObjects;
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

        // A preparação das ferramentas começa junto com a janela e corre em
        // paralelo: bloquear a abertura para verificar atualização deixaria o
        // aplicativo parecendo lento sem necessidade.
        Loaded += (_, _) => viewModel.InitializeCommand.Execute(null);
    }

    /// <summary>
    /// Abre o histórico.
    /// </summary>
    /// <remarks>
    /// Uma janela nova a cada abertura, para que a lista chegue recém-lida: o
    /// download que acabou de terminar precisa estar lá.
    /// </remarks>
    private void OnHistoryRequested(object sender, RoutedEventArgs e)
    {
        var history = _createHistoryWindow();

        history.Owner = this;
        history.ShowDialog();
    }

    /// <summary>
    /// Seleciona o nome inteiro ao receber o foco.
    /// </summary>
    /// <remarks>
    /// O campo chega preenchido com o título do vídeo, que pode estar em um
    /// alfabeto que o usuário nem lê. Deixar o cursor no meio desse texto
    /// obrigaria a apagar caractere por caractere; selecionado, basta digitar.
    /// </remarks>
    private void OnFileNameFocused(object sender, KeyboardFocusChangedEventArgs e) => FileNameBox.SelectAll();

    /// <remarks>
    /// O clique do mouse posiciona o cursor e desfaria a seleção acima. Quando o
    /// campo ainda não tem o foco, o clique só o entrega.
    /// </remarks>
    private void OnFileNameClicked(object sender, MouseButtonEventArgs e)
    {
        if (!FileNameBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            FileNameBox.Focus();
        }
    }

    /// <remarks>
    /// Recusa a tecla no momento em que ela é digitada, em vez de alterar o
    /// texto depois e mover o cursor de lugar. A limpeza definitiva continua
    /// acontecendo antes do download, porque texto colado não passa por aqui.
    /// </remarks>
    private void OnFileNameTyping(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(OutputFileName.IsAllowedCharacter);

    /// <remarks>
    /// O seletor de pastas é do Windows, e escolher pasta é assunto da janela: o
    /// ViewModel recebe o caminho, não a caixa de diálogo.
    /// </remarks>
    private async void OnChooseDestinationRequested(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Escolha onde salvar este download",
            InitialDirectory = _viewModel.SelectedDestination.Path ?? string.Empty
        };

        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.UseFolderAsync(dialog.FolderName);
        }
    }

    /// <remarks>
    /// A tela principal relê as preferências ao fechar a de configurações, para
    /// que a escolha valha no próximo download sem reabrir o aplicativo.
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
