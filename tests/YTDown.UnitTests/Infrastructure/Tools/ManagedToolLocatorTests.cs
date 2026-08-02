using FluentAssertions;
using YTDown.Infrastructure.Tools;

namespace YTDown.UnitTests.Infrastructure.Tools;

public class ManagedToolLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ytdown-tools-{Guid.NewGuid():N}");

    private string BundledDirectory => Path.Combine(_root, "bundled");

    private string UserDirectory => Path.Combine(_root, "user");

    public ManagedToolLocatorTests()
    {
        Directory.CreateDirectory(BundledDirectory);
        Directory.CreateDirectory(UserDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private ManagedToolLocator CreateLocator() => new(new ToolLocations(BundledDirectory, UserDirectory));

    private static string Place(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, string.Empty);

        return path;
    }

    /// <summary>
    /// A copia do perfil se mantem atualizada; a que acompanha a instalacao nao.
    /// </summary>
    [Fact]
    public void TryLocate_ForYtDlp_PrefersTheUserCopyOverTheBundledOne()
    {
        Place(BundledDirectory, "yt-dlp.exe");
        var expected = Place(UserDirectory, "yt-dlp.exe");

        CreateLocator().TryLocate(ExternalTool.YtDlp, out var path).Should().BeTrue();

        path.Should().Be(expected);
    }

    /// <summary>
    /// Garante que um download funcione mesmo antes de a instalacao terminar.
    /// </summary>
    [Fact]
    public void TryLocate_ForYtDlp_FallsBackToTheBundledCopy()
    {
        var expected = Place(BundledDirectory, "yt-dlp.exe");

        CreateLocator().TryLocate(ExternalTool.YtDlp, out var path).Should().BeTrue();

        path.Should().Be(expected);
    }

    /// <summary>
    /// O FFmpeg nunca se atualiza, entao uma copia no perfil nao deve ser usada.
    /// </summary>
    [Fact]
    public void TryLocate_ForFFmpeg_LooksOnlyAtTheBundledCopy()
    {
        Place(UserDirectory, "ffmpeg.exe");

        CreateLocator().TryLocate(ExternalTool.FFmpeg, out var ignored).Should().BeFalse();
        ignored.Should().BeNull();

        var expected = Place(BundledDirectory, "ffmpeg.exe");

        CreateLocator().TryLocate(ExternalTool.FFmpeg, out var found).Should().BeTrue();
        found.Should().Be(expected);
    }

    [Fact]
    public void TryLocate_WhenTheExecutableIsMissingEverywhere_Fails()
    {
        CreateLocator().TryLocate(ExternalTool.YtDlp, out var path).Should().BeFalse();
        path.Should().BeNull();
    }

    [Fact]
    public void TryLocate_WhenNoDirectoryExists_Fails()
    {
        var locator = new ManagedToolLocator(
            new ToolLocations(Path.Combine(_root, "inexistente"), Path.Combine(_root, "tambem-nao")));

        locator.TryLocate(ExternalTool.YtDlp, out var path).Should().BeFalse();
        path.Should().BeNull();
    }
}
