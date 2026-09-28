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
using System.Speech.Synthesis;
using AppVoiceInfo = ReadBack.Core.Models.VoiceInfo;

namespace ReadBack.Core.TTS;

/// <summary>
/// Offline Windows SAPI speech synthesis fallback.
/// Works with zero internet connection using installed Windows voices.
/// </summary>
public class SapiTTSClient : ITTSEngine
{
    public string Name => "Windows SAPI (Offline)";

    public Task<IReadOnlyList<AppVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        var list = new List<AppVoiceInfo>();
        try
        {
            using var synth = new SpeechSynthesizer();
            foreach (var voice in synth.GetInstalledVoices())
            {
                if (voice.Enabled)
                {
                    list.Add(new AppVoiceInfo(
                        $"sapi:{voice.VoiceInfo.Name}",
                        $"{voice.VoiceInfo.Name} (Windows Offline)",
                        voice.VoiceInfo.Culture.Name,
                        false,
                        true,
                        "Windows (Offline)"
                    ));
                }
            }
        }
        catch { }
        return Task.FromResult<IReadOnlyList<AppVoiceInfo>>(list);
    }

    public Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(false);

        try
        {
            using var synth = new SpeechSynthesizer();
            string cleanVoice = (voiceId ?? "").Replace("sapi:", "").Replace("Windows: ", "").Trim();
            if (!string.IsNullOrEmpty(cleanVoice))
            {
                try { synth.SelectVoice(cleanVoice); } catch { }
            }

            synth.Rate = ParseSapiRate(rate);

            // SAPI outputs WAV
            string wavPath = outputPath;
            if (!wavPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                wavPath = Path.ChangeExtension(outputPath, ".wav");
            }

            synth.SetOutputToWaveFile(wavPath);
            synth.Speak(text);
            synth.SetOutputToNull();

            if (wavPath != outputPath && File.Exists(wavPath))
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(wavPath, outputPath);
            }

            return Task.FromResult(File.Exists(outputPath) && new FileInfo(outputPath).Length > 0);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static int ParseSapiRate(string rate)
    {
        if (string.IsNullOrEmpty(rate)) return 0;
        rate = rate.Replace("%", "").Trim();
        if (int.TryParse(rate, out int pct))
        {
            int mapped = pct / 10;
            return Math.Clamp(mapped, -10, 10);
        }
        return 0;
    }
}
