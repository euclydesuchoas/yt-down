using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class YtDlpErrorClassifierTests
{
    [Theory]
    [InlineData("ERROR: [youtube] abc: Video unavailable")]
    [InlineData("ERROR: [youtube] abc: Private video. Sign in if you've been granted access to this video")]
    [InlineData("ERROR: [youtube] abc: This video has been removed by the uploader")]
    [InlineData("ERROR: [youtube] abc: This video is no longer available because the account has been terminated")]
    public void Classify_WithUnavailableVideo_ReturnsVideoUnavailable(string standardError)
    {
        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.VideoUnavailable);
    }

    [Theory]
    [InlineData("ERROR: [youtube] abc: Sign in to confirm your age. This video may be inappropriate for some users.")]
    [InlineData("ERROR: [youtube] abc: This video is age-restricted and can't be watched anonymously")]
    public void Classify_WithAgeRestriction_ReturnsAgeRestricted(string standardError)
    {
        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.AgeRestricted);
    }

    /// <summary>
    /// A mensagem de bloqueio regional também contém "Vídeo unavailable", então
    /// só é classificada corretamente por causa da ordem das regras.
    /// </summary>
    [Theory]
    [InlineData("ERROR: [youtube] abc: Video unavailable\nThe uploader has not made this video available in your country")]
    [InlineData("ERROR: [youtube] abc: This video is not available from your location")]
    public void Classify_WithRegionBlock_ReturnsRegionBlocked(string standardError)
    {
        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.RegionBlocked);
    }

    /// <summary>
    /// Mensagem real, capturada após vários downloads seguidos do mesmo endereço
    /// de rede. Note o apóstrofo tipográfico, que o marcador evita de propósito.
    /// </summary>
    [Fact]
    public void Classify_WhenYouTubeAsksForVerification_ReturnsBotCheckRequired()
    {
        const string standardError =
            "ERROR: [youtube] UKcJqQqiXq0: Sign in to confirm you’re not a bot. " +
            "Use --cookies-from-browser or --cookies for the authentication.";

        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.BotCheckRequired);
    }

    /// <summary>
    /// As duas mensagens começam igual: a de idade não pode ser confundida com a
    /// de verificação.
    /// </summary>
    [Fact]
    public void Classify_DistinguishesAgeRestrictionFromTheBotCheck()
    {
        YtDlpErrorClassifier
            .Classify("ERROR: [youtube] abc: Sign in to confirm your age. This video may be inappropriate.")
            .Should().Be(ErrorCode.AgeRestricted);
    }

    [Theory]
    [InlineData("ERROR: Unable to download webpage: <urlopen error [Errno 11001] getaddrinfo failed>")]
    [InlineData("ERROR: Unable to download API page: The read operation timed out")]
    [InlineData("ERROR: [Errno 101] Network is unreachable")]
    public void Classify_WithNetworkProblem_ReturnsNetworkError(string standardError)
    {
        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.NetworkError);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ERROR: algo totalmente novo que ainda não sabemos classificar")]
    public void Classify_WithUnrecognizedOutput_ReturnsToolFailure(string? standardError)
    {
        YtDlpErrorClassifier.Classify(standardError).Should().Be(ErrorCode.ToolFailure);
    }

    [Fact]
    public void Classify_IsCaseInsensitive()
    {
        YtDlpErrorClassifier.Classify("ERROR: VIDEO UNAVAILABLE").Should().Be(ErrorCode.VideoUnavailable);
    }
}
