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
using WpfClipboard = System.Windows.Clipboard;

namespace ReadBack.App.Services;

public class WindowsClipboardService : IClipboardService
{
    private const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    public string? GetText()
    {
        // Try native Win32 clipboard first (works from any thread, avoids COM/WPF STA requirements)
        for (int i = 0; i < 10; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                    if (hData != IntPtr.Zero)
                    {
                        IntPtr pText = GlobalLock(hData);
                        if (pText != IntPtr.Zero)
                        {
                            try
                            {
                                string? text = Marshal.PtrToStringUni(pText);
                                if (!string.IsNullOrWhiteSpace(text))
                                    return text;
                            }
                            finally
                            {
                                GlobalUnlock(hData);
                            }
                        }
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }

            Thread.Sleep(30);
        }

        // Secondary fallback to WPF Clipboard on STA thread
        string? fallbackText = null;
        try
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                if (WpfClipboard.ContainsText())
                    fallbackText = WpfClipboard.GetText();
            }
            else
            {
                var staThread = new Thread(() =>
                {
                    try
                    {
                        if (WpfClipboard.ContainsText())
                            fallbackText = WpfClipboard.GetText();
                    }
                    catch { }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join(500);
            }
        }
        catch { }

        return string.IsNullOrWhiteSpace(fallbackText) ? null : fallbackText;
    }
}
