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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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

public record IpcMessage(string Action, string? Text = null, bool Turbo = false, string? Speed = null, string? Voice = null);

public partial class App : System.Windows.Application
{
    private const string IPC_PIPE_NAME = "ReadBack_IPC_Pipe_SingleInstance";
    private static Mutex? _mutex;
    private static bool _ownsMutex;
    private static CancellationTokenSource? _ipcServerCts;
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

    private static string NormalizeSpeed(string? input, bool turbo = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return turbo ? "+100%" : "+0%";
        }

        string s = input.Trim().ToLowerInvariant();
        if (s.EndsWith("%")) return s;
        if (s.EndsWith("x")) s = s.Substring(0, s.Length - 1).Trim();

        return s switch
        {
            "0.85" => "-15%",
            "1" or "1.0" => "+0%",
            "1.15" => "+15%",
            "1.25" => "+25%",
            "1.5" => "+50%",
            "1.75" => "+75%",
            "2" or "2.0" => "+100%",
            "2.5" => "+150%",
            "3" or "3.0" => "+200%",
            _ => turbo ? "+100%" : "+0%"
        };
    }

    private static bool TrySendIpcCommand(IpcMessage msg)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", IPC_PIPE_NAME, PipeDirection.Out);
            client.Connect(300); // 300ms connection timeout
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            string json = JsonSerializer.Serialize(msg);
            writer.WriteLine(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void StartIpcServer(PlaybackController playback, TrayIconService trayIcon, IClipboardService clipboard, HudViewModel hudVm)
    {
        _ipcServerCts = new CancellationTokenSource();
        var token = _ipcServerCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        IPC_PIPE_NAME,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    string? line = await reader.ReadLineAsync(token);

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        try
                        {
                            var msg = JsonSerializer.Deserialize<IpcMessage>(line);
                            if (msg != null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    HandleIpcMessage(msg, playback, trayIcon, clipboard, hudVm);
                                });
                            }
                        }
                        catch { }
                    }
                }
                catch when (token.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    try { await Task.Delay(250, token); } catch { }
                }
            }
        }, token);
    }

    private static void HandleIpcMessage(IpcMessage msg, PlaybackController playback, TrayIconService trayIcon, IClipboardService clipboard, HudViewModel hudVm)
    {
        switch (msg.Action.ToLowerInvariant())
        {
            case "clip":
            case "speak":
            {
                string? effectiveSpeed = msg.Speed ?? (msg.Turbo ? "+100%" : null);
                string? effectiveVoice = msg.Voice;
                trayIcon.TriggerSpeakClipboard(effectiveVoice, effectiveSpeed);
                break;
            }
            case "text":
            {
                if (!string.IsNullOrWhiteSpace(msg.Text))
                {
                    string? effectiveSpeed = msg.Speed ?? (msg.Turbo ? "+100%" : null);
                    playback.PlayTextAsync(msg.Text, msg.Voice, effectiveSpeed);
                }
                break;
            }
            case "stop":
                playback.Stop();
                break;
            case "pause":
                playback.TogglePause();
                break;
            case "next":
                playback.NextChunk();
                break;
            case "prev":
                playback.PreviousChunk();
                break;
            case "hud":
                hudVm.IsVisible = !hudVm.IsVisible;
                break;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
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

        // Handle CLI Subcommands and Flags
        if (e.Args.Length > 0)
        {
            string first = e.Args[0].ToLowerInvariant();

            if (first is "--help" or "-h" or "help")
            {
                EnsureConsoleOutput();
                var ver = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
                Console.WriteLine($"ReadBack v{ver} - Instant Screen & Document Text-to-Speech");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  ReadBack.exe                       Run in system tray with floating HUD (default)");
                Console.WriteLine("  ReadBack.exe --clip, -c, speak     Read clipboard text (controls running instance or plays directly)");
                Console.WriteLine("  ReadBack.exe --turbo, -t           Read clipboard text in high-speed Turbo mode (2.0x)");
                Console.WriteLine("  ReadBack.exe --speed <rate>        Set reading speed (e.g. 1.5x, 2.0x, +100%)");
                Console.WriteLine("  ReadBack.exe --voice <name>        Select voice by name or ID");
                Console.WriteLine("  ReadBack.exe --stop                Stop active narration");
                Console.WriteLine("  ReadBack.exe --pause               Toggle pause/resume");
                Console.WriteLine("  ReadBack.exe --next                Skip to next paragraph");
                Console.WriteLine("  ReadBack.exe --prev                Jump to previous paragraph");
                Console.WriteLine("  ReadBack.exe --text <msg>          Read custom text string directly");
                Console.WriteLine("  ReadBack.exe --hud                 Toggle / show floating Heads-Up Display (HUD)");
                Console.WriteLine("  ReadBack.exe voices                List available natural online and offline voices");
                Console.WriteLine("  ReadBack.exe --version, -v         Show application version");
                Shutdown();
                Environment.Exit(0);
                return;
            }
            else if (first is "--version" or "-v" or "version")
            {
                EnsureConsoleOutput();
                var ver = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
                Console.WriteLine($"ReadBack v{ver}");
                Shutdown();
                Environment.Exit(0);
                return;
            }
            else if (first == "voices")
            {
                EnsureConsoleOutput();
                Console.WriteLine("\n--- ReadBack Available Voices ---");
                var voices = await ttsEngine.GetAvailableVoicesAsync();
                var edgeVoices = voices.Where(v => v.IsNeural).ToList();
                var sapiVoices = voices.Where(v => v.IsOffline).ToList();

                Console.WriteLine("\nNatural Online Voices (Microsoft Edge):");
                foreach (var v in edgeVoices)
                {
                    string mark = v.Id == settingsService.CurrentSettings.Voice ? " [Active]" : "";
                    string tag = v.Id switch
                    {
                        "en-US-ChristopherNeural" => " ⭐ [Recommended]",
                        "en-US-GuyNeural" => " ⚡ [Fast Reading / Articulated]",
                        "en-US-JennyNeural" => " ⚡ [Fast Reading]",
                        _ => ""
                    };
                    Console.WriteLine($"  * {v.DisplayName} ({v.Id}){tag}{mark}");
                }

                Console.WriteLine("\nInstalled System Voices (Windows Offline):");
                foreach (var v in sapiVoices)
                {
                    string tag = v.DisplayName.Contains("David", StringComparison.OrdinalIgnoreCase)
                        ? " ⚡ [Turbo / High-Speed Clarity]"
                        : "";
                    Console.WriteLine($"  * {v.DisplayName} ({v.Id}){tag}");
                }
                Console.WriteLine();
                Shutdown();
                return;
            }

            // Parse flags
            string action = "clip";
            bool isTurbo = false;
            string? customSpeed = null;
            string? customVoice = null;
            string? customText = null;

            for (int i = 0; i < e.Args.Length; i++)
            {
                string arg = e.Args[i].ToLowerInvariant();
                if (arg is "--stop" or "stop")
                {
                    action = "stop";
                }
                else if (arg is "--pause" or "-p" or "pause")
                {
                    action = "pause";
                }
                else if (arg is "--next" or "next")
                {
                    action = "next";
                }
                else if (arg is "--prev" or "prev")
                {
                    action = "prev";
                }
                else if (arg is "--clip" or "-c" or "--clipboard" or "speak")
                {
                    action = "clip";
                }
                else if (arg is "--hud" or "hud")
                {
                    action = "hud";
                }
                else if (arg is "--turbo" or "-t")
                {
                    isTurbo = true;
                }
                else if (arg is "--speed" or "-s" && i + 1 < e.Args.Length)
                {
                    customSpeed = NormalizeSpeed(e.Args[++i]);
                }
                else if (arg is "--voice" && i + 1 < e.Args.Length)
                {
                    customVoice = e.Args[++i];
                }
                else if (arg is "--text" or "text" && i + 1 < e.Args.Length)
                {
                    action = "text";
                    customText = string.Join(" ", e.Args.Skip(i + 1));
                    break;
                }
            }

            if (isTurbo && customSpeed == null)
            {
                customSpeed = "+100%"; // 2.0x Turbo speed
            }

            var ipcMsg = new IpcMessage(action, customText, isTurbo, customSpeed, customVoice);

            // Attempt to send to already running tray instance via Named Pipe
            if (TrySendIpcCommand(ipcMsg))
            {
                Shutdown(0);
                Environment.Exit(0);
                return;
            }

            // If no tray instance is running:
            if (action == "stop" || action == "pause" || action == "next" || action == "prev")
            {
                Shutdown(0);
                Environment.Exit(0);
                return;
            }

            // For clip or text, run standalone in current process
            string? textToRead = action == "text" ? customText : clipboardService.GetText();
            if (string.IsNullOrWhiteSpace(textToRead))
            {
                Shutdown(0);
                Environment.Exit(0);
                return;
            }

            var tcs = new TaskCompletionSource();
            playbackController.StateChanged += (s, ev) =>
            {
                if (ev.NewState == ReadBack.Core.Models.PlaybackState.Idle ||
                    ev.NewState == ReadBack.Core.Models.PlaybackState.Stopped)
                {
                    tcs.TrySetResult();
                }
            };

            await playbackController.PlayTextAsync(textToRead, customVoice, customSpeed);
            await tcs.Task;
            Shutdown(0);
            Environment.Exit(0);
            return;
        }

        // GUI / Tray Mode: Ensure single instance
        const string mutexName = "ReadBack_SingleInstance_Mutex_99F3B";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            Shutdown(0);
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);

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
        _trayIconService = new TrayIconService(
            playbackController,
            settingsService,
            clipboardService,
            ttsEngine,
            themeManager,
            startupService,
            showHudAction: () =>
            {
                hudVm.IsVisible = true;
                _hudWindow?.Activate();
            }
        );
        _trayIconService.ShowReadyNotification();

        // Start background IPC Server for CLI integration
        StartIpcServer(playbackController, _trayIconService, clipboardService, hudVm);

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
        _hotkeyService.HudRequested += () =>
        {
            hudVm.IsVisible = !hudVm.IsVisible;
            if (hudVm.IsVisible)
            {
                _hudWindow?.Activate();
            }
        };
        _hotkeyService.EscapeRequested += () =>
        {
            if (playbackController.IsPlaying)
            {
                playbackController.Stop();
            }
            else if (hudVm.IsVisible)
            {
                hudVm.IsVisible = false;
            }
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _ipcServerCts?.Cancel(); } catch { }
        try { _ipcServerCts?.Dispose(); } catch { }
        _hotkeyService?.Dispose();
        _trayIconService?.Dispose();
        _hudWindow?.Close();
        _hiddenWindow?.Close();
        if (_ownsMutex && _mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
        }
        try { _mutex?.Dispose(); } catch { }
        base.OnExit(e);
    }
}
