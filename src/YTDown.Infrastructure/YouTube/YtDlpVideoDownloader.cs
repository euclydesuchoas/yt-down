using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.Domain.ValueObjects;
using YTDown.Infrastructure.Processes;
using YTDown.Infrastructure.Tools;

namespace YTDown.Infrastructure.YouTube;

/// <inheritdoc cref="IVideoDownloader" />
public sealed class YtDlpVideoDownloader : IVideoDownloader
{
    /// <summary>
    /// Prefere H.264 com audio AAC, o que permite juntar sem reconverter e gera
    /// um MP4 que abre em qualquer lugar. O YouTube nao serve H.264 acima de
    /// 1080p, entao esta escolha tem esse teto por consequencia, e nao por
    /// limitacao imposta aqui. As alternativas cobrem videos que so existem em
    /// formato unico.
    /// </summary>
    private const string FormatSelector = "bv*[vcodec^=avc1]+ba[acodec^=mp4a]/b[ext=mp4]/b";

    /// <summary>
    /// O titulo entra no nome do arquivo, limitado para nao estourar o caminho
    /// maximo do Windows.
    /// </summary>
    private const string OutputTemplate = "%(title).100s.%(ext)s";

    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;

    public YtDlpVideoDownloader(IProcessRunner processRunner, IToolLocator toolLocator)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
    }

    public async Task<Result<DownloadedFileDto>> DownloadAsync(
        VideoUrl videoUrl,
        string destinationDirectory,
        IProgress<DownloadProgressDto> progress,
        CancellationToken cancellationToken)
    {
        if (!_toolLocator.TryLocate(ExternalTool.YtDlp, out var ytDlpPath) ||
            !_toolLocator.TryLocate(ExternalTool.FFmpeg, out var ffmpegPath))
        {
            return Result<DownloadedFileDto>.Failure(
                ErrorCode.ToolNotFound,
                "yt-dlp.exe ou ffmpeg.exe nao foi encontrado na pasta tools.");
        }

        // Os arquivos intermediarios ficam isolados em uma pasta propria, dentro
        // do destino para que a mudanca final seja no mesmo volume. Assim a
        // limpeza e apagar a pasta inteira, sem precisar adivinhar nomes.
        var workDirectory = Path.Combine(destinationDirectory, $".ytdown-{Guid.NewGuid():N}");

        var aggregator = new DownloadProgressAggregator();
        string? finalFilePath = null;

        void HandleOutputLine(string line)
        {
            if (YtDlpProgressParser.TryParse(line, out var streamProgress))
            {
                progress.Report(aggregator.ForStream(streamProgress));
            }
            else if (YtDlpProgressParser.TryParseFinalFilePath(line, out var path))
            {
                finalFilePath = path;
            }
            else if (line.StartsWith(YtDlpProgressParser.MergingPrefix, StringComparison.Ordinal))
            {
                progress.Report(aggregator.ForMerging());
            }
        }

        try
        {
            Directory.CreateDirectory(workDirectory);

            ProcessResult processResult;

            try
            {
                processResult = await _processRunner.RunAsync(
                    new ProcessRequest(
                        ytDlpPath,
                        BuildArguments(videoUrl, destinationDirectory, workDirectory, ffmpegPath),
                        YtDlpEnvironment.Variables),
                    HandleOutputLine,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Result<DownloadedFileDto>.Failure(ErrorCode.Canceled);
            }

            if (!processResult.Succeeded)
            {
                return Result<DownloadedFileDto>.Failure(
                    YtDlpErrorClassifier.Classify(processResult.StandardError),
                    processResult.StandardError);
            }

            if (finalFilePath is null || !File.Exists(finalFilePath))
            {
                return Result<DownloadedFileDto>.Failure(
                    ErrorCode.ToolFailure,
                    $"O yt-dlp terminou sem indicar o arquivo final. Saida: {processResult.StandardOutput}");
            }

            progress.Report(aggregator.ForCompletion());

            var file = new FileInfo(finalFilePath);

            return Result<DownloadedFileDto>.Success(new DownloadedFileDto(file.FullName, file.Name, file.Length));
        }
        catch (Exception exception)
        {
            // Este e o limite entre o aplicativo e o sistema operacional: qualquer
            // falha vira resultado tipado, para que o usuario receba uma mensagem
            // em vez de o aplicativo encerrar.
            return Result<DownloadedFileDto>.Failure(ErrorCode.Unexpected, exception.ToString());
        }
        finally
        {
            DeleteWorkDirectory(workDirectory);
        }
    }

    private static string[] BuildArguments(
        VideoUrl videoUrl,
        string destinationDirectory,
        string workDirectory,
        string ffmpegPath) =>
    [
        "-f", FormatSelector,
        "--merge-output-format", "mp4",
        "--ffmpeg-location", ffmpegPath,
        "--no-playlist",
        "--no-warnings",
        // Sem isto o progresso vem com retorno de carro e nunca fecha a linha.
        "--newline",
        // --print implica --quiet, que silencia o progresso. Este argumento o
        // traz de volta, e sem ele a barra so se moveria ao terminar.
        "--progress",
        "--progress-template", YtDlpProgressParser.ProgressTemplate,
        "--print", YtDlpProgressParser.FinalFileTemplate,
        "--paths", $"home:{destinationDirectory}",
        "--paths", $"temp:{workDirectory}",
        "-o", OutputTemplate,
        videoUrl.Value
    ];

    /// <remarks>
    /// O FFmpeg pode levar um instante para soltar os arquivos depois de
    /// encerrado, entao a remocao e tentada mais de uma vez antes de desistir.
    /// </remarks>
    private static void DeleteWorkDirectory(string workDirectory)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (Directory.Exists(workDirectory))
                {
                    Directory.Delete(workDirectory, recursive: true);
                }

                return;
            }
            catch (IOException)
            {
                Thread.Sleep(150);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
