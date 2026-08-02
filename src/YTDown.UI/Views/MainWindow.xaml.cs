using System.Windows;
using YTDown.UI.ViewModels;

namespace YTDown.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
