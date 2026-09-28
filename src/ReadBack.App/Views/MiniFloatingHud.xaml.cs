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
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using ReadBack.App.Services;
using ReadBack.App.ViewModels;

namespace ReadBack.App.Views;

public partial class MiniFloatingHud : Window
{
    private readonly HudViewModel _viewModel;
    private Storyboard? _waveAnimation;

    public MiniFloatingHud(HudViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _waveAnimation = TryFindResource("WavePulseAnimation") as Storyboard;

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                if (_viewModel.IsPlaying)
                {
                    _viewModel.StopCommand.Execute(null);
                }
                else
                {
                    _viewModel.IsVisible = false;
                }
                e.Handled = true;
            }
        };

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HudViewModel.IsVisible))
            {
                Dispatcher.Invoke(() =>
                {
                    if (_viewModel.IsVisible)
                    {
                        PositionTopCenter();
                        Show();
                        Activate();
                        ApplyDwmBackdrop();
                    }
                    else
                    {
                        Hide();
                    }
                });
            }
            else if (e.PropertyName == nameof(HudViewModel.CurrentTheme))
            {
                Dispatcher.Invoke(ApplyDwmBackdrop);
            }
            else if (e.PropertyName == nameof(HudViewModel.IsPlaying))
            {
                Dispatcher.Invoke(() =>
                {
                    if (_viewModel.IsPlaying)
                        _waveAnimation?.Begin();
                    else
                        _waveAnimation?.Stop();
                });
            }
        };

        Loaded += (s, e) =>
        {
            PositionTopCenter();
            ApplyDwmBackdrop();
        };
    }

    private void ApplyDwmBackdrop()
    {
        // Translucent gradient, specular border, and WPF drop shadow render
        // the clean, cohesive pill with 100% transparent surroundings.
    }

    private void PositionTopCenter()
    {
        // Position top center of the screen, similar to Windows 11 Dictation (Win+H)
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
        Top = 35; // Slight offset from top edge
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
