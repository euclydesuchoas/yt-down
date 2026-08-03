using System.Diagnostics.CodeAnalysis;

namespace YTDown.Domain.ValueObjects;

/// <summary>
/// Nome de arquivo aceitável para o sistema, sem extensão.
/// </summary>
/// <remarks>
/// O usuário digita o nome que quiser, e boa parte do que ele digita o Windows
/// recusa. Deixar isso chegar ao yt-dlp produziria uma falha que ninguém
/// entende; corrigir aqui produz um nome parecido com o pedido.
/// </remarks>
public sealed record OutputFileName
{
    /// <summary>
    /// Limite de caracteres do nome.
    /// </summary>
    /// <remarks>
    /// Não é limite do sistema, e sim margem: o caminho completo do Windows para
    /// em 260 caracteres, e a pasta escolhida pelo usuário pode já ser funda.
    /// </remarks>
    public const int MaximumLength = 100;

    /// <summary>
    /// Caracteres que o Windows recusa em nome de arquivo.
    /// </summary>
    /// <remarks>
    /// Escritos à mão em vez de <c>Path.GetInvalidFileNameChars</c>: a lista
    /// daquele método muda conforme o sistema onde o código roda, e o destino
    /// deste aplicativo é sempre o Windows.
    /// </remarks>
    private const string ForbiddenCharacters = @"<>:""/\|?*";

    /// <summary>
    /// Nomes que o Windows reserva para dispositivos, com ou sem extensão.
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
    /// Existe para que a tela possa recusar a tecla no momento em que ela é
    /// digitada, em vez de alterar o texto depois e mover o cursor do usuário.
    /// </remarks>
    public static bool IsAllowedCharacter(char character) =>
        !char.IsControl(character) && !ForbiddenCharacters.Contains(character);

    /// <summary>
    /// Aproveita o que der do nome pedido.
    /// </summary>
    /// <returns>
    /// Falso quando não sobra nada utilizável, caso em que quem chama decide o
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

        // O Windows descarta ponto e espaço no fim do nome em silêncio, o que
        // faria o arquivo gravado não bater com o nome pedido.
        cleaned = cleaned.TrimEnd(' ', '.').Trim();

        if (cleaned.Length == 0 || IsReserved(cleaned))
        {
            return false;
        }

        fileName = new OutputFileName(cleaned);

        return true;
    }

    /// <remarks>
    /// A reserva vale também com extensão: <c>NUL.mp4</c> é tão recusado quanto
    /// <c>NUL</c>.
    /// </remarks>
    private static bool IsReserved(string candidate)
    {
        var withoutExtension = candidate.Split('.')[0];

        return ReservedNames.Contains(withoutExtension, StringComparer.OrdinalIgnoreCase);
    }
}
