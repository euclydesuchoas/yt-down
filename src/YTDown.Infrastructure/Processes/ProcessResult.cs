namespace YTDown.Infrastructure.Processes;

/// <summary>
/// Desfecho da execução de um processo externo.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
