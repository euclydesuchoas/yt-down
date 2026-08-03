namespace YTDown.Domain.Exceptions;

/// <summary>
/// Lançada quando um texto não representa um vídeo do YouTube.
/// </summary>
/// <remarks>
/// Usada apenas por <c>VideoUrl.Create</c>, que assume entrada já confiável.
/// Entrada vinda do usuário deve passar por <c>VideoUrl.TryCreate</c>.
/// </remarks>
public sealed class InvalidVideoUrlException : Exception
{
    public InvalidVideoUrlException(string? candidate)
        : base($"'{candidate}' não é uma URL de vídeo do YouTube válida.")
    {
        Candidate = candidate;
    }

    public string? Candidate { get; }
}
