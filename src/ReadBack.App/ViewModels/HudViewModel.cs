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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ReadBack.Core.Models;
using ReadBack.Core.Services;

namespace ReadBack.App.ViewModels;

public class HudViewModel : INotifyPropertyChanged
{
    private readonly IPlaybackController _playback;
    private readonly ISettingsService _settings;
    private readonly Themes.IHudThemeManager _themeManager;

    private string _statusText = "Idle";
    private string _chunkProgress = "";
    private string _snippetText = "";
    private bool _isVisible = false;
    private bool _isPlaying = false;
    private bool _isPaused = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Themes.IHudTheme CurrentTheme => _themeManager.CurrentTheme;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ChunkProgress
    {
        get => _chunkProgress;
        set => SetProperty(ref _chunkProgress, value);
    }

    public string SnippetText
    {
        get => _snippetText;
        set => SetProperty(ref _snippetText, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PrevCommand { get; }
    public ICommand CloseCommand { get; }

    public HudViewModel(IPlaybackController playback, ISettingsService settings, Themes.IHudThemeManager themeManager)
    {
        _playback = playback;
        _settings = settings;
        _themeManager = themeManager;

        PlayPauseCommand = new RelayCommand(() => _playback.TogglePause());
        StopCommand = new RelayCommand(() => _playback.Stop());
        NextCommand = new RelayCommand(() => _playback.NextChunk());
        PrevCommand = new RelayCommand(() => _playback.PreviousChunk());
        CloseCommand = new RelayCommand(() => IsVisible = false);

        _playback.StateChanged += OnPlaybackStateChanged;
        _playback.ChunkChanged += OnChunkChanged;

        _themeManager.ThemeChanged += (s, theme) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTheme)));
            });
        };
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsPlaying = e.NewState == PlaybackState.Playing;
            IsPaused = e.NewState == PlaybackState.Paused;

            switch (e.NewState)
            {
                case PlaybackState.Playing:
                    StatusText = "Reading aloud";
                    if (_settings.CurrentSettings.ShowFloatingHud)
                        IsVisible = true;
                    break;
                case PlaybackState.Paused:
                    StatusText = "Paused";
                    break;
                case PlaybackState.Stopped:
                case PlaybackState.Idle:
                    StatusText = "Idle";
                    ChunkProgress = "";
                    SnippetText = "";
                    IsVisible = false;
                    break;
            }
        });
    }

    private void OnChunkChanged(object? sender, ChunkChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ChunkProgress = $"{e.ChunkIndex + 1} / {e.TotalChunks}";
            string text = e.Chunk.Text.Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 90)
                text = text.Substring(0, 87) + "...";
            SnippetText = text;
        });
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
