using System.Diagnostics.CodeAnalysis;

namespace YTDown.Infrastructure.Tools;

/// <inheritdoc cref="IToolLocator" />
public sealed class ManagedToolLocator : IToolLocator
{
    private readonly ToolLocations _locations;

    public ManagedToolLocator(ToolLocations locations)
    {
        _locations = locations;
    }

    public bool TryLocate(ExternalTool tool, [NotNullWhen(true)] out string? executablePath)
    {
        executablePath = null;

        foreach (var directory in DirectoriesFor(tool))
        {
            var candidate = Path.Combine(directory, ToolFileNames.For(tool));

            if (File.Exists(candidate))
            {
                executablePath = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ordem de procura, da preferida para a alternativa.
    /// </summary>
    /// <remarks>
    /// A copia do perfil do usuario vem primeiro por ser a que se mantem
    /// atualizada. A copia que acompanha a instalacao serve de reserva, e por
    /// isso um download funciona mesmo antes de a instalacao ter terminado.
    /// </remarks>
    private IEnumerable<string> DirectoriesFor(ExternalTool tool) =>
        tool == ExternalTool.YtDlp
            ? [_locations.UserDirectory, _locations.BundledDirectory]
            : [_locations.BundledDirectory];
}
