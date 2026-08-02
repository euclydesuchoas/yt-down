using System.Runtime.InteropServices;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Devolve a pasta escolhida pelo usuario, ou a pasta Downloads.
/// </summary>
/// <remarks>
/// Nao existe <c>SpecialFolder.Downloads</c> no .NET, e supor
/// <c>%USERPROFILE%\Downloads</c> ignora que a pasta pode ter sido movida para
/// outro disco ou para o OneDrive, o que e comum. Por isso a consulta vai ao
/// Windows, com essa suposicao apenas como ultimo recurso.
/// </remarks>
public sealed class WindowsDownloadLocationProvider : IDownloadLocationProvider
{
    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    private readonly ISettingsService _settings;

    public WindowsDownloadLocationProvider(ISettingsService settings) => _settings = settings;

    /// <remarks>
    /// A pasta escolhida pode ter deixado de existir: pendrive removido, unidade
    /// de rede fora do ar, pasta apagada. Nesse caso o download vai para
    /// Downloads em vez de falhar, porque um arquivo em lugar diferente do
    /// esperado ainda e melhor que nenhum arquivo.
    /// </remarks>
    public async Task<string> GetDestinationDirectoryAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);

        return settings.DestinationDirectory is { Length: > 0 } chosen && Directory.Exists(chosen)
            ? chosen
            : DownloadsDirectory();
    }

    public bool Exists(string directory) =>
        !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);

    private static string DownloadsDirectory() =>
        TryGetKnownFolderPath(DownloadsFolderId, out var path)
            ? path
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    private static bool TryGetKnownFolderPath(Guid folderId, out string path)
    {
        path = string.Empty;
        var buffer = IntPtr.Zero;

        try
        {
            if (SHGetKnownFolderPath(in folderId, dwFlags: 0, hToken: IntPtr.Zero, out buffer) != 0)
            {
                return false;
            }

            path = Marshal.PtrToStringUni(buffer) ?? string.Empty;

            return path.Length > 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(buffer);
            }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(in Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
