using System.Diagnostics.CodeAnalysis;

namespace YTDown.Application.Common;

/// <summary>
/// Resultado de uma operacao que pode falhar por um motivo esperado.
/// </summary>
/// <remarks>
/// Video indisponivel e queda de rede sao desfechos normais neste aplicativo,
/// nao situacoes excepcionais, entao percorrem o codigo como valor de retorno.
/// Excecao continua reservada para defeito de programacao.
/// </remarks>
public sealed class Result<TValue>
{
    private Result(TValue? value, ErrorCode? error, string? diagnostics)
    {
        Value = value;
        Error = error;
        Diagnostics = diagnostics;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public TValue? Value { get; }

    public ErrorCode? Error { get; }

    /// <summary>
    /// Detalhe tecnico da falha, para registro e depuracao.
    /// </summary>
    /// <remarks>Nunca exibido ao usuario: pode conter saida bruta de ferramenta externa.</remarks>
    public string? Diagnostics { get; }

    public static Result<TValue> Success(TValue value) => new(value, null, null);

    public static Result<TValue> Failure(ErrorCode error, string? diagnostics = null) =>
        new(default, error, diagnostics);
}
