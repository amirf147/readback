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

    public TrayIconService(
        IPlaybackController playback,
        ISettingsService settingsService,
        IClipboardService clipboardService,
        CompositeTTSEngine ttsEngine)
    {
        _playback = playback;
        _settingsService = settingsService;
        _clipboardService = clipboardService;
        _ttsEngine = ttsEngine;

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
            _playback.Stop();
            return;
        }

        string? text = _clipboardService.GetText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            _playback.PlayTextAsync(text);
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
        var voiceMenu = new ToolStripMenuItem("Voice");
        try
        {
            var voices = await _ttsEngine.GetAvailableVoicesAsync();
            foreach (var v in voices)
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
        catch { }
        menu.Items.Add(voiceMenu);

        // 3. Reading Speed Submenu
        var speedMenu = new ToolStripMenuItem("Reading Speed");
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

        // 4. Floating HUD Toggle
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

        // 5. Code Block Skipping
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

        // 6. Sound Chimes
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

        // 7. Offline Only
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

        // 8. Exit
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
