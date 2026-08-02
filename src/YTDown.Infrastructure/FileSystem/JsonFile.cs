using System.Text.Json;

namespace YTDown.Infrastructure.FileSystem;

/// <summary>
/// Le e escreve um arquivo JSON no perfil do usuario.
/// </summary>
/// <remarks>
/// O aplicativo guarda mais de uma coisa em disco, e todas com as mesmas
/// exigencias: sobreviver a uma queda no meio da escrita e nao impedir a
/// abertura quando o arquivo estiver ilegivel.
/// </remarks>
internal static class JsonFile
{
    /// <summary>
    /// Le o arquivo, ou devolve <c>null</c> quando ele nao existe ou nao pode
    /// ser interpretado.
    /// </summary>
    /// <remarks>
    /// Arquivo corrompido nao levanta excecao de proposito: perder o que estava
    /// guardado e aceitavel, deixar o aplicativo sem abrir nao e. Quem chama
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

        // Escreve ao lado e so entao substitui. Gravar por cima deixaria o
        // arquivo pela metade se a maquina caisse no meio da escrita.
        var temporaryPath = path + ".tmp";

        await using (var file = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(file, value, options, cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}
