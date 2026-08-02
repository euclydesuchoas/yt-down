namespace YTDown.Application.DTOs;

/// <summary>
/// Arquivo resultante de um download concluido.
/// </summary>
public sealed record DownloadedFileDto(string FilePath, string FileName, long SizeInBytes);
