namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma pasta oferecida na hora de baixar.
/// </summary>
/// <param name="Path">
/// Caminho escolhido, ou <c>null</c> para a pasta padrao das configuracoes.
/// </param>
public sealed record DestinationOption(string? Path)
{
    /// <remarks>
    /// O tipo do nulo e explicito porque <c>new(null)</c> serve tanto ao
    /// construtor do registro quanto ao de copia.
    /// </remarks>
    public static DestinationOption Default { get; } = new((string?)null);

    /// <summary>
    /// So o nome da pasta, porque o caminho inteiro nao cabe na linha e a pessoa
    /// reconhece "Roberto Carlos" mais rapido do que le a unidade e as pastas
    /// ate chegar nele. O caminho completo fica na dica da lista.
    /// </summary>
    public string Label => Path is null
        ? "Pasta padrão"
        : System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar)) is { Length: > 0 } name
            ? name
            : Path;
}
