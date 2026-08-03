namespace YTDown.Application.Interfaces;

/// <summary>
/// Mostra um arquivo ao usuário no gerenciador de arquivos do sistema.
/// </summary>
/// <remarks>
/// Existe para que a apresentação possa oferecer "abrir a pasta" sem iniciar
/// processo algum, o que é proibido naquela camada.
/// </remarks>
public interface IFileExplorer
{
    void RevealFile(string filePath);
}
