namespace YTDown.Application.Common;

/// <summary>
/// Etapa atual de um download.
/// </summary>
/// <remarks>
/// O YouTube entrega video e audio como dois arquivos separados, baixados em
/// sequencia e depois unidos pelo FFmpeg. O usuario nao precisa saber disso,
/// mas precisa entender por que a espera continua depois que o download
/// aparentemente terminou.
/// </remarks>
public enum DownloadStage
{
    DownloadingVideo,
    DownloadingAudio,
    Merging,
    Completed
}
