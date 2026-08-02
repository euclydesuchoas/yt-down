namespace YTDown.UI.ViewModels;

/// <summary>
/// Um teto de qualidade oferecido nas configuracoes.
/// </summary>
/// <remarks>
/// Diferente da lista da tela principal, que mostra o que aquele video tem, aqui
/// as opcoes sao fixas: a escolha vale para videos que ainda nem foram colados.
/// </remarks>
public sealed record DefaultQualityOption(int? Height)
{
    public string Label => Height is null ? "A melhor disponivel" : $"Ate {Height}p";
}
