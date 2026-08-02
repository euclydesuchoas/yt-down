namespace YTDown.Application.Interfaces;

/// <summary>
/// Diz onde os arquivos baixados devem ser gravados.
/// </summary>
/// <remarks>
/// Existe para manter a Application longe do sistema de arquivos. Hoje devolve
/// sempre a pasta Downloads do usuario; quando houver configuracao, so esta
/// implementacao muda.
/// </remarks>
public interface IDownloadLocationProvider
{
    string GetDestinationDirectory();
}
