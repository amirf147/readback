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
using System.Windows;
using System.Windows.Media.Effects;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace ReadBack.App.Themes;

/// <summary>
/// High Contrast Accessibility Theme.
/// </summary>
public class HighContrastTheme : IHudTheme
{
    public string Id => "high-contrast";
    public string DisplayName => "High Contrast (Accessibility)";

    public Brush BackgroundBrush => new SolidColorBrush(Colors.Black);
    public Brush BorderBrush => new SolidColorBrush(Colors.Yellow);
    public Thickness BorderThickness => new(2.5);
    public CornerRadius CornerRadius => new(16);

    public Brush TitleForeground => new SolidColorBrush(Colors.Cyan);
    public Brush TextForeground => new SolidColorBrush(Colors.White);
    public Brush BadgeBackground => new SolidColorBrush(Colors.DarkBlue);
    public Brush BadgeForeground => new SolidColorBrush(Colors.Yellow);

    public Brush ButtonBackground => new SolidColorBrush(Colors.Black);
    public Brush ButtonHoverBackground => new SolidColorBrush(Color.FromRgb(30, 30, 30));
    public Brush ButtonPressedBackground => new SolidColorBrush(Colors.Yellow);
    public Brush ButtonForeground => new SolidColorBrush(Colors.White);

    public Effect? WindowEffect => null;
    public int BackdropType => 0; // None
    public bool IsDarkMode => true;
}
