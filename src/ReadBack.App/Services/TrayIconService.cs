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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ReadBack.Core.Models;
using ReadBack.Core.Services;
using ReadBack.Core.TTS;

namespace ReadBack.App.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private Icon? _loadedIcon;
    private readonly IPlaybackController _playback;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboardService;
    private readonly CompositeTTSEngine _ttsEngine;
    private readonly Themes.IHudThemeManager _themeManager;
    private readonly IStartupService _startupService;
    private readonly Action? _showHudAction;

    public TrayIconService(
        IPlaybackController playback,
        ISettingsService settingsService,
        IClipboardService clipboardService,
        CompositeTTSEngine ttsEngine,
        Themes.IHudThemeManager themeManager,
        IStartupService? startupService = null,
        Action? showHudAction = null)
    {
        _playback = playback;
        _settingsService = settingsService;
        _clipboardService = clipboardService;
        _ttsEngine = ttsEngine;
        _themeManager = themeManager;
        _startupService = startupService ?? new WindowsStartupService();
        _showHudAction = showHudAction;

        _notifyIcon = new NotifyIcon();
        LoadIcon();
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "ReadBack - Instant Clipboard Narrator";
        BuildContextMenu();

        _notifyIcon.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _showHudAction?.Invoke();
            }
        };
        _notifyIcon.Click += (s, e) =>
        {
            if (e is not MouseEventArgs)
            {
                _showHudAction?.Invoke();
            }
        };

        _playback.StateChanged += (s, e) => UpdateTooltip(e.NewState);
    }

    public void ShowReadyNotification()
    {
        try
        {
            _notifyIcon.ShowBalloonTip(
                2500,
                "ReadBack Active 🎙️",
                "Running in system tray. Press Ctrl+Alt+C to speak clipboard.",
                ToolTipIcon.Info
            );
        }
        catch { }
    }

    public void CheckAndShowFirstRunPrompt()
    {
        var settings = _settingsService.CurrentSettings;
        if (settings.FirstRunPromptShown) return;

        settings.FirstRunPromptShown = true;
        _settingsService.Save();

        _notifyIcon.BalloonTipClicked += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:speech") { UseShellExecute = true });
            }
            catch { }
        };

        _notifyIcon.ShowBalloonTip(
            7000,
            "ReadBack Ready 🎙️ (Ctrl+Alt+C)",
            "Active voice: Microsoft Christopher (Natural Online).\nPress Ctrl+Alt+C to speak. Click here to view/download more Windows voices.",
            ToolTipIcon.Info
        );
    }

    private void LoadIcon()
    {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(iconPath))
        {
            try
            {
                _loadedIcon = new Icon(iconPath);
                _notifyIcon.Icon = _loadedIcon;
                return;
            }
            catch { }
        }

        try
        {
            string? exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                _loadedIcon = Icon.ExtractAssociatedIcon(exe);
                if (_loadedIcon != null)
                {
                    _notifyIcon.Icon = _loadedIcon;
                    return;
                }
            }
        }
        catch { }

        _notifyIcon.Icon = SystemIcons.Application;
    }

    private void UpdateTooltip(PlaybackState state)
    {
        string tooltip = state switch
        {
            PlaybackState.Playing => "ReadBack - Reading... (Ctrl+Alt+X to stop)",
            PlaybackState.Paused => "ReadBack - Paused (Ctrl+Alt+Space to resume)",
            _ => "ReadBack - Idle (Ctrl+Alt+C to speak)"
        };
        if (tooltip.Length >= 64) tooltip = tooltip.Substring(0, 63);
        _notifyIcon.Text = tooltip;
    }

    public void TriggerSpeakClipboard(string? overrideVoice = null, string? overrideSpeed = null)
    {
        if (_playback.IsPlaying)
        {
            if (_settingsService.CurrentSettings.SoundFeedback)
            {
                try { Console.Beep(500, 70); } catch { }
            }
            _playback.Stop();
            return;
        }

        if (_settingsService.CurrentSettings.SoundFeedback)
        {
            try { Console.Beep(900, 60); } catch { }
        }

        string? text = _clipboardService.GetText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            _playback.PlayTextAsync(text, overrideVoice, overrideSpeed);
        }
        else
        {
            if (_settingsService.CurrentSettings.SoundFeedback)
            {
                try { Console.Beep(400, 80); } catch { }
            }
            _notifyIcon.ShowBalloonTip(
                3500,
                "Clipboard Empty",
                "Copy some text first (Ctrl+C), then press Ctrl+Alt+C to listen.",
                ToolTipIcon.Info
            );
        }
    }

    public async void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        var currentSettings = _settingsService.CurrentSettings;

        // 1. Controls
        var itemSpeak = new ToolStripMenuItem("Narrate Clipboard\tCtrl+Alt+C", null, (s, e) => TriggerSpeakClipboard());
        var itemPause = new ToolStripMenuItem("Pause / Resume\tCtrl+Alt+Space", null, (s, e) => _playback.TogglePause());
        var itemStop = new ToolStripMenuItem("Stop Narration\tCtrl+Alt+X", null, (s, e) => _playback.Stop());
        var itemNext = new ToolStripMenuItem("Next Paragraph\tCtrl+Alt+Right", null, (s, e) => _playback.NextChunk());
        var itemPrev = new ToolStripMenuItem("Previous Paragraph\tCtrl+Alt+Left", null, (s, e) => _playback.PreviousChunk());
        var itemHud = new ToolStripMenuItem("Show Heads-Up Display\tCtrl+Alt+H", null, (s, e) => _showHudAction?.Invoke());

        menu.Items.Add(itemSpeak);
        menu.Items.Add(itemPause);
        menu.Items.Add(itemStop);
        menu.Items.Add(itemNext);
        menu.Items.Add(itemPrev);
        menu.Items.Add(itemHud);
        menu.Items.Add(new ToolStripSeparator());

        // 2. Neural Voice Selection Submenu
        var neuralMenu = new ToolStripMenuItem("🌐 Neural Voice Selection");
        // 3. SAPI Voice Selection Submenu
        var sapiMenu = new ToolStripMenuItem("🖥️ SAPI Voice Selection");

        try
        {
            var voices = await _ttsEngine.GetAvailableVoicesAsync();

            // Group: Natural Online Voices (Edge)
            var edgeVoices = voices.Where(v => v.IsNeural).ToList();

            // Filter to English neural voices or user's whitelist
            List<VoiceInfo> filteredNeural;
            if (currentSettings.NeuralVoiceWhitelist != null && currentSettings.NeuralVoiceWhitelist.Count > 0)
            {
                filteredNeural = edgeVoices.Where(v =>
                    currentSettings.NeuralVoiceWhitelist.Any(w =>
                        v.Id.Equals(w, StringComparison.OrdinalIgnoreCase) ||
                        v.Id.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                        v.DisplayName.Contains(w, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
            else
            {
                filteredNeural = edgeVoices.Where(v =>
                    v.Locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase) ||
                    v.Id.StartsWith("en-", StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var priorityIds = new[]
            {
                "en-US-ChristopherNeural",
                "en-US-GuyNeural",
                "en-US-JennyNeural",
                "en-US-AriaNeural",
                "en-US-EricNeural",
                "en-US-EmmaMultilingualNeural",
                "en-GB-SoniaNeural",
                "en-GB-RyanNeural"
            };

            var featured = filteredNeural
                .Where(v => priorityIds.Any(p => p.Equals(v.Id, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(v => Array.FindIndex(priorityIds, p => p.Equals(v.Id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var others = filteredNeural
                .Where(v => !priorityIds.Any(p => p.Equals(v.Id, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(v => v.DisplayName)
                .ToList();

            void AddNeuralVoiceItem(VoiceInfo v, ToolStripMenuItem targetMenu)
            {
                string label = v.Id switch
                {
                    "en-US-ChristopherNeural" => $"⭐ {v.DisplayName} [Recommended]",
                    "en-US-GuyNeural" => $"⚡ {v.DisplayName} [Fast Reading / Articulated]",
                    "en-US-JennyNeural" => $"⚡ {v.DisplayName} [Fast Reading]",
                    _ => v.DisplayName
                };

                var vItem = new ToolStripMenuItem(label, null, (s, e) =>
                {
                    currentSettings.Voice = v.Id;
                    if (currentSettings.OfflineOnly)
                    {
                        currentSettings.OfflineOnly = false;
                    }
                    _settingsService.Save();
                    BuildContextMenu();
                })
                {
                    Checked = !currentSettings.OfflineOnly && currentSettings.Voice.Equals(v.Id, StringComparison.OrdinalIgnoreCase)
                };
                targetMenu.DropDownItems.Add(vItem);
            }

            foreach (var v in featured)
            {
                AddNeuralVoiceItem(v, neuralMenu);
            }

            if (others.Count > 0)
            {
                if (featured.Count > 0)
                    neuralMenu.DropDownItems.Add(new ToolStripSeparator());

                if (currentSettings.NeuralVoiceWhitelist != null && currentSettings.NeuralVoiceWhitelist.Count > 0)
                {
                    foreach (var v in others)
                    {
                        AddNeuralVoiceItem(v, neuralMenu);
                    }
                }
                else
                {
                    var moreMenu = new ToolStripMenuItem($"More English Voices ({others.Count})...");
                    foreach (var v in others)
                    {
                        AddNeuralVoiceItem(v, moreMenu);
                    }
                    neuralMenu.DropDownItems.Add(moreMenu);
                }
            }

            neuralMenu.DropDownItems.Add(new ToolStripSeparator());
            var configVoicesItem = new ToolStripMenuItem("⚙️ Configure Voices in settings.json...", null, (s, e) => OpenConfigFile());
            neuralMenu.DropDownItems.Add(configVoicesItem);

            // Group: Installed Windows Offline Voices (SAPI)
            var sapiVoices = voices.Where(v => v.IsOffline).ToList();
            if (sapiVoices.Count > 0)
            {
                foreach (var v in sapiVoices)
                {
                    string label = v.DisplayName.Contains("David", StringComparison.OrdinalIgnoreCase)
                        ? $"⚡ {v.DisplayName} [Turbo / High-Speed Clarity]"
                        : v.DisplayName;

                    var vItem = new ToolStripMenuItem(label, null, (s, e) =>
                    {
                        currentSettings.Voice = v.Id;
                        _settingsService.Save();
                        BuildContextMenu();
                    })
                    {
                        Checked = currentSettings.Voice.Equals(v.Id, StringComparison.OrdinalIgnoreCase)
                    };
                    sapiMenu.DropDownItems.Add(vItem);
                }
            }

            sapiMenu.DropDownItems.Add(new ToolStripSeparator());
            var downloadVoicesItem = new ToolStripMenuItem("📥 Download More Windows Voices (Settings)...", null, (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:speech") { UseShellExecute = true });
                }
                catch { }
            });
            sapiMenu.DropDownItems.Add(downloadVoicesItem);
        }
        catch { }
        menu.Items.Add(neuralMenu);
        menu.Items.Add(sapiMenu);

        // 3. HUD Themes Submenu
        var themeSubMenu = new ToolStripMenuItem("🎨 HUD Theme");
        foreach (var theme in _themeManager.AvailableThemes)
        {
            var tItem = new ToolStripMenuItem(theme.DisplayName, null, (s, e) =>
            {
                _themeManager.ApplyTheme(theme.Id);
                BuildContextMenu();
            })
            {
                Checked = _themeManager.CurrentTheme.Id.Equals(theme.Id, StringComparison.OrdinalIgnoreCase)
            };
            themeSubMenu.DropDownItems.Add(tItem);
        }
        menu.Items.Add(themeSubMenu);

        // 4. Reading Speed Submenu
        var speedMenu = new ToolStripMenuItem("⚡ Reading Speed");
        string[] speeds = { "-15%", "+0%", "+15%", "+25%", "+50%", "+75%", "+100%", "+150%", "+200%" };
        string[] speedLabels = {
            "0.85x (Slower)",
            "1.0x (Normal)",
            "1.15x (Brisk)",
            "1.25x (Fast)",
            "1.5x (Very Fast)",
            "1.75x (Rapid)",
            "2.0x (⚡ Turbo - 2x)",
            "2.5x (⚡ Super Turbo - 2.5x)",
            "3.0x (⚡ Extreme Limit - 3x)"
        };

        for (int i = 0; i < speeds.Length; i++)
        {
            string spd = speeds[i];
            string lbl = speedLabels[i];
            var spdItem = new ToolStripMenuItem(lbl, null, (s, e) =>
            {
                currentSettings.Speed = spd;
                _settingsService.Save();
                BuildContextMenu();
            })
            {
                Checked = currentSettings.Speed == spd
            };
            speedMenu.DropDownItems.Add(spdItem);
        }
        menu.Items.Add(speedMenu);
        menu.Items.Add(new ToolStripSeparator());

        // 5. Startup at Windows Login
        var startupItem = new ToolStripMenuItem("🚀 Launch at Windows Startup", null, (s, e) =>
        {
            bool newState = !_startupService.IsStartupEnabled();
            _startupService.SetStartup(newState);
            currentSettings.LaunchAtStartup = newState;
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = _startupService.IsStartupEnabled()
        };
        menu.Items.Add(startupItem);

        // 6. Floating HUD Toggle
        var hudItem = new ToolStripMenuItem("Show Floating HUD (Win+H style)", null, (s, e) =>
        {
            currentSettings.ShowFloatingHud = !currentSettings.ShowFloatingHud;
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = currentSettings.ShowFloatingHud
        };
        menu.Items.Add(hudItem);

        // 6. Code Block Skipping
        var codeItem = new ToolStripMenuItem("Skip Code Snippets", null, (s, e) =>
        {
            currentSettings.SkipCodeBlocks = !currentSettings.SkipCodeBlocks;
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = currentSettings.SkipCodeBlocks
        };
        menu.Items.Add(codeItem);

        // 7. Sound Chimes
        var soundItem = new ToolStripMenuItem("Sound Feedback Chimes", null, (s, e) =>
        {
            currentSettings.SoundFeedback = !currentSettings.SoundFeedback;
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = currentSettings.SoundFeedback
        };
        menu.Items.Add(soundItem);

        // 8. Offline Only
        var offlineItem = new ToolStripMenuItem("Offline Only (Windows SAPI)", null, (s, e) =>
        {
            currentSettings.OfflineOnly = !currentSettings.OfflineOnly;
            if (currentSettings.OfflineOnly)
            {
                if (!currentSettings.Voice.StartsWith("sapi:", StringComparison.OrdinalIgnoreCase) &&
                    !currentSettings.Voice.StartsWith("Windows:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSettings.Voice = "sapi:Microsoft David Desktop";
                }
            }
            else
            {
                if (currentSettings.Voice.StartsWith("sapi:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSettings.Voice = "en-US-ChristopherNeural";
                }
            }
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = currentSettings.OfflineOnly
        };
        menu.Items.Add(offlineItem);
        menu.Items.Add(new ToolStripSeparator());

        // 9. User Guide & Settings Configuration
        var guideItem = new ToolStripMenuItem("📖 User Guide & Help...", null, (s, e) =>
        {
            try
            {
                string guidePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "USER_GUIDE.md");
                if (!File.Exists(guidePath))
                {
                    guidePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "README.md");
                }
                if (File.Exists(guidePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(guidePath) { UseShellExecute = true });
                }
            }
            catch { }
        });
        menu.Items.Add(guideItem);

        var settingsFileItem = new ToolStripMenuItem("⚙️ Open Settings Configuration (settings.json)...", null, (s, e) => OpenConfigFile());
        menu.Items.Add(settingsFileItem);
        menu.Items.Add(new ToolStripSeparator());

        // 10. Exit
        var exitItem = new ToolStripMenuItem("Exit ReadBack", null, (s, e) =>
        {
            _playback.Stop();
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        });
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ShowContextMenu()
    {
        try
        {
            var method = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(_notifyIcon, null);
                return;
            }
        }
        catch { }

        _notifyIcon.ContextMenuStrip?.Show(Cursor.Position);
    }

    private void OpenConfigFile()
    {
        try
        {
            string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReadBack");
            string settingsPath = Path.Combine(settingsDir, "settings.json");
            if (!File.Exists(settingsPath))
            {
                _settingsService.Save();
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(settingsPath) { UseShellExecute = true });
        }
        catch { }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _loadedIcon?.Dispose();
    }
}
