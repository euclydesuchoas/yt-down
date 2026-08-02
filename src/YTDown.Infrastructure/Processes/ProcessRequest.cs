namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Descreve a execucao de um programa externo.
/// </summary>
/// <param name="Arguments">
/// Cada argumento separado, nunca uma linha de comando concatenada: o
/// escapamento fica por conta do sistema.
/// </param>
/// <param name="EnvironmentVariables">
/// Variaveis acrescentadas ao ambiente herdado. Necessarias quando a
/// ferramenta precisa ser instruida sobre como se comportar, e nao ha opcao de
/// linha de comando equivalente.
/// </param>
public sealed record ProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null);
