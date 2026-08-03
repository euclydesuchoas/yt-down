using System.Diagnostics.CodeAnalysis;

namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Descobre onde está o executável de uma ferramenta externa.
/// </summary>
/// <remarks>
/// Existe como abstração desde o início porque o local definitivo das
/// ferramentas ainda vai mudar: elas precisarão viver em uma pasta gravável
/// para poderem se atualizar sozinhas, o que não acontece quando o aplicativo
/// está instalado em Arquivos de Programas.
/// </remarks>
public interface IToolLocator
{
    bool TryLocate(ExternalTool tool, [NotNullWhen(true)] out string? executablePath);
}
