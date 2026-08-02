using FluentAssertions;
using YTDown.Infrastructure.Tools;

namespace YTDown.UnitTests.Infrastructure.Tools;

public class LocalToolLocatorTests : IDisposable
{
    private readonly string _toolsDirectory =
        Path.Combine(Path.GetTempPath(), $"ytdown-tools-{Guid.NewGuid():N}");

    public LocalToolLocatorTests() => Directory.CreateDirectory(_toolsDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_toolsDirectory))
        {
            Directory.Delete(_toolsDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(ExternalTool.YtDlp, "yt-dlp.exe")]
    [InlineData(ExternalTool.FFmpeg, "ffmpeg.exe")]
    public void TryLocate_WhenExecutableExists_ReturnsItsFullPath(ExternalTool tool, string executableName)
    {
        var expectedPath = Path.Combine(_toolsDirectory, executableName);
        File.WriteAllText(expectedPath, string.Empty);

        var locator = new LocalToolLocator(_toolsDirectory);

        var located = locator.TryLocate(tool, out var executablePath);

        located.Should().BeTrue();
        executablePath.Should().Be(expectedPath);
    }

    [Fact]
    public void TryLocate_WhenExecutableIsMissing_Fails()
    {
        var locator = new LocalToolLocator(_toolsDirectory);

        var located = locator.TryLocate(ExternalTool.YtDlp, out var executablePath);

        located.Should().BeFalse();
        executablePath.Should().BeNull();
    }

    [Fact]
    public void TryLocate_WhenToolsDirectoryDoesNotExist_Fails()
    {
        var locator = new LocalToolLocator(Path.Combine(_toolsDirectory, "inexistente"));

        var located = locator.TryLocate(ExternalTool.FFmpeg, out var executablePath);

        located.Should().BeFalse();
        executablePath.Should().BeNull();
    }
}
