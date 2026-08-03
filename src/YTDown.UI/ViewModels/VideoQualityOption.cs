namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma qualidade oferecida ao usuário, entre as que o vídeo realmente tem.
/// </summary>
public sealed record VideoQualityOption(int Height)
{
    public string Label => $"{Height}p";
}
