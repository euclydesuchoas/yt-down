namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Descreve a execução de um programa externo.
/// </summary>
/// <param name="Arguments">
/// Cada argumento separado, nunca uma linha de comando concatenada: o
/// escapamento fica por conta do sistema.
/// </param>
/// <param name="EnvironmentVariables">
/// Variáveis acrescentadas ao ambiente herdado. Necessárias quando a
/// ferramenta precisa ser instruída sobre como se comportar, e não há opção de
/// linha de comando equivalente.
/// </param>
public sealed record ProcessRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null);
