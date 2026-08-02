using FluentAssertions;
using YTDown.Application.Common;
using YTDown.Infrastructure.YouTube;

namespace YTDown.UnitTests.Infrastructure.YouTube;

public class YtDlpOutputTemplateTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"ytdown-nomes-{Guid.NewGuid():N}");

    public YtDlpOutputTemplateTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string For(string? chosen, MediaKind kind = MediaKind.Video) =>
        YtDlpOutputTemplate.For(chosen, kind, _directory);

    private void GivenFileExists(string fileName) =>
        File.WriteAllText(Path.Combine(_directory, fileName), "arquivo de teste");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void For_WithoutAChosenName_LetsTheTitleDecide(string? chosen)
    {
        For(chosen).Should().Be(YtDlpOutputTemplate.FromTitle);
    }

    [Fact]
    public void For_WithAChosenName_UsesItAndLetsTheToolAddTheExtension()
    {
        For("Minha musica").Should().Be("Minha musica.%(ext)s");
    }

    /// <summary>
    /// O <c>%</c> abre um campo no template do yt-dlp. Sem duplicar, "100%" e
    /// "%(title)s" seriam lidos como instrucao em vez de texto.
    /// </summary>
    [Theory]
    [InlineData("Desconto de 100%", "Desconto de 100%%.%(ext)s")]
    [InlineData("Antes %(title)s Depois", "Antes %%(title)s Depois.%(ext)s")]
    public void For_EscapesThePercentSignSoThatItStaysText(string chosen, string expected)
    {
        For(chosen).Should().Be(expected);
    }

    /// <summary>
    /// O yt-dlp encontra o arquivo ja gravado, pula o download e termina com
    /// sucesso informando o caminho: o usuario receberia o arquivo antigo
    /// achando que baixou o novo.
    /// </summary>
    [Fact]
    public void For_WhenTheNameIsTaken_PicksTheNextOneLikeABrowserWould()
    {
        GivenFileExists("Musica.mp4");

        For("Musica").Should().Be("Musica (2).%(ext)s");
    }

    [Fact]
    public void For_KeepsCountingWhileTheNamesAreTaken()
    {
        GivenFileExists("Musica.mp4");
        GivenFileExists("Musica (2).mp4");
        GivenFileExists("Musica (3).mp4");

        For("Musica").Should().Be("Musica (4).%(ext)s");
    }

    /// <summary>
    /// A colisao e por extensao: um MP3 nao disputa o nome com um MP4.
    /// </summary>
    [Fact]
    public void For_LooksOnlyAtTheExtensionItIsAboutToWrite()
    {
        GivenFileExists("Musica.mp4");

        For("Musica", MediaKind.AudioOnly).Should().Be("Musica.%(ext)s");
    }

    [Fact]
    public void For_WithAudioOnly_ChecksTheMp3()
    {
        GivenFileExists("Musica.mp3");

        For("Musica", MediaKind.AudioOnly).Should().Be("Musica (2).%(ext)s");
    }

    [Theory]
    [InlineData(MediaKind.Video, "mp4")]
    [InlineData(MediaKind.AudioOnly, "mp3")]
    public void ExtensionFor_AnswersWhatTheArgumentsForce(MediaKind kind, string expected)
    {
        YtDlpOutputTemplate.ExtensionFor(kind).Should().Be(expected);
    }
}
