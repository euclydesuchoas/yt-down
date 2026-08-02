namespace YTDown.UI.ViewModels;

/// <summary>
/// Uma qualidade oferecida ao usuario, entre as que o video realmente tem.
/// </summary>
public sealed record VideoQualityOption(int Height)
{
    public string Label => $"{Height}p";
}
