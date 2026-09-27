namespace ReadBack.Core.Models;

public class AppSettings
{
    public string Voice { get; set; } = "en-US-JennyNeural";
    public string Speed { get; set; } = "+0%";
    public bool SkipCodeBlocks { get; set; } = true;
    public bool SoundFeedback { get; set; } = true;
    public bool OfflineOnly { get; set; } = false;
    public bool ShowFloatingHud { get; set; } = true;

    // Hotkey configurations
    public string HotkeySpeak { get; set; } = "ctrl+alt+c";
    public string HotkeyStop { get; set; } = "ctrl+alt+x";
    public string HotkeyPause { get; set; } = "ctrl+alt+space";
    public string HotkeyNext { get; set; } = "ctrl+alt+right";
    public string HotkeyPrev { get; set; } = "ctrl+alt+left";
}
