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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Xunit;
using Xunit.Abstractions;

namespace ReadBack.Tests;

public class HotkeyTests
{
    private readonly ITestOutputHelper _output;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    public HotkeyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_RegisterHotKey()
    {
        var thread = new Thread(() =>
        {
            var win = new Window { Width = 0, Height = 0, WindowStyle = WindowStyle.None };
            win.Show();
            nint hwnd = new WindowInteropHelper(win).Handle;

            bool resSpeak = RegisterHotKey(hwnd, 9001, 0x0002 | 0x0001 | 0x4000, 0x43); // Ctrl+Alt+C
            int errSpeak = Marshal.GetLastWin32Error();
            _output.WriteLine($"Ctrl+Alt+C registered: {resSpeak}, Win32Error: {errSpeak}");

            bool resStop = RegisterHotKey(hwnd, 9002, 0x0002 | 0x0001 | 0x4000, 0x58); // Ctrl+Alt+X
            int errStop = Marshal.GetLastWin32Error();
            _output.WriteLine($"Ctrl+Alt+X registered: {resStop}, Win32Error: {errStop}");

            if (resSpeak) UnregisterHotKey(hwnd, 9001);
            if (resStop) UnregisterHotKey(hwnd, 9002);

            win.Close();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
