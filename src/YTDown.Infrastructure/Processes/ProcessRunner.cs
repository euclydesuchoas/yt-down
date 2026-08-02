using System.Diagnostics;
using System.Text;

namespace YTDown.Infrastructure.Processes;

/// <inheritdoc cref="IProcessRunner" />
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? onStandardOutputLine,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Titulos de video trazem acentos e emoji; sem isto a saida chega corrompida.
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ArgumentList escapa cada argumento individualmente, evitando os problemas
        // de aspas de uma linha de comando montada por concatenacao.
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // As duas saidas sao lidas em paralelo: um buffer cheio trava o processo filho.
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
    /// pai deixaria o FFmpeg rodando e segurando o arquivo de saida.
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
            // O processo terminou sozinho entre a verificacao e o encerramento.
        }
    }
}
