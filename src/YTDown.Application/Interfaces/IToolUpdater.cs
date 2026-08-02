using YTDown.Application.Common;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Atualiza a ferramenta que extrai os videos.
/// </summary>
/// <remarks>
/// O YouTube muda com frequencia e quebra versoes antigas. Sem atualizacao, o
/// aplicativo tem prazo de validade, e o publico-alvo nao teria como resolver.
/// </remarks>
public interface IToolUpdater
{
    /// <returns>A nova versao, ou o proprio valor anterior quando nada mudou.</returns>
    Task<Result<string>> UpdateAsync(CancellationToken cancellationToken);
}
