using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class YtDlpVideoDownloaderTests : IDisposable
{
    private const string YtDlpPath = @"C:\app\tools\yt-dlp.exe";
    private const string FFmpegPath = @"C:\app\tools\ffmpeg.exe";

    private static readonly VideoUrl VideoUrl = VideoUrl.Create("https://youtu.be/UKcJqQqiXq0");

    private readonly string _destinationDirectory =
        Path.Combine(Path.GetTempPath(), $"ytdown-args-{Guid.NewGuid():N}");

    private readonly Mock<IProcessRunner> _processRunner = new();
    private readonly Mock<IToolLocator> _toolLocator = new();

    public YtDlpVideoDownloaderTests()
    {
        var ytDlp = YtDlpPath;
        var ffmpeg = FFmpegPath;

        _toolLocator.Setup(locator => locator.TryLocate(ExternalTool.YtDlp, out ytDlp!)).Returns(true);
        _toolLocator.Setup(locator => locator.TryLocate(ExternalTool.FFmpeg, out ffmpeg!)).Returns(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_destinationDirectory))
        {
            Directory.Delete(_destinationDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private async Task<IReadOnlyList<string>> CaptureArgumentsFor(DownloadOptionsDto options)
    {
        IReadOnlyList<string> arguments = [];

        _processRunner
            .Setup(runner => runner.RunAsync(
                It.IsAny<ProcessRequest>(),
                It.IsAny<Action<string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProcessRequest, Action<string>?, CancellationToken>(
                (request, _, _) => arguments = request.Arguments)
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var downloader = new YtDlpVideoDownloader(_processRunner.Object, _toolLocator.Object);

        await downloader.DownloadAsync(
            VideoUrl,
            options,
            _destinationDirectory,
            new Progress<DownloadProgressDto>(),
            CancellationToken.None);

        return arguments;
    }

    private static string FormatSelectorIn(IReadOnlyList<string> arguments) =>
        arguments[arguments.ToList().IndexOf("-f") + 1];

    [Fact]
    public async Task DownloadAsync_ForBestVideo_PrefersH264AndAskesForMp4()
    {
        var arguments = await CaptureArgumentsFor(DownloadOptionsDto.BestVideo);

        FormatSelectorIn(arguments).Should().StartWith("bv*[vcodec^=avc1]+ba[acodec^=mp4a]");
        arguments.Should().Contain("--merge-output-format").And.Contain("mp4");
        arguments.Should().NotContain("--extract-audio");
    }

    [Fact]
    public async Task DownloadAsync_WithAQualityLimit_AppliesItToEveryAlternative()
    {
        var arguments = await CaptureArgumentsFor(new DownloadOptionsDto(MediaKind.Video, MaximumHeight: 720));

        var selector = FormatSelectorIn(arguments);

        selector.Should().Contain("[height<=720]");
        // Se apenas a primeira alternativa fosse limitada, um video sem avc1 em
        // 720p cairia para a alternativa seguinte e baixaria em 1080p.
        selector.Split('/').Should().OnlyContain(alternative =>
            alternative.Contains("[height<=720]") || alternative == "b");
    }

    [Fact]
    public async Task DownloadAsync_ForAudioOnly_AsksForMp3AndIgnoresQuality()
    {
        var arguments = await CaptureArgumentsFor(DownloadOptionsDto.AudioOnly);

        FormatSelectorIn(arguments).Should().Be("ba/b");
        arguments.Should().Contain("--extract-audio");
        arguments.Should().Contain("mp3");
        arguments.Should().NotContain("--merge-output-format");
    }

    [Fact]
    public async Task DownloadAsync_KeepsIntermediateFilesOutOfTheDestination()
    {
        var arguments = await CaptureArgumentsFor(DownloadOptionsDto.BestVideo);

        arguments.Should().Contain(argument => argument.StartsWith($"home:{_destinationDirectory}"));
        arguments.Should().Contain(argument => argument.StartsWith("temp:"));
    }

    /// <summary>
    /// A pasta de trabalho e removida mesmo quando o download termina bem, para
    /// nao deixar nada visivel na pasta do usuario.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_LeavesNoWorkDirectoryBehind()
    {
        await CaptureArgumentsFor(DownloadOptionsDto.BestVideo);

        Directory.GetDirectories(_destinationDirectory).Should().BeEmpty();
    }
}
