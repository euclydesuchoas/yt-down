using YTDown.Application.Common;

namespace YTDown.Application.DTOs;

/// <summary>
/// Um download concluido, como fica registrado para consulta posterior.
/// </summary>
/// <remarks>
/// Guarda apenas o que se sabe no momento em que o download termina. Titulo e
/// canal ficam de fora de proposito: so existem quando o usuario buscou o video
/// antes, e baixar sem buscar e um caminho valido. O nome do arquivo ja e o
/// titulo, escrito pelo proprio yt-dlp, e e por ele que o usuario procura o
/// arquivo no disco.
/// </remarks>
/// <param name="Url">Endereco normalizado, suficiente para baixar de novo.</param>
public sealed record DownloadHistoryEntryDto(
    string Url,
    string FileName,
    string FilePath,
    long SizeInBytes,
    MediaKind Kind,
    DateTimeOffset CompletedAt);
