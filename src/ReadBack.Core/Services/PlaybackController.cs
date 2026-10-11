// Copyright 2026 Amir Farhadi
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// SPDX-License-Identifier: Apache-2.0
using ReadBack.Core.Chunking;
using ReadBack.Core.Filters;
using ReadBack.Core.Models;
using ReadBack.Core.TTS;

namespace ReadBack.Core.Services;

/// <summary>
/// High-performance streaming playback coordinator with concurrent synthesis and chunk navigation.
/// </summary>
public class PlaybackController : IPlaybackController
{
    private readonly INarrationPipeline _pipeline;
    private readonly ISpeechChunker _chunker;
    private readonly ITTSEngine _ttsEngine;
    private readonly ISettingsService _settingsService;
    private readonly Func<string, Task> _playAudioDelegate;
    private readonly Action _stopAudioDelegate;
    private readonly Action _pauseAudioDelegate;
    private readonly Action _resumeAudioDelegate;

    private readonly string _cacheDir;
    private readonly object _playbackLock = new();
    private CancellationTokenSource? _activeCts;
    private CancellationTokenSource? _synthesisCts;
    private Task _synthesisTask = Task.CompletedTask;
    private List<SpeechChunk> _chunks = new();
    private TaskCompletionSource<bool>[] _chunkSynthesis = Array.Empty<TaskCompletionSource<bool>>();
    private int _currentIndex = -1;
    private int _playbackGeneration;
    private string? _lastText;
    private int _lastPreviousIndex = -1;
    private DateTime _lastPreviousTime = DateTime.MinValue;
    private PlaybackState _state = PlaybackState.Idle;
    private static readonly TimeSpan PreviousDoublePressWindow = TimeSpan.FromSeconds(3);

    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs
                {
                    NewState = _state,
                    CurrentChunk = CurrentChunk
                });
            }
        }
    }

    public SpeechChunk? CurrentChunk
    {
        get
        {
            lock (_playbackLock)
                return (_currentIndex >= 0 && _currentIndex < _chunks.Count) ? _chunks[_currentIndex] : null;
        }
    }
    public int CurrentIndex
    {
        get
        {
            lock (_playbackLock)
                return _currentIndex;
        }
    }
    public int TotalChunks
    {
        get
        {
            lock (_playbackLock)
                return _chunks.Count;
        }
    }
    public bool IsPlaying => State == PlaybackState.Playing;
    public bool IsPaused => State == PlaybackState.Paused;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<ChunkChangedEventArgs>? ChunkChanged;

    public PlaybackController(
        INarrationPipeline pipeline,
        ISpeechChunker chunker,
        ITTSEngine ttsEngine,
        ISettingsService settingsService,
        Func<string, Task> playAudioDelegate,
        Action stopAudioDelegate,
        Action pauseAudioDelegate,
        Action resumeAudioDelegate)
    {
        _pipeline = pipeline;
        _chunker = chunker;
        _ttsEngine = ttsEngine;
        _settingsService = settingsService;
        _playAudioDelegate = playAudioDelegate;
        _stopAudioDelegate = stopAudioDelegate;
        _pauseAudioDelegate = pauseAudioDelegate;
        _resumeAudioDelegate = resumeAudioDelegate;

        _cacheDir = Path.Combine(Path.GetTempPath(), "ReadBack_AudioCache");
        Directory.CreateDirectory(_cacheDir);
    }

    public Task PlayTextAsync(string rawText, string? overrideVoice = null, string? overrideSpeed = null, CancellationToken ct = default)
    {
        List<SpeechChunk> chunkSnapshot;
        TaskCompletionSource<bool>[] chunkSynthesis;
        bool resumeCurrentText;

        lock (_playbackLock)
        {
            resumeCurrentText = !string.IsNullOrWhiteSpace(_lastText)
                && string.Equals(_lastText, rawText, StringComparison.Ordinal)
                && _chunks.Count > 0
                && _currentIndex >= 0
                && _currentIndex < _chunks.Count;
            chunkSnapshot = _chunks;
            chunkSynthesis = _chunkSynthesis;
        }

        if (!resumeCurrentText)
        {
            var filterContext = new FilterContext(_settingsService.CurrentSettings);
            string cleanedText = _pipeline.Process(rawText, filterContext);
            var newChunks = string.IsNullOrWhiteSpace(cleanedText)
                ? new List<SpeechChunk>()
                : _chunker.SplitIntoChunks(cleanedText).ToList();

            List<SpeechChunk> previousChunks;
            Task previousSynthesisTask;
            lock (_playbackLock)
            {
                previousChunks = _chunks;
                previousSynthesisTask = _synthesisTask;
                CancelPlaybackLoopLocked();
                CancelSynthesis();
                _chunks = newChunks;
                _lastText = newChunks.Count == 0 ? null : rawText;
                _currentIndex = newChunks.Count == 0 ? -1 : 0;
                _lastPreviousIndex = -1;
                _lastPreviousTime = DateTime.MinValue;
                _chunkSynthesis = newChunks
                    .Select(_ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
                    .ToArray();
                _synthesisCts = newChunks.Count == 0 ? null : CancellationTokenSource.CreateLinkedTokenSource(ct);
                chunkSnapshot = _chunks;
                chunkSynthesis = _chunkSynthesis;
            }
            _stopAudioDelegate();
            _ = CleanupCachedAudioFilesAsync(previousChunks, previousSynthesisTask);

            if (newChunks.Count == 0)
            {
                State = PlaybackState.Idle;
                return Task.CompletedTask;
            }
        }
        else
        {
            CancelPlaybackLoop();
            _stopAudioDelegate();
        }

        string voice = overrideVoice ?? _settingsService.CurrentSettings.Voice;
        if (overrideVoice == null && _settingsService.CurrentSettings.OfflineOnly &&
            !voice.StartsWith("sapi:", StringComparison.OrdinalIgnoreCase) &&
            !voice.StartsWith("Windows:", StringComparison.OrdinalIgnoreCase))
        {
            voice = "sapi:Microsoft David Desktop";
        }
        string speed = overrideSpeed ?? _settingsService.CurrentSettings.Speed;

        if (!resumeCurrentText)
        {
            CancellationTokenSource synthesisCts;
            lock (_playbackLock)
                synthesisCts = _synthesisCts!;
            var synthesisToken = synthesisCts.Token;
            Task synthesisTask = Task.Run(
                () => SynthesizeRemainingChunksAsync(chunkSnapshot, chunkSynthesis, voice, speed, synthesisToken),
                synthesisToken);
            lock (_playbackLock)
                _synthesisTask = synthesisTask;
        }

        StartPlaybackLoop(chunkSnapshot, chunkSynthesis, ct);
        return Task.CompletedTask;
    }

    private async Task SynthesizeRemainingChunksAsync(
        List<SpeechChunk> chunks,
        TaskCompletionSource<bool>[] chunkSynthesis,
        string voice,
        string speed,
        CancellationToken ct)
    {
        try
        {
            for (int i = 0; i < chunks.Count && !ct.IsCancellationRequested; i++)
            {
                var chunk = chunks[i];
                if (chunk.IsSynthesized)
                {
                    chunkSynthesis[i].TrySetResult(true);
                    continue;
                }

                string filePath = Path.Combine(_cacheDir, $"chunk_{i}_{Guid.NewGuid():N}.mp3");
                try
                {
                    bool success = await _ttsEngine.SynthesizeToFileAsync(chunk.Text, filePath, voice, speed, ct);
                    if (success && File.Exists(filePath))
                    {
                        chunk.AudioFilePath = filePath;
                        chunkSynthesis[i].TrySetResult(true);
                    }
                    else
                    {
                        chunkSynthesis[i].TrySetResult(false);
                        Console.Error.WriteLine($"[Playback] Speech synthesis failed for chunk {i}.");
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    chunkSynthesis[i].TrySetResult(false);
                    Console.Error.WriteLine($"[Playback] Speech synthesis failed for chunk {i}: {ex.Message}");
                }
            }
        }
        finally
        {
            foreach (var completion in chunkSynthesis)
            {
                completion.TrySetResult(false);
            }
        }
    }

    private async Task PlaybackLoopAsync(
        List<SpeechChunk> chunks,
        TaskCompletionSource<bool>[] chunkSynthesis,
        int startIndex,
        int generation,
        CancellationToken ct)
    {
        for (int chunkIndex = startIndex; chunkIndex >= 0 && chunkIndex < chunks.Count && !ct.IsCancellationRequested; chunkIndex++)
        {
            lock (_playbackLock)
            {
                if (generation != _playbackGeneration || !ReferenceEquals(chunks, _chunks))
                    return;
                _currentIndex = chunkIndex;
            }

            var chunk = chunks[chunkIndex];
            ChunkChanged?.Invoke(this, new ChunkChangedEventArgs
            {
                Chunk = chunk,
                ChunkIndex = chunkIndex,
                TotalChunks = chunks.Count
            });

            bool isSynthesized;
            try
            {
                isSynthesized = await chunkSynthesis[chunkIndex].Task.WaitAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }

            if (ct.IsCancellationRequested || !IsCurrentPlayback(chunks, generation, chunkIndex))
                break;

            if (!isSynthesized || !chunk.IsSynthesized)
            {
                if (!AdvanceAfterChunk(chunks, generation, chunkIndex))
                {
                    if (IsCurrentPlayback(chunks, generation, chunkIndex))
                        CompletePlayback(chunks, generation);
                    return;
                }
                continue;
            }

            try
            {
                await _playAudioDelegate(chunk.AudioFilePath!);
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    Console.Error.WriteLine($"[Playback] Audio playback failed for chunk {chunkIndex}: {ex.Message}");
            }

            if (ct.IsCancellationRequested)
                break;

            if (!AdvanceAfterChunk(chunks, generation, chunkIndex))
            {
                if (IsCurrentPlayback(chunks, generation, chunkIndex))
                    CompletePlayback(chunks, generation);
                return;
            }
        }

        if (!ct.IsCancellationRequested)
        {
            CompletePlayback(chunks, generation);
        }
    }

    private bool IsCurrentPlayback(List<SpeechChunk> chunks, int generation, int chunkIndex)
    {
        lock (_playbackLock)
        {
            return generation == _playbackGeneration
                && ReferenceEquals(chunks, _chunks)
                && _currentIndex == chunkIndex;
        }
    }

    private bool AdvanceAfterChunk(List<SpeechChunk> chunks, int generation, int chunkIndex)
    {
        lock (_playbackLock)
        {
            if (generation != _playbackGeneration || !ReferenceEquals(chunks, _chunks) || _currentIndex != chunkIndex)
                return false;

            if (chunkIndex >= chunks.Count - 1)
                return false;

            _currentIndex = chunkIndex + 1;
            return true;
        }
    }

    private void CancelPlaybackLoop()
    {
        lock (_playbackLock)
            CancelPlaybackLoopLocked();
    }

    private void CancelPlaybackLoopLocked()
    {
        _playbackGeneration++;
        _activeCts?.Cancel();
        _activeCts?.Dispose();
        _activeCts = null;
    }

    private void CancelSynthesis()
    {
        _synthesisCts?.Cancel();
        _synthesisCts?.Dispose();
        _synthesisCts = null;

        foreach (var completion in _chunkSynthesis)
            completion.TrySetResult(false);
    }

    private void StartPlaybackLoop(
        List<SpeechChunk> chunks,
        TaskCompletionSource<bool>[] chunkSynthesis,
        CancellationToken ct = default)
    {
        int startIndex;
        int generation;
        CancellationToken token;
        lock (_playbackLock)
        {
            _activeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            token = _activeCts.Token;
            startIndex = _currentIndex;
            generation = ++_playbackGeneration;
        }
        State = PlaybackState.Playing;
        _ = Task.Run(() => PlaybackLoopAsync(chunks, chunkSynthesis, startIndex, generation, token), token);
    }

    private void CompletePlayback(List<SpeechChunk> chunks, int generation)
    {
        lock (_playbackLock)
        {
            if (generation != _playbackGeneration || !ReferenceEquals(chunks, _chunks))
                return;
            CancelPlaybackLoopLocked();
            _currentIndex = -1;
            _lastText = null;
            _lastPreviousIndex = -1;
            _lastPreviousTime = DateTime.MinValue;
        }
        _stopAudioDelegate();
        _ = CleanupCachedAudioFilesAsync(chunks, _synthesisTask);
        State = PlaybackState.Idle;
    }

    private static async Task CleanupCachedAudioFilesAsync(IEnumerable<SpeechChunk> chunks, Task synthesisTask)
    {
        try
        {
            await synthesisTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Playback] Could not finish audio synthesis before cache cleanup: {ex.Message}");
        }

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrEmpty(chunk.AudioFilePath))
                continue;

            try
            {
                if (File.Exists(chunk.AudioFilePath))
                    File.Delete(chunk.AudioFilePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Playback] Could not remove cached audio '{chunk.AudioFilePath}': {ex.Message}");
            }
        }
    }

    public void Pause()
    {
        if (State == PlaybackState.Playing)
        {
            _pauseAudioDelegate();
            State = PlaybackState.Paused;
        }
    }

    public void Resume()
    {
        if (State == PlaybackState.Paused)
        {
            _resumeAudioDelegate();
            State = PlaybackState.Playing;
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else if (IsPlaying) Pause();
    }

    public void Stop()
    {
        CancelPlaybackLoop();
        _stopAudioDelegate();
        if (State != PlaybackState.Idle)
            State = PlaybackState.Stopped;
    }

    public void NextChunk()
    {
        List<SpeechChunk> chunks;
        TaskCompletionSource<bool>[] chunkSynthesis;
        int targetIndex;
        int generation;
        CancellationToken token;
        lock (_playbackLock)
        {
            if (_currentIndex < 0 || _currentIndex >= _chunks.Count)
                return;

            targetIndex = _currentIndex + 1;
            _lastPreviousIndex = -1;
            _lastPreviousTime = DateTime.MinValue;
            CancelPlaybackLoopLocked();
            if (targetIndex < _chunks.Count)
            {
                _currentIndex = targetIndex;
                _activeCts = new CancellationTokenSource();
                token = _activeCts.Token;
                chunks = _chunks;
                chunkSynthesis = _chunkSynthesis;
                generation = ++_playbackGeneration;
            }
            else
            {
                _activeCts = null;
                chunks = _chunks;
                chunkSynthesis = _chunkSynthesis;
                generation = -1;
                token = CancellationToken.None;
            }
        }

        _stopAudioDelegate();
        if (targetIndex >= chunks.Count)
        {
            State = PlaybackState.Stopped;
            return;
        }

        State = PlaybackState.Playing;
        _ = Task.Run(() => PlaybackLoopAsync(chunks, chunkSynthesis, targetIndex, generation, token), token);
    }

    public void PreviousChunk()
    {
        List<SpeechChunk> chunks;
        TaskCompletionSource<bool>[] chunkSynthesis;
        int targetIndex;
        int generation;
        CancellationToken token;
        lock (_playbackLock)
        {
            if (_currentIndex < 0 || _currentIndex >= _chunks.Count)
                return;

            DateTime now = DateTime.UtcNow;
            if (_lastPreviousIndex >= 0 && now - _lastPreviousTime <= PreviousDoublePressWindow)
            {
                targetIndex = Math.Max(0, _lastPreviousIndex - 1);
                _lastPreviousIndex = -1;
                _lastPreviousTime = DateTime.MinValue;
            }
            else
            {
                targetIndex = _currentIndex;
                _lastPreviousIndex = targetIndex;
            }
            _lastPreviousTime = now;

            CancelPlaybackLoopLocked();
            _currentIndex = targetIndex;
            _activeCts = new CancellationTokenSource();
            token = _activeCts.Token;
            chunks = _chunks;
            chunkSynthesis = _chunkSynthesis;
            generation = ++_playbackGeneration;
        }

        _stopAudioDelegate();
        State = PlaybackState.Playing;
        _ = Task.Run(() => PlaybackLoopAsync(chunks, chunkSynthesis, targetIndex, generation, token), token);
    }
}
