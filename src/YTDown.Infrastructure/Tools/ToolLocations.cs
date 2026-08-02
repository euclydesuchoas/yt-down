namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Onde cada ferramenta externa vive.
/// </summary>
/// <remarks>
/// Sao dois lugares por necessidade. O yt-dlp precisa se sobrescrever para se
/// atualizar, e nao consegue quando o aplicativo esta instalado em Arquivos de
/// Programas, entao vive em uma pasta do perfil do usuario. O FFmpeg nunca se
/// atualiza e fica junto do aplicativo: copiar cem megabytes na primeira
/// execucao seria uma espera visivel sem ganho algum.
/// </remarks>
public sealed class ToolLocations
{
    private const string ToolsFolderName = "tools";
    private const string ApplicationFolderName = "YTDown";

    public ToolLocations()
        : this(
            Path.Combine(AppContext.BaseDirectory, ToolsFolderName),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationFolderName,
                ToolsFolderName))
    {
    }

    public ToolLocations(string bundledDirectory, string userDirectory)
    {
        BundledDirectory = bundledDirectory;
        UserDirectory = userDirectory;
    }

    /// <summary>Pasta que acompanha a instalacao, somente leitura na pratica.</summary>
    public string BundledDirectory { get; }

    /// <summary>Pasta gravavel no perfil do usuario.</summary>
    public string UserDirectory { get; }
}
