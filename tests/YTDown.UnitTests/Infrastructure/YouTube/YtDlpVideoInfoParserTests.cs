using FluentAssertions;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class YtDlpVideoInfoParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "YouTube", "Fixtures", fileName));

    [Fact]
    public void TryParse_WithRealResponse_ReadsEveryField()
    {
        var parsed = YtDlpVideoInfoParser.TryParse(ReadFixture("video-info.json"), out var videoInfo);

        parsed.Should().BeTrue();
        videoInfo!.VideoId.Should().Be("UKcJqQqiXq0");
        videoInfo.ChannelName.Should().Be("TOHO animation");
        videoInfo.Duration.Should().Be(TimeSpan.FromSeconds(96));
        videoInfo.ThumbnailUrl.Should().Be("https://i.ytimg.com/vi/UKcJqQqiXq0/maxresdefault.jpg");
        videoInfo.Url.Should().Be("https://www.youtube.com/watch?v=UKcJqQqiXq0");
    }

    /// <summary>
    /// O titulo real do video de referencia e japones: se a leitura perder a
    /// codificacao em qualquer ponto, este teste falha.
    /// </summary>
    [Fact]
    public void TryParse_WithNonLatinTitle_PreservesTheCharacters()
    {
        var parsed = YtDlpVideoInfoParser.TryParse(ReadFixture("video-info.json"), out var videoInfo);

        parsed.Should().BeTrue();
        videoInfo!.Title.Should().Be(
            "『無職転生Ⅲ ～異世界行ったら本気だす～』ノンクレジットED映像／EDテーマ：「祈り、終われば」中島美嘉");
    }

    [Fact]
    public void TryParse_WithLiveStream_TreatsMissingDurationAsZero()
    {
        var parsed = YtDlpVideoInfoParser.TryParse(ReadFixture("live-stream.json"), out var videoInfo);

        parsed.Should().BeTrue();
        videoInfo!.Duration.Should().Be(TimeSpan.Zero);
        videoInfo.Title.Should().Be("lofi hip hop radio");
    }

    [Fact]
    public void TryParse_WithoutChannelField_FallsBackToUploader()
    {
        var parsed = YtDlpVideoInfoParser.TryParse(ReadFixture("legacy-uploader-only.json"), out var videoInfo);

        parsed.Should().BeTrue();
        videoInfo!.ChannelName.Should().Be("TOHO animation");
    }

    [Fact]
    public void TryParse_WithoutWebpageUrl_BuildsTheCanonicalUrl()
    {
        var parsed = YtDlpVideoInfoParser.TryParse(ReadFixture("legacy-uploader-only.json"), out var videoInfo);

        parsed.Should().BeTrue();
        videoInfo!.Url.Should().Be("https://www.youtube.com/watch?v=UKcJqQqiXq0");
        videoInfo.ThumbnailUrl.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao e json")]
    [InlineData("{ \"id\": \"UKcJqQqiXq0\" ")]
    [InlineData("[]")]
    [InlineData("\"apenas um texto\"")]
    public void TryParse_WithOutputThatIsNotAJsonObject_Fails(string? json)
    {
        var parsed = YtDlpVideoInfoParser.TryParse(json, out var videoInfo);

        parsed.Should().BeFalse();
        videoInfo.Should().BeNull();
    }

    [Theory]
    [InlineData("{ \"title\": \"sem identificador\" }")]
    [InlineData("{ \"id\": \"UKcJqQqiXq0\" }")]
    [InlineData("{ \"id\": \"\", \"title\": \"identificador vazio\" }")]
    public void TryParse_WithoutTheEssentialFields_Fails(string json)
    {
        var parsed = YtDlpVideoInfoParser.TryParse(json, out var videoInfo);

        parsed.Should().BeFalse();
        videoInfo.Should().BeNull();
    }
}
