using YTDown.Application.Common;
using YTDown.Application.DTOs;

namespace YTDown.UI.Resources;

/// <summary>
/// Descreve o andamento de um download em linguagem comum.
/// </summary>
/// <remarks>
/// O usuario nao precisa saber que video e audio chegam separados, mas precisa
/// entender que a espera continua depois que o download parece ter acabado.
/// Por isso a etapa de juncao aparece como "Finalizando", e nao com o nome da
/// ferramenta.
/// </remarks>
internal static class DownloadProgressText
{
    private const double BytesPerMegabyte = 1024d * 1024d;

    public static string For(DownloadProgressDto progress)
    {
        var parts = new List<string> { $"{StageOf(progress.Stage)} — {progress.Percentage}%" };

        if (progress.BytesPerSecond is > 0)
        {
            parts.Add($"{progress.BytesPerSecond.Value / BytesPerMegabyte:0.0} MB/s");
        }

        if (RemainingTimeOf(progress.TimeRemaining) is { } remaining)
        {
            parts.Add(remaining);
        }

        return string.Join("   ·   ", parts);
    }

    private static string StageOf(DownloadStage stage) => stage switch
    {
        DownloadStage.DownloadingVideo => "Baixando o video",
        DownloadStage.DownloadingAudio => "Baixando o audio",
        DownloadStage.Finishing => "Finalizando",
        DownloadStage.Completed => "Concluido",
        _ => "Baixando"
    };

    /// <summary>
    /// Estimativas de poucos segundos mudam a cada instante e so poluem a tela.
    /// </summary>
    private static string? RemainingTimeOf(TimeSpan? timeRemaining) => timeRemaining switch
    {
        null => null,
        { TotalSeconds: < 5 } => null,
        { TotalMinutes: < 1 } value => $"faltam {value.TotalSeconds:0} segundos",
        { TotalMinutes: < 60 } value => $"faltam {value.TotalMinutes:0} minutos",
        _ => "falta mais de uma hora"
    };
}
