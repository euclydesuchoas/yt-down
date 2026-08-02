using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDown.Application.Common;
using YTDown.Application.DTOs;
using YTDown.Application.Interfaces;
using YTDown.UI.Resources;

namespace YTDown.UI.ViewModels;

/// <summary>
/// Tela unica do aplicativo: recebe um endereco e mostra o video encontrado.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IVideoInfoService _videoInfoService;

    public MainViewModel(IVideoInfoService videoInfoService)
    {
        _videoInfoService = videoInfoService;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private VideoInfoDto? _video;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Duracao pronta para leitura. Transmissoes ao vivo chegam com duracao zero.
    /// </summary>
    public string? DurationText => Video switch
    {
        null => null,
        { Duration.Ticks: 0 } => "Ao vivo",
        { Duration: var duration } when duration.TotalHours >= 1 => duration.ToString(@"h\:mm\:ss"),
        { Duration: var duration } => duration.ToString(@"m\:ss")
    };

    partial void OnVideoChanged(VideoInfoDto? value) => OnPropertyChanged(nameof(DurationText));

    private bool CanSearch() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanSearch), IncludeCancelCommand = true)]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        Video = null;

        var result = await _videoInfoService.GetVideoInfoAsync(Url, cancellationToken);

        if (result.IsSuccess)
        {
            Video = result.Value;
            return;
        }

        // Cancelar foi uma escolha do usuario, nao uma falha: nada a comunicar.
        if (result.Error == ErrorCode.Canceled)
        {
            return;
        }

        ErrorMessage = ErrorMessages.For(result.Error.Value);
    }
}
