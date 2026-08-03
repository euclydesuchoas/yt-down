namespace YTDown.Application.Common;

/// <summary>
/// O que o usuário quer levar do vídeo.
/// </summary>
public enum MediaKind
{
    /// <summary>Vídeo com áudio, em MP4.</summary>
    Video,

    /// <summary>Somente a trilha sonora, convertida para MP3.</summary>
    AudioOnly
}
