namespace YTDown.Application.Common;

/// <summary>
/// Motivos de falha que a aplicacao sabe distinguir.
/// </summary>
/// <remarks>
/// A camada de apresentacao traduz cada codigo em uma mensagem para o usuario.
/// Nenhuma saida bruta de ferramenta externa chega a tela.
/// </remarks>
public enum ErrorCode
{
    /// <summary>O texto informado nao identifica um video do YouTube.</summary>
    InvalidUrl,

    /// <summary>O video foi removido, e privado ou nunca existiu.</summary>
    VideoUnavailable,

    /// <summary>O video exige confirmacao de idade.</summary>
    AgeRestricted,

    /// <summary>O video nao esta disponivel na regiao atual.</summary>
    RegionBlocked,

    /// <summary>Falha de rede ao contatar o YouTube.</summary>
    NetworkError,

    /// <summary>A ferramenta externa nao foi encontrada na instalacao.</summary>
    ToolNotFound,

    /// <summary>A ferramenta externa executou e falhou por um motivo nao reconhecido.</summary>
    ToolFailure,

    /// <summary>A operacao foi cancelada pelo usuario.</summary>
    Canceled,

    /// <summary>Falha nao prevista.</summary>
    Unexpected
}
