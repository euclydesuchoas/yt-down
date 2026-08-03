using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;

namespace YTDown.Infrastructure.YouTube;

/// <inheritdoc cref="IVideoMetadataProvider" />
public sealed class YtDlpMetadataProvider : IVideoMetadataProvider
{
    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;

    public YtDlpMetadataProvider(IProcessRunner processRunner, IToolLocator toolLocator)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
    }

    public async Task<Result<VideoInfoDto>> GetMetadataAsync(VideoUrl videoUrl, CancellationToken cancellationToken)
    {
        if (!_toolLocator.TryLocate(ExternalTool.YtDlp, out var ytDlpPath))
        {
            return Result<VideoInfoDto>.Failure(
                ErrorCode.ToolNotFound,
                "yt-dlp.exe não foi encontrado na pasta tools.");
        }

        ProcessResult processResult;

        try
        {
            processResult = await _processRunner.RunAsync(
                new ProcessRequest(ytDlpPath, BuildArguments(videoUrl), YtDlpEnvironment.Variables),
                onStandardOutputLine: null,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result<VideoInfoDto>.Failure(ErrorCode.Canceled);
        }
        catch (Exception exception)
        {
            // Este é o limite entre o aplicativo e o sistema operacional: qualquer
            // falha ao iniciar ou ler o processo vira um resultado tipado, para que
            // o usuário receba uma mensagem em vez de o aplicativo encerrar.
            return Result<VideoInfoDto>.Failure(ErrorCode.Unexpected, exception.ToString());
        }

        if (!processResult.Succeeded)
        {
            return Result<VideoInfoDto>.Failure(
                YtDlpErrorClassifier.Classify(processResult.StandardError),
                processResult.StandardError);
        }

        if (!YtDlpVideoInfoParser.TryParse(processResult.StandardOutput, out var videoInfo))
        {
            return Result<VideoInfoDto>.Failure(
                ErrorCode.ToolFailure,
                "A resposta do yt-dlp não pôde ser interpretada.");
        }

        return Result<VideoInfoDto>.Success(videoInfo);
    }

    private static string[] BuildArguments(VideoUrl videoUrl) =>
    [
        "--dump-single-json",
        // A URL já chega normalizada, mas a restrição explícita protege contra
        // qualquer forma de playlist que passe a ser aceita no futuro.
        "--no-playlist",
        "--no-warnings",
        "--no-progress",
        videoUrl.Value
    ];
}
