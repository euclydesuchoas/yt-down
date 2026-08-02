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
            "Este endereço não parece ser de um vídeo do YouTube. Copie o endereço da barra do navegador e cole aqui.",

        ErrorCode.VideoUnavailable =>
            "Este vídeo não está disponível. Ele pode ter sido removido ou ser privado.",

        ErrorCode.AgeRestricted =>
            "Este vídeo tem restrição de idade e não pode ser consultado.",

        ErrorCode.RegionBlocked =>
            "Este vídeo não está disponível no seu país.",

        ErrorCode.BotCheckRequired =>
            "O YouTube pediu uma verificação para este acesso. Espere alguns minutos antes de tentar de novo.",

        ErrorCode.DestinationUnavailable =>
            "A pasta escolhida não está mais disponível. Escolha outra pasta e tente de novo.",

        ErrorCode.NetworkError =>
            "Não foi possível conectar ao YouTube. Verifique sua conexão com a internet e tente de novo.",

        ErrorCode.ToolNotFound =>
            "Um componente necessário do YTDown não foi encontrado. Reinstale o aplicativo.",

        ErrorCode.ToolFailure =>
            "Não foi possível obter as informações deste vídeo. Tente de novo em alguns instantes.",

        ErrorCode.Canceled =>
            "Consulta cancelada.",

        _ => "Ocorreu um erro inesperado. Tente de novo."
    };
}
