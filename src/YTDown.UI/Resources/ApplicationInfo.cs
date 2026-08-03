using System.Reflection;

namespace YTDown.UI.Resources;

/// <summary>
/// Quem fez o aplicativo e qual versão está rodando.
/// </summary>
/// <remarks>
/// A versão vem do próprio assembly, e não de uma constante: ela é declarada uma
/// única vez, no csproj, e é de lá que o instalador e o nome do pacote também
/// saem. Uma cópia aqui divergiria na primeira publicação.
/// </remarks>
internal static class ApplicationInfo
{
    public const string Author = "Euclydes Uchoas";

    public static string Version => typeof(ApplicationInfo).Assembly.GetName().Version is { } version
        ? $"{version.Major}.{version.Minor}.{version.Build}"
        : "0.0.0";

    /// <summary>
    /// Linha de rodapé. A versão acompanha o crédito porque é a primeira coisa
    /// que se pergunta a quem relata um problema.
    /// </summary>
    public static string Credit => $"YTDown {Version}   ·   Feito por {Author}";
}
