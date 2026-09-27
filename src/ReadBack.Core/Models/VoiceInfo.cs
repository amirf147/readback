namespace ReadBack.Core.Models;

public record VoiceInfo(
    string Id,
    string DisplayName,
    string Locale,
    bool IsNeural,
    bool IsOffline
);
