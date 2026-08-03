using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Application.Services;

/// <inheritdoc cref="IDownloadHistoryService" />
public sealed class DownloadHistoryService : IDownloadHistoryService
{
    /// <summary>
    /// Quantos downloads o histórico lembra.
    /// </summary>
    /// <remarks>
    /// O histórico serve para reencontrar o que foi baixado há pouco. Passando
    /// de algumas dezenas ele deixa de responder a essa pergunta e vira uma
    /// lista que ninguém lê, então os mais antigos saem.
    /// </remarks>
    private const int MaximumEntries = 50;

    private readonly IDownloadHistoryStore _store;

    // Ler, alterar e gravar são três passos sobre o mesmo arquivo. Hoje só há um
    // download por vez, mas a tela também lê a lista, e nada garante que os dois
    // não se cruzem.
    private readonly SemaphoreSlim _access = new(1, 1);

    public DownloadHistoryService(IDownloadHistoryStore store) => _store = store;

    public async Task<IReadOnlyList<DownloadHistoryEntryDto>> GetRecentAsync(CancellationToken cancellationToken)
    {
        await _access.WaitAsync(cancellationToken);

        try
        {
            return await _store.ReadAsync(cancellationToken);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            return [];
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetRecentFoldersAsync(
        int maximum,
        CancellationToken cancellationToken)
    {
        var entries = await GetRecentAsync(cancellationToken);

        return
        [
            .. entries
                .Select(entry => Path.GetDirectoryName(entry.FilePath))
                .OfType<string>()
                .Where(folder => folder.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maximum)
        ];
    }

    public async Task RecordAsync(DownloadHistoryEntryDto entry, CancellationToken cancellationToken)
    {
        await _access.WaitAsync(cancellationToken);

        try
        {
            var entries = await _store.ReadAsync(cancellationToken);

            // Baixar o mesmo arquivo de novo atualiza o registro em vez de criar
            // um segundo: são duas linhas iguais apontando para o mesmo arquivo.
            var kept = entries
                .Where(existing => !existing.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase))
                .Take(MaximumEntries - 1);

            await _store.WriteAsync([entry, .. kept], cancellationToken);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            // O download terminou. Não registrá-lo é uma perda pequena; desfazer
            // um arquivo que já está no disco por causa disso seria pior.
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _access.WaitAsync(cancellationToken);

        try
        {
            await _store.WriteAsync([], cancellationToken);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            // A tela recarrega a lista depois de limpar: se a gravação falhou,
            // os registros continuam lá e o usuário vê que nada mudou.
        }
        finally
        {
            _access.Release();
        }
    }

    /// <summary>
    /// Falha ao alcançar o arquivo, e não defeito de programação.
    /// </summary>
    /// <remarks>
    /// Disco cheio, pasta sem permissão e arquivo travado por outro processo são
    /// desfechos possíveis fora do controle do aplicativo. Qualquer outra
    /// exceção continua subindo, porque aí o defeito é nosso.
    /// </remarks>
    private static bool IsStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
