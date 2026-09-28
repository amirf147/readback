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

namespace ReadBack.Core.TTS;

/// <summary>
/// Composite engine that prefers Microsoft Edge Neural voices with automatic offline SAPI fallback.
/// </summary>
public class CompositeTTSEngine : ITTSEngine
{
    public string Name => "Modular TTS Composite Engine";

    private readonly EdgeTTSClient _edgeClient;
    private readonly SapiTTSClient _sapiClient;
    private readonly List<ITTSEngine> _customEngines = new();

    public EdgeTTSClient EdgeClient => _edgeClient;
    public SapiTTSClient SapiClient => _sapiClient;

    public CompositeTTSEngine()
    {
        _edgeClient = new EdgeTTSClient();
        _sapiClient = new SapiTTSClient();
    }

    public void RegisterEngine(ITTSEngine engine)
    {
        if (engine != null && !_customEngines.Contains(engine))
        {
            _customEngines.Add(engine);
        }
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        var list = new List<VoiceInfo>();

        try
        {
            list.AddRange(await _edgeClient.GetAvailableVoicesAsync(ct));
        }
        catch { }

        try
        {
            list.AddRange(await _sapiClient.GetAvailableVoicesAsync(ct));
        }
        catch { }

        foreach (var engine in _customEngines)
        {
            try
            {
                list.AddRange(await engine.GetAvailableVoicesAsync(ct));
            }
            catch { }
        }

        return list;
    }

    public async Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
    {
        // 1. Direct offline SAPI request
        if (voiceId.StartsWith("sapi:", StringComparison.OrdinalIgnoreCase) ||
            voiceId.StartsWith("Windows:", StringComparison.OrdinalIgnoreCase))
        {
            return await _sapiClient.SynthesizeToFileAsync(text, outputPath, voiceId, rate, ct);
        }

        // 2. Custom registered engines
        foreach (var customEngine in _customEngines)
        {
            try
            {
                bool handled = await customEngine.SynthesizeToFileAsync(text, outputPath, voiceId, rate, ct);
                if (handled && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                    return true;
            }
            catch { }
        }

        // 3. Online Edge Neural TTS
        try
        {
            bool success = await _edgeClient.SynthesizeToFileAsync(text, outputPath, voiceId, rate, ct);
            if (success && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                return true;
        }
        catch
        {
            // Network failure or rate limit -> seamlessly fall back to local SAPI
        }

        // 4. Transparent offline fallback to local Windows voices
        return await _sapiClient.SynthesizeToFileAsync(text, outputPath, "", rate, ct);
    }
}
