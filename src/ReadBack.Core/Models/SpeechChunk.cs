namespace ReadBack.Core.Models;

/// <summary>
/// Represents a discrete segment of sanitized text queued for speech synthesis and playback.
/// </summary>
public record SpeechChunk
{
    public required int Index { get; init; }
    public required int TotalCount { get; init; }
    public required string Text { get; init; }
    public string? AudioFilePath { get; set; }
    public bool IsSynthesized => !string.IsNullOrEmpty(AudioFilePath) && File.Exists(AudioFilePath);
}
