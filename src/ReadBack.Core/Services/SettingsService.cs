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

using System.Text.Json;
using ReadBack.Core.Models;

namespace ReadBack.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFile;

    public AppSettings CurrentSettings { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(string? customFilePath = null)
    {
        _settingsFile = customFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ReadBack",
            "settings.json"
        );
        CurrentSettings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                string json = File.ReadAllText(_settingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
            SettingsChanged?.Invoke(this, CurrentSettings);
        }
        catch { }
    }
}
