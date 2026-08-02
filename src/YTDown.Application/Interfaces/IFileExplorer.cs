namespace YTDown.Application.Interfaces;

/// <summary>
/// Mostra um arquivo ao usuario no gerenciador de arquivos do sistema.
/// </summary>
/// <remarks>
/// Existe para que a apresentacao possa oferecer "abrir a pasta" sem iniciar
/// processo algum, o que e proibido naquela camada.
/// </remarks>
public interface IFileExplorer
{
    void RevealFile(string filePath);
}
