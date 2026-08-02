using System.Windows;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // A preparacao das ferramentas comeca junto com a janela e corre em
        // paralelo: bloquear a abertura para verificar atualizacao deixaria o
        // aplicativo parecendo lento sem necessidade.
        Loaded += (_, _) => viewModel.InitializeCommand.Execute(null);
    }
}
