namespace YTDown.Application.Common;

/// <summary>
/// Motivos de falha que a aplicação sabe distinguir.
/// </summary>
/// <remarks>
/// A camada de apresentação traduz cada código em uma mensagem para o usuário.
/// Nenhuma saída bruta de ferramenta externa chega à tela.
/// </remarks>
public enum ErrorCode
{
    /// <summary>O texto informado não identifica um vídeo do YouTube.</summary>
    InvalidUrl,

    /// <summary>O vídeo foi removido, é privado ou nunca existiu.</summary>
    VideoUnavailable,

    /// <summary>O vídeo exige confirmação de idade.</summary>
    AgeRestricted,

    /// <summary>O vídeo não está disponível na região atual.</summary>
    RegionBlocked,

    /// <summary>
    /// O YouTube exigiu verificação antes de liberar o acesso.
    /// </summary>
    /// <remarks>
    /// Costuma acontecer depois de muitos downloads seguidos do mesmo endereço
    /// de rede, e passa sozinho. Insistir imediatamente só prolonga o bloqueio,
    /// então a mensagem ao usuário pede espera em vez de nova tentativa.
    /// </remarks>
    BotCheckRequired,

    /// <summary>Falha de rede ao contatar o YouTube.</summary>
    NetworkError,

    /// <summary>A pasta escolhida para salvar não existe mais.</summary>
    /// <remarks>
    /// Acontece com pasta apagada, pendrive removido ou unidade de rede fora do
    /// ar. Só vale para a pasta escolhida à mão: quando o destino é o padrão,
    /// cair para a pasta Downloads é melhor que recusar o download.
    /// </remarks>
    DestinationUnavailable,

    /// <summary>A ferramenta externa não foi encontrada na instalação.</summary>
    ToolNotFound,

    /// <summary>A ferramenta externa executou e falhou por um motivo não reconhecido.</summary>
    ToolFailure,

    /// <summary>A operação foi cancelada pelo usuário.</summary>
    Canceled,

    /// <summary>Falha não prevista.</summary>
    Unexpected
}
