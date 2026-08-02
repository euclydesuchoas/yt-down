using System.Diagnostics.CodeAnalysis;

namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Procura as ferramentas na pasta <c>tools</c> ao lado do executavel.
/// </summary>
public sealed class LocalToolLocator : IToolLocator
{
    private const string ToolsFolderName = "tools";

    private static readonly IReadOnlyDictionary<ExternalTool, string> ExecutableNames =
        new Dictionary<ExternalTool, string>
        {
            [ExternalTool.YtDlp] = "yt-dlp.exe",
            [ExternalTool.FFmpeg] = "ffmpeg.exe"
        };

    private readonly string _toolsDirectory;

    public LocalToolLocator()
        : this(Path.Combine(AppContext.BaseDirectory, ToolsFolderName))
    {
    }

    public LocalToolLocator(string toolsDirectory)
    {
        _toolsDirectory = toolsDirectory;
    }

    public bool TryLocate(ExternalTool tool, [NotNullWhen(true)] out string? executablePath)
    {
        executablePath = null;

        if (!ExecutableNames.TryGetValue(tool, out var executableName))
        {
            return false;
        }

        var candidate = Path.Combine(_toolsDirectory, executableName);

        if (!File.Exists(candidate))
        {
            return false;
        }

        executablePath = candidate;
        return true;
    }
}
