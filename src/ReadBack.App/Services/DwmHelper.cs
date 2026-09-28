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
using System.Runtime.InteropServices;

namespace ReadBack.App.Services;

public static class DwmHelper
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;

    public static void ApplyBackdrop(IntPtr hwnd, int backdropType, bool isDarkMode)
    {
        if (hwnd == IntPtr.Zero) return;

        try
        {
            // Windows 11 rounded corners
            int cornerRound = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerRound, sizeof(int));

            // Immersive dark mode / light mode
            int darkMode = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // Windows 11 (22H2+) System Backdrop: 0=None, 2=Mica, 3=Acrylic, 4=Tabbed
            if (backdropType > 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            }
        }
        catch
        {
            // Graceful fallback on older Windows 10 versions where DWM attributes are ignored
        }
    }
}
