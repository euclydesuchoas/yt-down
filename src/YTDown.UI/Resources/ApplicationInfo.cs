using System.Reflection;

namespace YTDown.UI.Resources;

/// <summary>
/// Quem fez o aplicativo e qual versao esta rodando.
/// </summary>
/// <remarks>
/// A versao vem do proprio assembly, e nao de uma constante: ela e declarada uma
/// unica vez, no csproj, e e de la que o instalador e o nome do pacote tambem
/// saem. Uma copia aqui divergiria na primeira publicacao.
/// </remarks>
internal static class ApplicationInfo
{
    public const string Author = "Euclydes Uchoas";

    public static string Version => typeof(ApplicationInfo).Assembly.GetName().Version is { } version
        ? $"{version.Major}.{version.Minor}.{version.Build}"
        : "0.0.0";

    /// <summary>
    /// Linha de rodape. A versao acompanha o credito porque e a primeira coisa
    /// que se pergunta a quem relata um problema.
    /// </summary>
    public static string Credit => $"YTDown {Version}   ·   Feito por {Author}";
}
