using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;
using YTDown.Domain.ValueObjects;

namespace YTDown.UnitTests.Application.Services;

public class DownloadServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 14, 30, 0, TimeSpan.Zero);

    private const string CanonicalUrl = "https://www.youtube.com/watch?v=UKcJqQqiXq0";

    private static readonly DownloadedFileDto AnyFile =
        new(@"C:\Users\Euclydes\Downloads\video.mp4", "video.mp4", 20_000_000);

    private readonly Mock<IVideoDownloader> _videoDownloader = new();
    private readonly Mock<IDownloadLocationProvider> _locationProvider = new();
    private readonly Mock<IDownloadHistoryService> _history = new();

    public DownloadServiceTests() =>
        _locationProvider
            .Setup(provider => provider.GetDestinationDirectory())
            .Returns(@"C:\Users\Euclydes\Downloads");

    private DownloadService CreateService() =>
        new(_videoDownloader.Object, _locationProvider.Object, _history.Object, new FixedTimeProvider(Now));

    private void GivenDownloadReturns(Result<DownloadedFileDto> result) =>
        _videoDownloader
            .Setup(downloader => downloader.DownloadAsync(
                It.IsAny<VideoUrl>(),
                It.IsAny<DownloadOptionsDto>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<DownloadProgressDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private Task<Result<DownloadedFileDto>> DownloadAsync(
        string? url = CanonicalUrl,
        DownloadOptionsDto? options = null) =>
        CreateService().DownloadAsync(
            url,
            options ?? DownloadOptionsDto.BestVideo,
            new Progress<DownloadProgressDto>(),
            CancellationToken.None);

    /// <summary>
    /// Entrada invalida falha aqui, antes de iniciar qualquer processo externo.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WithSomethingThatIsNotAVideoAddress_FailsWithoutDownloading()
    {
        var result = await DownloadAsync("nao e um endereco");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.InvalidUrl);

        _videoDownloader.VerifyNoOtherCalls();
        _history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadAsync_WhenItSucceeds_RecordsTheDownloadInTheHistory()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyFile));

        await DownloadAsync(options: DownloadOptionsDto.AudioOnly);

        _history.Verify(
            history => history.RecordAsync(
                It.Is<DownloadHistoryEntryDto>(entry =>
                    entry.Url == CanonicalUrl
                    && entry.FileName == AnyFile.FileName
                    && entry.FilePath == AnyFile.FilePath
                    && entry.SizeInBytes == AnyFile.SizeInBytes
                    && entry.Kind == MediaKind.AudioOnly
                    && entry.CompletedAt == Now),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// O historico guarda o endereco normalizado, e nao o que o usuario colou:
    /// e ele que serve para baixar de novo.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_RecordsTheNormalizedAddress()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Success(AnyFile));

        await DownloadAsync("https://youtu.be/UKcJqQqiXq0?si=abc&t=42");

        _history.Verify(
            history => history.RecordAsync(
                It.Is<DownloadHistoryEntryDto>(entry => entry.Url == CanonicalUrl),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_WhenItFails_RecordsNothing()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Failure(ErrorCode.VideoUnavailable));

        var result = await DownloadAsync();

        result.IsSuccess.Should().BeFalse();
        _history.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Cancelar apaga tudo o que foi baixado: nao ha arquivo para lembrar.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WhenCancelled_RecordsNothing()
    {
        GivenDownloadReturns(Result<DownloadedFileDto>.Failure(ErrorCode.Canceled));

        await DownloadAsync();

        _history.VerifyNoOtherCalls();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
