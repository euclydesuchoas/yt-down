namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Desfecho da execucao de um processo externo.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
