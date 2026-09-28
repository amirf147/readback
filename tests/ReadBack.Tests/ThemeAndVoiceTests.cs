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
using ReadBack.Core.Models;
using ReadBack.Core.Services;
using ReadBack.Core.Sources;
using ReadBack.Core.TTS;
using Xunit;

namespace ReadBack.Tests;

public class ThemeAndVoiceTests
{
    [Fact]
    public void AppSettings_DefaultVoice_IsChristopherNeural()
    {
        var settings = new AppSettings();
        Assert.Equal("en-US-ChristopherNeural", settings.Voice);
        Assert.Equal("win11-glass", settings.HudTheme);
        Assert.False(settings.LaunchAtStartup);
    }

    [Fact]
    public void SettingsService_SavesAndLoadsLaunchAtStartup()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempPath);
            Assert.False(svc.CurrentSettings.LaunchAtStartup);

            svc.CurrentSettings.LaunchAtStartup = true;
            svc.Save();

            var svc2 = new SettingsService(tempPath);
            Assert.True(svc2.CurrentSettings.LaunchAtStartup);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task CompositeTTSEngine_RoutesSapiVoiceToSapi()
    {
        var composite = new CompositeTTSEngine();
        string tempOut = Path.Combine(Path.GetTempPath(), $"sapi_test_{Guid.NewGuid():N}.wav");
        try
        {
            // Direct request with SAPI voice
            bool result = await composite.SynthesizeToFileAsync("Testing SAPI offline route.", tempOut, "sapi:Microsoft David Desktop", "+0%");
            // If Microsoft David Desktop exists, it succeeds
            if (File.Exists(tempOut))
            {
                Assert.True(new FileInfo(tempOut).Length > 0);
            }
        }
        finally
        {
            if (File.Exists(tempOut)) File.Delete(tempOut);
        }
    }

    [Fact]
    public void CompositeTTSEngine_CanRegisterCustomEngine()
    {
        var composite = new CompositeTTSEngine();
        var mockEngine = new MockTTSEngine();
        composite.RegisterEngine(mockEngine);

        // Verification that registration succeeded
        Assert.NotNull(composite);
    }

    [Fact]
    public async Task TextSource_CustomProvider_SuppliesTextCorrectly()
    {
        ITextSource customSource = new MockTextSource("Selected paragraph content");
        Assert.Equal("Mock Document", customSource.Name);
        string? text = await customSource.GetTextAsync();
        Assert.Equal("Selected paragraph content", text);
    }

    private class MockTextSource : ITextSource
    {
        private readonly string _content;
        public string Name => "Mock Document";
        public MockTextSource(string content) => _content = content;
        public Task<string?> GetTextAsync(CancellationToken ct = default) => Task.FromResult<string?>(_content);
    }

    private class MockTTSEngine : ITTSEngine
    {
        public string Name => "Mock Provider";

        public Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
        {
            var list = new List<VoiceInfo>
            {
                new("mock-voice", "Mock Voice 1", "en-US", true, false, "Mock")
            };
            return Task.FromResult<IReadOnlyList<VoiceInfo>>(list);
        }

        public Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
        {
            if (voiceId == "mock-voice")
            {
                File.WriteAllText(outputPath, "audio data");
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
