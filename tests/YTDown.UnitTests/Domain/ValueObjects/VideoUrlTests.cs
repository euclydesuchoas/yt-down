using FluentAssertions;
using YTDown.Domain.Exceptions;
using YTDown.Domain.ValueObjects;

namespace YTDown.UnitTests.Domain.ValueObjects;

public class VideoUrlTests
{
    /// <summary>Vídeo de referência usado nos testes do projeto.</summary>
    private const string VideoId = "UKcJqQqiXq0";

    private const string CanonicalUrl = $"https://www.youtube.com/watch?v={VideoId}";

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("https://youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("http://www.youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("https://m.youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("https://music.youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("https://youtu.be/UKcJqQqiXq0")]
    [InlineData("https://www.youtube.com/shorts/UKcJqQqiXq0")]
    [InlineData("https://www.youtube.com/live/UKcJqQqiXq0")]
    [InlineData("https://www.youtube.com/embed/UKcJqQqiXq0")]
    [InlineData("https://www.youtube.com/v/UKcJqQqiXq0")]
    public void TryCreate_WithSupportedFormat_ExtractsVideoId(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeTrue();
        videoUrl!.VideoId.Should().Be(VideoId);
    }

    [Theory]
    [InlineData("www.youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("youtube.com/watch?v=UKcJqQqiXq0")]
    [InlineData("youtu.be/UKcJqQqiXq0")]
    public void TryCreate_WithoutScheme_ExtractsVideoId(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeTrue();
        videoUrl!.VideoId.Should().Be(VideoId);
    }

    [Theory]
    [InlineData("HTTPS://WWW.YOUTUBE.COM/watch?v=UKcJqQqiXq0")]
    [InlineData("   https://www.youtube.com/watch?v=UKcJqQqiXq0   ")]
    [InlineData("https://www.youtube.com/SHORTS/UKcJqQqiXq0")]
    public void TryCreate_WithCasingOrSurroundingWhitespace_ExtractsVideoId(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeTrue();
        videoUrl!.VideoId.Should().Be(VideoId);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=UKcJqQqiXq0&list=PLabcdefghij")]
    [InlineData("https://www.youtube.com/watch?list=PLabcdefghij&v=UKcJqQqiXq0&index=3")]
    [InlineData("https://www.youtube.com/watch?v=UKcJqQqiXq0&t=90s&pp=ygUFdGVzdGU")]
    [InlineData("https://youtu.be/UKcJqQqiXq0?si=AbCdEfGhIjKlMnOp")]
    [InlineData("https://youtu.be/UKcJqQqiXq0?t=42")]
    public void TryCreate_WithPlaylistTimeOrTrackingParameters_DiscardsThem(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeTrue();
        videoUrl!.Value.Should().Be(CanonicalUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("isso não é uma url")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://www.dailymotion.com/video/UKcJqQqiXq0")]
    [InlineData("ftp://www.youtube.com/watch?v=UKcJqQqiXq0")]
    public void TryCreate_WithInputThatIsNotAYouTubeVideo_Fails(string? input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeFalse();
        videoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLabcdefghij")]
    [InlineData("https://www.youtube.com/@algumcanal")]
    [InlineData("https://www.youtube.com/watch")]
    [InlineData("https://www.youtube.com/watch?v=")]
    public void TryCreate_WithYouTubeUrlThatIdentifiesNoVideo_Fails(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeFalse();
        videoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=curto")]
    [InlineData("https://www.youtube.com/watch?v=UKcJqQqiXq0DEMAIS")]
    [InlineData("https://www.youtube.com/watch?v=UKcJqQqiX!0")]
    public void TryCreate_WithMalformedVideoId_Fails(string input)
    {
        var succeeded = VideoUrl.TryCreate(input, out var videoUrl);

        succeeded.Should().BeFalse();
        videoUrl.Should().BeNull();
    }

    /// <summary>
    /// Um identificador solto é recusado de propósito: qualquer palavra de 11
    /// caracteres válidos passaria na verificação, e o erro só apareceria bem
    /// mais tarde, como uma falha confusa do yt-dlp.
    /// </summary>
    [Fact]
    public void TryCreate_WithBareVideoId_Fails()
    {
        var succeeded = VideoUrl.TryCreate(VideoId, out var videoUrl);

        succeeded.Should().BeFalse();
        videoUrl.Should().BeNull();
    }

    [Fact]
    public void Value_AlwaysUsesTheCanonicalWatchUrl()
    {
        var videoUrl = VideoUrl.Create("https://youtu.be/UKcJqQqiXq0?si=AbCdEfGhIjKlMnOp");

        videoUrl.Value.Should().Be(CanonicalUrl);
        videoUrl.ToString().Should().Be(CanonicalUrl);
    }

    [Fact]
    public void Equals_WithSameVideoInDifferentFormats_ReturnsTrue()
    {
        var fromWatch = VideoUrl.Create("https://www.youtube.com/watch?v=UKcJqQqiXq0&list=PLabcdefghij");
        var fromShortLink = VideoUrl.Create("https://youtu.be/UKcJqQqiXq0?t=42");

        fromShortLink.Should().Be(fromWatch);
        fromShortLink.GetHashCode().Should().Be(fromWatch.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentVideos_ReturnsFalse()
    {
        var first = VideoUrl.Create("https://www.youtube.com/watch?v=UKcJqQqiXq0");
        var second = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Create_WithInvalidInput_ThrowsCarryingTheOriginalInput()
    {
        const string input = "https://vimeo.com/123456789";

        var act = () => VideoUrl.Create(input);

        act.Should().Throw<InvalidVideoUrlException>()
            .Which.Candidate.Should().Be(input);
    }
}
