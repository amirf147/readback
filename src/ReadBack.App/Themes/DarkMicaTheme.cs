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
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ReadBack.App.Themes;

/// <summary>
/// Dark Mica Theme inspired by Windows 11 Fluent Design System.
/// </summary>
public class DarkMicaTheme : IHudTheme
{
    public string Id => "dark-mica";
    public string DisplayName => "Windows 11 Mica (Dark)";

    public Brush BackgroundBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6202024"));
    public Brush BorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383842"));
    public Thickness BorderThickness => new(1.0);
    public CornerRadius CornerRadius => new(24);

    public Brush TitleForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#93C5FD"));
    public Brush TextForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E7"));
    public Brush BadgeBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E36"));
    public Brush BadgeForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA"));

    public Brush ButtonBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A30"));
    public Brush ButtonHoverBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C46"));
    public Brush ButtonPressedBackground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
    public Brush ButtonForeground => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));

    public Effect? WindowEffect => new DropShadowEffect
    {
        BlurRadius = 20,
        ShadowDepth = 4,
        Direction = 270,
        Color = Color.FromRgb(0, 0, 0),
        Opacity = 0.5
    };

    public int BackdropType => 0; // Mica look rendered natively in WPF
    public bool IsDarkMode => true;
}
