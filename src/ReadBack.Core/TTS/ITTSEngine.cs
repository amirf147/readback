using ReadBack.Core.Models;

namespace ReadBack.Core.TTS;

public interface ITTSEngine
{
    string Name { get; }
    Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default);
    Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default);
}
