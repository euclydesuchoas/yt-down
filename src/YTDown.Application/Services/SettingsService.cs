using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Application.Services;

/// <inheritdoc cref="ISettingsService" />
/// <remarks>
/// As configurações são lidas do disco uma vez e ficam em memória. Todo download
/// consulta o destino, e ir ao disco a cada consulta seria pagar por uma leitura
/// que nunca muda sozinha: quem grava é o próprio aplicativo.
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsStore _store;
    private readonly SemaphoreSlim _access = new(1, 1);

    private SettingsDto? _current;

    public SettingsService(ISettingsStore store) => _store = store;

    public async Task<SettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        await _access.WaitAsync(cancellationToken);

        try
        {
            return _current ??= await LoadAsync(cancellationToken);
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task SaveAsync(SettingsDto settings, CancellationToken cancellationToken)
    {
        await _access.WaitAsync(cancellationToken);

        try
        {
            await _store.WriteAsync(settings, cancellationToken);

            _current = settings;
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            // Não guardar a escolha é um incômodo; impedir o usuário de fechar a
            // tela por causa disso seria pior. Ela vale até o aplicativo fechar.
            _current = settings;
        }
        finally
        {
            _access.Release();
        }
    }

    private async Task<SettingsDto> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _store.ReadAsync(cancellationToken) ?? SettingsDto.Default;
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            return SettingsDto.Default;
        }
    }

    /// <summary>
    /// Falha ao alcançar o arquivo, e não defeito de programação.
    /// </summary>
    private static bool IsStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
