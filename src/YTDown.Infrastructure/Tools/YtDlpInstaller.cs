using System.Text.Json;
using YTDown.Application.Common;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.Tools;

/// <summary>
/// Coloca o yt-dlp em uma pasta onde ele possa se sobrescrever.
/// </summary>
public sealed class YtDlpInstaller : IToolInstaller
{
    private const string ManifestFileName = "tools.lock.json";
    private const string InstalledVersionFileName = "installed-version.txt";
    private const string YtDlpManifestName = "yt-dlp";

    private readonly ToolLocations _locations;

    public YtDlpInstaller(ToolLocations locations)
    {
        _locations = locations;
    }

    /// <returns>Verdadeiro quando o arquivo foi copiado nesta chamada.</returns>
    public async Task<Result<bool>> EnsureInstalledAsync(CancellationToken cancellationToken)
    {
        var fileName = ToolFileNames.For(ExternalTool.YtDlp);
        var source = Path.Combine(_locations.BundledDirectory, fileName);

        if (!File.Exists(source))
        {
            return Result<bool>.Failure(
                ErrorCode.ToolNotFound,
                $"{fileName} não acompanha esta instalação.");
        }

        try
        {
            var destination = Path.Combine(_locations.UserDirectory, fileName);
            var bundledVersion = ReadBundledVersion();

            if (IsAlreadyInstalled(destination, bundledVersion))
            {
                return Result<bool>.Success(false);
            }

            Directory.CreateDirectory(_locations.UserDirectory);
            File.Copy(source, destination, overwrite: true);

            if (bundledVersion is not null)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(_locations.UserDirectory, InstalledVersionFileName),
                    bundledVersion,
                    cancellationToken);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<bool>.Failure(ErrorCode.Unexpected, exception.ToString());
        }
    }

    /// <remarks>
    /// A comparação é com a versão que acompanha a instalação, e não com a que
    /// está no disco: o yt-dlp instalado costuma estar mais novo, por ter se
    /// atualizado sozinho, e sobrescrevê-lo seria retroceder.
    /// </remarks>
    private bool IsAlreadyInstalled(string destination, string? bundledVersion)
    {
        if (!File.Exists(destination))
        {
            return false;
        }

        if (bundledVersion is null)
        {
            return true;
        }

        var markerPath = Path.Combine(_locations.UserDirectory, InstalledVersionFileName);

        return File.Exists(markerPath) &&
               string.Equals(File.ReadAllText(markerPath).Trim(), bundledVersion, StringComparison.Ordinal);
    }

    /// <summary>
    /// Versão declarada no manifesto que acompanha a instalação.
    /// </summary>
    /// <returns>Nulo quando o manifesto não está presente ou não pode ser lido.</returns>
    private string? ReadBundledVersion()
    {
        var manifestPath = Path.Combine(_locations.BundledDirectory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));

            if (!manifest.RootElement.TryGetProperty("tools", out var tools) ||
                tools.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var tool in tools.EnumerateArray())
            {
                if (tool.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String &&
                    name.GetString() == YtDlpManifestName &&
                    tool.TryGetProperty("version", out var version) &&
                    version.ValueKind == JsonValueKind.String)
                {
                    return version.GetString();
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return null;
        }

        return null;
    }
}
