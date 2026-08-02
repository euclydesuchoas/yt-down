using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Application.Services;

namespace YTDown.UnitTests.Application.Services;

public class DownloadHistoryServiceTests
{
    private readonly FakeStore _store = new();

    private DownloadHistoryService CreateService() => new(_store);

    private static DownloadHistoryEntryDto AnEntry(string fileName, int minutesAgo = 0) =>
        new("https://www.youtube.com/watch?v=UKcJqQqiXq0",
            fileName,
            $@"C:\Users\Euclydes\Downloads\{fileName}",
            20_000_000,
            MediaKind.Video,
            new DateTimeOffset(2026, 8, 2, 14, 30, 0, TimeSpan.Zero).AddMinutes(-minutesAgo));

    [Fact]
    public async Task GetRecentAsync_WithNothingRecorded_ReturnsAnEmptyList()
    {
        var entries = await CreateService().GetRecentAsync(CancellationToken.None);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordAsync_PutsTheNewestDownloadFirst()
    {
        var service = CreateService();

        await service.RecordAsync(AnEntry("primeiro.mp4"), CancellationToken.None);
        await service.RecordAsync(AnEntry("segundo.mp4"), CancellationToken.None);

        var entries = await service.GetRecentAsync(CancellationToken.None);

        entries.Select(entry => entry.FileName).Should().Equal("segundo.mp4", "primeiro.mp4");
    }

    /// <summary>
    /// Baixar o mesmo video de novo produz o mesmo arquivo. Duas linhas iguais
    /// apontando para ele nao ajudariam ninguem.
    /// </summary>
    [Fact]
    public async Task RecordAsync_WithAFileThatWasAlreadyDownloaded_KeepsOneEntryAndMovesItToTheTop()
    {
        var service = CreateService();

        await service.RecordAsync(AnEntry("musica.mp3"), CancellationToken.None);
        await service.RecordAsync(AnEntry("outro.mp4"), CancellationToken.None);
        await service.RecordAsync(AnEntry("musica.mp3"), CancellationToken.None);

        var entries = await service.GetRecentAsync(CancellationToken.None);

        entries.Select(entry => entry.FileName).Should().Equal("musica.mp3", "outro.mp4");
    }

    [Fact]
    public async Task RecordAsync_ComparesPathsWithoutCaringAboutCase()
    {
        var service = CreateService();
        var entry = AnEntry("video.mp4");

        await service.RecordAsync(entry, CancellationToken.None);
        await service.RecordAsync(entry with { FilePath = entry.FilePath.ToUpperInvariant() }, CancellationToken.None);

        (await service.GetRecentAsync(CancellationToken.None)).Should().ContainSingle();
    }

    [Fact]
    public async Task RecordAsync_ForgetsTheOldestOnceTheLimitIsReached()
    {
        var service = CreateService();

        for (var index = 1; index <= 55; index++)
        {
            await service.RecordAsync(AnEntry($"video-{index}.mp4"), CancellationToken.None);
        }

        var entries = await service.GetRecentAsync(CancellationToken.None);

        entries.Should().HaveCount(50);
        entries[0].FileName.Should().Be("video-55.mp4");
        entries[^1].FileName.Should().Be("video-6.mp4");
    }

    [Fact]
    public async Task ClearAsync_LeavesNothingBehind()
    {
        var service = CreateService();
        await service.RecordAsync(AnEntry("video.mp4"), CancellationToken.None);

        await service.ClearAsync(CancellationToken.None);

        (await service.GetRecentAsync(CancellationToken.None)).Should().BeEmpty();
    }

    /// <summary>
    /// O arquivo ja esta no disco quando o registro e escrito. Desfazer um
    /// download por causa de um historico que nao pode ser gravado seria pior
    /// que perder o registro.
    /// </summary>
    [Fact]
    public async Task RecordAsync_WhenTheFileCannotBeWritten_DoesNotFail()
    {
        _store.FailWith = new UnauthorizedAccessException("pasta somente leitura");

        var record = async () => await CreateService().RecordAsync(AnEntry("video.mp4"), CancellationToken.None);

        await record.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetRecentAsync_WhenTheFileCannotBeRead_ReturnsAnEmptyList()
    {
        _store.FailWith = new IOException("arquivo em uso por outro processo");

        var entries = await CreateService().GetRecentAsync(CancellationToken.None);

        entries.Should().BeEmpty();
    }

    /// <summary>
    /// So falha de acesso ao arquivo e tolerada. Defeito de programacao precisa
    /// aparecer, e nao ser confundido com um historico vazio.
    /// </summary>
    [Fact]
    public async Task GetRecentAsync_WhenSomethingElseBreaks_LetsItSurface()
    {
        _store.FailWith = new InvalidOperationException("defeito de programacao");

        var read = async () => await CreateService().GetRecentAsync(CancellationToken.None);

        await read.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class FakeStore : IDownloadHistoryStore
    {
        private IReadOnlyList<DownloadHistoryEntryDto> _entries = [];

        public Exception? FailWith { get; set; }

        public Task<IReadOnlyList<DownloadHistoryEntryDto>> ReadAsync(CancellationToken cancellationToken) =>
            FailWith is null ? Task.FromResult(_entries) : Task.FromException<IReadOnlyList<DownloadHistoryEntryDto>>(FailWith);

        public Task WriteAsync(IReadOnlyList<DownloadHistoryEntryDto> entries, CancellationToken cancellationToken)
        {
            if (FailWith is not null)
            {
                return Task.FromException(FailWith);
            }

            _entries = entries;

            return Task.CompletedTask;
        }
    }
}
