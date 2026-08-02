using FluentAssertions;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

/// <summary>
/// As linhas usadas aqui foram capturadas de um download real do video de
/// referencia, e nao inventadas.
/// </summary>
public class YtDlpProgressParserTests
{
    [Fact]
    public void TryParse_WithVideoStreamLine_ReadsEveryField()
    {
        const string line = "PROG|avc1.640028|downloading|1024|18637959|512037.11206485453|36";

        var parsed = YtDlpProgressParser.TryParse(line, out var progress);

        parsed.Should().BeTrue();
        progress!.IsVideoStream.Should().BeTrue();
        progress.IsFinished.Should().BeFalse();
        progress.DownloadedBytes.Should().Be(1024);
        progress.TotalBytes.Should().Be(18637959);
        progress.BytesPerSecond.Should().BeApproximately(512037.11, 0.01);
        progress.TimeRemaining.Should().Be(TimeSpan.FromSeconds(36));
    }

    /// <summary>
    /// Enquanto baixa o audio o yt-dlp informa o codec de video como "none": e
    /// so por isso os dois streams podem ser distinguidos.
    /// </summary>
    [Fact]
    public void TryParse_WithAudioStreamLine_MarksItAsNotVideo()
    {
        const string line = "PROG|none|downloading|1024|1556940|1023586.1048617731|1";

        var parsed = YtDlpProgressParser.TryParse(line, out var progress);

        parsed.Should().BeTrue();
        progress!.IsVideoStream.Should().BeFalse();
        progress.TotalBytes.Should().Be(1556940);
    }

    [Fact]
    public void TryParse_WithFinishedLine_MarksItAsFinishedAndAcceptsUnknownEta()
    {
        const string line = "PROG|avc1.640028|finished|18637959|18637959|25121550.505811907|NA";

        var parsed = YtDlpProgressParser.TryParse(line, out var progress);

        parsed.Should().BeTrue();
        progress!.IsFinished.Should().BeTrue();
        progress.TimeRemaining.Should().BeNull();
    }

    /// <summary>
    /// Sem total nao ha percentual possivel, e uma barra inventada engana mais
    /// do que a ausencia dela.
    /// </summary>
    [Theory]
    [InlineData("PROG|avc1.640028|downloading|1024|NA|512037.11|36")]
    [InlineData("PROG|avc1.640028|downloading|1024|0|512037.11|36")]
    public void TryParse_WithoutTotalSize_Fails(string line)
    {
        YtDlpProgressParser.TryParse(line, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[download] Destination: C:\\videos\\video.f137.mp4")]
    [InlineData("[youtube] UKcJqQqiXq0: Downloading webpage")]
    [InlineData("PROG|avc1|downloading|1024")]
    public void TryParse_WithLineThatIsNotProgress_Fails(string? line)
    {
        YtDlpProgressParser.TryParse(line, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParsePath_WithFinalFileLine_ReturnsThePath()
    {
        const string line = @"FINAL|C:\Users\Euclydes\Downloads\video.mp4";

        var parsed = YtDlpProgressParser.TryParsePath(line, YtDlpProgressParser.FinalFilePrefix, out var path);

        parsed.Should().BeTrue();
        path.Should().Be(@"C:\Users\Euclydes\Downloads\video.mp4");
    }

    [Fact]
    public void TryParsePath_WithDestinationLine_ReturnsTheTemporaryFile()
    {
        const string line = @"[download] Destination: C:\Users\Euclydes\Downloads\video.f137.mp4";

        var parsed = YtDlpProgressParser.TryParsePath(line, YtDlpProgressParser.DestinationPrefix, out var path);

        parsed.Should().BeTrue();
        path.Should().Be(@"C:\Users\Euclydes\Downloads\video.f137.mp4");
    }

    [Theory]
    [InlineData("FINAL|")]
    [InlineData("FINAL|   ")]
    [InlineData("outra coisa")]
    public void TryParsePath_WithoutAPath_Fails(string line)
    {
        YtDlpProgressParser.TryParsePath(line, YtDlpProgressParser.FinalFilePrefix, out _).Should().BeFalse();
    }
}
