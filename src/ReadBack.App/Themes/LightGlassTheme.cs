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
using System.Windows.Media.Effects;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using GradientStop = System.Windows.Media.GradientStop;
using LinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using Point = System.Windows.Point;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ReadBack.App.Themes;

/// <summary>
/// Light Acrylic Glass Theme for Windows 11 Light Mode enthusiasts.
/// </summary>
public class LightGlassTheme : IHudTheme
{
    public string Id => "light-glass";
    public string DisplayName => "Windows 11 Light Glass";

    public Brush BackgroundBrush { get; }
    public Brush BorderBrush { get; }
    public Thickness BorderThickness => new(1.2);
    public CornerRadius CornerRadius => new(24);

    public Brush TitleForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
    public Brush TextForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181B"));
    public Brush BadgeBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
    public Brush BadgeForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));

    public Brush ButtonBackground => new SolidColorBrush(Color.FromArgb(120, 241, 245, 249));
    public Brush ButtonHoverBackground => new SolidColorBrush(Color.FromArgb(200, 226, 232, 240));
    public Brush ButtonPressedBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
    public Brush ButtonForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));

    public Effect? WindowEffect => new DropShadowEffect
    {
        BlurRadius = 24,
        ShadowDepth = 4,
        Direction = 270,
        Color = Color.FromRgb(100, 116, 139),
        Opacity = 0.35
    };

    public int BackdropType => 0; // Translucent per-pixel alpha window
    public bool IsDarkMode => false;

    public LightGlassTheme()
    {
        var bg = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        bg.GradientStops.Add(new GradientStop(Color.FromArgb(225, 255, 255, 255), 0.0));
        bg.GradientStops.Add(new GradientStop(Color.FromArgb(210, 241, 245, 249), 1.0));
        BackgroundBrush = bg;

        var border = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        border.GradientStops.Add(new GradientStop(Color.FromArgb(200, 255, 255, 255), 0.0));
        border.GradientStops.Add(new GradientStop(Color.FromArgb(120, 203, 213, 225), 1.0));
        BorderBrush = border;
    }
}
