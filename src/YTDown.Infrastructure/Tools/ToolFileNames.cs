namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Nome do executavel de cada ferramenta.
/// </summary>
public static class ToolFileNames
{
    public static string For(ExternalTool tool) => tool switch
    {
        ExternalTool.YtDlp => "yt-dlp.exe",
        ExternalTool.FFmpeg => "ffmpeg.exe",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Ferramenta desconhecida.")
    };
}
