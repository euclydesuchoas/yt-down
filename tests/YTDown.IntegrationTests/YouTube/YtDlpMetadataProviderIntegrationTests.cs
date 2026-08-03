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
/// Marcados com a categoria Integration para poderem ser excluídos quando
/// não houver rede disponível.
/// </remarks>
[Trait("Category", "Integration")]
public class YtDlpMetadataProviderIntegrationTests
{
    private const string ReferenceVideoId = "UKcJqQqiXq0";

    private static YtDlpMetadataProvider CreateProvider()
    {
        var tools = FindToolsDirectory();

        // Nos testes as duas pastas são a mesma: o repositório não separa a
        // cópia do perfil da que acompanha a instalação.
        return new YtDlpMetadataProvider(new ProcessRunner(), new ManagedToolLocator(new ToolLocations(tools, tools)));
    }

    /// <summary>
    /// A pasta tools fica na raiz do repositório, não junto do binário de teste.
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
            "Pasta tools não encontrada. Execute scripts/bootstrap-tools.ps1 antes dos testes de integração.");
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
        // Afirmar o título exato é o que revela perda de codificação entre o
        // yt-dlp e o aplicativo. Um "não está vazio" deixaria passar.
        result.Value.Title.Should().Be(
            "『無職転生Ⅲ ～異世界行ったら本気だす～』ノンクレジットED映像／EDテーマ：「祈り、終われば」中島美嘉");
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
