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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ReadBack.Core.Services;

namespace ReadBack.App.Services;

public class WindowsHotkeyService : IHotkeyService
{
    private readonly ISettingsService _settingsService;
    private nint _hwnd;
    private HwndSource? _source;

    // Windows API constants
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_HOTKEY = 0x0312;
    private const long DuplicateTriggerWindowMs = 75;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int VK_C = 0x43;
    private const int VK_X = 0x58;
    private const int VK_SPACE = 0x20;
    private const int VK_RIGHT = 0x27;
    private const int VK_LEFT = 0x25;
    private const int VK_H = 0x48;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt key

    private const int HOTKEY_ID_SPEAK = 9001;
    private const int HOTKEY_ID_STOP = 9002;
    private const int HOTKEY_ID_PAUSE = 9003;
    private const int HOTKEY_ID_NEXT = 9004;
    private const int HOTKEY_ID_PREV = 9005;
    private const int HOTKEY_ID_HUD = 9006;

    public event Action? SpeakRequested;
    public event Action? StopRequested;
    public event Action? PauseRequested;
    public event Action? NextRequested;
    public event Action? PrevRequested;
    public event Action? HudRequested;
    public event Action? EscapeRequested;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);
    private LowLevelKeyboardProc? _llProc;
    private nint _hookId = 0;
    private readonly HashSet<int> _pressedKeys = new();
    private readonly object _pressedKeysLock = new();
    private readonly HashSet<int> _registeredHotkeys = new();
    private readonly object _triggerLock = new();
    private readonly Dictionary<int, long> _lastTriggerTimes = new();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

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
        InstallLowLevelHook();
    }

    private void InstallLowLevelHook()
    {
        try
        {
            _llProc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            nint moduleHandle = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _llProc, moduleHandle, 0);
        }
        catch { }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
            {
                lock (_pressedKeysLock)
                    _pressedKeys.Remove(vkCode);
            }
            else if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            {
                bool isFirstKeyDown;
                lock (_pressedKeysLock)
                    isFirstKeyDown = _pressedKeys.Add(vkCode);

                if (isFirstKeyDown)
                {
                    bool ctrl = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                    bool alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

                    if (ctrl && alt)
                    {
                        switch (vkCode)
                        {
                            case VK_C when !_registeredHotkeys.Contains(HOTKEY_ID_SPEAK):
                                TriggerSpeak();
                                break;
                            case VK_X when !_registeredHotkeys.Contains(HOTKEY_ID_STOP):
                                TriggerStop();
                                break;
                            case VK_SPACE when !_registeredHotkeys.Contains(HOTKEY_ID_PAUSE):
                                TriggerPause();
                                break;
                            case VK_RIGHT when !_registeredHotkeys.Contains(HOTKEY_ID_NEXT):
                                TriggerNext();
                                break;
                            case VK_LEFT when !_registeredHotkeys.Contains(HOTKEY_ID_PREV):
                                TriggerPrev();
                                break;
                            case VK_H when !_registeredHotkeys.Contains(HOTKEY_ID_HUD):
                                TriggerHud();
                                break;
                        }
                    }
                    else if (vkCode == VK_ESCAPE)
                    {
                        TriggerEscape();
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void RegisterHotkeys()
    {
        if (_hwnd == 0) return;
        UnregisterHotkeys();

        TryRegisterHotkey(HOTKEY_ID_SPEAK, VK_C);
        TryRegisterHotkey(HOTKEY_ID_STOP, VK_X);
        TryRegisterHotkey(HOTKEY_ID_PAUSE, VK_SPACE);
        TryRegisterHotkey(HOTKEY_ID_NEXT, VK_RIGHT);
        TryRegisterHotkey(HOTKEY_ID_PREV, VK_LEFT);
        TryRegisterHotkey(HOTKEY_ID_HUD, VK_H);
    }

    private void TryRegisterHotkey(int id, int virtualKey)
    {
        if (RegisterHotKey(_hwnd, id, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, (uint)virtualKey))
        {
            _registeredHotkeys.Add(id);
        }
        else
        {
            Console.Error.WriteLine($"[Hotkeys] Failed to register hotkey ID {id}; using the low-level keyboard hook as fallback.");
        }
    }

    public void UnregisterHotkeys()
    {
        if (_hwnd == 0) return;
        foreach (int id in _registeredHotkeys)
            UnregisterHotKey(_hwnd, id);
        _registeredHotkeys.Clear();
    }

    private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_ID_SPEAK:
                    TriggerSpeak();
                    handled = true;
                    break;
                case HOTKEY_ID_STOP:
                    TriggerStop();
                    handled = true;
                    break;
                case HOTKEY_ID_PAUSE:
                    TriggerPause();
                    handled = true;
                    break;
                case HOTKEY_ID_NEXT:
                    TriggerNext();
                    handled = true;
                    break;
                case HOTKEY_ID_PREV:
                    TriggerPrev();
                    handled = true;
                    break;
                case HOTKEY_ID_HUD:
                    TriggerHud();
                    handled = true;
                    break;
            }
        }
        return 0;
    }

    private void TriggerSpeak()
    {
        if (!TryBeginTrigger(HOTKEY_ID_SPEAK)) return;
        SpeakRequested?.Invoke();
    }

    private void TriggerStop()
    {
        if (!TryBeginTrigger(HOTKEY_ID_STOP)) return;
        StopRequested?.Invoke();
    }

    private void TriggerPause()
    {
        if (!TryBeginTrigger(HOTKEY_ID_PAUSE)) return;
        PauseRequested?.Invoke();
    }

    private void TriggerNext()
    {
        if (!TryBeginTrigger(HOTKEY_ID_NEXT)) return;
        NextRequested?.Invoke();
    }

    private void TriggerPrev()
    {
        if (!TryBeginTrigger(HOTKEY_ID_PREV)) return;
        PrevRequested?.Invoke();
    }

    private void TriggerHud()
    {
        if (!TryBeginTrigger(HOTKEY_ID_HUD)) return;
        HudRequested?.Invoke();
    }

    private void TriggerEscape()
    {
        if (!TryBeginTrigger(VK_ESCAPE)) return;
        EscapeRequested?.Invoke();
    }

    private bool TryBeginTrigger(int id)
    {
        long now = Environment.TickCount64;
        lock (_triggerLock)
        {
            if (_lastTriggerTimes.TryGetValue(id, out long lastTime)
                && now - lastTime < DuplicateTriggerWindowMs)
            {
                return false;
            }

            _lastTriggerTimes[id] = now;
            return true;
        }
    }

    public void Dispose()
    {
        if (_hookId != 0)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = 0;
        }

        UnregisterHotkeys();
        _source?.RemoveHook(HwndHook);
        _source?.Dispose();
        _source = null;
        lock (_pressedKeysLock)
            _pressedKeys.Clear();
    }
}
