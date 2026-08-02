using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.UI.Resources;

namespace YTDown.UI.ViewModels;

/// <summary>
/// Tela unica do aplicativo: recebe um endereco, mostra o video e o baixa.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IVideoInfoService _videoInfoService;
    private readonly IDownloadService _downloadService;
    private readonly IFileExplorer _fileExplorer;

    public MainViewModel(
        IVideoInfoService videoInfoService,
        IDownloadService downloadService,
        IFileExplorer fileExplorer)
    {
        _videoInfoService = videoInfoService;
        _downloadService = downloadService;
        _fileExplorer = fileExplorer;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private VideoInfoDto? _video;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private DownloadProgressDto? _progress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenContainingFolderCommand))]
    private DownloadedFileDto? _downloadedFile;

    /// <summary>
    /// Duracao pronta para leitura. Transmissoes ao vivo chegam com duracao zero.
    /// </summary>
    public string? DurationText => Video switch
    {
        null => null,
        { Duration.Ticks: 0 } => "Ao vivo",
        { Duration: var duration } when duration.TotalHours >= 1 => duration.ToString(@"h\:mm\:ss"),
        { Duration: var duration } => duration.ToString(@"m\:ss")
    };

    public string? ProgressText => Progress is null ? null : DownloadProgressText.For(Progress);

    partial void OnVideoChanged(VideoInfoDto? value) => OnPropertyChanged(nameof(DurationText));

    partial void OnProgressChanged(DownloadProgressDto? value) => OnPropertyChanged(nameof(ProgressText));

    private bool CanUseUrl() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanUseUrl), IncludeCancelCommand = true)]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        ResetResults();

        var result = await _videoInfoService.GetVideoInfoAsync(Url, cancellationToken);

        if (result.IsSuccess)
        {
            Video = result.Value;
            return;
        }

        ShowFailure(result.Error.Value);
    }

    [RelayCommand(CanExecute = nameof(CanUseUrl), IncludeCancelCommand = true)]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        DownloadedFile = null;

        // Construido aqui, na linha da interface, para que cada atualizacao
        // volte para ela sem que o ViewModel precise tratar disso.
        var progress = new Progress<DownloadProgressDto>(value => Progress = value);

        var result = await _downloadService.DownloadAsync(Url, progress, cancellationToken);

        Progress = null;

        if (result.IsSuccess)
        {
            DownloadedFile = result.Value;
            return;
        }

        ShowFailure(result.Error.Value);
    }

    private bool CanOpenContainingFolder() => DownloadedFile is not null;

    [RelayCommand(CanExecute = nameof(CanOpenContainingFolder))]
    private void OpenContainingFolder() => _fileExplorer.RevealFile(DownloadedFile!.FilePath);

    private void ResetResults()
    {
        ErrorMessage = null;
        Video = null;
        DownloadedFile = null;
        Progress = null;
    }

    /// <summary>
    /// Cancelar foi uma escolha do usuario, nao uma falha: nada a comunicar.
    /// </summary>
    private void ShowFailure(ErrorCode error)
    {
        if (error != ErrorCode.Canceled)
        {
            ErrorMessage = ErrorMessages.For(error);
        }
    }
}
