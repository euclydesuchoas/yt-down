using FluentAssertions;
using YTDown.Domain.ValueObjects;

namespace YTDown.UnitTests.Domain.ValueObjects;

public class OutputFileNameTests
{
    private static string Sanitized(string? candidate) =>
        OutputFileName.TryCreate(candidate, out var fileName) ? fileName.Value : string.Empty;

    [Fact]
    public void TryCreate_WithAnOrdinaryName_KeepsItAsItIs()
    {
        Sanitized("Detalhes Magicos").Should().Be("Detalhes Magicos");
    }

    /// <summary>
    /// Acento e outros alfabetos são conteúdo legítimo de nome de arquivo, e o
    /// público deste aplicativo escreve em português.
    /// </summary>
    [Theory]
    [InlineData("Canção da Manhã")]
    [InlineData("ドキドキ")]
    [InlineData("Coração & Alma - 100% ao vivo")]
    public void TryCreate_KeepsCharactersThatTheSystemAccepts(string candidate)
    {
        Sanitized(candidate).Should().Be(candidate);
    }

    [Theory]
    [InlineData(@"AC/DC", "ACDC")]
    [InlineData("Antes: Depois", "Antes Depois")]
    [InlineData("Qual?", "Qual")]
    [InlineData(@"a<b>c|d*e""f", "abcdef")]
    public void TryCreate_DropsWhatWindowsRefuses(string candidate, string expected)
    {
        Sanitized(candidate).Should().Be(expected);
    }

    /// <summary>
    /// O Windows descarta ponto e espaço no fim em silêncio, o que faria o
    /// arquivo gravado não bater com o nome pedido.
    /// </summary>
    [Theory]
    [InlineData("Musica...", "Musica")]
    [InlineData("Musica   ", "Musica")]
    [InlineData("  Musica  ", "Musica")]
    public void TryCreate_RemovesTheDotsAndSpacesAtTheEnd(string candidate, string expected)
    {
        Sanitized(candidate).Should().Be(expected);
    }

    [Fact]
    public void TryCreate_CutsWhatIsLongerThanTheLimit()
    {
        var name = Sanitized(new string('a', 250));

        name.Should().HaveLength(OutputFileName.MaximumLength);
    }

    /// <summary>
    /// Nomes de dispositivo são recusados pelo Windows mesmo com extensão.
    /// </summary>
    [Theory]
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("COM1")]
    [InlineData("LPT9.mp4")]
    public void TryCreate_RefusesTheNamesThatWindowsReserves(string candidate)
    {
        OutputFileName.TryCreate(candidate, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"//\\::")]
    [InlineData("...")]
    public void TryCreate_WithNothingUsable_Fails(string? candidate)
    {
        OutputFileName.TryCreate(candidate, out var fileName).Should().BeFalse();
        fileName.Should().BeNull();
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('ç', true)]
    [InlineData(' ', true)]
    [InlineData('%', true)]
    [InlineData(':', false)]
    [InlineData('/', false)]
    [InlineData('?', false)]
    [InlineData('\t', false)]
    public void IsAllowedCharacter_AnswersForASingleKey(char character, bool expected)
    {
        OutputFileName.IsAllowedCharacter(character).Should().Be(expected);
    }
}
