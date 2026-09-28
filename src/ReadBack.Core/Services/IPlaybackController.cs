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
