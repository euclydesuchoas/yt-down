namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Pasta do perfil do usuário onde o aplicativo guarda o que é dele.
/// </summary>
/// <remarks>
/// Existe para que a pasta seja decidida em um lugar só. Já são dois moradores,
/// as ferramentas e o histórico, e ambos precisam de escrita: a pasta da
/// instalação não serve, porque em Arquivos de Programas o usuário comum não
/// escreve.
/// </remarks>
public static class UserDataLocation
{
    private const string ApplicationFolderName = "YTDown";

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName);
}
