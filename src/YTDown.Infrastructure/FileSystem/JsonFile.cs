using System.Text.Json;

namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Lê e escreve um arquivo JSON no perfil do usuário.
/// </summary>
/// <remarks>
/// O aplicativo guarda mais de uma coisa em disco, e todas com as mesmas
/// exigências: sobreviver a uma queda no meio da escrita e não impedir a
/// abertura quando o arquivo estiver ilegível.
/// </remarks>
internal static class JsonFile
{
    /// <summary>
    /// Lê o arquivo, ou devolve <c>null</c> quando ele não existe ou não pode
    /// ser interpretado.
    /// </summary>
    /// <remarks>
    /// Arquivo corrompido não levanta exceção de propósito: perder o que estava
    /// guardado é aceitável, deixar o aplicativo sem abrir não é. Quem chama
    /// decide o que colocar no lugar.
    /// </remarks>
    public static async Task<T?> ReadAsync<T>(
        string path,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            await using var file = File.OpenRead(path);

            return await JsonSerializer.DeserializeAsync<T>(file, options, cancellationToken);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static async Task WriteAsync<T>(
        string path,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Escreve ao lado e só então substitui. Gravar por cima deixaria o
        // arquivo pela metade se a máquina caísse no meio da escrita.
        var temporaryPath = path + ".tmp";

        await using (var file = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(file, value, options, cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}
