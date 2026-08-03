namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Uma linha de progresso do yt-dlp, referente a um único stream.
/// </summary>
/// <param name="IsVideoStream">
/// O YouTube entrega vídeo e áudio separados. O yt-dlp informa o codec de vídeo
/// como <c>none</c> enquanto baixa o áudio, e é assim que os dois se distinguem.
/// </param>
public sealed record YtDlpProgressLine(
    bool IsVideoStream,
    bool IsFinished,
    long DownloadedBytes,
    long TotalBytes,
    double? BytesPerSecond,
    TimeSpan? TimeRemaining);
