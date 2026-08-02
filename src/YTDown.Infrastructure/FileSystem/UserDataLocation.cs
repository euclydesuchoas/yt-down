namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Pasta do perfil do usuario onde o aplicativo guarda o que e dele.
/// </summary>
/// <remarks>
/// Existe para que a pasta seja decidida em um lugar so. Ja sao dois moradores,
/// as ferramentas e o historico, e ambos precisam de escrita: a pasta da
/// instalacao nao serve, porque em Arquivos de Programas o usuario comum nao
/// escreve.
/// </remarks>
public static class UserDataLocation
{
    private const string ApplicationFolderName = "YTDown";

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName);
}
