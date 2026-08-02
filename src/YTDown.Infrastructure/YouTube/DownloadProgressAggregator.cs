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
/// pouco mais de 7% do total baixado. A juncao nao reporta progresso algum,
/// entao ocupa uma faixa curta e fixa no fim.
/// </remarks>
public sealed class DownloadProgressAggregator
{
    private const int VideoShare = 90;
    private const int AudioShare = 7;
    private const int MergingPercentage = VideoShare + AudioShare;

    private int _highestPercentage;

    public DownloadProgressDto ForStream(YtDlpProgressLine line)
    {
        var streamRatio = (double)line.DownloadedBytes / line.TotalBytes;

        var percentage = line.IsVideoStream
            ? streamRatio * VideoShare
            : VideoShare + streamRatio * AudioShare;

        return Build(
            (int)percentage,
            line.IsVideoStream ? DownloadStage.DownloadingVideo : DownloadStage.DownloadingAudio,
            line.BytesPerSecond,
            line.TimeRemaining);
    }

    public DownloadProgressDto ForMerging() =>
        Build(MergingPercentage, DownloadStage.Merging, bytesPerSecond: null, timeRemaining: null);

    public DownloadProgressDto ForCompletion() =>
        Build(100, DownloadStage.Completed, bytesPerSecond: null, timeRemaining: null);

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
