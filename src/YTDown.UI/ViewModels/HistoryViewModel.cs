using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.Interfaces;

namespace YTDown.UI.ViewModels;

/// <summary>
/// A lista do que já foi baixado.
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IDownloadHistoryService _downloadHistory;
    private readonly IFileExplorer _fileExplorer;
    private readonly TimeProvider _timeProvider;

    public HistoryViewModel(
        IDownloadHistoryService downloadHistory,
        IFileExplorer fileExplorer,
        TimeProvider timeProvider)
    {
        _downloadHistory = downloadHistory;
        _fileExplorer = fileExplorer;
        _timeProvider = timeProvider;
    }

    [ObservableProperty]
    private IReadOnlyList<DownloadHistoryItem> _items = [];

    public bool IsEmpty => Items.Count == 0;

    partial void OnItemsChanged(IReadOnlyList<DownloadHistoryItem> value) => OnPropertyChanged(nameof(IsEmpty));

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow();
        var entries = await _downloadHistory.GetRecentAsync(cancellationToken);

        Items = [.. entries.Select(entry => new DownloadHistoryItem(entry, now))];
    }

    /// <summary>
    /// Abre a pasta do arquivo, e não o arquivo: o usuário costuma querer
    /// mover, copiar ou renomear o que baixou.
    /// </summary>
    [RelayCommand]
    private void OpenContainingFolder(DownloadHistoryItem item) => _fileExplorer.RevealFile(item.Entry.FilePath);

    /// <summary>
    /// Esquece os registros. Nenhum arquivo baixado é apagado.
    /// </summary>
    [RelayCommand]
    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _downloadHistory.ClearAsync(cancellationToken);

        // Recarrega em vez de esvaziar a lista na mão: se a gravação falhou, a
        // tela mostra o que de fato sobrou.
        await LoadAsync(cancellationToken);
    }
}
