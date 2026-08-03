using YTDown.Application.Common;

namespace YTDown.Application.Interfaces;

/// <summary>
/// Atualiza a ferramenta que extrai os vídeos.
/// </summary>
/// <remarks>
/// O YouTube muda com frequência e quebra versões antigas. Sem atualização, o
/// aplicativo tem prazo de validade, e o público-alvo não teria como resolver.
/// </remarks>
public interface IToolUpdater
{
    /// <returns>A nova versão, ou o próprio valor anterior quando nada mudou.</returns>
    Task<Result<string>> UpdateAsync(CancellationToken cancellationToken);
}
