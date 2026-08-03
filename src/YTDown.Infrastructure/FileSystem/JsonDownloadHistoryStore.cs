using System.Text.Json;
using System.Text.Json.Serialization;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <inheritdoc cref="IDownloadHistoryStore" />
/// <remarks>
/// JSON e não um banco: são poucas dezenas de registros, lidos de uma vez só.
/// Um banco embarcado traria arquivo de esquema, migração e mais uma dependência
/// grande no instalador para resolver um problema que este aplicativo não tem.
/// Em texto, o arquivo também pode ser lido e corrigido à mão quando preciso.
/// </remarks>
public sealed class JsonDownloadHistoryStore : IDownloadHistoryStore
{
    private const string HistoryFileName = "history.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public JsonDownloadHistoryStore()
        : this(Path.Combine(UserDataLocation.Root, HistoryFileName))
    {
    }

    public JsonDownloadHistoryStore(string filePath) => _filePath = filePath;

    public async Task<IReadOnlyList<DownloadHistoryEntryDto>> ReadAsync(CancellationToken cancellationToken)
    {
        var entries = await JsonFile.ReadAsync<List<DownloadHistoryEntryDto>>(
            _filePath,
            SerializerOptions,
            cancellationToken);

        return entries ?? [];
    }

    public Task WriteAsync(IReadOnlyList<DownloadHistoryEntryDto> entries, CancellationToken cancellationToken) =>
        JsonFile.WriteAsync(_filePath, entries, SerializerOptions, cancellationToken);
}
