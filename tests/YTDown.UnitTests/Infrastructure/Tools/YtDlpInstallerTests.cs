using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Infrastructure.Tools;

namespace YTDown.UnitTests.Infrastructure.Tools;

public class YtDlpInstallerTests : IDisposable
{
    private const string ExecutableName = "yt-dlp.exe";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ytdown-install-{Guid.NewGuid():N}");

    private string BundledDirectory => Path.Combine(_root, "bundled");

    private string UserDirectory => Path.Combine(_root, "user");

    private string InstalledExecutable => Path.Combine(UserDirectory, ExecutableName);

    public YtDlpInstallerTests() => Directory.CreateDirectory(BundledDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private YtDlpInstaller CreateInstaller() => new(new ToolLocations(BundledDirectory, UserDirectory));

    private void GivenBundledExecutable(string content = "yt-dlp original") =>
        File.WriteAllText(Path.Combine(BundledDirectory, ExecutableName), content);

    private void GivenBundledManifest(string version) =>
        File.WriteAllText(
            Path.Combine(BundledDirectory, "tools.lock.json"),
            $$"""
              { "tools": [ { "name": "yt-dlp", "version": "{{version}}" }, { "name": "ffmpeg", "version": "8.1.2" } ] }
              """);

    [Fact]
    public async Task EnsureInstalledAsync_OnFirstRun_CopiesTheExecutableToTheWritableFolder()
    {
        GivenBundledExecutable();
        GivenBundledManifest("2026.07.04");

        var result = await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue(because: "o arquivo foi copiado nesta chamada");
        File.Exists(InstalledExecutable).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureInstalledAsync_WhenTheSameVersionIsAlreadyInstalled_DoesNothing()
    {
        GivenBundledExecutable();
        GivenBundledManifest("2026.07.04");

        await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        var result = await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    /// <summary>
    /// O yt-dlp instalado costuma estar mais novo que o da instalacao, por ter
    /// se atualizado sozinho. Sobrescreve-lo a cada abertura seria retroceder.
    /// </summary>
    [Fact]
    public async Task EnsureInstalledAsync_DoesNotOverwriteAnExecutableThatUpdatedItself()
    {
        GivenBundledExecutable();
        GivenBundledManifest("2026.07.04");
        await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        await File.WriteAllTextAsync(InstalledExecutable, "yt-dlp mais novo");

        await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        (await File.ReadAllTextAsync(InstalledExecutable)).Should().Be("yt-dlp mais novo");
    }

    /// <summary>
    /// Quando o aplicativo passa a trazer outra versao, ela deve substituir a
    /// que esta instalada.
    /// </summary>
    [Fact]
    public async Task EnsureInstalledAsync_WhenTheBundledVersionChanges_CopiesAgain()
    {
        GivenBundledExecutable("versao antiga");
        GivenBundledManifest("2026.07.04");
        await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        GivenBundledExecutable("versao nova");
        GivenBundledManifest("2026.12.01");

        var result = await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        result.Value.Should().BeTrue();
        (await File.ReadAllTextAsync(InstalledExecutable)).Should().Be("versao nova");
    }

    [Fact]
    public async Task EnsureInstalledAsync_WithoutAManifest_CopiesOnlyWhenTheFileIsMissing()
    {
        GivenBundledExecutable();

        (await CreateInstaller().EnsureInstalledAsync(CancellationToken.None)).Value.Should().BeTrue();
        (await CreateInstaller().EnsureInstalledAsync(CancellationToken.None)).Value.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureInstalledAsync_WhenTheApplicationDoesNotShipTheExecutable_Fails()
    {
        var result = await CreateInstaller().EnsureInstalledAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.ToolNotFound);
    }
}
