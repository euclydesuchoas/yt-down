using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// Um download concluído, como fica registrado para consulta posterior.
/// </summary>
/// <remarks>
/// Guarda apenas o que se sabe no momento em que o download termina. Título e
/// canal ficam de fora de propósito: só existem quando o usuário buscou o vídeo
/// antes, e baixar sem buscar é um caminho válido. O nome do arquivo já é o
/// título, escrito pelo próprio yt-dlp, e é por ele que o usuário procura o
/// arquivo no disco.
/// </remarks>
/// <param name="Url">Endereço normalizado, suficiente para baixar de novo.</param>
public sealed record DownloadHistoryEntryDto(
    string Url,
    string FileName,
    string FilePath,
    long SizeInBytes,
    MediaKind Kind,
    DateTimeOffset CompletedAt);
