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
/// Default Windows 11 Frosted Acrylic Glass Theme.
/// Combines native DWM Acrylic backdrop with sleek translucent glassmorphism aesthetics.
/// </summary>
public class Windows11GlassTheme : IHudTheme
{
    public string Id => "win11-glass";
    public string DisplayName => "Windows 11 Glass (Default)";

    public Brush BackgroundBrush { get; }
    public Brush BorderBrush { get; }
    public Thickness BorderThickness => new(1.2);
    public CornerRadius CornerRadius => new(24);

    public Brush TitleForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
    public Brush TextForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
    public Brush BadgeBackground => new SolidColorBrush(Color.FromArgb(200, 30, 41, 59));
    public Brush BadgeForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));

    public Brush ButtonBackground => new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
    public Brush ButtonHoverBackground => new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
    public Brush ButtonPressedBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
    public Brush ButtonForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));

    public Effect? WindowEffect => new DropShadowEffect
    {
        BlurRadius = 20,
        ShadowDepth = 3,
        Direction = 270,
        Color = Color.FromRgb(0, 0, 0),
        Opacity = 0.5
    };

    public int BackdropType => 0; // Translucent per-pixel alpha window
    public bool IsDarkMode => true;

    public Windows11GlassTheme()
    {
        // Frosted dark translucent glass background
        var bg = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        bg.GradientStops.Add(new GradientStop(Color.FromArgb(220, 24, 30, 42), 0.0)); // Dark sleek slate
        bg.GradientStops.Add(new GradientStop(Color.FromArgb(190, 15, 20, 30), 1.0));
        BackgroundBrush = bg;

        // Subtle specular highlight border
        var border = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        border.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.0)); // Crisp top highlight
        border.GradientStops.Add(new GradientStop(Color.FromArgb(35, 255, 255, 255), 0.5));
        border.GradientStops.Add(new GradientStop(Color.FromArgb(50, 96, 165, 250), 1.0)); // Blue tint bottom
        BorderBrush = border;
    }
}
