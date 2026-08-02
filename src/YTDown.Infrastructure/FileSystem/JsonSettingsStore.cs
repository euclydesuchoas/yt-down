using System.Text.Json;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.Infrastructure.FileSystem;

/// <inheritdoc cref="ISettingsStore" />
public sealed class JsonSettingsStore : ISettingsStore
{
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonSettingsStore()
        : this(Path.Combine(UserDataLocation.Root, SettingsFileName))
    {
    }

    public JsonSettingsStore(string filePath) => _filePath = filePath;

    public Task<SettingsDto?> ReadAsync(CancellationToken cancellationToken) =>
        JsonFile.ReadAsync<SettingsDto>(_filePath, SerializerOptions, cancellationToken);

    public Task WriteAsync(SettingsDto settings, CancellationToken cancellationToken) =>
        JsonFile.WriteAsync(_filePath, settings, SerializerOptions, cancellationToken);
}
