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

    private MainViewModel CreateViewModel() =>
        new(_videoInfoService.Object, _downloadService.Object, _fileExplorer.Object);

    private static DownloadedFileDto AnyDownloadedFile =>
        new(@"C:\Users\Euclydes\Downloads\video.mp4", "video.mp4", 20_000_000);

    private void GivenDownloadReturns(Result<DownloadedFileDto> result) =>
        _downloadService
            .Setup(service => service.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<IProgress<DownloadProgressDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public void Commands_AreDisabledWhileTheAddressIsEmpty()
    {
        var viewModel = CreateViewModel();

        viewModel.SearchCommand.CanExecute(null).Should().BeFalse();
        viewModel.DownloadCommand.CanExecute(null).Should().BeFalse();

        viewModel.Url = ValidUrl;

        viewModel.SearchCommand.CanExecute(null).Should().BeTrue();
        viewModel.DownloadCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadCommand_WhenItSucceeds_KeepsTheFileAndEnablesOpeningTheFolder()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

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

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

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

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().BeNull();
        viewModel.DownloadedFile.Should().BeNull();
    }

    [Fact]
    public async Task OpenContainingFolderCommand_RevealsTheDownloadedFile()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;
        await viewModel.DownloadCommand.ExecuteAsync(null);

        viewModel.OpenContainingFolderCommand.Execute(null);

        _fileExplorer.Verify(explorer => explorer.RevealFile(AnyDownloadedFile.FilePath), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_WhenItSucceeds_ShowsTheVideoWithAReadableDuration()
    {
        var video = new VideoInfoDto("UKcJqQqiXq0", "Titulo", "Canal", TimeSpan.FromSeconds(96), null, ValidUrl);

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
    public async Task SearchCommand_WithALiveStream_ShowsAoVivoInsteadOfZero()
    {
        var liveStream = new VideoInfoDto("jfKfPfyJRdk", "Radio", "Canal", TimeSpan.Zero, null, ValidUrl);

        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Success(liveStream));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DurationText.Should().Be("Ao vivo");
    }

    [Fact]
    public async Task SearchCommand_ClearsTheResultOfThePreviousDownload()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyDownloadedFile));

        _videoInfoService
            .Setup(service => service.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Failure(ErrorCode.InvalidUrl));

        var viewModel = CreateViewModel();
        viewModel.Url = ValidUrl;
        await viewModel.DownloadCommand.ExecuteAsync(null);

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.DownloadedFile.Should().BeNull();
    }
}
