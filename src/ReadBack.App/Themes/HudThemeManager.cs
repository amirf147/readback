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
using ReadBack.Core.Services;

namespace ReadBack.App.Themes;

public interface IHudThemeManager
{
    IReadOnlyList<IHudTheme> AvailableThemes { get; }
    IHudTheme CurrentTheme { get; }
    void ApplyTheme(string themeId);
    void RegisterTheme(IHudTheme theme);
    event EventHandler<IHudTheme>? ThemeChanged;
}

public class HudThemeManager : IHudThemeManager
{
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, IHudTheme> _themes = new(StringComparer.OrdinalIgnoreCase);
    private IHudTheme _currentTheme;

    public IReadOnlyList<IHudTheme> AvailableThemes => _themes.Values.ToList();
    public IHudTheme CurrentTheme => _currentTheme;

    public event EventHandler<IHudTheme>? ThemeChanged;

    public HudThemeManager(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Register default themes
        RegisterTheme(new Windows11GlassTheme());
        RegisterTheme(new DarkMicaTheme());
        RegisterTheme(new LightGlassTheme());
        RegisterTheme(new HighContrastTheme());

        string savedTheme = _settingsService.CurrentSettings.HudTheme;
        if (!_themes.TryGetValue(savedTheme, out _currentTheme!))
        {
            _currentTheme = _themes["win11-glass"];
        }
    }

    public void RegisterTheme(IHudTheme theme)
    {
        _themes[theme.Id] = theme;
    }

    public void ApplyTheme(string themeId)
    {
        if (_themes.TryGetValue(themeId, out var theme))
        {
            _currentTheme = theme;
            _settingsService.CurrentSettings.HudTheme = themeId;
            _settingsService.Save();
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }
}
