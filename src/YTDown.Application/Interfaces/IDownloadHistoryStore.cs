using YTDown.Application.DTOs;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Onde o histórico fica guardado entre uma execução e outra.
/// </summary>
/// <remarks>
/// Lê e escreve a lista inteira. São poucas dezenas de registros, e um formato
/// que permitisse alterar um de cada vez custaria mais do que resolve.
/// Quem serializa não decide o que entra na lista: isso é do serviço.
/// </remarks>
public interface IDownloadHistoryStore
{
    /// <summary>
    /// Lê o histórico gravado, do mais recente para o mais antigo.
    /// </summary>
    /// <remarks>Lista vazia quando ainda não há nada gravado.</remarks>
    Task<IReadOnlyList<DownloadHistoryEntryDto>> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(IReadOnlyList<DownloadHistoryEntryDto> entries, CancellationToken cancellationToken);
}
