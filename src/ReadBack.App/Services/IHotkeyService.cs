namespace ReadBack.App.Services;

public interface IHotkeyService : IDisposable
{
    void Initialize(nint windowHandle);
    void RegisterHotkeys();
    void UnregisterHotkeys();

    event Action? SpeakRequested;
    event Action? StopRequested;
    event Action? PauseRequested;
    event Action? NextRequested;
    event Action? PrevRequested;
}
