namespace YTDown.Application.Interfaces;

/// <summary>
/// Diz onde os arquivos baixados devem ser gravados.
/// </summary>
/// <remarks>
/// Existe para manter a Application longe do sistema de arquivos. Devolve a
/// pasta escolhida pelo usuario, ou a pasta Downloads enquanto nao houver
/// escolha.
/// </remarks>
public interface IDownloadLocationProvider
{
    Task<string> GetDestinationDirectoryAsync(CancellationToken cancellationToken);
}
