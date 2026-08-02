using System.Diagnostics.CodeAnalysis;

namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Descobre onde esta o executavel de uma ferramenta externa.
/// </summary>
/// <remarks>
/// Existe como abstracao desde o inicio porque o local definitivo das
/// ferramentas ainda vai mudar: elas precisarao viver em uma pasta gravavel
/// para poderem se atualizar sozinhas, o que nao acontece quando o aplicativo
/// esta instalado em Arquivos de Programas.
/// </remarks>
public interface IToolLocator
{
    bool TryLocate(ExternalTool tool, [NotNullWhen(true)] out string? executablePath);
}
