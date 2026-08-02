using System.Diagnostics;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <inheritdoc cref="IFileExplorer" />
public sealed class WindowsFileExplorer : IFileExplorer
{
    public void RevealFile(string filePath)
    {
        try
        {
            // O Explorer exige a virgula colada em /select e o caminho entre
            // aspas; montar por ArgumentList produz um escapamento que ele
            // interpreta como outro caminho.
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            })?.Dispose();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            // Nao conseguir abrir a pasta e irrelevante diante do download que
            // ja foi concluido: o arquivo esta la, e o caminho e exibido.
        }
    }
}
