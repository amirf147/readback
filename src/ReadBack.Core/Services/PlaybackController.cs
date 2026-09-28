// Copyright 2026 ReadBack Contributors
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
    private CancellationTokenSource? _activeCts;
    private List<SpeechChunk> _chunks = new();
    private int _currentIndex = -1;
    private PlaybackState _state = PlaybackState.Idle;

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

    public SpeechChunk? CurrentChunk => (_currentIndex >= 0 && _currentIndex < _chunks.Count) ? _chunks[_currentIndex] : null;
    public int CurrentIndex => _currentIndex;
    public int TotalChunks => _chunks.Count;
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

    public async Task PlayTextAsync(string rawText, CancellationToken ct = default)
    {
        Stop();

        var filterContext = new FilterContext(_settingsService.CurrentSettings);
        string cleanedText = _pipeline.Process(rawText, filterContext);
        if (string.IsNullOrWhiteSpace(cleanedText))
        {
            State = PlaybackState.Idle;
            return;
        }

        _chunks = _chunker.SplitIntoChunks(cleanedText).ToList();
        if (_chunks.Count == 0)
        {
            State = PlaybackState.Idle;
            return;
        }

        _activeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var activeToken = _activeCts.Token;

        State = PlaybackState.Playing;
        _currentIndex = 0;

        // Start background prefetch synthesis for all chunks
        _ = Task.Run(() => SynthesizeRemainingChunksAsync(_chunks, activeToken), activeToken);

        // Start sequential playback loop
        _ = Task.Run(() => PlaybackLoopAsync(activeToken), activeToken);
    }

    private async Task SynthesizeRemainingChunksAsync(List<SpeechChunk> chunks, CancellationToken ct)
    {
        var settings = _settingsService.CurrentSettings;
        for (int i = 0; i < chunks.Count && !ct.IsCancellationRequested; i++)
        {
            var chunk = chunks[i];
            if (chunk.IsSynthesized) continue;

            string filePath = Path.Combine(_cacheDir, $"chunk_{i}_{Guid.NewGuid():N}.mp3");
            bool success = await _ttsEngine.SynthesizeToFileAsync(chunk.Text, filePath, settings.Voice, settings.Speed, ct);
            if (success)
            {
                chunk.AudioFilePath = filePath;
            }
        }
    }

    private async Task PlaybackLoopAsync(CancellationToken ct)
    {
        while (_currentIndex < _chunks.Count && !ct.IsCancellationRequested)
        {
            var chunk = _chunks[_currentIndex];
            ChunkChanged?.Invoke(this, new ChunkChangedEventArgs
            {
                Chunk = chunk,
                ChunkIndex = _currentIndex,
                TotalChunks = _chunks.Count
            });

            // Wait for current chunk audio synthesis if not ready yet
            int waitCount = 0;
            while (!chunk.IsSynthesized && waitCount < 100 && !ct.IsCancellationRequested)
            {
                await Task.Delay(50, ct);
                waitCount++;
            }

            if (ct.IsCancellationRequested || !chunk.IsSynthesized)
                break;

            try
            {
                await _playAudioDelegate(chunk.AudioFilePath!);
            }
            catch
            {
                // Move past audio error
            }

            if (ct.IsCancellationRequested) break;

            _currentIndex++;
        }

        if (!ct.IsCancellationRequested)
        {
            Stop();
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
        _activeCts?.Cancel();
        _activeCts?.Dispose();
        _activeCts = null;

        _stopAudioDelegate();
        _currentIndex = -1;
        State = PlaybackState.Idle;

        // Clean cache files
        Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    foreach (var file in Directory.GetFiles(_cacheDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }
        });
    }

    public void NextChunk()
    {
        if (_currentIndex < _chunks.Count - 1)
        {
            _currentIndex++;
            _stopAudioDelegate();
        }
        else
        {
            Stop();
        }
    }

    public void PreviousChunk()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            _stopAudioDelegate();
        }
    }
}
