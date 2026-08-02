using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;
using YTDown.Domain.ValueObjects;

namespace YTDown.UnitTests.Application.Services;

public class VideoInfoServiceTests
{
    private const string VideoId = "UKcJqQqiXq0";

    private readonly Mock<IVideoMetadataProvider> _metadataProvider = new(MockBehavior.Strict);

    private VideoInfoService CreateService() => new(_metadataProvider.Object);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("isso nao e uma url")]
    [InlineData("https://vimeo.com/123456789")]
    public async Task GetVideoInfoAsync_WithInvalidUrl_FailsWithoutCallingTheProvider(string? rawUrl)
    {
        var service = CreateService();

        var result = await service.GetVideoInfoAsync(rawUrl, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.InvalidUrl);
        _metadataProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetVideoInfoAsync_WithUrlCarryingPlaylistParameters_QueriesTheNormalizedVideo()
    {
        VideoUrl? received = null;
        var expected = CreateVideoInfo();

        _metadataProvider
            .Setup(provider => provider.GetMetadataAsync(It.IsAny<VideoUrl>(), It.IsAny<CancellationToken>()))
            .Callback<VideoUrl, CancellationToken>((videoUrl, _) => received = videoUrl)
            .ReturnsAsync(Result<VideoInfoDto>.Success(expected));

        var service = CreateService();

        var result = await service.GetVideoInfoAsync(
            $"https://www.youtube.com/watch?v={VideoId}&list=PLabcdefghij&t=42",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
        received.Should().NotBeNull();
        received!.VideoId.Should().Be(VideoId);
    }

    [Fact]
    public async Task GetVideoInfoAsync_WhenProviderFails_PropagatesTheFailureUnchanged()
    {
        _metadataProvider
            .Setup(provider => provider.GetMetadataAsync(It.IsAny<VideoUrl>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VideoInfoDto>.Failure(ErrorCode.VideoUnavailable, "detalhe tecnico"));

        var service = CreateService();

        var result = await service.GetVideoInfoAsync($"https://youtu.be/{VideoId}", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.VideoUnavailable);
        result.Diagnostics.Should().Be("detalhe tecnico");
    }

    [Fact]
    public async Task GetVideoInfoAsync_ForwardsTheCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();

        _metadataProvider
            .Setup(provider => provider.GetMetadataAsync(It.IsAny<VideoUrl>(), cancellation.Token))
            .ReturnsAsync(Result<VideoInfoDto>.Success(CreateVideoInfo()));

        var service = CreateService();

        await service.GetVideoInfoAsync($"https://youtu.be/{VideoId}", cancellation.Token);

        _metadataProvider.Verify(
            provider => provider.GetMetadataAsync(It.IsAny<VideoUrl>(), cancellation.Token),
            Times.Once);
    }

    private static VideoInfoDto CreateVideoInfo() => new(
        VideoId,
        "Titulo do video",
        "Canal de teste",
        TimeSpan.FromMinutes(3),
        "https://i.ytimg.com/vi/UKcJqQqiXq0/maxresdefault.jpg",
        $"https://www.youtube.com/watch?v={VideoId}");
}
