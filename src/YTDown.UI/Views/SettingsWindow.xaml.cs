using System.Windows;
using Microsoft.Win32;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += (_, _) => viewModel.LoadCommand.Execute(null);
    }

    /// <remarks>
    /// O seletor de pastas é do Windows, e escolher pasta é assunto da janela:
    /// o ViewModel recebe o resultado, não a caixa de diálogo.
    /// </remarks>
    private void OnChooseFolderRequested(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Escolha onde salvar os downloads",
            InitialDirectory = _viewModel.DestinationDirectory ?? string.Empty
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.DestinationDirectory = dialog.FolderName;
        }
    }

    private async void OnSaveRequested(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveCommand.ExecuteAsync(null);

        DialogResult = true;
    }
}
