using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Transforma o progresso de cada stream em um único percentual.
/// </summary>
/// <remarks>
/// Sem isto o usuário veria a barra ir a 100% e voltar a zero, porque vídeo e
/// áudio são baixados em sequência, e depois ficaria parada durante a junção.
///
/// As faixas vêm da proporção real de bytes: no vídeo de referência o áudio é
/// pouco mais de 7% do total baixado. O trabalho do FFmpeg no fim não reporta
/// progresso algum, então ocupa uma faixa curta e fixa.
/// </remarks>
public sealed class DownloadProgressAggregator
{
    private const int VideoShare = 90;
    private const int AudioShare = 7;

    /// <summary>Sem vídeo para baixar, a trilha sozinha ocupa quase tudo.</summary>
    private const int AudioOnlyShare = 95;

    private readonly bool _audioOnly;

    private int _highestPercentage;

    public DownloadProgressAggregator(MediaKind kind) => _audioOnly = kind == MediaKind.AudioOnly;

    public DownloadProgressDto ForStream(YtDlpProgressLine line)
    {
        // O último stream terminar é o único aviso de que o FFmpeg começou: as
        // mensagens dos pos-processadores não chegam, porque --print, exigido
        // para saber o caminho final, implica --quiet.
        if (line.IsFinished && (_audioOnly || !line.IsVideoStream))
        {
            return ForFinishing();
        }

        var streamRatio = (double)line.DownloadedBytes / line.TotalBytes;

        if (_audioOnly)
        {
            return Build(
                (int)(streamRatio * AudioOnlyShare),
                DownloadStage.DownloadingAudio,
                line.BytesPerSecond,
                line.TimeRemaining);
        }

        var percentage = line.IsVideoStream
            ? streamRatio * VideoShare
            : VideoShare + streamRatio * AudioShare;

        return Build(
            (int)percentage,
            line.IsVideoStream ? DownloadStage.DownloadingVideo : DownloadStage.DownloadingAudio,
            line.BytesPerSecond,
            line.TimeRemaining);
    }

    public DownloadProgressDto ForCompletion() =>
        Build(100, DownloadStage.Completed, bytesPerSecond: null, timeRemaining: null);

    private DownloadProgressDto ForFinishing() =>
        Build(
            _audioOnly ? AudioOnlyShare : VideoShare + AudioShare,
            DownloadStage.Finishing,
            bytesPerSecond: null,
            timeRemaining: null);

    /// <summary>
    /// Garante que o percentual nunca retroceda, mesmo quando um stream começa
    /// do zero depois de outro ter terminado.
    /// </summary>
    private DownloadProgressDto Build(int percentage, DownloadStage stage, double? bytesPerSecond, TimeSpan? timeRemaining)
    {
        _highestPercentage = Math.Clamp(Math.Max(percentage, _highestPercentage), 0, 100);

        return new DownloadProgressDto(_highestPercentage, stage, bytesPerSecond, timeRemaining);
    }
}
