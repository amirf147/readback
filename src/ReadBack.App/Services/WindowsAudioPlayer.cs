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
using System.IO;
using System.Windows.Media;

namespace ReadBack.App.Services;

public class WindowsAudioPlayer : IAudioPlayer
{
    private MediaPlayer? _player;
    private TaskCompletionSource<bool>? _playbackTcs;
    private CancellationTokenSource? _watchdogCts;
    private readonly object _lock = new();

    public WindowsAudioPlayer()
    {
        CreatePlayerOnDispatcher();
    }

    private void CreatePlayerOnDispatcher()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (_player != null)
                {
                    _player.Stop();
                    _player.Close();
                }
            }
            catch { }

            _player = new MediaPlayer();
            _player.Volume = 1.0;

            _player.MediaOpened += (s, e) =>
            {
                try
                {
                    if (_player.NaturalDuration.HasTimeSpan)
                    {
                        var duration = _player.NaturalDuration.TimeSpan;
                        StartWatchdog(duration + TimeSpan.FromMilliseconds(750));
                    }
                }
                catch { }
            };

            _player.MediaEnded += (s, e) =>
            {
                CancelWatchdog();
                CompleteCurrentPlayback(true);
            };

            _player.MediaFailed += (s, e) =>
            {
                CancelWatchdog();
                CompleteCurrentPlayback(false);
            };
        });
    }

    private void StartWatchdog(TimeSpan timeout)
    {
        CancelWatchdog();
        _watchdogCts = new CancellationTokenSource();
        var token = _watchdogCts.Token;

        Task.Delay(timeout, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                CompleteCurrentPlayback(true);
            }
        }, TaskScheduler.Default);
    }

    private void CancelWatchdog()
    {
        try
        {
            _watchdogCts?.Cancel();
            _watchdogCts?.Dispose();
            _watchdogCts = null;
        }
        catch { }
    }

    private void CompleteCurrentPlayback(bool result)
    {
        lock (_lock)
        {
            _playbackTcs?.TrySetResult(result);
        }
    }

    public Task PlayFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.CompletedTask;

        lock (_lock)
        {
            _playbackTcs = new TaskCompletionSource<bool>();
        }

        // Estimate fallback duration based on file size (MP3 48kbps = 6,000 bytes/sec)
        long fileLen = 0;
        try { fileLen = new FileInfo(filePath).Length; } catch { }
        double estimatedSeconds = Math.Max(2.0, (fileLen / 6000.0) + 1.5);
        StartWatchdog(TimeSpan.FromSeconds(estimatedSeconds));

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (_player == null)
                    CreatePlayerOnDispatcher();

                _player?.Open(new Uri(filePath, UriKind.Absolute));
                _player?.Play();
            }
            catch
            {
                CompleteCurrentPlayback(false);
            }
        });

        return _playbackTcs.Task;
    }

    public void Pause()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try { _player?.Pause(); } catch { }
        });
    }

    public void Resume()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try { _player?.Play(); } catch { }
        });
    }

    public void Stop()
    {
        CancelWatchdog();

        lock (_lock)
        {
            _playbackTcs?.TrySetCanceled();
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                _player?.Stop();
            }
            catch { }
        });
    }
}
