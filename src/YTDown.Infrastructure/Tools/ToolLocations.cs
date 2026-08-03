using YTDown.Infrastructure.FileSystem;

namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Onde cada ferramenta externa vive.
/// </summary>
/// <remarks>
/// São dois lugares por necessidade. O yt-dlp precisa se sobrescrever para se
/// atualizar, e não consegue quando o aplicativo está instalado em Arquivos de
/// Programas, então vive em uma pasta do perfil do usuário. O FFmpeg nunca se
/// atualiza e fica junto do aplicativo: copiar cem megabytes na primeira
/// execução seria uma espera visível sem ganho algum.
/// </remarks>
public sealed class ToolLocations
{
    private const string ToolsFolderName = "tools";

    public ToolLocations()
        : this(
            Path.Combine(AppContext.BaseDirectory, ToolsFolderName),
            Path.Combine(UserDataLocation.Root, ToolsFolderName))
    {
    }

    public ToolLocations(string bundledDirectory, string userDirectory)
    {
        BundledDirectory = bundledDirectory;
        UserDirectory = userDirectory;
    }

    /// <summary>Pasta que acompanha a instalação, somente leitura na prática.</summary>
    public string BundledDirectory { get; }

    /// <summary>Pasta gravável no perfil do usuário.</summary>
    public string UserDirectory { get; }
}
