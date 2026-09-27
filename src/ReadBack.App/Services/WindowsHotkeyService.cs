using System.Runtime.InteropServices;
using System.Windows.Interop;
using ReadBack.Core.Services;

namespace ReadBack.App.Services;

public class WindowsHotkeyService : IHotkeyService
{
    private readonly ISettingsService _settingsService;
    private nint _hwnd;
    private HwndSource? _source;

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int HOTKEY_ID_SPEAK = 9001;
    private const int HOTKEY_ID_STOP = 9002;
    private const int HOTKEY_ID_PAUSE = 9003;
    private const int HOTKEY_ID_NEXT = 9004;
    private const int HOTKEY_ID_PREV = 9005;

    public event Action? SpeakRequested;
    public event Action? StopRequested;
    public event Action? PauseRequested;
    public event Action? NextRequested;
    public event Action? PrevRequested;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    public WindowsHotkeyService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize(nint windowHandle)
    {
        _hwnd = windowHandle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(HwndHook);
        RegisterHotkeys();
    }

    public void RegisterHotkeys()
    {
        if (_hwnd == 0) return;
        UnregisterHotkeys();

        RegisterHotKey(_hwnd, HOTKEY_ID_SPEAK, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x43); // 'C'
        RegisterHotKey(_hwnd, HOTKEY_ID_STOP, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x58);  // 'X'
        RegisterHotKey(_hwnd, HOTKEY_ID_PAUSE, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x20); // Space
        RegisterHotKey(_hwnd, HOTKEY_ID_NEXT, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x27);  // Right Arrow
        RegisterHotKey(_hwnd, HOTKEY_ID_PREV, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x25);  // Left Arrow
    }

    public void UnregisterHotkeys()
    {
        if (_hwnd == 0) return;
        UnregisterHotKey(_hwnd, HOTKEY_ID_SPEAK);
        UnregisterHotKey(_hwnd, HOTKEY_ID_STOP);
        UnregisterHotKey(_hwnd, HOTKEY_ID_PAUSE);
        UnregisterHotKey(_hwnd, HOTKEY_ID_NEXT);
        UnregisterHotKey(_hwnd, HOTKEY_ID_PREV);
    }

    private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_ID_SPEAK:
                    SpeakRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_STOP:
                    StopRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_PAUSE:
                    PauseRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_NEXT:
                    NextRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_PREV:
                    PrevRequested?.Invoke();
                    handled = true;
                    break;
            }
        }
        return 0;
    }

    public void Dispose()
    {
        UnregisterHotkeys();
        _source?.RemoveHook(HwndHook);
        _source?.Dispose();
        _source = null;
    }
}
