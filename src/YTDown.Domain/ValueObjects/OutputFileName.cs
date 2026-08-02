using System.Diagnostics.CodeAnalysis;

namespace YTDown.Domain.ValueObjects;

/// <summary>
/// Nome de arquivo aceitavel para o sistema, sem extensao.
/// </summary>
/// <remarks>
/// O usuario digita o nome que quiser, e boa parte do que ele digita o Windows
/// recusa. Deixar isso chegar ao yt-dlp produziria uma falha que ninguem
/// entende; corrigir aqui produz um nome parecido com o pedido.
/// </remarks>
public sealed record OutputFileName
{
    /// <summary>
    /// Limite de caracteres do nome.
    /// </summary>
    /// <remarks>
    /// Nao e limite do sistema, e sim margem: o caminho completo do Windows para
    /// em 260 caracteres, e a pasta escolhida pelo usuario pode ja ser funda.
    /// </remarks>
    public const int MaximumLength = 100;

    /// <summary>
    /// Caracteres que o Windows recusa em nome de arquivo.
    /// </summary>
    /// <remarks>
    /// Escritos a mao em vez de <c>Path.GetInvalidFileNameChars</c>: a lista
    /// daquele metodo muda conforme o sistema onde o codigo roda, e o destino
    /// deste aplicativo e sempre o Windows.
    /// </remarks>
    private const string ForbiddenCharacters = @"<>:""/\|?*";

    /// <summary>
    /// Nomes que o Windows reserva para dispositivos, com ou sem extensao.
    /// </summary>
    private static readonly string[] ReservedNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    private OutputFileName(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Se o caractere pode aparecer em um nome de arquivo.
    /// </summary>
    /// <remarks>
    /// Existe para que a tela possa recusar a tecla no momento em que ela e
    /// digitada, em vez de alterar o texto depois e mover o cursor do usuario.
    /// </remarks>
    public static bool IsAllowedCharacter(char character) =>
        !char.IsControl(character) && !ForbiddenCharacters.Contains(character);

    /// <summary>
    /// Aproveita o que der do nome pedido.
    /// </summary>
    /// <returns>
    /// Falso quando nao sobra nada utilizavel, caso em que quem chama decide o
    /// que usar no lugar.
    /// </returns>
    public static bool TryCreate(string? candidate, [NotNullWhen(true)] out OutputFileName? fileName)
    {
        fileName = null;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var cleaned = new string([.. candidate.Where(IsAllowedCharacter)]);

        if (cleaned.Length > MaximumLength)
        {
            cleaned = cleaned[..MaximumLength];
        }

        // O Windows descarta ponto e espaco no fim do nome em silencio, o que
        // faria o arquivo gravado nao bater com o nome pedido.
        cleaned = cleaned.TrimEnd(' ', '.').Trim();

        if (cleaned.Length == 0 || IsReserved(cleaned))
        {
            return false;
        }

        fileName = new OutputFileName(cleaned);

        return true;
    }

    /// <remarks>
    /// A reserva vale tambem com extensao: <c>NUL.mp4</c> e tao recusado quanto
    /// <c>NUL</c>.
    /// </remarks>
    private static bool IsReserved(string candidate)
    {
        var withoutExtension = candidate.Split('.')[0];

        return ReservedNames.Contains(withoutExtension, StringComparer.OrdinalIgnoreCase);
    }
}
