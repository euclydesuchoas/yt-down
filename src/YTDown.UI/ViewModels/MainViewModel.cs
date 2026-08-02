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
    private readonly IToolMaintenanceService _toolMaintenanceService;

    public MainViewModel(
        IVideoInfoService videoInfoService,
        IDownloadService downloadService,
        IFileExplorer fileExplorer,
        IToolMaintenanceService toolMaintenanceService)
    {
        _videoInfoService = videoInfoService;
        _downloadService = downloadService;
        _fileExplorer = fileExplorer;
        _toolMaintenanceService = toolMaintenanceService;
    }

    /// <summary>
    /// Aviso discreto sobre a preparacao das ferramentas. Vazio quando esta tudo
    /// em ordem, para nao ocupar a tela com o que nao exige atencao.
    /// </summary>
    [ObservableProperty]
    private string? _maintenanceMessage;

    /// <summary>
    /// Deixa o yt-dlp instalado em pasta gravavel e tenta atualiza-lo.
    /// </summary>
    /// <remarks>
    /// Roda em paralelo com o uso da tela: enquanto nao termina, um download
    /// ainda funciona com a copia que acompanha a instalacao.
    /// </remarks>
    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var status = new Progress<ToolMaintenanceStatus>(
            value => MaintenanceMessage = ToolMaintenanceText.For(value));

        await _toolMaintenanceService.PrepareAsync(status, cancellationToken);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private VideoInfoDto? _video;

    /// <summary>
    /// Qualidades do video consultado, da maior para a menor. Fica vazia ate
    /// que uma busca seja feita, porque so o proprio video sabe o que oferece.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<VideoQualityOption> _availableQualities = [];

    [ObservableProperty]
    private VideoQualityOption? _selectedQuality;

    /// <summary>Baixar somente a trilha sonora, convertida para MP3.</summary>
    [ObservableProperty]
    private bool _audioOnly;

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

    partial void OnVideoChanged(VideoInfoDto? value)
    {
        OnPropertyChanged(nameof(DurationText));

        AvailableQualities = value is null
            ? []
            : [.. value.AvailableHeights.Select(height => new VideoQualityOption(height))];

        // A maior qualidade e a escolha esperada por quem nao quer escolher.
        SelectedQuality = AvailableQualities.FirstOrDefault();
    }

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

        var result = await _downloadService.DownloadAsync(Url, BuildOptions(), progress, cancellationToken);

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

    /// <summary>
    /// Sem busca previa nao ha qualidades conhecidas, e o download usa a melhor
    /// disponivel: obrigar a buscar antes seria um passo a mais sem ganho.
    /// </summary>
    private DownloadOptionsDto BuildOptions() =>
        AudioOnly
            ? DownloadOptionsDto.AudioOnly
            : new DownloadOptionsDto(MediaKind.Video, SelectedQuality?.Height);

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
