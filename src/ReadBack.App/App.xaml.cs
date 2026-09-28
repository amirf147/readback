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
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ReadBack.App.Services;
using ReadBack.App.ViewModels;
using ReadBack.App.Views;
using ReadBack.Core.Chunking;
using ReadBack.Core.Filters;
using ReadBack.Core.Services;
using ReadBack.Core.TTS;

namespace ReadBack.App;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private TrayIconService? _trayIconService;
    private WindowsHotkeyService? _hotkeyService;
    private MiniFloatingHud? _hudWindow;
    private Window? _hiddenWindow;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    private static void EnsureConsoleOutput()
    {
        try
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(standardOutput);
                var standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetError(standardError);
            }
        }
        catch { }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        EnsureConsoleOutput();

        var settingsService = new SettingsService();
        var pipeline = new NarrationPipeline();
        var chunker = new SpeechChunker();
        var ttsEngine = new CompositeTTSEngine();
        var clipboardService = new WindowsClipboardService();
        var audioPlayer = new WindowsAudioPlayer();

        var playbackController = new PlaybackController(
            pipeline,
            chunker,
            ttsEngine,
            settingsService,
            filePath => audioPlayer.PlayFileAsync(filePath),
            () => audioPlayer.Stop(),
            () => audioPlayer.Pause(),
            () => audioPlayer.Resume()
        );

        // Handle CLI Subcommands
        if (e.Args.Length > 0)
        {
            string cmd = e.Args[0].ToLowerInvariant();

            if (cmd == "voices")
            {
                Console.WriteLine("\n--- ReadBack Available Voices ---");
                var voices = await ttsEngine.GetAvailableVoicesAsync();
                var edgeVoices = voices.Where(v => v.IsNeural).ToList();
                var sapiVoices = voices.Where(v => v.IsOffline).ToList();

                Console.WriteLine("\nNatural Online Voices (Microsoft Edge):");
                foreach (var v in edgeVoices)
                {
                    string mark = v.Id == settingsService.CurrentSettings.Voice ? " [Active]" : "";
                    Console.WriteLine($"  * {v.DisplayName} ({v.Id}){mark}");
                }

                Console.WriteLine("\nInstalled System Voices (Windows Offline):");
                foreach (var v in sapiVoices)
                {
                    Console.WriteLine($"  * {v.DisplayName} ({v.Id})");
                }
                Console.WriteLine();
                Shutdown();
                return;
            }
            else if (cmd == "speak")
            {
                string? text = clipboardService.GetText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("Clipboard is empty or contains no text.");
                    Shutdown();
                    return;
                }

                Console.WriteLine($"Reading clipboard text ({text.Length} chars)...");
                var tcs = new TaskCompletionSource();
                playbackController.StateChanged += (s, ev) =>
                {
                    if (ev.NewState == ReadBack.Core.Models.PlaybackState.Idle ||
                        ev.NewState == ReadBack.Core.Models.PlaybackState.Stopped)
                    {
                        tcs.TrySetResult();
                    }
                };

                await playbackController.PlayTextAsync(text);
                await tcs.Task;
                Shutdown();
                return;
            }
            else if (cmd == "text" && e.Args.Length > 1)
            {
                string text = string.Join(" ", e.Args.Skip(1));
                Console.WriteLine($"Reading text: {text}");
                var tcs = new TaskCompletionSource();
                playbackController.StateChanged += (s, ev) =>
                {
                    if (ev.NewState == ReadBack.Core.Models.PlaybackState.Idle ||
                        ev.NewState == ReadBack.Core.Models.PlaybackState.Stopped)
                    {
                        tcs.TrySetResult();
                    }
                };

                await playbackController.PlayTextAsync(text);
                await tcs.Task;
                Shutdown();
                return;
            }
            else if (cmd == "--help" || cmd == "-h" || cmd == "help")
            {
                Console.WriteLine("ReadBack - Instant Clipboard Text-to-Speech");
                Console.WriteLine("Usage:");
                Console.WriteLine("  ReadBack.exe         Run in system tray with floating HUD (default)");
                Console.WriteLine("  ReadBack.exe speak   Read clipboard text directly in console");
                Console.WriteLine("  ReadBack.exe text    Read custom text string directly");
                Console.WriteLine("  ReadBack.exe voices  List available natural online and offline voices");
                Shutdown();
                return;
            }
        }

        // GUI / Tray Mode: Ensure single instance
        const string mutexName = "ReadBack_SingleInstance_Mutex_99F3B";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            Console.WriteLine("ReadBack is already running in your system tray. Press Ctrl+Alt+C to speak clipboard.");
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Console.WriteLine("\n=======================================================");
        Console.WriteLine(" 🎙️ ReadBack is running in your System Tray!");
        Console.WriteLine($" • Active Voice: {settingsService.CurrentSettings.Voice}");
        Console.WriteLine(" • Hotkey:       Ctrl + Alt + C  (Narrate clipboard)");
        Console.WriteLine(" • Play/Pause:   Ctrl + Alt + Space");
        Console.WriteLine(" • Stop:         Ctrl + Alt + X or Esc");
        Console.WriteLine(" • Next/Prev:    Ctrl + Alt + Right / Left");
        Console.WriteLine(" • Tray Menu:    Right-click tray icon to change voices/themes");
        Console.WriteLine("=======================================================\n");

        // Sound feedback
        playbackController.StateChanged += (s, ev) =>
        {
            if (settingsService.CurrentSettings.SoundFeedback)
            {
                if (ev.NewState == ReadBack.Core.Models.PlaybackState.Playing)
                {
                    try { Console.Beep(900, 50); } catch { }
                }
            }
        };

        // Theming & HUD UI
        var themeManager = new Themes.HudThemeManager(settingsService);
        var hudVm = new HudViewModel(playbackController, settingsService, themeManager);
        _hudWindow = new MiniFloatingHud(hudVm);

        // Windows Startup Registration & Synchronization
        var startupService = new WindowsStartupService();
        if (settingsService.CurrentSettings.LaunchAtStartup && !startupService.IsStartupEnabled())
        {
            startupService.SetStartup(true);
        }

        // Tray Icon
        _trayIconService = new TrayIconService(playbackController, settingsService, clipboardService, ttsEngine, themeManager, startupService);
        _trayIconService.CheckAndShowFirstRunPrompt();

        // Hidden window for Win32 message loop & hotkeys
        _hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Visibility = Visibility.Hidden
        };
        _hiddenWindow.Show();

        nint hwnd = new WindowInteropHelper(_hiddenWindow).Handle;
        _hotkeyService = new WindowsHotkeyService(settingsService);
        _hotkeyService.Initialize(hwnd);

        // Wire hotkey events to tray and controller
        _hotkeyService.SpeakRequested += () => _trayIconService.TriggerSpeakClipboard();
        _hotkeyService.StopRequested += () => playbackController.Stop();
        _hotkeyService.PauseRequested += () => playbackController.TogglePause();
        _hotkeyService.NextRequested += () => playbackController.NextChunk();
        _hotkeyService.PrevRequested += () => playbackController.PreviousChunk();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayIconService?.Dispose();
        _hudWindow?.Close();
        _hiddenWindow?.Close();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
