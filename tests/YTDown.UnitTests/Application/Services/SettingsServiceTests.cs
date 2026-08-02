using FluentAssertions;
using Moq;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;

namespace YTDown.UnitTests.Application.Services;

public class SettingsServiceTests
{
    private readonly Mock<ISettingsStore> _store = new();

    private SettingsService CreateService() => new(_store.Object);

    private void GivenSaved(SettingsDto? settings) =>
        _store
            .Setup(store => store.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

    [Fact]
    public async Task GetAsync_WithNothingSaved_ReturnsTheDefaults()
    {
        GivenSaved(null);

        var settings = await CreateService().GetAsync(CancellationToken.None);

        settings.Should().Be(SettingsDto.Default);
    }

    [Fact]
    public async Task GetAsync_ReturnsWhatTheUserChose()
    {
        var saved = new SettingsDto(@"D:\Videos", 720);
        GivenSaved(saved);

        (await CreateService().GetAsync(CancellationToken.None)).Should().Be(saved);
    }

    /// <summary>
    /// Todo download consulta o destino. Ir ao disco a cada consulta seria pagar
    /// por uma leitura que nunca muda sozinha.
    /// </summary>
    [Fact]
    public async Task GetAsync_ReadsFromDiskOnlyOnce()
    {
        GivenSaved(new SettingsDto(@"D:\Videos", 720));

        var service = CreateService();

        await service.GetAsync(CancellationToken.None);
        await service.GetAsync(CancellationToken.None);
        await service.GetAsync(CancellationToken.None);

        _store.Verify(store => store.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_MakesTheNewChoiceValidWithoutReadingTheDiskAgain()
    {
        GivenSaved(null);

        var service = CreateService();
        await service.GetAsync(CancellationToken.None);

        var chosen = new SettingsDto(@"D:\Videos", 1080);
        await service.SaveAsync(chosen, CancellationToken.None);

        (await service.GetAsync(CancellationToken.None)).Should().Be(chosen);
        _store.Verify(store => store.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WritesTheChoiceToDisk()
    {
        GivenSaved(null);

        var chosen = new SettingsDto(@"D:\Videos", 480);
        await CreateService().SaveAsync(chosen, CancellationToken.None);

        _store.Verify(store => store.WriteAsync(chosen, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// A escolha vale ate o aplicativo fechar mesmo quando nao pode ser gravada:
    /// impedir o usuario de fechar a tela seria pior que perder a preferencia.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WhenTheFileCannotBeWritten_KeepsTheChoiceForThisSession()
    {
        GivenSaved(null);

        _store
            .Setup(store => store.WriteAsync(It.IsAny<SettingsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("pasta somente leitura"));

        var service = CreateService();
        var chosen = new SettingsDto(@"D:\Videos", 720);

        var save = async () => await service.SaveAsync(chosen, CancellationToken.None);

        await save.Should().NotThrowAsync();
        (await service.GetAsync(CancellationToken.None)).Should().Be(chosen);
    }

    [Fact]
    public async Task GetAsync_WhenTheFileCannotBeRead_FallsBackToTheDefaults()
    {
        _store
            .Setup(store => store.ReadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("arquivo em uso por outro processo"));

        (await CreateService().GetAsync(CancellationToken.None)).Should().Be(SettingsDto.Default);
    }

    /// <summary>
    /// So falha de acesso ao arquivo e tolerada; defeito de programacao precisa
    /// aparecer.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenSomethingElseBreaks_LetsItSurface()
    {
        _store
            .Setup(store => store.ReadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("defeito de programacao"));

        var read = async () => await CreateService().GetAsync(CancellationToken.None);

        await read.Should().ThrowAsync<InvalidOperationException>();
    }
}
