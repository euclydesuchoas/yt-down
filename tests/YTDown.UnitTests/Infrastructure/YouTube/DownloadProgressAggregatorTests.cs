using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class DownloadProgressAggregatorTests
{
    private static YtDlpProgressLine Video(long downloaded, long total = 100) =>
        new(IsVideoStream: true, IsFinished: downloaded >= total, downloaded, total, BytesPerSecond: 1024, TimeRemaining: TimeSpan.FromSeconds(5));

    private static YtDlpProgressLine Audio(long downloaded, long total = 100) =>
        new(IsVideoStream: false, IsFinished: downloaded >= total, downloaded, total, BytesPerSecond: 1024, TimeRemaining: TimeSpan.Zero);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 45)]
    [InlineData(100, 90)]
    public void ForStream_WithVideo_MapsOntoTheFirstNinetyPercent(long downloaded, int expected)
    {
        new DownloadProgressAggregator().ForStream(Video(downloaded)).Percentage.Should().Be(expected);
    }

    [Fact]
    public void ForStream_WithAudio_ContinuesWhereTheVideoStopped()
    {
        var aggregator = new DownloadProgressAggregator();
        aggregator.ForStream(Video(100));

        aggregator.ForStream(Audio(0)).Percentage.Should().Be(90);
        aggregator.ForStream(Audio(100)).Percentage.Should().Be(97);
    }

    /// <summary>
    /// Sem isto a barra iria a 100% e voltaria a zero quando o audio comecasse,
    /// que e exatamente o que faz o usuario achar que algo deu errado.
    /// </summary>
    [Fact]
    public void ForStream_NeverGoesBackwards()
    {
        var aggregator = new DownloadProgressAggregator();
        aggregator.ForStream(Video(100)).Percentage.Should().Be(90);

        var afterRestart = aggregator.ForStream(Audio(0));

        afterRestart.Percentage.Should().Be(90);
    }

    [Fact]
    public void ForStream_ReportsTheStageOfEachStream()
    {
        var aggregator = new DownloadProgressAggregator();

        aggregator.ForStream(Video(10)).Stage.Should().Be(DownloadStage.DownloadingVideo);
        aggregator.ForStream(Audio(10)).Stage.Should().Be(DownloadStage.DownloadingAudio);
    }

    [Fact]
    public void ForStream_CarriesSpeedAndRemainingTime()
    {
        var progress = new DownloadProgressAggregator().ForStream(Video(50));

        progress.BytesPerSecond.Should().Be(1024);
        progress.TimeRemaining.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ForMerging_HoldsAtNinetySevenWithoutSpeedOrEstimate()
    {
        var progress = new DownloadProgressAggregator().ForMerging();

        progress.Percentage.Should().Be(97);
        progress.Stage.Should().Be(DownloadStage.Merging);
        progress.BytesPerSecond.Should().BeNull();
        progress.TimeRemaining.Should().BeNull();
    }

    [Fact]
    public void ForCompletion_ReachesOneHundred()
    {
        var progress = new DownloadProgressAggregator().ForCompletion();

        progress.Percentage.Should().Be(100);
        progress.Stage.Should().Be(DownloadStage.Completed);
    }
}
