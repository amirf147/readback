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
                list.Add(new AppVoiceInfo(
                    voice.VoiceInfo.Name,
                    $"Windows: {voice.VoiceInfo.Name}",
                    voice.VoiceInfo.Culture.Name,
                    false,
                    true
                ));
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
            if (!string.IsNullOrEmpty(voiceId))
            {
                try { synth.SelectVoice(voiceId); } catch { }
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
