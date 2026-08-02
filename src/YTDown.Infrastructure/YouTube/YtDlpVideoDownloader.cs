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

        var aggregator = new DownloadProgressAggregator();
        var temporaryFiles = new List<string>();
        string? finalFilePath = null;

        void HandleOutputLine(string line)
        {
            if (YtDlpProgressParser.TryParse(line, out var streamProgress))
            {
                progress.Report(aggregator.ForStream(streamProgress));
            }
            else if (YtDlpProgressParser.TryParsePath(line, YtDlpProgressParser.FinalFilePrefix, out var path))
            {
                finalFilePath = path;
            }
            else if (YtDlpProgressParser.TryParsePath(line, YtDlpProgressParser.DestinationPrefix, out var temporary))
            {
                // Guardado para poder limpar caso o usuario cancele no meio.
                temporaryFiles.Add(temporary);
            }
            else if (line.StartsWith(YtDlpProgressParser.MergingPrefix, StringComparison.Ordinal))
            {
                progress.Report(aggregator.ForMerging());
            }
        }

        ProcessResult processResult;

        try
        {
            Directory.CreateDirectory(destinationDirectory);

            processResult = await _processRunner.RunAsync(
                ytDlpPath,
                BuildArguments(videoUrl, destinationDirectory, ffmpegPath),
                HandleOutputLine,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            DeleteLeftovers(temporaryFiles);

            return Result<DownloadedFileDto>.Failure(ErrorCode.Canceled);
        }
        catch (Exception exception)
        {
            DeleteLeftovers(temporaryFiles);

            return Result<DownloadedFileDto>.Failure(ErrorCode.Unexpected, exception.ToString());
        }

        if (!processResult.Succeeded)
        {
            DeleteLeftovers(temporaryFiles);

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

    private static string[] BuildArguments(VideoUrl videoUrl, string destinationDirectory, string ffmpegPath) =>
    [
        "-f", FormatSelector,
        "--merge-output-format", "mp4",
        "--ffmpeg-location", ffmpegPath,
        "--no-playlist",
        "--no-warnings",
        // Sem isto o progresso vem com retorno de carro e nunca fecha a linha.
        "--newline",
        "--progress-template", YtDlpProgressParser.ProgressTemplate,
        "--print", YtDlpProgressParser.FinalFileTemplate,
        "-o", Path.Combine(destinationDirectory, OutputTemplate),
        videoUrl.Value
    ];

    /// <summary>
    /// Remove os arquivos parciais que o yt-dlp so apaga quando termina bem.
    /// </summary>
    /// <remarks>
    /// O FFmpeg pode levar um instante para soltar o arquivo depois de
    /// encerrado, entao a remocao e tentada mais de uma vez antes de desistir.
    /// </remarks>
    private static void DeleteLeftovers(IReadOnlyList<string> temporaryFiles)
    {
        foreach (var path in temporaryFiles)
        {
            foreach (var candidate in new[] { path, path + ".part", path + ".ytdl" })
            {
                TryDelete(candidate);
            }
        }
    }

    private static void TryDelete(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
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
