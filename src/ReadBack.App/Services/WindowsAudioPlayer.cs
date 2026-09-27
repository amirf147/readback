using System.IO;
using System.Windows.Media;

namespace ReadBack.App.Services;

public class WindowsAudioPlayer : IAudioPlayer
{
    private MediaPlayer? _player;
    private TaskCompletionSource<bool>? _playbackTcs;
    private readonly object _lock = new();

    public WindowsAudioPlayer()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _player = new MediaPlayer();
            _player.MediaEnded += (s, e) =>
            {
                lock (_lock)
                {
                    _playbackTcs?.TrySetResult(true);
                }
            };
            _player.MediaFailed += (s, e) =>
            {
                lock (_lock)
                {
                    _playbackTcs?.TrySetResult(false);
                }
            };
        });
    }

    public Task PlayFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.CompletedTask;

        lock (_lock)
        {
            _playbackTcs = new TaskCompletionSource<bool>();
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                _player?.Open(new Uri(filePath, UriKind.Absolute));
                _player?.Play();
            }
            catch
            {
                lock (_lock) { _playbackTcs?.TrySetResult(false); }
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
        lock (_lock)
        {
            _playbackTcs?.TrySetCanceled();
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                _player?.Stop();
                _player?.Close();
            }
            catch { }
        });
    }
}
