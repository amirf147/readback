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
namespace ReadBack.Core.Models;

public class AppSettings
{
    public string Voice { get; set; } = "en-US-ChristopherNeural";
    public string Speed { get; set; } = "+0%";
    public bool SkipCodeBlocks { get; set; } = true;
    public bool SoundFeedback { get; set; } = true;
    public bool OfflineOnly { get; set; } = false;
    public bool ShowFloatingHud { get; set; } = true;
    public string HudTheme { get; set; } = "win11-glass";
    public bool LaunchAtStartup { get; set; } = false;
    public bool FirstRunPromptShown { get; set; } = false;

    // Hotkey configurations
    public string HotkeySpeak { get; set; } = "ctrl+alt+c";
    public string HotkeyStop { get; set; } = "ctrl+alt+x";
    public string HotkeyPause { get; set; } = "ctrl+alt+space";
    public string HotkeyNext { get; set; } = "ctrl+alt+right";
    public string HotkeyPrev { get; set; } = "ctrl+alt+left";
}
