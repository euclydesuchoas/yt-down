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

    /// <summary>
    /// Se a pasta ainda pode receber um download.
    /// </summary>
    /// <remarks>
    /// Existe para que a Application saiba responder sobre uma pasta sem
    /// conhecer o sistema de arquivos. Uma pasta escolhida a mao pode ter
    /// desaparecido entre a escolha e o download.
    /// </remarks>
    bool Exists(string directory);
}
