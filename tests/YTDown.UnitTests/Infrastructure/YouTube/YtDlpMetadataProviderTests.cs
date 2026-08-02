using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class YtDlpMetadataProviderTests
{
    private const string YtDlpPath = @"C:\app\tools\yt-dlp.exe";
    private const string VideoId = "UKcJqQqiXq0";

    private static readonly VideoUrl VideoUrl = VideoUrl.Create($"https://youtu.be/{VideoId}?si=AbCdEfGhIjKl");

    private readonly Mock<IProcessRunner> _processRunner = new(MockBehavior.Strict);
    private readonly Mock<IToolLocator> _toolLocator = new(MockBehavior.Strict);

    private YtDlpMetadataProvider CreateProvider() => new(_processRunner.Object, _toolLocator.Object);

    private void GivenYtDlpIsInstalled()
    {
        var path = YtDlpPath;

        _toolLocator
            .Setup(locator => locator.TryLocate(ExternalTool.YtDlp, out path!))
            .Returns(true);
    }

    private void GivenYtDlpProduces(ProcessResult processResult, Action<IReadOnlyList<string>>? captureArguments = null)
    {
        _processRunner
            .Setup(runner => runner.RunAsync(YtDlpPath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<string>, CancellationToken>((_, arguments, _) => captureArguments?.Invoke(arguments))
            .ReturnsAsync(processResult);
    }

    private static string SuccessfulJson => """
        { "id": "UKcJqQqiXq0", "title": "Titulo", "channel": "Canal", "duration": 96 }
        """;

    [Fact]
    public async Task GetMetadataAsync_WhenYtDlpIsMissing_FailsWithoutRunningAnything()
    {
        string? path = null;

        _toolLocator
            .Setup(locator => locator.TryLocate(ExternalTool.YtDlp, out path))
            .Returns(false);

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.ToolNotFound);
        _processRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMetadataAsync_WithSuccessfulRun_ReturnsTheParsedVideo()
    {
        GivenYtDlpIsInstalled();
        GivenYtDlpProduces(new ProcessResult(0, SuccessfulJson, string.Empty));

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VideoId.Should().Be(VideoId);
        result.Value.Duration.Should().Be(TimeSpan.FromSeconds(96));
    }

    [Fact]
    public async Task GetMetadataAsync_AsksForJsonOfASingleVideoUsingTheCanonicalUrl()
    {
        IReadOnlyList<string>? arguments = null;

        GivenYtDlpIsInstalled();
        GivenYtDlpProduces(new ProcessResult(0, SuccessfulJson, string.Empty), captured => arguments = captured);

        await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        arguments.Should().NotBeNull();
        arguments.Should().Contain("--dump-single-json");
        arguments.Should().Contain("--no-playlist");
        arguments!.Last().Should().Be($"https://www.youtube.com/watch?v={VideoId}");
    }

    [Fact]
    public async Task GetMetadataAsync_WhenYtDlpFails_ClassifiesTheErrorAndKeepsTheOutputForDiagnostics()
    {
        const string standardError = "ERROR: [youtube] UKcJqQqiXq0: Private video. Sign in if you've been granted access";

        GivenYtDlpIsInstalled();
        GivenYtDlpProduces(new ProcessResult(1, string.Empty, standardError));

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.VideoUnavailable);
        result.Diagnostics.Should().Be(standardError);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenOutputCannotBeParsed_ReturnsToolFailure()
    {
        GivenYtDlpIsInstalled();
        GivenYtDlpProduces(new ProcessResult(0, "isso nao e json", string.Empty));

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.ToolFailure);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenCancelled_ReturnsCanceled()
    {
        GivenYtDlpIsInstalled();

        _processRunner
            .Setup(runner => runner.RunAsync(YtDlpPath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Canceled);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenTheProcessCannotStart_ReturnsUnexpectedInsteadOfCrashing()
    {
        GivenYtDlpIsInstalled();

        _processRunner
            .Setup(runner => runner.RunAsync(YtDlpPath, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("acesso negado"));

        var result = await CreateProvider().GetMetadataAsync(VideoUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Unexpected);
        result.Diagnostics.Should().Contain("acesso negado");
    }
}
