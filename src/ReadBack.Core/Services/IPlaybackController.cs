using ReadBack.Core.Models;

namespace ReadBack.Core.Services;

public class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState NewState { get; init; }
    public SpeechChunk? CurrentChunk { get; init; }
}

public class ChunkChangedEventArgs : EventArgs
{
    public required SpeechChunk Chunk { get; init; }
    public required int ChunkIndex { get; init; }
    public required int TotalChunks { get; init; }
}

public interface IPlaybackController
{
    PlaybackState State { get; }
    SpeechChunk? CurrentChunk { get; }
    int CurrentIndex { get; }
    int TotalChunks { get; }
    bool IsPlaying { get; }
    bool IsPaused { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    event EventHandler<ChunkChangedEventArgs>? ChunkChanged;

    Task PlayTextAsync(string rawText, CancellationToken ct = default);
    void Pause();
    void Resume();
    void TogglePause();
    void Stop();
    void NextChunk();
    void PreviousChunk();
}
