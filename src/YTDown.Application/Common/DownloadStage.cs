namespace YTDown.Application.Common;

/// <summary>
/// Etapa atual de um download.
/// </summary>
/// <remarks>
/// O YouTube entrega vídeo e áudio como dois arquivos separados, baixados em
/// sequência e depois unidos pelo FFmpeg. O usuário não precisa saber disso,
/// mas precisa entender por que a espera continua depois que o download
/// aparentemente terminou.
/// </remarks>
public enum DownloadStage
{
    DownloadingVideo,
    DownloadingAudio,

    /// <summary>
    /// O download acabou e o FFmpeg ainda trabalha: unindo vídeo e áudio, ou
    /// convertendo a trilha para MP3.
    /// </summary>
    Finishing,

    Completed
}
