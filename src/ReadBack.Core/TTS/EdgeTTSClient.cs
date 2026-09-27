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

    private const string WssEndpoint = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EA654972831454495D3D15D6";
    private const string Origin = "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0";

    public static readonly IReadOnlyList<VoiceInfo> PopularVoices = new List<VoiceInfo>
    {
        new("en-US-JennyNeural", "Jenny (US Natural Female)", "en-US", true, false),
        new("en-US-GuyNeural", "Guy (US Natural Male)", "en-US", true, false),
        new("en-US-AriaNeural", "Aria (US Expressive Female)", "en-US", true, false),
        new("en-US-ChristopherNeural", "Christopher (US Storyteller Male)", "en-US", true, false),
        new("en-US-EricNeural", "Eric (US Friendly Male)", "en-US", true, false),
        new("en-GB-SoniaNeural", "Sonia (UK Natural Female)", "en-GB", true, false),
        new("en-GB-RyanNeural", "Ryan (UK Natural Male)", "en-GB", true, false),
    };

    public Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(PopularVoices);
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

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(12));

            await ws.ConnectAsync(new Uri(WssEndpoint), timeoutCts.Token);

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
        catch
        {
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
