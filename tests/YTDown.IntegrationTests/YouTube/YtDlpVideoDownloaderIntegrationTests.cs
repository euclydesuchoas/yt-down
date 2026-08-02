using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;
using YTDown.Infrastructure.YouTube;

namespace YTDown.IntegrationTests.YouTube;

/// <summary>
/// Baixa o video de referencia de verdade, com o yt-dlp e o FFmpeg reais.
/// </summary>
[Trait("Category", "Integration")]
public class YtDlpVideoDownloaderIntegrationTests : IDisposable
{
    private const string ReferenceVideoUrl = "https://www.youtube.com/watch?v=UKcJqQqiXq0";

    private readonly string _destinationDirectory =
        Path.Combine(Path.GetTempPath(), $"ytdown-download-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_destinationDirectory))
        {
            Directory.Delete(_destinationDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static YtDlpVideoDownloader CreateDownloader() =>
        new(new ProcessRunner(), new LocalToolLocator(FindToolsDirectory()));

    private static string FindToolsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "tools");

            if (File.Exists(Path.Combine(candidate, "yt-dlp.exe")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Pasta tools nao encontrada. Execute scripts/bootstrap-tools.ps1 antes dos testes de integracao.");
    }

    [Fact]
    public async Task DownloadAsync_ForTheReferenceVideo_ProducesAnMp4AndReachesOneHundred()
    {
        var reported = new List<DownloadProgressDto>();
        var progress = new SynchronousProgress(reported.Add);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var result = await CreateDownloader().DownloadAsync(
            VideoUrl.Create(ReferenceVideoUrl),
            DownloadOptionsDto.BestVideo,
            _destinationDirectory,
            progress,
            cancellation.Token);

        result.IsSuccess.Should().BeTrue(because: result.Diagnostics);
        result.Value!.FilePath.Should().EndWith(".mp4");
        File.Exists(result.Value.FilePath).Should().BeTrue();
        result.Value.SizeInBytes.Should().BeGreaterThan(1_000_000);

        reported.Should().NotBeEmpty();
        reported.Select(entry => entry.Percentage).Should().BeInAscendingOrder(
            because: "a barra nunca pode andar para tras");
        reported[^1].Percentage.Should().Be(100);
        reported.Select(entry => entry.Stage).Should().Contain(DownloadStage.DownloadingVideo);
        reported.Select(entry => entry.Stage).Should().Contain(DownloadStage.Completed);

        // O yt-dlp remove os proprios arquivos parciais quando termina bem.
        Directory.GetFiles(_destinationDirectory).Should().ContainSingle();
    }

    [Fact]
    public async Task DownloadAsync_ForAudioOnly_ProducesAnMp3AndPassesThroughTheFinishingStage()
    {
        var reported = new List<DownloadProgressDto>();
        var progress = new SynchronousProgress(reported.Add);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var result = await CreateDownloader().DownloadAsync(
            VideoUrl.Create(ReferenceVideoUrl),
            DownloadOptionsDto.AudioOnly,
            _destinationDirectory,
            progress,
            cancellation.Token);

        result.IsSuccess.Should().BeTrue(because: result.Diagnostics);
        result.Value!.FilePath.Should().EndWith(".mp3");
        File.Exists(result.Value.FilePath).Should().BeTrue();

        reported.Select(entry => entry.Percentage).Should().BeInAscendingOrder();
        // A conversao para MP3 nao reporta progresso, entao esta etapa so existe
        // porque o fim do stream de audio e usado como marcador.
        reported.Select(entry => entry.Stage).Should().Contain(DownloadStage.Finishing);
        reported[^1].Percentage.Should().Be(100);

        Directory.GetFiles(_destinationDirectory).Should().ContainSingle();
    }

    [Fact]
    public async Task DownloadAsync_WithAQualityLimit_RespectsIt()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var limited = await CreateDownloader().DownloadAsync(
            VideoUrl.Create(ReferenceVideoUrl),
            new DownloadOptionsDto(MediaKind.Video, MaximumHeight: 360),
            _destinationDirectory,
            new Progress<DownloadProgressDto>(),
            cancellation.Token);

        limited.IsSuccess.Should().BeTrue(because: limited.Diagnostics);

        // O mesmo video em 1080p passa de 20 MB; em 360p fica bem abaixo disso.
        limited.Value!.SizeInBytes.Should().BeLessThan(8_000_000);
    }

    [Fact]
    public async Task DownloadAsync_WhenCancelledMidway_StopsAndLeavesNoPartialFile()
    {
        using var cancellation = new CancellationTokenSource();

        var progress = new SynchronousProgress(entry =>
        {
            if (entry.Percentage >= 1)
            {
                cancellation.Cancel();
            }
        });

        var result = await CreateDownloader().DownloadAsync(
            VideoUrl.Create(ReferenceVideoUrl),
            DownloadOptionsDto.BestVideo,
            _destinationDirectory,
            progress,
            cancellation.Token);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Canceled);

        Directory.GetFiles(_destinationDirectory).Should().BeEmpty(
            because: "arquivos parciais precisam ser removidos ao cancelar");
    }

    /// <summary>
    /// Entrega no mesmo instante e na mesma ordem, ao contrario de Progress,
    /// que reagenda cada notificacao.
    /// </summary>
    private sealed class SynchronousProgress : IProgress<DownloadProgressDto>
    {
        private readonly Action<DownloadProgressDto> _onReport;

        public SynchronousProgress(Action<DownloadProgressDto> onReport) => _onReport = onReport;

        public void Report(DownloadProgressDto value) => _onReport(value);
    }
}
