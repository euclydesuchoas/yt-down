using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;
using YTDown.Infrastructure.YouTube;

namespace YTDown.IntegrationTests.YouTube;

/// <summary>
/// Exercita o yt-dlp de verdade, contra o YouTube de verdade.
/// </summary>
/// <remarks>
/// Exige rede e as ferramentas baixadas por scripts/bootstrap-tools.ps1.
/// Marcados com a categoria Integration para poderem ser excluidos quando
/// nao houver rede disponivel.
/// </remarks>
[Trait("Category", "Integration")]
public class YtDlpMetadataProviderIntegrationTests
{
    private const string ReferenceVideoId = "UKcJqQqiXq0";

    private static YtDlpMetadataProvider CreateProvider() =>
        new(new ProcessRunner(), new LocalToolLocator(FindToolsDirectory()));

    /// <summary>
    /// A pasta tools fica na raiz do repositorio, nao junto do binario de teste.
    /// </summary>
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
    public async Task GetMetadataAsync_ForTheReferenceVideo_ReturnsRealData()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var videoUrl = VideoUrl.Create($"https://www.youtube.com/watch?v={ReferenceVideoId}");

        var result = await CreateProvider().GetMetadataAsync(videoUrl, cancellation.Token);

        result.IsSuccess.Should().BeTrue(because: result.Diagnostics);
        result.Value!.VideoId.Should().Be(ReferenceVideoId);
        result.Value.ChannelName.Should().Be("TOHO animation");
        result.Value.Duration.Should().Be(TimeSpan.FromSeconds(96));
        result.Value.Title.Should().NotBeNullOrWhiteSpace();
        result.Value.ThumbnailUrl.Should().StartWith("https://");
        result.Value.Url.Should().Be($"https://www.youtube.com/watch?v={ReferenceVideoId}");
    }

    [Fact]
    public async Task GetMetadataAsync_WhenTheUrlCarriesAPlaylist_ReturnsOnlyThatVideo()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var videoUrl = VideoUrl.Create(
            $"https://www.youtube.com/watch?v={ReferenceVideoId}&list=PLabcdefghijklmnop&index=7");

        var result = await CreateProvider().GetMetadataAsync(videoUrl, cancellation.Token);

        result.IsSuccess.Should().BeTrue(because: result.Diagnostics);
        result.Value!.VideoId.Should().Be(ReferenceVideoId);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenAlreadyCancelled_ReturnsCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var videoUrl = VideoUrl.Create($"https://www.youtube.com/watch?v={ReferenceVideoId}");

        var result = await CreateProvider().GetMetadataAsync(videoUrl, cancellation.Token);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Canceled);
    }
}
