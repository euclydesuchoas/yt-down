using FluentAssertions;
using YTDown.Application.DTOs;
using YTDown.Infrastructure.FileSystem;

namespace YTDown.UnitTests.Infrastructure.FileSystem;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ytdown-settings-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_root, "settings.json");

    private JsonSettingsStore CreateStore() => new(FilePath);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ReadAsync_BeforeAnythingIsSaved_ReturnsNothing()
    {
        (await CreateStore().ReadAsync(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_ReturnsWhatWasSaved()
    {
        var settings = new SettingsDto(@"D:\Videos", 720);

        await CreateStore().WriteAsync(settings, CancellationToken.None);

        (await CreateStore().ReadAsync(CancellationToken.None)).Should().Be(settings);
    }

    [Fact]
    public async Task WriteAsync_KeepsTheChoiceOfLettingTheApplicationDecide()
    {
        await CreateStore().WriteAsync(SettingsDto.Default, CancellationToken.None);

        var saved = await CreateStore().ReadAsync(CancellationToken.None);

        saved!.DestinationDirectory.Should().BeNull();
        saved.MaximumHeight.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_WithACorruptedFile_ReturnsNothingInsteadOfFailing()
    {
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(FilePath, "isto nao e json");

        (await CreateStore().ReadAsync(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_LeavesNoTemporaryFileBehind()
    {
        await CreateStore().WriteAsync(new SettingsDto(@"D:\Videos", 1080), CancellationToken.None);

        Directory.GetFiles(_root).Should().ContainSingle().Which.Should().EndWith("settings.json");
    }
}
