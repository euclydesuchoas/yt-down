using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Transforma o progresso de cada stream em um unico percentual.
/// </summary>
/// <remarks>
/// Sem isto o usuario veria a barra ir a 100% e voltar a zero, porque video e
/// audio sao baixados em sequencia, e depois ficaria parada durante a juncao.
///
/// As faixas vem da proporcao real de bytes: no video de referencia o audio e
/// pouco mais de 7% do total baixado. O trabalho do FFmpeg no fim nao reporta
/// progresso algum, entao ocupa uma faixa curta e fixa.
/// </remarks>
public sealed class DownloadProgressAggregator
{
    private const int VideoShare = 90;
    private const int AudioShare = 7;

    /// <summary>Sem video para baixar, a trilha sozinha ocupa quase tudo.</summary>
    private const int AudioOnlyShare = 95;

    private readonly bool _audioOnly;

    private int _highestPercentage;

    public DownloadProgressAggregator(MediaKind kind) => _audioOnly = kind == MediaKind.AudioOnly;

    public DownloadProgressDto ForStream(YtDlpProgressLine line)
    {
        // O ultimo stream terminar e o unico aviso de que o FFmpeg comecou: as
        // mensagens dos pos-processadores nao chegam, porque --print, exigido
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
    /// Garante que o percentual nunca retroceda, mesmo quando um stream comeca
    /// do zero depois de outro ter terminado.
    /// </summary>
    private DownloadProgressDto Build(int percentage, DownloadStage stage, double? bytesPerSecond, TimeSpan? timeRemaining)
    {
        _highestPercentage = Math.Clamp(Math.Max(percentage, _highestPercentage), 0, 100);

        return new DownloadProgressDto(_highestPercentage, stage, bytesPerSecond, timeRemaining);
    }
}
