using System.Runtime.InteropServices;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Devolve a pasta Downloads do usuario.
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

    public string GetDestinationDirectory() =>
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
