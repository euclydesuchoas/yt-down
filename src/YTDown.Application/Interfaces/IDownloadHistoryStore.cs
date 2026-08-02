using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Onde o historico fica guardado entre uma execucao e outra.
/// </summary>
/// <remarks>
/// Le e escreve a lista inteira. Sao poucas dezenas de registros, e um formato
/// que permitisse alterar um de cada vez custaria mais do que resolve.
/// Quem serializa nao decide o que entra na lista: isso e do servico.
/// </remarks>
public interface IDownloadHistoryStore
{
    /// <summary>
    /// Le o historico gravado, do mais recente para o mais antigo.
    /// </summary>
    /// <remarks>Lista vazia quando ainda nao ha nada gravado.</remarks>
    Task<IReadOnlyList<DownloadHistoryEntryDto>> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(IReadOnlyList<DownloadHistoryEntryDto> entries, CancellationToken cancellationToken);
}
