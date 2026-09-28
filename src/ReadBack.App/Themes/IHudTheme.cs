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

namespace ReadBack.App.Themes;

public interface IHudTheme
{
    string Id { get; }
    string DisplayName { get; }
    Brush BackgroundBrush { get; }
    Brush BorderBrush { get; }
    Thickness BorderThickness { get; }
    CornerRadius CornerRadius { get; }
    Brush TitleForeground { get; }
    Brush TextForeground { get; }
    Brush BadgeBackground { get; }
    Brush BadgeForeground { get; }
    Brush ButtonBackground { get; }
    Brush ButtonHoverBackground { get; }
    Brush ButtonPressedBackground { get; }
    Brush ButtonForeground { get; }
    Effect? WindowEffect { get; }
    int BackdropType { get; } // 0 = None, 2 = Mica, 3 = Acrylic
    bool IsDarkMode { get; }
}
