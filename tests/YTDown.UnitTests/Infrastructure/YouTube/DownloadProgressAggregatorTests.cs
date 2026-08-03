using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class DownloadProgressAggregatorTests
{
    private static YtDlpProgressLine Video(long downloaded, long total = 100) =>
        new(IsVideoStream: true, IsFinished: downloaded >= total, downloaded, total,
            BytesPerSecond: 1024, TimeRemaining: TimeSpan.FromSeconds(5));

    private static YtDlpProgressLine Audio(long downloaded, long total = 100) =>
        new(IsVideoStream: false, IsFinished: downloaded >= total, downloaded, total,
            BytesPerSecond: 1024, TimeRemaining: TimeSpan.FromSeconds(5));

    private static DownloadProgressAggregator ForVideo() => new(MediaKind.Video);

    private static DownloadProgressAggregator ForAudioOnly() => new(MediaKind.AudioOnly);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 45)]
    [InlineData(99, 89)]
    public void ForStream_WithVideo_MapsOntoTheFirstNinetyPercent(long downloaded, int expected)
    {
        ForVideo().ForStream(Video(downloaded)).Percentage.Should().Be(expected);
    }

    [Fact]
    public void ForStream_WithAudio_ContinuesWhereTheVideoStopped()
    {
        var aggregator = ForVideo();
        aggregator.ForStream(Video(99));

        aggregator.ForStream(Audio(0)).Percentage.Should().Be(90);
        aggregator.ForStream(Audio(99)).Percentage.Should().Be(96);
    }

    /// <summary>
    /// Uma barra que anda para tras faz o usuário achar que algo deu errado.
    /// O yt-dlp pode reportar menos bytes que antes ao retomar um stream.
    /// </summary>
    [Fact]
    public void ForStream_NeverGoesBackwards()
    {
        var aggregator = ForVideo();
        aggregator.ForStream(Video(50)).Percentage.Should().Be(45);

        aggregator.ForStream(Video(10)).Percentage.Should().Be(45);
    }

    [Fact]
    public void ForStream_ReportsTheStageOfEachStream()
    {
        var aggregator = ForVideo();

        aggregator.ForStream(Video(10)).Stage.Should().Be(DownloadStage.DownloadingVideo);
        aggregator.ForStream(Audio(10)).Stage.Should().Be(DownloadStage.DownloadingAudio);
    }

    [Fact]
    public void ForStream_CarriesSpeedAndRemainingTime()
    {
        var progress = ForVideo().ForStream(Video(50));

        progress.BytesPerSecond.Should().Be(1024);
        progress.TimeRemaining.Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// O último stream terminar é o único aviso de que o FFmpeg começou: as
    /// mensagens dos pos-processadores não chegam, porque --print implica
    /// --quiet.
    /// </summary>
    [Fact]
    public void ForStream_WhenTheLastStreamFinishes_EntersTheFinishingStage()
    {
        var aggregator = ForVideo();
        aggregator.ForStream(Video(50));

        var progress = aggregator.ForStream(Audio(100));

        progress.Stage.Should().Be(DownloadStage.Finishing);
        progress.Percentage.Should().Be(97);
        progress.BytesPerSecond.Should().BeNull();
        progress.TimeRemaining.Should().BeNull();
    }

    /// <summary>
    /// O vídeo terminar não significa que acabou: o áudio ainda vem depois.
    /// </summary>
    [Fact]
    public void ForStream_WhenOnlyTheVideoStreamFinishes_StaysDownloading()
    {
        ForVideo().ForStream(Video(100)).Stage.Should().Be(DownloadStage.DownloadingVideo);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 47)]
    [InlineData(99, 94)]
    public void ForStream_WhenOnlyAudioWasAsked_TheSingleStreamFillsNearlyEverything(long downloaded, int expected)
    {
        ForAudioOnly().ForStream(Audio(downloaded)).Percentage.Should().Be(expected);
    }

    [Fact]
    public void ForStream_WhenOnlyAudioWasAsked_TheStreamFinishingMeansConversionStarted()
    {
        var progress = ForAudioOnly().ForStream(Audio(100));

        progress.Stage.Should().Be(DownloadStage.Finishing);
        progress.Percentage.Should().Be(95);
    }

    [Fact]
    public void ForCompletion_ReachesOneHundred()
    {
        var progress = ForVideo().ForCompletion();

        progress.Percentage.Should().Be(100);
        progress.Stage.Should().Be(DownloadStage.Completed);
    }
}
