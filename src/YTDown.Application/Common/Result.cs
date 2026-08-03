using System.Diagnostics.CodeAnalysis;

namespace YTDown.Application.Common;

/// <summary>
/// Resultado de uma operação que pode falhar por um motivo esperado.
/// </summary>
/// <remarks>
/// Vídeo indisponível e queda de rede são desfechos normais neste aplicativo,
/// não situações excepcionais, então percorrem o código como valor de retorno.
/// Exceção continua reservada para defeito de programação.
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
    /// Detalhe técnico da falha, para registro e depuração.
    /// </summary>
    /// <remarks>Nunca exibido ao usuário: pode conter saída bruta de ferramenta externa.</remarks>
    public string? Diagnostics { get; }

    public static Result<TValue> Success(TValue value) => new(value, null, null);

    public static Result<TValue> Failure(ErrorCode error, string? diagnostics = null) =>
        new(default, error, diagnostics);
}
