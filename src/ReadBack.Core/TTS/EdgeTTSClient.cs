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
using System.Net.WebSockets;
using System.Security;
using System.Text;
using ReadBack.Core.Models;

namespace ReadBack.Core.TTS;

/// <summary>
/// Ultra-fast pure .NET WebSocket client for Microsoft Edge Neural TTS.
/// Requires zero third-party packages or python runtimes.
/// </summary>
public class EdgeTTSClient : ITTSEngine
{
    public string Name => "Microsoft Edge Neural TTS";

    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string SecMsGecVersion = "1-143.0.3650.75";
    private const string Origin = "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0";
    private const string VoiceListUrl = $"https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list?trustedclienttoken={TrustedClientToken}";

    private static readonly HttpClient _httpClient = new();

    public static readonly IReadOnlyList<VoiceInfo> PopularVoices = new List<VoiceInfo>
    {
        new("en-US-ChristopherNeural", "Microsoft Christopher Online (Natural) - English (United States)", "en-US", true, false),
        new("en-US-JennyNeural", "Microsoft Jenny Online (Natural) - English (United States)", "en-US", true, false),
        new("en-US-GuyNeural", "Microsoft Guy Online (Natural) - English (United States)", "en-US", true, false),
        new("en-US-AriaNeural", "Microsoft Aria Online (Natural) - English (United States)", "en-US", true, false),
        new("en-US-EricNeural", "Microsoft Eric Online (Natural) - English (United States)", "en-US", true, false),
        new("en-US-EmmaMultilingualNeural", "Microsoft Emma Online (Natural) - English (United States)", "en-US", true, false),
        new("en-GB-SoniaNeural", "Microsoft Sonia Online (Natural) - English (United Kingdom)", "en-GB", true, false),
        new("en-GB-RyanNeural", "Microsoft Ryan Online (Natural) - English (United Kingdom)", "en-GB", true, false),
    };

    private static IReadOnlyList<VoiceInfo>? _cachedVoices;

    public async Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        if (_cachedVoices != null && _cachedVoices.Count > 0)
            return _cachedVoices;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, VoiceListUrl);
            req.Headers.Add("User-Agent", UserAgent);
            var resp = await _httpClient.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var list = new List<VoiceInfo>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    string shortName = element.GetProperty("ShortName").GetString() ?? "";
                    string friendlyName = element.TryGetProperty("FriendlyName", out var fn) ? (fn.GetString() ?? shortName) : shortName;
                    string locale = element.TryGetProperty("Locale", out var loc) ? (loc.GetString() ?? "en-US") : "en-US";
                    if (!string.IsNullOrEmpty(shortName))
                    {
                        list.Add(new VoiceInfo(shortName, friendlyName, locale, true, false));
                    }
                }

                if (list.Count > 0)
                {
                    _cachedVoices = list;
                    return list;
                }
            }
        }
        catch
        {
            // Fall back to PopularVoices if offline or fetch fails
        }

        return PopularVoices;
    }

    private static string GenerateSecMsGec()
    {
        // Switch to Windows file time epoch (1601-01-01 00:00:00 UTC) = 11644473600 seconds
        double unixSec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        double ticks = unixSec + 11644473600.0;
        // Round down to the nearest 5 minutes (300 seconds)
        ticks -= ticks % 300;
        // Convert to 100-nanosecond intervals
        ticks *= 10000000.0;
        string strToHash = $"{ticks:0}{TrustedClientToken}";
        byte[] hash = System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(strToHash));
        return Convert.ToHexString(hash);
    }

    public async Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Origin", Origin);
        ws.Options.SetRequestHeader("User-Agent", UserAgent);
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("Cookie", $"muid={Guid.NewGuid():N};");

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

            string secMsGec = GenerateSecMsGec();
            string connId = Guid.NewGuid().ToString("N");
            string wsUrl = $"wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken={TrustedClientToken}&Sec-MS-GEC={secMsGec}&Sec-MS-GEC-Version={SecMsGecVersion}&ConnectionId={connId}";

            await ws.ConnectAsync(new Uri(wsUrl), timeoutCts.Token);

            // 1. Send speech.config message
            string configMsg = "Content-Type:application/json;charset=utf-8\r\nPath:speech.config\r\n\r\n{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"true\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            byte[] configBytes = Encoding.UTF8.GetBytes(configMsg);
            await ws.SendAsync(new ArraySegment<byte>(configBytes), WebSocketMessageType.Text, true, timeoutCts.Token);

            // 2. Build and send SSML message
            string reqId = Guid.NewGuid().ToString("N");
            string escapedText = SecurityElement.Escape(text);
            string ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='{voiceId}'><prosody pitch='+0Hz' rate='{rate}' volume='+0%'>{escapedText}</prosody></voice></speak>";
            string ssmlMsg = $"X-RequestId:{reqId}\r\nContent-Type:application/ssml+xml\r\nPath:ssml\r\n\r\n{ssml}";
            byte[] ssmlBytes = Encoding.UTF8.GetBytes(ssmlMsg);
            await ws.SendAsync(new ArraySegment<byte>(ssmlBytes), WebSocketMessageType.Text, true, timeoutCts.Token);

            // 3. Receive binary audio packets
            var tempPath = outputPath + ".tmp";
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[16384];

                while (ws.State == WebSocketState.Open && !timeoutCts.Token.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary && result.Count > 2)
                    {
                        // In Edge TTS binary messages:
                        // First 2 bytes are big-endian integer indicating header length
                        int headerLength = (buffer[0] << 8) | buffer[1];
                        int audioOffset = 2 + headerLength;
                        if (result.Count > audioOffset)
                        {
                            int audioLength = result.Count - audioOffset;
                            await fileStream.WriteAsync(buffer.AsMemory(audioOffset, audioLength), timeoutCts.Token);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string msgText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (msgText.Contains("Path:turn.end"))
                        {
                            break; // Done with synthesis
                        }
                    }
                }
            }

            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tempPath, outputPath);
            return new FileInfo(outputPath).Length > 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EdgeTTS ERROR] {ex}");
            if (File.Exists(outputPath + ".tmp"))
            {
                try { File.Delete(outputPath + ".tmp"); } catch { }
            }
            return false;
        }
        finally
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting)
            {
                try { await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
            }
        }
    }
}
