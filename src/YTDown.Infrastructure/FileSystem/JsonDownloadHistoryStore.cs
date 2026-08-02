using System.Text.Json;
using System.Text.Json.Serialization;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <inheritdoc cref="IDownloadHistoryStore" />
/// <remarks>
/// JSON e nao um banco: sao poucas dezenas de registros, lidos de uma vez so.
/// Um banco embarcado traria arquivo de esquema, migracao e mais uma dependencia
/// grande no instalador para resolver um problema que este aplicativo nao tem.
/// Em texto, o arquivo tambem pode ser lido e corrigido a mao quando preciso.
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
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            await using var file = File.OpenRead(_filePath);

            var entries = await JsonSerializer.DeserializeAsync<List<DownloadHistoryEntryDto>>(
                file,
                SerializerOptions,
                cancellationToken);

            return entries ?? [];
        }
        catch (JsonException)
        {
            // Historico ilegivel e um inconveniente, nao um impedimento: perder a
            // lista e aceitavel, deixar o aplicativo sem abrir nao e. O proximo
            // download reescreve o arquivo.
            return [];
        }
    }

    public async Task WriteAsync(
        IReadOnlyList<DownloadHistoryEntryDto> entries,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Escreve ao lado e so entao substitui. Gravar por cima deixaria o
        // historico pela metade se a maquina caisse no meio da escrita.
        var temporaryPath = _filePath + ".tmp";

        await using (var file = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(file, entries, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
