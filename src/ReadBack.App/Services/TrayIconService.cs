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
    private readonly IPlaybackController _playback;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboardService;
    private readonly CompositeTTSEngine _ttsEngine;
    private readonly Themes.IHudThemeManager _themeManager;
    private readonly IStartupService _startupService;

    public TrayIconService(
        IPlaybackController playback,
        ISettingsService settingsService,
        IClipboardService clipboardService,
        CompositeTTSEngine ttsEngine,
        Themes.IHudThemeManager themeManager,
        IStartupService? startupService = null)
    {
        _playback = playback;
        _settingsService = settingsService;
        _clipboardService = clipboardService;
        _ttsEngine = ttsEngine;
        _themeManager = themeManager;
        _startupService = startupService ?? new WindowsStartupService();

        _notifyIcon = new NotifyIcon();
        LoadIcon();
        BuildContextMenu();

        _notifyIcon.Visible = true;
        _notifyIcon.Text = "ReadBack - Instant Clipboard Narrator";
        _notifyIcon.DoubleClick += (s, e) => TriggerSpeakClipboard();
        _notifyIcon.Click += (s, e) =>
        {
            if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
            {
                TriggerSpeakClipboard();
            }
        };

        _playback.StateChanged += (s, e) => UpdateTooltip(e.NewState);
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
            _notifyIcon.Icon = new Icon(iconPath);
        }
        else
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }
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

    public void TriggerSpeakClipboard()
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
            _playback.PlayTextAsync(text);
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

        menu.Items.Add(itemSpeak);
        menu.Items.Add(itemPause);
        menu.Items.Add(itemStop);
        menu.Items.Add(itemNext);
        menu.Items.Add(itemPrev);
        menu.Items.Add(new ToolStripSeparator());

        // 2. Voice Submenu
        var voiceMenu = new ToolStripMenuItem("🗣️ Voice Selection");
        try
        {
            var voices = await _ttsEngine.GetAvailableVoicesAsync();

            // Group: Natural Online Voices (Edge)
            var edgeVoices = voices.Where(v => v.IsNeural).ToList();
            if (edgeVoices.Count > 0)
            {
                var edgeHeader = new ToolStripMenuItem("Natural Online Voices (Edge)") { Enabled = false };
                voiceMenu.DropDownItems.Add(edgeHeader);

                foreach (var v in edgeVoices)
                {
                    string label = v.Id == "en-US-ChristopherNeural"
                        ? $"⭐ {v.DisplayName} [Recommended]"
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
                    voiceMenu.DropDownItems.Add(vItem);
                }
            }

            // Group: Installed Windows Offline Voices (SAPI)
            var sapiVoices = voices.Where(v => v.IsOffline).ToList();
            if (sapiVoices.Count > 0)
            {
                if (edgeVoices.Count > 0)
                    voiceMenu.DropDownItems.Add(new ToolStripSeparator());

                var sapiHeader = new ToolStripMenuItem("Installed Windows Voices (Offline)") { Enabled = false };
                voiceMenu.DropDownItems.Add(sapiHeader);

                foreach (var v in sapiVoices)
                {
                    var vItem = new ToolStripMenuItem(v.DisplayName, null, (s, e) =>
                    {
                        currentSettings.Voice = v.Id;
                        _settingsService.Save();
                        BuildContextMenu();
                    })
                    {
                        Checked = currentSettings.Voice.Equals(v.Id, StringComparison.OrdinalIgnoreCase)
                    };
                    voiceMenu.DropDownItems.Add(vItem);
                }
            }

            voiceMenu.DropDownItems.Add(new ToolStripSeparator());

            // Add shortcut to Windows Voice Settings for downloading more voices
            var downloadVoicesItem = new ToolStripMenuItem("📥 Download More Windows Voices (Settings)...", null, (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:speech") { UseShellExecute = true });
                }
                catch { }
            });
            voiceMenu.DropDownItems.Add(downloadVoicesItem);
        }
        catch { }
        menu.Items.Add(voiceMenu);

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
        string[] speeds = { "-15%", "+0%", "+15%", "+25%", "+50%" };
        string[] speedLabels = { "0.85x (Slower)", "1.0x (Normal)", "1.15x (Brisk)", "1.25x (Fast)", "1.5x (Super Fast)" };

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
            _settingsService.Save();
            BuildContextMenu();
        })
        {
            Checked = currentSettings.OfflineOnly
        };
        menu.Items.Add(offlineItem);
        menu.Items.Add(new ToolStripSeparator());

        // 9. User Guide & Documentation
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

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
