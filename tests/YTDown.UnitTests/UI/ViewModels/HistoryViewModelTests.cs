using System.Globalization;
using FluentAssertions;
using Moq;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.UI.ViewModels;

namespace YTDown.UnitTests.UI.ViewModels;

public class HistoryViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 20, 0, 0, TimeSpan.Zero);

    private readonly Mock<IDownloadHistoryService> _downloadHistory = new();
    private readonly Mock<IFileExplorer> _fileExplorer = new();

    private HistoryViewModel CreateViewModel() =>
        new(_downloadHistory.Object, _fileExplorer.Object, new FixedTimeProvider(Now));

    private static DownloadHistoryEntryDto AnEntry(
        string fileName = "video.mp4",
        long sizeInBytes = 20_000_000,
        MediaKind kind = MediaKind.Video,
        int hoursAgo = 1) =>
        new("https://www.youtube.com/watch?v=UKcJqQqiXq0",
            fileName,
            $@"C:\Users\Euclydes\Downloads\{fileName}",
            sizeInBytes,
            kind,
            Now.AddHours(-hoursAgo));

    private void GivenHistoryContains(params DownloadHistoryEntryDto[] entries) =>
        _downloadHistory
            .Setup(history => history.GetRecentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

    [Fact]
    public async Task LoadCommand_WithNothingRecorded_ReportsAnEmptyHistory()
    {
        GivenHistoryContains();

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Items.Should().BeEmpty();
        viewModel.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadCommand_KeepsTheOrderTheHistoryGave()
    {
        GivenHistoryContains(AnEntry("recente.mp4"), AnEntry("antigo.mp4"));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Items.Select(item => item.FileName).Should().Equal("recente.mp4", "antigo.mp4");
        viewModel.IsEmpty.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "hoje às 19:00")]
    [InlineData(21, "ontem às 23:00")]
    [InlineData(72, "30/07/2026 às 20:00")]
    public async Task LoadCommand_SaysWhenTheDownloadHappenedInPlainWords(int hoursAgo, string expected)
    {
        GivenHistoryContains(AnEntry(hoursAgo: hoursAgo));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Items.Should().ContainSingle().Which.Description.Should().StartWith(expected);
    }

    [Fact]
    public async Task LoadCommand_DescribesAnAudioDownloadAsAudio()
    {
        GivenHistoryContains(AnEntry("musica.mp3", 5_000_000, MediaKind.AudioOnly));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var description = viewModel.Items.Single().Description;

        description.Should().Contain("Áudio");
        description.Should().NotContain("Vídeo");
    }

    /// <summary>
    /// Bytes não dizem nada a quem abre o histórico.
    /// </summary>
    [Theory]
    [InlineData(20_000_000L, "19,1 MB")]
    [InlineData(500_000L, "488 KB")]
    [InlineData(3_000_000_000L, "2,8 GB")]
    public async Task LoadCommand_ShowsTheSizeInAUnitTheUserReads(long sizeInBytes, string expected)
    {
        // O tamanho é escrito na cultura da máquina. Fixá-la aqui é o que torna
        // o esperado previsível, em vez de depender de onde o teste roda.
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        GivenHistoryContains(AnEntry(sizeInBytes: sizeInBytes));

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Items.Single().Description.Should().EndWith(expected);
    }

    [Fact]
    public async Task OpenContainingFolderCommand_RevealsTheFileOfThatEntry()
    {
        var entry = AnEntry();
        GivenHistoryContains(entry);

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.OpenContainingFolderCommand.Execute(viewModel.Items.Single());

        _fileExplorer.Verify(explorer => explorer.RevealFile(entry.FilePath), Times.Once);
    }

    [Fact]
    public async Task ClearCommand_ForgetsTheRecordsAndShowsTheResult()
    {
        GivenHistoryContains(AnEntry());

        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        GivenHistoryContains();

        await viewModel.ClearCommand.ExecuteAsync(null);

        _downloadHistory.Verify(history => history.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        viewModel.Items.Should().BeEmpty();
    }

    /// <summary>
    /// A tela recarrega depois de limpar em vez de esvaziar a lista na mão: se a
    /// gravação falhou, o usuário ve que os registros continuam lá.
    /// </summary>
    [Fact]
    public async Task ClearCommand_WhenNothingCouldBeErased_KeepsShowingWhatIsStillThere()
    {
        GivenHistoryContains(AnEntry());

        var viewModel = CreateViewModel();

        await viewModel.ClearCommand.ExecuteAsync(null);

        viewModel.Items.Should().ContainSingle();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
