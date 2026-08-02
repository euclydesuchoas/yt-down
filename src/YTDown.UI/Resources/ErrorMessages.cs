using YTDown.Application.Common;

namespace YTDown.UI.Resources;

/// <summary>
/// Traduz um motivo de falha em uma frase para o usuario.
/// </summary>
/// <remarks>
/// Cada mensagem diz o que aconteceu e, quando existe, o que fazer a seguir.
/// O publico do aplicativo nao sabe o que e yt-dlp, entao nenhuma saida tecnica
/// aparece na tela: ela fica em Diagnostics, para depuracao.
/// </remarks>
internal static class ErrorMessages
{
    public static string For(ErrorCode error) => error switch
    {
        ErrorCode.InvalidUrl =>
            "Este endereco nao parece ser de um video do YouTube. Copie o endereco da barra do navegador e cole aqui.",

        ErrorCode.VideoUnavailable =>
            "Este video nao esta disponivel. Ele pode ter sido removido ou ser privado.",

        ErrorCode.AgeRestricted =>
            "Este video tem restricao de idade e nao pode ser consultado.",

        ErrorCode.RegionBlocked =>
            "Este video nao esta disponivel no seu pais.",

        ErrorCode.BotCheckRequired =>
            "O YouTube pediu uma verificacao para este acesso. Espere alguns minutos antes de tentar de novo.",

        ErrorCode.NetworkError =>
            "Nao foi possivel conectar ao YouTube. Verifique sua conexao com a internet e tente de novo.",

        ErrorCode.ToolNotFound =>
            "Um componente necessario do YTDown nao foi encontrado. Reinstale o aplicativo.",

        ErrorCode.ToolFailure =>
            "Nao foi possivel obter as informacoes deste video. Tente de novo em alguns instantes.",

        ErrorCode.Canceled =>
            "Consulta cancelada.",

        _ => "Ocorreu um erro inesperado. Tente de novo."
    };
}
