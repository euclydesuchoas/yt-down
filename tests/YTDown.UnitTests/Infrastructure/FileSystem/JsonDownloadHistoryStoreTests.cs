using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Infrastructure.FileSystem;

namespace YTDown.UnitTests.Infrastructure.FileSystem;

public class JsonDownloadHistoryStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ytdown-history-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_root, "history.json");

    private JsonDownloadHistoryStore CreateStore() => new(FilePath);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static DownloadHistoryEntryDto AnEntry(string fileName = "video.mp4") =>
        new("https://www.youtube.com/watch?v=UKcJqQqiXq0",
            fileName,
            $@"C:\Users\Euclydes\Downloads\{fileName}",
            20_000_000,
            MediaKind.Video,
            new DateTimeOffset(2026, 8, 2, 14, 30, 0, TimeSpan.FromHours(-3)));

    [Fact]
    public async Task ReadAsync_BeforeAnythingIsWritten_ReturnsNothing()
    {
        var entries = await CreateStore().ReadAsync(CancellationToken.None);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_CreatesTheFolderThatDoesNotExistYet()
    {
        await CreateStore().WriteAsync([AnEntry()], CancellationToken.None);

        File.Exists(FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_ReturnsEveryFieldThatWasWritten()
    {
        var entry = AnEntry();

        await CreateStore().WriteAsync([entry], CancellationToken.None);

        var entries = await CreateStore().ReadAsync(CancellationToken.None);

        entries.Should().ContainSingle().Which.Should().Be(entry);
    }

    [Fact]
    public async Task WriteAsync_PreservesTheOrderItReceived()
    {
        DownloadHistoryEntryDto[] written = [AnEntry("primeiro.mp4"), AnEntry("segundo.mp4"), AnEntry("terceiro.mp4")];

        await CreateStore().WriteAsync(written, CancellationToken.None);

        var entries = await CreateStore().ReadAsync(CancellationToken.None);

        entries.Select(entry => entry.FileName).Should().Equal("primeiro.mp4", "segundo.mp4", "terceiro.mp4");
    }

    [Fact]
    public async Task WriteAsync_ReplacesWhatWasThereBefore()
    {
        var store = CreateStore();

        await store.WriteAsync([AnEntry("antigo.mp4")], CancellationToken.None);
        await store.WriteAsync([AnEntry("novo.mp4")], CancellationToken.None);

        var entries = await store.ReadAsync(CancellationToken.None);

        entries.Should().ContainSingle().Which.FileName.Should().Be("novo.mp4");
    }

    /// <summary>
    /// Titulos de video usam qualquer alfabeto, e o nome do arquivo vem do
    /// titulo. Um historico que estrague acentos ou japones seria inutil.
    /// </summary>
    [Fact]
    public async Task WriteAsync_KeepsCharactersThatAreNotAscii()
    {
        const string fileName = "ドキドキ - cancao da manha.mp4";

        await CreateStore().WriteAsync([AnEntry(fileName)], CancellationToken.None);

        var entries = await CreateStore().ReadAsync(CancellationToken.None);

        entries.Should().ContainSingle().Which.FileName.Should().Be(fileName);
    }

    /// <summary>
    /// Perder a lista e aceitavel; deixar o aplicativo sem abrir nao e.
    /// </summary>
    [Fact]
    public async Task ReadAsync_WithACorruptedFile_ReturnsNothingInsteadOfFailing()
    {
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(FilePath, "{ isto nao e json");

        var entries = await CreateStore().ReadAsync(CancellationToken.None);

        entries.Should().BeEmpty();
    }

    /// <summary>
    /// O arquivo e o unico lugar onde o formato aparece por extenso: convem que
    /// continue legivel por gente, para poder ser conferido a mao.
    /// </summary>
    [Fact]
    public async Task WriteAsync_RecordsTheKindByNameAndNotByNumber()
    {
        await CreateStore().WriteAsync(
            [AnEntry() with { Kind = MediaKind.AudioOnly }],
            CancellationToken.None);

        var content = await File.ReadAllTextAsync(FilePath);

        content.Should().Contain("AudioOnly");
    }

    [Fact]
    public async Task WriteAsync_LeavesNoTemporaryFileBehind()
    {
        await CreateStore().WriteAsync([AnEntry()], CancellationToken.None);

        Directory.GetFiles(_root).Should().ContainSingle().Which.Should().EndWith("history.json");
    }
}
