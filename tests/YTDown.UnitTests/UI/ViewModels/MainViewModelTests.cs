using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.UI.ViewModels;

namespace YTDown.UnitTests.UI.ViewModels;

public class MainViewModelTests
{
    private const string ValidUrl = "https://www.youtube.com/watch?v=UKcJqQqiXq0";

    private readonly Mock<IVideoInfoService> _videoInfoService = new();
    private readonly Mock<IDownloadService> _downloadService = new();
    private readonly Mock<IFileExplorer> _fileExplorer = new();
    private readonly Mock<IToolMaintenanceService> _toolMaintenanceService = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Mock<IDownloadHistoryService> _downloadHistory = new();

    public MainViewModelTests()
    {
        GivenSettings(SettingsDto.Default);
        GivenRecentFolders();
    }

    private void GivenRecentFolders(params string[] folders) =>
        _downloadHistory
            .Setup(history => history.GetRecentFoldersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders);

    private void GivenSettings(SettingsDto settings) =>
        _settingsService
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

    private MainViewModel CreateViewModel() =>
        new(_videoInfoService.Object,
            _downloadService.Object,
            _fileExplorer.Object,
            _toolMaintenanceService.Object,
            _settingsService.Object,
            _downloadHistory.Object);

    private static DownloadedFileDto AnyDownloadedFile =>
        new(@"C:\Users\Euclydes\Downloads\video.mp4", "video.mp4", 20_000_000);

    private void GivenDownloadReturns(
        Result<DownloadedFileDto> result,
        Action<DownloadOptionsDto>? captureOptions = null) =>
        _downloadService
            .Setup(service => service.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<DownloadOptionsDto>(),
                It.IsAny<IProgress<DownloadProgressDto>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string?, DownloadOptionsDto, IProgress<DownloadProgressDto>, CancellationToken>(
                (_, options, _, _) => captureOptions?.Invoke(options))
            .ReturnsAsync(result);

    private void GivenSearchReturns(IReadOnlyList<int> availableHeights) =>
        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(new VideoInfoDto(
                "UKcJqQqiXq0", "Titulo", "Canal", TimeSpan.FromSeconds(96), null, ValidUrl, availableHeights)));

    /// <summary>
    /// A versao sai do assembly, e nao de uma constante: uma copia divergiria da
    /// declarada no csproj na primeira publicacao.
    /// </summary>
    [Fact]
    public void Credit_NamesTheAuthorAndTheVersionThatIsRunning()
    {
        var credit = CreateViewModel().Credit;

        credit.Should().Contain("Euclydes Uchoas");
        credit.Should().MatchRegex(@"YTDown \d+\.\d+\.\d+");
    }

    /// <summary>
    /// Prepara a tela no estado em que o usuario pode baixar: endereco colado e
    /// busca concluida.
    /// </summary>
    private async Task<MainViewModel> AfterSearchAsync(IReadOnlyList<int>? availableHeights = null)
    {
        GivenSearchReturns(availableHeights ?? [1080, 720, 480]);

        var viewModel = CreateViewModel();
        await viewModel.RefreshSettingsCommand.ExecuteAsync(null);

        viewModel.Url = ValidUrl;
        await viewModel.SearchCommand.ExecuteAsync(null);

        return viewModel;
    }

    [Fact]
    public void SearchCommand_IsDisabledWhileTheAddressIsEmpty()
    {
        var viewModel = CreateViewModel();

        viewModel.SearchCommand.CanExecute(null).Should().BeFalse();

        viewModel.Url = ValidUrl;

        viewModel.SearchCommand.CanExecute(null).Should().BeTrue();
    }

    /// <summary>
    /// Baixar sem buscar antes esconderia do usuario a qualidade que ele vai
    /// receber, e nao confirmaria que o endereco aponta para o video certo.
    /// </summary>
    [Fact]
    public async Task DownloadCommand_IsDisabledUntilASearchSucceeds()
    {
        GivenSearchReturns([1080, 720]);

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        viewModel.DownloadCommand.CanExecute(null).Should().BeFalse();

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DownloadCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadCommand_WhenTheSearchFails_StaysDisabled()
    {
        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Failure(ErrorCode.VideoUnavailable));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DownloadCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>
    /// Sem isso seria possivel buscar um video, colar outro endereco e baixar o
    /// segundo enquanto o primeiro continua na tela.
    /// </summary>
    [Fact]
    public async Task ChangingTheAddress_DiscardsTheVideoThatWasFoundAndDisablesDownloading()
    {
        var viewModel = await AfterSearchAsync();

        viewModel.Url = "https://www.youtube.com/watch?v=jfKfPfyJRdk";

        viewModel.Video.Should().BeNull();
        viewModel.AvailableQualities.Should().BeEmpty();
        viewModel.DownloadCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task DownloadCommand_WhenItSucceeds_KeepsTheFileAndEnablesOpeningTheFolder()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = await AfterSearchAsync();

        viewModel.OpenContainingFolderCommand.CanExecute(null).Should().BeFalse();

        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.DownloadedFile.Should().Be(AnyDownloadedFile);
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.Progress.Should().BeNull(because: "o andamento deixa de existir quando termina");
        viewModel.OpenContainingFolderCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadCommand_WhenItFails_ShowsAMessageWithoutTechnicalDetail()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Failure(
            ErrorCode.VideoUnavailable,
            "ERROR: [youtube] UKcJqQqiXq0: Private video"));

        var viewModel = await AfterSearchAsync();

        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.DownloadedFile.Should().BeNull();
        viewModel.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        viewModel.ErrorMessage.Should().NotContain("youtube]", because: "saida de ferramenta nunca chega a tela");
        viewModel.ErrorMessage.Should().NotContain("ERROR");
    }

    /// <summary>
    /// Cancelar foi uma escolha do usuario. Tratar como erro faria o aplicativo
    /// acusar uma falha que nao houve.
    /// </summary>
    [Fact]
    public async Task DownloadCommand_WhenCancelled_ShowsNoError()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Failure(ErrorCode.Canceled));

        var viewModel = await AfterSearchAsync();

        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().BeNull();
        viewModel.DownloadedFile.Should().BeNull();
    }

    [Fact]
    public async Task OpenContainingFolderCommand_RevealsTheDownloadedFile()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = await AfterSearchAsync();
        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.OpenContainingFolderCommand.Execute(null);

        _fileExplorer.Verify(explorer => explorer.RevealFile(AnyDownloadedFile.FilePath), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_WhenItSucceeds_ShowsTheVideoWithAReadableDuration()
    {
        var video = new VideoInfoDto(
            "UKcJqQqiXq0", "Titulo", "Canal", TimeSpan.FromSeconds(96), null, ValidUrl, [1080, 720, 480]);

        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(video));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.Video.Should().Be(video);
        viewModel.DurationText.Should().Be("1:36");
    }

    [Fact]
    public async Task SearchCommand_OffersTheQualitiesOfTheVideoAndPreSelectsTheBest()
    {
        var video = new VideoInfoDto(
            "UKcJqQqiXq0", "Titulo", "Canal", TimeSpan.FromSeconds(96), null, ValidUrl, [1080, 720, 480]);

        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(video));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.AvailableQualities.Select(quality => quality.Label).Should().Equal("1080p", "720p", "480p");
        viewModel.SelectedQuality!.Height.Should().Be(1080);
    }

    [Fact]
    public async Task DownloadCommand_UsesTheChosenQuality()
    {
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync();
        viewModel.SelectedQuality = new VideoQualityOption(720);

        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.Kind.Should().Be(MediaKind.Video);
        options.MaximumHeight.Should().Be(720);
    }

    /// <summary>
    /// Transmissao ao vivo nao declara altura nenhuma. Sem qualidade escolhida,
    /// vale o teto das configuracoes em vez de nenhum limite.
    /// </summary>
    [Fact]
    public async Task DownloadCommand_WhenTheVideoDeclaresNoQuality_UsesTheCeilingFromTheSettings()
    {
        GivenSettings(new SettingsDto(MaximumHeight: 720));
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync([]);

        viewModel.SelectedQuality.Should().BeNull();

        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.MaximumHeight.Should().Be(720);
    }

    [Fact]
    public async Task DownloadCommand_WhenOnlyAudioWasAsked_IgnoresTheChosenQuality()
    {
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync();
        viewModel.SelectedQuality = new VideoQualityOption(720);
        viewModel.AudioOnly = true;

        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.Kind.Should().Be(MediaKind.AudioOnly);
        options.MaximumHeight.Should().BeNull();
    }

    [Fact]
    public async Task SearchCommand_WithALiveStream_ShowsAoVivoInsteadOfZero()
    {
        var liveStream = new VideoInfoDto("jfKfPfyJRdk", "Radio", "Canal", TimeSpan.Zero, null, ValidUrl, []);

        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(liveStream));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DurationText.Should().Be("Ao vivo");
    }

    /// <summary>
    /// O teto escolhido nas configuracoes decide o que ja vem marcado, sem tirar
    /// da lista o que o video oferece.
    /// </summary>
    [Fact]
    public async Task SearchCommand_WithAQualityCeiling_PreSelectsTheBestThatFitsInIt()
    {
        GivenSettings(new SettingsDto(MaximumHeight: 720));
        GivenSearchReturns([1080, 720, 480]);

        var viewModel = CreateViewModel();
        await viewModel.RefreshSettingsCommand.ExecuteAsync(null);
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.SelectedQuality!.Height.Should().Be(720);
        viewModel.AvailableQualities.Select(quality => quality.Height).Should().Equal(1080, 720, 480);
    }

    /// <summary>
    /// O teto e um limite, e nao uma exigencia: um video que so exista abaixo
    /// dele continua sendo baixado.
    /// </summary>
    [Fact]
    public async Task SearchCommand_WhenEveryQualityIsBelowTheCeiling_PreSelectsTheBestAvailable()
    {
        GivenSettings(new SettingsDto(MaximumHeight: 1080));
        GivenSearchReturns([480, 360]);

        var viewModel = CreateViewModel();
        await viewModel.RefreshSettingsCommand.ExecuteAsync(null);
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.SelectedQuality!.Height.Should().Be(480);
    }

    /// <summary>
    /// O campo mostra o nome que sera gravado, e nao uma promessa que o disco
    /// recusaria.
    /// </summary>
    [Fact]
    public async Task SearchCommand_SuggestsTheVideoTitleAlreadyCleanedAsTheFileName()
    {
        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(new VideoInfoDto(
                "UKcJqQqiXq0", @"AC/DC: ao vivo?", "Canal", TimeSpan.FromSeconds(96), null, ValidUrl, [1080])));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.FileName.Should().Be("ACDC ao vivo");
    }

    [Fact]
    public async Task DownloadCommand_UsesTheNameThatIsOnTheScreen()
    {
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync();
        viewModel.FileName = "Minha musica";

        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.FileName.Should().Be("Minha musica");
    }

    /// <summary>
    /// A extensao nao e digitada: ela e consequencia da escolha entre video e
    /// audio.
    /// </summary>
    [Fact]
    public async Task ExtensionText_FollowsTheChoiceBetweenVideoAndAudio()
    {
        var viewModel = await AfterSearchAsync();

        viewModel.ExtensionText.Should().Be(".mp4");

        viewModel.AudioOnly = true;

        viewModel.ExtensionText.Should().Be(".mp3");
    }

    [Fact]
    public async Task Destinations_OfferTheDefaultFolderFirstAndThenTheRecentOnes()
    {
        GivenRecentFolders(@"D:\Musicas\Elvis", @"D:\Musicas\Roberto");

        var viewModel = CreateViewModel();
        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.Destinations.Select(option => option.Path)
            .Should().Equal(null, @"D:\Musicas\Elvis", @"D:\Musicas\Roberto");

        viewModel.SelectedDestination.Should().Be(DestinationOption.Default);
        viewModel.Destinations[0].Label.Should().Be("Pasta padrão");
        viewModel.Destinations[1].Label.Should().Be("Elvis", because: "o caminho inteiro nao cabe na linha");
    }

    [Fact]
    public async Task DownloadCommand_WithoutChoosingAFolder_LetsTheDefaultOneDecide()
    {
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync();
        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.DestinationDirectory.Should().BeNull();
    }

    [Fact]
    public async Task DownloadCommand_UsesTheFolderChosenForThisDownload()
    {
        DownloadOptionsDto? options = null;
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile), captured => options = captured);

        var viewModel = await AfterSearchAsync();
        await viewModel.UseFolderAsync(@"D:\Musicas\Elvis");

        await viewModel.DownloadCommand.ExecuteAsync(null);

        options!.DestinationDirectory.Should().Be(@"D:\Musicas\Elvis");
    }

    /// <summary>
    /// O historico so registra downloads concluidos, entao a pasta recem-apontada
    /// ainda nao esta la. Sem entrar na lista, ela sumiria logo apos ser
    /// escolhida.
    /// </summary>
    [Fact]
    public async Task UseFolder_KeepsAFolderThatIsNotInTheHistoryYet()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeCommand.ExecuteAsync(null);

        await viewModel.UseFolderAsync(@"D:\Musicas\Elvis");

        viewModel.SelectedDestination.Path.Should().Be(@"D:\Musicas\Elvis");
        viewModel.Destinations.Select(option => option.Path).Should().Contain(@"D:\Musicas\Elvis");
    }

    /// <summary>
    /// Quem separa doze musicas em uma pasta escolheria a mesma doze vezes.
    /// </summary>
    [Fact]
    public async Task TheChosenFolder_SurvivesFromOneDownloadToTheNext()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = await AfterSearchAsync();
        await viewModel.UseFolderAsync(@"D:\Musicas\Elvis");

        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.SelectedDestination.Path.Should().Be(@"D:\Musicas\Elvis");
    }

    [Fact]
    public async Task SearchCommand_ClearsTheResultOfThePreviousDownload()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = await AfterSearchAsync();
        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.DownloadedFile.Should().NotBeNull();

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DownloadedFile.Should().BeNull();
    }
}
