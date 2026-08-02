using FluentAssertions;
using YTDown.Infrastructure.Tools;

namespace YTDown.UnitTests.Infrastructure.Tools;

/// <summary>
/// As saidas usadas aqui foram capturadas de uma execucao real de
/// <c>yt-dlp -U</c>, e nao inventadas.
/// </summary>
public class YtDlpUpdaterTests
{
    [Fact]
    public void TryReadVersion_WhenAlreadyUpToDate_ReadsTheVersion()
    {
        const string output = """
            Latest version: stable@2026.07.04 from yt-dlp/yt-dlp
            yt-dlp is up to date (stable@2026.07.04 from yt-dlp/yt-dlp)
            """;

        YtDlpUpdater.TryReadVersion(output, out var version).Should().BeTrue();
        version.Should().Be("2026.07.04");
    }

    [Fact]
    public void TryReadVersion_WhenItUpdates_ReadsTheNewVersion()
    {
        const string output = """
            Latest version: stable@2026.12.01 from yt-dlp/yt-dlp
            Current version: stable@2026.07.04 from yt-dlp/yt-dlp
            Updating to stable@2026.12.01 from yt-dlp/yt-dlp ...
            Updated yt-dlp to stable@2026.12.01 from yt-dlp/yt-dlp
            """;

        YtDlpUpdater.TryReadVersion(output, out var version).Should().BeTrue();
        version.Should().Be("2026.12.01");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ERROR: unable to reach the update server")]
    public void TryReadVersion_WithoutAVersion_Fails(string? output)
    {
        YtDlpUpdater.TryReadVersion(output, out var version).Should().BeFalse();
        version.Should().BeEmpty();
    }
}
