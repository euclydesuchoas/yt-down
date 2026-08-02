namespace YTDown.Domain.Exceptions;

/// <summary>
/// Lancada quando um texto nao representa um video do YouTube.
/// </summary>
/// <remarks>
/// Usada apenas por <c>VideoUrl.Create</c>, que assume entrada ja confiavel.
/// Entrada vinda do usuario deve passar por <c>VideoUrl.TryCreate</c>.
/// </remarks>
public sealed class InvalidVideoUrlException : Exception
{
    public InvalidVideoUrlException(string? candidate)
        : base($"'{candidate}' nao e uma URL de video do YouTube valida.")
    {
        Candidate = candidate;
    }

    public string? Candidate { get; }
}
