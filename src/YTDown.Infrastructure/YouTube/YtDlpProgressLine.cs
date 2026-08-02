namespace YTDown.Infrastructure.YouTube;

/// <summary>
/// Uma linha de progresso do yt-dlp, referente a um unico stream.
/// </summary>
/// <param name="IsVideoStream">
/// O YouTube entrega video e audio separados. O yt-dlp informa o codec de video
/// como <c>none</c> enquanto baixa o audio, e e assim que os dois se distinguem.
/// </param>
public sealed record YtDlpProgressLine(
    bool IsVideoStream,
    bool IsFinished,
    long DownloadedBytes,
    long TotalBytes,
    double? BytesPerSecond,
    TimeSpan? TimeRemaining);
