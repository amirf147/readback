using System.Media;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "ReadBack_SingleInstance_Mutex_99F3B";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "ReadBack is already running in your system tray.\nPress Ctrl+Alt+C to speak clipboard.",
                "ReadBack",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Core services
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

        // Sound feedback
        playbackController.StateChanged += (s, ev) =>
        {
            if (settingsService.CurrentSettings.SoundFeedback)
            {
                if (ev.NewState == ReadBack.Core.Models.PlaybackState.Playing)
                    SystemSounds.Asterisk.Play();
            }
        };

        // UI: Floating HUD
        var hudVm = new HudViewModel(playbackController, settingsService);
        _hudWindow = new MiniFloatingHud(hudVm);

        // Tray Icon
        _trayIconService = new TrayIconService(playbackController, settingsService, clipboardService, ttsEngine);

        // Hidden window for Win32 RegisterHotKey hook
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

        // Hotkey events
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
