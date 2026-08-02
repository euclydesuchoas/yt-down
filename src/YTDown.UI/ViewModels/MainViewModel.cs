using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;
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
        await RefreshSettingsAsync(cancellationToken);
        await RefreshDestinationsAsync(cancellationToken);

        var status = new Progress<ToolMaintenanceStatus>(
            value => MaintenanceMessage = ToolMaintenanceText.For(value));

        await _toolMaintenanceService.PrepareAsync(status, cancellationToken);
    }

    /// <summary>
    /// Rele as preferencias.
    /// </summary>
    /// <remarks>
    /// Chamado tambem quando a tela de configuracoes fecha, para que a escolha
    /// valha ja no proximo download, sem reabrir o aplicativo.
    /// </remarks>
    [RelayCommand]
    private async Task RefreshSettingsAsync(CancellationToken cancellationToken)
    {
        _settings = await _settingsService.GetAsync(cancellationToken);

        SelectedQuality = PreferredQuality();
    }

    /// <summary>
    /// Quantas pastas recentes cabem na lista sem transforma-la em um segundo
    /// historico.
    /// </summary>
    private const int RecentFolderCount = 5;

    /// <summary>
    /// Recarrega as pastas oferecidas, preservando a que esta escolhida.
    /// </summary>
    private async Task RefreshDestinationsAsync(CancellationToken cancellationToken)
    {
        var recent = await _downloadHistory.GetRecentFoldersAsync(RecentFolderCount, cancellationToken);
        var chosen = SelectedDestination;

        List<DestinationOption> options = [DestinationOption.Default];
        options.AddRange(recent.Select(folder => new DestinationOption(folder)));

        // Uma pasta recem-apontada no seletor ainda nao esta no historico, que so
        // registra downloads concluidos. Sem isto ela sumiria da lista no instante
        // seguinte ao de ser escolhida.
        if (chosen.Path is { Length: > 0 } path && !recent.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(chosen);
        }

        Destinations = options;
        SelectedDestination = options.FirstOrDefault(option => option == chosen) ?? DestinationOption.Default;
    }

    /// <summary>
    /// Passa a usar uma pasta que o usuario acabou de apontar no seletor.
    /// </summary>
    /// <remarks>
    /// Quem escolhe a pasta e a janela, que conhece o seletor do Windows; o
    /// ViewModel so recebe o resultado.
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

    /// <summary>
    /// Nome do arquivo, sem extensao.
    /// </summary>
    /// <remarks>
    /// Comeca com o titulo do video ja limpo, para que o campo mostre o nome que
    /// sera gravado, e nao uma promessa que o disco recusaria. Vazio volta a
    /// valer o titulo.
    /// </remarks>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// Extensao mostrada ao lado do campo. Nao e digitada: ela e consequencia da
    /// escolha entre video e audio.
    /// </summary>
    public string ExtensionText => AudioOnly ? ".mp3" : ".mp4";

    partial void OnAudioOnlyChanged(bool value) => OnPropertyChanged(nameof(ExtensionText));

    /// <summary>
    /// Pastas oferecidas: a padrao, sempre primeiro, seguida das usadas
    /// recentemente.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<DestinationOption> _destinations = [DestinationOption.Default];

    /// <summary>
    /// Pasta deste download.
    /// </summary>
    /// <remarks>
    /// Continua valendo entre um download e outro de proposito. Quem esta
    /// separando doze musicas em uma pasta escolheria a mesma doze vezes, o que
    /// e quase tao ruim quanto ir as configuracoes. Volta ao padrao ao fechar o
    /// aplicativo, que e onde a preferencia duradoura mora.
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

    /// <summary>Autoria e versao, no rodape da janela.</summary>
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
    /// A qualidade ja marcada quando a lista aparece.
    /// </summary>
    /// <remarks>
    /// O teto escolhido nas configuracoes e um limite, e nao uma exigencia: um
    /// video que so exista abaixo dele continua sendo oferecido. Sem teto, a
    /// maior e a escolha esperada por quem nao quer escolher.
    /// </remarks>
    private VideoQualityOption? PreferredQuality() =>
        _settings.MaximumHeight is { } maximum
            ? AvailableQualities.FirstOrDefault(quality => quality.Height <= maximum)
              ?? AvailableQualities.LastOrDefault()
            : AvailableQualities.FirstOrDefault();

    partial void OnProgressChanged(DownloadProgressDto? value) => OnPropertyChanged(nameof(ProgressText));

    /// <summary>
    /// Mexeu no endereco, o resultado na tela deixa de valer.
    /// </summary>
    /// <remarks>
    /// Sem isso seria possivel buscar um video, colar outro endereco e baixar o
    /// segundo enquanto o primeiro continua na tela. Limpar tambem desabilita o
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
    /// Baixar exige uma busca bem-sucedida: e ela que diz quais qualidades
    /// existem e confirma que o endereco aponta para o video certo.
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

        // Construido aqui, na linha da interface, para que cada atualizacao
        // volte para ela sem que o ViewModel precise tratar disso.
        var progress = new Progress<DownloadProgressDto>(value => Progress = value);

        var result = await _downloadService.DownloadAsync(Url, BuildOptions(), progress, cancellationToken);

        Progress = null;

        if (result.IsSuccess)
        {
            DownloadedFile = result.Value;

            // A pasta usada agora esta no historico, e vira uma opcao para o
            // proximo download.
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
    /// A qualidade so fica nula quando o video nao declara altura nenhuma, o que
    /// acontece em transmissoes ao vivo. Ai vale o teto das configuracoes, em
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
