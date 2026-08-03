using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;
using YTDown.UI.Resources;

namespace YTDown.UI.ViewModels;

/// <summary>
/// Tela única do aplicativo: recebe um endereço, mostra o vídeo e o baixa.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IVideoInfoService _videoInfoService;
    private readonly IDownloadService _downloadService;
    private readonly IFileExplorer _fileExplorer;
    private readonly IToolMaintenanceService _toolMaintenanceService;
    private readonly ISettingsService _settingsService;
    private readonly IDownloadHistoryService _downloadHistory;

    private SettingsDto _settings = SettingsDto.Default;

    public MainViewModel(
        IVideoInfoService videoInfoService,
        IDownloadService downloadService,
        IFileExplorer fileExplorer,
        IToolMaintenanceService toolMaintenanceService,
        ISettingsService settingsService,
        IDownloadHistoryService downloadHistory)
    {
        _videoInfoService = videoInfoService;
        _downloadService = downloadService;
        _fileExplorer = fileExplorer;
        _toolMaintenanceService = toolMaintenanceService;
        _settingsService = settingsService;
        _downloadHistory = downloadHistory;
    }

    /// <summary>
    /// Aviso discreto sobre a preparação das ferramentas. Vazio quando está tudo
    /// em ordem, para não ocupar a tela com o que não exige atenção.
    /// </summary>
    [ObservableProperty]
    private string? _maintenanceMessage;

    /// <summary>
    /// Deixa o yt-dlp instalado em pasta gravável e tenta atualizá-lo.
    /// </summary>
    /// <remarks>
    /// Roda em paralelo com o uso da tela: enquanto não termina, um download
    /// ainda funciona com a cópia que acompanha a instalação.
    /// </remarks>
    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshSettingsAsync(cancellationToken);
        await RefreshDestinationsAsync(cancellationToken);

        var status = new Progress<ToolMaintenanceStatus>(
            value => MaintenanceMessage = ToolMaintenanceText.For(value));

        await _toolMaintenanceService.PrepareAsync(status, cancellationToken);
    }

    /// <summary>
    /// Relê as preferências.
    /// </summary>
    /// <remarks>
    /// Chamado também quando a tela de configurações fecha, para que a escolha
    /// valha já no próximo download, sem reabrir o aplicativo.
    /// </remarks>
    [RelayCommand]
    private async Task RefreshSettingsAsync(CancellationToken cancellationToken)
    {
        _settings = await _settingsService.GetAsync(cancellationToken);

        SelectedQuality = PreferredQuality();
    }

    /// <summary>
    /// Quantas pastas recentes cabem na lista sem transforma-la em um segundo
    /// histórico.
    /// </summary>
    private const int RecentFolderCount = 5;

    /// <summary>
    /// Recarrega as pastas oferecidas, preservando a que está escolhida.
    /// </summary>
    private async Task RefreshDestinationsAsync(CancellationToken cancellationToken)
    {
        var recent = await _downloadHistory.GetRecentFoldersAsync(RecentFolderCount, cancellationToken);
        var chosen = SelectedDestination;

        List<DestinationOption> options = [DestinationOption.Default];
        options.AddRange(recent.Select(folder => new DestinationOption(folder)));

        // Uma pasta recém-apontada no seletor ainda não está no histórico, que só
        // registra downloads concluídos. Sem isto ela sumiria da lista no instante
        // seguinte ao de ser escolhida.
        if (chosen.Path is { Length: > 0 } path && !recent.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(chosen);
        }

        Destinations = options;
        SelectedDestination = options.FirstOrDefault(option => option == chosen) ?? DestinationOption.Default;
    }

    /// <summary>
    /// Passa a usar uma pasta que o usuário acabou de apontar no seletor.
    /// </summary>
    /// <remarks>
    /// Quem escolhe a pasta é a janela, que conhece o seletor do Windows; o
    /// ViewModel só recebe o resultado.
    /// </remarks>
    public async Task UseFolderAsync(string folder, CancellationToken cancellationToken = default)
    {
        SelectedDestination = new DestinationOption(folder);

        await RefreshDestinationsAsync(cancellationToken);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private VideoInfoDto? _video;

    /// <summary>
    /// Qualidades do vídeo consultado, da maior para a menor. Fica vazia até
    /// que uma busca seja feita, porque só o próprio vídeo sabe o que oferece.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<VideoQualityOption> _availableQualities = [];

    [ObservableProperty]
    private VideoQualityOption? _selectedQuality;

    /// <summary>Baixar somente a trilha sonora, convertida para MP3.</summary>
    [ObservableProperty]
    private bool _audioOnly;

    /// <summary>
    /// Nome do arquivo, sem extensão.
    /// </summary>
    /// <remarks>
    /// Começa com o título do vídeo já limpo, para que o campo mostre o nome que
    /// será gravado, e não uma promessa que o disco recusaria. Vazio volta a
    /// valer o título.
    /// </remarks>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// Extensão mostrada ao lado do campo. Não é digitada: ela é consequência da
    /// escolha entre vídeo e áudio.
    /// </summary>
    public string ExtensionText => AudioOnly ? ".mp3" : ".mp4";

    partial void OnAudioOnlyChanged(bool value) => OnPropertyChanged(nameof(ExtensionText));

    /// <summary>
    /// Pastas oferecidas: a padrão, sempre primeiro, seguida das usadas
    /// recentemente.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<DestinationOption> _destinations = [DestinationOption.Default];

    /// <summary>
    /// Pasta deste download.
    /// </summary>
    /// <remarks>
    /// Continua valendo entre um download e outro de propósito. Quem está
    /// separando doze músicas em uma pasta escolheria a mesma doze vezes, o que
    /// é quase tão ruim quanto ir às configurações. Volta ao padrão ao fechar o
    /// aplicativo, que é onde a preferência duradoura mora.
    /// </remarks>
    [ObservableProperty]
    private DestinationOption _selectedDestination = DestinationOption.Default;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private DownloadProgressDto? _progress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenContainingFolderCommand))]
    private DownloadedFileDto? _downloadedFile;

    /// <summary>
    /// Duração pronta para leitura. Transmissões ao vivo chegam com duração zero.
    /// </summary>
    public string? DurationText => Video switch
    {
        null => null,
        { Duration.Ticks: 0 } => "Ao vivo",
        { Duration: var duration } when duration.TotalHours >= 1 => duration.ToString(@"h\:mm\:ss"),
        { Duration: var duration } => duration.ToString(@"m\:ss")
    };

    public string? ProgressText => Progress is null ? null : DownloadProgressText.For(Progress);

    /// <summary>Autoria e versão, no rodapé da janela.</summary>
    public string Credit => ApplicationInfo.Credit;

    partial void OnVideoChanged(VideoInfoDto? value)
    {
        OnPropertyChanged(nameof(DurationText));

        AvailableQualities = value is null
            ? []
            : [.. value.AvailableHeights.Select(height => new VideoQualityOption(height))];

        SelectedQuality = PreferredQuality();

        FileName = value is not null && OutputFileName.TryCreate(value.Title, out var suggested)
            ? suggested.Value
            : string.Empty;
    }

    /// <summary>
    /// A qualidade já marcada quando a lista aparece.
    /// </summary>
    /// <remarks>
    /// O teto escolhido nas configurações é um limite, e não uma exigência: um
    /// vídeo que só exista abaixo dele continua sendo oferecido. Sem teto, a
    /// maior é a escolha esperada por quem não quer escolher.
    /// </remarks>
    private VideoQualityOption? PreferredQuality() =>
        _settings.MaximumHeight is { } maximum
            ? AvailableQualities.FirstOrDefault(quality => quality.Height <= maximum)
              ?? AvailableQualities.LastOrDefault()
            : AvailableQualities.FirstOrDefault();

    partial void OnProgressChanged(DownloadProgressDto? value) => OnPropertyChanged(nameof(ProgressText));

    /// <summary>
    /// Mexeu no endereço, o resultado na tela deixa de valer.
    /// </summary>
    /// <remarks>
    /// Sem isso seria possível buscar um vídeo, colar outro endereço e baixar o
    /// segundo enquanto o primeiro continua na tela. Limpar também desabilita o
    /// Baixar, o que obriga a buscar de novo.
    /// </remarks>
    partial void OnUrlChanged(string value)
    {
        if (Video is not null)
        {
            ResetResults();
        }
    }

    private bool CanUseUrl() => !string.IsNullOrWhiteSpace(Url);

    /// <summary>
    /// Baixar exige uma busca bem-sucedida: é ela que diz quais qualidades
    /// existem e confirma que o endereço aponta para o vídeo certo.
    /// </summary>
    private bool CanDownload() => Video is not null;

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

    [RelayCommand(CanExecute = nameof(CanDownload), IncludeCancelCommand = true)]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        DownloadedFile = null;

        // Construído aqui, na linha da interface, para que cada atualização
        // volte para ela sem que o ViewModel precise tratar disso.
        var progress = new Progress<DownloadProgressDto>(value => Progress = value);

        var result = await _downloadService.DownloadAsync(Url, BuildOptions(), progress, cancellationToken);

        Progress = null;

        if (result.IsSuccess)
        {
            DownloadedFile = result.Value;

            // A pasta usada agora está no histórico, e vira uma opção para o
            // próximo download.
            await RefreshDestinationsAsync(CancellationToken.None);
            return;
        }

        ShowFailure(result.Error.Value);
    }

    private bool CanOpenContainingFolder() => DownloadedFile is not null;

    [RelayCommand(CanExecute = nameof(CanOpenContainingFolder))]
    private void OpenContainingFolder() => _fileExplorer.RevealFile(DownloadedFile!.FilePath);

    /// <summary>
    /// Traduz as escolhas da tela no que o download precisa saber.
    /// </summary>
    /// <remarks>
    /// A qualidade só fica nula quando o vídeo não declara altura nenhuma, o que
    /// acontece em transmissões ao vivo. Aí vale o teto das configurações, em
    /// vez de nenhum limite.
    /// </remarks>
    private DownloadOptionsDto BuildOptions() =>
        AudioOnly
            ? new DownloadOptionsDto(
                MediaKind.AudioOnly,
                DestinationDirectory: SelectedDestination.Path,
                FileName: FileName)
            : new DownloadOptionsDto(
                MediaKind.Video,
                SelectedQuality?.Height ?? _settings.MaximumHeight,
                SelectedDestination.Path,
                FileName);

    private void ResetResults()
    {
        ErrorMessage = null;
        Video = null;
        DownloadedFile = null;
        Progress = null;
    }

    /// <summary>
    /// Cancelar foi uma escolha do usuário, não uma falha: nada a comunicar.
    /// </summary>
    private void ShowFailure(ErrorCode error)
    {
        if (error != ErrorCode.Canceled)
        {
            ErrorMessage = ErrorMessages.For(error);
        }
    }
}
