using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;

namespace YTDown.UI.ViewModels;

/// <summary>
/// O que o usuário decide uma vez.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService) => _settingsService = settingsService;

    /// <remarks>
    /// Os tetos oferecidos param em 1080p porque o YouTube não entrega H.264
    /// acima disso, e é H.264 que permite entregar um MP4 sem reconverter.
    /// </remarks>
    public IReadOnlyList<DefaultQualityOption> QualityOptions { get; } =
        [new(null), new(1080), new(720), new(480), new(360)];

    /// <summary>Pasta escolhida, ou <c>null</c> para a pasta Downloads.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestinationDescription))]
    [NotifyCanExecuteChangedFor(nameof(UseDownloadsFolderCommand))]
    private string? _destinationDirectory;

    [ObservableProperty]
    private DefaultQualityOption _selectedQuality = new(null);

    /// <summary>
    /// A pasta como o usuário a lê. Sem escolha, diz qual é o destino em vez de
    /// ficar em branco.
    /// </summary>
    public string DestinationDescription =>
        DestinationDirectory is { Length: > 0 } chosen ? chosen : "Sua pasta Downloads";

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        DestinationDirectory = settings.DestinationDirectory;
        SelectedQuality = QualityOptions.FirstOrDefault(option => option.Height == settings.MaximumHeight)
                          ?? QualityOptions[0];
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken) =>
        _settingsService.SaveAsync(
            new SettingsDto(DestinationDirectory, SelectedQuality.Height),
            cancellationToken);

    private bool CanUseDownloadsFolder() => DestinationDirectory is { Length: > 0 };

    /// <summary>Volta ao destino de quem nunca abriu esta tela.</summary>
    [RelayCommand(CanExecute = nameof(CanUseDownloadsFolder))]
    private void UseDownloadsFolder() => DestinationDirectory = null;
}
