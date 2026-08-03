using System.Text.RegularExpressions;
using YTDown.Application.Common;
using YTDown.Application.Interfaces;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.YouTube;

namespace YTDown.Infrastructure.Tools;

/// <inheritdoc cref="IToolUpdater" />
public sealed partial class YtDlpUpdater : IToolUpdater
{
    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;

    public YtDlpUpdater(IProcessRunner processRunner, IToolLocator toolLocator)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
    }

    public async Task<Result<string>> UpdateAsync(CancellationToken cancellationToken)
    {
        if (!_toolLocator.TryLocate(ExternalTool.YtDlp, out var ytDlpPath))
        {
            return Result<string>.Failure(ErrorCode.ToolNotFound, "yt-dlp.exe não foi encontrado.");
        }

        ProcessResult processResult;

        try
        {
            processResult = await _processRunner.RunAsync(
                new ProcessRequest(ytDlpPath, ["-U"], YtDlpEnvironment.Variables),
                onStandardOutputLine: null,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Failure(ErrorCode.Canceled);
        }
        catch (Exception exception)
        {
            return Result<string>.Failure(ErrorCode.Unexpected, exception.ToString());
        }

        if (!processResult.Succeeded)
        {
            // Ficar sem atualizar não impede o uso: é quase sempre falta de rede.
            return Result<string>.Failure(ErrorCode.NetworkError, processResult.StandardError);
        }

        return TryReadVersion(processResult.StandardOutput, out var version)
            ? Result<string>.Success(version)
            : Result<string>.Failure(ErrorCode.ToolFailure, processResult.StandardOutput);
    }

    /// <summary>
    /// Extrai a versão da saída do yt-dlp.
    /// </summary>
    /// <remarks>
    /// O padrão <c>stable@versão</c> aparece tanto quando há atualização quanto
    /// quando já está em dia, então serve para os dois casos.
    /// </remarks>
    public static bool TryReadVersion(string? output, out string version)
    {
        version = string.Empty;

        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        var match = VersionPattern().Match(output);

        if (!match.Success)
        {
            return false;
        }

        version = match.Groups[1].Value;
        return true;
    }

    [GeneratedRegex(@"stable@([0-9][\w.\-]*)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionPattern();
}
