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
    private sealed class PlaybackOperation
    {
        public TaskCompletionSource<bool> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenSource? WatchdogCts;
        public MediaPlayer? Player { get; set; }
    }

    private readonly object _lock = new();
    private PlaybackOperation? _currentOperation;

    public Task PlayFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.CompletedTask;

        var operation = new PlaybackOperation();
        PlaybackOperation? previousOperation;
        lock (_lock)
        {
            previousOperation = _currentOperation;
            _currentOperation = operation;
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CancelOperation(previousOperation);

            lock (_lock)
            {
                if (!ReferenceEquals(_currentOperation, operation))
                {
                    CancelOperation(operation);
                    return;
                }
            }

            var player = new MediaPlayer { Volume = 1.0 };
            operation.Player = player;
            player.MediaOpened += (_, _) =>
            {
                try
                {
                    if (player.NaturalDuration.HasTimeSpan)
                    {
                        StartWatchdog(operation, player.NaturalDuration.TimeSpan + TimeSpan.FromMilliseconds(750));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Audio] Could not read audio duration: {ex.Message}");
                }
            };
            player.MediaEnded += (_, _) => CompleteOperation(operation, true);
            player.MediaFailed += (_, e) =>
            {
                Console.Error.WriteLine($"[Audio] Media playback failed: {e.ErrorException?.Message ?? "Unknown media error"}");
                CompleteOperation(operation, false);
            };

            long fileLen = 0;
            try { fileLen = new FileInfo(filePath).Length; }
            catch (Exception ex) { Console.Error.WriteLine($"[Audio] Could not read audio file size: {ex.Message}"); }
            double estimatedSeconds = Math.Max(2.0, (fileLen / 6000.0) + 1.5);
            StartWatchdog(operation, TimeSpan.FromSeconds(estimatedSeconds));

            try
            {
                player.Open(new Uri(filePath, UriKind.Absolute));
                player.Play();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Audio] Could not start audio playback: {ex.Message}");
                CompleteOperation(operation, false);
            }
        });

        return operation.Completion.Task;
    }

    private void StartWatchdog(PlaybackOperation operation, TimeSpan timeout)
    {
        var watchdogCts = new CancellationTokenSource();
        lock (_lock)
        {
            if (!ReferenceEquals(_currentOperation, operation))
            {
                watchdogCts.Dispose();
                return;
            }

            var previousWatchdog = Interlocked.Exchange(ref operation.WatchdogCts, watchdogCts);
            if (previousWatchdog != null)
            {
                previousWatchdog.Cancel();
                previousWatchdog.Dispose();
            }
        }
        _ = WatchPlaybackAsync(operation, timeout, watchdogCts.Token);
    }

    private async Task WatchPlaybackAsync(PlaybackOperation operation, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            await Task.Delay(timeout, ct);
            CompleteOperation(operation, true);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static void CancelWatchdog(PlaybackOperation operation)
    {
        var watchdogCts = Interlocked.Exchange(ref operation.WatchdogCts, null);
        if (watchdogCts == null)
            return;

        try
        {
            watchdogCts.Cancel();
        }
        finally
        {
            watchdogCts.Dispose();
        }
    }

    private void CompleteOperation(PlaybackOperation operation, bool result)
    {
        lock (_lock)
        {
            if (!ReferenceEquals(_currentOperation, operation))
                return;
            _currentOperation = null;
        }

        CancelWatchdog(operation);
        operation.Completion.TrySetResult(result);
    }

    private static void CancelOperation(PlaybackOperation? operation)
    {
        if (operation is null)
            return;

        CancelWatchdog(operation);
        operation.Completion.TrySetCanceled();
        try
        {
            operation.Player?.Stop();
            operation.Player?.Close();
            operation.Player = null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Audio] Could not close the previous audio player: {ex.Message}");
        }
    }

    public void Pause()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                try { _currentOperation?.Player?.Pause(); }
                catch (Exception ex) { Console.Error.WriteLine($"[Audio] Could not pause playback: {ex.Message}"); }
            }
        });
    }

    public void Resume()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                try { _currentOperation?.Player?.Play(); }
                catch (Exception ex) { Console.Error.WriteLine($"[Audio] Could not resume playback: {ex.Message}"); }
            }
        });
    }

    public void Stop()
    {
        PlaybackOperation? operation;
        lock (_lock)
        {
            operation = _currentOperation;
            _currentOperation = null;
        }

        if (operation is null)
            return;

        CancelWatchdog(operation);
        operation.Completion.TrySetCanceled();
        System.Windows.Application.Current.Dispatcher.Invoke(() => CancelOperationPlayer(operation));
    }

    private static void CancelOperationPlayer(PlaybackOperation operation)
    {
        try
        {
            operation.Player?.Stop();
            operation.Player?.Close();
            operation.Player = null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Audio] Could not stop playback: {ex.Message}");
        }
    }
}
