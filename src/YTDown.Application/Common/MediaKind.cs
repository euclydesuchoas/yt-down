namespace YTDown.Application.Common;

/// <summary>
/// O que o usuario quer levar do video.
/// </summary>
public enum MediaKind
{
    /// <summary>Video com audio, em MP4.</summary>
    Video,

    /// <summary>Somente a trilha sonora, convertida para MP3.</summary>
    AudioOnly
}
