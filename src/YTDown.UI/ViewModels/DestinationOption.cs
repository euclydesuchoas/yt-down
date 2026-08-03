namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma pasta oferecida na hora de baixar.
/// </summary>
/// <param name="Path">
/// Caminho escolhido, ou <c>null</c> para a pasta padrão das configurações.
/// </param>
public sealed record DestinationOption(string? Path)
{
    /// <remarks>
    /// O tipo do nulo é explícito porque <c>new(null)</c> serve tanto ao
    /// construtor do registro quanto ao de cópia.
    /// </remarks>
    public static DestinationOption Default { get; } = new((string?)null);

    /// <summary>
    /// Só o nome da pasta, porque o caminho inteiro não cabe na linha e a pessoa
    /// reconhece "Roberto Carlos" mais rápido do que lê a unidade e as pastas
    /// até chegar nele. O caminho completo fica na dica da lista.
    /// </summary>
    public string Label => Path is null
        ? "Pasta padrão"
        : System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar)) is { Length: > 0 } name
            ? name
            : Path;
}
