using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Application.Services;

/// <inheritdoc cref="IDownloadHistoryService" />
public sealed class DownloadHistoryService : IDownloadHistoryService
{
    /// <summary>
    /// Quantos downloads o historico lembra.
    /// </summary>
    /// <remarks>
    /// O historico serve para reencontrar o que foi baixado ha pouco. Passando
    /// de algumas dezenas ele deixa de responder a essa pergunta e vira uma
    /// lista que ninguem le, entao os mais antigos saem.
    /// </remarks>
    private const int MaximumEntries = 50;

    private readonly IDownloadHistoryStore _store;

    // Ler, alterar e gravar sao tres passos sobre o mesmo arquivo. Hoje so ha um
    // download por vez, mas a tela tambem le a lista, e nada garante que os dois
    // nao se cruzem.
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
            // um segundo: sao duas linhas iguais apontando para o mesmo arquivo.
            var kept = entries
                .Where(existing => !existing.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase))
                .Take(MaximumEntries - 1);

            await _store.WriteAsync([entry, .. kept], cancellationToken);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            // O download terminou. Nao registra-lo e uma perda pequena; desfazer
            // um arquivo que ja esta no disco por causa disso seria pior.
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
            // A tela recarrega a lista depois de limpar: se a gravacao falhou,
            // os registros continuam la e o usuario ve que nada mudou.
        }
        finally
        {
            _access.Release();
        }
    }

    /// <summary>
    /// Falha ao alcancar o arquivo, e nao defeito de programacao.
    /// </summary>
    /// <remarks>
    /// Disco cheio, pasta sem permissao e arquivo travado por outro processo sao
    /// desfechos possiveis fora do controle do aplicativo. Qualquer outra
    /// excecao continua subindo, porque ai o defeito e nosso.
    /// </remarks>
    private static bool IsStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
