using FluentAssertions;
using Moq;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Infrastructure.FileSystem;

namespace YTDown.UnitTests.Infrastructure.FileSystem;

public class WindowsDownloadLocationProviderTests : IDisposable
{
    private readonly string _chosenDirectory =
        Path.Combine(Path.GetTempPath(), $"ytdown-destination-{Guid.NewGuid():N}");

    private readonly Mock<ISettingsService> _settings = new();

    private WindowsDownloadLocationProvider CreateProvider() => new(_settings.Object);

    public void Dispose()
    {
        if (Directory.Exists(_chosenDirectory))
        {
            Directory.Delete(_chosenDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private void GivenSettings(SettingsDto settings) =>
        _settings
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

    [Fact]
    public async Task GetDestinationDirectoryAsync_WithNothingChosen_UsesTheDownloadsFolder()
    {
        GivenSettings(SettingsDto.Default);

        var destination = await CreateProvider().GetDestinationDirectoryAsync(CancellationToken.None);

        destination.Should().EndWith("Downloads");
    }

    [Fact]
    public async Task GetDestinationDirectoryAsync_UsesTheFolderTheUserChose()
    {
        Directory.CreateDirectory(_chosenDirectory);
        GivenSettings(new SettingsDto(_chosenDirectory));

        var destination = await CreateProvider().GetDestinationDirectoryAsync(CancellationToken.None);

        destination.Should().Be(_chosenDirectory);
    }

    [Fact]
    public void Exists_DistinguishesAFolderThatIsThereFromOneThatIsNot()
    {
        Directory.CreateDirectory(_chosenDirectory);

        var provider = CreateProvider();

        provider.Exists(_chosenDirectory).Should().BeTrue();
        provider.Exists(Path.Combine(_chosenDirectory, "nao-existe")).Should().BeFalse();
        provider.Exists("   ").Should().BeFalse();
    }

    /// <summary>
    /// Pendrive removido, unidade de rede fora do ar, pasta apagada. Um arquivo
    /// em lugar diferente do esperado ainda é melhor que nenhum arquivo.
    /// </summary>
    [Fact]
    public async Task GetDestinationDirectoryAsync_WhenTheChosenFolderIsGone_FallsBackToDownloads()
    {
        GivenSettings(new SettingsDto(_chosenDirectory));

        var destination = await CreateProvider().GetDestinationDirectoryAsync(CancellationToken.None);

        destination.Should().NotBe(_chosenDirectory);
        destination.Should().EndWith("Downloads");
    }
}
