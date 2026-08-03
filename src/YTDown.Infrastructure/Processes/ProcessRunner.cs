using System.Diagnostics;
using System.Text;

namespace YTDown.Infrastructure.Processes;

/// <inheritdoc cref="IProcessRunner" />
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Títulos de vídeo trazem acentos, ideogramas e emoji. Isto resolve
            // apenas a leitura; instruir o programa a *escrever* em UTF-8 é
            // responsabilidade de quem o conhece, via EnvironmentVariables.
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ArgumentList escapa cada argumento individualmente, evitando os problemas
        // de aspas de uma linha de comando montada por concatenação.
        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var (name, value) in request.EnvironmentVariables)
            {
                startInfo.Environment[name] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // As duas saídas são lidas em paralelo: um buffer cheio trava o processo filho.
        var standardOutput = ReadLinesAsync(process.StandardOutput, onStandardOutputLine, cancellationToken);
        var standardError = ReadLinesAsync(process.StandardError, onLine: null, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TerminateProcessTree(process);
            throw;
        }

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static async Task<string> ReadLinesAsync(
        StreamReader reader,
        Action<string>? onLine,
        CancellationToken cancellationToken)
    {
        var accumulated = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            accumulated.AppendLine(line);
            onLine?.Invoke(line);
        }

        return accumulated.ToString();
    }

    /// <remarks>
    /// O yt-dlp inicia o FFmpeg como processo filho. Encerrar apenas o processo
    /// pai deixaria o FFmpeg rodando e segurando o arquivo de saída.
    /// </remarks>
    private static void TerminateProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // O processo terminou sozinho entre a verificação e o encerramento.
        }
    }
}
