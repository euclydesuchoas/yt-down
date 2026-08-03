namespace YTDown.UI.ViewModels;

/// <summary>
/// Um teto de qualidade oferecido nas configurações.
/// </summary>
/// <remarks>
/// Diferente da lista da tela principal, que mostra o que aquele vídeo tem, aqui
/// as opções são fixas: a escolha vale para vídeos que ainda nem foram colados.
/// </remarks>
public sealed record DefaultQualityOption(int? Height)
{
    public string Label => Height is null ? "A melhor disponível" : $"Até {Height}p";
}
