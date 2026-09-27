using ReadBack.Core.Models;

namespace ReadBack.Core.TTS;

/// <summary>
/// Composite engine that prefers Microsoft Edge Neural voices with automatic offline SAPI fallback.
/// </summary>
public class CompositeTTSEngine : ITTSEngine
{
    public string Name => "Smart Edge Neural with Offline Fallback";

    private readonly EdgeTTSClient _edgeClient = new();
    private readonly SapiTTSClient _sapiClient = new();

    public async Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        var list = new List<VoiceInfo>();
        list.AddRange(await _edgeClient.GetAvailableVoicesAsync(ct));
        list.AddRange(await _sapiClient.GetAvailableVoicesAsync(ct));
        return list;
    }

    public async Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
    {
        // If caller explicitly selected a SAPI voice, use SAPI directly
        if (voiceId.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase) ||
            voiceId.StartsWith("Windows:", StringComparison.OrdinalIgnoreCase))
        {
            string cleanVoice = voiceId.Replace("Windows: ", "");
            return await _sapiClient.SynthesizeToFileAsync(text, outputPath, cleanVoice, rate, ct);
        }

        // Otherwise attempt Edge Neural
        try
        {
            bool success = await _edgeClient.SynthesizeToFileAsync(text, outputPath, voiceId, rate, ct);
            if (success) return true;
        }
        catch
        {
            // Transparent fallback to offline SAPI
        }

        // Fallback to SAPI
        return await _sapiClient.SynthesizeToFileAsync(text, outputPath, "", rate, ct);
    }
}
