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
    /// Baixar o mesmo vídeo de novo produz o mesmo arquivo. Duas linhas iguais
    /// apontando para ele não ajudariam ninguém.
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

    private static DownloadHistoryEntryDto AnEntryIn(string folder, string fileName) =>
        AnEntry(fileName) with { FilePath = Path.Combine(folder, fileName) };

    [Fact]
    public async Task GetRecentFoldersAsync_WithNothingRecorded_ReturnsAnEmptyList()
    {
        (await CreateService().GetRecentFoldersAsync(5, CancellationToken.None)).Should().BeEmpty();
    }

    /// <summary>
    /// Quem organiza por assunto volta às mesmas pastas. Repeti-las na lista
    /// gastaria o espaço que as outras precisam.
    /// </summary>
    [Fact]
    public async Task GetRecentFoldersAsync_ListsEachFolderOnceFromTheMostRecent()
    {
        var service = CreateService();

        await service.RecordAsync(AnEntryIn(@"D:\Musicas\Elvis", "um.mp3"), CancellationToken.None);
        await service.RecordAsync(AnEntryIn(@"D:\Musicas\Roberto", "dois.mp3"), CancellationToken.None);
        await service.RecordAsync(AnEntryIn(@"D:\Musicas\Elvis", "tres.mp3"), CancellationToken.None);

        var folders = await service.GetRecentFoldersAsync(5, CancellationToken.None);

        folders.Should().Equal(@"D:\Musicas\Elvis", @"D:\Musicas\Roberto");
    }

    [Fact]
    public async Task GetRecentFoldersAsync_ComparesFoldersWithoutCaringAboutCase()
    {
        var service = CreateService();

        await service.RecordAsync(AnEntryIn(@"D:\Musicas", "um.mp3"), CancellationToken.None);
        await service.RecordAsync(AnEntryIn(@"D:\MUSICAS", "dois.mp3"), CancellationToken.None);

        (await service.GetRecentFoldersAsync(5, CancellationToken.None)).Should().ContainSingle();
    }

    [Fact]
    public async Task GetRecentFoldersAsync_StopsAtTheLimitItWasGiven()
    {
        var service = CreateService();

        for (var index = 1; index <= 8; index++)
        {
            await service.RecordAsync(AnEntryIn($@"D:\Pasta{index}", $"video-{index}.mp4"), CancellationToken.None);
        }

        var folders = await service.GetRecentFoldersAsync(3, CancellationToken.None);

        folders.Should().Equal(@"D:\Pasta8", @"D:\Pasta7", @"D:\Pasta6");
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
    /// O arquivo já está no disco quando o registro é escrito. Desfazer um
    /// download por causa de um histórico que não pode ser gravado seria pior
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
    /// Só falha de acesso ao arquivo é tolerada. Defeito de programação precisa
    /// aparecer, e não ser confundido com um histórico vazio.
    /// </summary>
    [Fact]
    public async Task GetRecentAsync_WhenSomethingElseBreaks_LetsItSurface()
    {
        _store.FailWith = new InvalidOperationException("defeito de programação");

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
